using HotelOS.Shared.Enums;

namespace HotelOS.Shared.Algorithms.Maintenance;

/// <summary>An item flowing through the maintenance priority queue.</summary>
public sealed record MaintenanceRequestItem
{
    public required Guid RequestId { get; init; }
    public required Guid RoomId { get; init; }
    public required string RoomNumber { get; init; }
    public required string Description { get; init; }
    public required MaintenancePriority Priority { get; init; }
    public required DateTime SubmittedAt { get; init; }
}
