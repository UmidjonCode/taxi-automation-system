namespace HotelOS.Shared.Algorithms.Maintenance;

/// <summary>
/// Priority queue serving Critical → High → Normal → Low, and within a priority
/// the earliest submission first (FIFO). Backed by a binary heap.
/// </summary>
public interface IMaintenancePriorityQueue
{
    void Enqueue(MaintenanceRequestItem item);
    MaintenanceRequestItem Dequeue();
    bool TryDequeue(out MaintenanceRequestItem item);
    MaintenanceRequestItem Peek();
    int Count { get; }

    /// <summary>Ordered snapshot (highest priority first) without draining the queue.</summary>
    IReadOnlyList<MaintenanceRequestItem> Snapshot();
}
