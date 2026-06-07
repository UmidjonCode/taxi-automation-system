namespace HotelOS.Reception.Models;

/// <summary>
/// Temporary hold on a room while the guest completes payment.
/// Holds expire after a configurable duration (default 5 minutes).
/// While active, no other guest can book or hold the same room for overlapping dates.
/// </summary>
public class RoomHold
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid RoomId { get; set; }
    public Room? Room { get; set; }

    public Guid GuestId { get; set; }
    public Guest? Guest { get; set; }

    public DateTime CheckIn { get; set; }
    public DateTime CheckOut { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>When this hold expires and the room becomes available again.</summary>
    public DateTime ExpiresAt { get; set; }

    public RoomHoldStatus Status { get; set; } = RoomHoldStatus.Active;
}

public enum RoomHoldStatus
{
    Active = 0,
    Confirmed = 1,   // Converted to a real booking
    Expired = 2,      // Timed out — room released
    Released = 3      // Guest manually cancelled the hold
}
