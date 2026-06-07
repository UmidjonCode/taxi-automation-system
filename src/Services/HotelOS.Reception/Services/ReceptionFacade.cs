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

    public async Task<IReadOnlyList<Room>> SearchRoomsAsync(
        RoomStyle? style, DateTime checkIn, DateTime checkOut, Guid? branchId, CancellationToken ct = default)
    {
        var rooms = await _db.Rooms.AsNoTracking()
            .Where(r => r.Status != RoomStatus.OutOfService && r.CleanStatus == RoomCleanStatus.Clean)
            .Where(r => style == null || r.Style == style)
            .Where(r => branchId == null || r.BranchId == branchId)
            .ToListAsync(ct);

        var free = new List<Room>();
        foreach (var room in rooms)
            if (!await HasOverlapAsync(room.Id, checkIn, checkOut, ct))
                free.Add(room);
        return free;
    }

    public async Task<BookingResponse> CreateBookingAsync(CreateBookingRequest req, CancellationToken ct = default)
    {
        if (req.CheckOut.Date <= req.CheckIn.Date)
            throw new InvalidOperationException("CheckOut must be after CheckIn.");

        var branchId = req.BranchId ?? ReceptionSeeder.DefaultBranchId;
        var guest = await ResolveGuestAsync(req, ct);

        // Candidate set: right style, in service, clean and free for the dates.
        var candidateRooms = await _db.Rooms
            .Where(r => r.Style == req.Style && r.Status != RoomStatus.OutOfService
                        && r.CleanStatus == RoomCleanStatus.Clean && r.BranchId == branchId)
            .ToListAsync(ct);

        var free = new List<Room>();
        foreach (var room in candidateRooms)
            if (!await HasOverlapAsync(room.Id, req.CheckIn, req.CheckOut, ct))
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

    // ---------- helpers ----------

    private Task<bool> HasOverlapAsync(Guid roomId, DateTime checkIn, DateTime checkOut, CancellationToken ct) =>
        _db.Bookings.AnyAsync(b =>
            b.RoomId == roomId
            && (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.CheckedIn)
            && b.CheckIn < checkOut && checkIn < b.CheckOut, ct);

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
