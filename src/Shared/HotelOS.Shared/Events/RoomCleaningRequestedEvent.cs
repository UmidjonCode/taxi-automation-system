namespace HotelOS.Shared.Events;

/// <summary>Published by Reception (on checkout) asking Housekeeping to clean a room.</summary>
[EventKey("room.cleaning.requested")]
public sealed record RoomCleaningRequestedEvent : IntegrationEvent
{
    public required Guid RoomId { get; init; }
    public required string RoomNumber { get; init; }
    public required int Floor { get; init; }
}
