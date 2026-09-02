using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Xunit;

namespace Falah.Domain.Tests;

public class OrderReconstituteTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    private static OrderItem Item(decimal price) =>
        new("i1", "prod-1", null, "برجر", null, Money.FromEgp(price), Money.FromEgp(price), 1, Money.FromEgp(price), false, null, null, null);

    private static Order FreshOrder(decimal total)
    {
        var items = new[] { Item(total) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(total), null, null);
        return Order.Create(plan, "ord-1", "#0128", 128, "cashier-1", "shift-1", items, "req-1", "day-1", Now);
    }

    private static Order Reconstitute(Order source) => Order.Reconstitute(
        source.Id, source.OrderNumber, source.SequenceNumber, source.CashierId, source.ShiftId, source.OrderType,
        source.CustomerName, source.CustomerPhone, source.DeliveryAddress, source.DeliveryArea, source.DeliveryFee,
        source.Items, source.Subtotal, source.CompAmount, source.CompReasonCode, source.CompAuthorizedBy, source.CompApprovalId,
        source.TaxAmount, source.Total, source.PaymentMethod, source.SplitPayments, source.AmountPaid, source.ChangeDue,
        source.CheckoutRequestId, source.BusinessDayId, source.CreatedAtUtc, source.Status, source.IsPaid,
        source.ElectronicPayment, source.RefundAmount, source.Refunds);

    [Fact]
    public void Reconstituted_order_preserves_every_persisted_field()
    {
        var original = FreshOrder(200m);
        var rebuilt = Reconstitute(original);

        Assert.Equal(original.Id, rebuilt.Id);
        Assert.Equal(original.Total, rebuilt.Total);
        Assert.Equal(original.Status, rebuilt.Status);
        Assert.Equal(original.IsPaid, rebuilt.IsPaid);
        Assert.Equal(original.RefundAmount, rebuilt.RefundAmount);
    }

    [Fact]
    public void A_reconstituted_order_enforces_the_exact_same_refund_invariants_as_a_freshly_created_one()
    {
        var original = FreshOrder(200m);
        var refund = new OrderRefund("r1", "op1", "k1", Money.FromEgp(250m), PaymentMethod.Cash, "tm1", Now, "c1", "m1", "reason", "shift-1", "shift-1");

        var rebuilt = Reconstitute(original);
        Assert.Throws<DomainException>(() => rebuilt.ApplyRefund(refund));
    }

    [Fact]
    public void A_reconstituted_order_that_already_has_partial_refunds_computes_the_correct_remaining_balance()
    {
        var original = FreshOrder(200m);
        original.ApplyRefund(new OrderRefund("r1", "op1", "k1", Money.FromEgp(50m), PaymentMethod.Cash, "tm1", Now, "c1", "m1", "reason", "shift-1", "shift-1"));

        var rebuilt = Reconstitute(original);
        Assert.Equal(Money.FromEgp(50m), rebuilt.RefundAmount);
        Assert.Single(rebuilt.Refunds);
        Assert.Equal(Money.FromEgp(150m), RefundRules.RefundableRemaining(rebuilt.Total, rebuilt.RefundAmount));

        rebuilt.ApplyRefund(new OrderRefund("r2", "op2", "k2", Money.FromEgp(150m), PaymentMethod.Cash, "tm2", Now, "c1", "m1", "reason", "shift-1", "shift-1"));
        Assert.Equal(OrderStatus.Refunded, rebuilt.Status);
    }
}
