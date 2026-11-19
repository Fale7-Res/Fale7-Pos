namespace Falah.Client.Tests;

using CallCenterServices = Falah.Client.CallCenterWinUI.Services;
using CallCenterModels = Falah.Client.CallCenterWinUI.Models;
using Xunit;

/// <summary>
/// UI pass 2026-11-08: the Call Center's small "إلغاء الطلب" link cleared the cart AND the caller's name,
/// phone and address on a single tap, with no question asked. The Cashier asks first. A phone order is
/// several minutes of a caller's time; the desk now asks too, unless there is nothing to lose.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public class CallCenterClearOrderTests
{
    private static Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel Vm() =>
        new(new CallCenterServices.AppSession(new CallCenterServices.ApiClient(new HttpClient { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) }, new());

    [Fact]
    public void Cancelling_an_order_with_items_asks_first_and_keeping_it_changes_nothing()
    {
        var vm = Vm();
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500 });
        vm.CustomerPhone = "01000000091";

        vm.RequestClearOrderCommand.Execute(null);
        Assert.True(vm.IsClearOrderOpen);
        Assert.Single(vm.Cart);

        vm.CancelClearOrderCommand.Execute(null);
        Assert.False(vm.IsClearOrderOpen);
        Assert.Single(vm.Cart);
        Assert.Equal("01000000091", vm.CustomerPhone);
    }

    [Fact]
    public void Confirming_clears_the_items_and_the_caller()
    {
        var vm = Vm();
        vm.Cart.Add(new CallCenterModels.CartLine { ProductId = "p", ProductName = "كبدة", UnitPricePiastres = 4500 });
        vm.CustomerPhone = "01000000091";

        vm.RequestClearOrderCommand.Execute(null);
        vm.ConfirmClearOrderCommand.Execute(null);

        Assert.False(vm.IsClearOrderOpen);
        Assert.Empty(vm.Cart);
        Assert.Equal("", vm.CustomerPhone);
    }

    [Fact]
    public void An_empty_order_is_reset_without_a_question()
    {
        var vm = Vm();
        vm.RequestClearOrderCommand.Execute(null);
        Assert.False(vm.IsClearOrderOpen);
    }
}
