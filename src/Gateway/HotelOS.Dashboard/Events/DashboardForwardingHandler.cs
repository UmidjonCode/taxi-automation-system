using System.Reflection;
using HotelOS.Dashboard.Hubs;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.AspNetCore.SignalR;

namespace HotelOS.Dashboard.Events;

/// <summary>
/// A single generic Observer that forwards ANY integration event to all
/// connected dashboard clients over WebSockets. Registered as an open generic,
/// it is reused for all 15 event types — no per-event boilerplate.
/// </summary>
public sealed class DashboardForwardingHandler<TEvent> : IIntegrationEventHandler<TEvent>
    where TEvent : IntegrationEvent
{
    private readonly IHubContext<DashboardHub> _hub;
    private readonly ILogger<DashboardForwardingHandler<TEvent>> _logger;

    public DashboardForwardingHandler(IHubContext<DashboardHub> hub, ILogger<DashboardForwardingHandler<TEvent>> logger)
    {
        _hub = hub;
        _logger = logger;
    }

    public async Task HandleAsync(TEvent @event, CancellationToken ct = default)
    {
        var topic = typeof(TEvent).GetCustomAttribute<EventKeyAttribute>()?.Key ?? typeof(TEvent).Name;

        await _hub.Clients.All.SendAsync("event", new
        {
            topic,
            name = typeof(TEvent).Name,
            occurredAt = @event.OccurredAt,
            payload = @event
        }, ct);

        _logger.LogInformation("Dashboard <- {Topic}", topic);
    }
}
