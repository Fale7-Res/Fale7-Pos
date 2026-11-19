namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.CashDrawer;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Domain.Printing;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// The agreed operating workflow (owner, 2026-11-08), end to end against a real test database:
/// - A driver delivers across two shifts and settles with the cashier who receives his money. He earns the full
///   delivery fee recorded on each order he delivered; the net cash goes into the receiving shift's drawer.
/// - A partial settlement carries its remainder forward; fees are recognised exactly once.
/// - Prepaid deliveries are never collected again, and the fees owed on them are paid out of the drawer
///   explicitly, never clamped to zero.
/// - A close splits counted cash into the float left in the drawer and the cash handed to a named manager, and
///   the next shift opens on that float without a second issue from the treasury.
/// Amounts are piastres; 100 = 1 EGP.
/// </summary>
public class DriverFeeSettlementAndHandoverTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierA = default!;
    private string _cashierB = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;
    private string _dayId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierA = await _factory.SeedUserAsync("كاشير الصباح", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _cashierB = await _factory.SeedUserAsync("كاشير المساء", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _factory.StationUserId = _managerId;
        _dayId = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---------- helpers ----------

    private static async Task<HttpResponseMessage> Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r;
    }

    private Task<long> BalanceAsync(string accountId) => _factory.QueryAsync(async db =>
        Falah.Domain.Financial.FinancialRules.TreasuryBalanceFromMovements(
            await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsAsync(db, accountId, default), accountId).Piastres);

    private async Task<OpenShiftResult> OpenShiftAsync(string cashierId, long openingPiastres, string name = "صباحية") =>
        (await (await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", name, cashierId, openingPiastres)))).Content.ReadFromJsonAsync<OpenShiftResult>())!;

    private async Task<long> ExpectedAsync(string shiftId, long counted) =>
        (await (await Ok(_client.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, counted))))
            .Content.ReadFromJsonAsync<PrepareShiftCloseResult>())!.ExpectedCashPiastres;

    private async Task<CloseShiftResult> CloseAsync(string shiftId, string cashierId, long counted, long? retained, string? receiverId, string? reason = null)
    {
        await ExpectedAsync(shiftId, counted);
        return (await (await Ok(_client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"close-{Guid.NewGuid():N}", shiftId, cashierId, counted, reason, null, null, retained, receiverId))))
            .Content.ReadFromJsonAsync<CloseShiftResult>())!;
    }

    private async Task SellCashAsync(string shiftId, string cashierId, long price)
    {
        var productId = await _factory.SeedProductAsync(price);
        await Ok(_client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: price, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null)));
    }

    /// <summary>A delivery order with its own area fee. Cash is collected on delivery; card is prepaid at the till.</summary>
    private async Task<(string OrderId, string DeliveryId, long Total)> DeliveryOrderAsync(string shiftId, string cashierId, long food, long fee, string method, string area)
    {
        await _factory.SeedDeliveryServiceAsync($"svc-{area}", fee, area); // AUD-01: the zone the order names, at the fee it sends
        var productId = await _factory.SeedProductAsync(food);
        var response = await Ok(_client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: cashierId, ShiftId: shiftId,
            OrderType: "delivery", PaymentMethod: method,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: fee, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: method == "cash" ? 0 : food + fee, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: "عميل", CustomerPhone: "01000000000", DeliveryAddress: "شارع", DeliveryArea: area, BusinessDayId: null,
            DeliveryStreet: "شارع 9", DeliveryServiceId: $"svc-{area}", DeliveryServiceName: area)));
        var order = (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == order.OrderId).Select(d => d.Id).FirstAsync());
        var total = await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.TotalPiastres).FirstAsync());
        Assert.Equal(food + fee, total);
        return (order.OrderId, deliveryId, total);
    }

    private async Task<string> CreateDriverAsync(string name = "سائق الاختبار") =>
        (await (await Ok(_client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", name, _dispatcherId)))).Content.ReadFromJsonAsync<DriverDto>())!.Id;

    private async Task DispatchAsync(string deliveryId, string driverId)
    {
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null)));
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcherId, null, null)));
    }

    private Task<HttpResponseMessage> ResultAsync(string deliveryId, string result, string? reason = null, string? disposition = null) =>
        _client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result",
            new RecordDeliveryResultRequest($"req-{Guid.NewGuid():N}", deliveryId, result, null, reason, null, _dispatcherId, null, null, disposition));

    private Task<DriverAccountDto> AccountAsync(string driverId) =>
        _client.GetFromJsonAsync<DriverAccountDto>($"/api/delivery/drivers/{driverId}/account?actorUserId={_dispatcherId}")!;

    private static SettleDriverCustodyRequest Settle(string driverId, string actorId, long received, long paid = 0, bool keepOpen = false,
        string? reason = null, string? approvalId = null, string? approverId = null) =>
        new($"set-{Guid.NewGuid():N}", driverId, received, reason, actorId, approvalId, approverId, paid, keepOpen);

    private Task<HttpResponseMessage> PostSettleAsync(SettleDriverCustodyRequest request, HttpClient? client = null) =>
        (client ?? _client).PostAsJsonAsync($"/api/delivery/drivers/{request.DriverId}/settle", request);

    private async Task<SettleDriverCustodyResult> SettleOkAsync(SettleDriverCustodyRequest request) =>
        (await (await Ok(PostSettleAsync(request))).Content.ReadFromJsonAsync<SettleDriverCustodyResult>())!;

    private async Task<List<string>> BlockerCodesAsync() =>
        (await _client.GetFromJsonAsync<List<CloseBlockerProbe>>($"/api/business-day/{_dayId}/close-blockers"))!.Select(b => b.Code).ToList();

    private sealed record CloseBlockerProbe(string Code, string EntityType, string EntityId, string Message);

    // ---------- the full trading cycle ----------

    /// <summary>
    /// The acceptance scenario in the handover report, with its exact money:
    /// Shift A opens on 990, gets a 10 top-up, sells 300 cash, pays a 50 expense; a driver takes two cash orders
    /// (area fees 20 and 30) and one card-prepaid order (fee 25). A closes on 1,250 counted, leaves 1,000 and hands
    /// 250 to the manager. Shift B opens on the 1,000 left, sells 100, receives the driver: collected 350, fees 75,
    /// net 275 - first 200 (partial, 75 carried), then the 75. B closes on 1,375, leaves 1,000, hands 375.
    /// </summary>
    [Fact]
    public async Task A_driver_across_two_shifts_settles_his_net_after_fees_into_the_receiving_drawer_and_the_float_carries_over()
    {
        var main0 = await BalanceAsync("tre-main");

        // ---- shift A ----
        var a = await OpenShiftAsync(_cashierA, 99_000);
        Assert.Equal(99_000, a.OpeningCashPiastres);
        Assert.Null(a.InheritedFloatPiastres);
        // The manager tops the drawer up to the 1,000 target: a separate, traceable movement from the treasury.
        await Ok(_client.PostAsJsonAsync("/api/cash-drawer/cash-movement",
            new CashierCashMovementRequest($"cm-{Guid.NewGuid():N}", a.ShiftId, "cash_in", 1_000, "إكمال عهدة الدرج إلى 1000", _managerId, null, null)));
        await SellCashAsync(a.ShiftId, _cashierA, 30_000);
        await Ok(_client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", a.ShiftId, "مستلزمات تشغيل", 5_000, "شراء ثلج", "tre-drawer", _managerId, null, null)));

        var driverId = await CreateDriverAsync();
        var o1 = await DeliveryOrderAsync(a.ShiftId, _cashierA, 20_000, 2_000, "cash", "المنطقة-1");
        var o2 = await DeliveryOrderAsync(a.ShiftId, _cashierA, 10_000, 3_000, "cash", "المنطقة-2");
        var o3 = await DeliveryOrderAsync(a.ShiftId, _cashierA, 15_000, 2_500, "card", "المنطقة-3");
        foreach (var o in new[] { o1, o2, o3 }) await DispatchAsync(o.DeliveryId, driverId);
        await Ok(ResultAsync(o1.DeliveryId, "Delivered"));                 // delivered during A

        // Cash orders on the way are not drawer cash: 990 + 10 + 300 - 50.
        Assert.Equal(125_000, await ExpectedAsync(a.ShiftId, 125_000));
        var closeA = await CloseAsync(a.ShiftId, _cashierA, 125_000, retained: 100_000, receiverId: _managerId);
        Assert.Equal(0, closeA.DifferencePiastres);
        Assert.Equal(100_000, closeA.RetainedFloatPiastres);
        Assert.Equal(25_000, closeA.HandedOverPiastres);
        Assert.Equal("المدير", closeA.HandoverReceiverName);
        var handoverA = await _factory.QueryAsync(db => db.TreasuryMovements.SingleAsync(m => m.ShiftId == a.ShiftId && m.Type == "shift_handover"));
        Assert.Equal(25_000, handoverA.AmountPiastres);                     // only what physically left the drawer

        // ---- shift B opens on what A left: no second float from the treasury ----
        var mainBeforeB = await BalanceAsync("tre-main");
        var b = await OpenShiftAsync(_cashierB, 100_000, "مسائية");
        Assert.Equal(100_000, b.InheritedFloatPiastres);
        Assert.Equal(100_000, b.OpeningCashPiastres);
        Assert.Equal(mainBeforeB, await BalanceAsync("tre-main"));
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == b.ShiftId && m.Type == "shift_opening_float")));
        Assert.Equal(100_000, await BalanceAsync("tre-drawer"));             // the drawer ledger holds exactly the float

        await Ok(ResultAsync(o2.DeliveryId, "Delivered"));                 // delivered during B
        await Ok(ResultAsync(o3.DeliveryId, "Delivered"));                 // prepaid: nothing to collect
        await SellCashAsync(b.ShiftId, _cashierB, 10_000);

        // ---- the driver's account: collected 350, fees 75 (20 + 30 + 25), net 275 ----
        var account = await AccountAsync(driverId);
        Assert.Equal(35_000, account.CashCustodyPiastres);
        Assert.Equal(7_500, account.PendingFeesPiastres);
        Assert.Equal(27_500, account.NetDuePiastres);
        Assert.True(account.CanSettle);

        var ordersBefore = await _factory.QueryAsync(db => db.Orders.CountAsync());
        var partial = await SettleOkAsync(Settle(driverId, _dispatcherId, 20_000, keepOpen: true));
        Assert.Equal(35_000, partial.CashCollectedPiastres);
        Assert.Equal(7_500, partial.FeeEntitlementPiastres);
        Assert.Equal(7_500, partial.CarriedOutPiastres);
        Assert.Equal(0, partial.DifferencePiastres);
        Assert.Equal(b.ShiftId, partial.ShiftId);
        Assert.Contains("UNSETTLED_DRIVER_BALANCE", await BlockerCodesAsync());

        var carried = await AccountAsync(driverId);
        Assert.Equal(7_500, carried.CarriedBalancePiastres);
        Assert.Equal(0, carried.PendingFeesPiastres);                       // fees are recognised once, in the partial
        Assert.Equal(7_500, carried.NetDuePiastres);
        Assert.True(carried.CanSettle);

        var final = await SettleOkAsync(Settle(driverId, _dispatcherId, 7_500));
        Assert.Equal(7_500, final.CarriedInPiastres);
        Assert.Equal(0, final.FeeEntitlementPiastres);
        Assert.Equal(0, final.CarriedOutPiastres);
        Assert.Equal(0, final.DifferencePiastres);
        Assert.DoesNotContain("UNSETTLED_DRIVER_BALANCE", await BlockerCodesAsync());
        Assert.Equal(ordersBefore, await _factory.QueryAsync(db => db.Orders.CountAsync()));   // settlement is not a sale

        var intoDrawer = await _factory.QueryAsync(db => db.TreasuryMovements
            .Where(m => m.Type == "driver_settlement").Select(m => new { m.AmountPiastres, m.DestinationAccountId, m.ShiftId }).ToListAsync());
        Assert.Equal(new long[] { 20_000, 7_500 }, intoDrawer.Select(m => m.AmountPiastres).OrderByDescending(x => x).ToArray());
        Assert.All(intoDrawer, m => { Assert.Equal("tre-drawer", m.DestinationAccountId); Assert.Equal(b.ShiftId, m.ShiftId); });

        // ---- close B: 1,000 + 100 sale + 275 from the driver ----
        Assert.Equal(137_500, await ExpectedAsync(b.ShiftId, 137_500));
        var closeB = await CloseAsync(b.ShiftId, _cashierB, 137_500, retained: 100_000, receiverId: _managerId);
        Assert.Equal(37_500, closeB.HandedOverPiastres);

        var statementB = await _factory.QueryAsync(db => ShiftCloseStatements.LoadFrozenAsync(db, b.ShiftId, default));
        Assert.NotNull(statementB);
        Assert.Equal(100_000, statementB!.Cash.OpeningPiastres);
        Assert.Equal(100_000, statementB.Cash.InheritedFloatPiastres);
        Assert.Equal(27_500, statementB.Cash.DriverCashPiastres);
        Assert.Equal(10_000, statementB.Cash.CashSalesPiastres);
        Assert.Equal(137_500, statementB.Cash.ExpectedPiastres);
        Assert.Equal(100_000, statementB.Cash.RetainedFloatPiastres);
        Assert.Equal(37_500, statementB.Cash.HandedOverPiastres);
        Assert.Equal("المدير", statementB.Cash.HandoverReceiverName);
        Assert.Equal(statementB.Cash.CountedPiastres, statementB.Cash.RetainedFloatPiastres + statementB.Cash.HandedOverPiastres);

        var statementA = await _factory.QueryAsync(db => ShiftCloseStatements.LoadFrozenAsync(db, a.ShiftId, default));
        Assert.Equal(1_000, statementA!.Cash.CashInPiastres);
        Assert.Equal(5_000, statementA.Cash.DrawerExpensesPiastres);
        Assert.Null(statementA.Cash.InheritedFloatPiastres);

        // The treasury: 990 + 10 issued, 250 + 375 handed back. The 1,000 float never left the drawer.
        Assert.Equal(main0 - 99_000 - 1_000 + 25_000 + 37_500, await BalanceAsync("tre-main"));
        Assert.Equal(100_000, await BalanceAsync("tre-drawer"));

        // The owner's dashboard reconciles: collected = the payment channels + what the drivers handed in + the
        // fees they kept. 300 + 100 cash, 175 card, 275 handed in, 75 kept = 925 = every order sold.
        var dashboard = (await _client.GetFromJsonAsync<Falah.Application.Reports.OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={Falah.Domain.Common.BusinessClock.Today():yyyy-MM-dd}"))!;
        Assert.Equal(27_500, dashboard.Cash.SettledFromDriversPiastres);
        Assert.Equal(7_500, dashboard.Cash.DriverFeesRetainedPiastres);
        Assert.Equal(92_500, dashboard.Cash.CollectedSalesPiastres);
        Assert.Equal(dashboard.Cash.CollectedSalesPiastres,
            dashboard.Cash.CashPiastres + dashboard.Cash.CardPiastres + dashboard.Cash.InstaPayPiastres + dashboard.Cash.WalletPiastres
            + dashboard.Cash.SettledFromDriversPiastres + dashboard.Cash.DriverFeesRetainedPiastres);
    }

    // ---------- prepaid: the restaurant owes the driver ----------

    [Fact]
    public async Task Fees_on_prepaid_deliveries_are_paid_out_of_the_drawer_explicitly_and_never_clamped_to_zero()
    {
        var shift = await OpenShiftAsync(_cashierA, 50_000);
        var driverId = await CreateDriverAsync();
        var prepaid = await DeliveryOrderAsync(shift.ShiftId, _cashierA, 15_000, 2_500, "card", "المنطقة-3");
        await DispatchAsync(prepaid.DeliveryId, driverId);
        await Ok(ResultAsync(prepaid.DeliveryId, "Delivered"));

        var account = await AccountAsync(driverId);
        Assert.Equal(0, account.CashCustodyPiastres);                       // prepaid: nothing collected again
        Assert.Equal(-2_500, account.NetDuePiastres);

        // Paying more than he is owed, or receiving and paying at once, is refused.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PostSettleAsync(Settle(driverId, _dispatcherId, 0, paid: 3_000))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PostSettleAsync(Settle(driverId, _dispatcherId, 100, paid: 2_500))).StatusCode);

        // Left open, the debt to him is carried - not dropped - and blocks the day close.
        var carried = await SettleOkAsync(Settle(driverId, _dispatcherId, 0, keepOpen: true));
        Assert.Equal(-2_500, carried.CarriedOutPiastres);
        Assert.Equal(2_500, carried.FeeEntitlementPiastres);
        Assert.Contains("UNSETTLED_DRIVER_BALANCE", await BlockerCodesAsync());
        Assert.Equal(-2_500, (await AccountAsync(driverId)).NetDuePiastres);

        var expectedBefore = await ExpectedAsync(shift.ShiftId, 0);
        var paid = await SettleOkAsync(Settle(driverId, _dispatcherId, 0, paid: 2_500));
        Assert.Equal(2_500, paid.PaidToDriverPiastres);
        Assert.Equal(0, paid.FeeEntitlementPiastres);                       // not a second fee
        Assert.Equal(0, paid.CarriedOutPiastres);
        Assert.NotNull(paid.PayoutTreasuryMovementId);
        var payout = await _factory.QueryAsync(db => db.TreasuryMovements.SingleAsync(m => m.Type == "driver_fee_payout"));
        Assert.Equal("tre-drawer", payout.SourceAccountId);
        Assert.Equal(shift.ShiftId, payout.ShiftId);
        Assert.Equal(expectedBefore - 2_500, await ExpectedAsync(shift.ShiftId, 0));
        Assert.DoesNotContain("UNSETTLED_DRIVER_BALANCE", await BlockerCodesAsync());

        // Nothing is left to settle.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PostSettleAsync(Settle(driverId, _dispatcherId, 0))).StatusCode);
    }

    [Fact]
    public async Task A_failed_cash_delivery_earns_no_fee_and_its_cash_is_not_expected_from_the_driver()
    {
        var shift = await OpenShiftAsync(_cashierA, 50_000);
        var driverId = await CreateDriverAsync();
        var delivered = await DeliveryOrderAsync(shift.ShiftId, _cashierA, 20_000, 2_000, "cash", "المنطقة-1");
        var failed = await DeliveryOrderAsync(shift.ShiftId, _cashierA, 10_000, 3_000, "cash", "المنطقة-2");
        await DispatchAsync(delivered.DeliveryId, driverId);
        await DispatchAsync(failed.DeliveryId, driverId);
        await Ok(ResultAsync(delivered.DeliveryId, "Delivered"));
        await Ok(ResultAsync(failed.DeliveryId, "Failed", reason: "customer_unreachable", disposition: "not_prepared"));

        var account = await AccountAsync(driverId);
        Assert.Equal(22_000, account.CashCustodyPiastres);
        Assert.Equal(2_000, account.PendingFeesPiastres);                   // the failed delivery's 30 is not earned
        Assert.Equal(20_000, account.NetDuePiastres);

        var settled = await SettleOkAsync(Settle(driverId, _dispatcherId, 20_000));
        Assert.Equal(2_000, settled.FeeEntitlementPiastres);
        Assert.Equal(0, settled.DifferencePiastres);
    }

    [Fact]
    public async Task A_short_final_settlement_records_the_difference_with_a_reason_and_a_manager_approval()
    {
        var shift = await OpenShiftAsync(_cashierA, 50_000);
        var driverId = await CreateDriverAsync();
        var order = await DeliveryOrderAsync(shift.ShiftId, _cashierA, 20_000, 2_000, "cash", "المنطقة-1");
        await DispatchAsync(order.DeliveryId, driverId);
        await Ok(ResultAsync(order.DeliveryId, "Delivered"));

        // Due 200; he brings 195. No reason or approval: refused, nothing posted.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await PostSettleAsync(Settle(driverId, _dispatcherId, 19_500))).StatusCode);
        Assert.Equal(0, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "driver_settlement")));

        var approval = (await (await Ok(_factory.PostAsSessionOfAsync(_client, _dispatcherId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "delivery_settlement_approve", "عجز 5 ج")))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
        var result = await SettleOkAsync(Settle(driverId, _dispatcherId, 19_500, reason: "عجز 5 ج", approvalId: approval.ApprovalId, approverId: approval.ApproverUserId));
        Assert.Equal(-500, result.DifferencePiastres);
        Assert.Equal(20_000, result.ExpectedPiastres);
        Assert.Equal(0, result.CarriedOutPiastres);
    }

    // ---------- duplicates, concurrency, a lost response and a restart ----------

    [Fact]
    public async Task Two_concurrent_final_settlements_of_a_carried_balance_post_it_exactly_once()
    {
        var shift = await OpenShiftAsync(_cashierA, 50_000);
        var driverId = await CreateDriverAsync();
        var order = await DeliveryOrderAsync(shift.ShiftId, _cashierA, 20_000, 2_000, "cash", "المنطقة-1");
        await DispatchAsync(order.DeliveryId, driverId);
        await Ok(ResultAsync(order.DeliveryId, "Delivered"));
        await SettleOkAsync(Settle(driverId, _dispatcherId, 15_000, keepOpen: true));   // 50 carried

        var first = PostSettleAsync(Settle(driverId, _dispatcherId, 5_000));
        var second = PostSettleAsync(Settle(driverId, _dispatcherId, 5_000));
        var responses = await Task.WhenAll(first, second);
        Assert.Single(responses, r => r.IsSuccessStatusCode);
        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.UnprocessableEntity);
        var amounts = await _factory.QueryAsync(db => db.TreasuryMovements.Where(m => m.Type == "driver_settlement").Select(m => m.AmountPiastres).ToListAsync());
        Assert.Equal(new long[] { 15_000, 5_000 }, amounts.OrderByDescending(x => x).ToArray());
        Assert.Equal(0, (await AccountAsync(driverId)).CarriedBalancePiastres);
    }

    [Fact]
    public async Task A_settlement_whose_response_was_lost_replays_after_a_server_restart_without_posting_again()
    {
        var shift = await OpenShiftAsync(_cashierA, 50_000);
        var driverId = await CreateDriverAsync();
        var order = await DeliveryOrderAsync(shift.ShiftId, _cashierA, 20_000, 2_000, "cash", "المنطقة-1");
        await DispatchAsync(order.DeliveryId, driverId);
        await Ok(ResultAsync(order.DeliveryId, "Delivered"));

        var request = Settle(driverId, _dispatcherId, 20_000);
        _ = await PostSettleAsync(request);                                  // committed; the response never reached the till

        // A new server process on the same database; the till re-sends the request it kept on disk.
        using var restarted = new FalahApiFactory(reuseDatabaseName: _factory.DatabaseName);
        var retried = await PostSettleAsync(request, restarted.CreateAuthenticatedClient(_factory));
        Assert.True(retried.IsSuccessStatusCode, await retried.Content.ReadAsStringAsync());
        var replay = (await retried.Content.ReadFromJsonAsync<SettleDriverCustodyResult>())!;
        Assert.True(replay.WasReplayed);
        Assert.Equal(2_000, replay.FeeEntitlementPiastres);
        Assert.Equal(20_000, replay.ActualPiastres);
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "driver_settlement")));
        Assert.Equal(1, await _factory.QueryAsync(db => db.DriverSettlements.CountAsync()));
    }

    // ---------- the close and the next opening ----------

    [Fact]
    public async Task A_shortage_stays_with_the_shift_that_had_it_and_the_next_cashier_is_not_charged_for_an_inherited_gap()
    {
        var main0 = await BalanceAsync("tre-main");
        var a = await OpenShiftAsync(_cashierA, 100_000);
        await SellCashAsync(a.ShiftId, _cashierA, 20_000);
        // Expected 1,200; counted 1,195 (5 short, within the threshold): A carries the shortage, whatever it retains.
        var closeA = await CloseAsync(a.ShiftId, _cashierA, 119_500, retained: 100_000, receiverId: _managerId, reason: "فكة ناقصة");
        Assert.Equal(-500, closeA.DifferencePiastres);
        Assert.Equal(19_500, closeA.HandedOverPiastres);

        // B finds only 990 in the drawer: the 10 gap is recorded on its own against A's handover, and B starts from 990.
        var b = await OpenShiftAsync(_cashierB, 99_000, "مسائية");
        Assert.Equal(100_000, b.InheritedFloatPiastres);
        Assert.Equal(99_000, b.OpeningCashPiastres);
        var gap = await _factory.QueryAsync(db => db.TreasuryMovements.SingleAsync(m => m.Type == "opening_float_shortfall"));
        Assert.Equal(1_000, gap.AmountPiastres);
        Assert.Equal("tre-drawer", gap.SourceAccountId);
        Assert.Null(gap.ShiftId);
        Assert.Equal(a.ShiftId, gap.ReferenceId);
        Assert.Equal(99_000, await _factory.ExpectedDrawerCashAsync(b.ShiftId));       // B is answerable for 990, not 1,000
        Assert.Equal(99_000, await BalanceAsync("tre-drawer"));             // the drawer ledger states what is really there

        // B hands everything over: an overage is the counted figure, not hidden in the float.
        var closeB = await CloseAsync(b.ShiftId, _cashierB, 99_500, retained: 0, receiverId: _managerId, reason: "زيادة");
        Assert.Equal(500, closeB.DifferencePiastres);
        Assert.Equal(99_500, closeB.HandedOverPiastres);
        Assert.Equal(0, await BalanceAsync("tre-drawer"));

        // The next opening on an empty drawer issues its float from the treasury, as before.
        var c = await OpenShiftAsync(_cashierA, 100_000);
        Assert.Null(c.InheritedFloatPiastres);
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == c.ShiftId && m.Type == "shift_opening_float")));
        // Treasury: 1,000 out, 195 + 995 back, 1,000 out again.
        Assert.Equal(main0 - 100_000 + 19_500 + 99_500 - 100_000, await BalanceAsync("tre-main"));
    }

    [Fact]
    public async Task The_handover_split_and_receiver_are_validated()
    {
        var a = await OpenShiftAsync(_cashierA, 50_000);
        await ExpectedAsync(a.ShiftId, 50_000);
        CloseShiftRequest Close(long? retained, string? receiver) =>
            new($"close-{Guid.NewGuid():N}", a.ShiftId, _cashierA, 50_000, null, null, null, retained, receiver);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/shifts/close", Close(60_000, _managerId))).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/shifts/close", Close(-1, _managerId))).StatusCode);
        // A cashier without shifts_manage cannot be named as the manager who took the cash.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await _client.PostAsJsonAsync("/api/shifts/close", Close(10_000, _cashierB))).StatusCode);
        Assert.Equal("Open", await _factory.QueryAsync(db => db.Shifts.Where(s => s.Id == a.ShiftId).Select(s => s.Status).FirstAsync()));

        // Left out, the old behaviour holds: everything counted is handed over.
        var closed = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/close", Close(null, null)))).Content.ReadFromJsonAsync<CloseShiftResult>())!;
        Assert.Equal(0, closed.RetainedFloatPiastres);
        Assert.Equal(50_000, closed.HandedOverPiastres);
        var receivers = await _client.GetFromJsonAsync<List<HandoverReceiverDto>>("/api/shifts/handover-receivers");
        Assert.Contains(receivers!, r => r.UserId == _managerId);
        Assert.DoesNotContain(receivers!, r => r.UserId == _cashierB);
    }

    [Fact]
    public async Task A_shift_running_past_midnight_hands_its_float_to_the_next_shift_in_the_same_business_day()
    {
        // The trading night: the business day opened yesterday evening and is still running now.
        var yesterday = Falah.Domain.Common.BusinessClock.Today().AddDays(-1);
        await _factory.QueryAsync(async db =>
        {
            var day = await db.BusinessDays.SingleAsync(d => d.Id == _dayId);
            day.BusinessDate = yesterday;
            day.OpenedAtUtc = DateTimeOffset.UtcNow.AddHours(-6);
            return await db.SaveChangesAsync();
        });
        var a = await OpenShiftAsync(_cashierA, 100_000, "مسائية");
        await _factory.QueryAsync(async db =>                                // opened before Cairo midnight
        {
            var s = await db.Shifts.SingleAsync(x => x.Id == a.ShiftId);
            s.OpenedAtUtc = DateTimeOffset.UtcNow.AddHours(-5);
            return await db.SaveChangesAsync();
        });
        await SellCashAsync(a.ShiftId, _cashierA, 30_000);
        var closeA = await CloseAsync(a.ShiftId, _cashierA, 130_000, retained: 100_000, receiverId: _managerId);
        Assert.Equal(30_000, closeA.HandedOverPiastres);

        var b = await OpenShiftAsync(_cashierB, 100_000, "صباحية");
        Assert.Equal(100_000, b.InheritedFloatPiastres);
        var days = await _factory.QueryAsync(db => db.Shifts.Where(s => s.Id == a.ShiftId || s.Id == b.ShiftId).Select(s => s.BusinessDayId).Distinct().ToListAsync());
        Assert.Equal(new[] { _dayId }, days);
        var statementA = await _factory.QueryAsync(db => ShiftCloseStatements.LoadFrozenAsync(db, a.ShiftId, default));
        Assert.Equal(yesterday, statementA!.BusinessDate);
        Assert.Equal(100_000, statementA.Cash.RetainedFloatPiastres);
    }

    // ---------- the statement: print failure after commit, then a safe reprint ----------

    [Fact]
    public async Task A_close_whose_paper_failed_to_print_reprints_the_same_frozen_figures_and_moves_no_money()
    {
        var a = await OpenShiftAsync(_cashierA, 100_000);
        await SellCashAsync(a.ShiftId, _cashierA, 25_000);
        var driverId = await CreateDriverAsync();
        var order = await DeliveryOrderAsync(a.ShiftId, _cashierA, 20_000, 2_000, "cash", "المنطقة-1");
        await DispatchAsync(order.DeliveryId, driverId);
        await Ok(ResultAsync(order.DeliveryId, "Delivered"));
        await SettleOkAsync(Settle(driverId, _dispatcherId, 20_000));
        await CloseAsync(a.ShiftId, _cashierA, 145_000, retained: 100_000, receiverId: _managerId);

        // The printer failed after the close committed (as the print agent reports it).
        var job = await _factory.QueryAsync(db => db.PrintJobs.SingleAsync(j => j.OrderId == a.ShiftId && j.OrderNumber == PrintRules.ShiftCloseLabel));
        await _factory.QueryAsync(async db =>
        {
            var j = await db.PrintJobs.SingleAsync(x => x.Id == job.Id);
            j.Status = "Failed";
            j.LastError = "الطابعة غير متصلة";
            return await db.SaveChangesAsync();
        });
        var original = await TicketTextAsync(job.Id);
        foreach (var line in new[] { "توريدات السائقين", "عهدة متروكة في الدرج للوردية التالية", "المستلم" })
            Assert.Contains(original, l => l.Contains(line));

        // Later activity, then the reprint.
        await OpenShiftAsync(_cashierB, 100_000, "مسائية");
        var movementsBefore = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        await Ok(_client.PostAsJsonAsync("/api/print-jobs/reprint-document",
            new ReprintDocumentRequest($"rp-{Guid.NewGuid():N}", "ShiftClose", a.ShiftId, _managerId, "الطابعة كانت مفصولة")));
        var reprint = await _factory.QueryAsync(db => db.PrintJobs.SingleAsync(j => j.OrderId == a.ShiftId && j.IsReprint));
        var reprinted = await TicketTextAsync(reprint.Id);
        Assert.Equal(original, reprinted.Skip(reprinted.Count - original.Count).ToList());
        Assert.Equal(movementsBefore, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync()));
    }

    private async Task<List<string>> TicketTextAsync(string jobId)
    {
        var agent = await _factory.SeedUserAsync($"وكيل طباعة {Guid.NewGuid():N}", "print_agent", "print_agent");
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(agent));
        var ticket = await (await client.GetAsync($"/api/print-jobs/{jobId}/ticket")).Content.ReadFromJsonAsync<ComposedTicketDto>();
        return ticket!.Lines.SelectMany(TicketDtoText.Of).Where(t => t is not null).Select(t => t!).ToList();
    }
}
