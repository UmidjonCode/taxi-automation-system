using HotelOS.Reception.Models;
using HotelOS.Shared.Enums;

namespace HotelOS.Reception.Data;

/// <summary>Seeds the brief's requirement: 10 rooms across 2 floors, plus a demo guest.</summary>
public static class ReceptionSeeder
{
    /// <summary>Default branch id (multi-branch is supported; we seed one branch).</summary>
    public static readonly Guid DefaultBranchId = new("b9a1c0de-0001-0001-0001-000000000001");

    public static void Seed(ReceptionDbContext db)
    {
        if (db.Rooms.Any()) return;

        var now = DateTime.UtcNow;

        // 10 rooms over 2 floors. LastCleanedAt is staggered so the
        // "longest clean duration" ranking is visible in demos.
        var rooms = new List<Room>
        {
            NewRoom("101", 1, RoomStyle.Standard,      80m,  "Elevator", now.AddHours(-30)),
            NewRoom("102", 1, RoomStyle.Standard,      80m,  "Elevator", now.AddHours(-12)),
            NewRoom("103", 1, RoomStyle.Deluxe,        120m, "Quiet",    now.AddHours(-48)),
            NewRoom("104", 1, RoomStyle.Deluxe,        120m, "Quiet",    now.AddHours(-6)),
            NewRoom("105", 1, RoomStyle.FamilySuite,   180m, "Quiet",    now.AddHours(-20)),
            NewRoom("201", 2, RoomStyle.Standard,      80m,  "Elevator", now.AddHours(-40)),
            NewRoom("202", 2, RoomStyle.Deluxe,        120m, "Elevator", now.AddHours(-18)),
            NewRoom("203", 2, RoomStyle.FamilySuite,   180m, "Quiet",    now.AddHours(-9)),
            NewRoom("204", 2, RoomStyle.BusinessSuite, 250m, "Quiet",    now.AddHours(-72)),
            NewRoom("205", 2, RoomStyle.BusinessSuite, 250m, "Quiet",    now.AddHours(-3)),
        };
        db.Rooms.AddRange(rooms);

        db.Guests.Add(new Guest
        {
            Id = new Guid("a0000000-0000-0000-0000-000000000001"),
            FullName = "Demo Guest",
            Email = "demo.guest@hotelos.local",
            PhoneNumber = "+998000000000",
            NationalId = "AA0000000"
        });

        db.SaveChanges();
    }

    private static Room NewRoom(string number, int floor, RoomStyle style, decimal rate, string zone, DateTime lastCleaned) => new()
    {
        RoomNumber = number,
        Floor = floor,
        Style = style,
        NightlyRate = rate,
        Status = RoomStatus.Available,
        CleanStatus = RoomCleanStatus.Clean,
        LastCleanedAt = lastCleaned,
        ProximityZone = zone,
        KeyCode = $"K{number}",
        BranchId = DefaultBranchId
    };
}
