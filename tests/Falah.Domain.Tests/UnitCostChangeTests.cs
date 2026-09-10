namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Xunit;

/// <summary>
/// This system values an ingredient at its LAST purchase price, so one received invoice line
/// becomes the cost basis immediately - and with it every recipe using that ingredient, the
/// food-cost percentage and the gross margin the owner reads. During the acceptance audit a single
/// mistyped unit cost (1,500 instead of 15,000) moved a recipe from 2,250 to 225 and the day's food
/// cost from 6,750 to 675, with nothing anywhere recording that it had happened.
///
/// These rules describe the move so it can be recorded. They deliberately do not judge whether a
/// price is acceptable and do not block: prices move, pack sizes change, and what counts as a
/// reasonable cost is the restaurant's call, not this system's.
/// </summary>
public class UnitCostChangeTests
{
    private static Money P(long piastres) => Money.FromPiastres(piastres);

    [Fact]
    public void A_first_ever_purchase_establishes_the_basis_rather_than_moving_it()
    {
        var change = PurchasingRules.DescribeUnitCostChange(null, P(15_000));

        Assert.True(change.IsFirstEverPurchase);
        Assert.False(change.HasChanged);
        Assert.Null(change.PreviousUnitCost);
        Assert.Null(change.Factor);
        Assert.False(change.IsOrderOfMagnitude);
    }

    [Fact]
    public void Buying_at_the_same_price_is_not_a_change()
    {
        var change = PurchasingRules.DescribeUnitCostChange(P(15_000), P(15_000));

        Assert.False(change.HasChanged);
        Assert.False(change.IsOrderOfMagnitude);
        Assert.Equal(1d, change.Factor);
    }

    [Fact]
    public void An_ordinary_price_move_is_recorded_but_not_flagged()
    {
        // Suppliers raise prices. That is worth a line in the trail, not an alarm.
        var change = PurchasingRules.DescribeUnitCostChange(P(15_000), P(18_000));

        Assert.True(change.HasChanged);
        Assert.Equal(1.2d, change.Factor);
        Assert.False(change.IsOrderOfMagnitude);
    }

    [Fact]
    public void The_misplaced_decimal_that_started_this_is_flagged()
    {
        // The exact figures from the audit: 15,000 keyed as 1,500.
        var change = PurchasingRules.DescribeUnitCostChange(P(15_000), P(1_500));

        Assert.True(change.HasChanged);
        Assert.True(change.IsOrderOfMagnitude);
        Assert.Equal(0.1d, change.Factor);
        Assert.Equal(15_000, change.PreviousUnitCost!.Value.Piastres);
    }

    [Fact]
    public void A_decimal_slipped_the_other_way_is_flagged_too()
    {
        var change = PurchasingRules.DescribeUnitCostChange(P(1_500), P(15_000));

        Assert.True(change.IsOrderOfMagnitude);
        Assert.Equal(10d, change.Factor);
    }

    [Theory]
    [InlineData(15_000, 149_999, false)]  // just under tenfold
    [InlineData(15_000, 150_000, true)]   // exactly tenfold
    [InlineData(15_000, 1_501, false)]    // just over a tenth
    [InlineData(15_000, 1_500, true)]     // exactly a tenth
    public void The_flag_turns_on_at_a_factor_of_ten_in_either_direction(long previous, long updated, bool expected) =>
        Assert.Equal(expected, PurchasingRules.DescribeUnitCostChange(P(previous), P(updated)).IsOrderOfMagnitude);

    [Fact]
    public void A_previous_cost_of_zero_counts_as_no_basis_at_all()
    {
        // Zero is not a price anything was bought at - dividing by it would produce infinity, and
        // treating it as a real previous cost would report an infinite increase.
        var change = PurchasingRules.DescribeUnitCostChange(Money.Zero, P(15_000));

        Assert.True(change.IsFirstEverPurchase);
        Assert.Null(change.Factor);
    }
}
