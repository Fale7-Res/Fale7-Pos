namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Xunit;

public class ManagerApprovalTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<IssueManagerApprovalResult> IssueAsync(string action, string reason = "اعتماد اختبار") =>
        (await (await _client.PostAsJsonAsync("/api/approvals/issue", new IssueManagerApprovalRequest(_managerId, action, reason)))
            .Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;

    [Fact]
    public async Task An_approval_issued_by_a_manager_can_authorize_a_comp_checkout()
    {
        var approval = await IssueAsync("orders_comp");
        var productId = await _factory.SeedProductAsync(10000);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approval.ApprovalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality");

        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        var order = await response.Content.ReadFromJsonAsync<CheckoutOrderResult>();
        Assert.Equal(0, order!.TotalPiastres);
    }

    [Fact]
    public async Task A_consumed_approval_cannot_authorize_a_second_checkout()
    {
        var approval = await IssueAsync("orders_comp");
        var productId = await _factory.SeedProductAsync(10000);
        CheckoutOrderRequest MakeRequest(string reqId) => new(
            CheckoutRequestId: reqId, CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approval.ApprovalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality");

        var first = await _client.PostAsJsonAsync("/api/orders/checkout", MakeRequest($"req-{Guid.NewGuid():N}"));
        first.EnsureSuccessStatusCode();

        var second = await _client.PostAsJsonAsync("/api/orders/checkout", MakeRequest($"req-{Guid.NewGuid():N}"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, second.StatusCode);
    }

    [Fact]
    public async Task An_approval_issued_by_a_plain_cashier_is_rejected()
    {
        var response = await _client.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_cashierId, "orders_comp", "محاولة غير مصرح بها"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_approval_issued_for_refund_lets_an_unprivileged_cashier_refund_an_order()
    {
        var refundProductId = await _factory.SeedProductAsync(10000);
        var checkoutRequest = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(refundProductId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 10000, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var order = (await (await _client.PostAsJsonAsync("/api/orders/checkout", checkoutRequest)).Content.ReadFromJsonAsync<CheckoutOrderResult>())!;

        var approval = await IssueAsync("orders_refund");
        // _cashierId only has "pos" - no orders_refund permission - so this can ONLY succeed via the approval.
        var refundRequest = new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", order.OrderId, 5000, "طلب العميل", _cashierId, approval.ApprovalId, _managerId);
        var response = await _client.PostAsJsonAsync("/api/orders/refund", refundRequest);

        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task An_approval_persists_and_is_still_consumable_from_a_completely_separate_server_instance()
    {
        // Proves durability independent of any in-process cache: a second WebApplicationFactory is
        // a fresh app host with its own DI container and its own fresh EfApprovalRegistry instance
        // - the only thing it shares with the first is the SQL Server database itself.
        var approval = await IssueAsync("orders_comp");
        var productId = await _factory.SeedProductAsync(10000);

        using var restarted = new FalahApiFactory(reuseDatabaseName: _factory.DatabaseName);
        var restartedClient = restarted.CreateClient();

        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approval.ApprovalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality");

        var response = await restartedClient.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
    }
}
