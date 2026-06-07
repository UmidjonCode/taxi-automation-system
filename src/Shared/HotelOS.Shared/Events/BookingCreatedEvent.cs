namespace HotelOS.Shared.Events;

/// <summary>Published by Reception when a guest creates a booking (room held, awaiting advance payment).</summary>
[EventKey("booking.created")]
public sealed record BookingCreatedEvent : IntegrationEvent
{
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required string RoomNumber { get; init; }
    public required DateTime CheckIn { get; init; }
    public required DateTime CheckOut { get; init; }
    public required Guid BranchId { get; init; }
}
