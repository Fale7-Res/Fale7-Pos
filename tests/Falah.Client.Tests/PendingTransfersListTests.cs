using System.Net;
using System.Text;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>Owner decision 2026-11-25 (audit U19): the till lists the InstaPay/wallet transfers still waiting, and picking
/// one opens it in the order lookup, where its "مراجعة التحويل المعلق" action already is.</summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class PendingTransfersListTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body) =>
        new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    [Fact]
    public async Task The_waiting_transfers_are_listed_and_one_opens_in_the_lookup()
    {
        var lookedUp = new List<string>();
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(r =>
        {
            var path = r.RequestUri!.PathAndQuery;
            if (path == "/api/orders/pending-transfers")
                return Task.FromResult(Json("""[{"orderId":"o1","orderNumber":"#0130","method":"instapay","amountPiastres":4500,"customerName":null,"createdAtUtc":"2026-11-25T01:02:00+00:00"}]"""));
            lookedUp.Add(path);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        })) { BaseAddress = new Uri("http://test") }))
        { CurrentUser = new UserSummaryDto("cashier", "كاشير", "cashier", new[] { "pos" }), ShiftId = "shift" };
        var vm = session.GetCashier(new());

        await vm.LoadPendingTransfersCommand.ExecuteAsync(null);

        Assert.True(vm.HasPendingTransfers);
        var row = Assert.Single(vm.PendingTransfers);
        Assert.Equal(4500, row.AmountPiastres);

        await vm.OpenPendingTransferCommand.ExecuteAsync(row);
        Assert.Equal("#0130", vm.OrderLookupNumber);
        Assert.Single(lookedUp);
    }

    [Fact]
    public async Task No_waiting_transfer_says_so()
    {
        var session = new AppSession(new ApiClient(new HttpClient(new Handler(_ => Task.FromResult(Json("[]")))) { BaseAddress = new Uri("http://test") }))
        { CurrentUser = new UserSummaryDto("cashier", "كاشير", "cashier", new[] { "pos" }), ShiftId = "shift" };
        var vm = session.GetCashier(new());

        await vm.LoadPendingTransfersCommand.ExecuteAsync(null);

        Assert.False(vm.HasPendingTransfers);
        Assert.Equal("ما فيش تحويلات مستنية تأكيد.", vm.ReprintStatusMessage);
    }
}
