using Falah.Client.CashierWinUI.ViewModels;
using Falah.Domain.Delivery;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>UI-REQ-001: what one driver-statement row says on screen.</summary>
public class DriverStatementRowTests
{
    private static DriverStatementOrder Order(string? name = "عميل", string? phone = "0100", bool cancelled = false,
        string status = "Delivered", string? from = null, long refunded = 0) =>
        new("o1", "#0135", DateTimeOffset.UtcNow, name, phone, "شارع 5", "cash", status, 34_000, 1_500, 35_500, refunded,
            0, 0, 0, true, from, cancelled);

    [Fact]
    public void The_name_stands_alone_and_the_phone_shares_the_address_line()
    {
        var row = new DriverStatementRow(Order());
        Assert.Equal("عميل", row.Customer);
        Assert.Equal("0100 • شارع 5", row.Address);
    }

    [Fact]
    public void Without_a_name_the_phone_takes_its_place_and_is_not_repeated()
    {
        var row = new DriverStatementRow(Order(name: null));
        Assert.Equal("0100", row.Customer);
        Assert.Equal("شارع 5", row.Address);
    }

    [Fact]
    public void A_voided_failed_reassigned_row_says_all_of_it()
    {
        var row = new DriverStatementRow(Order(cancelled: true, status: "Failed", from: "سائق محمود"));
        Assert.Equal("ملغى - خارج المجاميع • فشل التوصيل • من سائق محمود", row.Notes);
    }
}
