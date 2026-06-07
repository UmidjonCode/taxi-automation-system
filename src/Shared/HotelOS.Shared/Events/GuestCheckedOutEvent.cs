namespace HotelOS.Shared.Events;

/// <summary>Published by Reception at checkout. Triggers housekeeping cleaning and closes room-service tabs.</summary>
[EventKey("guest.checkedout")]
public sealed record GuestCheckedOutEvent : IntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required int Floor { get; init; }
    public required DateTime CheckOut { get; init; }
    public required decimal FinalBill { get; init; }
}
