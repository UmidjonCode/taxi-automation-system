using HotelOS.RoomService.Data;
using HotelOS.RoomService.Events;
using HotelOS.RoomService.Services;
using HotelOS.Shared.Events;
using HotelOS.Shared.Messaging;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddDbContext<RoomServiceDbContext>(o =>
    o.UseSqlite(builder.Configuration.GetConnectionString("RoomServiceDb") ?? "Data Source=roomservice.db"));

builder.Services.AddScoped<RoomServiceFacade>();
builder.Services.AddScoped<GuestCheckedOutHandler>();

builder.Services.AddHotelOsEventBus(o =>
{
    builder.Configuration.GetSection("EventBus").Bind(o);
    o.QueueName = "roomservice.queue";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RoomServiceDbContext>();
    db.Database.EnsureCreated();
    RoomServiceSeeder.Seed(db);
}

var bus = app.Services.GetRequiredService<IEventBus>();
bus.Subscribe<GuestCheckedOutEvent, GuestCheckedOutHandler>();
bus.StartConsuming();

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.MapGet("/", () => Results.Ok(new { service = "RoomService", status = "running" }));

app.Run();
