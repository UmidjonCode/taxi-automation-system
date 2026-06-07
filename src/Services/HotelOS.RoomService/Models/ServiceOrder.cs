using HotelOS.Shared.Enums;

namespace HotelOS.RoomService.Models;

/// <summary>A room-service order placed against a booking.</summary>
public class ServiceOrder
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid GuestId { get; set; }
    public Guid RoomId { get; set; }
    public decimal TotalCost { get; set; }
    public ServiceOrderStatus Status { get; set; } = ServiceOrderStatus.Received;
    public DateTime OrderedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DeliveredAt { get; set; }

    public List<ServiceOrderLine> Lines { get; set; } = new();
}
