using HotelOS.Reception.Algorithms;
using HotelOS.Shared.Algorithms.Billing;
using Xunit;

namespace HotelOS.Algorithms.Tests;

public class BillingCalculatorTests
{
    private readonly BillingCalculator _sut = new();
    private static readonly DateTime Jan10 = new(2026, 1, 10, 14, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Final_bill_is_rate_times_nights_plus_service_plus_extras_no_tax()
    {
        var ctx = new BillingContext
        {
            BookingId = Guid.NewGuid(),
            NightlyRate = 100m,
            CheckIn = Jan10,
            CheckOut = Jan10.AddDays(3),                 // 3 nights
            RoomServiceTotal = 50m,
            ExtraCharges = new[] { new BillingLineItem("Spa", 20m) },
            AdvancePayment = 100m
        };

        var bill = _sut.CalculateFinalBill(ctx);

        Assert.Equal(3, bill.Nights);
        Assert.Equal(300m, bill.RoomCharge);
        Assert.Equal(20m, bill.ExtrasTotal);
        Assert.Equal(370m, bill.Subtotal);   // 300 + 50 + 20
        Assert.Equal(370m, bill.GrandTotal);  // no tax
        Assert.Equal(270m, bill.BalanceDue);  // minus 100 advance
    }

    [Fact]
    public void Charges_minimum_one_night_for_same_day()
    {
        var ctx = new BillingContext
        {
            BookingId = Guid.NewGuid(),
            NightlyRate = 120m,
            CheckIn = Jan10,
            CheckOut = Jan10,                            // 0 calendar nights
            AdvancePayment = 0m
        };

        var bill = _sut.CalculateFinalBill(ctx);

        Assert.Equal(1, bill.Nights);
        Assert.Equal(120m, bill.GrandTotal);
    }

    [Fact]
    public void Refund_is_full_when_cancelled_24h_or_more_before_checkin()
    {
        var refund = _sut.CalculateRefund(new RefundContext
        {
            BookingId = Guid.NewGuid(),
            CheckIn = Jan10,
            CancellationTime = Jan10.AddHours(-48),
            AdvancePayment = 200m
        });

        Assert.True(refund.Refundable);
        Assert.Equal(200m, refund.RefundAmount);
    }

    [Fact]
    public void Refund_is_half_when_cancelled_within_24h()
    {
        var refund = _sut.CalculateRefund(new RefundContext
        {
            BookingId = Guid.NewGuid(),
            CheckIn = Jan10,
            CancellationTime = Jan10.AddHours(-10),
            AdvancePayment = 200m
        });

        Assert.True(refund.Refundable);
        Assert.Equal(100m, refund.RefundAmount);   // 50%
    }

    [Fact]
    public void No_refund_after_checkin_time()
    {
        var refund = _sut.CalculateRefund(new RefundContext
        {
            BookingId = Guid.NewGuid(),
            CheckIn = Jan10,
            CancellationTime = Jan10.AddHours(2),
            AdvancePayment = 200m
        });

        Assert.False(refund.Refundable);
        Assert.Equal(0m, refund.RefundAmount);
    }
}
