using HotelOS.RoomService.Data;
using HotelOS.RoomService.Models;
using HotelOS.Shared.Enums;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomService.Services;

/// <summary>Facade over the room-service subsystem: menu, ordering, delivery.</summary>
public sealed class RoomServiceFacade
{
    private readonly RoomServiceDbContext _db;
    private readonly IEventBus _bus;
    private readonly ILogger<RoomServiceFacade> _logger;

    public RoomServiceFacade(RoomServiceDbContext db, IEventBus bus, ILogger<RoomServiceFacade> logger)
    {
        _db = db;
        _bus = bus;
        _logger = logger;
    }

    public Task<List<MenuItem>> GetMenuAsync(CancellationToken ct = default) =>
        _db.MenuItems.AsNoTracking().Where(m => m.IsAvailable).ToListAsync(ct);

    public Task<List<ServiceOrder>> GetOrdersAsync(CancellationToken ct = default) =>
        _db.ServiceOrders.AsNoTracking().Include(o => o.Lines).OrderByDescending(o => o.OrderedAt).ToListAsync(ct);

    public async Task<OrderResponse> PlaceOrderAsync(PlaceOrderRequest req, CancellationToken ct = default)
    {
        if (req.Items is null || req.Items.Count == 0)
            throw new InvalidOperationException("An order must contain at least one item.");

        var ids = req.Items.Select(i => i.MenuItemId).ToList();
        var menu = await _db.MenuItems.Where(m => ids.Contains(m.Id) && m.IsAvailable).ToListAsync(ct);

        var order = new ServiceOrder { BookingId = req.BookingId, GuestId = req.GuestId, RoomId = req.RoomId };
        decimal total = 0m;

        foreach (var line in req.Items)
        {
            var item = menu.FirstOrDefault(m => m.Id == line.MenuItemId)
                ?? throw new InvalidOperationException($"Menu item {line.MenuItemId} is unavailable.");
            if (line.Quantity <= 0)
                throw new InvalidOperationException("Quantity must be positive.");

            order.Lines.Add(new ServiceOrderLine
            {
                MenuItemId = item.Id,
                Name = item.Name,
                UnitPrice = item.Price,
                Quantity = line.Quantity
            });
            total += item.Price * line.Quantity;
        }

        order.TotalCost = Math.Round(total, 2, MidpointRounding.AwayFromZero);
        _db.ServiceOrders.Add(order);
        await _db.SaveChangesAsync(ct);

        var summary = string.Join(", ", order.Lines.Select(l => $"{l.Quantity}x {l.Name}"));

        await _bus.PublishAsync(new RoomServiceOrderPlacedEvent
        {
            OrderId = order.Id,
            BookingId = order.BookingId,
            GuestId = order.GuestId,
            RoomId = order.RoomId,
            ItemsSummary = summary,
            TotalCost = order.TotalCost
        }, ct);

        _logger.LogInformation("Order {Order} placed ({Total:0.00}).", order.Id, order.TotalCost);
        return new OrderResponse { OrderId = order.Id, Status = order.Status.ToString(), TotalCost = order.TotalCost, ItemsSummary = summary };
    }

    public async Task<OrderResponse> MarkDeliveredAsync(Guid orderId, CancellationToken ct = default)
    {
        var order = await _db.ServiceOrders.Include(o => o.Lines).FirstOrDefaultAsync(o => o.Id == orderId, ct)
            ?? throw new KeyNotFoundException($"Order {orderId} not found.");
        if (order.Status == ServiceOrderStatus.Delivered)
            throw new InvalidOperationException("Order already delivered.");
        if (order.Status == ServiceOrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot deliver a cancelled order.");

        order.Status = ServiceOrderStatus.Delivered;
        order.DeliveredAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        await _bus.PublishAsync(new RoomServiceOrderDeliveredEvent
        {
            OrderId = order.Id,
            BookingId = order.BookingId,
            RoomId = order.RoomId,
            TotalCost = order.TotalCost,
            DeliveredAt = order.DeliveredAt.Value
        }, ct);

        var summary = string.Join(", ", order.Lines.Select(l => $"{l.Quantity}x {l.Name}"));
        _logger.LogInformation("Order {Order} delivered.", order.Id);
        return new OrderResponse { OrderId = order.Id, Status = order.Status.ToString(), TotalCost = order.TotalCost, ItemsSummary = summary };
    }

    /// <summary>On guest checkout, void any orders that never got delivered.</summary>
    public async Task CancelOpenOrdersForBookingAsync(Guid bookingId, CancellationToken ct = default)
    {
        var open = await _db.ServiceOrders
            .Where(o => o.BookingId == bookingId && o.Status != ServiceOrderStatus.Delivered && o.Status != ServiceOrderStatus.Cancelled)
            .ToListAsync(ct);
        if (open.Count == 0) return;

        foreach (var o in open) o.Status = ServiceOrderStatus.Cancelled;
        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("Cancelled {Count} open order(s) for booking {Booking}.", open.Count, bookingId);
    }
}
