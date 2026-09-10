namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Audit;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Xunit;

/// <summary>
/// The general-purpose audit trail (row 15 of IMPLEMENTATION_MATRIX.md) - was DOMAIN_ONLY until now
/// (Falah.Domain.Audit.AuditEvent/IAuditLog existed but were referenced nowhere in the codebase).
/// Wired into a curated, evidence-based set of the system's most sensitive existing actions (order
/// refunds, shift close, every Treasury cash-lifecycle command, expenses) - each appended to the
/// SAME DbContext/transaction as the business action it describes, so a rolled-back operation can
/// never leave an orphaned audit row. Distinct from /api/control-center/audit-events (the narrower
/// business-day/user-management trail built in earlier slices).
/// </summary>
public class AuditLogVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "safe_drop", "expenses_add_drawer");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var opened = await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000))).Content.ReadFromJsonAsync<OpenShiftResult>();
        _shiftId = opened!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<List<AuditEventDto>> ListEventsAsync(string? category = null, string? severity = null, string? userId = null)
    {
        var query = $"actorUserId={_managerId}"
            + (category is null ? "" : $"&category={category}")
            + (severity is null ? "" : $"&severity={severity}")
            + (userId is null ? "" : $"&userId={userId}");
        return (await (await _client.GetAsync($"/api/audit/events?{query}")).Content.ReadFromJsonAsync<List<AuditEventDto>>())!;
    }

    private async Task<CheckoutOrderResult> CheckoutCashAsync(long totalPiastres)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var request = new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", request);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    // ---- Orders: refund ----

    [Fact]
    public async Task A_refund_writes_an_order_category_audit_event()
    {
        var order = await CheckoutCashAsync(10000);
        var refund = await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", order.OrderId, 10000, "طلب العميل", _managerId, null, null));
        refund.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "order");
        Assert.Contains(events, e => e.ReferenceId == order.OrderId && e.Action == "استرداد طلب");
    }

    [Fact]
    public async Task A_small_refund_is_normal_severity_a_large_one_is_critical()
    {
        var smallOrder = await CheckoutCashAsync(10000);
        (await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", smallOrder.OrderId, 10000, "سبب", _managerId, null, null))).EnsureSuccessStatusCode();

        var largeOrder = await CheckoutCashAsync(60000); // >= LargeRefundThresholdPiastres (50000)
        (await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", largeOrder.OrderId, 60000, "سبب", _managerId, null, null))).EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "order");
        Assert.Equal("normal", events.Single(e => e.ReferenceId == smallOrder.OrderId).Severity);
        Assert.Equal("critical", events.Single(e => e.ReferenceId == largeOrder.OrderId).Severity);
    }

    [Fact]
    public async Task A_replayed_refund_never_duplicates_its_audit_event()
    {
        var order = await CheckoutCashAsync(10000);
        var requestId = $"rreq-{Guid.NewGuid():N}";
        var request = new RefundOrderRequest(requestId, order.OrderId, 10000, "سبب", _managerId, null, null);
        (await _client.PostAsJsonAsync("/api/orders/refund", request)).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/orders/refund", request)).EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "order");
        Assert.Single(events.Where(e => e.ReferenceId == order.OrderId));
    }

    // ---- Shifts ----

    [Fact]
    public async Task Closing_a_shift_writes_a_shift_category_audit_event()
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", _shiftId, _cashierId, 50000, null, null, null));
        response.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "shift");
        Assert.Contains(events, e => e.ShiftId == _shiftId && e.Action == "إغلاق وردية" && e.Severity == "normal");
    }

    // ---- Treasury ----

    [Fact]
    public async Task A_deposit_writes_a_cash_category_audit_event()
    {
        var response = await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 50000, "tre-main", "تمويل", null, _managerId, null, null));
        response.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "cash");
        Assert.Contains(events, e => e.Action == "إيداع خارجي" && e.Severity == "normal");
    }

    [Fact]
    public async Task An_owner_withdrawal_is_always_critical()
    {
        await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 50000, "tre-main", "تمويل", null, _managerId, null, null));
        var response = await _client.PostAsJsonAsync("/api/treasury/withdraw",
            new WithdrawalRequest($"wd-{Guid.NewGuid():N}", 10000, "tre-main", "مصروف شخصي", null, _managerId, null, null));
        response.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "cash", severity: "critical");
        Assert.Contains(events, e => e.Action == "سحب مالك");
    }

    [Fact]
    public async Task A_transfer_writes_a_cash_category_audit_event()
    {
        await _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest($"dep-{Guid.NewGuid():N}", "owner_deposit", 50000, "tre-main", "تمويل", null, _managerId, null, null));
        var response = await _client.PostAsJsonAsync("/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-bank", 10000, "تحويل للبنك", _managerId, null, null));
        response.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "cash");
        Assert.Contains(events, e => e.Action == "تحويل بين حسابات الخزينة");
    }

    [Fact]
    public async Task A_safe_drop_writes_a_cash_category_audit_event()
    {
        await CheckoutCashAsync(20000); // ensures the drawer actually has cash to drop
        var response = await _client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", _shiftId, 10000, "زيادة في الدرج", _cashierId, null, null));
        response.EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "cash");
        Assert.Contains(events, e => e.Action == "إيداع آمن" && e.ShiftId == _shiftId);
    }

    // ---- Expenses ----

    [Fact]
    public async Task A_small_expense_is_normal_a_large_one_is_critical()
    {
        await CheckoutCashAsync(300000); // fund the drawer well beyond both expense amounts
        (await _client.PostAsJsonAsync("/api/expenses", new Falah.Application.Expenses.AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", _shiftId, "صيانة", 5000, "إصلاح بسيط", "tre-drawer", _cashierId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/expenses", new Falah.Application.Expenses.AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", _shiftId, "صيانة", 150000, "إصلاح كبير", "tre-drawer", _cashierId, null, null))).EnsureSuccessStatusCode();

        var events = await ListEventsAsync(category: "cash");
        Assert.Contains(events, e => e.Action == "تسجيل مصروف" && e.Details.Contains("5000") && e.Severity == "normal");
        Assert.Contains(events, e => e.Action == "تسجيل مصروف" && e.Details.Contains("150000") && e.Severity == "critical");
    }

    // ---- Access control ----

    [Fact]
    public async Task Reading_the_audit_log_requires_audit_view_or_control_center()
    {
        var noPermissionUser = await _factory.SeedUserAsync("موظف عادي", "cashier", "pos");
        var response = await _client.GetAsync($"/api/audit/events?actorUserId={noPermissionUser}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task A_user_holding_only_audit_view_can_read_the_log()
    {
        var auditor = await _factory.SeedUserAsync("مراجع", "supervisor", "audit_view");
        var response = await _client.GetAsync($"/api/audit/events?actorUserId={auditor}");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Filtering_by_category_returns_only_matching_events()
    {
        var order = await CheckoutCashAsync(10000);
        (await _client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"rreq-{Guid.NewGuid():N}", order.OrderId, 10000, "سبب", _managerId, null, null))).EnsureSuccessStatusCode();
        (await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", _shiftId, _cashierId, 50000, null, null, null))).EnsureSuccessStatusCode();

        var orderEvents = await ListEventsAsync(category: "order");
        Assert.All(orderEvents, e => Assert.Equal("order", e.Category));
        var shiftEvents = await ListEventsAsync(category: "shift");
        Assert.All(shiftEvents, e => Assert.Equal("shift", e.Category));
    }
}
