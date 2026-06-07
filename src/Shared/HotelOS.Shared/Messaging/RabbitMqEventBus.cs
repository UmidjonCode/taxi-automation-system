using System.Collections.Concurrent;
using System.Reflection;
using System.Text;
using System.Text.Json;
using HotelOS.Shared.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace HotelOS.Shared.Messaging;

/// <summary>
/// RabbitMQ topic-exchange implementation of <see cref="IEventBus"/> — the concrete
/// "subject" of the Observer pattern. A publisher fires an event; subscribers
/// (handlers) living in OTHER services react, with no direct service-to-service call.
///
/// Routing key for every message is taken from the event's [EventKey] attribute,
/// so the contract and its topic can never drift apart.
/// </summary>
public sealed class RabbitMqEventBus : IEventBus, IDisposable
{
    private readonly RabbitMqConnection _connection;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly EventBusOptions _options;
    private readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

    private readonly ConcurrentDictionary<string, Type> _eventTypes = new();      // routingKey -> event type
    private readonly ConcurrentDictionary<string, List<Type>> _handlers = new();  // routingKey -> handler types

    private readonly object _publishLock = new();
    private IModel? _publishChannel;
    private IModel? _consumerChannel;

    public RabbitMqEventBus(
        RabbitMqConnection connection,
        IServiceScopeFactory scopeFactory,
        IOptions<EventBusOptions> options,
        ILogger<RabbitMqEventBus> logger)
    {
        _connection = connection;
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    private static string RoutingKeyOf(Type eventType) =>
        eventType.GetCustomAttribute<EventKeyAttribute>()?.Key
        ?? throw new InvalidOperationException($"Event '{eventType.Name}' is missing the [EventKey] attribute.");

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default) where TEvent : IntegrationEvent
    {
        if (!_connection.IsConnected) _connection.TryConnect();

        var routingKey = RoutingKeyOf(typeof(TEvent));
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), _json);

        lock (_publishLock)
        {
            _publishChannel ??= CreateTopicChannel();
            var props = _publishChannel.CreateBasicProperties();
            props.DeliveryMode = 2;                 // persistent
            props.ContentType = "application/json";
            props.MessageId = @event.EventId.ToString();
            props.Type = routingKey;
            _publishChannel.BasicPublish(_options.ExchangeName, routingKey, mandatory: false, basicProperties: props, body: body);
        }

        _logger.LogInformation("Published {Event} -> {RoutingKey}", typeof(TEvent).Name, routingKey);
        return Task.CompletedTask;
    }

    public void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>
    {
        var key = RoutingKeyOf(typeof(TEvent));
        _eventTypes[key] = typeof(TEvent);
        var handlers = _handlers.GetOrAdd(key, _ => new List<Type>());
        lock (handlers)
        {
            if (!handlers.Contains(typeof(THandler))) handlers.Add(typeof(THandler));
        }
        _logger.LogInformation("Subscribed {Handler} to {RoutingKey}", typeof(THandler).Name, key);
    }

    public void StartConsuming()
    {
        _consumerChannel = CreateTopicChannel();
        _consumerChannel.QueueDeclare(_options.QueueName, durable: true, exclusive: false, autoDelete: false);

        foreach (var key in _handlers.Keys)
            _consumerChannel.QueueBind(_options.QueueName, _options.ExchangeName, key);

        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.Received += OnMessageReceivedAsync;
        _consumerChannel.BasicConsume(_options.QueueName, autoAck: false, consumer);

        _logger.LogInformation("Consuming '{Queue}' bound to {Count} routing key(s): {Keys}",
            _options.QueueName, _handlers.Count, string.Join(", ", _handlers.Keys));
    }

    private IModel CreateTopicChannel()
    {
        var channel = _connection.CreateChannel();
        channel.ExchangeDeclare(_options.ExchangeName, ExchangeType.Topic, durable: true, autoDelete: false);
        return channel;
    }

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        var routingKey = ea.RoutingKey;
        var json = Encoding.UTF8.GetString(ea.Body.Span);

        try
        {
            if (_eventTypes.TryGetValue(routingKey, out var eventType) &&
                _handlers.TryGetValue(routingKey, out var handlerTypes))
            {
                var @event = (IntegrationEvent)JsonSerializer.Deserialize(json, eventType, _json)!;
                var handlerInterface = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
                var handleMethod = handlerInterface.GetMethod("HandleAsync")!;

                await using var scope = _scopeFactory.CreateAsyncScope();
                foreach (var handlerType in handlerTypes.ToArray())
                {
                    var handler = scope.ServiceProvider.GetService(handlerType);
                    if (handler is null)
                    {
                        _logger.LogWarning("No DI registration for handler {Handler}.", handlerType.Name);
                        continue;
                    }
                    await (Task)handleMethod.Invoke(handler, new object?[] { @event, CancellationToken.None })!;
                }
            }

            _consumerChannel!.BasicAck(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed handling message on {RoutingKey}; nacking without requeue.", routingKey);
            _consumerChannel!.BasicNack(ea.DeliveryTag, multiple: false, requeue: false);
        }
    }

    public void Dispose()
    {
        try { _consumerChannel?.Dispose(); } catch { /* ignore */ }
        try { _publishChannel?.Dispose(); } catch { /* ignore */ }
    }
}
