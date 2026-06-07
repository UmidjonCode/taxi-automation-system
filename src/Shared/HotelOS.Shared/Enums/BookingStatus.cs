namespace HotelOS.Shared.Enums;

/// <summary>Lifecycle of a booking from creation through checkout/cancellation.</summary>
public enum BookingStatus
{
    Pending = 0,
    Confirmed = 1,
    CheckedIn = 2,
    CheckedOut = 3,
    Cancelled = 4
}
