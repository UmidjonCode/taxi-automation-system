using HotelOS.Maintenance.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Maintenance.Data;

/// <summary>Maintenance's private database (Database-per-Service).</summary>
public class MaintenanceDbContext : DbContext
{
    public MaintenanceDbContext(DbContextOptions<MaintenanceDbContext> options) : base(options) { }

    public DbSet<MaintenanceRequest> MaintenanceRequests => Set<MaintenanceRequest>();
}
