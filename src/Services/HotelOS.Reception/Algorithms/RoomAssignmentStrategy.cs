using HotelOS.Shared.Algorithms.RoomAssignment;
using HotelOS.Shared.Enums;

namespace HotelOS.Reception.Algorithms;

/// <summary>
/// Default multi-criteria room matcher (Strategy pattern).
/// Hard filter: style match + Available + Clean.
/// Ranking:    preferred floor → proximity → longest clean duration → room number.
/// </summary>
public sealed class RoomAssignmentStrategy : IRoomAssignmentStrategy
{
    public RoomAssignmentResult Assign(RoomAssignmentRequest request, IReadOnlyList<RoomCandidate> candidates)
    {
        // STEP 1 — hard constraints.
        var eligible = candidates.Where(c =>
                c.Style == request.RequestedStyle &&
                c.Status == RoomStatus.Available &&
                c.CleanStatus == RoomCleanStatus.Clean)
            .ToList();

        if (eligible.Count == 0)
            return RoomAssignmentResult.Failed(
                $"No clean, available room of style '{request.RequestedStyle}' for the requested dates.");

        // STEP 2-3 — lexicographic ranking; best is first after sort.
        eligible.Sort(new RoomCandidateComparer(request));
        var best = eligible[0];

        // STEP 4-5 — derive the match tier and a human-readable explanation.
        int floorMiss = request.PreferredFloor is null || best.Floor == request.PreferredFloor ? 0 : 1;
        int proximityMiss = request.ProximityPreference is null
            || string.Equals(best.ProximityZone, request.ProximityPreference, StringComparison.OrdinalIgnoreCase) ? 0 : 1;
        int tier = floorMiss * 2 + proximityMiss; // 0 ideal … 3 fallback

        double cleanHours = (DateTime.UtcNow - best.LastCleanedAt).TotalHours;
        string reason =
            $"Matched style {request.RequestedStyle}; " +
            $"{(floorMiss == 0 ? "floor preference met" : "floor fallback")}; " +
            $"{(proximityMiss == 0 ? "proximity met" : "proximity fallback")}; " +
            $"room clean for {cleanHours:F1}h.";

        return RoomAssignmentResult.Assigned(best.RoomId, best.RoomNumber, tier, reason);
    }
}
