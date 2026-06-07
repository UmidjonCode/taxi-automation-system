using HotelOS.Maintenance.Services;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;

namespace HotelOS.Maintenance.Events;

/// <summary>Housekeeping reported a fault → persist it and enqueue by priority.</summary>
public sealed class RoomMaintenanceRequestedHandler : IIntegrationEventHandler<RoomMaintenanceRequestedEvent>
{
    private readonly MaintenanceFacade _facade;

    public RoomMaintenanceRequestedHandler(MaintenanceFacade facade) => _facade = facade;

    public Task HandleAsync(RoomMaintenanceRequestedEvent e, CancellationToken ct = default) =>
        _facade.IntakeAsync(e, ct);
}
