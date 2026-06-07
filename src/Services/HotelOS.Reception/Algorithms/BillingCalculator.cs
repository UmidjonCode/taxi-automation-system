using HotelOS.Shared.Algorithms.Billing;

namespace HotelOS.Reception.Algorithms;

/// <summary>
/// Final bill = nightly rate × nights + room service + extra charges (no tax).
/// Refund = 24-hour cancellation policy: ≥24h before check-in → full refund;
/// &lt;24h → 50% refund; after check-in → none.
/// </summary>
public sealed class BillingCalculator : IBillingCalculator
{
    private const int RefundCutoffHours = 24;
    private const decimal LateCancellationRefundRate = 0.5m;

    public BillingResult CalculateFinalBill(BillingContext c)
    {
        int nights = Math.Max(1, (c.CheckOut.Date - c.CheckIn.Date).Days); // minimum one night
        decimal roomCharge = c.NightlyRate * nights;
        decimal extrasTotal = c.ExtraCharges.Sum(e => e.Amount);
        decimal subtotal = roomCharge + c.RoomServiceTotal + extrasTotal;
        decimal grandTotal = subtotal; // brief: no tax
        decimal balanceDue = grandTotal - c.AdvancePayment;

        var breakdown = new List<BillingLineItem>
        {
            new($"Room: {nights} night(s) @ {c.NightlyRate:0.00}", Round(roomCharge))
        };
        if (c.RoomServiceTotal > 0) breakdown.Add(new("Room service", Round(c.RoomServiceTotal)));
        breakdown.AddRange(c.ExtraCharges.Select(e => new BillingLineItem(e.Description, Round(e.Amount))));
        if (c.AdvancePayment > 0) breakdown.Add(new("Advance payment", -Round(c.AdvancePayment)));

        return new BillingResult
        {
            Nights = nights,
            RoomCharge = Round(roomCharge),
            RoomServiceTotal = Round(c.RoomServiceTotal),
            ExtrasTotal = Round(extrasTotal),
            Subtotal = Round(subtotal),
            GrandTotal = Round(grandTotal),
            AdvancePayment = Round(c.AdvancePayment),
            BalanceDue = Round(balanceDue),
            Breakdown = breakdown
        };
    }

    public RefundResult CalculateRefund(RefundContext c)
    {
        double hoursBeforeCheckIn = (c.CheckIn - c.CancellationTime).TotalHours;

        if (hoursBeforeCheckIn >= RefundCutoffHours)
            return new RefundResult
            {
                Refundable = true,
                RefundAmount = Round(c.AdvancePayment),
                PolicyApplied = $"Cancelled ≥{RefundCutoffHours}h before check-in — full refund."
            };

        if (hoursBeforeCheckIn >= 0)
            return new RefundResult
            {
                Refundable = true,
                RefundAmount = Round(c.AdvancePayment * LateCancellationRefundRate),
                PolicyApplied = $"Cancelled <{RefundCutoffHours}h before check-in — {LateCancellationRefundRate * 100:0}% refund."
            };

        return new RefundResult
        {
            Refundable = false,
            RefundAmount = 0m,
            PolicyApplied = "Cancelled after check-in — no refund."
        };
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.AwayFromZero);
}
