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

    // ---- Delivery audit, owner decision 2026-11-24 (D3): a delivery is all cash with the driver or all paid ----

    [Fact]
    public void A_split_payment_is_refused_on_a_delivery_order()
    {
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(50m)), new PaymentSplit(PaymentMethod.Card, Money.FromEgp(50m)) };
        var ex = Assert.Throws<DomainException>(() => CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.Multi, new[] { Item(100m) },
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), splits, null));
        Assert.Contains("التوصيل", ex.Message);
    }

    /// <summary>Owner decision 2026-11-24 (workflow redesign): cash, InstaPay or wallet on a delivery - never card.</summary>
    [Fact]
    public void A_card_payment_is_refused_on_a_delivery_order()
    {
        var ex = Assert.Throws<DomainException>(() => CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.Card, new[] { Item(100m) },
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null));
        Assert.Contains("البطاقة", ex.Message);
        Assert.Equal(PaymentMethod.InstaPay, CheckoutRules.BuildPlan(OrderType.Delivery, PaymentMethod.InstaPay, new[] { Item(100m) },
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null).PaymentMethod);
    }

    // ---- UI-0025, owner decision 2026-11-22: a pickup is registered and prepared, the customer pays on collection ----

    [Fact]
    public void A_pay_on_collection_pickup_takes_no_money_and_is_not_paid()
    {
        var plan = CheckoutRules.BuildPlan(OrderType.Pickup, PaymentMethod.Cash, new[] { Item(100m) },
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.Zero, null, null, payOnCollection: true);

        Assert.True(plan.PayOnCollection);
        Assert.False(plan.IsPaid);
        Assert.True(plan.DefersAllMoney);
        Assert.Equal(Money.FromEgp(100m), plan.PayableTotal);
        Assert.Equal(Money.Zero, plan.AmountPaid);
        Assert.Null(plan.ElectronicLeg);
    }

    [Fact]
    public void Only_a_pickup_can_be_paid_on_collection()
    {
        var ex = Assert.Throws<DomainException>(() => CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, new[] { Item(100m) },
            Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.Zero, null, null, payOnCollection: true));
        Assert.Contains("الاستلام", ex.Message);
    }

    [Fact]
    public void Collecting_a_pickup_in_cash_is_paid_and_gives_change()
    {
        var plan = CheckoutRules.BuildCollectionPlan(Money.FromEgp(100m), PaymentMethod.Cash, Money.FromEgp(150m), null, null);
        Assert.True(plan.IsPaid);
        Assert.False(plan.DefersAllMoney);
        Assert.Equal(Money.FromEgp(50m), plan.ChangeDue);
    }

    [Fact]
    public void Collecting_a_pickup_by_instapay_is_a_pending_claim_like_any_transfer()
    {
        var plan = CheckoutRules.BuildCollectionPlan(Money.FromEgp(100m), PaymentMethod.InstaPay, Money.FromEgp(100m), null, null);
        Assert.False(plan.IsPaid);
        Assert.True(plan.DefersAllMoney);
        Assert.Equal(ElectronicPaymentStatus.Pending, plan.ElectronicLeg!.Status);
    }

    [Fact]
    public void Collecting_less_than_the_order_is_refused()
    {
        Assert.Throws<DomainException>(() => CheckoutRules.BuildCollectionPlan(Money.FromEgp(100m), PaymentMethod.Cash, Money.FromEgp(99m), null, null));
    }

    [Fact]
    public void Collecting_by_split_must_add_up_to_the_order()
    {
        var splits = new[] { new PaymentSplit(PaymentMethod.Cash, Money.FromEgp(40m)), new PaymentSplit(PaymentMethod.Card, Money.FromEgp(50m)) };
        Assert.Throws<DomainException>(() => CheckoutRules.BuildCollectionPlan(Money.FromEgp(100m), PaymentMethod.Multi, Money.FromEgp(90m), splits, null));
    }

    // Audit 2026-11-18: a discount above the items' value was accepted, the taxable base clamped to 0,
    // and the whole requested figure stored - so the discount reports showed money never given away.
    [Fact]
    public void A_discount_larger_than_the_items_is_refused()
    {
        var items = new[] { Item(100m) };

        var ex = Assert.Throws<DomainException>(() => CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
            Money.FromEgp(100.01m), taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(100m), null, null));
        Assert.Contains("الخصم", ex.Message);
    }

    [Fact]
    public void A_discount_of_exactly_the_items_is_accepted()
    {
        var items = new[] { Item(100m) };

        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
            Money.FromEgp(100m), taxRule: null, Money.Zero, Money.Zero, Money.Zero, null, null);

        Assert.Equal(Money.Zero, plan.PayableTotal);
    }

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

    /// <summary>Whole-project review 2026-11-17: a fully comped order collects nothing, so there is no
    /// transfer to wait for. It used to open a 0 ج "pending" InstaPay leg that held the kitchen ticket
    /// and blocked the day close until someone confirmed a payment that could never arrive.</summary>
    [Theory]
    [InlineData(PaymentMethod.InstaPay)]
    [InlineData(PaymentMethod.Wallet)]
    public void A_fully_comped_electronic_order_has_no_pending_transfer_and_is_paid(PaymentMethod method)
    {
        var items = new[] { Item(100m) };
        var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, method, items,
            Money.Zero, taxRule: null, Money.Zero, Money.FromEgp(100m), Money.Zero, null, "vodafone");

        Assert.Equal(Money.Zero, plan.PayableTotal);
        Assert.Null(plan.ElectronicLeg);
        Assert.True(plan.IsPaid);
        Assert.True(plan.DefersAllMoney, "nothing changed hands, so nothing posts");
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