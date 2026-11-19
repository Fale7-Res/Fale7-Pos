namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.Delivery;
using Falah.Domain.Orders;
using Xunit;

/// <summary>AUD-01: the delivery charge is the zone's configured fee, decided by the server.</summary>
public class DeliveryFeeAuthorityTests
{
    private static readonly DeliveryRules.DeliveryServiceTerms ZoneAt15 = new(Active: true, Fee: Money.FromPiastres(1500));

    [Fact]
    public void A_delivery_order_is_charged_the_zones_fee_when_the_station_sent_the_same_fee() =>
        Assert.Equal(Money.FromPiastres(1500), DeliveryRules.ResolveDeliveryFee(OrderType.Delivery, Money.FromPiastres(1500), ZoneAt15));

    [Theory]
    [InlineData(0)]
    [InlineData(1000)]
    [InlineData(30000)]
    public void A_fee_that_is_not_the_zones_is_refused(long sent)
    {
        var ex = Assert.Throws<DomainException>(() => DeliveryRules.ResolveDeliveryFee(OrderType.Delivery, Money.FromPiastres(sent), ZoneAt15));
        Assert.Contains("15", ex.Message);
    }

    [Fact]
    public void A_zone_with_no_fee_configured_charges_nothing() =>
        Assert.Equal(Money.Zero, DeliveryRules.ResolveDeliveryFee(OrderType.Delivery, Money.Zero, new(true, Money.Zero)));

    [Fact]
    public void An_unknown_zone_is_refused() =>
        Assert.Throws<DomainException>(() => DeliveryRules.ResolveDeliveryFee(OrderType.Delivery, Money.Zero, null));

    [Fact]
    public void A_retired_zone_is_refused_even_with_the_right_fee() =>
        Assert.Throws<DomainException>(() => DeliveryRules.ResolveDeliveryFee(OrderType.Delivery, Money.FromPiastres(1500), ZoneAt15 with { Active = false }));

    [Theory]
    [InlineData(OrderType.Takeaway)]
    [InlineData(OrderType.Pickup)]
    public void Only_a_delivery_order_carries_a_fee(OrderType type)
    {
        Assert.Equal(Money.Zero, DeliveryRules.ResolveDeliveryFee(type, Money.Zero, null));
        Assert.Throws<DomainException>(() => DeliveryRules.ResolveDeliveryFee(type, Money.FromPiastres(1000), null));
    }
}
