using Falah.Domain.ControlCenter;
using Xunit;

namespace Falah.Domain.Tests;

public class ControlCenterRulesTests
{
    private static ControlCenterRules.BuildExceptionsInputs Clean() => new(
        ClosedShiftsWithDifference: Array.Empty<(string, long)>(),
        ShiftDiscrepancyThresholdPiastres: 5000,
        RefundedOrders: Array.Empty<(string, long)>(),
        Expenses: Array.Empty<(string, long)>(),
        UnsettledCustodyIds: Array.Empty<string>(),
        IncompleteJournalIds: Array.Empty<string>());

    [Fact]
    public void A_clean_day_produces_no_exceptions()
    {
        Assert.Empty(ControlCenterRules.BuildExceptions(Clean()));
    }

    [Fact]
    public void A_shift_shortage_below_threshold_is_only_a_warning()
    {
        var input = Clean() with { ClosedShiftsWithDifference = new[] { ("shift-1", -2000L) } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("warning", exception.Severity);
        Assert.Equal("shift-shift-1", exception.Id);
        Assert.Equal("financial", exception.Category);
    }

    [Fact]
    public void A_shift_shortage_at_or_beyond_threshold_is_critical()
    {
        var input = Clean() with { ClosedShiftsWithDifference = new[] { ("shift-1", -5000L) } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("critical", exception.Severity);
    }

    [Fact]
    public void A_shift_with_zero_difference_produces_no_exception()
    {
        var input = Clean() with { ClosedShiftsWithDifference = new[] { ("shift-1", 0L) } };
        Assert.Empty(ControlCenterRules.BuildExceptions(input));
    }

    [Fact]
    public void A_refund_exactly_at_the_large_refund_threshold_is_flagged()
    {
        var input = Clean() with { RefundedOrders = new[] { ("ord-1", ControlCenterRules.LargeRefundThresholdPiastres) } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("orders", exception.Category);
        Assert.Equal("refund-ord-1", exception.Id);
    }

    [Fact]
    public void A_refund_just_below_the_threshold_is_not_flagged()
    {
        var input = Clean() with { RefundedOrders = new[] { ("ord-1", ControlCenterRules.LargeRefundThresholdPiastres - 1) } };
        Assert.Empty(ControlCenterRules.BuildExceptions(input));
    }

    [Fact]
    public void A_large_expense_is_flagged()
    {
        var input = Clean() with { Expenses = new[] { ("exp-1", ControlCenterRules.LargeExpenseThresholdPiastres) } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("expenses", exception.Category);
    }

    [Fact]
    public void An_unsettled_custody_is_always_critical()
    {
        var input = Clean() with { UnsettledCustodyIds = new[] { "cust-1" } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("critical", exception.Severity);
        Assert.Equal("delivery", exception.Category);
    }

    [Fact]
    public void An_incomplete_journal_entry_is_always_critical()
    {
        var input = Clean() with { IncompleteJournalIds = new[] { "op-1" } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("critical", exception.Severity);
        Assert.Equal("system", exception.Category);
    }

    // AUD-13: a delivery order that was never even made ready is money and food nobody has dealt with. It does not
    // block the close (FIN-0051: such an order cannot be cancelled yet, so a blocker could hold the day forever), but
    // it must be on the list the manager reviews before closing, like a ready order with no driver.
    [Fact]
    public void A_delivery_order_never_made_ready_is_flagged_for_review()
    {
        var input = Clean() with { Deliveries = new[] { ("delivery-9", (string?)"#0150", "New", false, (DateTimeOffset?)null) } };
        var exception = Assert.Single(ControlCenterRules.BuildExceptions(input));
        Assert.Equal("delivery", exception.Category);
        Assert.Equal("warning", exception.Severity);
        Assert.Equal("delivery-new-delivery-9", exception.Id);
        Assert.Contains("#0150", exception.Message);
    }
}
