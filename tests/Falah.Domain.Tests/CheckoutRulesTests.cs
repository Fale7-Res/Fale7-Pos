using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Xunit;

namespace Falah.Domain.Tests;

public class CheckoutRulesTests
{
    private static OrderItem Item(decimal unitPrice, int qty = 1) =>
        new("i1", "prod-1", null, "شاورما فراخ", null, Money.FromEgp(unitPrice), Money.FromEgp(unitPrice), qty,
            Money.FromEgp(unitPrice * qty), false, null, null, null);

    [Fact]
    public void Plain_cash_order_is_paid_immediately_and_defers_nothing()
    {
        var items = new[] { Item(100m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null);

        Assert.Equal(Money.FromEgp(100m), plan.PayableTotal);
        Assert.True(plan.IsPaid);
        Assert.False(plan.DefersAllMoney);
        Assert.Null(plan.ElectronicLeg);
    }

    [Fact]
    public void Cash_on_delivery_defers_the_money_and_is_not_marked_paid()
    {
        var items = new[] { Item(100m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.Cash, items,
            Money.Zero, taxRule: null, Money.FromEgp(20m), Money.Zero, Money.Zero, null, null);

        Assert.True(plan.CashOnDelivery);
        Assert.True(plan.DefersAllMoney);
        Assert.False(plan.IsPaid);
    }

    [Fact]
    public void A_fully_comped_delivery_is_not_treated_as_cash_on_delivery()
    {
        var items = new[] { Item(100m) };
        // Order value 100 + delivery fee 20 = 120, fully absorbed by a 120 comp.
        var plan = CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.Cash, items,
            Money.Zero, taxRule: null, Money.FromEgp(20m), Money.FromEgp(120m), Money.Zero, null, null);

        Assert.Equal(Money.Zero, plan.PayableTotal);
        Assert.False(plan.CashOnDelivery, "nothing to collect on delivery is not cash-on-delivery custody");
        Assert.True(plan.DefersAllMoney, "a fully comped order posts no Treasury movement at all");
    }

    [Fact]
    public void InstaPay_payment_is_pending_and_defers_money_until_confirmed()
    {
        var items = new[] { Item(100m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.InstaPay, items,
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null);

        Assert.NotNull(plan.ElectronicLeg);
        Assert.Equal(ElectronicPaymentStatus.Pending, plan.ElectronicLeg!.Status);
        Assert.True(plan.DefersAllMoney);
        Assert.False(plan.IsPaid);
    }

    [Fact]
    public void Comp_reduces_payable_total_but_never_below_zero()
    {
        var items = new[] { Item(50m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
            Money.Zero, taxRule: null, Money.Zero, Money.FromEgp(80m), Money.Zero, null, null);

        Assert.Equal(Money.Zero, plan.PayableTotal);
    }

    [Fact]
    public void Insufficient_cash_payment_is_rejected()
    {
        var items = new[] { Item(100m) };
        Assert.Throws<DomainException>(() =>
            CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
                Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(50m), null, null));
    }

    [Fact]
    public void Multi_payment_without_split_details_is_rejected()
    {
        var items = new[] { Item(100m) };
        Assert.Throws<DomainException>(() =>
            CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Multi, items,
                Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null));
    }

    [Fact]
    public void Multi_payment_split_must_sum_to_exactly_the_payable_total()
    {
        var items = new[] { Item(200m) };
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(150m)), new PaymentSplit(PaymentMethod.Card, Money.FromEgp(100m)) };
        Assert.Throws<DomainException>(() =>
            CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Multi, items,
                Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(250m), splits, null));
    }

    [Fact]
    public void Multi_payment_split_summing_exactly_succeeds_and_selects_the_electronic_leg()
    {
        var items = new[] { Item(200m) };
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(100m)), new PaymentSplit(PaymentMethod.InstaPay, Money.FromEgp(100m)) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Multi, items,
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(200m), splits, null);

        Assert.NotNull(plan.ElectronicLeg);
        Assert.Equal(Money.FromEgp(100m), plan.ElectronicLeg!.Amount);
        // A multi-payment order does NOT defer everything just because one leg is electronic - the
        // cash leg still posts immediately; only that one leg is deferred.
        Assert.False(plan.DefersAllMoney);
    }

    [Fact]
    public void Discount_reduces_the_taxable_base_before_tax_is_applied()
    {
        var items = new[] { Item(100m) };
        var taxRule = new TaxRule("tax-1", "ضريبة", 1400, TaxMode.Exclusive, true); // 14%
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
            Money.FromEgp(20m), taxRule, Money.Zero, Money.Zero, Money.FromEgp(91.2m), null, null);

        // (100 - 20) * 1.14 = 91.2
        Assert.Equal(Money.FromEgp(91.2m), plan.PayableTotal);
    }
}
