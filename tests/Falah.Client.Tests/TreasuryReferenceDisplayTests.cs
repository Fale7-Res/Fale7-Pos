using Falah.Application.Treasury;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Native run 2026-11-07: a refund line in the treasury ledger read
/// "order_refund ord-1e2eba8a…" - an internal type and id - instead of the order it refunded.</summary>
public class TreasuryReferenceDisplayTests
{
    private static TreasuryViewModel.MovementRow Row(string type, string? label) => new()
    {
        Movement = new TreasuryMovementDto("mv-1", "refund", "tre-drawer", null, 1000, type, "ord-1e2eba8a",
            "shift-1", "صنف ناقص", "مدير", null, DateTimeOffset.UtcNow, label),
    };

    [Fact]
    public void A_refund_names_the_order_number_it_refunded()
        => Assert.Equal("استرجاع طلب #0138", Row("order_refund", "#0138").ReferenceDisplay);

    [Fact]
    public void A_refund_never_shows_the_internal_reference_type()
        => Assert.DoesNotContain("order_refund", Row("order_refund", null).ReferenceDisplay);

    [Theory]
    [InlineData("external_funding")]
    [InlineData("some_future_type")]
    public void No_reference_shows_an_internal_type_or_id(string type)
    {
        var shown = Row(type, null).ReferenceDisplay;
        Assert.DoesNotContain(type, shown);
        Assert.DoesNotContain("ord-1e2eba8a", shown);
    }

    /// <summary>Native run 2026-11-07 (shift recovery proof): the ledger line for a shift's float read
    /// "shift_opening_float". Every type the server writes into the treasury ledger needs a name.</summary>
    [Theory]
    [InlineData("shift_opening_float")]
    [InlineData("shift_handover")]
    [InlineData("cash_shortage")]
    [InlineData("cash_overage")]
    [InlineData("cash_in_transfer")]
    [InlineData("cash_out_transfer")]
    [InlineData("employee_payment")]
    [InlineData("settlement_shortage")]
    public void Every_ledger_type_the_server_writes_has_an_arabic_name(string type)
    {
        var row = new TreasuryViewModel.MovementRow
        {
            Movement = new TreasuryMovementDto("mv-1", type, "tre-main", "tre-drawer", 1000, null!, null!,
                "shift-1", "س", "مدير", null, DateTimeOffset.UtcNow, null),
        };
        Assert.NotEqual(type, row.TypeLabel);
        Assert.DoesNotContain("_", row.TypeLabel);
    }
}
