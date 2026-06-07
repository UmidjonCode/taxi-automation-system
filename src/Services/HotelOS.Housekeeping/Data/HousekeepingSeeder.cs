using HotelOS.Housekeeping.Models;

namespace HotelOS.Housekeeping.Data;

public static class HousekeepingSeeder
{
    public static void Seed(HousekeepingDbContext db)
    {
        if (db.Housekeepers.Any()) return;

        db.Housekeepers.AddRange(
            new Housekeeper { Id = new Guid("c0000000-0000-0000-0000-000000000001"), FullName = "Aziza Karimova" },
            new Housekeeper { Id = new Guid("c0000000-0000-0000-0000-000000000002"), FullName = "Bekzod Aliyev" });

        db.SaveChanges();
    }
}
