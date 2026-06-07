namespace HotelOS.RoomService.Models;

/// <summary>An item on the room-service menu (amenity / food / drink).</summary>
public class MenuItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = default!;
    public string Category { get; set; } = default!;
    public decimal Price { get; set; }
    public bool IsAvailable { get; set; } = true;
}
