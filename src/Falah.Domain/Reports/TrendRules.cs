namespace Falah.Domain.Reports;

using Falah.Domain.Common;

/// <summary>
/// Turning a figure into a direction.
///
/// A number on its own tells an owner nothing - 12,450 is neither good nor bad. What makes it a
/// decision is whether it is above or below the comparable period, and by how much.
///
/// Two rules decide what "comparable" means here, and both are deliberate:
///
/// Yesterday is the wrong comparison for a restaurant. Thursday against Wednesday compares two
/// different kinds of day and the percentage means nothing. The comparable period is the SAME span
/// one week earlier, so a Friday is judged against a Friday.
///
/// Growth from zero is undefined, not one hundred percent. A day that follows a closed day would
/// otherwise report a spectacular increase that describes nothing at all.
/// </summary>
public static class TrendRules
{
    /// <summary>The comparable earlier span: the same number of days, shifted back exactly one week,
    /// so each weekday lines up with itself.</summary>
    public static BusinessDateRange ComparablePeriod(BusinessDateRange current) =>
        new(current.From.AddDays(-7), current.To.AddDays(-7));

    /// <param name="Percent">Null when the earlier figure was zero - the change exists but has no
    /// meaningful ratio, and inventing one would be the loudest number on the screen.</param>
    public readonly record struct Change(long CurrentPiastres, long PreviousPiastres, long DeltaPiastres, double? Percent);

    public static Change Compare(Money current, Money previous)
    {
        var delta = current.Piastres - previous.Piastres;
        double? percent = previous.Piastres != 0
            ? Math.Round((double)delta / Math.Abs(previous.Piastres) * 100, 1)
            : null;
        return new Change(current.Piastres, previous.Piastres, delta, percent);
    }

    public static Change Compare(long currentPiastres, long previousPiastres) =>
        Compare(Money.FromPiastres(currentPiastres), Money.FromPiastres(previousPiastres));
}
