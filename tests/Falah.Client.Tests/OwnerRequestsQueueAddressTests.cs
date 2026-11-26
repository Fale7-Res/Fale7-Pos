using System.Net;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Falah.Application.Customers;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Users;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;
using CallCenter = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

/// <summary>
/// Owner's requests, 2026-11-25:
/// 1. a saved address shows in full - floor, apartment and all - in the list it is picked from (both apps) and on the
///    customers page; picking one fills every field;
/// 2. «ترحيل»: the queue's screen is cleared of finished orders for the next shift - delivery, pickup or both - with a
///    warning for the orders still in progress, which stay until they finish. Nothing is deleted.
/// </summary>
public sealed class OwnerRequestsQueueAddressTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, Web), Encoding.UTF8, "application/json") };

    // ---------------- 1. the address in full ----------------

    private static readonly CustomerAddressDto Home = new("addr-1", "مج2", "ورقة", "13د", "1", "4", "جنب الجامع", "svc-1", "توصيل",
        DateTimeOffset.UtcNow, DateTimeOffset.UtcNow);

    [Fact]
    public void A_saved_address_reads_in_full_with_floor_and_apartment()
    {
        Assert.Equal("المنطقة مج2، ش ورقة، عمارة 13د، دور 1، شقة 4 — جنب الجامع", Home.FullText);
        // Two addresses on the same street are told apart by what differs.
        Assert.NotEqual(Home.FullText, (Home with { Floor = "3", Apartment = "9" }).FullText);
    }

    [Fact]
    public void The_full_text_is_not_part_of_the_wire_contract() =>
        Assert.DoesNotContain("fullText", JsonSerializer.Serialize(Home, Web));

    private static string Source(params string[] parts)
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "Falah.sln"))) dir = dir.Parent;
        Assert.NotNull(dir);
        return Path.Combine(new[] { dir!.FullName, "src" }.Concat(parts).ToArray());
    }

    private static XElement SavedAddressBox(string file) =>
        Assert.Single(XDocument.Load(file).Descendants(), e => e.Name.LocalName == "ComboBox" && (string?)e.Attribute("Header") == "عنوان محفوظ");

    [Fact]
    public void Both_apps_list_each_saved_address_in_full()
    {
        Assert.Equal("FullText", (string?)SavedAddressBox(Source("Falah.Client.CashierWinUI", "Views", "DeliveryDetailsView.xaml")).Attribute("DisplayMemberPath"));
        Assert.Equal("FullText", (string?)SavedAddressBox(Source("Falah.Client.CallCenterWinUI", "Views", "CallCenterDeliveryEditorView.xaml")).Attribute("DisplayMemberPath"));
    }

    [Fact]
    public void The_customers_page_shows_each_address_in_full() =>
        Assert.Contains(XDocument.Load(Source("Falah.Client.CashierWinUI", "Views", "CustomersView.xaml")).Descendants(),
            e => e.Name.LocalName == "TextBlock" && ((string?)e.Attribute("Text"))?.Contains("FullText") == true);

    [Fact]
    public void Picking_a_saved_address_fills_every_field_in_both_apps()
    {
        var till = new Cashier.Services.AppSession(new Cashier.Services.ApiClient(new HttpClient { BaseAddress = new Uri("http://x.invalid") }))
        { CurrentUser = new UserSummaryDto("u", "كاشير", "cashier", new[] { "pos" }), ShiftId = "s" }.GetCashier(new());
        till.SavedAddresses.Add(Home);
        till.SelectedSavedAddress = Home;
        Assert.Equal(("مج2", "ورقة", "13د", "1", "4", "جنب الجامع"),
            (till.DeliveryArea, till.DeliveryStreet, till.DeliveryBuilding, till.DeliveryFloor, till.DeliveryApartment, till.DeliveryLandmark));

        var desk = new CallCenter.ViewModels.CallCenterViewModel(new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient(
            new HttpClient { BaseAddress = new Uri("http://x.invalid") })) { CurrentUser = new UserSummaryDto("u", "كول", "cashier", new[] { "pos" }) }, new());
        desk.SavedAddresses.Add(Home);
        desk.SelectedSavedAddress = Home;
        Assert.Equal(("مج2", "ورقة", "13د", "1", "4", "جنب الجامع"),
            (desk.DeliveryArea, desk.DeliveryStreet, desk.DeliveryBuilding, desk.DeliveryFloor, desk.DeliveryApartment, desk.DeliveryLandmark));
    }

    // ---------------- 2. «ترحيل» ----------------

    private static QueueOrderDto Order(string id, string type, string stage, bool needsRefund = false) =>
        new(id, "#" + id, type, "عميل", "0100", null, null, 5_000, "cash", "paid", stage, stage, null, null, false, false, true,
            null, null, null, null, 0, needsRefund, null, DateTimeOffset.UtcNow, null, null, null, false);

    private static readonly QueueOrderDto[] Queue =
    {
        Order("d1", "delivery", "delivered"),
        Order("d2", "delivery", "out"),
        Order("d3", "delivery", "failed", needsRefund: true),   // still owes the customer a refund: not finished
        Order("p1", "pickup", "completed"),
        Order("p2", "pickup", "preparing"),
    };

    private sealed record Sent(string Path, JsonElement Body);

    private static HttpClient Server(List<Sent> sent, Func<int, HttpResponseMessage?>? answer = null)
    {
        var writes = 0;
        return new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Get)
                return path == "/api/orders/queue" ? Json(Queue) : Json(Array.Empty<object>());
            sent.Add(new Sent(path, JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone()));
            if (answer?.Invoke(++writes) is { } custom) return custom;
            return Json(new ClearOrderQueueResult(1, 2, false));
        })) { BaseAddress = new Uri("http://fixture.invalid") };
    }

    private static readonly string[] Dispatcher = { "pos", "delivery_manage" };

    private static Cashier.ViewModels.DeliveryViewModel Till(HttpClient http) =>
        new(new Cashier.Services.AppSession(new Cashier.Services.ApiClient(http))
        { CurrentUser = new UserSummaryDto("u1", "أحمد", "cashier", Dispatcher), ShiftId = "shift" });

    private static CallCenter.ViewModels.DeliveryViewModel Desk(HttpClient http) =>
        new(new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient(http))
        { CurrentUser = new UserSummaryDto("u1", "كول سنتر", "cashier", Dispatcher) });

    [Fact]
    public async Task Till_clear_counts_what_leaves_warns_about_what_stays_and_sends_the_chosen_kinds()
    {
        var sent = new List<Sent>();
        var vm = Till(Server(sent));
        await vm.LoadAsync();

        vm.OpenQueueClearCommand.Execute(null);
        Assert.True(vm.IsQueueClearOpen);
        Assert.Equal((2, 3), (vm.QueueClearFinishedCount, vm.QueueClearInProgressCount));
        Assert.True(vm.HasQueueClearWarning);
        Assert.Contains("3", vm.QueueClearWarning);

        vm.QueueClearPickups = false;
        Assert.Equal((1, 2), (vm.QueueClearFinishedCount, vm.QueueClearInProgressCount));
        await vm.ConfirmQueueClearCommand.ExecuteAsync(null);

        var post = Assert.Single(sent);
        Assert.Equal("/api/orders/queue/clear", post.Path);
        Assert.True(post.Body.GetProperty("deliveries").GetBoolean());
        Assert.False(post.Body.GetProperty("pickups").GetBoolean());
        Assert.StartsWith("qclr-", post.Body.GetProperty("requestId").GetString());
        Assert.False(vm.IsQueueClearOpen);
        Assert.Contains("1 طلب خلص", vm.ActionNote);
        Assert.Contains("فضل 2", vm.ActionNote);
    }

    [Fact]
    public async Task Call_center_clear_works_the_same()
    {
        var sent = new List<Sent>();
        var vm = Desk(Server(sent));
        await vm.LoadAsync();

        vm.OpenQueueClearCommand.Execute(null);
        Assert.Equal((2, 3), (vm.QueueClearFinishedCount, vm.QueueClearInProgressCount));
        await vm.ConfirmQueueClearCommand.ExecuteAsync(null);

        Assert.Equal("/api/orders/queue/clear", Assert.Single(sent).Path);
        Assert.False(vm.IsQueueClearOpen);
    }

    [Fact]
    public async Task Nothing_chosen_sends_nothing()
    {
        var sent = new List<Sent>();
        var vm = Till(Server(sent));
        await vm.LoadAsync();
        vm.OpenQueueClearCommand.Execute(null);
        vm.QueueClearDeliveries = false;
        vm.QueueClearPickups = false;

        await vm.ConfirmQueueClearCommand.ExecuteAsync(null);

        Assert.Empty(sent);
        Assert.NotNull(vm.QueueClearError);
        Assert.True(vm.IsQueueClearOpen);
    }

    [Fact]
    public async Task A_lost_answer_keeps_the_window_open_and_the_retry_sends_the_same_request_id()
    {
        var sent = new List<Sent>();
        var vm = Till(Server(sent, n => n == 1 ? new HttpResponseMessage(HttpStatusCode.InternalServerError) : null));
        await vm.LoadAsync();
        vm.OpenQueueClearCommand.Execute(null);

        await vm.ConfirmQueueClearCommand.ExecuteAsync(null);
        Assert.True(vm.IsQueueClearOpen);
        Assert.NotNull(vm.QueueClearError);
        await vm.ConfirmQueueClearCommand.ExecuteAsync(null);

        Assert.Equal(2, sent.Count);
        Assert.Equal(sent[0].Body.GetProperty("requestId").GetString(), sent[1].Body.GetProperty("requestId").GetString());
        Assert.False(vm.IsQueueClearOpen);
    }

    [Fact]
    public async Task A_delivered_order_offers_no_settlement_step_on_the_queue()
    {
        var vm = Till(Server(new()));
        await vm.LoadAsync();

        var delivered = vm.DeliveryStages.Single(s => s.Key == "delivered");
        Assert.Equal("اتسلّم للعميل (1)", delivered.Header);
        Assert.False(delivered.Cards.Single().HasNextAction);
    }

    [Fact]
    public void Both_apps_have_the_clear_button_and_window()
    {
        foreach (var app in new[] { "Falah.Client.CashierWinUI", "Falah.Client.CallCenterWinUI" })
        {
            var view = XDocument.Load(Source(app, "Views", "DeliveryView.xaml")).Descendants().ToList();
            Assert.Contains(view, e => (string?)e.Attribute("Content") == "ترحيل" && (string?)e.Attribute("Command") == "{Binding OpenQueueClearCommand}");
            var modals = XDocument.Load(Source(app, "Views", "DeliveryModalsView.xaml")).Descendants().ToList();
            Assert.Contains(modals, e => e.Name.LocalName == "CheckBox" && (string?)e.Attribute("Content") == "التوصيل");
            Assert.Contains(modals, e => e.Name.LocalName == "CheckBox" && (string?)e.Attribute("Content") == "الاستلام من الفرع");
            Assert.Contains(modals, e => (string?)e.Attribute("Content") == "إزالة من الواجهة");
        }
    }
}
