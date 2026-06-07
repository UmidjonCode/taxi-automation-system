using HotelOS.Shared.Enums;

namespace HotelOS.Reception.Models;

/// <summary>A physical room. Reception is the source of truth for room state.</summary>
public class Room
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string RoomNumber { get; set; } = default!;
    public int Floor { get; set; }
    public RoomStyle Style { get; set; }
    public decimal NightlyRate { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public RoomCleanStatus CleanStatus { get; set; } = RoomCleanStatus.Clean;
    public DateTime LastCleanedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Zone tag for proximity preferences, e.g. "Elevator" or "Quiet".</summary>
    public string? ProximityZone { get; set; }

    /// <summary>Base physical key code for the room; issued keys derive from this.</summary>
    public string KeyCode { get; set; } = default!;

    public Guid BranchId { get; set; }
}
