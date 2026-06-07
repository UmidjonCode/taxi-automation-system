namespace HotelOS.Shared.Events;

/// <summary>
/// Base type for every message published on the broker. Carries correlation
/// metadata shared by all events. Concrete events are immutable records.
/// </summary>
public abstract record IntegrationEvent
{
    /// <summary>Unique id of this specific message (for idempotency / tracing).</summary>
    public Guid EventId { get; init; } = Guid.NewGuid();

    /// <summary>UTC instant the event was created by its publisher.</summary>
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
}
