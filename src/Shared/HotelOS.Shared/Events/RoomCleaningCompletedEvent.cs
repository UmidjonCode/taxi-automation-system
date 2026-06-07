namespace HotelOS.Shared.Events;

/// <summary>Published by Housekeeping when cleaning finishes. Reception flips the room back to Clean/Available.</summary>
[EventKey("room.cleaning.completed")]
public sealed record RoomCleaningCompletedEvent : IntegrationEvent
{
    public required Guid TaskId { get; init; }
    public required Guid RoomId { get; init; }
    public required DateTime CompletedAt { get; init; }
}
