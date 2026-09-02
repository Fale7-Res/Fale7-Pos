using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Xunit;

namespace Falah.Domain.Tests;

public class OrderPaymentQueriesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem Item(decimal price) =>
        new("i1", "prod-1", null, "برجر", null, Money.FromEgp(price), Money.FromEgp(price), 1, Money.FromEgp(price), false, null, null, null);

    private static Order CashOrder(decimal total, string id) =>
        Order.Create(
            CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, new[] { Item(total) }, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(total), null, null),
            id, $"#{id}", 1, "cashier-1", "shift-1", new[] { Item(total) }, $"req-{id}", "day-1", Now);

    [Fact]
    public void Cash_sales_count_the_net_amount_after_refunds()
    {
        var order = CashOrder(200m, "1");
        order.ApplyRefund(new OrderRefund("r1", "op1", "k1", Money.FromEgp(50m), PaymentMethod.Cash, "tm1", Now, "c1", "m1", "reason", "shift-1", "shift-1"));

        var totals = OrderPaymentQueries.PaymentMethodTotals(new[] { order });
        Assert.Equal(Money.FromEgp(150m), totals[PaymentMethod.Cash]);
    }

    [Fact]
    public void An_unconfirmed_instapay_order_contributes_nothing_until_confirmed()
    {
        var items = new[] { Item(150m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.InstaPay, items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(150m), null, null);
        var order = Order.Create(plan, "ord-2", "#2", 2, "cashier-1", "shift-1", items, "req-2", "day-1", Now);

        var totals = OrderPaymentQueries.PaymentMethodTotals(new[] { order });
        Assert.Equal(Money.Zero, totals[PaymentMethod.InstaPay]);

        order.ConfirmElectronicPayment();
        var totalsAfterConfirm = OrderPaymentQueries.PaymentMethodTotals(new[] { order });
        Assert.Equal(Money.FromEgp(150m), totalsAfterConfirm[PaymentMethod.InstaPay]);
    }

    [Fact]
    public void A_split_orders_cash_leg_counts_immediately_while_its_sibling_electronic_leg_is_still_pending()
    {
        var items = new[] { Item(200m) };
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(100m)), new PaymentSplit(PaymentMethod.InstaPay, Money.FromEgp(100m)) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Multi, items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(200m), splits, null);
        var order = Order.Create(plan, "ord-3", "#3", 3, "cashier-1", "shift-1", items, "req-3", "day-1", Now);

        var totals = OrderPaymentQueries.PaymentMethodTotals(new[] { order });
        Assert.Equal(Money.FromEgp(100m), totals[PaymentMethod.Cash]);
        Assert.Equal(Money.Zero, totals[PaymentMethod.InstaPay]);
    }

    [Fact]
    public void CashSalesFromOrders_is_the_cash_channel_of_PaymentMethodTotals()
    {
        var order = CashOrder(75m, "9");
        Assert.Equal(Money.FromEgp(75m), OrderPaymentQueries.CashSalesFromOrders(new[] { order }));
    }
}
