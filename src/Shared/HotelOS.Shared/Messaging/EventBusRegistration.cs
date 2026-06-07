using Microsoft.Extensions.DependencyInjection;

namespace HotelOS.Shared.Messaging;

/// <summary>One-line DI registration of the event bus for any service.</summary>
public static class EventBusRegistration
{
    public static IServiceCollection AddHotelOsEventBus(
        this IServiceCollection services, Action<EventBusOptions> configure)
    {
        services.Configure(configure);
        services.AddSingleton<RabbitMqConnection>();
        services.AddSingleton<IEventBus, RabbitMqEventBus>();
        return services;
    }
}
