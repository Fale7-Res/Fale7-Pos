using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Financial;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>
/// Owner-confirmed driver settlement (docs/UI_OWNER_REQUESTS.md, 2026-11-08): the driver keeps the FULL delivery
/// fee recorded on each order he successfully delivered. He settles with the cashier who receives his money.
///
/// What he owes is the cash he collected, minus the fees he earned, plus any balance carried from an earlier
/// partial settlement. A negative figure means the restaurant owes him. It is paid out explicitly or carried,
/// never clamped to zero.
/// </summary>
public class DriverSettlementRulesTests
{
    private static Money P(long piastres) => Money.FromPiastres(piastres);

    private static DeliveryFacts Delivery(string id, long fee, DeliveryStatus status = DeliveryStatus.Delivered,
        DateTimeOffset? delivered = null, DateTimeOffset? failed = null, DateTimeOffset? returned = null) =>
        new(id, $"o-{id}", $"#{id}", "drv", status, PaymentMethod.Cash, false, P(10_000), P(10_000), "shift", null, Money.Zero,
            DeliveredAtUtc: delivered ?? (status == DeliveryStatus.Delivered ? DateTimeOffset.UtcNow : null),
            FailedAtUtc: failed, ReturnedAtUtc: returned, DeliveryFee: P(fee));

    [Fact]
    public void The_driver_earns_each_completed_deliverys_recorded_fee_once_and_nothing_for_failed_or_returned_ones()
    {
        var fees = DriverSettlementRules.FeeEntitlement(new[]
        {
            Delivery("1", 1_500),
            Delivery("2", 2_500),                                                   // a farther area, its own recorded fee
            Delivery("3", 1_500, DeliveryStatus.Failed, failed: DateTimeOffset.UtcNow),
            Delivery("4", 1_500, DeliveryStatus.Returned, delivered: DateTimeOffset.UtcNow, returned: DateTimeOffset.UtcNow),
        });
        Assert.Equal(P(4_000), fees);
    }

    [Fact]
    public void Cash_collected_minus_fees_is_what_the_driver_hands_over()
    {
        var f = DriverSettlementRules.Calculate(carriedIn: Money.Zero, cashCollected: P(23_000), fees: P(3_000),
            received: P(20_000), paidToDriver: Money.Zero, keepRemainderOpen: false);
        Assert.Equal(P(20_000), f.Due);
        Assert.Equal(Money.Zero, f.Remaining);
        Assert.Equal(Money.Zero, f.Difference);
        Assert.Equal(Money.Zero, f.CarriedOut);
    }

    [Fact]
    public void A_partial_settlement_carries_the_rest_and_the_final_one_collects_it_exactly()
    {
        var first = DriverSettlementRules.Calculate(Money.Zero, P(23_000), P(3_000), P(12_000), Money.Zero, keepRemainderOpen: true);
        Assert.Equal(P(8_000), first.CarriedOut);
        Assert.Equal(Money.Zero, first.Difference);                     // not a shortage: it is still owed

        var final = DriverSettlementRules.Calculate(first.CarriedOut, Money.Zero, Money.Zero, P(8_000), Money.Zero, keepRemainderOpen: false);
        Assert.Equal(P(8_000), final.Due);
        Assert.Equal(Money.Zero, final.Difference);
        Assert.Equal(Money.Zero, final.CarriedOut);
    }

    [Fact]
    public void Prepaid_deliveries_leave_the_restaurant_owing_the_driver_which_is_paid_out_or_carried_never_clamped()
    {
        // Two prepaid deliveries (nothing to collect), fees 15 + 25.
        var owed = DriverSettlementRules.Calculate(Money.Zero, Money.Zero, P(4_000), Money.Zero, P(4_000), keepRemainderOpen: false);
        Assert.Equal(P(-4_000), owed.Due);
        Assert.True(owed.RestaurantOwesDriver);
        Assert.Equal(Money.Zero, owed.Remaining);

        var carried = DriverSettlementRules.Calculate(Money.Zero, Money.Zero, P(4_000), Money.Zero, Money.Zero, keepRemainderOpen: true);
        Assert.Equal(P(-4_000), carried.CarriedOut);                    // still owed to the driver, on record

        // Finalising without paying would silently drop what the restaurant owes: it is an explicit difference.
        var dropped = DriverSettlementRules.Calculate(Money.Zero, Money.Zero, P(4_000), Money.Zero, Money.Zero, keepRemainderOpen: false);
        Assert.Equal(P(4_000), dropped.Difference);
    }

    [Fact]
    public void A_short_final_settlement_is_a_recorded_shortage()
    {
        var f = DriverSettlementRules.Calculate(Money.Zero, P(23_000), P(3_000), P(19_500), Money.Zero, keepRemainderOpen: false);
        Assert.Equal(P(-500), f.Difference);
        Assert.Equal(Money.Zero, f.CarriedOut);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    public void Negative_amounts_are_refused(long received, long paid) =>
        Assert.Throws<DomainException>(() => DriverSettlementRules.Calculate(Money.Zero, P(1_000), Money.Zero, P(received), P(paid), false));

    [Fact]
    public void Paying_the_driver_more_than_the_restaurant_owes_or_receiving_and_paying_at_once_is_refused()
    {
        Assert.Throws<DomainException>(() => DriverSettlementRules.Calculate(Money.Zero, Money.Zero, P(4_000), Money.Zero, P(5_000), false));
        Assert.Throws<DomainException>(() => DriverSettlementRules.Calculate(Money.Zero, P(23_000), P(3_000), Money.Zero, P(100), false));
        Assert.Throws<DomainException>(() => DriverSettlementRules.Calculate(Money.Zero, Money.Zero, P(4_000), P(100), P(100), false));
    }
}
