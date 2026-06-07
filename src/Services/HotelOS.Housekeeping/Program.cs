using HotelOS.Housekeeping.Data;
using HotelOS.Housekeeping.Events;
using HotelOS.Housekeeping.Services;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<HousekeepingDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("HousekeepingDb") ?? "Data Source=housekeeping.db"));

builder.Services.AddScoped<HousekeepingFacade>();
builder.Services.AddScoped<RoomCleaningRequestedHandler>();

builder.Services.AddHotelOsEventBus(o =>
{
    builder.Configuration.GetSection("EventBus").Bind(o);
    o.QueueName = "housekeeping.queue";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<HousekeepingDbContext>();
    db.Database.EnsureCreated();
    HousekeepingSeeder.Seed(db);
}

var bus = app.Services.GetRequiredService<IEventBus>();
bus.Subscribe<RoomCleaningRequestedEvent, RoomCleaningRequestedHandler>();
bus.StartConsuming();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "Housekeeping", status = "running" }));

app.Run();
