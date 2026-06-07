namespace HotelOS.Shared.Events;

/// <summary>Published by Reception when a room key (or master key) is issued to a guest.</summary>
[EventKey("key.issued")]
public sealed record KeyIssuedEvent : IntegrationEvent
{
    public required Guid KeyId { get; init; }
    public required Guid RoomId { get; init; }
    public required Guid GuestId { get; init; }
    public required bool IsMasterKey { get; init; }
    public required DateTime IssuedAt { get; init; }
}
