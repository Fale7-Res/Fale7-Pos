namespace Falah.Domain.Purchasing;

using Falah.Domain.Common;

/// <summary>
/// How long the restaurant has been carrying each unpaid supplier invoice.
///
/// Two questions are deliberately kept apart here, because conflating them is how a payables report
/// starts lying. "How old is this invoice" is always answerable - it has a receipt date. "Is it
/// overdue" is only answerable when someone agreed a due date; an invoice with no term is not
/// overdue, and is not on time either. It is reported in its own bucket rather than folded into
/// either answer.
///
/// The bucket boundaries are the ordinary accounting ones (current, 1-30, 31-60, 61-90, over 90).
/// They are a way of reading the same numbers, not a policy about when the restaurant should pay.
/// </summary>
public static class PayablesAging
{
    public enum Bucket { NotYetDue, Days1To30, Days31To60, Days61To90, Over90, NoDueDate }

    public static string Label(Bucket bucket) => bucket switch
    {
        Bucket.NotYetDue => "لم يحن موعدها",
        Bucket.Days1To30 => "متأخرة 1-30 يوم",
        Bucket.Days31To60 => "متأخرة 31-60 يوم",
        Bucket.Days61To90 => "متأخرة 61-90 يوم",
        Bucket.Over90 => "متأخرة أكثر من 90 يوم",
        _ => "بدون تاريخ استحقاق",
    };

    /// <summary>Days past the due date, or null when there is no due date to be past. Negative
    /// values are not returned: an invoice that is not yet due is zero days overdue, not "-6".</summary>
    public static int? DaysOverdue(DateOnly? dueDate, DateOnly asOf)
    {
        if (dueDate is null) return null;
        var days = asOf.DayNumber - dueDate.Value.DayNumber;
        return days <= 0 ? 0 : days;
    }

    public static Bucket Classify(DateOnly? dueDate, DateOnly asOf) => DaysOverdue(dueDate, asOf) switch
    {
        null => Bucket.NoDueDate,
        0 => Bucket.NotYetDue,
        <= 30 => Bucket.Days1To30,
        <= 60 => Bucket.Days31To60,
        <= 90 => Bucket.Days61To90,
        _ => Bucket.Over90,
    };

    /// <summary>How long the invoice has been on the books, whether or not it has a due date. This
    /// is what makes an untermed invoice visible at all instead of sitting outside every bucket.</summary>
    public static int AgeInDays(DateOnly receivedOn, DateOnly asOf) =>
        Math.Max(0, asOf.DayNumber - receivedOn.DayNumber);

    // A due date in the past is deliberately NOT rejected.
    //
    // This system stamps every invoice's receipt date as "now", so a rule saying "the due date must
    // not precede the invoice" collapses into "no past due date, ever" - and that blocks an entirely
    // ordinary case: entering an invoice a week late, whose agreed term has already expired. That
    // invoice really is overdue, and refusing to record its date would force the operator to leave
    // the field blank, losing a true fact to protect against a typo the aging report itself makes
    // loud (an invoice sitting in "متأخرة أكثر من 90 يوم" that nobody recognises).
    //
    // Deciding when a term is unreasonable is the restaurant's call, not this system's.
}
