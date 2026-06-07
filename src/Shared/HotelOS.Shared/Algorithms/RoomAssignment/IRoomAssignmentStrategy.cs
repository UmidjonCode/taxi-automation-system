namespace HotelOS.Shared.Algorithms.RoomAssignment;

/// <summary>
/// Strategy pattern: pluggable multi-criteria room matcher. The default
/// implementation filters on style + clean + available, then ranks by
/// floor preference → proximity → longest clean duration.
/// </summary>
public interface IRoomAssignmentStrategy
{
    RoomAssignmentResult Assign(RoomAssignmentRequest request, IReadOnlyList<RoomCandidate> candidates);
}
