using Falah.Application.Menu;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;
using CallCenter = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

/// <summary>Owner, 2026-11-17: tapping «فلافل صمون» a second time added a second «فلافل صمون» line
/// instead of making the first one ×2. The same item - same product, same choice, same offer, no note
/// of its own yet - now raises the quantity; anything that differs still gets its own line, because
/// the kitchen reads each line separately.</summary>
public class CartMergeTests
{
    private static MenuProductDto Product(string id, params MenuVariantDto[] variants) =>
        new(id, "فلافل صمون", 1_000, false, true, variants, Array.Empty<MenuOfferDto>(), null);

    private static Cashier.ViewModels.CashierViewModel Till() => new(
        new Cashier.Services.AppSession(new Cashier.Services.ApiClient("http://localhost:5151"))
            { CurrentUser = new("actor", "اختبار", "cashier", new[] { "pos" }) },
        new Cashier.ViewModels.HeaderViewModel());

    private static CallCenter.ViewModels.CallCenterViewModel Desk() => new(
        new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient("http://localhost:5151"))
            { CurrentUser = new("operator", "موظف", "cashier", new[] { "pos" }) },
        new());

    [Fact]
    public void Tapping_the_same_item_twice_on_the_till_makes_one_line_of_two()
    {
        var vm = Till();
        var falafel = Product("p-falafel");
        vm.SelectProductCommand.Execute(falafel);
        vm.SelectProductCommand.Execute(falafel);

        var line = Assert.Single(vm.Cart);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(2_000, vm.CartSubtotalPiastres);
    }

    [Fact]
    public void A_different_choice_or_a_line_with_its_own_note_stays_separate_on_the_till()
    {
        var vm = Till();
        var small = new MenuVariantDto("v-s", "صغير", 1_000);
        var large = new MenuVariantDto("v-l", "كبير", 1_500);
        var sandwich = Product("p-sandwich", small, large);

        vm.SelectProductCommand.Execute(sandwich); vm.SelectVariantCommand.Execute(small);
        vm.SelectProductCommand.Execute(sandwich); vm.SelectVariantCommand.Execute(large);
        Assert.Equal(2, vm.Cart.Count);

        vm.Cart[0].Notes = "بدون طحينة";
        vm.SelectProductCommand.Execute(sandwich); vm.SelectVariantCommand.Execute(small);
        Assert.Equal(3, vm.Cart.Count);
        Assert.All(vm.Cart, l => Assert.Equal(1, l.Quantity));
    }

    [Fact]
    public void Tapping_the_same_item_twice_at_the_call_center_makes_one_line_of_two()
    {
        var vm = Desk();
        var falafel = Product("p-falafel");
        vm.SelectProductCommand.Execute(falafel);
        vm.SelectProductCommand.Execute(falafel);

        var line = Assert.Single(vm.Cart);
        Assert.Equal(2, line.Quantity);
    }
}
