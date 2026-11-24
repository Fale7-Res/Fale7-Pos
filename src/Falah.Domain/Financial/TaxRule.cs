namespace Falah.Domain.Financial;

using Falah.Domain.Common;

public enum TaxMode { Inclusive, Exclusive }

public sealed record TaxRule(string Id, string Name, int RateBasisPoints, TaxMode Mode, bool Enabled);

public readonly record struct TaxCalculation(Money Taxable, Money Tax, Money Final);

/// <summary>Ported 1:1 from lib/tax-engine.mjs.</summary>
public static class TaxEngine
{
    // Owner decision 2026-11-23 (audit F-13): half a piastre of VAT rounds up, like every other amount.
    public static TaxCalculation Calculate(Money amount, TaxRule? rule)
    {
        if (rule is not { Enabled: true } r || r.RateBasisPoints <= 0)
            return new TaxCalculation(amount, Money.Zero, amount);

        if (r.Mode == TaxMode.Inclusive)
        {
            var tax = Money.FromPiastres((long)Math.Round(amount.Piastres * (decimal)r.RateBasisPoints / (10000m + r.RateBasisPoints), MidpointRounding.AwayFromZero));
            return new TaxCalculation(amount - tax, tax, amount);
        }
        else
        {
            var tax = Money.FromPiastres((long)Math.Round(amount.Piastres * (decimal)r.RateBasisPoints / 10000m, MidpointRounding.AwayFromZero));
            return new TaxCalculation(amount, tax, amount + tax);
        }
    }
}
