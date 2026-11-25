using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Owner decision 2026-11-25 (audit G2): a walk-in takeaway with grill items was "preparing" from the moment it
/// was paid and nothing ever completed it - there is no grill screen - so the order list showed yesterday's takeaways as
/// "قيد التحضير" for ever. A takeaway is complete when it is paid; deliveries and pickups keep their own end.</summary>
public class OrderInitialStatusTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 12, 0, 0, TimeSpan.Zero);

    private static Order Create(OrderType type, bool grill)
    {
        var items = new[] { new OrderItem("i1", "p1", null, "كبدة", null, Money.FromEgp(45m), Money.FromEgp(45m), 1, Money.FromEgp(45m), grill, null, null, null) };
        var plan = CheckoutRules.BuildPlan(type, PaymentMethod.Cash, items, Money.Zero, null, Money.Zero, Money.Zero, Money.FromEgp(45m), null, null);
        return Order.Create(plan, "ord-1", "#0128", 128, "cashier-1", "shift-1", items, "req-1", "day-1", Now);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_paid_takeaway_is_complete_whether_or_not_it_has_grill_items(bool grill) =>
        Assert.Equal(OrderStatus.Completed, Create(OrderType.Takeaway, grill).Status);

    [Fact]
    public void A_pickup_stays_in_preparation_until_it_is_handed_over() =>
        Assert.Equal(OrderStatus.Preparing, Create(OrderType.Pickup, grill: false).Status);
}
