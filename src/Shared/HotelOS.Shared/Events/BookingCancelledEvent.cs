namespace HotelOS.Shared.Events;

/// <summary>Published by Reception when a booking is cancelled. Carries the computed refund (24h policy).</summary>
[EventKey("booking.cancelled")]
public sealed record BookingCancelledEvent : IntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required DateTime CancelledAt { get; init; }
    public required decimal RefundAmount { get; init; }
}
