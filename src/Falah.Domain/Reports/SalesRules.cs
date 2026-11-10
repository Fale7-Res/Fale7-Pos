namespace Falah.Domain.Reports;

using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;

/// <summary>
/// Ported from selectSalesReport in lib/reporting-domain.mjs. Filtering (by business date, shift)
/// happens at the caller/query layer, exactly like the reference app's own function operates over
/// an already-selected `sales` array - callers must have already excluded Cancelled orders before
/// calling this (the reference app's own `orders.filter(status !== 'cancelled')` at the selection
/// point). Every figure here is derived from Order's own frozen fields; nothing is recomputed from
/// today's live prices/rules, so a later menu or offer edit can never retroactively change a past
/// report - the same guarantee Order's own snapshot fields already give a single order.
/// </summary>
public static class SalesRules
{
    public readonly record struct SalesReport(
        int OrderCount, Money GrossSales, Money Refunds, Money NetSales, Money ListSales,
        Money OffersValue, Money CompsValue, Money ManualDiscounts, Money PendingElectronicSales,
        Money CashWithDrivers, Money CollectedSales, Money AverageOrderValue,
        Money Cash, Money Card, Money InstaPay, Money Wallet,
        Money Pickup, Money Takeaway, Money Delivery);

    /// <summary>One refund as an event of the period it happened in. The refunded order's type, total
    /// and tax ride along because a refund can reverse a sale from outside the period - the order
    /// itself need not be among the period's sales for its refund to count in the period.</summary>
    public readonly record struct RefundEvent(
        string OrderId, Money Amount, PaymentMethod PaymentMethod, OrderType OrderType, Money OrderTotal, Money OrderTaxAmount);

    /// <summary>The refunds already applied to these orders, one event per order, as if each had
    /// happened in the same period as its sale - the basis every report used before refunds were
    /// accounted for on their own date. Kept for callers that only have the orders.</summary>
    public static IReadOnlyList<RefundEvent> RefundEventsOf(IEnumerable<Order> orders) =>
        orders.Where(o => o.RefundAmount.IsPositive)
            .Select(o => new RefundEvent(o.Id, o.RefundAmount, o.PaymentMethod, o.OrderType, o.Total, o.TaxAmount))
            .ToList();

    /// <summary>The orders' own refunds, counted against the period of the sale. See the overload
    /// that takes the period's refund events for the basis reports use.</summary>
    public static SalesReport ComputeSalesReport(
        IReadOnlyList<Order> sales, IReadOnlySet<string> orderIdsWithCashOutsideRestaurant) =>
        ComputeSalesReport(sales, orderIdsWithCashOutsideRestaurant, RefundEventsOf(sales));

