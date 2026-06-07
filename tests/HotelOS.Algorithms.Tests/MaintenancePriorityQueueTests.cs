using HotelOS.Maintenance.Algorithms;
using HotelOS.Shared.Algorithms.Maintenance;
using HotelOS.Shared.Enums;
using Xunit;

namespace HotelOS.Algorithms.Tests;

public class MaintenancePriorityQueueTests
{
    private static readonly DateTime Base = new(2026, 1, 1, 8, 0, 0, DateTimeKind.Utc);

    private static MaintenanceRequestItem Item(MaintenancePriority priority, int minutesAfterBase) => new()
    {
        RequestId = Guid.NewGuid(),
        RoomId = Guid.NewGuid(),
        RoomNumber = "100",
        Description = $"{priority} @ {minutesAfterBase}",
        Priority = priority,
        SubmittedAt = Base.AddMinutes(minutesAfterBase)
    };

    [Fact]
    public void Serves_critical_before_lower_priorities_regardless_of_insert_order()
    {
        var q = new MaintenancePriorityQueue();
        q.Enqueue(Item(MaintenancePriority.Low, 0));
        q.Enqueue(Item(MaintenancePriority.Normal, 0));
        q.Enqueue(Item(MaintenancePriority.Critical, 0));
        q.Enqueue(Item(MaintenancePriority.High, 0));

        Assert.Equal(MaintenancePriority.Critical, q.Dequeue().Priority);
        Assert.Equal(MaintenancePriority.High, q.Dequeue().Priority);
        Assert.Equal(MaintenancePriority.Normal, q.Dequeue().Priority);
        Assert.Equal(MaintenancePriority.Low, q.Dequeue().Priority);
    }

    [Fact]
    public void Within_same_priority_is_fifo_by_submission_time()
    {
        var q = new MaintenancePriorityQueue();
        var first = Item(MaintenancePriority.High, 1);
        var second = Item(MaintenancePriority.High, 5);
        var third = Item(MaintenancePriority.High, 9);

        // enqueue out of time order
        q.Enqueue(third);
        q.Enqueue(first);
        q.Enqueue(second);

        Assert.Equal(first.RequestId, q.Dequeue().RequestId);
        Assert.Equal(second.RequestId, q.Dequeue().RequestId);
        Assert.Equal(third.RequestId, q.Dequeue().RequestId);
    }

    [Fact]
    public void TryDequeue_on_empty_returns_false()
    {
        var q = new MaintenancePriorityQueue();
        Assert.False(q.TryDequeue(out _));
        Assert.Equal(0, q.Count);
    }

    [Fact]
    public void Snapshot_is_ordered_but_does_not_drain()
    {
        var q = new MaintenancePriorityQueue();
        q.Enqueue(Item(MaintenancePriority.Low, 0));
        q.Enqueue(Item(MaintenancePriority.Critical, 0));

        var snapshot = q.Snapshot();

        Assert.Equal(2, q.Count); // not drained
        Assert.Equal(MaintenancePriority.Critical, snapshot[0].Priority);
        Assert.Equal(MaintenancePriority.Low, snapshot[1].Priority);
    }

    [Fact]
    public void Peek_returns_highest_without_removing()
    {
        var q = new MaintenancePriorityQueue();
        q.Enqueue(Item(MaintenancePriority.Normal, 0));
        q.Enqueue(Item(MaintenancePriority.Critical, 0));

        Assert.Equal(MaintenancePriority.Critical, q.Peek().Priority);
        Assert.Equal(2, q.Count);
    }
}
