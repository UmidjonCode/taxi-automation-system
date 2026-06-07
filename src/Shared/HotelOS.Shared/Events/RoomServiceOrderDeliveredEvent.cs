namespace HotelOS.Shared.Events;

/// <summary>Published by Room Service on delivery. Reception adds the cost to the guest's final bill.</summary>
[EventKey("roomservice.order.delivered")]
public sealed record RoomServiceOrderDeliveredEvent : IntegrationEvent
{
    public required Guid OrderId { get; init; }
    public required Guid BookingId { get; init; }
    public required Guid RoomId { get; init; }
    public required decimal TotalCost { get; init; }
    public required DateTime DeliveredAt { get; init; }
}
