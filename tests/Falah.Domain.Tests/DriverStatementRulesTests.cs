using Falah.Domain.Delivery;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>UI-REQ-001: the statement's totals are exactly the shown rows' sums, cancelled orders are
/// listed but are not sales, and the payment filter matches the order's own method.</summary>
public class DriverStatementRulesTests
{
    private static DriverStatementOrder Row(string number, string method, long food, long fee, bool settled = false,
        long collected = 0, long prepaid = 0, long toCollect = 0, bool cancelled = false, string status = "Delivered", long refunded = 0) =>
        new(number, number, DateTimeOffset.UtcNow, null, null, null, method, status, food, fee, food + fee, refunded,
            prepaid, toCollect, collected, settled, null, cancelled);

    [Fact]
    public void Totals_keep_food_fees_prepaid_collected_and_settled_apart_and_skip_cancelled_rows()
    {
        var totals = DriverStatementRules.Totals(new[]
        {
            Row("#1", "cash", 10_000, 1_500, settled: true, collected: 11_500, toCollect: 11_500),
            Row("#2", "card", 8_000, 1_500, prepaid: 9_500),
            Row("#3", "cash", 6_000, 1_500, collected: 7_500, toCollect: 7_500, refunded: 1_000),
            Row("#4", "cash", 4_000, 1_500, cancelled: true, status: "Failed"),
        });

        Assert.Equal(3, totals.InvoiceCount);
        Assert.Equal(24_000, totals.FoodSalesPiastres);
        Assert.Equal(4_500, totals.DeliveryFeesPiastres);
        Assert.Equal(28_500, totals.OrderTotalPiastres);
        Assert.Equal(9_500, totals.PrepaidPiastres);
        Assert.Equal(19_000, totals.CashToCollectPiastres);
        Assert.Equal(19_000, totals.CollectedPiastres);
        Assert.Equal(11_500, totals.CollectedSettledPiastres);
        Assert.Equal(7_500, totals.CollectedUnsettledPiastres);
        Assert.Equal(1_000, totals.RefundedPiastres);
        Assert.Equal(1, totals.CancelledCount);
        Assert.Equal(1, totals.FailedOrReturnedCount);     // the failure is visible though the order was voided
    }

    [Theory]
    [InlineData("cash", null, true)]
    [InlineData("cash", "all", true)]
    [InlineData("cash", "cash", true)]
    [InlineData("card", "cash", false)]
    [InlineData("instapay", "INSTAPAY", true)]
    public void The_payment_filter_matches_the_orders_own_method(string method, string? filter, bool expected) =>
        Assert.Equal(expected, DriverStatementRules.MatchesPaymentFilter(method, filter));
}
