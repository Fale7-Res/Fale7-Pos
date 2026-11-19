namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// SEC3. A manager approval is a manager typing their PIN at a cashier's till for one action, once.
/// It was single-use and bound to its action, but never expired and could be spent by anybody who
/// held its id - so an approval granted for a moment was a standing permission, transferable to any
/// session. It is now spent only by the user whose session asked for it, and only within its
/// lifetime (<c>Approvals:LifetimeMinutes</c>).
/// </summary>
public class ApprovalLifecycleTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private WebApplicationFactory<Program> _host = default!;
    private readonly ManualClock _clock = new(DateTimeOffset.UtcNow);
    private string _managerId = default!;
    private string _cashierId = default!;
    private string _otherCashierId = default!;
    private string _shiftId = default!;
    private string _productId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?> { ["Approvals:LifetimeMinutes"] = "15" });
        _host = _factory.WithWebHostBuilder(b => b.ConfigureServices(s => s.AddSingleton<TimeProvider>(_clock)));
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        _otherCashierId = await _factory.SeedUserAsync("كاشير آخر", "cashier", "pos");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await As(_cashierId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
        _productId = await _factory.SeedProductAsync(10000);
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        _factory.Dispose();
        return Task.CompletedTask;
    }

    /// <summary>Every request from this client is that user's own session - as at a real till,
    /// where the approval is asked for and spent from the same screen.</summary>
    private HttpClient As(string userId)
    {
        var client = _host.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<IssueManagerApprovalResult> IssueAsync(HttpClient till, string action = "orders_comp")
    {
        var response = await till.PostAsJsonAsync("/api/approvals/issue", new IssueManagerApprovalRequest(_factory.PinFor(_managerId), action, "اعتماد اختبار"));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
    }

    private CheckoutOrderRequest CompCheckout(string cashierId, string approvalId) => new(
        CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: cashierId, ShiftId: _shiftId,
        OrderType: "takeaway", PaymentMethod: "cash",
        Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
        DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
        CompApprovalId: approvalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
        AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
        TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
        CompReasonCode: "hospitality");

    private sealed record ErrorBody(string? error);

    [Fact]
    public async Task An_approval_can_be_spent_only_by_the_cashier_whose_till_asked_for_it()
    {
        var approval = await IssueAsync(As(_cashierId));

        var stolen = await As(_otherCashierId).PostAsJsonAsync("/api/orders/checkout", CompCheckout(_otherCashierId, approval.ApprovalId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, stolen.StatusCode);
        // Someone else's approval is answered exactly like a made-up one.
        var madeUp = await As(_otherCashierId).PostAsJsonAsync("/api/orders/checkout", CompCheckout(_otherCashierId, $"approval-{Guid.NewGuid()}"));
        Assert.Equal((await madeUp.Content.ReadFromJsonAsync<ErrorBody>())!.error, (await stolen.Content.ReadFromJsonAsync<ErrorBody>())!.error);

        // The refused attempt did not spend it: its owner still can.
        (await As(_cashierId).PostAsJsonAsync("/api/orders/checkout", CompCheckout(_cashierId, approval.ApprovalId))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task An_approval_left_unspent_past_its_lifetime_is_refused_and_a_fresh_one_works()
    {
        var till = As(_cashierId);
        var approval = await IssueAsync(till);
        _clock.Advance(TimeSpan.FromMinutes(16));

        var late = await till.PostAsJsonAsync("/api/orders/checkout", CompCheckout(_cashierId, approval.ApprovalId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, late.StatusCode);
        Assert.Contains("انتهت صلاحية", (await late.Content.ReadFromJsonAsync<ErrorBody>())!.error);

        var fresh = await IssueAsync(till);
        (await till.PostAsJsonAsync("/api/orders/checkout", CompCheckout(_cashierId, fresh.ApprovalId))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task An_approval_spent_within_its_lifetime_is_accepted()
    {
        var till = As(_cashierId);
        var approval = await IssueAsync(till);
        _clock.Advance(TimeSpan.FromMinutes(14));

        (await till.PostAsJsonAsync("/api/orders/checkout", CompCheckout(_cashierId, approval.ApprovalId))).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Expiry_applies_to_approvals_spent_through_a_permission_gate_too()
    {
        // A refund by a cashier without orders_refund goes through AuthorizeCommand, not the comp path.
        var till = As(_cashierId);
        var sale = await (await till.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null)))
            .Content.ReadFromJsonAsync<CheckoutOrderResult>();
        var approval = await IssueAsync(till, "orders_refund");
        _clock.Advance(TimeSpan.FromMinutes(16));

        var refund = await till.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", sale!.OrderId, 5000, "طلب العميل", _cashierId, approval.ApprovalId, _managerId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refund.StatusCode);
    }

    [Fact]
    public async Task An_approval_for_one_action_authorises_nothing_else()
    {
        var till = As(_cashierId);
        var approval = await IssueAsync(till, "orders_refund");

        var comp = await till.PostAsJsonAsync("/api/orders/checkout", CompCheckout(_cashierId, approval.ApprovalId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, comp.StatusCode);
    }

    /// <summary>Two requests racing to spend one approval: one wins, the other is told plainly - not a
    /// 500, which a till reads as "outcome unknown" and keeps retrying.</summary>
    [Fact]
    public async Task Two_requests_spending_one_approval_at_once_succeed_once_and_the_other_is_refused_plainly()
    {
        var till = As(_cashierId);
        var approval = await IssueAsync(till);

        var responses = await Task.WhenAll(Enumerable.Range(0, 4)
            .Select(_ => till.PostAsJsonAsync("/api/orders/checkout", CompCheckout(_cashierId, approval.ApprovalId))));

        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.All(responses.Where(r => !r.IsSuccessStatusCode), r => Assert.True((int)r.StatusCode < 500, $"refused with {(int)r.StatusCode}"));
    }

    private sealed class ManualClock : TimeProvider
    {
        private DateTimeOffset _now;
        public ManualClock(DateTimeOffset start) => _now = start;
        public override DateTimeOffset GetUtcNow() => _now;
        public void Advance(TimeSpan by) => _now += by;
    }
}
