using HotelOS.RoomService.Services;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;

namespace HotelOS.RoomService.Events;

/// <summary>When a guest checks out, void any room-service orders that were never delivered.</summary>
public sealed class GuestCheckedOutHandler : IIntegrationEventHandler<GuestCheckedOutEvent>
{
    private readonly RoomServiceFacade _facade;

    public GuestCheckedOutHandler(RoomServiceFacade facade) => _facade = facade;

    public Task HandleAsync(GuestCheckedOutEvent e, CancellationToken ct = default) =>
        _facade.CancelOpenOrdersForBookingAsync(e.BookingId, ct);
}
