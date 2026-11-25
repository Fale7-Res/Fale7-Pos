using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// Workflow redesign (owner decisions 2026-11-24) on the till's order queue: orders are grouped by the stage the server
/// gives them; the next step is the server's; cancelling an unpaid order needs no PIN before the kitchen has its paper
/// and a manager's PIN plus the prepared answer after it; a prepaid pickup is handed over with one press (a retry reuses
/// its request id); an unpaid one is paid and handed over in one step; a delivery offers no card.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class OrderQueueScreenTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-queue-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public OrderQueueScreenTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

    public void Dispose()
    {
        RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static QueueOrderDto Q(string id, string type, string stage, string? next, string payment = "paid",
        bool canCancel = false, bool needsAdmin = false, long total = 12_000) =>
        new(id, "#" + id, type, "عميل " + id, "0100", type == "delivery" ? "المنطقة" : null, null, total, "cash", payment,
            stage, "مرحلة " + stage, next, next is null ? null : "خطوة " + next, canCancel, needsAdmin, needsAdmin,
            type == "delivery" ? "delivery-" + id : null, type == "delivery" ? "Ready" : null, null, null, 0, false, null,
            DateTimeOffset.UtcNow, null, null, null, payment == "pay_at_counter");

    private sealed record Sent(string Path, JsonElement Body);

    private static (DeliveryViewModel Vm, List<Sent> Sent) Screen(IReadOnlyList<QueueOrderDto> queue, string role = "cashier",
        Func<string, HttpResponseMessage?>? answer = null, params string[] permissions)
    {
        var sent = new List<Sent>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Get)
                return path == "/api/orders/queue" ? Json(JsonSerializer.Serialize(queue, new JsonSerializerOptions(JsonSerializerDefaults.Web))) : Json("[]");
            sent.Add(new Sent(path, JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone()));
            if (answer?.Invoke(path) is { } custom) return custom;
            if (path == "/api/approvals/issue")
                return Json("""{"approvalId":"apr-1","approverUserId":"mgr","approverName":"المدير","action":"x","approvedAtUtc":"2026-11-24T10:00:00Z"}""");
            if (path == "/api/orders/hand-over-pickup")
                return Json("""{"orderId":"p1","orderNumber":"#p1","handedOverAtUtc":"2026-11-24T10:00:00Z","wasReplayed":false,"printJobsQueued":true}""");
            if (path == "/api/orders/collect-pickup")
                return Json("""{"orderId":"p1","orderNumber":"#p1","sequenceNumber":1,"totalPiastres":12000,"amountPaidPiastres":15000,"changeDuePiastres":3000,"status":"completed","isPaid":true,"wasReplayed":false}""");
            return Json("{}");
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http))
        { CurrentUser = new("u1", "المستخدم", role, permissions.Length == 0 ? new[] { "pos", "delivery_manage" } : permissions), ShiftId = "shift" };
        return (new DeliveryViewModel(session), sent);
    }

    [Fact]
    public async Task Orders_are_grouped_by_the_servers_stage_in_the_order_of_their_life()
    {
        var (vm, _) = Screen(new[]
        {
            Q("d2", "delivery", "ready", "assign"), Q("d1", "delivery", "new", "print_kitchen"),
            Q("p1", "pickup", "preparing", "collect", "pay_at_counter"), Q("p2", "pickup", "completed", null),
        });

        await vm.LoadAsync();

        Assert.Equal(new[] { "new", "ready" }, vm.DeliveryStages.Select(s => s.Key));
        Assert.Equal(new[] { "preparing", "completed" }, vm.PickupStages.Select(s => s.Key));
        Assert.Equal("خطوة collect", vm.PickupStages[0].Cards.Single().NextActionLabel);
        Assert.Equal("يدفع وهو بيستلم", vm.PickupStages[0].Cards.Single().PaymentLabel);
        Assert.False(vm.PickupStages[1].Cards.Single().HasNextAction);
    }

    [Fact]
    public async Task Cancelling_before_the_kitchen_paper_sends_no_pin_and_gives_the_stock_back()
    {
        var (vm, sent) = Screen(new[] { Q("p1", "pickup", "new", "print_kitchen", "pay_at_counter", canCancel: true) });
        await vm.LoadAsync();
        var card = vm.PickupStages[0].Cards.Single();

        vm.OpenQueueCancelCommand.Execute(card);
        Assert.False(vm.QueueCancelNeedsAdmin);
        vm.QueueCancelReason = "العميل لغى";
        await vm.ConfirmQueueCancelCommand.ExecuteAsync(null);

        var cancel = Assert.Single(sent, s => s.Path == "/api/orders/cancel-uncollected-pickup");
        Assert.Equal("not_prepared", cancel.Body.GetProperty("stockDisposition").GetString());
        Assert.DoesNotContain(sent, s => s.Path == "/api/approvals/issue");
        Assert.Null(vm.QueueCancelTarget);
    }

    [Fact]
    public async Task Cancelling_after_the_kitchen_paper_needs_the_managers_pin_and_the_prepared_answer()
    {
        var (vm, sent) = Screen(new[] { Q("d1", "delivery", "preparing", "mark_ready", "cash_on_delivery", canCancel: true, needsAdmin: true) });
        await vm.LoadAsync();
        vm.OpenQueueCancelCommand.Execute(vm.DeliveryStages[0].Cards.Single());
        vm.QueueCancelReason = "العميل لغى";

        await vm.ConfirmQueueCancelCommand.ExecuteAsync(null);
        Assert.NotNull(vm.QueueCancelError);                               // the prepared answer is missing
        vm.IsQueueCancelPrepared = true;
        await vm.ConfirmQueueCancelCommand.ExecuteAsync(null);
        Assert.Contains("رمز المدير", vm.QueueCancelError);                // the PIN is missing
        Assert.DoesNotContain(sent, s => s.Path == "/api/orders/cancel-undispatched-delivery");

        vm.QueueCancelManagerPin = "4321";
        await vm.ConfirmQueueCancelCommand.ExecuteAsync(null);

        Assert.Equal("delivery_override", Assert.Single(sent, s => s.Path == "/api/approvals/issue").Body.GetProperty("action").GetString());
        var cancel = Assert.Single(sent, s => s.Path == "/api/orders/cancel-undispatched-delivery");
        Assert.Equal("prepared", cancel.Body.GetProperty("stockDisposition").GetString());
        Assert.Equal("apr-1", cancel.Body.GetProperty("managerApprovalId").GetString());
    }

    [Fact]
    public async Task A_prepaid_pickup_is_handed_over_with_one_press_and_a_retry_reuses_its_request_id()
    {
        var lost = true;
        var (vm, sent) = Screen(new[] { Q("p1", "pickup", "preparing", "hand_over") },
            answer: path => path == "/api/orders/hand-over-pickup" && lost ? new HttpResponseMessage(HttpStatusCode.BadGateway) : null);
        await vm.LoadAsync();
        var card = vm.PickupStages[0].Cards.Single();

        await vm.RunNextActionCommand.ExecuteAsync(card);
        lost = false;
        await vm.RunNextActionCommand.ExecuteAsync(card);

        var presses = sent.Where(s => s.Path == "/api/orders/hand-over-pickup").Select(s => s.Body.GetProperty("requestId").GetString()).ToList();
        Assert.Equal(2, presses.Count);
        Assert.Equal(presses[0], presses[1]);
        Assert.Contains("اتسلّم", vm.ActionNote);
    }

    [Fact]
    public async Task An_unpaid_pickup_is_paid_and_handed_over_in_one_step_and_too_little_cash_is_refused()
    {
        var (vm, sent) = Screen(new[] { Q("p1", "pickup", "preparing", "collect", "pay_at_counter") });
        await vm.LoadAsync();
        await vm.RunNextActionCommand.ExecuteAsync(vm.PickupStages[0].Cards.Single());
        Assert.True(vm.IsCollectOpen);

        vm.CollectTenderText = "100";
        await vm.ConfirmCollectCommand.ExecuteAsync(null);
        Assert.NotNull(vm.CollectError);
        Assert.DoesNotContain(sent, s => s.Path == "/api/orders/collect-pickup");

        vm.CollectTenderText = "150";
        await vm.ConfirmCollectCommand.ExecuteAsync(null);
        var collect = Assert.Single(sent, s => s.Path == "/api/orders/collect-pickup");
        Assert.Equal(15_000, collect.Body.GetProperty("amountPaidPiastres").GetInt64());
        Assert.False(vm.IsCollectOpen);
        Assert.Contains("الباقي", vm.ActionNote);
    }

    [Fact]
    public void A_paid_delivery_shows_nothing_to_collect_when_its_result_is_recorded()
    {
        var (vm, _) = Screen(Array.Empty<QueueOrderDto>());
        var paid = new DeliveryDto("delivery-1", "ord-1", "#0140", "OutForDelivery", "driver", "سائق",
            12_000, 12_000, "instapay", true, "shift", DateTimeOffset.UtcNow);

        vm.OpenOutcomeCommand.Execute(paid);

        Assert.Equal(CashInput.Format(0), vm.OutcomeCollectedText);
        Assert.Contains(vm.FailureReasons, r => r.Code == "phone_off");
    }
}
