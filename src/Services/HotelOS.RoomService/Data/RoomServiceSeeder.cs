using HotelOS.RoomService.Models;

namespace HotelOS.RoomService.Data;

public static class RoomServiceSeeder
{
    public static void Seed(RoomServiceDbContext db)
    {
        if (db.MenuItems.Any()) return;

        db.MenuItems.AddRange(
            new MenuItem { Name = "Club Sandwich",   Category = "Food",     Price = 12.50m },
            new MenuItem { Name = "Caesar Salad",     Category = "Food",     Price = 9.00m },
            new MenuItem { Name = "Fresh Orange Juice", Category = "Drink",  Price = 5.00m },
            new MenuItem { Name = "Bottle of Water",  Category = "Drink",    Price = 2.00m },
            new MenuItem { Name = "Spa Slippers",     Category = "Amenity",  Price = 7.50m },
            new MenuItem { Name = "Late Checkout",    Category = "Amenity",  Price = 25.00m });

        db.SaveChanges();
    }
}
