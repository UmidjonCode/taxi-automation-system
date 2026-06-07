using HotelOS.Reception.Algorithms;
using HotelOS.Reception.Data;
using HotelOS.Reception.Events;
using HotelOS.Reception.Services;
using HotelOS.Shared.Algorithms.Billing;
using HotelOS.Shared.Algorithms.RoomAssignment;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
    });
});

builder.Services.AddDbContext<ReceptionDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("ReceptionDb") ?? "Data Source=reception.db"));

// Algorithms (Strategy pattern) + subsystem services.
builder.Services.AddScoped<IRoomAssignmentStrategy, RoomAssignmentStrategy>();
builder.Services.AddScoped<IBillingCalculator, BillingCalculator>();
builder.Services.AddSingleton<RoomKeyFactory>();
builder.Services.AddScoped<ReceptionFacade>();

// Event handlers (Observer subscribers).
builder.Services.AddScoped<RoomCleaningCompletedHandler>();
builder.Services.AddScoped<RoomServiceOrderDeliveredHandler>();

builder.Services.AddHotelOsEventBus(o =>
{
    builder.Configuration.GetSection("EventBus").Bind(o);
    o.QueueName = "reception.queue";
});

var app = builder.Build();

// Create + seed the per-service database (10 rooms / 2 floors).
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReceptionDbContext>();
    db.Database.EnsureCreated();
    ReceptionSeeder.Seed(db);
}

// Wire subscriptions then start draining this service's queue.
var bus = app.Services.GetRequiredService<IEventBus>();
bus.Subscribe<RoomCleaningCompletedEvent, RoomCleaningCompletedHandler>();
bus.Subscribe<RoomServiceOrderDeliveredEvent, RoomServiceOrderDeliveredHandler>();
bus.StartConsuming();

app.UseSwagger();
app.UseSwaggerUI();
app.UseCors("AllowFrontend");
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "Reception", status = "running" }));

app.Run();
