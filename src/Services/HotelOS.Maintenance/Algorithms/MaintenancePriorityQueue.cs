using HotelOS.Shared.Algorithms.Maintenance;

namespace HotelOS.Maintenance.Algorithms;

/// <summary>
/// Thread-safe priority queue backed by a binary heap
/// (<see cref="PriorityQueue{TElement,TPriority}"/>). Registered as a singleton
/// so the whole service shares one ordered backlog of maintenance work.
/// Enqueue/Dequeue are O(log n); Peek is O(1).
/// </summary>
public sealed class MaintenancePriorityQueue : IMaintenancePriorityQueue
{
    private readonly PriorityQueue<MaintenanceRequestItem, MaintenanceKey> _heap = new(new MaintenanceKeyComparer());
    private readonly object _lock = new();
    private long _sequence;

    public int Count
    {
        get { lock (_lock) { return _heap.Count; } }
    }

    public void Enqueue(MaintenanceRequestItem item)
    {
        lock (_lock)
        {
            var key = new MaintenanceKey(item.Priority, item.SubmittedAt, _sequence++);
            _heap.Enqueue(item, key);
        }
    }

    public MaintenanceRequestItem Dequeue()
    {
        lock (_lock)
        {
            if (_heap.Count == 0) throw new InvalidOperationException("The maintenance queue is empty.");
            return _heap.Dequeue();
        }
    }

    public bool TryDequeue(out MaintenanceRequestItem item)
    {
        lock (_lock)
        {
            if (_heap.TryDequeue(out var element, out _))
            {
                item = element;
                return true;
            }
            item = default!;
            return false;
        }
    }

    public MaintenanceRequestItem Peek()
    {
        lock (_lock)
        {
            if (_heap.Count == 0) throw new InvalidOperationException("The maintenance queue is empty.");
            return _heap.Peek();
        }
    }

    public IReadOnlyList<MaintenanceRequestItem> Snapshot()
    {
        lock (_lock)
        {
            var comparer = new MaintenanceKeyComparer();
            return _heap.UnorderedItems
                .OrderBy(entry => entry.Priority, comparer)
                .Select(entry => entry.Element)
                .ToList();
        }
    }
}
