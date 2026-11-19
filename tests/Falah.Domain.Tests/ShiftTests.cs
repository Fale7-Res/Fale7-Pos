using Falah.Domain.Common;
using Falah.Domain.Shifts;
using Xunit;

namespace Falah.Domain.Tests;

public class ShiftTests
{
    private static readonly DateTimeOffset Now = new(2026, 3, 5, 10, 0, 0, TimeSpan.Zero);
    private static readonly Money Threshold = Money.FromEgp(20m);

    [Fact]
    public void Opening_a_shift_with_negative_opening_cash_is_rejected()
    {
        Assert.Throws<DomainException>(() =>
            Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(-10m), Now));
    }

    [Fact]
    public void Balanced_drawer_closes_without_any_manager_approval()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        var result = shift.PrepareBlindClose(Money.FromEgp(1200m), Money.FromEgp(1200m), Threshold);

        Assert.True(result.IsBalanced);
        Assert.False(result.RequiresManagerApproval);

        shift.Close("user-1", result, reason: null, authorizedBy: null, Now.AddHours(8));

        Assert.Equal(ShiftStatus.Closed, shift.Status);
        Assert.Equal(Money.Zero, shift.CashDifference);
    }

    [Fact]
    public void Small_difference_within_threshold_closes_without_approval()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        // 10 EGP short, threshold is 20 EGP.
        var result = shift.PrepareBlindClose(Money.FromEgp(1190m), Money.FromEgp(1200m), Threshold);

        Assert.False(result.RequiresManagerApproval);
        shift.Close("user-1", result, reason: null, authorizedBy: null, Now.AddHours(8));
        Assert.Equal(ShiftStatus.Closed, shift.Status);
    }

    // Owner decision 2026-11-18: once the preview has shown the expected cash, a count that differs
    // from the first one is a re-count and needs a manager, however small the difference left.
    [Fact]
    public void A_count_that_differs_from_the_first_declared_count_needs_a_manager_even_when_it_balances()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);

        var result = shift.PrepareBlindClose(Money.FromEgp(1200m), Money.FromEgp(1200m), Threshold, firstDeclared: Money.FromEgp(1210m));

        Assert.True(result.IsBalanced);
        Assert.True(result.RequiresManagerApproval);
        Assert.True(result.IsRecount);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(1210)]
    public void The_first_count_or_the_same_count_again_is_not_a_recount(int? firstDeclaredEgp)
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        Money? first = firstDeclaredEgp is { } egp ? Money.FromEgp(egp) : null;

        var result = shift.PrepareBlindClose(Money.FromEgp(1210m), Money.FromEgp(1200m), Threshold, firstDeclared: first);

        Assert.False(result.IsRecount);
        Assert.False(result.RequiresManagerApproval); // 10 ج over, inside the 20 ج threshold
    }

    [Fact]
    public void Large_shortage_beyond_threshold_is_blocked_without_manager_approval()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        // 100 EGP short, threshold is 20 EGP.
        var result = shift.PrepareBlindClose(Money.FromEgp(1100m), Money.FromEgp(1200m), Threshold);

        Assert.True(result.RequiresManagerApproval);
        Assert.Throws<DomainException>(() =>
            shift.Close("user-1", result, reason: null, authorizedBy: null, Now.AddHours(8)));
    }

    [Fact]
    public void Large_shortage_closes_once_a_manager_name_and_reason_are_supplied()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        var result = shift.PrepareBlindClose(Money.FromEgp(1100m), Money.FromEgp(1200m), Threshold);

        shift.Close("user-1", result, reason: "فكة اتصرفت غلط", authorizedBy: "المدير أحمد", Now.AddHours(8));

        Assert.Equal(ShiftStatus.Closed, shift.Status);
        Assert.Equal("المدير أحمد", shift.DifferenceAuthorizedBy);
        Assert.Equal(Money.FromEgp(-100m), shift.CashDifference);
    }

    [Fact]
    public void A_large_overage_also_requires_approval_not_just_a_shortage()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        var result = shift.PrepareBlindClose(Money.FromEgp(1300m), Money.FromEgp(1200m), Threshold);

        Assert.True(result.RequiresManagerApproval);
        Assert.Throws<DomainException>(() =>
            shift.Close("user-1", result, reason: null, authorizedBy: null, Now.AddHours(8)));
    }

    [Fact]
    public void Negative_actual_cash_count_is_rejected()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        Assert.Throws<DomainException>(() =>
            shift.PrepareBlindClose(Money.FromEgp(-1m), Money.FromEgp(1200m), Threshold));
    }

    [Fact]
    public void An_already_closed_shift_cannot_be_closed_again()
    {
        var shift = Shift.Open("s1", "صباحية", "user-1", Money.FromEgp(500m), Now);
        var result = shift.PrepareBlindClose(Money.FromEgp(500m), Money.FromEgp(500m), Threshold);
        shift.Close("user-1", result, null, null, Now.AddHours(8));

        Assert.Throws<DomainException>(() =>
            shift.PrepareBlindClose(Money.FromEgp(500m), Money.FromEgp(500m), Threshold));
    }
}
