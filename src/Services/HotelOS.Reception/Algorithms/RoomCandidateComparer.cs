using HotelOS.Shared.Algorithms.RoomAssignment;

namespace HotelOS.Reception.Algorithms;

/// <summary>
/// Lexicographic ordering for room candidates (Phase 2 design):
///   1. preferred-floor match   (matches sort first)
///   2. proximity match         (matches sort first)
///   3. longest clean duration  (oldest LastCleanedAt first)
///   4. room number             (deterministic final tie-break)
/// Because floor/proximity are soft keys, "fallback" is automatic: if nothing
/// matches the preference, all candidates tie on that key and the comparison
/// flows through to the next key.
/// </summary>
public sealed class RoomCandidateComparer : IComparer<RoomCandidate>
{
    private readonly RoomAssignmentRequest _request;

    public RoomCandidateComparer(RoomAssignmentRequest request) => _request = request;

    public int Compare(RoomCandidate? x, RoomCandidate? y)
    {
        ArgumentNullException.ThrowIfNull(x);
        ArgumentNullException.ThrowIfNull(y);

        int byFloor = FloorRank(x).CompareTo(FloorRank(y));
        if (byFloor != 0) return byFloor;

        int byProximity = ProximityRank(x).CompareTo(ProximityRank(y));
        if (byProximity != 0) return byProximity;

        // Longest clean duration = the room cleaned longest ago = OLDEST LastCleanedAt
        // first. Oldest-first is ASCENDING by date, so compare x to y directly.
        int byCleanDuration = x.LastCleanedAt.CompareTo(y.LastCleanedAt);
        if (byCleanDuration != 0) return byCleanDuration;

        return string.CompareOrdinal(x.RoomNumber, y.RoomNumber);
    }

    private int FloorRank(RoomCandidate r) =>
        _request.PreferredFloor is null ? 0 : (r.Floor == _request.PreferredFloor ? 0 : 1);

    private int ProximityRank(RoomCandidate r) =>
        _request.ProximityPreference is null ? 0
        : (string.Equals(r.ProximityZone, _request.ProximityPreference, StringComparison.OrdinalIgnoreCase) ? 0 : 1);
}
