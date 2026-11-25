using System.Net;
using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using CallCenterModels = Falah.Client.CallCenterWinUI.Models;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Audit 2026-11-25 U25 (Call Center desk on a full trading day): hints stayed on screen after they stopped
/// being true - "السلة فارغة" with items in the cart, and "عميل جديد - أدخل الاسم والعنوان" after the name and address
/// were saved.</summary>
public sealed class CallCenterDeskHintTests
{
    private sealed class NotFound : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
    }

    private static Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel Desk() =>
        new(new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new HttpClient(new NotFound()) { BaseAddress = new Uri("http://fixture.invalid") }))
            { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) }, new());

    [Fact]
    public async Task The_empty_cart_message_goes_away_once_an_item_is_added()
    {
        var vm = Desk();
        await vm.SubmitOrderCommand.ExecuteAsync(null);
        Assert.Equal("السلة فارغة.", vm.CheckoutError);

        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500, Quantity = 1 });

        Assert.Null(vm.CheckoutError);
    }

    [Fact]
    public void The_new_customer_hint_goes_away_once_the_details_are_saved()
    {
        var vm = Desk();
        vm.SetOrderTypeCommand.Execute("pickup");
        vm.CustomerLookupNote = "عميل جديد - أدخل الاسم والعنوان.";
        vm.CustomerPhone = "01122334455";
        vm.CustomerName = "خالد مصطفى";

        vm.SaveDeliveryDetailsCommand.Execute(null);

        Assert.Null(vm.CustomerLookupNote);
    }
}
