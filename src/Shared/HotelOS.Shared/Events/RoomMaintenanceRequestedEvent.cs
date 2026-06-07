using HotelOS.Shared.Enums;

namespace HotelOS.Shared.Events;

/// <summary>Published by Housekeeping when a room needs maintenance. Maintenance enqueues it by priority.</summary>
[EventKey("room.maintenance.requested")]
public sealed record RoomMaintenanceRequestedEvent : IntegrationEvent
{
    public required Guid RoomId { get; init; }
    public required string RoomNumber { get; init; }
    public required string Description { get; init; }
    public required MaintenancePriority Priority { get; init; }
    public required Guid ReportedBy { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
