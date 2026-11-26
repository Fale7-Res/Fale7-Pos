using System.Xml.Linq;
using Falah.Application.Users;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;
using CallCenter = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

/// <summary>Owner, 2026-11-25: tapping a cart line's quantity opens a number pad - 0-9, delete, clear, «تم» - in both
/// apps, so a large quantity is typed instead of pressed «+» many times. Never 0 or empty; at most 999.</summary>
public sealed class QuantityPadTests
{
    private static Cashier.ViewModels.CashierViewModel Till()
    {
        var vm = new Cashier.Services.AppSession(new Cashier.Services.ApiClient(new HttpClient { BaseAddress = new Uri("http://x.invalid") }))
        { CurrentUser = new UserSummaryDto("u", "كاشير", "cashier", new[] { "pos" }), ShiftId = "s" }.GetCashier(new());
        vm.Cart.Add(new Cashier.Models.CartLine { ProductId = "p", ProductName = "شاورما", UnitPricePiastres = 5_000 });
        return vm;
    }

    private static CallCenter.ViewModels.CallCenterViewModel Desk()
    {
        var vm = new CallCenter.ViewModels.CallCenterViewModel(new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient(
            new HttpClient { BaseAddress = new Uri("http://x.invalid") })) { CurrentUser = new UserSummaryDto("u", "كول", "cashier", new[] { "pos" }) }, new());
        vm.Cart.Add(new CallCenter.Models.CartLine { ProductId = "p", ProductName = "شاورما", UnitPricePiastres = 5_000 });
        return vm;
    }

    [Fact]
    public void Till_types_a_quantity_on_the_pad()
    {
        var vm = Till();
        var line = vm.Cart.Single();

        vm.OpenQuantityPadCommand.Execute(line);
        Assert.True(vm.IsQuantityPadOpen);
        Assert.Equal("", vm.QuantityPadText);   // starts empty: the first digit is the new quantity, not appended to 1
        vm.QuantityPadDigitCommand.Execute("2");
        vm.QuantityPadDigitCommand.Execute("5");
        vm.ConfirmQuantityPadCommand.Execute(null);

        Assert.False(vm.IsQuantityPadOpen);
        Assert.Equal(25, line.Quantity);
        Assert.Equal(125_000, vm.CartSubtotalPiastres);
    }

    [Fact]
    public void Delete_and_clear_edit_the_typed_number()
    {
        var vm = Till();
        vm.OpenQuantityPadCommand.Execute(vm.Cart.Single());
        foreach (var d in "123") vm.QuantityPadDigitCommand.Execute(d.ToString());
        vm.QuantityPadBackspaceCommand.Execute(null);
        Assert.Equal("12", vm.QuantityPadText);
        vm.QuantityPadClearCommand.Execute(null);
        Assert.Equal("", vm.QuantityPadText);
    }

    [Theory]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("1000")]
    [InlineData("ab")]
    public void An_empty_zero_too_large_or_non_number_quantity_is_refused_and_the_line_keeps_its_quantity(string typed)
    {
        var vm = Till();
        var line = vm.Cart.Single();
        vm.OpenQuantityPadCommand.Execute(line);
        vm.QuantityPadText = typed;

        vm.ConfirmQuantityPadCommand.Execute(null);

        Assert.True(vm.IsQuantityPadOpen);
        Assert.NotNull(vm.QuantityPadError);
        Assert.Equal(1, line.Quantity);
    }

    [Fact]
    public void The_pad_never_holds_more_than_three_digits()
    {
        var vm = Till();
        vm.OpenQuantityPadCommand.Execute(vm.Cart.Single());
        foreach (var d in "12345") vm.QuantityPadDigitCommand.Execute(d.ToString());
        Assert.Equal("123", vm.QuantityPadText);
    }

    [Fact]
    public void Back_closes_without_changing_anything()
    {
        var vm = Till();
        var line = vm.Cart.Single();
        vm.OpenQuantityPadCommand.Execute(line);
        vm.QuantityPadDigitCommand.Execute("9");
        vm.CloseQuantityPadCommand.Execute(null);

        Assert.False(vm.IsQuantityPadOpen);
        Assert.Equal(1, line.Quantity);
    }

    [Fact]
    public void Call_center_types_a_quantity_on_the_pad()
    {
        var vm = Desk();
        var line = vm.Cart.Single();

        vm.OpenQuantityPadCommand.Execute(line);
        vm.QuantityPadDigitCommand.Execute("4");
        vm.QuantityPadDigitCommand.Execute("0");
        vm.ConfirmQuantityPadCommand.Execute(null);

        Assert.Equal(40, line.Quantity);
        Assert.False(vm.IsQuantityPadOpen);
    }

    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        return Path.Combine(new[] { dir!.FullName, "src" }.Concat(parts).ToArray());
    }

    [Theory]
    [InlineData("Falah.Client.CashierWinUI", "CartPanelView.xaml", "CashierView.xaml")]
    [InlineData("Falah.Client.CallCenterWinUI", "CallCenterOrderPanelView.xaml", "CallCenterView.xaml")]
    public void The_quantity_opens_the_pad_and_the_pad_has_every_key(string app, string cart, string host)
    {
        Assert.Contains(XDocument.Load(Source(app, "Views", cart)).Descendants(),
            e => ((string?)e.Attribute("Command"))?.Contains("OpenQuantityPadCommand") == true);
        var pad = XDocument.Load(Source(app, "Views", host)).Descendants().ToList();
        foreach (var digit in Enumerable.Range(0, 10))
            Assert.Contains(pad, e => (string?)e.Attribute("CommandParameter") == digit.ToString()
                && ((string?)e.Attribute("Command"))?.Contains("QuantityPadDigitCommand") == true);
        Assert.Contains(pad, e => (string?)e.Attribute("Content") == "تم");
    }
}