    /// <summary>
    /// Cash-basis: an order's money counts as COLLECTED only once the restaurant actually holds it.
    ///
    /// Sales and refunds are each counted in the period they happened in. Gross sales are the
    /// period's own orders at their full value; refunds are the refunds MADE in the period, whichever
    /// period the sale they reverse belongs to. So a period already reported never changes when an
    /// old sale is refunded later, and the period the refund is paid in shows it - in net sales and
    /// in the payment channel the money went back through - the same day the drawer pays it.
    /// </summary>
    /// <param name="orderIdsWithCashOutsideRestaurant">
    /// Orders whose cash-on-delivery money has not yet been transferred into the restaurant's own
    /// custody by a driver settlement. The caller supplies this because settlement lives in the
    /// Delivery aggregate, not on Order - Order.IsPaid is frozen at checkout time and is never
    /// flipped by the delivery lifecycle, so it cannot answer this question on its own.
    /// </param>
    /// <param name="refundsInPeriod">Every refund made in the period being reported.</param>
    public static SalesReport ComputeSalesReport(
        IReadOnlyList<Order> sales, IReadOnlySet<string> orderIdsWithCashOutsideRestaurant,
        IReadOnlyList<RefundEvent> refundsInPeriod)
    {
        var grossSales = SumMoney(sales, o => o.Total);
        var refunds = Money.FromPiastres(refundsInPeriod.Sum(r => r.Amount.Piastres));
        // Value given away, kept in THREE separate buckets because they answer different questions
        // and are authorized differently: offersValue (a live price rule - never called a discount,
        // the offer price IS the price), compsValue (PIN-authorized hospitality/complaint - full
        // value stays on the order, this is what the restaurant absorbed), manualDiscounts (a
        // cashier-entered reduction to the taxable base). Each item's own list-vs-charged gap is
        // clamped to zero BEFORE summing (never after), matching the reference app exactly.
        var offersValue = Money.FromPiastres(sales.Sum(o => o.Items.Sum(i =>
            Math.Max(0, i.ListUnitPrice.Piastres - i.UnitPrice.Piastres) * i.Quantity)));
        var compsValue = SumMoney(sales, o => o.CompAmount);
        var manualDiscounts = SumMoney(sales, o => o.Discount);
        var listSales = grossSales + offersValue + compsValue + manualDiscounts;
        var netSales = grossSales - refunds;

        // An electronic sale whose transfer has not been confirmed is a CLAIM, not money - it must
        // never be counted as collected, or this report would claim cash the restaurant does not
        // have and would disagree with the drawer and the Business Day close. Only the claim itself
        // is withheld: the leg's own amount, which is the whole order for a standalone InstaPay/
        // wallet sale but only the electronic part of a split, whose cash/card part is already held.
        var pendingElectronicSales = Money.FromPiastres(sales.Sum(o =>
            o.ElectronicPayment is { Status: ElectronicPaymentStatus.Pending } leg ? leg.Amount.Piastres : 0));

        // The same principle applied to cash-on-delivery, which the original formula missed: until a
        // driver settlement moves it into the restaurant's own treasury, COD money is either still
        // with the customer (not yet delivered) or in the driver's custody (delivered, unsettled).
        // Either way the restaurant does not hold it, so counting it as collected would claim cash
        // that is not there - exactly what excluding pending electronic sales already guards against.
        // At the sale's full value, like gross sales: a refund of it is already a refund of the
        // period it was paid in (it left the drawer), so netting it here as well would count the
        // money the drawer paid out as money the restaurant still has coming.
        var cashWithDrivers = Money.FromPiastres(sales
            .Where(o => orderIdsWithCashOutsideRestaurant.Contains(o.Id))
            .Sum(o => o.Total.Piastres));

        var collectedSales = netSales - pendingElectronicSales - cashWithDrivers;

        // Same channel attribution as the Shifts drawer reconciliation and the Business Day close
        // snapshot - a split payment's cash/card leg posts immediately, its InstaPay/wallet leg
        // only once confirmed - less the period's refunds through each channel.
        var soldThrough = OrderPaymentQueries.PaymentMethodTotalsAsSold(sales);
        Money Channel(PaymentMethod method) => soldThrough[method] - Money.FromPiastres(refundsInPeriod
            .Where(r => r.PaymentMethod == method).Sum(r => r.Amount.Piastres));

        Money OrderTypeNet(OrderType type) =>
            Money.FromPiastres(sales.Where(o => o.OrderType == type).Sum(o => o.Total.Piastres)
                - refundsInPeriod.Where(r => r.OrderType == type).Sum(r => r.Amount.Piastres));

        var averageOrderValue = sales.Count > 0 ? Money.FromPiastres(netSales.Piastres / sales.Count) : Money.Zero;

        return new SalesReport(
            sales.Count, grossSales, refunds, netSales, listSales, offersValue, compsValue, manualDiscounts,
            pendingElectronicSales, cashWithDrivers, collectedSales, averageOrderValue,
            Channel(PaymentMethod.Cash), Channel(PaymentMethod.Card), Channel(PaymentMethod.InstaPay), Channel(PaymentMethod.Wallet),
            OrderTypeNet(OrderType.Pickup), OrderTypeNet(OrderType.Takeaway), OrderTypeNet(OrderType.Delivery));
    }

    private static Money SumMoney(IReadOnlyList<Order> orders, Func<Order, Money> selector) =>
        Money.FromPiastres(orders.Sum(o => selector(o).Piastres));

    public readonly record struct TaxReport(Money Gross, Money Taxable, Money Tax, Money RefundTaxImpact, Money NetTax);

    /// <summary>Ported verbatim from TaxScreen.tsx's own report computation. Reads ONLY each
    /// order's own frozen TaxAmount/TaxableBase - an order that was never taxed at checkout
    /// (TaxAmount/TaxableBase both zero, no rule was active then) reports as zero, never
    /// retroactively recomputed with whatever tax rule happens to be active today (the exact
    /// failure mode legacy's own code comment warns against: activating a new rule must never
    /// suddenly inflate this report for revenue that was never actually taxed). RefundTaxImpact is
    /// a proportional estimate (orderTax * refundAmount/total), only applied when the order was
    /// both taxed and refunded - never negative, never exceeding the order's own tax.</summary>
    public static TaxReport ComputeTaxReport(IReadOnlyList<Order> orders) =>
        ComputeTaxReport(orders, RefundEventsOf(orders));

    /// <summary>The same report with the tax given back counted in the period the refund was made,
    /// like SalesRules' sales figures: each refund event carries its order's own frozen tax and
    /// total, so the proportional estimate is the same one, applied on the refund's date.</summary>
    public static TaxReport ComputeTaxReport(IReadOnlyList<Order> orders, IReadOnlyList<RefundEvent> refundsInPeriod)
    {
        var gross = SumMoney(orders, o => o.Total);
        var taxable = SumMoney(orders, o => o.TaxableBase);
        var tax = SumMoney(orders, o => o.TaxAmount);
        var refundTaxImpact = Money.FromPiastres(refundsInPeriod.Sum(r =>
            r.Amount.IsPositive && r.OrderTotal.IsPositive && r.OrderTaxAmount.IsPositive
                ? (long)Math.Round(r.OrderTaxAmount.Piastres * (decimal)r.Amount.Piastres / r.OrderTotal.Piastres)
                : 0));
        return new TaxReport(gross, taxable, tax, refundTaxImpact, tax - refundTaxImpact);
    }
}
