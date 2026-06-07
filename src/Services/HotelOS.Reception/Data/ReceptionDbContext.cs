using HotelOS.Reception.Models;
using Microsoft.EntityFrameworkCore;

namespace HotelOS.Reception.Data;

/// <summary>Reception's private database (Database-per-Service). No other service may touch it.</summary>
public class ReceptionDbContext : DbContext
{
    public ReceptionDbContext(DbContextOptions<ReceptionDbContext> options) : base(options) { }

    public DbSet<Guest> Guests => Set<Guest>();
    public DbSet<Room> Rooms => Set<Room>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<BookingExtra> BookingExtras => Set<BookingExtra>();
    public DbSet<RoomKey> RoomKeys => Set<RoomKey>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Guest>().HasIndex(g => g.Email).IsUnique();
        b.Entity<Room>().HasIndex(r => r.RoomNumber).IsUnique();

        b.Entity<Booking>()
            .HasOne(x => x.Guest).WithMany(g => g.Bookings).HasForeignKey(x => x.GuestId);
        b.Entity<Booking>()
            .HasOne(x => x.Room).WithMany().HasForeignKey(x => x.RoomId);
        b.Entity<Booking>()
            .HasMany(x => x.Extras).WithOne().HasForeignKey(x => x.BookingId);
    }
}
