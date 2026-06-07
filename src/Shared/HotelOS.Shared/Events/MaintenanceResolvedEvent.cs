namespace HotelOS.Shared.Events;

/// <summary>Published by Maintenance when a request is resolved. Housekeeping may then re-clean / re-open the room.</summary>
[EventKey("maintenance.resolved")]
public sealed record MaintenanceResolvedEvent : IntegrationEvent
{
    public required Guid RequestId { get; init; }
    public required Guid RoomId { get; init; }
    public required DateTime ResolvedAt { get; init; }
}
