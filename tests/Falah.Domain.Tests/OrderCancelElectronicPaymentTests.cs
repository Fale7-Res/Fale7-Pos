using Falah.Domain.Common;
using Falah.Domain.Orders;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>
/// Ported from cancelPendingElectronicPayment in the reference app's lib/store.tsx, found during
/// the legacy audit - resolves what was previously an open TODO with concrete evidence rather than
/// invented behavior.
/// </summary>
public class OrderCancelElectronicPaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem Item(decimal price) =>
        new("i1", "prod-1", null, "شاورما فراخ", null, Falah.Domain.Common.Money.FromEgp(price), Money.FromEgp(price), 1, Money.FromEgp(price), false, null, null, null);

    private static Order InstaPayOrder()
    {
        var items = new[] { Item(150m) };
        var plan = Falah.Domain.Orders.CheckoutRules.BuildPlan(OrderType.Takeaway, Falah.Domain.Financial.PaymentMethod.InstaPay,
            items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(150m), null, null);
        return Order.Create(plan, "ord-1", "#0128", 128, "cashier-1", "shift-1", items, "req-1", "day-1", Now);
    }

    [Fact]
    public void Cancelling_a_pending_electronic_order_marks_the_whole_order_cancelled()
    {
        var order = InstaPayOrder();
        order.CancelElectronicPayment("لم يصل التحويل", "كاشير-1", Now.AddMinutes(5));

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.False(order.IsPaid);
        Assert.Equal(Falah.Domain.Orders.ElectronicPaymentStatus.Cancelled, order.ElectronicPayment!.Status);
        Assert.Equal("لم يصل التحويل", order.CancelReason);
        Assert.Equal("كاشير-1", order.CancelledBy);
    }

    [Fact]
    public void Cancelling_twice_is_idempotent_and_does_not_throw()
    {
        var order = InstaPayOrder();
        order.CancelElectronicPayment("سبب", "أحمد", Now);
        order.CancelElectronicPayment("سبب آخر", "شخص آخر", Now.AddMinutes(1));

        // The first cancellation's details stick - the second call is a no-op by design.
        Assert.Equal("سبب", order.CancelReason);
        Assert.Equal("أحمد", order.CancelledBy);
    }

    [Fact]
    public void A_confirmed_electronic_payment_cannot_be_cancelled_refund_must_be_used_instead()
    {
        var order = InstaPayOrder();
        order.ConfirmElectronicPayment();
        Assert.Throws<DomainException>(() => order.CancelElectronicPayment("سبب", "أحمد", Now));
    }

    [Fact]
    public void An_order_with_no_electronic_leg_cannot_be_cancelled_through_this_path()
    {
        var items = new[] { Item(100m) };
        var plan = Falah.Domain.Orders.CheckoutRules.BuildPlan(OrderType.Takeaway, Falah.Domain.Financial.PaymentMethod.Cash,
            items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null);
        var cashOrder = Order.Create(plan, "ord-2", "#0129", 129, "cashier-1", "shift-1", items, "req-2", "day-1", Now);

        Assert.Throws<DomainException>(() => cashOrder.CancelElectronicPayment("سبب", "أحمد", Now));
    }

    [Fact]
    public void An_empty_reason_is_rejected()
    {
        var order = InstaPayOrder();
        Assert.Throws<DomainException>(() => order.CancelElectronicPayment("   ", "أحمد", Now));
    }
}
