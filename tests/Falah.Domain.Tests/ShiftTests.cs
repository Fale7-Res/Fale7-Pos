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
