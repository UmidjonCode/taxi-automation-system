using HotelOS.Shared.Enums;

namespace HotelOS.Maintenance.Models;

/// <summary>A persisted maintenance request (durable record behind the in-memory queue).</summary>
public class MaintenanceRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RoomId { get; set; }
    public string RoomNumber { get; set; } = default!;
    public string Description { get; set; } = default!;
    public MaintenancePriority Priority { get; set; }
    public MaintenanceStatus Status { get; set; } = MaintenanceStatus.Open;
    public Guid ReportedBy { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ResolvedAt { get; set; }
}
