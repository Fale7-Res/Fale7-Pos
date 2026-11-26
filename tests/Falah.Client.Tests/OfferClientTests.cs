using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Menu;
using Falah.Application.Users;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;
using CallCenter = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

/// <summary>Owner, 2026-11-25: offers on the stations - each offer priced by its kind for the size chosen, shown only
/// where it may be used; a gift follows the total (by itself, or as a button, as the offer says) and leaves the cart
/// when the order no longer earns it; the menu refreshes quietly through the shift.</summary>
public sealed class OfferClientTests
{
    private sealed class Handler(Func<HttpRequestMessage, HttpResponseMessage> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => Task.FromResult(send(request));
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, Web), Encoding.UTF8, "application/json") };

    private static readonly MenuOfferDto TenPercent = new("o-pct", "خصم 10%", 0, "ItemPercent", 1_000);
    private static readonly MenuOfferDto DeliveryOnly = new("o-del", "عرض التوصيل", 4_000, OrderTypes: new[] { "delivery" });
    private static readonly MenuOfferDto DeskOnly = new("o-desk", "عرض الكول سنتر", 4_000, Stations: new[] { "callcenter" });

    private static MenuProductDto Kofta => new("kofta", "كفتة", 9_000, false, true,
        new[] { new MenuVariantDto("small", "صمون", 9_000), new MenuVariantDto("large", "فرنساوي", 9_500) },
        new[] { TenPercent, DeliveryOnly, DeskOnly }, null);

    private static MenuProductDto Meal => new("meal", "وجبة", 10_000, false, true, Array.Empty<MenuVariantDto>(), Array.Empty<MenuOfferDto>(), null);

    private static MenuGiftOfferDto ColaGift(bool autoAdd, bool includesDelivery = true) => new("g-cola", "هدية الكولا", includesDelivery, autoAdd, null, null,
        new[] { new MenuGiftTierDto(20_000, "cola", null, "كولا لتر", null, 1), new MenuGiftTierDto(50_000, "cola", null, "كولا لتر", null, 2) });

    private static MenuResponse Menu(MenuGiftOfferDto? gift = null) => new(
        new[] { new MenuCategoryDto("c1", "ساندوتشات", null, new[] { Kofta, Meal }), new MenuCategoryDto("c2", "وجبات", null, new[] { Meal }) },
        Array.Empty<string>(), gift is null ? null : new[] { gift });

    private static HttpClient Server(Func<MenuResponse> menu, Action? onMenu = null) => new(new Handler(r =>
    {
        var path = r.RequestUri!.AbsolutePath;
        if (path == "/api/menu") { onMenu?.Invoke(); return Json(menu()); }
        return Json(Array.Empty<object>());
    })) { BaseAddress = new Uri("http://fixture.invalid") };

    private static async Task<Cashier.ViewModels.CashierViewModel> TillAsync(MenuGiftOfferDto? gift = null)
    {
        var vm = new Cashier.Services.AppSession(new Cashier.Services.ApiClient(Server(() => Menu(gift))))
        { CurrentUser = new UserSummaryDto("u", "كاشير", "cashier", new[] { "pos" }), ShiftId = "s" }.GetCashier(new());
        await vm.LoadMenuAsync();
        return vm;
    }

    private static Cashier.Models.ProductTile Tile(Cashier.ViewModels.CashierViewModel vm, string id)
    {
        vm.SearchQuery = id == "kofta" ? "كفتة" : "وجبة";
        return vm.VisibleProducts.First(t => t.Product.Id == id);
    }

    [Fact]
    public async Task A_percentage_offer_is_priced_on_the_size_chosen()
    {
        var vm = await TillAsync();
        var choice = Tile(vm, "kofta").Offers.Single(o => o.Offer.Id == "o-pct");

        vm.SelectOfferCommand.Execute(choice);
        Assert.Contains("خصم 10%", vm.PendingOfferBanner);
        vm.SelectVariantCommand.Execute(Kofta.Variants[1]);

        var line = vm.Cart.Single();
        Assert.Equal((8_550L, "o-pct"), (line.UnitPricePiastres, line.AppliedOfferId));
        Assert.Equal("خصم 10%", choice.Offer.PriceLabel);
    }

    [Fact]
    public async Task An_offer_shows_only_for_the_order_types_and_station_it_allows()
    {
        var vm = await TillAsync();
        Assert.DoesNotContain(Tile(vm, "kofta").Offers, o => o.Offer.Id == "o-del");
        Assert.DoesNotContain(Tile(vm, "kofta").Offers, o => o.Offer.Id == "o-desk");   // a till, not the call center

        vm.OrderType = "delivery";
        vm.IsDeliveryEditorOpen = false;
        Assert.Contains(Tile(vm, "kofta").Offers, o => o.Offer.Id == "o-del");
    }

    [Fact]
    public async Task An_automatic_gift_follows_the_total_up_and_down()
    {
        var vm = await TillAsync(ColaGift(autoAdd: true));
        vm.SelectProductCommand.Execute(Meal);
        Assert.DoesNotContain(vm.Cart, l => l.IsGift);    // 100 ج

        vm.SelectProductCommand.Execute(Meal);            // 200 ج
        var gift = Assert.Single(vm.Cart, l => l.IsGift);
        Assert.Equal((1, 0L, "g-cola"), (gift.Quantity, gift.UnitPricePiastres, gift.AppliedOfferId));
        Assert.Equal(20_000, vm.GrandTotalPiastres);       // the gift is free

        for (var i = 0; i < 3; i++) vm.SelectProductCommand.Execute(Meal);   // 500 ج: the higher tier only
        Assert.Equal(2, Assert.Single(vm.Cart, l => l.IsGift).Quantity);

        vm.DecreaseQuantityCommand.Execute(vm.Cart.First(l => !l.IsGift));    // 400 ج (the meals are one line of 5)
        Assert.Equal(1, Assert.Single(vm.Cart, l => l.IsGift).Quantity);
        foreach (var l in vm.Cart.Where(l => !l.IsGift).ToList()) vm.RemoveLineCommand.Execute(l);
        Assert.Empty(vm.Cart);                                                 // nothing left: no gift either
    }

    [Fact]
    public async Task A_gift_the_cashier_adds_is_offered_as_a_button()
    {
        var vm = await TillAsync(ColaGift(autoAdd: false));
        vm.SelectProductCommand.Execute(Meal);
        vm.SelectProductCommand.Execute(Meal);

        Assert.DoesNotContain(vm.Cart, l => l.IsGift);
        Assert.True(vm.HasGiftSuggestion);
        Assert.Contains("كولا لتر", vm.GiftSuggestion);
        vm.AddGiftCommand.Execute(null);

        Assert.Equal(1, Assert.Single(vm.Cart, l => l.IsGift).Quantity);
        Assert.False(vm.HasGiftSuggestion);
        vm.DecreaseQuantityCommand.Execute(vm.Cart.First(l => !l.IsGift));    // below 200: the gift goes
        Assert.DoesNotContain(vm.Cart, l => l.IsGift);
    }

    [Fact]
    public async Task The_quiet_refresh_keeps_the_section_on_screen_and_picks_up_a_new_offer()
    {
        var calls = 0;
        MenuGiftOfferDto? gift = null;
        var vm = new Cashier.Services.AppSession(new Cashier.Services.ApiClient(Server(() => Menu(gift), () => calls++)))
        { CurrentUser = new UserSummaryDto("u", "كاشير", "cashier", new[] { "pos" }), ShiftId = "s" }.GetCashier(new());
        await vm.LoadMenuAsync();
        vm.SelectedCategory = vm.Categories.Single(c => c.Id == "c2");
        vm.SelectProductCommand.Execute(Meal);
        vm.SelectProductCommand.Execute(Meal);

        gift = ColaGift(autoAdd: true);                  // the owner adds an offer while the till is open
        await vm.RefreshMenuQuietlyCommand.ExecuteAsync(null);

        Assert.Equal(2, calls);
        Assert.Equal("c2", vm.SelectedCategory!.Id);
        Assert.Contains(vm.Cart, l => l.IsGift);
    }

    [Fact]
    public async Task Call_center_prices_an_offer_and_follows_a_gift_the_same_way()
    {
        var vm = new CallCenter.ViewModels.CallCenterViewModel(new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient(
            Server(() => Menu(ColaGift(autoAdd: true))))) { CurrentUser = new UserSummaryDto("u", "كول", "cashier", new[] { "pos" }) }, new());
        await vm.RetryLoadMenuCommand.ExecuteAsync(null);
        vm.SearchQuery = "كفتة";
        var tile = vm.VisibleProducts.First(t => t.Product.Id == "kofta");
        Assert.Contains(tile.Offers, o => o.Offer.Id == "o-desk");   // the call center's own offer
        Assert.Contains(tile.Offers, o => o.Offer.Id == "o-del");    // the desk takes deliveries

        vm.SelectOfferCommand.Execute(tile.Offers.Single(o => o.Offer.Id == "o-pct"));
        vm.SelectVariantCommand.Execute(Kofta.Variants[0]);
        Assert.Equal(8_100, vm.Cart.Single().UnitPricePiastres);

        vm.SelectProductCommand.Execute(Meal);
        vm.SelectProductCommand.Execute(Meal);   // 81 + 200 = 281 ج
        Assert.Equal(1, Assert.Single(vm.Cart, l => l.IsGift).Quantity);
    }
}
