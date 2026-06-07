using HotelOS.Shared.Enums;

namespace HotelOS.Shared.Algorithms.Maintenance;

/// <summary>
/// Composite sort key for the priority queue. Ordered by Priority, then
/// SubmittedAt (FIFO within a priority), then a monotonic Sequence so two
/// requests with identical timestamps still have a stable, deterministic order.
/// </summary>
public readonly record struct MaintenanceKey(
    MaintenancePriority Priority,
    DateTime SubmittedAt,
    long Sequence);
