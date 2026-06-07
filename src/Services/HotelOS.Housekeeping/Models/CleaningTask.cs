using HotelOS.Shared.Enums;

namespace HotelOS.Housekeeping.Models;

/// <summary>A unit of cleaning work created when Reception asks for a room to be cleaned.</summary>
public class CleaningTask
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }              // local copy; no cross-service FK
    public string RoomNumber { get; set; } = default!;
    public int Floor { get; set; }
    public Guid? HousekeeperId { get; set; }
    public CleaningTaskStatus Status { get; set; } = CleaningTaskStatus.Pending;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? Notes { get; set; }
}
