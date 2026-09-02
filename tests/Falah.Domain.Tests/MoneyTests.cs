using Falah.Domain.Common;
using Xunit;

namespace Falah.Domain.Tests;

public class MoneyTests
{
    [Theory]
    [InlineData("45.50", 4550)]
    [InlineData("0.01", 1)]
    [InlineData("100", 10000)]
    [InlineData("0.10", 10)]
    public void FromEgp_converts_to_exact_piastres_no_floating_point_drift(string egpText, long expectedPiastres)
    {
        var money = Money.FromEgp(decimal.Parse(egpText));
        Assert.Equal(expectedPiastres, money.Piastres);
    }

    [Fact]
    public void Adding_classic_floating_point_trap_amounts_is_exact()
    {
        // 0.1 + 0.2 famously != 0.3 in double/float arithmetic. In piastres it's plain integers.
        var a = Money.FromEgp(0.10m);
        var b = Money.FromEgp(0.20m);
        Assert.Equal(Money.FromEgp(0.30m), a + b);
        Assert.Equal(30, (a + b).Piastres);
    }

    [Fact]
    public void Repeated_addition_never_drifts()
    {
        var total = Money.Zero;
        for (var i = 0; i < 10_000; i++) total += Money.FromEgp(0.01m);
        Assert.Equal(Money.FromEgp(100m), total);
    }

    [Fact]
    public void Subtraction_can_go_negative_for_difference_reporting()
    {
        var difference = Money.FromEgp(50m) - Money.FromEgp(75m);
        Assert.True(difference.IsNegative);
        Assert.Equal(-2500, difference.Piastres);
        Assert.Equal(2500, difference.Abs().Piastres);
    }

    [Fact]
    public void Ordering_operators_compare_by_piastres()
    {
        Assert.True(Money.FromEgp(10m) < Money.FromEgp(10.01m));
        Assert.True(Money.FromEgp(10.01m) > Money.FromEgp(10m));
        Assert.True(Money.FromEgp(10m) >= Money.FromEgp(10m));
    }
}
