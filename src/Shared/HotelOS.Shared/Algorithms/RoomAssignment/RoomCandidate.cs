using HotelOS.Shared.Enums;

namespace HotelOS.Shared.Algorithms.RoomAssignment;

/// <summary>A room the Reception service pre-loaded as a possible match for a request.</summary>
public sealed record RoomCandidate
{
    public required Guid RoomId { get; init; }
    public required string RoomNumber { get; init; }
    public required int Floor { get; init; }
    public required RoomStyle Style { get; init; }
    public required RoomStatus Status { get; init; }
    public required RoomCleanStatus CleanStatus { get; init; }

    /// <summary>When the room was last cleaned; drives the "longest clean duration" ranking.</summary>
    public required DateTime LastCleanedAt { get; init; }

    /// <summary>Optional zone tag (e.g. "Elevator", "Quiet") matched against the guest preference.</summary>
    public string? ProximityZone { get; init; }
}
