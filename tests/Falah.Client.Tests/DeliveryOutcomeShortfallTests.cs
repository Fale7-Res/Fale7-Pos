using System.Net;
using System.Text;
using System.Text.Json;
using Falah.Application.Delivery;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;
using Xunit;

namespace Falah.Client.Tests;

/// <summary>
/// AUD-03 on the dispatch screen: recording a delivery whose customer paid less than was due needs the authority
/// that approves a settlement difference. A dispatcher without it is asked for a manager's PIN, the screen issues
/// that approval for delivery_settlement_approve and sends it with the result; an exact amount needs nothing.
/// </summary>
[Collection(RetainedRequestStorageCollection.Name)]
public sealed class DeliveryOutcomeShortfallTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "falah-shortfall-" + Guid.NewGuid().ToString("N"));
    private readonly IRetainedRequestStore? _previous = RetainedRequestStorage.Current;

    public DeliveryOutcomeShortfallTests() => RetainedRequestStorage.Current = new FileRetainedRequestStore(_dir);

    public void Dispose()
    {
        RetainedRequestStorage.Current = _previous;
        if (Directory.Exists(_dir)) Directory.Delete(_dir, true);
    }

    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK) { Content = new StringContent(body, Encoding.UTF8, "application/json") };

    private static (DeliveryViewModel Vm, List<JsonElement> Results, List<JsonElement> Approvals) Screen(params string[] permissions)
    {
        var results = new List<JsonElement>();
        var approvals = new List<JsonElement>();
        var http = new HttpClient(new Handler(async r =>
        {
            var path = r.RequestUri!.AbsolutePath;
            if (r.Method == HttpMethod.Post && path == "/api/approvals/issue")
            {
                approvals.Add(JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone());
                return Json("""{"approvalId":"apr-1","approverUserId":"mgr","approverName":"المدير","action":"delivery_settlement_approve","approvedAtUtc":"2026-11-13T10:00:00Z"}""");
            }
            if (r.Method == HttpMethod.Post && path.EndsWith("/result"))
            {
                results.Add(JsonDocument.Parse(await r.Content!.ReadAsStringAsync()).RootElement.Clone());
                return Json("""{"deliveryId":"delivery-1","status":"Delivered","wasReplayed":false}""");
            }
            return Json("[]");
        })) { BaseAddress = new Uri("http://fixture.invalid") };
        var session = new AppSession(new ApiClient(http)) { CurrentUser = new("dispatcher", "موظف التوصيل", "dispatcher", permissions), ShiftId = "shift" };
        return (new DeliveryViewModel(session), results, approvals);
    }

    private static readonly DeliveryDto Out = new("delivery-1", "ord-1", "#0140", "OutForDelivery", "driver", "سائق",
        12_000, 12_000, "cash", false, "shift", DateTimeOffset.UtcNow);

    [Fact]
    public async Task A_shortfall_without_the_authority_asks_for_a_manager_pin_and_sends_that_approval()
    {
        var (vm, results, approvals) = Screen("delivery_manage");
        vm.OpenOutcomeCommand.Execute(Out);
        vm.OutcomeCollectedText = CashInput.Format(10_000);
        vm.OutcomeReason = "العميل دفع 100 فقط";

        Assert.True(vm.OutcomeShortfallNeedsApproval);
        Assert.Contains("20", vm.OutcomeShortfallLabel);

        await vm.ConfirmOutcomeCommand.ExecuteAsync(null);   // no PIN yet
        Assert.Empty(results);
        Assert.NotNull(vm.OutcomeError);

        vm.OutcomeManagerPin = "4321";
        await vm.ConfirmOutcomeCommand.ExecuteAsync(null);

        var approval = Assert.Single(approvals);
        Assert.Equal("delivery_settlement_approve", approval.GetProperty("action").GetString());
        var sent = Assert.Single(results);
        Assert.Equal(10_000, sent.GetProperty("collectedAmountPiastres").GetInt64());
        Assert.Equal("apr-1", sent.GetProperty("managerApprovalId").GetString());
        Assert.Equal("mgr", sent.GetProperty("managerApproverUserId").GetString());
    }

    [Fact]
    public async Task The_exact_amount_needs_no_pin_and_no_approval()
    {
        var (vm, results, approvals) = Screen("delivery_manage");
        vm.OpenOutcomeCommand.Execute(Out);

        Assert.False(vm.OutcomeShortfallNeedsApproval);
        await vm.ConfirmOutcomeCommand.ExecuteAsync(null);

        Assert.Empty(approvals);
        var sent = Assert.Single(results);
        Assert.Equal(JsonValueKind.Null, sent.GetProperty("managerApprovalId").ValueKind);
    }

    [Fact]
    public void A_user_holding_the_authority_is_not_asked_for_a_pin()
    {
        var (vm, _, _) = Screen("delivery_manage", "delivery_settlement_approve");
        vm.OpenOutcomeCommand.Execute(Out);
        vm.OutcomeCollectedText = CashInput.Format(10_000);

        Assert.True(vm.IsOutcomeShortfall);
        Assert.False(vm.OutcomeShortfallNeedsApproval);
    }
}
