namespace HotelOS.Shared.Algorithms.Billing;

/// <summary>A single named charge on a bill (e.g. "Minibar", "Spa").</summary>
public sealed record BillingLineItem(string Description, decimal Amount);

/// <summary>All inputs needed to compute a final bill. No tax (per project brief).</summary>
public sealed record BillingContext
{
    public required Guid BookingId { get; init; }
    public required decimal NightlyRate { get; init; }
    public required DateTime CheckIn { get; init; }
    public required DateTime CheckOut { get; init; }
    public decimal RoomServiceTotal { get; init; }
    public IReadOnlyList<BillingLineItem> ExtraCharges { get; init; } = Array.Empty<BillingLineItem>();
    public decimal AdvancePayment { get; init; }
}

/// <summary>Fully itemised bill: rate × nights + room service + extras − advance.</summary>
public sealed record BillingResult
{
    public required int Nights { get; init; }
    public required decimal RoomCharge { get; init; }
    public required decimal RoomServiceTotal { get; init; }
    public required decimal ExtrasTotal { get; init; }
    public required decimal Subtotal { get; init; }
    public required decimal GrandTotal { get; init; }
    public required decimal AdvancePayment { get; init; }
    public required decimal BalanceDue { get; init; }
    public IReadOnlyList<BillingLineItem> Breakdown { get; init; } = Array.Empty<BillingLineItem>();
}

/// <summary>Inputs for the 24-hour cancellation refund calculation.</summary>
public sealed record RefundContext
{
    public required Guid BookingId { get; init; }
    public required DateTime CheckIn { get; init; }
    public required DateTime CancellationTime { get; init; }
    public required decimal AdvancePayment { get; init; }
}

/// <summary>Result of the refund policy: how much is returned and which rule applied.</summary>
public sealed record RefundResult
{
    public required bool Refundable { get; init; }
    public required decimal RefundAmount { get; init; }
    public required string PolicyApplied { get; init; }
}
