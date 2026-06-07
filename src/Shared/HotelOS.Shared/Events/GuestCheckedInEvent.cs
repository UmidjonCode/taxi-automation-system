namespace HotelOS.Shared.Events;

/// <summary>Published by Reception when a receptionist checks a guest in.</summary>
[EventKey("guest.checkedin")]
public sealed record GuestCheckedInEvent : IntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required int Floor { get; init; }
    public required DateTime CheckIn { get; init; }
}
