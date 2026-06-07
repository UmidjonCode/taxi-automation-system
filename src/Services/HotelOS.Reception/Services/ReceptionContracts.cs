using HotelOS.Shared.Enums;

namespace HotelOS.Reception.Services;

public sealed record GuestInfo(string FullName, string Email, string PhoneNumber, string? NationalId);

/// <summary>Request to search &amp; book an available room (R1/R3 of the brief).</summary>
public sealed record CreateBookingRequest
{
    public Guid? GuestId { get; init; }
    public GuestInfo? Guest { get; init; }
    public RoomStyle Style { get; init; }
    public DateTime CheckIn { get; init; }
    public DateTime CheckOut { get; init; }
    public int? PreferredFloor { get; init; }
    public string? ProximityPreference { get; init; }
    public decimal AdvancePayment { get; init; }
    public Guid? BranchId { get; init; }
}

public sealed record BookingResponse
{
    public required Guid BookingId { get; init; }
    public required Guid GuestId { get; init; }
    public required Guid RoomId { get; init; }
    public required string RoomNumber { get; init; }
    public required int MatchTier { get; init; }
    public required string AssignmentReason { get; init; }
    public required string Status { get; init; }
    public required decimal NightlyRate { get; init; }
    public required decimal AdvancePayment { get; init; }
}

public sealed record CheckInResponse
{
    public required Guid BookingId { get; init; }
    public required string RoomNumber { get; init; }
    public required string KeyCode { get; init; }
    public required bool IsMasterKey { get; init; }
}

public sealed record ConfirmBookingRequest(decimal AdvancePayment);
public sealed record AddExtraRequest(string Description, decimal Amount);
public sealed record CreateHoldRequest(Guid RoomId, Guid GuestId, DateTime CheckIn, DateTime CheckOut);
public sealed record ConfirmHoldRequest(decimal AdvancePayment);

