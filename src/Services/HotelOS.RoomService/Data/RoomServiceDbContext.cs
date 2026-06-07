using HotelOS.RoomService.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.RoomService.Data;

/// <summary>Room Service's private database (Database-per-Service).</summary>
public class RoomServiceDbContext : DbContext
{
    public RoomServiceDbContext(DbContextOptions<RoomServiceDbContext> options) : base(options) { }

    public DbSet<MenuItem> MenuItems => Set<MenuItem>();
    public DbSet<ServiceOrder> ServiceOrders => Set<ServiceOrder>();
    public DbSet<ServiceOrderLine> ServiceOrderLines => Set<ServiceOrderLine>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<ServiceOrder>()
            .HasMany(o => o.Lines).WithOne().HasForeignKey(l => l.ServiceOrderId);
    }
}
