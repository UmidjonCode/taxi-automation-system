using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;

namespace HotelOS.Shared.Messaging;

/// <summary>
/// Owns the single long-lived RabbitMQ connection and retries the initial
/// connect with exponential backoff (the broker often starts after the service).
/// </summary>
public sealed class RabbitMqConnection : IDisposable
{
    private readonly ConnectionFactory _factory;
    private readonly ILogger<RabbitMqConnection> _logger;
    private readonly int _retryAttempts;
    private readonly object _lock = new();
    private IConnection? _connection;

    public RabbitMqConnection(IOptions<EventBusOptions> options, ILogger<RabbitMqConnection> logger)
    {
        var o = options.Value;
        _logger = logger;
        _retryAttempts = o.RetryConnectAttempts;
        _factory = new ConnectionFactory
        {
            HostName = o.HostName,
            Port = o.Port,
            UserName = o.UserName,
            Password = o.Password,
            DispatchConsumersAsync = true,        // required for AsyncEventingBasicConsumer
            AutomaticRecoveryEnabled = true
        };
    }

    public bool IsConnected => _connection is { IsOpen: true };

    public IConnection Connection => _connection
        ?? throw new InvalidOperationException("RabbitMQ connection not established.");

    public bool TryConnect()
    {
        lock (_lock)
        {
            if (IsConnected) return true;

            for (var attempt = 1; attempt <= _retryAttempts; attempt++)
            {
                try
                {
                    _connection = _factory.CreateConnection();
                    if (IsConnected)
                    {
                        _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}.", _factory.HostName, _factory.Port);
                        return true;
                    }
                }
                catch (Exception ex) when (ex is BrokerUnreachableException or SocketException)
                {
                    var delaySeconds = Math.Min(30, Math.Pow(2, attempt));
                    _logger.LogWarning("RabbitMQ connect attempt {Attempt}/{Max} failed: {Message}. Retrying in {Delay}s.",
                        attempt, _retryAttempts, ex.Message, delaySeconds);
                    Thread.Sleep(TimeSpan.FromSeconds(delaySeconds));
                }
            }

            _logger.LogError("Could not connect to RabbitMQ after {Max} attempts.", _retryAttempts);
            return false;
        }
    }

    public IModel CreateChannel()
    {
        if (!IsConnected && !TryConnect())
            throw new InvalidOperationException("No RabbitMQ connection available.");
        return Connection.CreateModel();
    }

    public void Dispose()
    {
        try { _connection?.Dispose(); }
        catch (Exception ex) { _logger.LogWarning("Error disposing RabbitMQ connection: {Message}", ex.Message); }
    }
}
