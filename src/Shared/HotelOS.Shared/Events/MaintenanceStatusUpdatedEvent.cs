using HotelOS.Shared.Enums;

namespace HotelOS.Shared.Events;

/// <summary>Published by Maintenance whenever a request changes status. Feeds the live dashboard.</summary>
[EventKey("maintenance.status.updated")]
public sealed record MaintenanceStatusUpdatedEvent : IntegrationEvent
{
    public required Guid RequestId { get; init; }
    public required Guid RoomId { get; init; }
    public required MaintenanceStatus Status { get; init; }
    public required DateTime UpdatedAt { get; init; }
}
