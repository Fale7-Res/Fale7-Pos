namespace Falah.Domain.Delivery;

/// <summary>
/// UI-REQ-001: one delivery driver's statement for one restaurant business day.
///
/// Which orders: the orders of that business day (the order's own BusinessDayId - a sale counts in
/// the period it was made, so a delivery finished after midnight still belongs to the day it was sold)
/// whose delivery is currently assigned to the driver. A delivery reassigned away belongs to the driver
/// who ended up with it - the one holding its custody - and the row says whom it came from.
///
/// The figures are kept apart on purpose and never added together:
/// - food sales: what the order charged for food (order total minus its delivery charge);
/// - delivery charge: what the customer paid for delivery - NOT the driver's wage or commission;
/// - order total; refunds made on these orders;
/// - prepaid: the part already paid before the driver left (card, confirmed transfer);
/// - cash to collect: what the driver is responsible for collecting, by the same rule the custody
///   settlement uses (DeliveryRules.DeliveryCashResponsibility);
/// - collected: what the driver recorded collecting, and whether that custody is settled yet.
/// </summary>
public sealed record DriverStatementOrder(
    string OrderId,
    string OrderNumber,
    DateTimeOffset OrderedAtUtc,
    string? CustomerName,
    string? CustomerPhone,
    string? Address,
    string PaymentMethod,
    string DeliveryStatus,
    long FoodSalesPiastres,
    long DeliveryFeePiastres,
    long OrderTotalPiastres,
    long RefundedPiastres,
    long PrepaidPiastres,
    long CashToCollectPiastres,
    long CollectedPiastres,
    bool CustodySettled,
    string? ReassignedFromName,
    bool Cancelled);

public sealed record DriverStatementTotals(
    int InvoiceCount,
    long FoodSalesPiastres,
    long DeliveryFeesPiastres,
    long OrderTotalPiastres,
    long RefundedPiastres,
    long PrepaidPiastres,
    long CashToCollectPiastres,
    long CollectedPiastres,
    long CollectedSettledPiastres,
    long CollectedUnsettledPiastres,
    int FailedOrReturnedCount,
    int CancelledCount);

public static class DriverStatementRules
{
    /// <summary>The payment-method filter values the statement accepts; null or "all" means every method.</summary>
    public static readonly IReadOnlyList<string> PaymentFilters = new[] { "all", "cash", "card", "instapay", "wallet", "multi" };

    public static bool MatchesPaymentFilter(string orderPaymentMethod, string? filter) =>
        string.IsNullOrWhiteSpace(filter) || filter == "all"
        || string.Equals(orderPaymentMethod, filter, StringComparison.OrdinalIgnoreCase);

    /// <summary>Totals over exactly the rows shown. Cancelled orders are listed but are not sales:
    /// they are counted separately and add nothing to any money total or to the invoice count.</summary>
    public static DriverStatementTotals Totals(IEnumerable<DriverStatementOrder> rows)
    {
        var all = rows.ToList();
        var live = all.Where(r => !r.Cancelled).ToList();
        return new DriverStatementTotals(
            InvoiceCount: live.Count,
            FoodSalesPiastres: live.Sum(r => r.FoodSalesPiastres),
            DeliveryFeesPiastres: live.Sum(r => r.DeliveryFeePiastres),
            OrderTotalPiastres: live.Sum(r => r.OrderTotalPiastres),
            RefundedPiastres: live.Sum(r => r.RefundedPiastres),
            PrepaidPiastres: live.Sum(r => r.PrepaidPiastres),
            CashToCollectPiastres: live.Sum(r => r.CashToCollectPiastres),
            CollectedPiastres: live.Sum(r => r.CollectedPiastres),
            CollectedSettledPiastres: live.Where(r => r.CustodySettled).Sum(r => r.CollectedPiastres),
            CollectedUnsettledPiastres: live.Where(r => !r.CustodySettled).Sum(r => r.CollectedPiastres),
            // Counted over every row: a failed cash order is voided by the result itself, and the failure must still show.
            FailedOrReturnedCount: all.Count(r => r.DeliveryStatus is "Failed" or "Returned"),
            CancelledCount: all.Count - live.Count);
    }
}
