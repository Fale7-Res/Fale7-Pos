using System.Net;
using System.Net.Http.Json;
using System.Text;
using Falah.Application.Orders;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Models;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>UI-0025 (owner decision 2026-11-22): a pickup is registered and sent to the kitchen first and
/// paid when the customer collects it - from the till's pickup details, then from Order Lookup. The
/// collection is a money command: a lost answer keeps it and retries it with the same request id.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class PickupCollectionClientTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-collect-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public PickupCollectionClientTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

    public void Dispose()
    {
        RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static CashierViewModel Till(Func<HttpRequestMessage, Task<HttpResponseMessage>> send, params string[] permissions)
    {
        // A manager at the till approves their own cancel; a cashier needs a manager's PIN.
        var role = permissions.Contains("all") ? "manager" : "cashier";
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://test") }))
        { CurrentUser = new UserSummaryDto("cashier", "كاشير", role, permissions.Length == 0 ? new[] { "pos" } : permissions), ShiftId = "shift-2" };
        return session.GetCashier(new());
    }

    private static OrderDetailsDto Awaiting() => new OrderDetailsDto(
        "o1", "#0042", 42, "pickup", "cash", Array.Empty<OrderItemDto>(), 5000, 0, 0, 5000, 0, 0, "completed", false,
        null, 0, DateTimeOffset.UtcNow, "cashier", "أحمد", "01012345678", null, null, null, null, null, null, null, null, null, null, 0, 0, "shift-1")
    { AwaitingCollection = true };

    private const string Paid = """{"orderId":"o1","orderNumber":"#0042","sequenceNumber":42,"totalPiastres":5000,"amountPaidPiastres":6000,"changeDuePiastres":1000,"status":"completed","isPaid":true,"electronicPaymentStatus":null,"wasReplayed":false,"printJobsQueued":true,"awaitingCollection":false}""";

    [Fact]
    public async Task Collecting_in_cash_sends_the_tender_for_this_shift_and_shows_the_change()
    {
        CollectPickupPaymentRequest? sent = null;
        var vm = Till(async r =>
        {
            sent = await r.Content!.ReadFromJsonAsync<CollectPickupPaymentRequest>();
            return Json(Paid);
        });
        vm.OrderLookupResult = Awaiting();
        Assert.True(vm.CanCollectPickup);
        Assert.Equal("50.00", vm.CollectAmountText);
        Assert.Contains("ما اتدفعش", vm.LookupPaymentBreakdown);

        vm.CollectAmountText = "60";
        await vm.CollectPickupCommand.ExecuteAsync(null);

        Assert.Equal("o1", sent!.OrderId);
        Assert.Equal("shift-2", sent.ShiftId);
        Assert.Equal("cash", sent.PaymentMethod);
        Assert.Equal(6000, sent.AmountPaidPiastres);
        Assert.False(vm.CanCollectPickup);
        Assert.True(vm.OrderLookupResult!.IsPaid);
        Assert.Contains("10", vm.ReprintStatusMessage);
        Assert.True(vm.CanEditOperation);
    }

    [Fact]
    public async Task Cash_short_of_the_total_sends_nothing()
    {
        var sent = 0;
        var vm = Till(_ => { sent++; return Task.FromResult(Json(Paid)); });
        vm.OrderLookupResult = Awaiting();
        vm.CollectAmountText = "40";

        await vm.CollectPickupCommand.ExecuteAsync(null);

        Assert.Equal(0, sent);
        Assert.NotNull(vm.CollectError);
    }

    [Fact]
    public async Task A_lost_answer_keeps_the_collection_and_retries_it_with_the_same_request_id()
    {
        var bodies = new List<CollectPickupPaymentRequest>();
        var fail = true;
        var vm = Till(async r =>
        {
            bodies.Add((await r.Content!.ReadFromJsonAsync<CollectPickupPaymentRequest>())!);
            if (fail) throw new HttpRequestException("answer lost");
            return Json(Paid);
        });
        vm.OrderLookupResult = Awaiting();
        vm.CollectMethod = "card";

        await vm.CollectPickupCommand.ExecuteAsync(null);
        Assert.False(vm.CanEditOperation);

        fail = false;
        vm.CollectMethod = "cash";   // a change on screen does not alter the kept request
        await vm.CollectPickupCommand.ExecuteAsync(null);

        Assert.Equal(2, bodies.Count);
        Assert.Equal(bodies[0], bodies[1]);
        Assert.Equal("card", bodies[1].PaymentMethod);
        Assert.True(vm.CanEditOperation);
    }

    [Fact]
    public async Task Cancelling_a_pickup_nobody_came_for_needs_a_reason_and_says_what_became_of_the_food()
    {
        CancelUncollectedPickupRequest? sent = null;
        var vm = Till(async r =>
        {
            sent = await r.Content!.ReadFromJsonAsync<CancelUncollectedPickupRequest>();
            return new HttpResponseMessage(HttpStatusCode.OK);
        }, "all");
        vm.OrderLookupResult = Awaiting();

        await vm.CancelUncollectedPickupCommand.ExecuteAsync(null);
        Assert.Null(sent);
        Assert.NotNull(vm.CancelPickupError);

        vm.CancelPickupReason = "العميل ما جاش";
        vm.CancelPickupPrepared = false;
        await vm.CancelUncollectedPickupCommand.ExecuteAsync(null);

        Assert.Equal("not_prepared", sent!.StockDisposition);
        Assert.Equal("o1", sent.OrderId);
        Assert.Equal("cancelled", vm.OrderLookupResult!.Status);
        Assert.False(vm.CanCollectPickup);
    }

    // Owner, 2026-11-23: a cashier needs a manager's PIN to cancel, whatever permissions they hold.
    [Fact]
    public async Task A_cashier_cannot_cancel_without_a_managers_pin_even_holding_orders_refund()
    {
        var sent = 0;
        var vm = Till(_ => { sent++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); }, "pos", "orders_refund");
        vm.OrderLookupResult = Awaiting();
        vm.CancelPickupReason = "العميل ما جاش";
        Assert.True(vm.CancelPickupNeedsApproval);

        await vm.CancelUncollectedPickupCommand.ExecuteAsync(null);

        Assert.Equal(0, sent);
        Assert.NotNull(vm.CancelPickupError);
    }

    [Fact]
    public async Task A_pickup_paid_on_collection_is_registered_with_no_money()
    {
        CheckoutOrderRequest? sent = null;
        var vm = Till(async r =>
        {
            sent = await r.Content!.ReadFromJsonAsync<CheckoutOrderRequest>();
            return Json("""{"orderId":"o1","orderNumber":"#0042","sequenceNumber":42,"totalPiastres":5000,"amountPaidPiastres":0,"changeDuePiastres":0,"status":"completed","isPaid":false,"electronicPaymentStatus":null,"wasReplayed":false,"printJobsQueued":true,"awaitingCollection":true}""");
        });
        vm.OrderType = "pickup";
        vm.CustomerName = "أحمد";
        vm.CustomerPhone = "01012345678";
        vm.PayOnCollection = true;
        vm.Cart.Add(new CartLine { ProductId = "p", ProductName = "ساندوتش", UnitPricePiastres = 5000 });
        vm.OpenCheckoutCommand.Execute(null);
        vm.AmountPaidText = "";

        await vm.SubmitPaymentCommand.ExecuteAsync(null);

        Assert.True(sent!.PayOnCollection);
        Assert.Equal(0, sent.AmountPaidPiastres);
        Assert.Equal("اتسجل طلب الاستلام", vm.CheckoutTitle);
    }
}
