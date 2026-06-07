using HotelOS.Shared.Enums;

namespace HotelOS.Housekeeping.Services;

public sealed record StartCleaningRequest(Guid? HousekeeperId);

public sealed record ReportMaintenanceRequest(
    Guid RoomId,
    string RoomNumber,
    string Description,
    MaintenancePriority Priority,
    Guid HousekeeperId);
