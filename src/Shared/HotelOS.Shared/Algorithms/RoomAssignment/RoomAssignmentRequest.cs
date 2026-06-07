using HotelOS.Shared.Enums;

namespace HotelOS.Shared.Algorithms.RoomAssignment;

/// <summary>Immutable description of what a guest wants from the room-assignment algorithm.</summary>
public sealed record RoomAssignmentRequest
{
    public required RoomStyle RequestedStyle { get; init; }
    public int? PreferredFloor { get; init; }
    public string? ProximityPreference { get; init; }
    public required DateTime CheckIn { get; init; }
    public required DateTime CheckOut { get; init; }
    public required Guid BranchId { get; init; }
}
