namespace HotelOS.Reception.Models;

/// <summary>An extra amenity/charge attached to a booking (e.g. spa, minibar, airport pickup).</summary>
public class BookingExtra
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public string Description { get; set; } = default!;
    public decimal Amount { get; set; }
}
