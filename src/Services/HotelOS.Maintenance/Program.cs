using HotelOS.Maintenance.Algorithms;
using HotelOS.Maintenance.Data;
using HotelOS.Maintenance.Events;
using HotelOS.Maintenance.Services;
using HotelOS.Shared.Algorithms.Maintenance;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<MaintenanceDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("MaintenanceDb") ?? "Data Source=maintenance.db"));

// The priority queue is a singleton: one shared, ordered backlog for the service.
builder.Services.AddSingleton<IMaintenancePriorityQueue, MaintenancePriorityQueue>();
builder.Services.AddScoped<MaintenanceFacade>();
builder.Services.AddScoped<RoomMaintenanceRequestedHandler>();

builder.Services.AddHotelOsEventBus(o =>
{
    builder.Configuration.GetSection("EventBus").Bind(o);
    o.QueueName = "maintenance.queue";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MaintenanceDbContext>();
    db.Database.EnsureCreated();

    // Refill the in-memory queue from any open rows left by a previous run.
    var facade = scope.ServiceProvider.GetRequiredService<MaintenanceFacade>();
    await facade.RehydrateQueueAsync();
}

var bus = app.Services.GetRequiredService<IEventBus>();
bus.Subscribe<RoomMaintenanceRequestedEvent, RoomMaintenanceRequestedHandler>();
bus.StartConsuming();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "Maintenance", status = "running" }));

app.Run();
