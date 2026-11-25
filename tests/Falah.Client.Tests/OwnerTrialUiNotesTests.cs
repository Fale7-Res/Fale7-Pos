using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Users;
using Xunit;
using Cashier = Falah.Client.CashierWinUI;
using CallCenter = Falah.Client.CallCenterWinUI;

namespace Falah.Client.Tests;

/// <summary>
/// The owner's trial on a copy of the restaurant's data (2026-11-25), on the release candidate 1a25612:
/// 1. the order queue said "لا توجد طلبات توصيل جارية" on top of two delivered orders waiting for their driver's
///    settlement - the empty state read the old dispatch board (which no longer lists them), not the queue it sits under;
/// 2. an order handed to one driver could not be given to another from either app - the server has done it all along
///    (POST /api/delivery/{id}/reassign) but no screen called it.
/// Both apps, same rules.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class OwnerTrialUiNotesTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-trial-notes-" + Guid.NewGuid().ToString("N"));
    private readonly Cashier.Services.IRetainedRequestStore? _previousCashier = Cashier.Services.RetainedRequestStorage.Current;
    private readonly CallCenter.Services.IRetainedRequestStore? _previousCallCenter = CallCenter.Services.RetainedRequestStorage.Current;

    public OwnerTrialUiNotesTests()
    {
        Cashier.Services.RetainedRequestStorage.Current = new Cashier.Services.FileRetainedRequestStore(Path.Combine(_dir, "cashier"));
        CallCenter.Services.RetainedRequestStorage.Current = new CallCenter.Services.FileRetainedRequestStore(Path.Combine(_dir, "callcenter"));
    }

    public void Dispose()
    {
        Cashier.Services.RetainedRequestStorage.Current = _previousCashier;
        CallCenter.Services.RetainedRequestStorage.Current = _previousCallCenter;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
    private static HttpResponseMessage Json(object value) =>
        new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(value, Web), Encoding.UTF8, "application/json") };

    private static QueueOrderDto Card(string id, string stage, string? next, string? driverId, string? driverName, string deliveryStatus) =>
        new(id, "#" + id, "delivery", "عميل " + id, "0100", "المنطقة", "شارع 9", 7_500, "cash", "cash_on_delivery",
            stage, "مرحلة " + stage, next, next is null ? null : "خطوة " + next, false, false, true,
            "delivery-" + id, deliveryStatus, driverId, driverName, 7_500, false, null,
            DateTimeOffset.UtcNow, null, null, null, false);

    private static DeliveryDto Row(string id, string status, string? driverId, string? driverName) =>
        new("delivery-" + id, id, "#" + id, status, driverId, driverName, 7_500, 7_500, "cash", false, "shift", DateTimeOffset.UtcNow);

    private static readonly DriverDto Sayed = new("drv-1", "سيد عبد الله", true);
    private static readonly DriverDto Mahmoud = new("drv-2", "محمود السائق", true);

    private sealed record Sent(string Path, JsonElement Body);

    /// <summary>A fake server: the queue, the dispatch board and the drivers as given; every write recorded.</summary>
    private static HttpClient Server(IReadOnlyList<QueueOrderDto> queue, IReadOnlyList<DeliveryDto> board, List<Sent> sent,
        Func<string, int, HttpResponseMessage?>? answer = null)
    {
        var writes = 0;
        return new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Get)
                return path switch
                {
                    "/api/orders/queue" => Json(queue),
                    "/api/delivery/board" => Json(board),
                    "/api/delivery/drivers" => Json(new[] { Sayed, Mahmoud }),
                    _ => Json(Array.Empty<object>()),
                };
            sent.Add(new Sent(path, JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone()));
            if (answer?.Invoke(path, ++writes) is { } custom) return custom;
            if (path.EndsWith("/reassign"))
                return Json(new ReassignDeliveryResult("delivery-o1", Mahmoud.Id, Mahmoud.Name, false, false));
            return Json(new { });
        })) { BaseAddress = new Uri("http://fixture.invalid") };
    }

    private static readonly string[] Dispatcher = { "pos", "delivery_manage" };

    private static Cashier.ViewModels.DeliveryViewModel CashierQueue(HttpClient http, params string[] permissions) =>
        new(new Cashier.Services.AppSession(new Cashier.Services.ApiClient(http))
        { CurrentUser = new UserSummaryDto("u1", "أحمد الكاشير", "cashier", permissions.Length == 0 ? Dispatcher : permissions), ShiftId = "shift" });

    private static CallCenter.ViewModels.DeliveryViewModel CallCenterQueue(HttpClient http, params string[] permissions) =>
        new(new CallCenter.Services.AppSession(new CallCenter.Services.ApiClient(http))
        { CurrentUser = new UserSummaryDto("u1", "موظف الكول سنتر", "cashier", permissions.Length == 0 ? Dispatcher : permissions) });

    // ---------------- 1. the empty state follows the queue on screen ----------------

    [Fact]
    public async Task Till_queue_with_only_delivered_orders_awaiting_settlement_does_not_say_there_are_no_deliveries()
    {
        var vm = CashierQueue(Server(new[] { Card("o1", "delivered", "settle_driver", Sayed.Id, Sayed.Name, "Delivered") },
            Array.Empty<DeliveryDto>(), new()));

        await vm.LoadAsync();

        Assert.Single(vm.DeliveryStages);
        Assert.False(vm.HasNoDeliveries);
    }

    [Fact]
    public async Task Call_center_queue_with_only_delivered_orders_awaiting_settlement_does_not_say_there_are_no_deliveries()
    {
        var vm = CallCenterQueue(Server(new[] { Card("o1", "delivered", "settle_driver", Sayed.Id, Sayed.Name, "Delivered") },
            Array.Empty<DeliveryDto>(), new()));

        await vm.LoadAsync();

        Assert.Single(vm.DeliveryStages);
        Assert.False(vm.HasNoDeliveries);
    }

    [Fact]
    public async Task An_empty_queue_still_says_there_are_no_deliveries()
    {
        var till = CashierQueue(Server(Array.Empty<QueueOrderDto>(), Array.Empty<DeliveryDto>(), new()));
        var desk = CallCenterQueue(Server(Array.Empty<QueueOrderDto>(), Array.Empty<DeliveryDto>(), new()));

        await till.LoadAsync();
        await desk.LoadAsync();

        Assert.True(till.HasNoDeliveries);
        Assert.True(desk.HasNoDeliveries);
    }

    // ---------------- 2. an order out with one driver goes to another ----------------

    private static QueueOrderDto OutWithSayed => Card("o1", "out", "record_result", Sayed.Id, Sayed.Name, "OutForDelivery");
    private static DeliveryDto OutWithSayedRow => Row("o1", "OutForDelivery", Sayed.Id, Sayed.Name);

    [Fact]
    public async Task Till_moves_an_order_out_with_one_driver_to_another_with_a_reason()
    {
        var sent = new List<Sent>();
        var vm = CashierQueue(Server(new[] { OutWithSayed }, new[] { OutWithSayedRow }, sent));
        await vm.LoadAsync();
        var card = vm.DeliveryStages.Single().Cards.Single();
        Assert.True(card.CanReassign);

        vm.OpenReassignCommand.Execute(card);
        Assert.True(vm.IsReassignOpen);
        Assert.Equal(new[] { Mahmoud.Id }, vm.ReassignDriverOptions.Select(d => d.Id)); // not the driver who has it

        vm.ReassignDriver = vm.ReassignDriverOptions.Single();
        await vm.ConfirmReassignCommand.ExecuteAsync(null);
        Assert.NotNull(vm.ReassignError);                                                // the server needs a reason
        Assert.Empty(sent);

        vm.ReassignReason = "السائق الأول عربيته عطلت";
        await vm.ConfirmReassignCommand.ExecuteAsync(null);

        var write = Assert.Single(sent);
        Assert.Equal("/api/delivery/delivery-o1/reassign", write.Path);
        Assert.Equal(Mahmoud.Id, write.Body.GetProperty("newDriverId").GetString());
        Assert.Equal("السائق الأول عربيته عطلت", write.Body.GetProperty("reason").GetString());
        Assert.Equal("u1", write.Body.GetProperty("actorUserId").GetString());
        Assert.False(vm.IsReassignOpen);
        Assert.Contains(Mahmoud.Name, vm.ActionNote);
    }

    [Fact]
    public async Task Call_center_moves_an_order_out_with_one_driver_to_another_with_a_reason()
    {
        var sent = new List<Sent>();
        var vm = CallCenterQueue(Server(new[] { OutWithSayed }, new[] { OutWithSayedRow }, sent));
        await vm.LoadAsync();
        var card = vm.DeliveryStages.Single().Cards.Single();
        Assert.True(card.CanReassign);

        vm.OpenReassignCommand.Execute(card);
        Assert.Equal(new[] { Mahmoud.Id }, vm.ReassignDriverOptions.Select(d => d.Id));
        vm.ReassignDriver = vm.ReassignDriverOptions.Single();
        vm.ReassignReason = "تغيير خط السير";
        await vm.ConfirmReassignCommand.ExecuteAsync(null);

        var write = Assert.Single(sent);
        Assert.Equal("/api/delivery/delivery-o1/reassign", write.Path);
        Assert.Equal(Mahmoud.Id, write.Body.GetProperty("newDriverId").GetString());
        Assert.False(vm.IsReassignOpen);
        Assert.Contains(Mahmoud.Name, vm.ActionNote);
    }

    [Fact]
    public async Task Only_a_dispatcher_is_offered_the_move_and_only_for_an_order_that_is_out()
    {
        var queue = new[] { OutWithSayed, Card("o2", "ready", "assign", null, null, "Ready") };
        var board = new[] { OutWithSayedRow, Row("o2", "Ready", null, null) };
        var dispatcher = CashierQueue(Server(queue, board, new()));
        var tillOnly = CashierQueue(Server(queue, board, new()), "pos");
        var desk = CallCenterQueue(Server(queue, board, new()), "pos");
        await dispatcher.LoadAsync(); await tillOnly.LoadAsync(); await desk.LoadAsync();

        Assert.Equal(new[] { false, true }, dispatcher.DeliveryStages.SelectMany(s => s.Cards).OrderBy(c => c.OrderId == "o1").Select(c => c.CanReassign));
        Assert.All(tillOnly.DeliveryStages.SelectMany(s => s.Cards), c => Assert.False(c.CanReassign));
        Assert.All(desk.DeliveryStages.SelectMany(s => s.Cards), c => Assert.False(c.CanReassign));
    }

    [Fact]
    public async Task A_move_whose_answer_was_lost_is_resent_with_the_same_request_id_and_never_becomes_a_second_move()
    {
        var sent = new List<Sent>();
        var vm = CashierQueue(Server(new[] { OutWithSayed }, new[] { OutWithSayedRow }, sent,
            (path, n) => path.EndsWith("/reassign") && n == 1 ? throw new HttpRequestException("network lost") : null));
        await vm.LoadAsync();
        vm.OpenReassignCommand.Execute(vm.DeliveryStages.Single().Cards.Single());
        vm.ReassignDriver = vm.ReassignDriverOptions.Single();
        vm.ReassignReason = "عطل";

        await vm.ConfirmReassignCommand.ExecuteAsync(null);   // the answer never came back
        Assert.True(vm.IsReassignOpen);
        Assert.NotNull(vm.ReassignError);
        Assert.False(vm.CanEditAction);                        // the screen holds until the outcome is known

        vm.ReassignReason = "سبب تاني";                         // the retry resends what was sent, not the edited form
        await vm.ConfirmReassignCommand.ExecuteAsync(null);

        Assert.Equal(2, sent.Count);
        Assert.Equal(sent[0].Body.GetProperty("requestId").GetString(), sent[1].Body.GetProperty("requestId").GetString());
        Assert.Equal("عطل", sent[1].Body.GetProperty("reason").GetString());
        Assert.False(vm.IsReassignOpen);
        Assert.True(vm.CanEditAction);
    }
}
