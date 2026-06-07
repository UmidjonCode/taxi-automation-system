namespace HotelOS.Shared.Events;

/// <summary>Published by Reception when a key is returned at checkout.</summary>
[EventKey("key.returned")]
public sealed record KeyReturnedEvent : IntegrationEvent
{
    public required Guid KeyId { get; init; }
    public required Guid RoomId { get; init; }
    public required DateTime ReturnedAt { get; init; }
}
