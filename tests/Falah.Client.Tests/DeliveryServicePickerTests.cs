using System.Text.Json;
using System.Xml.Linq;
using Falah.Application.Customers;
using Falah.Application.Delivery;
using Falah.Application.Users;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;
using CallCenter = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

/// <summary>Owner, 2026-11-26: about 50 delivery services - the order taker types part of the name and picks from
/// suggestions (with the fee) instead of scrolling a long list, in the cashier and the call center.</summary>
public sealed class DeliveryServicePickerTests
{
    private static DeliveryServiceDto Service(string id, string name, long? fee) =>
        new(id, name, fee, true, "owner", DateTimeOffset.UtcNow);

    private static readonly DeliveryServiceDto[] Services =
    {
        Service("s1", "مدينة نصر", 3_000),
        Service("s2", "6 أكتوبر", 5_000),
        Service("s3", "نصر الدين", 2_550),
        Service("s4", "المعادي", null),
        Service("s5", "الشيخ زايد", 6_000),
    };

    [Fact]
    public void Search_ignores_hamza_ta_marbuta_and_ya_spelling_and_case()
    {
        Assert.Equal(new[] { "s2" }, DeliveryServiceSearch.Find(Services, "اكتوبر").Select(s => s.Id));
        Assert.Equal(new[] { "s1" }, DeliveryServiceSearch.Find(Services, "مدينه").Select(s => s.Id));
        Assert.Equal(new[] { "s4" }, DeliveryServiceSearch.Find(Services, "  معادى ").Select(s => s.Id));
    }

    [Fact]
    public void A_name_starting_with_the_text_comes_before_one_containing_it()
    {
        Assert.Equal(new[] { "s3", "s1" }, DeliveryServiceSearch.Find(Services, "نصر").Select(s => s.Id));
        // A word inside the name counts as a start too ("زايد" in "الشيخ زايد").
        Assert.Equal(new[] { "s5" }, DeliveryServiceSearch.Find(Services, "زايد").Select(s => s.Id));
    }

    [Fact]
    public void An_empty_search_lists_every_service()
    {
        Assert.Equal(Services.Select(s => s.Id), DeliveryServiceSearch.Find(Services, "").Select(s => s.Id));
        Assert.Empty(DeliveryServiceSearch.Find(Services, "حلوان"));
    }

    [Fact]
    public void Each_suggestion_shows_its_fee_and_the_label_stays_off_the_wire()
    {
        Assert.Equal("مدينة نصر — 30 ج", Services[0].PickerLabel);
        Assert.Equal("نصر الدين — 25.50 ج", Services[2].PickerLabel);
        Assert.Equal("المعادي", Services[3].PickerLabel);
        Assert.DoesNotContain("pickerLabel", JsonSerializer.Serialize(Services[0], new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private static Cashier.ViewModels.CashierViewModel Till()
    {
        var vm = new Cashier.Services.AppSession(new Cashier.Services.ApiClient(new HttpClient { BaseAddress = new Uri("http://x.invalid") }))
        { CurrentUser = new UserSummaryDto("u", "كاشير", "cashier", new[] { "pos" }), ShiftId = "s" }.GetCashier(new());
        foreach (var s in Services) vm.DeliveryServices.Add(s);
        return vm;
    }

    private static CallCenter.ViewModels.CallCenterViewModel Desk()
    {
        var vm = new CallCenter.ViewModels.CallCenterViewModel(new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient(
            new HttpClient { BaseAddress = new Uri("http://x.invalid") })) { CurrentUser = new UserSummaryDto("u", "كول", "cashier", new[] { "pos" }) }, new());
        foreach (var s in Services) vm.DeliveryServices.Add(s);
        return vm;
    }

    [Fact]
    public void Cashier_typing_suggests_choosing_selects_and_editing_unselects()
    {
        var vm = Till();
        vm.SearchDeliveryServices("اكتوبر");
        Assert.Equal(new[] { "s2" }, vm.DeliveryServiceSuggestions.Select(s => s.Id));

        vm.SelectedDeliveryService = vm.DeliveryServiceSuggestions[0];
        Assert.Equal("6 أكتوبر", vm.DeliveryServiceQuery);
        Assert.Equal("s2", vm.SelectedDeliveryService!.Id);

        // Typing something else drops the earlier service: its fee must not stay on the order unseen.
        vm.SearchDeliveryServices("6 أكتو");
        Assert.Null(vm.SelectedDeliveryService);
        Assert.Equal("6 أكتو", vm.DeliveryServiceQuery);

        // Typing a whole name selects it.
        vm.SearchDeliveryServices("مدينه نصر");
        Assert.Equal("s1", vm.SelectedDeliveryService?.Id);
    }

    [Fact]
    public void Call_center_typing_suggests_choosing_selects_and_editing_unselects()
    {
        var vm = Desk();
        vm.SearchDeliveryServices("نصر");
        Assert.Equal(new[] { "s3", "s1" }, vm.DeliveryServiceSuggestions.Select(s => s.Id));

        vm.SelectedDeliveryService = vm.DeliveryServiceSuggestions[1];
        Assert.Equal("مدينة نصر", vm.DeliveryServiceQuery);
        Assert.Contains("30", vm.DeliveryFeeDisplay);

        vm.SearchDeliveryServices("مدينة");
        Assert.Null(vm.SelectedDeliveryService);

        vm.SearchDeliveryServices("");
        Assert.Equal(Services.Length, vm.DeliveryServiceSuggestions.Count);
    }

    [Fact]
    public void A_saved_address_puts_its_service_name_in_the_box_in_both_apps()
    {
        var home = new CustomerAddressDto("addr-1", "مدينة نصر", "عباس", "5", "2", "3", null, "s1", "مدينة نصر",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);
        var till = Till();
        till.SavedAddresses.Add(home);
        till.SelectedSavedAddress = home;
        Assert.Equal(("s1", "مدينة نصر"), (till.SelectedDeliveryService?.Id, till.DeliveryServiceQuery));

        var desk = Desk();
        desk.SavedAddresses.Add(home);
        desk.SelectedSavedAddress = home;
        Assert.Equal(("s1", "مدينة نصر"), (desk.SelectedDeliveryService?.Id, desk.DeliveryServiceQuery));
    }

    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(new[] { dir!.FullName, "src" }.Concat(parts).ToArray());
    }

    [Theory]
    [InlineData("Falah.Client.CashierWinUI", "DeliveryDetailsView.xaml")]
    [InlineData("Falah.Client.CallCenterWinUI", "CallCenterDeliveryEditorView.xaml")]
    public void Both_apps_pick_the_service_from_a_search_box(string app, string view)
    {
        var elements = XDocument.Load(Source(app, "Views", view)).Descendants().ToList();
        Assert.DoesNotContain(elements, e => e.Name.LocalName == "ComboBox" && (string?)e.Attribute("Header") == "خدمة التوصيل");
        var box = Assert.Single(elements, e => e.Name.LocalName == "AutoSuggestBox" && (string?)e.Attribute("Header") == "خدمة التوصيل");
        Assert.Equal("PickerLabel", (string?)box.Attribute("DisplayMemberPath"));
    }
}
