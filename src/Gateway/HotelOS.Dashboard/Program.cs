using HotelOS.Dashboard.Events;
using HotelOS.Dashboard.Hubs;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSignalR();

// One open-generic forwarder reused for every event type.
builder.Services.AddScoped(typeof(DashboardForwardingHandler<>));

builder.Services.AddHotelOsEventBus(o =>
{
    builder.Configuration.GetSection("EventBus").Bind(o);
    o.QueueName = "dashboard.queue";
});

var app = builder.Build();

// Subscribe the dashboard to the WHOLE event catalogue, then start consuming.
var bus = app.Services.GetRequiredService<IEventBus>();
bus.Subscribe<BookingCreatedEvent, DashboardForwardingHandler<BookingCreatedEvent>>();
bus.Subscribe<BookingConfirmedEvent, DashboardForwardingHandler<BookingConfirmedEvent>>();
bus.Subscribe<BookingCancelledEvent, DashboardForwardingHandler<BookingCancelledEvent>>();
bus.Subscribe<GuestCheckedInEvent, DashboardForwardingHandler<GuestCheckedInEvent>>();
bus.Subscribe<GuestCheckedOutEvent, DashboardForwardingHandler<GuestCheckedOutEvent>>();
bus.Subscribe<RoomCleaningRequestedEvent, DashboardForwardingHandler<RoomCleaningRequestedEvent>>();
bus.Subscribe<RoomCleaningStartedEvent, DashboardForwardingHandler<RoomCleaningStartedEvent>>();
bus.Subscribe<RoomCleaningCompletedEvent, DashboardForwardingHandler<RoomCleaningCompletedEvent>>();
bus.Subscribe<RoomMaintenanceRequestedEvent, DashboardForwardingHandler<RoomMaintenanceRequestedEvent>>();
bus.Subscribe<MaintenanceStatusUpdatedEvent, DashboardForwardingHandler<MaintenanceStatusUpdatedEvent>>();
bus.Subscribe<MaintenanceResolvedEvent, DashboardForwardingHandler<MaintenanceResolvedEvent>>();
bus.Subscribe<RoomServiceOrderPlacedEvent, DashboardForwardingHandler<RoomServiceOrderPlacedEvent>>();
bus.Subscribe<RoomServiceOrderDeliveredEvent, DashboardForwardingHandler<RoomServiceOrderDeliveredEvent>>();
bus.Subscribe<KeyIssuedEvent, DashboardForwardingHandler<KeyIssuedEvent>>();
bus.Subscribe<KeyReturnedEvent, DashboardForwardingHandler<KeyReturnedEvent>>();
bus.StartConsuming();

app.UseDefaultFiles();
app.UseStaticFiles();
app.MapHub<DashboardHub>("/hub/dashboard");

app.Run();
