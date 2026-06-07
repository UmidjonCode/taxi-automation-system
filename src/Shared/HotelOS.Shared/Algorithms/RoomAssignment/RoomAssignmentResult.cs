namespace HotelOS.Shared.Algorithms.RoomAssignment;

/// <summary>Outcome of the room-assignment algorithm, with a human-readable explanation.</summary>
public sealed record RoomAssignmentResult
{
    public required bool Success { get; init; }
    public Guid? RoomId { get; init; }
    public string? RoomNumber { get; init; }

    /// <summary>0 = ideal (style+floor+proximity) … up to 3 = fallback (any clean room).</summary>
    public int MatchTier { get; init; }

    public required string Reason { get; init; }

    public static RoomAssignmentResult Failed(string reason) =>
        new() { Success = false, Reason = reason };

    public static RoomAssignmentResult Assigned(Guid roomId, string roomNumber, int tier, string reason) =>
        new() { Success = true, RoomId = roomId, RoomNumber = roomNumber, MatchTier = tier, Reason = reason };
}
