using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.ViewModels;
namespace Falah.Client.Tests;
public class PaymentStatusCasingTests
{
    [Theory]
    [InlineData("pending")]
    [InlineData("Pending")]
    [InlineData("PENDING")]
    public void Board_warns_about_unconfirmed_transfer_regardless_of_casing(string status)
    {
        var row = new DeliveryViewModel.BoardRow { Delivery = new DeliveryDto(
            "d", "o", "#1", "Ready", null, null, 0, 1000, "instapay", false,
            null, DateTimeOffset.UtcNow, ElectronicPaymentStatus: status) };
        Assert.True(row.IsPaymentUnconfirmed);
        Assert.Equal("تحويل لم يُؤكَّد", row.PaymentStateLabel);
    }
    [Theory]
    [InlineData("confirmed", true, "مدفوع")]
    [InlineData(null, false, "تحصيل عند التسليم")]
    public void Board_keeps_paid_and_cash_collection_distinct(string? status, bool paid, string expected)
    {
        var row = new DeliveryViewModel.BoardRow { Delivery = new DeliveryDto(
            "d", "o", "#1", "Ready", null, null, 1000, 1000, "cash", paid,
            null, DateTimeOffset.UtcNow, ElectronicPaymentStatus: status) };
        Assert.False(row.IsPaymentUnconfirmed);
        Assert.Equal(expected, row.PaymentStateLabel);
    }
}
