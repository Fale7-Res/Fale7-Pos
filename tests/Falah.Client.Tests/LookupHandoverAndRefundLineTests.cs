using System.Net;
using System.Net.Http.Json;
using System.Text;
using Falah.Application.Orders;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Audit 2026-11-25 (full trading day on the real apps).
/// G1: a cashier holding only "pos" lands on the till, not the shell, so the order queue - the one place a prepaid
/// pickup was handed over - was out of reach: the pickup stayed "preparing" and its customer never got a receipt.
/// The order lookup now hands it over.
/// U15: an order already partly refunded looked untouched in the lookup (only its total), inviting a second refund.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class LookupHandoverAndRefundLineTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static CashierViewModel Till(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(send)) { BaseAddress = new Uri("http://test") }))
        { CurrentUser = new UserSummaryDto("cashier", "كاشير", "cashier", new[] { "pos" }), ShiftId = "shift-1" };
        return session.GetCashier(new());
    }

    private static OrderDetailsDto Order(string type = "pickup", string status = "preparing", bool paid = true,
        long refunded = 0, DateTimeOffset? handedOver = null, bool awaiting = false) => new OrderDetailsDto(
        "o1", "#0131", 131, type, "cash", Array.Empty<OrderItemDto>(), 5000, 0, 0, 5000, 5000, 0, status, paid,
        null, refunded, DateTimeOffset.UtcNow, "cashier", "محمد علي", "01012345678", null, null, null, null, null, null, null, null, null, null, 0, 0, "shift-1")
    { HandedOverAtUtc = handedOver, AwaitingCollection = awaiting };

    private const string HandedOver = """{"orderId":"o1","orderNumber":"#0131","handedOverAtUtc":"2026-11-25T01:20:00+00:00","wasReplayed":false,"printJobsQueued":true}""";

    [Fact]
    public async Task A_paid_pickup_not_yet_handed_over_is_handed_over_from_the_lookup_by_the_cashier()
    {
        HandOverPickupRequest? sent = null;
        string? path = null;
        var vm = Till(async r =>
        {
            path = r.RequestUri!.AbsolutePath;
            sent = await r.Content!.ReadFromJsonAsync<HandOverPickupRequest>();
            return Json(HandedOver);
        });
        vm.OrderLookupResult = Order();
        Assert.True(vm.CanHandOverPickup);

        await vm.HandOverPickupCommand.ExecuteAsync(null);

        Assert.Equal("/api/orders/hand-over-pickup", path);
        Assert.Equal("o1", sent!.OrderId);
        Assert.Equal("cashier", sent.ActorUserId);
        Assert.False(string.IsNullOrWhiteSpace(sent.RequestId));
        Assert.False(vm.CanHandOverPickup);
        Assert.Equal("completed", vm.OrderLookupResult!.Status);
        Assert.Contains("#0131", vm.ReprintStatusMessage);
    }

    [Fact]
    public async Task A_refused_handover_keeps_the_button_and_shows_the_servers_words_and_a_retry_reuses_the_request_id()
    {
        var ids = new List<string>();
        Func<HttpResponseMessage> answer = () => Json("""{"error":"الطلب ده اترجّعت فلوسه بالكامل، ما ينفعش يتسلّم."}""", HttpStatusCode.UnprocessableEntity);
        var vm = Till(async r => { ids.Add((await r.Content!.ReadFromJsonAsync<HandOverPickupRequest>())!.RequestId); return answer(); });
        vm.OrderLookupResult = Order();

        await vm.HandOverPickupCommand.ExecuteAsync(null);

        Assert.Equal("الطلب ده اترجّعت فلوسه بالكامل، ما ينفعش يتسلّم.", vm.HandOverError);
        Assert.True(vm.CanHandOverPickup);
        answer = () => Json("{}", HttpStatusCode.InternalServerError);   // the answer is lost: the same press must be retried as is
        await vm.HandOverPickupCommand.ExecuteAsync(null);
        await vm.HandOverPickupCommand.ExecuteAsync(null);
        Assert.Equal(ids[1], ids[2]);
    }

    [Theory]
    [InlineData("takeaway", "preparing", true, 0, false, false)]   // not a pickup
    [InlineData("pickup", "refunded", true, 5000, false, false)]   // refunded in full
    [InlineData("pickup", "cancelled", true, 0, false, false)]
    [InlineData("pickup", "completed", true, 0, true, false)]      // already handed over
    [InlineData("pickup", "preparing", false, 0, false, true)]     // unpaid: collected (and handed over) instead
    public void The_handover_is_offered_only_for_a_paid_open_pickup(string type, string status, bool paid, long refunded, bool handed, bool awaiting)
    {
        var vm = Till(_ => Task.FromResult(Json("{}")));
        vm.OrderLookupResult = Order(type, status, paid, refunded, handed ? DateTimeOffset.UtcNow : null, awaiting);
        Assert.False(vm.CanHandOverPickup);
    }

    [Fact]
    public void A_partly_refunded_order_says_how_much_went_back_and_what_is_left()
    {
        var vm = Till(_ => Task.FromResult(Json("{}")));
        vm.OrderLookupResult = Order(type: "takeaway", refunded: 4500) with { TotalPiastres = 14500 };

        Assert.True(vm.HasLookupRefund);
        Assert.Contains("45.00", vm.LookupRefundLine);
        Assert.Contains("100.00", vm.LookupRefundLine);

        vm.OrderLookupResult = Order(type: "takeaway");
        Assert.False(vm.HasLookupRefund);
    }
}
