namespace Falah.Domain.Reports;

using Falah.Domain.Common;

/// <summary>
/// What the restaurant actually kept, and what it spent to keep it.
///
/// Every figure here is a subtraction of numbers this system already produces - net sales from
/// SalesRules, food cost from CostingRules, wages from the payroll/ledger. Nothing is estimated,
/// allocated or smoothed.
///
/// The one thing that needs care is incomplete cost coverage. When some sold line has no known
/// ingredient cost, the food-cost total is an UNDERSTATEMENT, so gross profit computed from it is an
/// OVERSTATEMENT - the most dangerous direction for a number an owner makes decisions from. Rather
/// than hide that behind a confident figure, the result carries the coverage with it and says
/// plainly whether it is complete. It is never silently treated as zero cost, which is the same rule
/// CostingRules already applies one level down.
/// </summary>
public static class ProfitabilityRules
{
    /// <param name="KnownFoodCost">Null when NOT ONE sold line had a known cost. Zero would read as
    /// "this food was free", which is the most flattering possible lie about a restaurant.</param>
    /// <param name="IsComplete">False when any sold line had no known cost. The profit figure is
    /// then an upper bound, not a measurement.</param>
    public readonly record struct GrossProfitResult(
        Money NetSales,
        Money? KnownFoodCost,
        Money? GrossProfit,
        double? FoodCostPercent,
        double? GrossProfitPercent,
        int SoldLineCount,
        int KnownLineCount,
        bool IsComplete);

    public static GrossProfitResult ComputeGrossProfit(
        Money netSales, Money knownFoodCost, int soldLineCount, int knownLineCount)
    {
        var isComplete = soldLineCount == 0 || knownLineCount >= soldLineCount;

        // Nothing at all is known about what these sales cost. Reporting zero cost - and therefore a
        // hundred percent margin - is not a conservative approximation, it is the opposite of one.
        // The whole figure is withheld until at least one line can be priced.
        if (soldLineCount > 0 && knownLineCount == 0)
            return new GrossProfitResult(netSales, null, null, null, null, soldLineCount, 0, false);

        var grossProfit = netSales - knownFoodCost;

        // Percentages of a non-positive sales figure are meaningless, not zero: a day with no sales
        // has no food-cost percentage at all, and printing 0% would read as "excellent".
        double? foodCostPercent = null;
        double? grossProfitPercent = null;
        if (netSales.Piastres > 0)
        {
            foodCostPercent = Math.Round((double)knownFoodCost.Piastres / netSales.Piastres * 100, 2);
            grossProfitPercent = Math.Round((double)grossProfit.Piastres / netSales.Piastres * 100, 2);
        }

        return new GrossProfitResult(netSales, knownFoodCost, grossProfit,
            foodCostPercent, grossProfitPercent, soldLineCount, knownLineCount, isComplete);
    }

    /// <param name="IsComplete">False when any figure feeding it was itself incomplete - the same
    /// rule as above, carried up rather than reset.</param>
    public readonly record struct OperatingMarginResult(
        Money NetSales,
        Money? KnownFoodCost,
        Money Expenses,
        Money Wages,
        Money? OperatingResult,
        double? FoodCostPercent,
        double? WagePercent,
        double? ExpensePercent,
        double? OperatingPercent,
        bool IsComplete);

    /// <summary>Net sales less the three costs this system actually records: food, recorded
    /// expenses, and wages posted for the period. It is deliberately NOT called net profit - rent,
    /// depreciation, financing and anything else the restaurant does not enter here are absent, and
    /// naming it profit would invite an owner to treat it as the bottom line.</summary>
    public static OperatingMarginResult ComputeOperatingMargin(
        Money netSales, Money knownFoodCost, Money expenses, Money wages,
        int soldLineCount, int knownLineCount)
    {
        var isComplete = soldLineCount == 0 || knownLineCount >= soldLineCount;

        double? Percent(Money value) => netSales.Piastres > 0
            ? Math.Round((double)value.Piastres / netSales.Piastres * 100, 2)
            : null;

        // Food cost entirely unknown makes the operating result unknowable too - it would otherwise
        // be reported as if the food had cost nothing. Expenses and wages stay visible on their own,
        // because those ARE known and the owner should still see them.
        if (soldLineCount > 0 && knownLineCount == 0)
            return new OperatingMarginResult(netSales, null, expenses, wages, null,
                null, Percent(wages), Percent(expenses), null, false);

        var result = netSales - knownFoodCost - expenses - wages;
        return new OperatingMarginResult(netSales, knownFoodCost, expenses, wages, result,
            Percent(knownFoodCost), Percent(wages), Percent(expenses), Percent(result), isComplete);
    }
}
