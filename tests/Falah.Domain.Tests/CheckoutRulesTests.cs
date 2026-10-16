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

    // ---- Requested pickup time ----

    private static readonly DateTimeOffset PickupNow = new(2026, 9, 12, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public void No_requested_time_is_the_normal_case_and_always_accepted()
    {
        // Most pickups are "on the way". A time nobody asked for must never be invented for them.
        CheckoutRules.AssertRequestedPickupTime(OrderType.Pickup, null, PickupNow);
        CheckoutRules.AssertRequestedPickupTime(OrderType.Takeaway, null, PickupNow);
        CheckoutRules.AssertRequestedPickupTime(OrderType.Delivery, null, PickupNow);
    }

    [Fact]
    public void A_time_in_the_past_is_refused()
    {
        Assert.Throws<DomainException>(() =>
            CheckoutRules.AssertRequestedPickupTime(OrderType.Pickup, PickupNow.AddHours(-1), PickupNow));
    }

    [Fact]
    public void A_time_that_went_stale_while_the_cashier_worked_is_still_accepted()
    {
        // The grace period exists so picking "now" and then finishing the order is not punished.
        CheckoutRules.AssertRequestedPickupTime(OrderType.Pickup, PickupNow.AddMinutes(-3), PickupNow);
    }

    [Fact]
    public void A_time_absurdly_far_ahead_is_refused_as_a_typo()
    {
        // Not a policy about how far ahead this restaurant takes orders - a guard against 2027
        // being typed where 2026 was meant.
        Assert.Throws<DomainException>(() =>
            CheckoutRules.AssertRequestedPickupTime(OrderType.Pickup, PickupNow.AddDays(400), PickupNow));
    }

    [Fact]
    public void A_time_later_today_or_next_week_is_accepted()
    {
        CheckoutRules.AssertRequestedPickupTime(OrderType.Pickup, PickupNow.AddHours(4), PickupNow);
        CheckoutRules.AssertRequestedPickupTime(OrderType.Pickup, PickupNow.AddDays(7), PickupNow);
    }

    [Fact]
    public void A_collection_time_belongs_to_a_pickup_and_to_nothing_else()
    {
        Assert.Throws<DomainException>(() =>
            CheckoutRules.AssertRequestedPickupTime(OrderType.Delivery, PickupNow.AddHours(2), PickupNow));
        Assert.Throws<DomainException>(() =>
            CheckoutRules.AssertRequestedPickupTime(OrderType.Takeaway, PickupNow.AddHours(2), PickupNow));
    }
}