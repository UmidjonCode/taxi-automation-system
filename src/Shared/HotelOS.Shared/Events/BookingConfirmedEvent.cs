namespace HotelOS.Shared.Events;

/// <summary>Published by Reception once the advance payment is recorded and the booking is confirmed.</summary>
[EventKey("booking.confirmed")]
public sealed record BookingConfirmedEvent : IntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required decimal AdvancePayment { get; init; }
}
