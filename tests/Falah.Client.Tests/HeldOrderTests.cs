using Falah.Application.Menu;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;

namespace Falah.Client.Tests;

/// <summary>Owner, 2026-11-17/18: the till could hold one order at a time. He chose fixed basket tabs
/// above the cart - «سلة 1 | سلة 2 | … | +» - over a hold button: tapping a tab works on that order,
/// and each basket keeps its own items, type and customer details. Nothing is sent to the server and
/// no money moves until a basket is paid.</summary>
public class HeldOrderTests
{
    private static MenuProductDto Product(string id, string name, long price) =>
        new(id, name, price, false, true, Array.Empty<MenuVariantDto>(), Array.Empty<MenuOfferDto>(), null);

    private static Cashier.ViewModels.CashierViewModel Till() => new(
        new Cashier.Services.AppSession(new Cashier.Services.ApiClient("http://localhost:5151"))
            { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) },
        new Cashier.ViewModels.HeaderViewModel());

    [Fact]
    public void The_till_starts_with_one_active_basket()
    {
        var vm = Till();
        var tab = Assert.Single(vm.Baskets);
        Assert.True(tab.IsActive);
        Assert.Equal("سلة 1", tab.Title);
    }

    [Fact]
    public void A_new_basket_starts_empty_and_switching_back_restores_the_first_whole()
    {
        var vm = Till();
        var falafel = Product("p-falafel", "فلافل صمون", 1_000);
        vm.SelectProductCommand.Execute(falafel);
        vm.SelectProductCommand.Execute(falafel);
        vm.SetOrderTypeCommand.Execute("delivery");
        vm.CustomerName = "عميل أول";

        vm.AddBasketCommand.Execute(null);

        Assert.Equal(2, vm.Baskets.Count);
        Assert.True(vm.Baskets[1].IsActive);
        Assert.False(vm.Baskets[0].IsActive);
        Assert.Empty(vm.Cart);
        Assert.Equal("takeaway", vm.OrderType);
        Assert.Equal("", vm.CustomerName);
        Assert.Equal(2, vm.Baskets[0].ItemCount);           // the tab still shows what it holds

        vm.SelectProductCommand.Execute(Product("p-pepsi", "بيبسي", 2_000));
        vm.SwitchBasketCommand.Execute(vm.Baskets[0]);

        var line = Assert.Single(vm.Cart);
        Assert.Equal(("p-falafel", 2), (line.ProductId, line.Quantity));
        Assert.Equal("delivery", vm.OrderType);
        Assert.Equal("عميل أول", vm.CustomerName);
        Assert.False(vm.IsDeliveryEditorOpen);
        Assert.Equal(1, vm.Baskets[1].ItemCount);           // the Pepsi basket kept its order

        vm.SwitchBasketCommand.Execute(vm.Baskets[1]);
        Assert.Equal("p-pepsi", Assert.Single(vm.Cart).ProductId);
        Assert.Equal(2_000, vm.CartSubtotalPiastres);
    }

    [Fact]
    public void Only_an_empty_basket_that_is_not_the_last_can_be_closed()
    {
        var vm = Till();
        vm.CloseBasketCommand.Execute(vm.Baskets[0]);
        Assert.Single(vm.Baskets);                          // never zero baskets

        vm.SelectProductCommand.Execute(Product("p-falafel", "فلافل صمون", 1_000));
        vm.AddBasketCommand.Execute(null);
        vm.CloseBasketCommand.Execute(vm.Baskets[0]);
        Assert.Equal(2, vm.Baskets.Count);                  // it holds an order: use «إلغاء الطلب» first

        vm.CloseBasketCommand.Execute(vm.Baskets[1]);       // the empty, active one
        var left = Assert.Single(vm.Baskets);
        Assert.True(left.IsActive);
        Assert.Equal("p-falafel", Assert.Single(vm.Cart).ProductId);
    }

    [Fact]
    public void Baskets_cannot_be_switched_from_the_payment_screen()
    {
        var vm = Till();
        vm.SelectProductCommand.Execute(Product("p-falafel", "فلافل صمون", 1_000));
        vm.AddBasketCommand.Execute(null);
        vm.SelectProductCommand.Execute(Product("p-pepsi", "بيبسي", 2_000));
        vm.IsCheckoutOpen = true;

        vm.SwitchBasketCommand.Execute(vm.Baskets[0]);
        vm.AddBasketCommand.Execute(null);

        Assert.Equal("p-pepsi", Assert.Single(vm.Cart).ProductId);
        Assert.Equal(2, vm.Baskets.Count);
    }
}
