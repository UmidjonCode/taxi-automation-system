namespace HotelOS.Reception.Models;

/// <summary>
/// Login identity — separate from Guest (which is a hotel data record).
/// One Account can optionally link to one Guest (for guest accounts).
/// Receptionist accounts have no linked Guest.
/// </summary>
public class Account
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public AccountRole Role { get; set; } = AccountRole.Guest;

    /// <summary>FK to Guest record. Null for staff/receptionist accounts.</summary>
    public Guid? GuestId { get; set; }
    public Guest? Guest { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum AccountRole
{
    Guest = 0,
    Receptionist = 1
}
