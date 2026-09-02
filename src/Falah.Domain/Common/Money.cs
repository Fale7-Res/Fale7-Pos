namespace Falah.Domain.Common;

/// <summary>
/// Every amount in the system is an integer count of piastres (1 EGP = 100 piastres).
/// The current web app mixes EGP floats and integer piastres across its Core/Enterprise stores;
/// this type exists so the C# rewrite has exactly one money representation everywhere, with no
/// floating point in the storage or arithmetic path at all.
/// </summary>
public readonly record struct Money : IComparable<Money>
{
    public long Piastres { get; }

    private Money(long piastres) => Piastres = piastres;

    public static readonly Money Zero = new(0);

    public static Money FromPiastres(long piastres) => new(piastres);

    /// <summary>
    /// Only for boundary input (an operator typing "45.50" EGP into a field). Internal
    /// calculations must never round-trip through decimal/EGP - they stay in piastres.
    /// </summary>
    public static Money FromEgp(decimal egp)
    {
        var piastres = decimal.Round(egp * 100m, 0, MidpointRounding.AwayFromZero);
        return new Money((long)piastres);
    }

    public decimal Egp => Piastres / 100m;

    public static Money operator +(Money a, Money b) => new(a.Piastres + b.Piastres);
    public static Money operator -(Money a, Money b) => new(a.Piastres - b.Piastres);
    public static Money operator -(Money a) => new(-a.Piastres);
    public static Money operator *(Money a, long factor) => new(a.Piastres * factor);

    public static bool operator >(Money a, Money b) => a.Piastres > b.Piastres;
    public static bool operator <(Money a, Money b) => a.Piastres < b.Piastres;
    public static bool operator >=(Money a, Money b) => a.Piastres >= b.Piastres;
    public static bool operator <=(Money a, Money b) => a.Piastres <= b.Piastres;

    public int CompareTo(Money other) => Piastres.CompareTo(other.Piastres);

    public bool IsNegative => Piastres < 0;
    public bool IsZero => Piastres == 0;
    public bool IsPositive => Piastres > 0;

    public Money Abs() => new(Math.Abs(Piastres));

    public override string ToString() => Egp.ToString("0.00");
}
