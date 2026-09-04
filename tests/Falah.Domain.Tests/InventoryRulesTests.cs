using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Xunit;

namespace Falah.Domain.Tests;

public class InventoryRulesTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_count_adjustment_requires_a_reason()
    {
        Assert.Throws<DomainException>(() => InventoryRules.AssertCountAdjustmentInput(1000, ""));
    }

    [Fact]
    public void A_count_adjustment_requires_a_positive_quantity()
    {
        Assert.Throws<DomainException>(() => InventoryRules.AssertCountAdjustmentInput(0, "جرد شهري"));
        Assert.Throws<DomainException>(() => InventoryRules.AssertCountAdjustmentInput(-500, "جرد شهري"));
    }

    [Fact]
    public void A_valid_count_adjustment_does_not_throw()
    {
        InventoryRules.AssertCountAdjustmentInput(500, "جرد شهري");
    }

    [Fact]
    public void Waste_requires_one_of_the_three_recognized_loss_types()
    {
        Assert.Throws<DomainException>(() => InventoryRules.AssertWasteInput(1000, "خطأ تحضير", StockMovementType.Usage));
        InventoryRules.AssertWasteInput(1000, "خطأ تحضير", StockMovementType.Waste);
        InventoryRules.AssertWasteInput(1000, "تلف تخزين", StockMovementType.Spoilage);
        InventoryRules.AssertWasteInput(1000, "كسر أثناء المناولة", StockMovementType.Breakage);
    }

    [Fact]
    public void Waste_requires_a_reason_and_a_positive_quantity()
    {
        Assert.Throws<DomainException>(() => InventoryRules.AssertWasteInput(1000, "", StockMovementType.Waste));
        Assert.Throws<DomainException>(() => InventoryRules.AssertWasteInput(0, "سبب", StockMovementType.Waste));
    }

    [Fact]
    public void Changing_the_unit_of_an_item_with_existing_movements_is_rejected()
    {
        Assert.Throws<DomainException>(() => InventoryRules.AssertUnitChangeAllowed(hasExistingMovements: true, unitChanged: true));
    }

    [Fact]
    public void Changing_the_unit_of_an_item_with_no_movements_is_allowed()
    {
        InventoryRules.AssertUnitChangeAllowed(hasExistingMovements: false, unitChanged: true);
    }

    [Fact]
    public void Editing_other_fields_without_changing_the_unit_is_always_allowed_even_with_movements()
    {
        InventoryRules.AssertUnitChangeAllowed(hasExistingMovements: true, unitChanged: false);
    }

    [Fact]
    public void Stock_balance_nets_in_against_out_across_all_movement_types()
    {
        var movements = new[]
        {
            new StockMovementFacts("inv-1", StockMovementType.PurchaseReceipt, Money.FromEgp(10m), Now, 5000, StockDirection.In),
            new StockMovementFacts("inv-1", StockMovementType.Usage, null, Now, 2000, StockDirection.Out),
            new StockMovementFacts("inv-1", StockMovementType.Waste, Money.FromEgp(10m), Now, 500, StockDirection.Out),
            new StockMovementFacts("inv-1", StockMovementType.CountAdjustment, null, Now, 300, StockDirection.In),
            new StockMovementFacts("inv-2", StockMovementType.PurchaseReceipt, Money.FromEgp(1m), Now, 99999, StockDirection.In), // different item - excluded
        };

        Assert.Equal(5000 - 2000 - 500 + 300, InventoryRules.StockBalanceMilliUnits(movements, "inv-1"));
    }

    [Fact]
    public void Movement_totals_keep_purchases_usage_waste_and_count_adjustment_in_separate_buckets()
    {
        var movements = new[]
        {
            new StockMovementFacts("inv-1", StockMovementType.PurchaseReceipt, Money.FromEgp(10m), Now, 5000, StockDirection.In),
            new StockMovementFacts("inv-1", StockMovementType.ManualReceipt, null, Now, 1000, StockDirection.In),
            new StockMovementFacts("inv-1", StockMovementType.Usage, null, Now, 2000, StockDirection.Out),
            new StockMovementFacts("inv-1", StockMovementType.Spoilage, Money.FromEgp(10m), Now, 300, StockDirection.Out),
            new StockMovementFacts("inv-1", StockMovementType.Breakage, Money.FromEgp(10m), Now, 200, StockDirection.Out),
            new StockMovementFacts("inv-1", StockMovementType.CountAdjustment, null, Now, 400, StockDirection.Out),
        };

        var totals = InventoryRules.ActualMovementTotals(movements, "inv-1");

        Assert.Equal(6000, totals.PurchasedMilliUnits); // purchase_receipt + manual_receipt
        Assert.Equal(2000, totals.UsageMilliUnits);
        Assert.Equal(500, totals.WasteMilliUnits); // spoilage + breakage
        Assert.Equal(-400, totals.CountAdjustmentNetMilliUnits);
    }

    [Fact]
    public void Movement_totals_respect_the_optional_date_range()
    {
        var movements = new[]
        {
            new StockMovementFacts("inv-1", StockMovementType.Usage, null, Now.AddDays(-10), 1000, StockDirection.Out),
            new StockMovementFacts("inv-1", StockMovementType.Usage, null, Now, 2000, StockDirection.Out),
        };

        var totals = InventoryRules.ActualMovementTotals(movements, "inv-1", from: Now.AddDays(-1));
        Assert.Equal(2000, totals.UsageMilliUnits);
    }

    [Fact]
    public void Unexplained_variance_is_theoretical_minus_recorded_and_carries_count_adjustment_alongside_not_merged()
    {
        var actual = new InventoryRules.MovementTotals(PurchasedMilliUnits: 0, UsageMilliUnits: 1800, WasteMilliUnits: 100, CountAdjustmentNetMilliUnits: -50);
        var result = InventoryRules.UnexplainedVariance(theoreticalUsageMilliUnits: 2000, actual);

        Assert.Equal(2000, result.TheoreticalUsageMilliUnits);
        Assert.Equal(1900, result.RecordedConsumptionMilliUnits);
        Assert.Equal(100, result.VarianceMilliUnits);
        Assert.Equal(-50, result.CountAdjustmentNetMilliUnits);
    }
}
