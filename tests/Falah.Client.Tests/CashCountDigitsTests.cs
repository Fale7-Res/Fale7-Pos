using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Whole-project review 2026-11-17: a denomination count typed on an Arabic keyboard ("٣")
/// was read as 0 - the money fields beside it already accepted Arabic digits - so the saved count
/// under-reported the cash and the server recorded a false shortage.</summary>
public class CashCountDigitsTests
{
    [Theory]
    [InlineData("3", 3)]
    [InlineData("٣", 3)]
    [InlineData("١٢", 12)]
    [InlineData("۱۵", 15)]
    [InlineData(" 7 ", 7)]
    [InlineData("", 0)]
    [InlineData("-2", 0)]
    [InlineData("2.5", 0)]
    [InlineData("abc", 0)]
    public void A_count_reads_western_and_arabic_digits_alike(string typed, long expected)
    {
        var row = new CashCountViewModel.DenominationRow { DenominationPiastres = 20_000, QuantityText = typed };
        Assert.Equal(expected, row.Quantity);
        Assert.Equal(CashInput.Display(expected * 20_000) + " ج", row.LineTotalDisplay);   // audit 2026-11-25 U6: separated
    }
}
