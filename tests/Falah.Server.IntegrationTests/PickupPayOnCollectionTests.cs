namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// UI-0025 (owner decision 2026-11-22): a pickup is registered and sent to the kitchen first, and the
/// customer pays when collecting it. It is a sale only once paid - at that moment and in the shift that
/// took the money. An unpaid one blocks the business-day close until it is collected or a manager
/// cancels it (nothing to refund; the food is wasted or back in stock).
/// </summary>
public class PickupPayOnCollectionTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _productId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _factory.StationUserId = _managerId;
        await _factory.FundMainAccountAsync();
        _productId = await _factory.SeedProductAsync(5000, "ساندوتش");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> OpenDayAsync() =>
        (await (await _client.PostAsJsonAsync("/api/business-day/open", new OpenBusinessDayRequest($"bd-{Guid.NewGuid():N}", _managerId)))
            .Content.ReadFromJsonAsync<OpenBusinessDayResult>())!.BusinessDayId;

    private async Task<string> OpenShiftAsync(long opening = 50000)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open", new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, opening));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task CloseShiftAsync(string shiftId, long counted)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, counted, null, null, null));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
    }

    // prepare-close records the count it is given as the shift's first blind count, so a shift that is
    // closed afterwards is prepared with the count it will be closed with.
    private async Task<long> ExpectedCashAsync(string shiftId, long counted = 0) =>
        (await (await _client.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, counted)))
            .Content.ReadFromJsonAsync<PrepareShiftCloseResult>())!.ExpectedCashPiastres;

    private Task<HttpResponseMessage> RegisterAsync(string shiftId, string orderType = "pickup") =>
        _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: orderType, PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 0, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "أحمد", CustomerPhone: "01012345678", DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null,
            PayOnCollection: true));

    private async Task<CheckoutOrderResult> RegisterOkAsync(string shiftId)
    {
        var response = await RegisterAsync(shiftId);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private Task<HttpResponseMessage> CollectAsync(string orderId, string shiftId, string method, long paid, string? requestId = null) =>
        _client.PostAsJsonAsync("/api/orders/collect-pickup",
            new CollectPickupPaymentRequest(requestId ?? $"col-{Guid.NewGuid():N}", orderId, _cashierId, shiftId, method, paid));

    private async Task<SalesReportDto> SalesTodayAsync()
    {
        var today = Falah.Domain.Common.BusinessClock.Today().ToString("yyyy-MM-dd");
        return (await _client.GetFromJsonAsync<SalesReportDto>($"/api/reports/sales?actorUserId={_managerId}&from={today}&to={today}"))!;
    }

    private async Task CompleteClosingChecklistAsync(string dayId)
    {
        foreach (var itemId in new[] { "close-shifts", "close-drawer", "close-delivery" })
            (await _client.PostAsJsonAsync($"/api/business-day/{dayId}/checklist/{itemId}",
                new CompleteChecklistItemRequest(dayId, itemId, true, _managerId))).EnsureSuccessStatusCode();
    }

    private Task<HttpResponseMessage> CloseDayAsync(string dayId) =>
        _client.PostAsJsonAsync($"/api/business-day/{dayId}/close", new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", dayId, _managerId, null, null));

    [Fact]
    public async Task A_registered_pickup_is_no_sale_until_its_customer_pays_on_collection()
    {
        await OpenDayAsync();
        var shiftId = await OpenShiftAsync();
        var registered = await RegisterOkAsync(shiftId);

        Assert.False(registered.IsPaid);
        Assert.True(registered.AwaitingCollection);
        Assert.Equal(5000, registered.TotalPiastres);
        var before = await SalesTodayAsync();
        Assert.Equal(0, before.OrderCount);
        Assert.Equal(0, before.NetSalesPiastres);
        Assert.Equal(50000, await ExpectedCashAsync(shiftId));
        var waiting = await _client.GetFromJsonAsync<List<AwaitingPickupDto>>("/api/orders/awaiting-collection");
        Assert.Equal(registered.OrderId, Assert.Single(waiting!).OrderId);

        var collected = await CollectAsync(registered.OrderId, shiftId, "cash", 6000);
        collected.EnsureSuccessStatusCode();
        var paid = (await collected.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        Assert.True(paid.IsPaid);
        Assert.Equal(1000, paid.ChangeDuePiastres);

        var after = await SalesTodayAsync();
        Assert.Equal(1, after.OrderCount);
        Assert.Equal(5000, after.NetSalesPiastres);
        Assert.Equal(5000, after.CashPiastres);
        Assert.Equal(55000, await ExpectedCashAsync(shiftId));
        Assert.Empty((await _client.GetFromJsonAsync<List<AwaitingPickupDto>>("/api/orders/awaiting-collection"))!);
    }

    [Fact]
    public async Task Collected_in_a_later_shift_the_sale_and_the_cash_belong_to_that_shift()
    {
        await OpenDayAsync();
        var first = await OpenShiftAsync();
        var registered = await RegisterOkAsync(first);
        Assert.Equal(50000, await ExpectedCashAsync(first, counted: 50000));
        await CloseShiftAsync(first, 50000);

        var second = await OpenShiftAsync();
        (await CollectAsync(registered.OrderId, second, "cash", 5000)).EnsureSuccessStatusCode();

        Assert.Equal(55000, await ExpectedCashAsync(second));
        var order = await _factory.QueryAsync(db => db.Orders.AsNoTracking().SingleAsync(o => o.Id == registered.OrderId));
        Assert.Equal(second, order.ShiftId);
        Assert.Equal(first, order.RegisteredShiftId);
        var movementShift = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .Where(m => m.ReferenceId == registered.OrderId && m.Type == "sale_collection").Select(m => m.ShiftId).SingleAsync());
        Assert.Equal(second, movementShift);
    }

    [Fact]
    public async Task A_repeated_collection_replays_and_a_second_collection_is_refused()
    {
        await OpenDayAsync();
        var shiftId = await OpenShiftAsync();
        var registered = await RegisterOkAsync(shiftId);

        (await CollectAsync(registered.OrderId, shiftId, "card", 5000, requestId: "col-same")).EnsureSuccessStatusCode();
        var replay = (await (await CollectAsync(registered.OrderId, shiftId, "card", 5000, requestId: "col-same")).Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        Assert.True(replay.WasReplayed);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await CollectAsync(registered.OrderId, shiftId, "cash", 5000)).StatusCode);

        var movements = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking()
            .CountAsync(m => m.ReferenceId == registered.OrderId && m.Type == "sale_collection"));
        Assert.Equal(1, movements);
        Assert.Equal(5000, (await SalesTodayAsync()).CardPiastres);
    }

    [Fact]
    public async Task An_unpaid_pickup_blocks_the_day_close_until_a_manager_cancels_it()
    {
        var dayId = await OpenDayAsync();
        var shiftId = await OpenShiftAsync();
        var registered = await RegisterOkAsync(shiftId);
        await CloseShiftAsync(shiftId, 50000);
        await CompleteClosingChecklistAsync(dayId);

        var blocked = await CloseDayAsync(dayId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, blocked.StatusCode);
        Assert.Contains("UNCOLLECTED_PICKUP", await blocked.Content.ReadAsStringAsync());

        // The cashier alone cannot cancel it.
        var refused = await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup",
            new CancelUncollectedPickupRequest($"cx-{Guid.NewGuid():N}", registered.OrderId, "العميل ما جاش", "not_prepared", _cashierId, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);

        var approvalId = (await (await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "pickup_cancel", "العميل ما جاش"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!.ApprovalId;
        (await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup",
            new CancelUncollectedPickupRequest($"cx-{Guid.NewGuid():N}", registered.OrderId, "العميل ما جاش", "not_prepared", _cashierId, approvalId, _managerId)))
            .EnsureSuccessStatusCode();

        (await CloseDayAsync(dayId)).EnsureSuccessStatusCode();
        Assert.Equal(0, (await SalesTodayAsync()).NetSalesPiastres);
        var noMoney = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking().CountAsync(m => m.ReferenceId == registered.OrderId));
        Assert.Equal(0, noMoney);
    }

    // Owner, 2026-11-23: cancelling an unpaid pickup always takes a manager's PIN - a cashier's own
    // permissions (even orders_refund) are not enough. A manager logged in at the till is the manager.
    [Fact]
    public async Task A_cashier_cannot_cancel_an_unpaid_pickup_without_a_managers_pin_whatever_they_hold()
    {
        var trusted = await _factory.SeedUserAsync("كاشير قديم", "cashier", "pos", "orders_refund", "shifts_open_own");
        await OpenDayAsync();
        var shiftId = await OpenShiftAsync();
        var registered = await RegisterOkAsync(shiftId);

        var refused = await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup",
            new CancelUncollectedPickupRequest($"cx-{Guid.NewGuid():N}", registered.OrderId, "العميل ما جاش", "prepared", trusted, null, null));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);

        (await _client.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup",
            new CancelUncollectedPickupRequest($"cx-{Guid.NewGuid():N}", registered.OrderId, "العميل ما جاش", "prepared", _managerId, null, null)))
            .EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Only_a_pickup_can_be_paid_on_collection()
    {
        await OpenDayAsync();
        var shiftId = await OpenShiftAsync();
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await RegisterAsync(shiftId, orderType: "takeaway")).StatusCode);
    }
}
