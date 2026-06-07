using HotelOS.Shared.Events;

namespace HotelOS.Shared.Messaging;

/// <summary>
/// Abstraction over the message broker. Services depend ONLY on this interface,
/// never on each other. Publish = fire-and-forget event; Subscribe = register a
/// handler; StartConsuming = begin draining this service's queue.
/// </summary>
public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
        where TEvent : IntegrationEvent;

    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    /// <summary>Declares the queue, binds all subscribed routing keys and starts the consumer.</summary>
    void StartConsuming();
}
