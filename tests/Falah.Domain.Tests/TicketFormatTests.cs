using System.Globalization;
using System.Text.RegularExpressions;
using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Domain.Orders;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Domain.Tests;

/// <summary>Paper redesign (spec 2026-11-17, "Money on paper" / "Date/time on paper" / "Codes and
/// numbers inside Arabic text"): one formatting vocabulary for every printed paper.</summary>
public class TicketFormatTests
{
    private sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture, _ui;
        public CultureScope(string name)
        {
            (_culture, _ui) = (CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture);
            CultureInfo.CurrentCulture = CultureInfo.CurrentUICulture = new CultureInfo(name);
        }
        public void Dispose() { CultureInfo.CurrentCulture = _culture; CultureInfo.CurrentUICulture = _ui; }
    }

    [Fact]
    public void Amount_groups_thousands_and_keeps_latin_digits_under_arabic_culture()
    {
        using var _ = new CultureScope("ar-EG");
        Assert.Equal("12,500 ج", TicketFormat.Amount(Money.FromPiastres(1_250_000)));
        Assert.Equal("25.50 ج", TicketFormat.Amount(Money.FromPiastres(2_550)));
        Assert.Equal("25.05 ج", TicketFormat.Amount(Money.FromPiastres(2_505)));
        Assert.Equal("0 ج", TicketFormat.Amount(Money.Zero));
        Assert.Equal("-25 ج", TicketFormat.Amount(Money.FromPiastres(-2_500)));
        Assert.Equal("12,500 ج", TicketFormat.Amount(1_250_000L));
    }

    [Fact]
    public void Date_and_time_are_cairo_with_ltr_marks()
    {
        var at = new DateTimeOffset(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);   // 19:42 Cairo
        Assert.Equal("‎17-11-2026", TicketFormat.Date(at));
        Assert.Equal("‎07:42 م", TicketFormat.Time(at));
        // A right-to-left mark between the two parts stops the date and the time merging into one
        // left-to-right run, which put «م» against the date on paper (final review, Important 1).
        Assert.Equal("‎17-11-2026 ‏ ‎07:42 م", TicketFormat.DateTime(at));
        Assert.Equal("‎12:05 ص", TicketFormat.Time(new DateTimeOffset(2026, 11, 16, 22, 5, 0, TimeSpan.Zero)));
    }

    [Fact]
    public void Code_isolates_order_numbers() => Assert.Equal("‎#0281‎", TicketFormat.Code("#0281"));

    [Fact]
    public void No_composed_paper_contains_an_unformatted_thousand()
    {
        var at = new DateTimeOffset(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);
        var header = new ReceiptHeader("o-1", "فالح", "ألذ سندوتش", "", "#0286", at, "أحمد", OrderType.Takeaway, null, null, null, null);
        var items = new[] { new TicketItem("وجبة عائلية", null, 1, Money.FromPiastres(1_250_000), Money.FromPiastres(1_250_000), null, null, null, "Charcoal") };
        var totals = new ReceiptTotals(Money.FromPiastres(1_250_000), Money.Zero, Money.Zero, null, Money.Zero, Money.Zero,
            Money.FromPiastres(1_250_000), Money.FromPiastres(1_300_000), Money.FromPiastres(50_000), PaymentMethod.Cash);
        var receipt = PrintRules.ComposeReceiptTicket(PrintRoute.Receipt, header, items, totals);

        var opened = at.AddHours(-8);
        var statement = new ShiftCloseStatement(
            ShiftCloseStatement.KindName, 1, "فالح", null, "shift-abcdef123456", "123456", "مسائية", new DateOnly(2026, 11, 17),
            opened, at, "أحمد", "منى", null,
            new ShiftStatementSales(42, 1_250_000, 0, 0, 0, 1_250_000, 0, 1_250_000, 900_000, 350_000, 0, 0, 0, 0, 1_250_000, 0),
            Array.Empty<ShiftStatementExpense>(),
            new ShiftStatementCash(100_000, 900_000, 0, 0, 0, 0, 0, 1_000_000, 1_000_000, 0, 1_000_000, "الخزينة الرئيسية"),
            null, Array.Empty<string>());
        var close = PrintRules.ComposeShiftCloseStatement(statement);

        foreach (var ticket in new[] { receipt, close })
        {
            var texts = Texts(ticket);
            Assert.DoesNotContain(texts, t => Regex.IsMatch(t, @"\d{4,}(\.\d+)? ج"));
            Assert.Contains(texts, t => t.Contains("12,500 ج"));
        }
    }

    private static List<string> Texts(ComposedTicket ticket) =>
        ticket.Lines.SelectMany(l => l switch
        {
            TicketText t => new[] { t.Text },
            TicketRow r => r.Cells.Select(c => c.Text).ToArray(),
            _ => Array.Empty<string>(),
        }).ToList();
}
