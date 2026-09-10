namespace Falah.Domain.Tests;

using Falah.Domain.Common;
using Falah.Domain.Reports;
using Xunit;

/// <summary>
/// Gross profit is the number an owner will price and staff from, so the tests here are mostly about
/// the ways it could lie: incomplete cost coverage overstating profit, and percentages of nothing
/// reading as excellent performance.
/// </summary>
public class ProfitabilityRulesTests
{
    private static Money P(long piastres) => Money.FromPiastres(piastres);

    [Fact]
    public void Gross_profit_is_net_sales_less_food_cost()
    {
        var result = ProfitabilityRules.ComputeGrossProfit(P(100_000), P(32_000), soldLineCount: 10, knownLineCount: 10);

        Assert.Equal(68_000, result.GrossProfit!.Value.Piastres);
        Assert.Equal(32d, result.FoodCostPercent);
        Assert.Equal(68d, result.GrossProfitPercent);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void Incomplete_cost_coverage_is_reported_not_hidden()
    {
        // Three of ten sold lines had no known ingredient cost. The food cost is therefore an
        // understatement and the profit an overstatement - the caller must be able to see that.
        var result = ProfitabilityRules.ComputeGrossProfit(P(100_000), P(20_000), soldLineCount: 10, knownLineCount: 7);

        Assert.False(result.IsComplete);
        Assert.Equal(10, result.SoldLineCount);
        Assert.Equal(7, result.KnownLineCount);
    }

    [Fact]
    public void A_day_with_no_sales_has_no_percentage_rather_than_zero_percent()
    {
        // 0% food cost would read as a perfect day; the truth is that the ratio does not exist.
        var result = ProfitabilityRules.ComputeGrossProfit(Money.Zero, Money.Zero, 0, 0);

        Assert.Null(result.FoodCostPercent);
        Assert.Null(result.GrossProfitPercent);
        Assert.Equal(0, result.GrossProfit!.Value.Piastres);
    }

    [Fact]
    public void A_period_with_no_sold_lines_still_counts_as_complete()
    {
        var result = ProfitabilityRules.ComputeGrossProfit(Money.Zero, Money.Zero, 0, 0);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void Food_cost_above_sales_produces_a_negative_gross_profit_not_a_clamped_zero()
    {
        var result = ProfitabilityRules.ComputeGrossProfit(P(10_000), P(14_000), 5, 5);

        Assert.Equal(-4_000, result.GrossProfit!.Value.Piastres);
        Assert.Equal(140d, result.FoodCostPercent);
        Assert.Equal(-40d, result.GrossProfitPercent);
    }

    [Fact]
    public void Operating_margin_subtracts_food_expenses_and_wages()
    {
        var result = ProfitabilityRules.ComputeOperatingMargin(
            netSales: P(200_000), knownFoodCost: P(60_000), expenses: P(25_000), wages: P(40_000),
            soldLineCount: 20, knownLineCount: 20, wagePostingCount: 4);

        Assert.Equal(75_000, result.OperatingResult!.Value.Piastres);
        Assert.Equal(30d, result.FoodCostPercent);
        Assert.Equal(20d, result.WagePercent);
        Assert.Equal(12.5d, result.ExpensePercent);
        Assert.Equal(37.5d, result.OperatingPercent);
        Assert.True(result.IsComplete);
    }

    [Fact]
    public void Incomplete_coverage_carries_up_into_the_operating_margin()
    {
        var result = ProfitabilityRules.ComputeOperatingMargin(
            netSales: P(200_000), knownFoodCost: P(60_000), expenses: P(25_000), wages: P(40_000),
            soldLineCount: 20, knownLineCount: 19, wagePostingCount: 4);

        Assert.False(result.IsComplete);
    }

    [Fact]
    public void Operating_margin_can_be_negative()
    {
        var result = ProfitabilityRules.ComputeOperatingMargin(
            netSales: P(50_000), knownFoodCost: P(20_000), expenses: P(20_000), wages: P(30_000), 5, 5, 3);

        Assert.Equal(-20_000, result.OperatingResult!.Value.Piastres);
        Assert.Equal(-40d, result.OperatingPercent);
    }

    [Fact]
    public void Sales_with_no_known_cost_at_all_report_no_profit_rather_than_a_hundred_percent_margin()
    {
        // Caught by running the dashboard on real data: two sold lines, neither priced, and the
        // report cheerfully announced 0% food cost and a 100% margin. Zero cost is not a cautious
        // guess about a restaurant - it is the most flattering lie available.
        var result = ProfitabilityRules.ComputeGrossProfit(P(130_500), Money.Zero, soldLineCount: 2, knownLineCount: 0);

        Assert.Null(result.KnownFoodCost);
        Assert.Null(result.GrossProfit);
        Assert.Null(result.FoodCostPercent);
        Assert.Null(result.GrossProfitPercent);
        Assert.False(result.IsComplete);
        Assert.Equal(130_500, result.NetSales.Piastres);
    }

    [Fact]
    public void One_known_line_out_of_many_is_enough_to_report_a_bounded_figure()
    {
        // Partial knowledge is still knowledge, as long as it is labelled incomplete.
        var result = ProfitabilityRules.ComputeGrossProfit(P(100_000), P(9_000), soldLineCount: 10, knownLineCount: 1);

        Assert.NotNull(result.GrossProfit);
        Assert.False(result.IsComplete);
    }

    [Fact]
    public void An_unknown_food_cost_makes_the_operating_result_unknown_but_keeps_wages_and_expenses_visible()
    {
        var result = ProfitabilityRules.ComputeOperatingMargin(
            netSales: P(100_000), knownFoodCost: Money.Zero, expenses: P(9_500), wages: P(20_000),
            soldLineCount: 4, knownLineCount: 0, wagePostingCount: 2);

        Assert.Null(result.OperatingResult);
        Assert.Null(result.OperatingPercent);
        Assert.Null(result.KnownFoodCost);
        // These are genuinely known and stay reported.
        Assert.Equal(9_500, result.Expenses.Piastres);
        Assert.Equal(20_000, result.Wages.Piastres);
        Assert.Equal(20d, result.WagePercent);
        Assert.Equal(9.5d, result.ExpensePercent);
    }

    [Fact]
    public void A_period_with_no_wage_postings_reports_an_unknown_wage_ratio_not_zero_percent()
    {
        // The owner dashboard showed "نسبة الأجور 0%" for a day nobody had posted any labour on.
        // Arithmetically 0/75000 is zero; as an answer to "what is our labour ratio" it is a claim
        // that labour cost nothing, which is a different statement from nobody having recorded any.
        var result = ProfitabilityRules.ComputeOperatingMargin(
            netSales: P(75_000), knownFoodCost: P(20_000), expenses: Money.Zero, wages: Money.Zero,
            soldLineCount: 10, knownLineCount: 10, wagePostingCount: 0);

        Assert.Null(result.WagePercent);
        // Everything else it does know stays reported.
        Assert.Equal(0, result.Wages.Piastres);
        Assert.Equal(55_000, result.OperatingResult!.Value.Piastres);
    }

    [Fact]
    public void Wages_that_were_posted_and_summed_to_zero_still_report_a_ratio()
    {
        // The distinction is whether anything was RECORDED, not whether it added up to zero.
        var result = ProfitabilityRules.ComputeOperatingMargin(
            netSales: P(75_000), knownFoodCost: P(20_000), expenses: Money.Zero, wages: Money.Zero,
            soldLineCount: 10, knownLineCount: 10, wagePostingCount: 3);

        Assert.Equal(0d, result.WagePercent);
    }
}
