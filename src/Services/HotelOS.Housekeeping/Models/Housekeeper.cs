namespace HotelOS.Housekeeping.Models;

/// <summary>A housekeeping staff member.</summary>
public class Housekeeper
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string FullName { get; set; } = default!;
    public bool IsAvailable { get; set; } = true;
}
