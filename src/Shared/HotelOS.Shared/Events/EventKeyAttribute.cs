namespace HotelOS.Shared.Events;

/// <summary>
/// Binds an <see cref="IntegrationEvent"/> to its RabbitMQ topic routing key
/// (e.g. "booking.created"). The event bus reads this to publish/subscribe,
/// so the routing key lives next to the contract and can never drift.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class EventKeyAttribute : Attribute
{
    public string Key { get; }
    public EventKeyAttribute(string key) => Key = key;
}
