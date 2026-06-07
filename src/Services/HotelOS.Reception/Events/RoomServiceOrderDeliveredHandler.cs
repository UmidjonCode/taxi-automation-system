using HotelOS.Reception.Data;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Reception.Events;

/// <summary>Accumulate delivered room-service charges onto the booking for the final bill.</summary>
public sealed class RoomServiceOrderDeliveredHandler : IIntegrationEventHandler<RoomServiceOrderDeliveredEvent>
{
    private readonly ReceptionDbContext _db;
    private readonly ILogger<RoomServiceOrderDeliveredHandler> _logger;

    public RoomServiceOrderDeliveredHandler(ReceptionDbContext db, ILogger<RoomServiceOrderDeliveredHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task HandleAsync(RoomServiceOrderDeliveredEvent e, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FirstOrDefaultAsync(b => b.Id == e.BookingId, ct);
        if (booking is null) return;

        booking.RoomServiceCharges += e.TotalCost;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Booking {Booking}: +{Amount:0.00} room-service (order {Order}).",
            e.BookingId, e.TotalCost, e.OrderId);
    }
}
