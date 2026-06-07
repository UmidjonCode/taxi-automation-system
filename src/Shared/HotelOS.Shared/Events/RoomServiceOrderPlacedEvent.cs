namespace HotelOS.Shared.Events;

/// <summary>Published by Room Service when a guest places an order. Shown live on the dashboard.</summary>
[EventKey("roomservice.order.placed")]
public sealed record RoomServiceOrderPlacedEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required string ItemsSummary { get; init; }
    public required decimal TotalCost { get; init; }
}
