using HotelOS.Housekeeping.Services;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;

namespace HotelOS.Housekeeping.Events;

/// <summary>Reception (on checkout) asked for a room to be cleaned → create a cleaning task.</summary>
public sealed class RoomCleaningRequestedHandler : IIntegrationEventHandler<RoomCleaningRequestedEvent>
{
    private readonly HousekeepingFacade _facade;

    public RoomCleaningRequestedHandler(HousekeepingFacade facade) => _facade = facade;

    public Task HandleAsync(RoomCleaningRequestedEvent e, CancellationToken ct = default) =>
        _facade.CreateTaskAsync(e.RoomId, e.RoomNumber, e.Floor, ct);
}
