namespace HotelOS.Shared.Enums;

/// <summary>Lifecycle of a room-service order.</summary>
public enum ServiceOrderStatus
{
    Received = 0,
    Preparing = 1,
    Delivered = 2,
    Cancelled = 3
}
