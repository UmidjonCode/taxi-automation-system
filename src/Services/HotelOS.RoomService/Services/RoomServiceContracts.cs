namespace HotelOS.RoomService.Services;

public sealed record OrderLineRequest(Guid MenuItemId, int Quantity);

public sealed record PlaceOrderRequest(
    Guid BookingId,
    Guid GuestId,
    Guid RoomId,
    List<OrderLineRequest> Items);

public sealed record OrderResponse
{
    public required Guid OrderId { get; init; }
    public required string Status { get; init; }
    public required decimal TotalCost { get; init; }
    public required string ItemsSummary { get; init; }
}
