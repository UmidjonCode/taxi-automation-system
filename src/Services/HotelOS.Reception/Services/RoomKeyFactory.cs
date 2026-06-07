using HotelOS.Reception.Models;

namespace HotelOS.Reception.Services;

/// <summary>Factory pattern: builds room keys, including master keys that open every room.</summary>
public sealed class RoomKeyFactory
{
    public RoomKey CreateForRoom(Room room, Guid guestId) => new()
    {
        RoomId = room.Id,
        KeyCode = $"{room.KeyCode}-{ShortCode()}",
        IsMasterKey = false,
        IssuedTo = guestId,
        IssuedAt = DateTime.UtcNow
    };

    public RoomKey CreateMasterKey(Guid issuedTo) => new()
    {
        RoomId = null,
        KeyCode = $"MASTER-{ShortCode()}",
        IsMasterKey = true,
        IssuedTo = issuedTo,
        IssuedAt = DateTime.UtcNow
    };

    private static string ShortCode() => Guid.NewGuid().ToString("N")[..8].ToUpperInvariant();
}
