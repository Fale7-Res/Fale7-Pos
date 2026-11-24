namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Delivery;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Application.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// Owner, before handover (2026-11-24): "I'm afraid of defects like the printing one." That defect needed two
/// features in sequence - a manager's permission change, then a print agent's claim - and every test had
/// exercised each one alone. The first test puts every everyday station call (till, call center, dispatcher,
/// every print route, the screens' reads) as the very next request after every back-office action. Red proof
/// 2026-11-24: with IsolationLevelResetInterceptor unregistered it fails with exactly the 12 print claims after
/// the permission change and the deactivation (500) and nothing else; registered, it passes. A first version
/// that ran a sale before the claims passed WITHOUT the fix - the sale's own READ COMMITTED transaction reset the
/// connection - which is why each call is now prepared before the admin action and sent right after it.
/// The second test sends the same mix concurrently and asserts that nothing answers 500 (it does not, by
/// itself, reproduce the leak). Both end with the drawer still reconciling.
/// </summary>
public class MixedStationTrafficTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _ownerId = default!;
    private string _cashierId = default!;
    private string _agentId = default!;
    private string _shiftId = default!;
    private string _businessDayId = default!;
    private string _productId = default!;
    private string _driverId = default!;
    private readonly List<string> _failures = new();

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "delivery_manage");
        _agentId = await _factory.SeedUserAsync("وكيل الطباعة", "print_agent", "print_agent");
        await _factory.FundMainAccountAsync(1_000_000);
        await _factory.SeedDeliveryServiceAsync("svc-1", 1_500, "توصيل سريع");
        _productId = await _factory.SeedProductAsync(5_000, "فلافل");

        var day = await Ok<OpenBusinessDayResult>(As(_ownerId).PostAsJsonAsync("/api/business-day/open",
            new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _ownerId)));
        _businessDayId = day.BusinessDayId;
        _shiftId = (await Ok<OpenShiftResult>(As(_cashierId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50_000)))).ShiftId;
        _driverId = (await Ok<DriverDto>(As(_ownerId).PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"drv-{Guid.NewGuid():N}", "سائق", _ownerId)))).Id;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var response = await call;
        Assert.True(response.IsSuccessStatusCode, $"{(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    /// <summary>Records instead of throwing, so one run reports every step that failed, named.</summary>
    private async Task Expect(string step, Task<HttpResponseMessage> call, bool mustSucceed = true)
    {
        var response = await call;
        var code = (int)response.StatusCode;
        if (code < 500 && (!mustSucceed || response.IsSuccessStatusCode)) return;
        var body = await response.Content.ReadAsStringAsync();
        lock (_failures) _failures.Add($"{step}: {code} {body}");
    }

    private CheckoutOrderRequest Sale(string type) => new(
        CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
        OrderType: type, PaymentMethod: "cash",
        Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
        DiscountPiastres: 0, DeliveryFeePiastres: type == "delivery" ? 1_500 : 0, CompAmountPiastres: 0,
        CompApprovalId: null, CompApproverUserId: null, CompReason: null,
        AmountPaidPiastres: type == "takeaway" ? 5_000 : 0, SplitPayments: null, WalletProvider: null,
        TableNumber: null, CustomerName: type == "takeaway" ? null : "عميل", CustomerPhone: type == "takeaway" ? null : "01000000000",
        DeliveryAddress: null, DeliveryArea: type == "delivery" ? "المنطقة أ" : null, BusinessDayId: null,
        DeliveryStreet: type == "delivery" ? "شارع 9" : null,
        DeliveryServiceId: type == "delivery" ? "svc-1" : null, DeliveryServiceName: type == "delivery" ? "توصيل سريع" : null,
        CustomerPrintRoute: type == "takeaway" ? null : "CallCenter",
        PayOnCollection: type == "pickup");

    /// <summary>One round of everyday station traffic: a till sale, a phoned delivery and pickup, the dispatcher
    /// sending the delivery out, every print route's claim, and the reads each screen polls.</summary>
    private async Task StationRoundAsync(string label)
    {
        var till = As(_cashierId);
        await Expect($"{label}: till sale", till.PostAsJsonAsync("/api/orders/checkout", Sale("takeaway")));
        var deliveryResponse = await till.PostAsJsonAsync("/api/orders/checkout", Sale("delivery"));
        if (!deliveryResponse.IsSuccessStatusCode) { await Expect($"{label}: call-center delivery", Task.FromResult(deliveryResponse)); return; }
        var delivery = (await deliveryResponse.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        await Expect($"{label}: call-center pickup", till.PostAsJsonAsync("/api/orders/checkout", Sale("pickup")));

        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == delivery.OrderId).Select(d => d.Id).FirstAsync());
        await Expect($"{label}: mark ready", till.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready",
            new MarkDeliveryReadyRequest(deliveryId, _cashierId, null, null)));
        await Expect($"{label}: assign driver", till.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"asg-{Guid.NewGuid():N}", deliveryId, _driverId, _cashierId, null, null)));

        var agent = As(_agentId);
        foreach (var route in new[] { "Receipt", "CallCenter", "Grill", "Charcoal", "DriverDispatch", "DeliveryOrder" })
            await Expect($"{label}: print claim {route}", agent.PostAsJsonAsync("/api/print-jobs/claim", new ClaimPrintJobsRequest(route, $"station-{route}")));

        await Expect($"{label}: delivery board", till.GetAsync("/api/delivery/board"));
        await Expect($"{label}: drivers", till.GetAsync("/api/delivery/drivers"));
        await Expect($"{label}: driver account", till.GetAsync($"/api/delivery/drivers/{_driverId}/account"));
        await Expect($"{label}: orders", till.GetAsync($"/api/orders?actorUserId={_cashierId}"));
        await Expect($"{label}: current shift", till.GetAsync("/api/shifts/current"));
        await Expect($"{label}: owner day report", As(_ownerId).GetAsync($"/api/reports/business-day/{_businessDayId}?actorUserId={_ownerId}"));
    }

    /// <summary>The back-office actions a manager does during service. The first two run SERIALIZABLE
    /// transactions - the source of the 2026-11-23 leak.</summary>
    private IEnumerable<(string Name, Func<Task<HttpResponseMessage>> Call)> AdminActions()
    {
        var owner = As(_ownerId);
        yield return ("permission change", () => owner.PostAsJsonAsync($"/api/users/{_cashierId}/permissions",
            new UpdateUserPermissionsRequest(_cashierId, new[] { "cash_count" }, Array.Empty<string>(), _ownerId)));
        yield return ("user deactivated", async () =>
        {
            var userId = await _factory.SeedUserAsync($"موظف {Guid.NewGuid():N}"[..12], "cashier", "pos");
            return await owner.PostAsJsonAsync($"/api/users/{userId}/active", new SetUserActiveRequest(userId, false, "ترك العمل", _ownerId));
        });
        yield return ("manager approval", () => owner.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_ownerId), "orders_comp", "ضيافة")));
        yield return ("driver added", () => owner.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"drv-{Guid.NewGuid():N}", "سائق جديد", _ownerId)));
        yield return ("settings read", () => owner.GetAsync("/api/settings"));
    }

    /// <summary>One everyday station call. Prepare runs BEFORE the back-office action (anything it sends would
    /// itself reset a leaked connection and hide the fault); Act runs as the very next request after it, which is
    /// how the print claim met the permission change's connection on 2026-11-23.</summary>
    private sealed record StationCall(string Name, Func<Task<object?>> Prepare, Func<object?, Task<HttpResponseMessage>> Act);

    private async Task<string> ReadyDeliveryAsync(bool markReady)
    {
        var sale = await Ok<CheckoutOrderResult>(As(_cashierId).PostAsJsonAsync("/api/orders/checkout", Sale("delivery")));
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == sale.OrderId).Select(d => d.Id).FirstAsync());
        if (markReady)
            (await As(_cashierId).PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _cashierId, null, null)))
                .EnsureSuccessStatusCode();
        return deliveryId;
    }

    private IEnumerable<StationCall> StationCalls()
    {
        var till = As(_cashierId);
        var agent = As(_agentId);
        static Task<object?> Nothing() => Task.FromResult<object?>(null);

        yield return new("till sale", Nothing, _ => till.PostAsJsonAsync("/api/orders/checkout", Sale("takeaway")));
        yield return new("call-center delivery", Nothing, _ => till.PostAsJsonAsync("/api/orders/checkout", Sale("delivery")));
        yield return new("call-center pickup", Nothing, _ => till.PostAsJsonAsync("/api/orders/checkout", Sale("pickup")));
        yield return new("mark ready", async () => await ReadyDeliveryAsync(markReady: false),
            id => till.PostAsJsonAsync($"/api/delivery/{id}/ready", new MarkDeliveryReadyRequest((string)id!, _cashierId, null, null)));
        yield return new("assign driver", async () => await ReadyDeliveryAsync(markReady: true),
            id => till.PostAsJsonAsync($"/api/delivery/{id}/assign", new AssignDeliveryRequest($"asg-{Guid.NewGuid():N}", (string)id!, _driverId, _cashierId, null, null)));
        foreach (var route in new[] { "Receipt", "CallCenter", "Grill", "Charcoal", "DriverDispatch", "DeliveryOrder" })
            yield return new($"print claim {route}", Nothing, _ => agent.PostAsJsonAsync("/api/print-jobs/claim", new ClaimPrintJobsRequest(route, $"station-{route}")));
        yield return new("delivery board", Nothing, _ => till.GetAsync("/api/delivery/board"));
        yield return new("drivers", Nothing, _ => till.GetAsync("/api/delivery/drivers"));
        yield return new("driver account", Nothing, _ => till.GetAsync($"/api/delivery/drivers/{_driverId}/account"));
        yield return new("orders", Nothing, _ => till.GetAsync($"/api/orders?actorUserId={_cashierId}"));
        yield return new("current shift", Nothing, _ => till.GetAsync("/api/shifts/current"));
        yield return new("owner day report", Nothing, _ => As(_ownerId).GetAsync($"/api/reports/business-day/{_businessDayId}?actorUserId={_ownerId}"));
    }

    [Fact]
    public async Task Every_station_call_works_as_the_very_next_request_after_each_back_office_action()
    {
        foreach (var (adminName, adminCall) in AdminActions())
            foreach (var station in StationCalls())
            {
                var prepared = await station.Prepare();
                await Expect($"admin {adminName} (before {station.Name})", adminCall());
                await Expect($"{station.Name} right after {adminName}", station.Act(prepared));
            }

        Assert.True(_failures.Count == 0, "Steps that failed:\n" + string.Join("\n", _failures));
        await AssertShiftStillReconcilesAsync();
    }

    [Fact]
    public async Task Stations_and_back_office_at_the_same_moment_never_answer_500()
    {
        for (var round = 0; round < 3; round++)
        {
            var admin = AdminActions().Select(a => Expect($"round {round} admin: {a.Name}", a.Call(), mustSucceed: false));
            var stations = Enumerable.Range(0, 3).Select(i => StationRoundAsync($"round {round} station {i}"));
            await Task.WhenAll(admin.Concat(stations));
        }

        Assert.True(_failures.Count == 0, "Steps that failed:\n" + string.Join("\n", _failures));
        await AssertShiftStillReconcilesAsync();
    }

    /// <summary>After all that traffic the till still closes on its own arithmetic: the float plus the takeaway cash
    /// (deliveries are with the driver, pickups unpaid) is what the server expects in the drawer.</summary>
    private async Task AssertShiftStillReconcilesAsync()
    {
        var takeawayCash = await _factory.QueryAsync(db => db.Orders
            .Where(o => o.ShiftId == _shiftId && o.OrderType == "takeaway" && o.Status != "cancelled").SumAsync(o => o.TotalPiastres));
        var expected = await _factory.ExpectedDrawerCashAsync(_shiftId);
        Assert.Equal(50_000 + takeawayCash, expected);

        var preview = await Ok<PrepareShiftCloseResult>(As(_cashierId).PostAsJsonAsync("/api/shifts/prepare-close",
            new PrepareShiftCloseRequest(_shiftId, expected)));
        Assert.Equal((expected, 0L), (preview.ExpectedCashPiastres, preview.DifferencePiastres));
    }
}
