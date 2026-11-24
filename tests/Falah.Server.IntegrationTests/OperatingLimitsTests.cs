namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Application.Users;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Owner decisions 2026-11-24 (group A of the operating rules): the till is warned and the moment recorded when its
/// drawer passes 3,000 ج; a driver holding 1,500 ج is given no new cash order without a manager's PIN; a PIN is
/// changed every 90 days (print agents excepted), and a session with an expired PIN can do nothing else first.
/// </summary>
public class OperatingLimitsTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير الاختبار", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "safe_drop");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _factory.StationUserId = _dispatcherId;
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        await _factory.SeedDeliveryServiceAsync("svc-1", 0, "توصيل سريع");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenShiftAsync(long openingPiastres = 50_000) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingPiastres)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;

    private async Task<CheckoutOrderResult> CheckoutAsync(string shiftId, long totalPiastres, string orderType = "takeaway")
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var delivery = orderType == "delivery";
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: orderType, PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: delivery ? 0 : totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: delivery ? "عميل" : null, CustomerPhone: delivery ? "01000000000" : null,
            DeliveryAddress: null, DeliveryArea: delivery ? "المنطقة أ" : null, BusinessDayId: null,
            DeliveryStreet: delivery ? "شارع 9" : null, DeliveryServiceId: delivery ? "svc-1" : null, DeliveryServiceName: delivery ? "توصيل سريع" : null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private Task<int> DrawerAuditCountAsync() =>
        _factory.QueryAsync(db => db.AuditEvents.CountAsync(a => a.Id.StartsWith("audit-drawer-over-limit-")));

    // ---- Safe-drop warning ----

    [Fact]
    public async Task Passing_the_safe_drop_limit_warns_the_till_and_records_the_crossing_once()
    {
        var shiftId = await OpenShiftAsync(50_000);   // 500 in the drawer

        var under = await CheckoutAsync(shiftId, 200_000);   // 2,500
        Assert.Null(under.DrawerCashAlert);
        Assert.Equal(0, await DrawerAuditCountAsync());

        var over = await CheckoutAsync(shiftId, 60_000);     // 3,100
        Assert.Contains("إيداع آمن", over.DrawerCashAlert);
        var still = await CheckoutAsync(shiftId, 10_000);    // 3,200
        Assert.NotNull(still.DrawerCashAlert);
        Assert.Equal(1, await DrawerAuditCountAsync());

        (await _client.PostAsJsonAsync("/api/treasury/safe-drop", new Falah.Application.Treasury.SafeDropRequest(
            $"drop-{Guid.NewGuid():N}", shiftId, 250_000, "إيداع", _cashierId, null, null))).EnsureSuccessStatusCode();   // 700
        Assert.Null((await CheckoutAsync(shiftId, 10_000)).DrawerCashAlert);   // 800
        Assert.NotNull((await CheckoutAsync(shiftId, 250_000)).DrawerCashAlert);   // 3,300 - a second crossing
        Assert.Equal(2, await DrawerAuditCountAsync());
    }

    // ---- Driver custody cap ----

    private async Task<string> ReadyDeliveryAsync(string shiftId, long totalPiastres)
    {
        var order = await CheckoutAsync(shiftId, totalPiastres, "delivery");
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order.OrderId).Select(d => d.Id).FirstAsync());
        (await _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null))).EnsureSuccessStatusCode();
        return deliveryId;
    }

    private Task<HttpResponseMessage> AssignAsync(string deliveryId, string driverId, string? approvalId = null, string? approverId = null) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcherId, approvalId, approverId));

    [Fact]
    public async Task A_driver_past_the_custody_limit_gets_no_new_cash_order_without_a_managers_pin()
    {
        var shiftId = await OpenShiftAsync();
        var driverId = (await (await _client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق", _dispatcherId))).Content.ReadFromJsonAsync<DriverDto>())!.Id;
        (await AssignAsync(await ReadyDeliveryAsync(shiftId, 100_000), driverId)).EnsureSuccessStatusCode();   // 1,000 with him

        var second = await ReadyDeliveryAsync(shiftId, 60_000);   // would make 1,600
        var refused = await AssignAsync(second, driverId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
        Assert.Contains("حد العهدة", await refused.Content.ReadAsStringAsync());
        Assert.Equal(1, await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DriverId == driverId)));

        var approval = (await (await _factory.PostAsSessionOfAsync(_client, _dispatcherId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "driver_custody_limit", "زحمة")))
            .Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
        (await AssignAsync(second, driverId, approval.ApprovalId, approval.ApproverUserId)).EnsureSuccessStatusCode();
        Assert.Equal(2, await _factory.QueryAsync(db => db.DriverCustodies.CountAsync(c => c.DriverId == driverId)));
        Assert.Equal(1, await _factory.QueryAsync(db => db.AuditEvents.CountAsync(a => a.Action == "تعدي حد عهدة السائق" && a.AuthorizedBy == "المدير")));
    }

    // ---- PIN age ----

    private async Task BackdatePinAsync(string userId, int daysAgo) =>
        await _factory.QueryAsync(async db =>
        {
            db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = "pin_changed", Actor = "اختبار", EntityId = userId,
                CreatedAtUtc = DateTimeOffset.UtcNow.AddDays(-daysAgo),
            });
            return await db.SaveChangesAsync();
        });

    private async Task<LoginResultDto> LoginAsync(string userId)
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(_factory.PinFor(userId)));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<LoginResultDto>())!;
    }

    private HttpClient SessionClient(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task A_pin_older_than_90_days_must_be_changed_before_the_session_can_do_anything_else()
    {
        var userId = await _factory.SeedUserAsync("كاشير قديم", "cashier", "pos");
        await BackdatePinAsync(userId, 91);

        var login = await LoginAsync(userId);
        Assert.True(login.PinChangeRequired);
        var session = SessionClient(login.Token);
        var blocked = await session.GetAsync("/api/shifts/current");
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Contains("pinChangeRequired", await blocked.Content.ReadAsStringAsync());

        (await session.PostAsJsonAsync($"/api/users/{userId}/change-pin",
            new ChangePinRequest(userId, _factory.PinFor(userId), "582917", userId, $"pin-{Guid.NewGuid():N}"))).EnsureSuccessStatusCode();

        Assert.NotEqual(HttpStatusCode.Forbidden, (await session.GetAsync("/api/shifts/current")).StatusCode);
    }

    [Fact]
    public async Task A_recent_pin_and_an_account_with_no_history_log_in_normally_and_the_clock_starts_at_first_login()
    {
        var recent = await _factory.SeedUserAsync("كاشير جديد", "cashier", "pos");
        await BackdatePinAsync(recent, 30);
        Assert.False((await LoginAsync(recent)).PinChangeRequired);

        var noHistory = await _factory.SeedUserAsync("حساب قبل القاعدة", "cashier", "pos");
        Assert.False((await LoginAsync(noHistory)).PinChangeRequired);
        Assert.Equal(1, await _factory.QueryAsync(db => db.ControlAuditEvents.CountAsync(e => e.EntityId == noHistory && e.Action == "pin_age_baseline")));
    }

    [Fact]
    public async Task A_print_agent_account_is_never_asked_to_change_its_pin()
    {
        var agent = await _factory.SeedUserAsync("وكيل الطباعة", "cashier", "print_agent");
        await BackdatePinAsync(agent, 400);

        var login = await LoginAsync(agent);

        Assert.False(login.PinChangeRequired);
        Assert.NotEqual(HttpStatusCode.Forbidden, (await SessionClient(login.Token).GetAsync("/api/print-jobs/pending?route=Receipt")).StatusCode);
    }
}
