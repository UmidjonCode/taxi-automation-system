namespace HotelOS.Shared.Messaging;

/// <summary>Connection + topology settings for the RabbitMQ event bus (bound from appsettings).</summary>
public sealed class EventBusOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";

    /// <summary>Shared topic exchange all services publish to / subscribe from.</summary>
    public string ExchangeName { get; set; } = "hotel.events";

    /// <summary>The durable queue THIS service consumes from. One queue per service.</summary>
    public string QueueName { get; set; } = "hotelos.queue";

    /// <summary>How many times to retry the initial broker connection (exponential backoff).</summary>
    public int RetryConnectAttempts { get; set; } = 8;
}
