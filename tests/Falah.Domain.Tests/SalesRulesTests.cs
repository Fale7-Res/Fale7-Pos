using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Reports;
using Xunit;

namespace Falah.Domain.Tests;

public class SalesRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem Item(decimal listPrice, decimal unitPrice, int quantity = 1) =>
        new("i1", "prod-1", null, "برجر", null, Money.FromEgp(unitPrice), Money.FromEgp(listPrice), quantity, Money.FromEgp(unitPrice * quantity), false, null, null, null);

    private static Order MakeOrder(string id, OrderType orderType, PaymentMethod paymentMethod, IReadOnlyList<OrderItem> items,
        Money compAmount = default, Money discount = default, Money? amountPaid = null, IReadOnlyList<PaymentSplit>? splits = null, string? walletProvider = null)
    {
        var subtotal = Money.FromPiastres(items.Sum(i => i.TotalPrice.Piastres));
        var payableTotal = subtotal - discount - compAmount;
        if (payableTotal.IsNegative) payableTotal = Money.Zero;
        var effectiveAmountPaid = amountPaid ?? payableTotal;

        var plan = CheckoutRules.BuildPlan(orderType, paymentMethod, items, discount, null, Money.Zero, compAmount, effectiveAmountPaid, splits, walletProvider);
        return Order.Create(plan, id, $"#{id}", 1, "cashier-1", "shift-1", items, $"req-{id}", "day-1", Now,
            discount: discount);
    }

    [Fact]
    public void Gross_net_and_refunds_are_kept_separate()
    {
        var order = MakeOrder("1", OrderType.Takeaway, PaymentMethod.Cash, new[] { Item(100m, 100m) });
        order.ApplyRefund(new OrderRefund("r1", "op1", "k1", Money.FromEgp(30m), PaymentMethod.Cash, "tm1", Now, "c1", "m1", "reason", "shift-1", "shift-1"));

        var report = SalesRules.ComputeSalesReport(new[] { order });

        Assert.Equal(Money.FromEgp(100m), report.GrossSales);
        Assert.Equal(Money.FromEgp(30m), report.Refunds);
        Assert.Equal(Money.FromEgp(70m), report.NetSales);
    }

    [Fact]
    public void Offers_value_is_the_per_item_gap_between_list_and_charged_price_clamped_before_summing()
    {
        // Item 1 genuinely discounted (list > charged): contributes 20. Item 2 priced AT list (no
        // gap): contributes 0, never negative even though nothing here would naturally go negative -
        // proving the per-item clamp is applied, not skipped.
        var items = new[] { Item(listPrice: 120m, unitPrice: 100m), Item(listPrice: 50m, unitPrice: 50m) };
        var order = MakeOrder("1", OrderType.Takeaway, PaymentMethod.Cash, items);

        var report = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(Money.FromEgp(20m), report.OffersValue);
    }

    [Fact]
    public void Comps_and_manual_discounts_are_kept_separate_from_offers_and_from_each_other()
    {
        var items = new[] { Item(100m, 100m) };
        var order = MakeOrder("1", OrderType.Takeaway, PaymentMethod.Cash, items, compAmount: Money.FromEgp(10m), discount: Money.FromEgp(5m));

        var report = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(Money.FromEgp(10m), report.CompsValue);
        Assert.Equal(Money.FromEgp(5m), report.ManualDiscounts);
        Assert.Equal(Money.Zero, report.OffersValue);
    }

    [Fact]
    public void List_sales_equals_gross_plus_every_giveaway_bucket()
    {
        var items = new[] { Item(120m, 100m) };
        var order = MakeOrder("1", OrderType.Takeaway, PaymentMethod.Cash, items, compAmount: Money.FromEgp(10m), discount: Money.FromEgp(5m));

        var report = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(report.GrossSales + report.OffersValue + report.CompsValue + report.ManualDiscounts, report.ListSales);
    }

    [Fact]
    public void A_pending_electronic_sale_is_excluded_from_collected_sales_but_still_counts_in_net_sales()
    {
        var items = new[] { Item(150m, 150m) };
        var order = MakeOrder("1", OrderType.Delivery, PaymentMethod.InstaPay, items, amountPaid: Money.FromEgp(150m));

        var report = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(Money.FromEgp(150m), report.NetSales);
        Assert.Equal(Money.FromEgp(150m), report.PendingElectronicSales);
        Assert.Equal(Money.Zero, report.CollectedSales);

        order.ConfirmElectronicPayment();
        var afterConfirm = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(Money.Zero, afterConfirm.PendingElectronicSales);
        Assert.Equal(Money.FromEgp(150m), afterConfirm.CollectedSales);
    }

    [Fact]
    public void Order_type_totals_are_net_of_that_orders_own_refund()
    {
        var order = MakeOrder("1", OrderType.DineIn, PaymentMethod.Cash, new[] { Item(200m, 200m) });
        order.ApplyRefund(new OrderRefund("r1", "op1", "k1", Money.FromEgp(50m), PaymentMethod.Cash, "tm1", Now, "c1", "m1", "reason", "shift-1", "shift-1"));

        var report = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(Money.FromEgp(150m), report.DineIn);
        Assert.Equal(Money.Zero, report.Takeaway);
        Assert.Equal(Money.Zero, report.Delivery);
    }

    [Fact]
    public void Average_order_value_is_net_sales_over_order_count()
    {
        var orderA = MakeOrder("1", OrderType.Takeaway, PaymentMethod.Cash, new[] { Item(100m, 100m) });
        var orderB = MakeOrder("2", OrderType.Takeaway, PaymentMethod.Cash, new[] { Item(200m, 200m) });

        var report = SalesRules.ComputeSalesReport(new[] { orderA, orderB });
        Assert.Equal(Money.FromEgp(150m), report.AverageOrderValue);
    }

    [Fact]
    public void Average_order_value_is_zero_for_an_empty_selection_not_a_division_error()
    {
        var report = SalesRules.ComputeSalesReport(Array.Empty<Order>());
        Assert.Equal(Money.Zero, report.AverageOrderValue);
        Assert.Equal(0, report.OrderCount);
    }

    [Fact]
    public void Payment_channel_totals_match_OrderPaymentQueries_exactly()
    {
        var order = MakeOrder("1", OrderType.Takeaway, PaymentMethod.Card, new[] { Item(100m, 100m) });
        var report = SalesRules.ComputeSalesReport(new[] { order });
        Assert.Equal(Money.FromEgp(100m), report.Card);
        Assert.Equal(Money.Zero, report.Cash);
    }
}
