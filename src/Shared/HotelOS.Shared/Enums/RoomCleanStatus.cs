namespace HotelOS.Shared.Enums;

/// <summary>Housekeeping cleanliness state. Only <see cref="Clean"/> rooms are assignable.</summary>
public enum RoomCleanStatus
{
    Clean = 0,
    Dirty = 1,
    InProgress = 2
}
