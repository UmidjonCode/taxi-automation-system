using HotelOS.Shared.Algorithms.Maintenance;

namespace HotelOS.Maintenance.Algorithms;

/// <summary>
/// Defines "served first" for the priority queue:
///   1. Priority    (Critical=0 … Low=3, ascending → Critical first)
///   2. SubmittedAt (ascending → FIFO within a priority)
///   3. Sequence    (ascending → stable tie-break on identical timestamps)
/// </summary>
public sealed class MaintenanceKeyComparer : IComparer<MaintenanceKey>
{
    public int Compare(MaintenanceKey a, MaintenanceKey b)
    {
        int byPriority = ((int)a.Priority).CompareTo((int)b.Priority);
        if (byPriority != 0) return byPriority;

        int byTime = a.SubmittedAt.CompareTo(b.SubmittedAt);
        if (byTime != 0) return byTime;

        return a.Sequence.CompareTo(b.Sequence);
    }
}
