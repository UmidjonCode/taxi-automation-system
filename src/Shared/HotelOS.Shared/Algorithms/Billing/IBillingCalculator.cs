namespace HotelOS.Shared.Algorithms.Billing;

/// <summary>Pure billing calculations: final checkout bill and 24-hour cancellation refund.</summary>
public interface IBillingCalculator
{
    BillingResult CalculateFinalBill(BillingContext context);
    RefundResult CalculateRefund(RefundContext context);
}
