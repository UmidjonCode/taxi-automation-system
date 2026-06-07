using HotelOS.Shared.Events;

namespace HotelOS.Shared.Messaging;

/// <summary>
/// A subscriber in the Observer pattern. Each service implements one of these
/// per event it cares about; the event bus invokes <see cref="HandleAsync"/>
/// inside a fresh DI scope when a matching message arrives.
/// </summary>
public interface IIntegrationEventHandler<in TEvent> where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event, CancellationToken ct = default);
}
