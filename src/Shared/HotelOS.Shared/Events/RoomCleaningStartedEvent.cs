namespace HotelOS.Shared.Events;

/// <summary>Published by Housekeeping when a housekeeper begins cleaning a room.</summary>
[EventKey("room.cleaning.started")]
public sealed record RoomCleaningStartedEvent : IntegrationEvent
{
    public required Guid TaskId { get; init; }
    public required Guid RoomId { get; init; }
    public required Guid HousekeeperId { get; init; }
    public required DateTime StartedAt { get; init; }
}
