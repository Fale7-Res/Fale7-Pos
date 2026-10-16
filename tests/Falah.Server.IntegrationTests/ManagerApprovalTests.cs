namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class ManagerApprovalTests : IAsyncLifetime
{
    [Fact]
    public async Task Two_prevalidated_consumers_cannot_commit_the_same_approval()
    {
        var approval = await IssueAsync("orders_comp");
        using var firstScope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(_factory.Services);
        using var secondScope = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.CreateScope(_factory.Services);
        var first = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>(firstScope.ServiceProvider);
        var second = Microsoft.Extensions.DependencyInjection.ServiceProviderServiceExtensions.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>(secondScope.ServiceProvider);
        var a = new Falah.Infrastructure.Persistence.EfApprovalRegistry(first);
        var b = new Falah.Infrastructure.Persistence.EfApprovalRegistry(second);
        Falah.Domain.Authorization.AuthorizationPolicy.ValidateApproval(a, "orders_comp", a.Find(approval.ApprovalId)!);
        Falah.Domain.Authorization.AuthorizationPolicy.ValidateApproval(b, "orders_comp", b.Find(approval.ApprovalId)!);
        await first.SaveChangesAsync();
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => second.SaveChangesAsync());
    }

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<IssueManagerApprovalResult> IssueAsync(string action, string reason = "اعتماد اختبار") =>
        (await (await _client.PostAsJsonAsync("/api/approvals/issue", new IssueManagerApprovalRequest(_factory.PinFor(_managerId), action, reason)))
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
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
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
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
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
            new IssueManagerApprovalRequest(_factory.PinFor(_cashierId), "orders_comp", "محاولة غير مصرح بها"));
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
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
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
        var restartedClient = restarted.CreateAuthenticatedClient(_factory);

        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
            CompApprovalId: approval.ApprovalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            CompReasonCode: "hospitality");

        var response = await restartedClient.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
    }
    // ---- A cashier who holds orders_comp still needs a REAL approval ----

    /// <summary>The comp's approver is written on the order and in the audit trail, so holding
    /// orders_comp must not let a cashier name a manager who never approved anything. Before this
    /// was fixed, the permission short-circuited the check and any approval id was accepted.</summary>
    [Fact]
    public async Task A_cashier_holding_orders_comp_cannot_name_an_approver_who_never_approved()
    {
        var compCashier = await _factory.SeedUserAsync("كاشير بصلاحية ضيافة", "cashier", "pos", "orders_comp");
        var productId = await _factory.SeedProductAsync(10000);
        var refundApproval = await IssueAsync("orders_refund");

        foreach (var approvalId in new[] { $"approval-{Guid.NewGuid()}", refundApproval.ApprovalId })
        {
            var response = await _client.PostAsJsonAsync("/api/orders/checkout", CompCheckout(compCashier, productId, approvalId));
            Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        }

        var orders = await _factory.QueryAsync(db => db.Orders.CountAsync(o => o.CashierId == compCashier));
        Assert.Equal(0, orders);
    }

    [Fact]
    public async Task A_cashier_holding_orders_comp_spends_a_real_approval_exactly_once()
    {
        var compCashier = await _factory.SeedUserAsync("كاشير بصلاحية ضيافة ٢", "cashier", "pos", "orders_comp");
        var productId = await _factory.SeedProductAsync(10000);
        var approval = await IssueAsync("orders_comp");

        var first = await _client.PostAsJsonAsync("/api/orders/checkout", CompCheckout(compCashier, productId, approval.ApprovalId));
        first.EnsureSuccessStatusCode();

        var reused = await _client.PostAsJsonAsync("/api/orders/checkout", CompCheckout(compCashier, productId, approval.ApprovalId));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, reused.StatusCode);
    }

    private CheckoutOrderRequest CompCheckout(string cashierId, string productId, string approvalId) => new(
        CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: cashierId, ShiftId: _shiftId,
        OrderType: "takeaway", PaymentMethod: "cash",
        Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
        DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 10000,
        CompApprovalId: approvalId, CompApproverUserId: _managerId, CompReason: "ضيافة",
        AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
        TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
        CompReasonCode: "hospitality");
}
