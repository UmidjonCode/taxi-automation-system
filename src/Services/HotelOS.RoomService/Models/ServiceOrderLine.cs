namespace HotelOS.RoomService.Models;

/// <summary>A single line within a room-service order.</summary>
public class ServiceOrderLine
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ServiceOrderId { get; set; }
    public Guid MenuItemId { get; set; }
    public string Name { get; set; } = default!;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
}
