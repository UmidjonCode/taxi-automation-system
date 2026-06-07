using HotelOS.Housekeeping.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Housekeeping.Data;

/// <summary>Housekeeping's private database (Database-per-Service).</summary>
public class HousekeepingDbContext : DbContext
{
    public HousekeepingDbContext(DbContextOptions<HousekeepingDbContext> options) : base(options) { }

    public DbSet<Housekeeper> Housekeepers => Set<Housekeeper>();
    public DbSet<CleaningTask> CleaningTasks => Set<CleaningTask>();
}
