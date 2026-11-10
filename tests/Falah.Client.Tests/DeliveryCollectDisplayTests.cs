using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Native run 2026-11-07: a card-paid delivery showed "للتحصيل 90.00 ج" on the board and in
/// the assign dialog, although the server opens no custody for it (DeliveryRules.DeliveryCashResponsibility).
/// A driver told to collect from a customer who already paid is a double charge.</summary>
public class DeliveryCollectDisplayTests
{
    private static DeliveryDto Dto(string method, bool settled) => new(
        "delivery-1", "ord-1", "#0139", "New", null, null, 9000, 9000, method, settled, "shift-1", DateTimeOffset.UtcNow);

    [Theory]
    [InlineData("card", true)]
    [InlineData("instapay", true)]
    [InlineData("wallet", true)]
    public void A_paid_order_has_nothing_to_collect(string method, bool settled)
    {
        Assert.Equal(0, DeliveryViewModel.CollectPiastres(Dto(method, settled)));
        Assert.Equal("—", new DeliveryViewModel.BoardRow { Delivery = Dto(method, settled) }.AmountToCollectDisplay);
    }

    [Theory]
    [InlineData("cash", false)]
    [InlineData("instapay", false)]   // unconfirmed transfer: the money is not in yet
    public void Cash_on_delivery_and_unconfirmed_transfers_still_show_the_amount(string method, bool settled)
    {
        Assert.Equal(9000, DeliveryViewModel.CollectPiastres(Dto(method, settled)));
        Assert.Equal("90.00 ج", new DeliveryViewModel.BoardRow { Delivery = Dto(method, settled) }.AmountToCollectDisplay);
    }
}
