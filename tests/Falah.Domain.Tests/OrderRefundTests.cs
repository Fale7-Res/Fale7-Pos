using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Shifts;
using Xunit;

namespace Falah.Domain.Tests;

public class OrderRefundTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem Item(decimal price) =>
        new("i1", "prod-1", null, "برجر", null, Money.FromEgp(price), Money.FromEgp(price), 1, Money.FromEgp(price), false, null, null, null);

    private static Order MakeOrder(decimal total, PaymentMethod method = PaymentMethod.Cash)
    {
        var items = new[] { Item(total) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, method, items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(total), null, null);
        return Order.Create(plan, "ord-1", "#0128", 128, "cashier-1", "shift-1", items, "req-1", "day-1", Now);
    }

    private static OrderRefund MakeRefund(Money amount) => new(
        "ref-1", "op-1", "refund-req-1", amount, PaymentMethod.Cash, "tm-1", Now, "cashier-1", "manager-1", "طلب العميل", "shift-1", "shift-1");

    [Fact]
    public void Partial_refund_keeps_the_order_completed_and_tracks_the_remaining_balance()
    {
        var order = MakeOrder(200m);
        order.ApplyRefund(MakeRefund(Money.FromEgp(50m)));

        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(Money.FromEgp(50m), order.RefundAmount);
        Assert.Equal(Money.FromEgp(150m), RefundRules.RefundableRemaining(order.Total, order.RefundAmount));
    }

    [Fact]
    public void Refunding_the_full_remaining_amount_marks_the_order_refunded()
    {
        var order = MakeOrder(200m);
        order.ApplyRefund(MakeRefund(Money.FromEgp(200m)));

        Assert.Equal(OrderStatus.Refunded, order.Status);
    }

    [Fact]
    public void Two_partial_refunds_that_exactly_exhaust_the_order_mark_it_refunded()
    {
        var order = MakeOrder(200m);
        order.ApplyRefund(MakeRefund(Money.FromEgp(120m)));
        order.ApplyRefund(MakeRefund(Money.FromEgp(80m)));

        Assert.Equal(OrderStatus.Refunded, order.Status);
        Assert.Equal(2, order.Refunds.Count);
    }

    [Fact]
    public void A_refund_larger_than_what_remains_is_rejected()
    {
        var order = MakeOrder(200m);
        order.ApplyRefund(MakeRefund(Money.FromEgp(150m)));

        Assert.Throws<DomainException>(() => order.ApplyRefund(MakeRefund(Money.FromEgp(100m))));
    }

    private static Order SplitOrder()
    {
        var items = new[] { Item(200m) };
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(100m)), new PaymentSplit(PaymentMethod.Card, Money.FromEgp(100m)) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Multi, items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(200m), splits, null);
        return Order.Create(plan, "ord-2", "#0129", 129, "cashier-1", "shift-1", items, "req-2", "day-1", Now);
    }

    private static OrderRefund RefundThrough(PaymentMethod method, Money amount) => MakeRefund(amount) with { PaymentMethod = method };

    // FIN-0005, owner decision 2026-11-22: a split-paid order is refunded through ONE of the methods it
    // was paid with, chosen at the refund - up to what remains refundable on the order.
    [Fact]
    public void A_split_order_is_refunded_through_one_of_its_own_payment_methods()
    {
        var order = SplitOrder();
        order.ApplyRefund(RefundThrough(PaymentMethod.Card, Money.FromEgp(150m)));
        Assert.Equal(Money.FromEgp(150m), order.RefundAmount);
    }

    [Theory]
    [InlineData(PaymentMethod.InstaPay)]
    [InlineData(PaymentMethod.Multi)]
    public void A_split_order_is_never_refunded_through_a_method_it_was_not_paid_with(PaymentMethod method)
    {
        Assert.Throws<DomainException>(() => SplitOrder().ApplyRefund(RefundThrough(method, Money.FromEgp(50m))));
    }

    [Theory]
    [InlineData(PaymentMethod.InstaPay)]
    [InlineData(PaymentMethod.Wallet)]
    public void An_unconfirmed_transfer_cannot_be_refunded(PaymentMethod method)
    {
        var order = MakeOrder(200m, method);
        Assert.Throws<DomainException>(() => order.ApplyRefund(MakeRefund(Money.FromEgp(50m))));
        Assert.Empty(order.Refunds);
        Assert.Equal(Money.Zero, order.RefundAmount);
    }

    [Fact]
    public void Cash_with_a_driver_cannot_be_refunded_before_settlement()
    {
        var items = new[] { Item(200m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.Cash, items,
            Money.Zero, null, Money.Zero, Money.Zero, Money.Zero, null, null);
        var order = Order.Create(plan, "cod", "#1", 1, "cashier", "shift", items, "req", "day", Now);
        Assert.Throws<DomainException>(() => order.ApplyRefund(MakeRefund(Money.FromEgp(50m))));
        Assert.Empty(order.Refunds);
    }

    [Fact]
    public void A_confirmed_transfer_can_be_refunded()
    {
        var order = MakeOrder(200m, PaymentMethod.InstaPay);
        order.ConfirmElectronicPayment();
        order.ApplyRefund(MakeRefund(Money.FromEgp(50m)));
        Assert.Equal(Money.FromEgp(50m), order.RefundAmount);
    }

    [Fact]
    public void ClampRefundAmount_never_returns_more_than_what_remains()
    {
        var remaining = Money.FromEgp(30m);
        Assert.Equal(Money.FromEgp(30m), RefundRules.ClampRefundAmount(Money.FromEgp(500m), remaining));
    }

    [Fact]
    public void Cash_refund_against_a_closed_original_shift_requires_an_open_shift_to_post_against()
    {
        Assert.Throws<DomainException>(() =>
            RefundRules.ResolveOperationalShift("shift-old", ShiftStatus.Closed, activeShiftId: null, PaymentMethod.Cash));
    }

    [Fact]
    public void Cash_refund_from_a_closed_shift_posts_against_the_currently_open_shift_not_the_original()
    {
        var policy = RefundRules.ResolveOperationalShift("shift-old", ShiftStatus.Closed, "shift-new", PaymentMethod.Cash);
        Assert.True(policy.HistoricalImmutable);
        Assert.Equal("shift-new", policy.OperationalShiftId);
    }

    [Fact]
    public void Card_refund_from_a_closed_shift_does_not_require_an_open_shift()
    {
        var policy = RefundRules.ResolveOperationalShift("shift-old", ShiftStatus.Closed, activeShiftId: null, PaymentMethod.Card);
        Assert.Null(policy.OperationalShiftId);
    }

    [Fact]
    public void Refund_from_a_still_open_original_shift_posts_against_that_same_shift()
    {
        var policy = RefundRules.ResolveOperationalShift("shift-old", ShiftStatus.Open, "shift-old", PaymentMethod.Cash);
        Assert.False(policy.HistoricalImmutable);
        Assert.Equal("shift-old", policy.OperationalShiftId);
    }

    [Fact]
    public void Cannot_cancel_an_order_that_already_has_a_refund()
    {
        Assert.Throws<DomainException>(() => RefundRules.AssertOrderCanBeCancelled(Money.FromEgp(10m)));
    }

    [Fact]
    public void Settled_cash_delivery_can_be_refunded()
    {
        var items = new[] { Item(200m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.Cash, items,
            Money.Zero, null, Money.Zero, Money.Zero, Money.Zero, null, null);
        var order = Order.Create(plan, "cod", "#1", 1, "cashier", "shift", items, "req", "day", Now);
        order.ApplyRefund(MakeRefund(Money.FromEgp(50m)), cashOnDeliverySettled: true);
        Assert.Equal(Money.FromEgp(50m), order.RefundAmount);
    }

    // Owner decision 2026-11-18: from the large-refund threshold (500 ج) up, a refund needs a
    // manager's approval even from someone who holds orders_refund.
    [Theory]
    [InlineData(49_999, false)]
    [InlineData(50_000, true)]
    [InlineData(120_000, true)]
    public void A_refund_from_the_large_refund_threshold_up_needs_a_manager(long amountPiastres, bool needsManager)
    {
        Assert.Equal(needsManager, RefundRules.NeedsManagerApproval(Money.FromPiastres(amountPiastres)));
    }
}
