namespace HotelOS.Shared.Enums;

/// <summary>
/// Maintenance severity. The numeric value IS the priority order:
/// the priority queue serves the LOWEST value first (Critical before Low).
/// </summary>
public enum MaintenancePriority
{
    Critical = 0,
    High = 1,
    Normal = 2,
    Low = 3
}
