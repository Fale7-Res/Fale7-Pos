using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Falah.Application.Menu;
using Falah.Application.Pricing;
using Falah.Application.Users;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;

namespace Falah.Client.Tests;

/// <summary>Owner, 2026-11-25: the offers screen saves every offer shape with its conditions; each kind asks for what
/// it needs and only that.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class OfferEditorTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-offer-editor-" + Guid.NewGuid().ToString("N"));
    private readonly Cashier.Services.IRetainedRequestStore? _previous = Cashier.Services.RetainedRequestStorage.Current;
    public OfferEditorTests() => Cashier.Services.RetainedRequestStorage.Current = new Cashier.Services.FileRetainedRequestStore(_dir);
    public void Dispose()
    {
        Cashier.Services.RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, Web), Encoding.UTF8, "application/json") };

    private static async Task<(Cashier.ViewModels.MenuEditorViewModel Vm, List<SavePriceRuleRequest> Sent)> EditorAsync()
    {
        var sent = new List<SavePriceRuleRequest>();
        var categories = new[] { new CategoryDetailsDto("c-drinks", "مشروبات", null, 1, true) };
        var products = new[]
        {
            new ProductDetailsDto("cola", "c-drinks", "كولا", 2_500, false, null, true, 1, null,
                new[] { new ProductVariantDetailsDto("v-1l", "لتر", 2_500, null), new ProductVariantDetailsDto("v-2l", "2 لتر", 4_000, null) }),
            new ProductDetailsDto("kofta", "c-drinks", "كفتة", 9_000, false, null, true, 2, null, Array.Empty<ProductVariantDetailsDto>()),
        };
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Post && path == "/api/price-rules")
            {
                var body = (await r.Content!.ReadFromJsonAsync<SavePriceRuleRequest>(Web))!;
                sent.Add(body);
                return Json(new PriceRuleDto("r1", body.Name, body.ProductIds, body.FixedPricePiastres, null, null, null, true));
            }
            return path switch
            {
                "/api/categories/all" => Json(categories),
                "/api/products/all" => Json(products),
                _ => Json(Array.Empty<object>()),
            };
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new Cashier.Services.AppSession(new Cashier.Services.ApiClient(http))
        { CurrentUser = new UserSummaryDto("owner", "المالك", "owner", new[] { "all" }) };
        var vm = new Cashier.ViewModels.MenuEditorViewModel(session);
        await vm.OpenAsync();
        vm.SetKindCommand.Execute("prices");
        vm.NewCommand.Execute(null);
        return (vm, sent);
    }

    [Fact]
    public async Task A_gift_offer_is_saved_with_its_tiers_sizes_and_options()
    {
        var (vm, sent) = await EditorAsync();
        Assert.True(vm.IsReady, vm.LoadError);
        vm.Name = "هدية الكولا";
        vm.OfferKind = vm.OfferKinds.Single(k => k.Id == "OrderGift");
        Assert.True(vm.IsOfferGift);
        Assert.False(vm.HasPrice);          // a gift has no price field
        Assert.False(vm.IsOfferOnItems);

        vm.AddGiftTierCommand.Execute(null);
        var tier = vm.GiftTiers.Single();
        tier.ThresholdText = "200";
        tier.Product = vm.GiftProductChoices.Single(p => p.Id == "cola");
        Assert.True(tier.HasVariants);
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Empty(sent);                  // two sizes: the size must be chosen
        Assert.Contains("حجم", vm.ActionError);

        tier.Variant = tier.VariantChoices.Single(v => v.Id == "v-1l");
        vm.GiftIncludesDelivery = false;
        vm.GiftAutoAdd = true;
        vm.OfferDelivery = true;
        vm.MaxPerDayText = "30";
        await vm.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(sent);
        Assert.Equal(("OrderGift", false, true, 30), (request.Kind, request.GiftIncludesDelivery, request.GiftAutoAdd, request.MaxPerDay));
        Assert.Equal(new GiftTierDto(20_000, "cola", "v-1l", 1), Assert.Single(request.GiftTiers!));
        Assert.Equal(new[] { "delivery" }, request.OrderTypes);
        Assert.Null(request.Stations);
    }

    [Fact]
    public async Task A_category_percentage_needs_a_category_and_a_percentage()
    {
        var (vm, sent) = await EditorAsync();
        vm.Name = "خصم المشروبات";
        vm.OfferKind = vm.OfferKinds.Single(k => k.Id == "CategoryPercent");
        vm.PercentText = "10";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Empty(sent);
        Assert.Contains("قسم", vm.ActionError);

        vm.PriceCategories.Single().Selected = true;
        vm.StartDateText = "2026-12-01";
        vm.EndDateText = "2026-12-31";
        vm.OfferTill = true;
        await vm.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(sent);
        Assert.Equal(("CategoryPercent", 1_000), (request.Kind, request.PercentBasisPoints));
        Assert.Equal(new[] { "c-drinks" }, request.CategoryIds);
        Assert.Empty(request.ProductIds);
        Assert.Equal((new DateOnly(2026, 12, 1), new DateOnly(2026, 12, 31)), (request.StartDate, request.EndDate));
        Assert.Equal(new[] { "till" }, request.Stations);
    }

    [Fact]
    public async Task A_bad_date_or_limit_or_half_a_time_window_is_refused_before_sending()
    {
        var (vm, sent) = await EditorAsync();
        vm.Name = "عرض";
        vm.PriceText = "40";
        vm.PriceProducts.First(p => p.Id == "kofta").Selected = true;

        vm.StartDateText = "1/12/2026";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("2026-12-31", vm.ActionError);
        vm.StartDateText = "";

        vm.MaxPerOrderText = "0";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("الحد الأقصى", vm.ActionError);
        vm.MaxPerOrderText = "";

        vm.StartTimeText = "22:00";
        await vm.SaveCommand.ExecuteAsync(null);
        Assert.Contains("البداية والنهاية", vm.ActionError);
        vm.EndTimeText = "02:00";
        await vm.SaveCommand.ExecuteAsync(null);

        var request = Assert.Single(sent);
        Assert.Equal(("ItemFixedPrice", 4_000L, new TimeOnly(22, 0), new TimeOnly(2, 0)), (request.Kind, request.FixedPricePiastres, request.StartTime!.Value, request.EndTime!.Value));
    }
}
