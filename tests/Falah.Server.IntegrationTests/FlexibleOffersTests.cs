namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Menu;
using Falah.Application.Orders;
using Falah.Application.Pricing;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Owner, 2026-11-25: offers of every shape, saved from the offers screen and charged by the server - a price or a
/// percentage on items, an amount or a percentage on a whole category, a gift on an order over an amount (the highest
/// tier only, with or without the delivery fee); limited to order types, stations, dates and hours, and in quantity per
/// order, per day and overall. An offer that may no longer be used is refused with its name, never charged silently.
/// </summary>
public class FlexibleOffersTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _ownerId = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        _factory.StationUserId = _cashierId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        await _factory.SeedDeliveryServiceAsync("svc-1", 3_000, "توصيل");
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---- helpers ----

    private Task<string> CategoryOfAsync(string productId) =>
        _factory.QueryAsync(db => db.Products.Where(p => p.Id == productId).Select(p => p.CategoryId).FirstAsync());

    private async Task<PriceRuleDto> SaveAsync(string name, string kind, IReadOnlyList<string>? products = null, long fixedPrice = 0,
        int percent = 0, long amountOff = 0, IReadOnlyList<string>? categories = null, IReadOnlyList<GiftTierDto>? tiers = null,
        IReadOnlyList<string>? orderTypes = null, IReadOnlyList<string>? stations = null, int? maxPerOrder = null, int? maxPerDay = null,
        bool giftIncludesDelivery = true, bool giftAutoAdd = false, DateOnly? startDate = null, DateOnly? endDate = null)
    {
        _factory.StationUserId = _ownerId;
        var response = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, name, products ?? Array.Empty<string>(), fixedPrice, null, null, null, true, _ownerId, null, null,
            kind, percent, amountOff, categories, startDate, endDate, orderTypes, stations, maxPerOrder, maxPerDay, null, tiers, giftIncludesDelivery, giftAutoAdd));
        _factory.StationUserId = _cashierId;
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<PriceRuleDto>())!;
    }

    private Task<HttpResponseMessage> SellAsync(IReadOnlyList<CheckoutItemRequest> items, string orderType = "takeaway", string? route = null, long paid = 1_000_000) =>
        _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: orderType, PaymentMethod: "cash", Items: items,
            DiscountPiastres: 0, DeliveryFeePiastres: orderType == "delivery" ? 3_000 : 0, CompAmountPiastres: 0, CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: orderType == "delivery" ? 0 : paid, SplitPayments: null, WalletProvider: null, TableNumber: null,
            CustomerName: orderType == "takeaway" ? null : "عميل", CustomerPhone: orderType == "takeaway" ? null : "01012345678",
            DeliveryAddress: null, DeliveryArea: orderType == "delivery" ? "المنطقة" : null, BusinessDayId: null,
            DeliveryStreet: orderType == "delivery" ? "شارع" : null, DeliveryServiceId: orderType == "delivery" ? "svc-1" : null,
            CustomerPrintRoute: route));

    private static CheckoutItemRequest Line(string product, int qty = 1, string? rule = null, string? variant = null) => new(product, variant, qty, rule, null);

    private async Task<CheckoutOrderResult> SoldAsync(HttpResponseMessage response)
    {
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private static async Task RefusedAsync(HttpResponseMessage response, string contains)
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Contains(contains, await response.Content.ReadAsStringAsync());
    }

    // ---- saving ----

    [Fact]
    public async Task Every_setting_of_an_offer_is_saved_and_read_back()
    {
        var product = await _factory.SeedProductAsync(5_000);
        var category = await CategoryOfAsync(product);
        var saved = await SaveAsync("خصم المشروبات", "CategoryPercent", percent: 1_000, categories: new[] { category },
            orderTypes: new[] { "delivery", "pickup" }, stations: new[] { "callcenter" }, maxPerOrder: 3, maxPerDay: 50,
            startDate: new DateOnly(2026, 11, 1), endDate: new DateOnly(2026, 12, 31));

        var read = (await _client.GetFromJsonAsync<List<PriceRuleDto>>("/api/price-rules"))!.Single(r => r.Id == saved.Id);
        Assert.Equal(("CategoryPercent", 1_000, 3, 50), (read.Kind, read.PercentBasisPoints, read.MaxPerOrder, read.MaxPerDay));
        Assert.Equal(new[] { category }, read.CategoryIds);
        Assert.Equal(new[] { "delivery", "pickup" }, read.OrderTypes);
        Assert.Equal(new[] { "callcenter" }, read.Stations);
        Assert.Equal((new DateOnly(2026, 11, 1), new DateOnly(2026, 12, 31)), (read.StartDate, read.EndDate));
        Assert.Empty(read.ProductIds);   // a category offer keeps no item list
    }

    [Fact]
    public async Task A_request_without_the_new_settings_still_saves_a_fixed_price_offer()
    {
        var product = await _factory.SeedProductAsync(5_000);
        _factory.StationUserId = _ownerId;
        var response = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "عرض قديم", new[] { product }, 4_000, null, null, null, true, _ownerId, null, null));
        _factory.StationUserId = _cashierId;
        response.EnsureSuccessStatusCode();
        var rule = (await response.Content.ReadFromJsonAsync<PriceRuleDto>())!;
        Assert.Equal(("ItemFixedPrice", 4_000L), (rule.Kind, rule.FixedPricePiastres));
        Assert.Equal(4_000, (await SoldAsync(await SellAsync(new[] { Line(product, 1, rule.Id) })) ).TotalPiastres);
    }

    [Fact]
    public async Task A_gift_whose_product_has_several_sizes_must_name_one()
    {
        var cola = await _factory.SeedProductAsync(2_000, "كولا");
        await _factory.SeedVariantAsync(cola, 2_000, "لتر");
        await _factory.SeedVariantAsync(cola, 3_000, "لتر ونص");
        _factory.StationUserId = _ownerId;
        var response = await _client.PostAsJsonAsync("/api/price-rules", new SavePriceRuleRequest(
            $"req-{Guid.NewGuid():N}", null, "هدية", Array.Empty<string>(), 0, null, null, null, true, _ownerId, null, null,
            "OrderGift", GiftTiers: new[] { new GiftTierDto(20_000, cola, null, 1) }));
        await RefusedAsync(response, "اختار حجم الهدية");
    }

    // ---- item and category prices ----

    [Fact]
    public async Task A_percentage_on_an_item_comes_off_each_size()
    {
        var product = await _factory.SeedProductAsync(9_000, "كفتة");
        var large = await _factory.SeedVariantAsync(product, 9_500, "فرنساوي");
        var rule = await SaveAsync("10% كفتة", "ItemPercent", products: new[] { product }, percent: 1_000);

        var order = await SoldAsync(await SellAsync(new[] { Line(product, 2, rule.Id, large) }));

        Assert.Equal(17_100, order.TotalPiastres);   // 95 - 9.50 = 85.50, twice
        var item = await _factory.QueryAsync(db => db.OrderItems.FirstAsync(i => i.OrderId == order.OrderId));
        Assert.Equal((8_550L, 9_500L, rule.Id), (item.UnitPricePiastres, item.ListUnitPricePiastres, item.PriceRuleId));
    }

    [Fact]
    public async Task An_amount_off_a_category_applies_to_its_items_only()
    {
        var sandwich = await _factory.SeedProductAsync(5_000, "ساندوتش");
        var other = await _factory.SeedProductAsync(5_000, "صنف من قسم تاني");
        var rule = await SaveAsync("10 ج على الساندوتشات", "CategoryAmount", amountOff: 1_000, categories: new[] { await CategoryOfAsync(sandwich) });

        Assert.Equal(4_000, (await SoldAsync(await SellAsync(new[] { Line(sandwich, 1, rule.Id) }))).TotalPiastres);
        await RefusedAsync(await SellAsync(new[] { Line(other, 1, rule.Id) }), "مش على");
    }

    [Fact]
    public async Task Every_offer_is_optional_the_same_item_sells_at_its_normal_price_without_it()
    {
        var product = await _factory.SeedProductAsync(5_000);
        await SaveAsync("عرض", "ItemFixedPrice", products: new[] { product }, fixedPrice: 4_000);
        Assert.Equal(5_000, (await SoldAsync(await SellAsync(new[] { Line(product) }))).TotalPiastres);
    }

    // ---- who may use it ----

    [Fact]
    public async Task An_offer_for_delivery_from_the_call_center_is_refused_on_a_takeaway_at_the_till()
    {
        var product = await _factory.SeedProductAsync(5_000);
        var rule = await SaveAsync("عرض الكول سنتر", "ItemFixedPrice", products: new[] { product }, fixedPrice: 4_000,
            orderTypes: new[] { "delivery" }, stations: new[] { "callcenter" });

        await RefusedAsync(await SellAsync(new[] { Line(product, 1, rule.Id) }), "مش متاح للطلب ده");
        await RefusedAsync(await SellAsync(new[] { Line(product, 1, rule.Id) }, "delivery"), "مش متاح للطلب ده");   // the till
        var desk = await SoldAsync(await SellAsync(new[] { Line(product, 1, rule.Id) }, "delivery", "CallCenter"));
        Assert.Equal(4_000 + 3_000, desk.TotalPiastres);
    }

    [Fact]
    public async Task An_offer_outside_its_dates_is_refused_and_not_on_the_menu()
    {
        var product = await _factory.SeedProductAsync(5_000);
        var rule = await SaveAsync("عرض انتهى", "ItemFixedPrice", products: new[] { product }, fixedPrice: 4_000,
            startDate: new DateOnly(2026, 1, 1), endDate: new DateOnly(2026, 1, 31));

        await RefusedAsync(await SellAsync(new[] { Line(product, 1, rule.Id) }), "مش شغال دلوقتي");
        var menu = (await _client.GetFromJsonAsync<MenuResponse>("/api/menu"))!;
        Assert.Empty(menu.Categories.SelectMany(c => c.Products).Single(p => p.Id == product).ApplicableOffers);
    }

    // ---- limits ----

    [Fact]
    public async Task A_per_order_limit_counts_every_line_of_the_offer()
    {
        var product = await _factory.SeedProductAsync(5_000);
        var rule = await SaveAsync("3 بس", "ItemFixedPrice", products: new[] { product }, fixedPrice: 4_000, maxPerOrder: 3);

        await RefusedAsync(await SellAsync(new[] { Line(product, 2, rule.Id), Line(product, 2, rule.Id) }), "3 قطعة بس في الطلب");
        Assert.Equal(3 * 4_000 + 5_000, (await SoldAsync(await SellAsync(new[] { Line(product, 3, rule.Id), Line(product, 1) }))).TotalPiastres);
    }

    [Fact]
    public async Task A_daily_limit_stops_the_offer_once_its_pieces_are_sold_and_hides_it_from_the_menu()
    {
        var product = await _factory.SeedProductAsync(5_000);
        var rule = await SaveAsync("أول 5", "ItemFixedPrice", products: new[] { product }, fixedPrice: 4_000, maxPerDay: 5);

        await SoldAsync(await SellAsync(new[] { Line(product, 4, rule.Id) }));
        await RefusedAsync(await SellAsync(new[] { Line(product, 2, rule.Id) }), "فاضل منه 1");
        await SoldAsync(await SellAsync(new[] { Line(product, 1, rule.Id) }));

        var menu = (await _client.GetFromJsonAsync<MenuResponse>("/api/menu"))!;
        Assert.Empty(menu.Categories.SelectMany(c => c.Products).Single(p => p.Id == product).ApplicableOffers);
    }

    // ---- gifts ----

    private async Task<(string Food, string Cola, PriceRuleDto Gift)> GiftSetupAsync(bool includesDelivery = true)
    {
        var food = await _factory.SeedProductAsync(10_000, "وجبة");
        var cola = await _factory.SeedProductAsync(2_500, "كولا لتر");
        var gift = await SaveAsync("هدية الكولا", "OrderGift", giftIncludesDelivery: includesDelivery,
            tiers: new[] { new GiftTierDto(20_000, cola, null, 1), new GiftTierDto(50_000, cola, null, 2) });
        return (food, cola, gift);
    }

    [Fact]
    public async Task An_order_over_200_gets_one_cola_free_and_the_cola_keeps_its_price_for_the_reports()
    {
        var (food, cola, gift) = await GiftSetupAsync();

        var order = await SoldAsync(await SellAsync(new[] { Line(food, 2), Line(cola, 1, gift.Id) }));

        Assert.Equal(20_000, order.TotalPiastres);
        var giftLine = await _factory.QueryAsync(db => db.OrderItems.FirstAsync(i => i.OrderId == order.OrderId && i.ProductId == cola));
        Assert.Equal((0L, 2_500L, "هدية الكولا"), (giftLine.UnitPricePiastres, giftLine.ListUnitPricePiastres, giftLine.PriceRuleName));
    }

    [Fact]
    public async Task Only_the_highest_tier_is_given_never_both()
    {
        var (food, cola, gift) = await GiftSetupAsync();

        await RefusedAsync(await SellAsync(new[] { Line(food, 5), Line(cola, 3, gift.Id) }), "2 بس");
        await SoldAsync(await SellAsync(new[] { Line(food, 5), Line(cola, 2, gift.Id) }));
        await RefusedAsync(await SellAsync(new[] { Line(food, 4), Line(cola, 2, gift.Id) }), "1 بس");   // 400 reaches the 200 tier only
    }

    [Fact]
    public async Task An_order_below_the_first_tier_cannot_take_the_gift()
    {
        var (food, cola, gift) = await GiftSetupAsync();
        await RefusedAsync(await SellAsync(new[] { Line(food, 1), Line(cola, 1, gift.Id) }), "ما وصلش لمبلغ هدية");
    }

    [Fact]
    public async Task The_delivery_fee_counts_toward_the_gift_only_when_the_offer_says_so()
    {
        // 190 of food + 30 delivery = 220.
        var (food, cola, withFee) = await GiftSetupAsync(includesDelivery: true);
        var cheap = await _factory.SeedProductAsync(19_000, "وجبة 190");
        await SoldAsync(await SellAsync(new[] { Line(cheap), Line(cola, 1, withFee.Id) }, "delivery"));

        var withoutFee = await SaveAsync("هدية من غير التوصيل", "OrderGift", giftIncludesDelivery: false,
            tiers: new[] { new GiftTierDto(20_000, cola, null, 1) });
        await RefusedAsync(await SellAsync(new[] { Line(cheap), Line(cola, 1, withoutFee.Id) }, "delivery"), "ما وصلش");
    }

    [Fact]
    public async Task The_menu_lists_the_gift_offer_with_each_tier_named()
    {
        var (_, cola, gift) = await GiftSetupAsync();
        var menu = (await _client.GetFromJsonAsync<MenuResponse>("/api/menu"))!;

        var offered = Assert.Single(menu.GiftOffers!, g => g.Id == gift.Id);
        Assert.Equal(new long[] { 20_000, 50_000 }, offered.Tiers.Select(t => t.ThresholdPiastres));
        Assert.All(offered.Tiers, t => Assert.Equal(("كولا لتر", cola), (t.ProductName, t.ProductId)));
        Assert.Equal(2, offered.TierFor(60_000)!.Quantity);
    }
}
