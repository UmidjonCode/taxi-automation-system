namespace HotelOS.Reception.Models;

/// <summary>
/// A key issued to a guest. A normal key opens one room; a master key
/// (RoomId == null, IsMasterKey == true) opens every room in the branch.
/// </summary>
public class RoomKey
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? RoomId { get; set; }
    public string KeyCode { get; set; } = default!;
    public bool IsMasterKey { get; set; }
    public Guid? IssuedTo { get; set; }
    public DateTime IssuedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReturnedAt { get; set; }
}
