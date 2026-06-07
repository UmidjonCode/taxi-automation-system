using HotelOS.Shared.Enums;

namespace HotelOS.Reception.Models;

/// <summary>A guest's reservation of a room for a date range.</summary>
public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid GuestId { get; set; }
    public Guest? Guest { get; set; }

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid BranchId { get; set; }
    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }

    /// <summary>Rate captured at booking time so later rate changes don't alter the bill.</summary>
    public decimal NightlyRate { get; set; }
    public decimal AdvancePayment { get; set; }

    /// <summary>Running total of delivered room-service orders (accumulated via events).</summary>
    public decimal RoomServiceCharges { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CancelledAt { get; set; }

    public List<BookingExtra> Extras { get; set; } = new();
}
