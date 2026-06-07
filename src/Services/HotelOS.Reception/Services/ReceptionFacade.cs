using System.Data;
using HotelOS.Reception.Data;
using HotelOS.Reception.Models;
using HotelOS.Shared.Algorithms.Billing;
using HotelOS.Shared.Algorithms.RoomAssignment;
using HotelOS.Shared.Enums;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Reception.Services;

/// <summary>
/// Facade pattern: one entry point that hides the whole booking subsystem —
/// rooms, guests, the assignment &amp; billing algorithms, key issuing, and event
/// publishing — behind a few clear operations the controllers call.
/// </summary>
public sealed class ReceptionFacade
{
    /// <summary>How long a room hold lasts before auto-expiry.</summary>
    public static readonly TimeSpan HoldDuration = TimeSpan.FromMinutes(5);

    private readonly ReceptionDbContext _db;
    private readonly IRoomAssignmentStrategy _assignment;
    private readonly IBillingCalculator _billing;
    private readonly RoomKeyFactory _keyFactory;
    private readonly IEventBus _bus;
    private readonly ILogger<ReceptionFacade> _logger;

    public ReceptionFacade(
        ReceptionDbContext db,
        IRoomAssignmentStrategy assignment,
        IBillingCalculator billing,
        RoomKeyFactory keyFactory,
        IEventBus bus,
        ILogger<ReceptionFacade> logger)
    {
        _db = db;
        _assignment = assignment;
        _billing = billing;
        _keyFactory = keyFactory;
        _bus = bus;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ROOM SEARCH
    // ═══════════════════════════════════════════════════════════════════

    public record RoomSearchResult(Room Room, bool IsAvailable);

    public async Task<IReadOnlyList<RoomSearchResult>> SearchRoomsAsync(
        RoomStyle? style, DateTime checkIn, DateTime checkOut, Guid? branchId, CancellationToken ct = default)
    {
        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.Status != RoomStatus.OutOfService && r.CleanStatus == RoomCleanStatus.Clean)
            .Where(r => style == null || r.Style == style)
            .Where(r => branchId == null || r.BranchId == branchId)
            .ToListAsync(ct);

        var results = new List<RoomSearchResult>();
        foreach (var room in rooms)
        {
            var hasOverlap = await HasOverlapOrHoldAsync(room.Id, checkIn, checkOut, ct);
            results.Add(new RoomSearchResult(room, !hasOverlap));
        }
        return results;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  ROOM HOLD SYSTEM — 5-minute temporary reservation during payment
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Places a 5-minute hold on a specific room for the given dates.
    /// Uses a SERIALIZABLE transaction to prevent two users from holding the same room.
    /// If the room is already booked or held by another user, throws InvalidOperationException.
    /// </summary>
    public async Task<RoomHold> CreateHoldAsync(Guid roomId, Guid guestId, DateTime checkIn, DateTime checkOut, CancellationToken ct = default)
    {
        if (checkOut.Date <= checkIn.Date)
            throw new InvalidOperationException("CheckOut must be after CheckIn.");

        // Use SERIALIZABLE isolation to prevent the TOCTOU race condition.
        // SQLite implements this as BEGIN EXCLUSIVE — only one writer at a time.
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var room = await _db.Rooms.FindAsync(new object[] { roomId }, ct)
                ?? throw new KeyNotFoundException($"Room {roomId} not found.");

            // Check for overlapping confirmed bookings
            var hasBookingOverlap = await HasOverlapAsync(roomId, checkIn, checkOut, ct);
            if (hasBookingOverlap)
                throw new InvalidOperationException("This room is already booked for the selected dates.");

            // Check for overlapping active holds by OTHER users
            var hasHoldOverlap = await _db.RoomHolds.AnyAsync(h =>
                h.RoomId == roomId
                && h.Status == RoomHoldStatus.Active
                && h.ExpiresAt > DateTime.UtcNow
                && h.CheckIn < checkOut && checkIn < h.CheckOut
                && h.GuestId != guestId, ct);

            if (hasHoldOverlap)
                throw new InvalidOperationException("This room is currently being held by another guest. Please try again in a few minutes.");

            // Cancel any existing active hold by the SAME user for the same room+dates
            var existingHold = await _db.RoomHolds.FirstOrDefaultAsync(h =>
                h.RoomId == roomId && h.GuestId == guestId && h.Status == RoomHoldStatus.Active, ct);
            if (existingHold != null)
                existingHold.Status = RoomHoldStatus.Released;

            var hold = new RoomHold
            {
                RoomId = roomId,
                GuestId = guestId,
                CheckIn = checkIn,
                CheckOut = checkOut,
                ExpiresAt = DateTime.UtcNow.Add(HoldDuration)
            };

            _db.RoomHolds.Add(hold);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            _logger.LogInformation("Hold {HoldId} created for room {RoomId} by guest {GuestId}. Expires at {Expiry}.",
                hold.Id, roomId, guestId, hold.ExpiresAt);

            return hold;
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>
    /// Converts an active, non-expired hold into a confirmed booking.
    /// Uses a SERIALIZABLE transaction — if the hold has expired, the booking is rejected.
    /// </summary>
    public async Task<BookingResponse> ConfirmHoldAsync(Guid holdId, decimal advancePayment, CancellationToken ct = default)
    {
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var hold = await _db.RoomHolds
                .Include(h => h.Room)
                .FirstOrDefaultAsync(h => h.Id == holdId, ct)
                ?? throw new KeyNotFoundException($"Hold {holdId} not found.");

            if (hold.Status != RoomHoldStatus.Active)
                throw new InvalidOperationException($"Hold is no longer active (status: {hold.Status}).");

            if (hold.ExpiresAt <= DateTime.UtcNow)
            {
                hold.Status = RoomHoldStatus.Expired;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                throw new InvalidOperationException("Your hold has expired. The room may have been taken. Please search again.");
            }

            // Double-check no booking was sneaked in (belt and suspenders)
            var hasOverlap = await HasOverlapAsync(hold.RoomId, hold.CheckIn, hold.CheckOut, ct);
            if (hasOverlap)
            {
                hold.Status = RoomHoldStatus.Released;
                await _db.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                throw new InvalidOperationException("Room was booked by another process. Please search again.");
            }

            // Convert hold → booking
            hold.Status = RoomHoldStatus.Confirmed;

            var booking = new Booking
            {
                GuestId = hold.GuestId,
                RoomId = hold.RoomId,
                BranchId = hold.Room!.BranchId,
                CheckIn = hold.CheckIn,
                CheckOut = hold.CheckOut,
                NightlyRate = hold.Room.NightlyRate,
                AdvancePayment = advancePayment,
                Status = advancePayment > 0 ? BookingStatus.Confirmed : BookingStatus.Pending
            };

            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Publish events outside the transaction
            await _bus.PublishAsync(new BookingCreatedEvent
            {
                BookingId = booking.Id,
                GuestId = hold.GuestId,
                RoomId = hold.RoomId,
                RoomNumber = hold.Room.RoomNumber,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                BranchId = booking.BranchId
            }, ct);

            if (booking.Status == BookingStatus.Confirmed)
                await _bus.PublishAsync(new BookingConfirmedEvent
                {
                    BookingId = booking.Id,
                    GuestId = hold.GuestId,
                    RoomId = hold.RoomId,
                    AdvancePayment = booking.AdvancePayment
                }, ct);

            _logger.LogInformation("Hold {HoldId} confirmed → Booking {BookingId} for room {Room}.",
                holdId, booking.Id, hold.Room.RoomNumber);

            return new BookingResponse
            {
                BookingId = booking.Id,
                GuestId = hold.GuestId,
                RoomId = hold.RoomId,
                RoomNumber = hold.Room.RoomNumber,
                MatchTier = 0,
                AssignmentReason = "Hold confirmed — room secured.",
                Status = booking.Status.ToString(),
                NightlyRate = booking.NightlyRate,
                AdvancePayment = booking.AdvancePayment
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    /// <summary>Guest manually releases a hold before it expires.</summary>
    public async Task ReleaseHoldAsync(Guid holdId, CancellationToken ct = default)
    {
        var hold = await _db.RoomHolds.FindAsync(new object[] { holdId }, ct)
            ?? throw new KeyNotFoundException($"Hold {holdId} not found.");

        if (hold.Status != RoomHoldStatus.Active)
            throw new InvalidOperationException($"Hold is not active (status: {hold.Status}).");

        hold.Status = RoomHoldStatus.Released;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Hold {HoldId} released manually.", holdId);
    }

    /// <summary>Expires all holds that have passed their ExpiresAt time. Called by HoldExpiryService.</summary>
    public async Task<int> ExpireStaleHoldsAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var staleHolds = await _db.RoomHolds
            .Where(h => h.Status == RoomHoldStatus.Active && h.ExpiresAt <= now)
            .ToListAsync(ct);

        foreach (var hold in staleHolds)
            hold.Status = RoomHoldStatus.Expired;

        if (staleHolds.Count > 0)
        {
            await _db.SaveChangesAsync(ct);
            _logger.LogInformation("Expired {Count} stale room holds.", staleHolds.Count);
        }

        return staleHolds.Count;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  GUEST LOOKUPS
    // ═══════════════════════════════════════════════════════════════════

    public async Task<IReadOnlyList<BookingResponse>> GetBookingsByEmailAsync(string email, CancellationToken ct = default)
    {
        var guest = await _db.Guests.FirstOrDefaultAsync(g => g.Email == email, ct);
        if (guest == null) return Array.Empty<BookingResponse>();

        var bookings = await _db.Bookings
            .Include(b => b.Room)
            .Where(b => b.GuestId == guest.Id)
            .OrderByDescending(b => b.CheckIn)
            .ToListAsync(ct);

        return bookings.Select(b => new BookingResponse
        {
            BookingId = b.Id,
            GuestId = b.GuestId,
            RoomId = b.RoomId,
            RoomNumber = b.Room!.RoomNumber,
            MatchTier = 0,
            AssignmentReason = "",
            Status = b.Status.ToString(),
            NightlyRate = b.NightlyRate,
            AdvancePayment = b.AdvancePayment
        }).ToList();
    }

    // ═══════════════════════════════════════════════════════════════════
    //  DIRECT BOOKING (still supported for Receptionist / API use)
    // ═══════════════════════════════════════════════════════════════════

    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest req, CancellationToken ct = default)
    {
        if (req.CheckOut.Date <= req.CheckIn.Date)
            throw new InvalidOperationException("CheckOut must be after CheckIn.");

        // SERIALIZABLE transaction to prevent the TOCTOU double-booking race.
        using var tx = await _db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);

        try
        {
            var branchId = req.BranchId ?? ReceptionSeeder.DefaultBranchId;
            var guest = await ResolveGuestAsync(req, ct);

            // Candidate set: right style, in service, clean and free for the dates.
            var candidateRooms = await _db.Rooms
                .Where(r => r.Style == req.Style && r.Status != RoomStatus.OutOfService
                            && r.CleanStatus == RoomCleanStatus.Clean && r.BranchId == branchId)
                .ToListAsync(ct);

            var free = new List<Room>();
            foreach (var room in candidateRooms)
                if (!await HasOverlapOrHoldAsync(room.Id, req.CheckIn, req.CheckOut, ct))
                    free.Add(room);

            var assignmentRequest = new RoomAssignmentRequest
            {
                RequestedStyle = req.Style,
                PreferredFloor = req.PreferredFloor,
                ProximityPreference = req.ProximityPreference,
                CheckIn = req.CheckIn,
                CheckOut = req.CheckOut,
                BranchId = branchId
            };

            var result = _assignment.Assign(assignmentRequest, free.Select(ToCandidate).ToList());
            if (!result.Success || result.RoomId is null)
                throw new InvalidOperationException(result.Reason);

            var room2 = free.First(r => r.Id == result.RoomId);

            var booking = new Booking
            {
                GuestId = guest.Id,
                RoomId = room2.Id,
                BranchId = branchId,
                CheckIn = req.CheckIn,
                CheckOut = req.CheckOut,
                NightlyRate = room2.NightlyRate,
                AdvancePayment = req.AdvancePayment,
                Status = req.AdvancePayment > 0 ? BookingStatus.Confirmed : BookingStatus.Pending
            };
            _db.Bookings.Add(booking);
            await _db.SaveChangesAsync(ct);
            await tx.CommitAsync(ct);

            // Publish events outside the transaction
            await _bus.PublishAsync(new BookingCreatedEvent
            {
                BookingId = booking.Id,
                GuestId = guest.Id,
                RoomId = room2.Id,
                RoomNumber = room2.RoomNumber,
                CheckIn = booking.CheckIn,
                CheckOut = booking.CheckOut,
                BranchId = branchId
            }, ct);

            if (booking.Status == BookingStatus.Confirmed)
                await _bus.PublishAsync(new BookingConfirmedEvent
                {
                    BookingId = booking.Id,
                    GuestId = guest.Id,
                    RoomId = room2.Id,
                    AdvancePayment = booking.AdvancePayment
                }, ct);

            _logger.LogInformation("Booking {Id} created for room {Room} (tier {Tier}).",
                booking.Id, room2.RoomNumber, result.MatchTier);

            return new BookingResponse
            {
                BookingId = booking.Id,
                GuestId = guest.Id,
                RoomId = room2.Id,
                RoomNumber = room2.RoomNumber,
                MatchTier = result.MatchTier,
                AssignmentReason = result.Reason,
                Status = booking.Status.ToString(),
                NightlyRate = room2.NightlyRate,
                AdvancePayment = booking.AdvancePayment
            };
        }
        catch
        {
            await tx.RollbackAsync(ct);
            throw;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  BOOKING LIFECYCLE
    // ═══════════════════════════════════════════════════════════════════

    public async Task<BookingResponse> ConfirmBookingAsync(Guid bookingId, decimal advancePayment, CancellationToken ct = default)
    {
        var booking = await LoadBookingAsync(bookingId, includeRoom: true, ct);
        if (booking.Status != BookingStatus.Pending)
            throw new InvalidOperationException($"Only a Pending booking can be confirmed (current: {booking.Status}).");

        booking.AdvancePayment += advancePayment;
        booking.Status = BookingStatus.Confirmed;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new BookingConfirmedEvent
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = booking.RoomId,
            AdvancePayment = booking.AdvancePayment
        }, ct);

        return new BookingResponse
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = booking.RoomId,
            RoomNumber = booking.Room!.RoomNumber,
            MatchTier = 0,
            AssignmentReason = "Booking confirmed; advance payment recorded.",
            Status = booking.Status.ToString(),
            NightlyRate = booking.NightlyRate,
            AdvancePayment = booking.AdvancePayment
        };
    }

    public async Task<RefundResult> CancelBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await LoadBookingAsync(bookingId, includeRoom: false, ct);
        if (booking.Status is BookingStatus.Cancelled or BookingStatus.CheckedOut)
            throw new InvalidOperationException($"Cannot cancel a {booking.Status} booking.");

        var refund = _billing.CalculateRefund(new RefundContext
        {
            BookingId = booking.Id,
            CheckIn = booking.CheckIn,
            CancellationTime = DateTime.UtcNow,
            AdvancePayment = booking.AdvancePayment
        });

        booking.Status = BookingStatus.Cancelled;
        booking.CancelledAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new BookingCancelledEvent
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = booking.RoomId,
            CancelledAt = booking.CancelledAt.Value,
            RefundAmount = refund.RefundAmount
        }, ct);

        _logger.LogInformation("Booking {Id} cancelled. {Policy} Refund={Refund:0.00}",
            booking.Id, refund.PolicyApplied, refund.RefundAmount);
        return refund;
    }

    public async Task<CheckInResponse> CheckInAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await LoadBookingAsync(bookingId, includeRoom: true, ct);
        if (booking.Status != BookingStatus.Confirmed)
            throw new InvalidOperationException($"Booking must be Confirmed to check in (current: {booking.Status}).");

        var room = booking.Room!;
        booking.Status = BookingStatus.CheckedIn;
        room.Status = RoomStatus.Occupied;

        var key = _keyFactory.CreateForRoom(room, booking.GuestId);
        _db.RoomKeys.Add(key);
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new GuestCheckedInEvent
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = room.Id,
            Floor = room.Floor,
            CheckIn = DateTime.UtcNow
        }, ct);
        await _bus.PublishAsync(new KeyIssuedEvent
        {
            KeyId = key.Id,
            RoomId = room.Id,
            GuestId = booking.GuestId,
            IsMasterKey = key.IsMasterKey,
            IssuedAt = key.IssuedAt
        }, ct);

        return new CheckInResponse
        {
            BookingId = booking.Id,
            RoomNumber = room.RoomNumber,
            KeyCode = key.KeyCode,
            IsMasterKey = key.IsMasterKey
        };
    }

    public async Task<BillingResult> CheckOutAsync(Guid bookingId, CancellationToken ct = default)
    {
        var booking = await _db.Bookings
            .Include(b => b.Room)
            .Include(b => b.Extras)
            .FirstOrDefaultAsync(b => b.Id == bookingId, ct)
            ?? throw new KeyNotFoundException($"Booking {bookingId} not found.");

        if (booking.Status != BookingStatus.CheckedIn)
            throw new InvalidOperationException($"Booking must be CheckedIn to check out (current: {booking.Status}).");

        var room = booking.Room!;
        var bill = _billing.CalculateFinalBill(new BillingContext
        {
            BookingId = booking.Id,
            NightlyRate = booking.NightlyRate,
            CheckIn = booking.CheckIn,
            CheckOut = booking.CheckOut,
            RoomServiceTotal = booking.RoomServiceCharges,
            ExtraCharges = booking.Extras.Select(e => new BillingLineItem(e.Description, e.Amount)).ToList(),
            AdvancePayment = booking.AdvancePayment
        });

        booking.Status = BookingStatus.CheckedOut;
        room.Status = RoomStatus.Available;
        room.CleanStatus = RoomCleanStatus.Dirty; // must be cleaned before re-assignment

        var openKeys = await _db.RoomKeys
            .Where(k => k.RoomId == room.Id && k.IssuedTo == booking.GuestId && k.ReturnedAt == null)
            .ToListAsync(ct);
        foreach (var k in openKeys) k.ReturnedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new GuestCheckedOutEvent
        {
            BookingId = booking.Id,
            GuestId = booking.GuestId,
            RoomId = room.Id,
            Floor = room.Floor,
            CheckOut = DateTime.UtcNow,
            FinalBill = bill.GrandTotal
        }, ct);
        await _bus.PublishAsync(new RoomCleaningRequestedEvent
        {
            RoomId = room.Id,
            RoomNumber = room.RoomNumber,
            Floor = room.Floor
        }, ct);
        foreach (var k in openKeys)
            await _bus.PublishAsync(new KeyReturnedEvent
            {
                KeyId = k.Id,
                RoomId = room.Id,
                ReturnedAt = k.ReturnedAt!.Value
            }, ct);

        _logger.LogInformation("Booking {Id} checked out. Final bill {Total:0.00}.", booking.Id, bill.GrandTotal);
        return bill;
    }

    public async Task AddExtraChargeAsync(Guid bookingId, string description, decimal amount, CancellationToken ct = default)
    {
        var booking = await LoadBookingAsync(bookingId, includeRoom: false, ct);
        if (booking.Status is BookingStatus.CheckedOut or BookingStatus.Cancelled)
            throw new InvalidOperationException($"Cannot add charges to a {booking.Status} booking.");

        _db.BookingExtras.Add(new BookingExtra { BookingId = bookingId, Description = description, Amount = amount });
        await _db.SaveChangesAsync(ct);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  HELPERS
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Checks for overlapping confirmed bookings only (no holds).</summary>
    private Task<bool> HasOverlapAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct) =>
        _db.Bookings.AnyAsync(b =>
            b.RoomId == roomId
            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            && b.CheckIn < checkOut && checkIn < b.CheckOut, ct);

    /// <summary>Checks for overlapping bookings AND active holds.</summary>
    private async Task<bool> HasOverlapOrHoldAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct)
    {
        var hasBooking = await HasOverlapAsync(roomId, checkIn, checkOut, ct);
        if (hasBooking) return true;

        var now = DateTime.UtcNow;
        return await _db.RoomHolds.AnyAsync(h =>
            h.RoomId == roomId
            && h.Status == RoomHoldStatus.Active
            && h.ExpiresAt > now
            && h.CheckIn < checkOut && checkIn < h.CheckOut, ct);
    }

    private async Task<Guest> ResolveGuestAsync(CreateBookingRequest req, CancellationToken ct)
    {
        if (req.GuestId is { } id)
            return await _db.Guests.FindAsync(new object?[] { id }, ct)
                ?? throw new KeyNotFoundException($"Guest {id} not found.");

        if (req.Guest is null)
            throw new InvalidOperationException("Either GuestId or Guest details must be supplied.");

        var existing = await _db.Guests.FirstOrDefaultAsync(g => g.Email == req.Guest.Email, ct);
        if (existing is not null) return existing;

        var guest = new Guest
        {
            FullName = req.Guest.FullName,
            Email = req.Guest.Email,
            PhoneNumber = req.Guest.PhoneNumber,
            NationalId = req.Guest.NationalId
        };
        _db.Guests.Add(guest);
        await _db.SaveChangesAsync(ct);
        return guest;
    }

    private async Task<Booking> LoadBookingAsync(Guid id, bool includeRoom, CancellationToken ct)
    {
        var query = _db.Bookings.AsQueryable();
        if (includeRoom) query = query.Include(b => b.Room);
        return await query.FirstOrDefaultAsync(b => b.Id == id, ct)
            ?? throw new KeyNotFoundException($"Booking {id} not found.");
    }

    private static RoomCandidate ToCandidate(Room r) => new()
    {
        RoomId = r.Id,
        RoomNumber = r.RoomNumber,
        Floor = r.Floor,
        Style = r.Style,
        Status = r.Status,
        CleanStatus = r.CleanStatus,
        LastCleanedAt = r.LastCleanedAt,
        ProximityZone = r.ProximityZone
    };
}
