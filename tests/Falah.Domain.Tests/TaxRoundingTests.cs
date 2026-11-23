using Falah.Domain.Common;
using Falah.Domain.Financial;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Owner decision 2026-11-23 (independent audit F-13): VAT rounds half a piastre UP, like every other
/// money amount in the system (Money.FromEgp, payroll). It used Math.Round's default (half to even), so 2.5
/// piastres of VAT became 2 while 3.5 became 4.</summary>
public class TaxRoundingTests
{
    [Theory]
    [InlineData(25, 3)]   // 2.5 -> 3 (half to even gave 2)
    [InlineData(35, 4)]   // 3.5 -> 4 either way
    [InlineData(24, 2)]   // 2.4 -> 2
    public void Exclusive_vat_rounds_half_a_piastre_up(long amountPiastres, long expectedTax)
    {
        var rule = new TaxRule("t", "ضريبة", 1000, TaxMode.Exclusive, true);
        var result = TaxEngine.Calculate(Money.FromPiastres(amountPiastres), rule);
        Assert.Equal(expectedTax, result.Tax.Piastres);
        Assert.Equal(amountPiastres + expectedTax, result.Final.Piastres);
    }

    [Fact]
    public void Inclusive_vat_rounds_half_a_piastre_up()
    {
        // 100% inclusive: half of 5 piastres is 2.5 -> 3.
        var rule = new TaxRule("t", "ضريبة", 10_000, TaxMode.Inclusive, true);
        var result = TaxEngine.Calculate(Money.FromPiastres(5), rule);
        Assert.Equal(3, result.Tax.Piastres);
        Assert.Equal(5, result.Final.Piastres);
    }
}
