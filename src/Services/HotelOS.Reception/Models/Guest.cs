namespace HotelOS.Reception.Models;

/// <summary>A hotel guest. Owned solely by the Reception service.</summary>
public class Guest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string PhoneNumber { get; set; } = default!;
    public string? NationalId { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Booking> Bookings { get; set; } = new();
}
