using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.ViewModels;
namespace Falah.Client.Tests;

/// <summary>FIN-0051 (owner decision 2026-11-22): the board offers "إلغاء الطلب" only for an unpaid
/// cash-on-delivery order that has not been handed to a driver. The server enforces the same rule.</summary>
public class DeliveryCancelBeforeDispatchTests
{
    private static DeliveryDto Delivery(string status, string method, bool paid, string? electronic = null) => new(
        "d", "o", "#1", status, null, null, 1000, 1000, method, paid, null, DateTimeOffset.UtcNow, ElectronicPaymentStatus: electronic);

    [Theory]
    [InlineData("New", "cash", false, null, true)]
    [InlineData("Preparing", "cash", false, null, true)]
    [InlineData("Ready", "cash", false, null, true)]
    [InlineData("Assigned", "cash", false, null, false)]
    [InlineData("OutForDelivery", "cash", false, null, false)]
    [InlineData("Ready", "card", true, null, false)]
    [InlineData("Ready", "instapay", false, "pending", false)]
    public void Only_an_unpaid_cash_on_delivery_order_still_on_the_board_can_be_cancelled(
        string status, string method, bool paid, string? electronic, bool expected)
    {
        var row = new DeliveryViewModel.BoardRow { Delivery = Delivery(status, method, paid, electronic) };
        Assert.Equal(expected, row.CanCancel);
    }
}
