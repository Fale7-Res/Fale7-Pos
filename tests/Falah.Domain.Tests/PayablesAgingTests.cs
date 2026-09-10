namespace Falah.Domain.Tests;

using Falah.Domain.Purchasing;
using Xunit;

/// <summary>
/// The failure this guards against is a payables report that reports invented deadlines. An invoice
/// nobody agreed a term for is not late; treating a missing due date as "due immediately" would put
/// most of a real restaurant's supplier invoices into an overdue bucket on day one, and an owner
/// who learns the overdue figure is noise stops reading it at all.
/// </summary>
public class PayablesAgingTests
{
    private static readonly DateOnly Today = new(2026, 9, 10);

    [Fact]
    public void An_invoice_with_no_due_date_is_never_overdue()
    {
        Assert.Null(PayablesAging.DaysOverdue(null, Today));
        Assert.Equal(PayablesAging.Bucket.NoDueDate, PayablesAging.Classify(null, Today));
    }

    [Fact]
    public void An_invoice_not_yet_due_is_zero_days_overdue_not_negative()
    {
        var due = Today.AddDays(6);
        Assert.Equal(0, PayablesAging.DaysOverdue(due, Today));
        Assert.Equal(PayablesAging.Bucket.NotYetDue, PayablesAging.Classify(due, Today));
    }

    [Fact]
    public void An_invoice_due_today_is_not_yet_late()
    {
        Assert.Equal(0, PayablesAging.DaysOverdue(Today, Today));
        Assert.Equal(PayablesAging.Bucket.NotYetDue, PayablesAging.Classify(Today, Today));
    }

    [Theory]
    [InlineData(1, PayablesAging.Bucket.Days1To30)]
    [InlineData(30, PayablesAging.Bucket.Days1To30)]
    [InlineData(31, PayablesAging.Bucket.Days31To60)]
    [InlineData(60, PayablesAging.Bucket.Days31To60)]
    [InlineData(61, PayablesAging.Bucket.Days61To90)]
    [InlineData(90, PayablesAging.Bucket.Days61To90)]
    [InlineData(91, PayablesAging.Bucket.Over90)]
    public void Buckets_are_inclusive_at_their_upper_bound(int daysLate, PayablesAging.Bucket expected)
    {
        var due = Today.AddDays(-daysLate);
        Assert.Equal(daysLate, PayablesAging.DaysOverdue(due, Today));
        Assert.Equal(expected, PayablesAging.Classify(due, Today));
    }

    [Fact]
    public void Age_is_answerable_even_when_overdue_is_not()
    {
        // The point of tracking age separately: an untermed invoice still has a receipt date, so
        // "we have been carrying this for 45 days" stays visible even though "late" does not apply.
        var received = Today.AddDays(-45);
        Assert.Equal(45, PayablesAging.AgeInDays(received, Today));
        Assert.Null(PayablesAging.DaysOverdue(null, Today));
    }

    [Fact]
    public void An_invoice_received_in_the_future_ages_zero_days_not_negative()
    {
        Assert.Equal(0, PayablesAging.AgeInDays(Today.AddDays(3), Today));
    }

    [Fact]
    public void An_invoice_entered_late_with_an_already_expired_term_is_simply_overdue()
    {
        // Receipt is always stamped "now", so an invoice keyed in a week after the goods arrived
        // carries a term that has already expired. That is a true statement about a real invoice,
        // not an input error - the report says overdue rather than refusing the date.
        var expiredTerm = Today.AddDays(-6);
        Assert.Equal(6, PayablesAging.DaysOverdue(expiredTerm, Today));
        Assert.Equal(PayablesAging.Bucket.Days1To30, PayablesAging.Classify(expiredTerm, Today));
    }

    [Fact]
    public void Every_bucket_has_a_label()
    {
        foreach (var bucket in Enum.GetValues<PayablesAging.Bucket>())
            Assert.False(string.IsNullOrWhiteSpace(PayablesAging.Label(bucket)));
    }
}
