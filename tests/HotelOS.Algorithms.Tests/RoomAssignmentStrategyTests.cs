using HotelOS.Reception.Algorithms;
using HotelOS.Shared.Algorithms.RoomAssignment;
using HotelOS.Shared.Enums;
using Xunit;

namespace HotelOS.Algorithms.Tests;

public class RoomAssignmentStrategyTests
{
    private readonly RoomAssignmentStrategy _sut = new();
    private static readonly DateTime Now = DateTime.UtcNow;

    private static RoomCandidate Room(string number, int floor, RoomStyle style,
        RoomStatus status = RoomStatus.Available, RoomCleanStatus clean = RoomCleanStatus.Clean,
        double cleanedHoursAgo = 1, string? zone = null) => new()
    {
        RoomId = Guid.NewGuid(),
        RoomNumber = number,
        Floor = floor,
        Style = style,
        Status = status,
        CleanStatus = clean,
        LastCleanedAt = Now.AddHours(-cleanedHoursAgo),
        ProximityZone = zone
    };

    private static RoomAssignmentRequest Request(RoomStyle style, int? floor = null, string? proximity = null) => new()
    {
        RequestedStyle = style,
        PreferredFloor = floor,
        ProximityPreference = proximity,
        CheckIn = Now.Date,
        CheckOut = Now.Date.AddDays(2),
        BranchId = Guid.NewGuid()
    };

    [Fact]
    public void Filters_out_wrong_style_dirty_and_occupied_rooms()
    {
        var candidates = new[]
        {
            Room("101", 1, RoomStyle.Deluxe),                                   // wrong style
            Room("102", 1, RoomStyle.Standard, clean: RoomCleanStatus.Dirty),   // dirty
            Room("103", 1, RoomStyle.Standard, status: RoomStatus.Occupied),    // occupied
            Room("104", 1, RoomStyle.Standard)                                  // the only valid one
        };

        var result = _sut.Assign(Request(RoomStyle.Standard), candidates);

        Assert.True(result.Success);
        Assert.Equal("104", result.RoomNumber);
    }

    [Fact]
    public void Prefers_requested_floor()
    {
        var candidates = new[]
        {
            Room("105", 1, RoomStyle.Standard, cleanedHoursAgo: 50),
            Room("205", 2, RoomStyle.Standard, cleanedHoursAgo: 2)
        };

        var result = _sut.Assign(Request(RoomStyle.Standard, floor: 2), candidates);

        Assert.True(result.Success);
        Assert.Equal("205", result.RoomNumber);
        Assert.Equal(0, result.MatchTier); // ideal: floor met (no proximity asked)
    }

    [Fact]
    public void Among_equal_floor_picks_longest_clean_duration()
    {
        var candidates = new[]
        {
            Room("101", 1, RoomStyle.Standard, cleanedHoursAgo: 10),
            Room("102", 1, RoomStyle.Standard, cleanedHoursAgo: 40) // clean longest -> chosen
        };

        var result = _sut.Assign(Request(RoomStyle.Standard, floor: 1), candidates);

        Assert.Equal("102", result.RoomNumber);
    }

    [Fact]
    public void Falls_back_to_other_floor_when_preferred_floor_has_no_match()
    {
        var candidates = new[]
        {
            Room("101", 1, RoomStyle.Standard, cleanedHoursAgo: 5) // only floor 1 available
        };

        var result = _sut.Assign(Request(RoomStyle.Standard, floor: 2), candidates);

        Assert.True(result.Success);
        Assert.Equal("101", result.RoomNumber);
        Assert.True(result.MatchTier >= 2); // floor fallback
    }

    [Fact]
    public void Proximity_preference_wins_over_clean_duration()
    {
        var candidates = new[]
        {
            Room("101", 1, RoomStyle.Standard, cleanedHoursAgo: 80, zone: "Elevator"),
            Room("102", 1, RoomStyle.Standard, cleanedHoursAgo: 5,  zone: "Quiet")
        };

        var result = _sut.Assign(Request(RoomStyle.Standard, floor: 1, proximity: "Quiet"), candidates);

        Assert.Equal("102", result.RoomNumber); // proximity beats the longer-clean room
    }

    [Fact]
    public void Returns_failure_when_no_clean_available_room()
    {
        var candidates = new[]
        {
            Room("101", 1, RoomStyle.Standard, clean: RoomCleanStatus.Dirty)
        };

        var result = _sut.Assign(Request(RoomStyle.Standard), candidates);

        Assert.False(result.Success);
        Assert.Null(result.RoomId);
    }
}
