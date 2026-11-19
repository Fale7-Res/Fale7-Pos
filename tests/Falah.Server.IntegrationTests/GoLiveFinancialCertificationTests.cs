namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Purchasing;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// Go-live financial certification: one overnight trading day, end to end through the public API, against totals
/// calculated by hand before the run. Nothing is dropped or reclassified to make it balance; every figure below is
/// asserted to the piastre, and both reconciliations must leave zero unexplained.
///
/// The business day was opened YESTERDAY evening and is still trading after midnight: every order is made today
/// (calendar) inside yesterday's business day. Main is funded with 1,000,000 (capital). One product at 10,000 whose
/// recipe costs 3,000; delivery zone fee 1,000.
///
///   Shift opens on a 50,000 float (main -> drawer).
///   S1 cash 10,000 · S2 card 10,000 · S3 InstaPay 10,000 confirmed · S4 InstaPay 10,000 never paid, voided (not a sale)
///   S5 cash 10,000, 4,000 refunded in cash from the drawer
///   D1 COD 11,000 delivered by driver A, full · D2 COD 11,000 delivered by driver A, 9,000 collected (manager approved),
///      the 2,000 classified as an approved discount
///   Driver A settles: collected 20,000 − his fees 2,000 = 18,000 into the drawer
///   Expense 5,000 typed, 500 paid: 4,500 corrected as a posting error
///   Safe drop 20,000 typed, 2,000 dropped: 18,000 corrected
///   Supplier invoice 30,000 on credit; supplier credit 5,000 -> owed 25,000
///   Transfer main -> bank 100,000 that never happened, corrected in full
///   Expected drawer = 50,000 + 10,000 + 10,000 − 4,000 + 18,000 − 500 − 2,000 = 81,500; counted 81,500, handed to the
///      manager, who confirms it
///
///   Sales: gross 62,000 (S1 S2 S3 S5 D1 D2), refunds 4,000, net 58,000.
///   Bridge: 40,000 through the treasury + 20,000 settled by the driver + 2,000 approved discount = 62,000, residual 0.
///   Customer receipts: 40,000 sales + 18,000 remittance = 58,000 (cash 38,000, electronic 20,000).
///   Balances: main 1,000,000 − 50,000 + 2,000 + 81,500 = 1,033,500; drawer 0; bank 10,000; InstaPay 10,000 -> 1,053,500
///      = 1,000,000 + 58,000 − 4,000 − 500.
///   Estimated operating result: 58,000 − food 6 × 3,000 − expenses 500 − driver pay 2,000 − approved discount 2,000 = 35,500.
/// </summary>
public class GoLiveFinancialCertificationTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;
    private string _dayId = default!;
    private string _productId = default!;
    private string _itemId = default!;
    private static readonly DateOnly Today = BusinessClock.Today();
    private static readonly DateOnly Yesterday = Today.AddDays(-1);

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer", "orders_refund", "safe_drop");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _dayId = $"day-{Guid.NewGuid():N}";
        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<Falah.Infrastructure.Persistence.FalahDbContext>();
            db.BusinessDays.Add(new BusinessDayEntity
            {
                Id = _dayId, BusinessDate = Yesterday, OpenedBy = "seed", OpenedAtUtc = DateTimeOffset.UtcNow.AddHours(-6),
                Status = "Open", CloseRevision = 0, OpenRequestId = $"seed-{Guid.NewGuid():N}",
            });
            await db.SaveChangesAsync();
        }
        await _factory.FundMainAccountAsync(1_000_000);
        await _factory.SeedDeliveryServiceAsync("svc-z", 1_000, "المنطقة");
        (_productId, _, _itemId) = await _factory.SeedProductWithRecipeAsync(10_000, 1_000);
        await _factory.QueryAsync(db => db.InventoryItems.Where(i => i.Id == _itemId)
            .ExecuteUpdateAsync(s => s.SetProperty(i => i.EstimatedUnitCostPiastres, 3_000L)));
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task<T> Ok<T>(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return (await r.Content.ReadFromJsonAsync<T>())!;
    }

    private static async Task Accept(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
    }

    private Task<HttpResponseMessage> As(string userId, string path, object body) => _factory.PostAsSessionOfAsync(_client, userId, path, body);

    private Task<CheckoutOrderResult> SellAsync(string shiftId, string type, string payment)
    {
        var delivery = type == "delivery";
        return Ok<CheckoutOrderResult>(_client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId, OrderType: type, PaymentMethod: payment,
            Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: delivery ? 1_000 : 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: delivery ? 0 : 10_000, SplitPayments: null, WalletProvider: null, TableNumber: null,
            CustomerName: delivery ? "عميل" : null, CustomerPhone: delivery ? "01000000000" : null, DeliveryAddress: null,
            DeliveryArea: delivery ? "المنطقة" : null, BusinessDayId: null, DeliveryStreet: delivery ? "شارع" : null,
            DeliveryServiceId: delivery ? "svc-z" : null, DeliveryServiceName: delivery ? "المنطقة" : null)));
    }

    private async Task<string> IssueAsync(string tillUserId, string action) =>
        (await Ok<IssueManagerApprovalResult>(As(tillUserId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), action, "اعتماد")))).ApprovalId;

    private async Task<string> DeliverAsync(string orderId, string driverId, long? collected = null)
    {
        var deliveryId = await _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());
        await Accept(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null)));
        await Accept(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcherId, null, null)));
        string? approval = collected is null ? null : await IssueAsync(_dispatcherId, "delivery_settlement_approve");
        await Accept(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest(
            $"req-{Guid.NewGuid():N}", deliveryId, "delivered", collected, collected is null ? null : "العميل دفع أقل", null, _dispatcherId,
            approval, approval is null ? null : _managerId, null)));
        return deliveryId;
    }

    private static FinancialKpiDto Kpi(OwnerFinancialReportDto r, string key) => r.Sections.SelectMany(s => s.Kpis).Single(k => k.Key == key);

    [Fact]
    public async Task An_overnight_trading_day_reconciles_to_the_piastre_from_the_first_sale_to_the_business_day_close()
    {
        var shiftId = (await Ok<OpenShiftResult>(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "مسائية", _cashierId, 50_000)))).ShiftId;
        Assert.Equal(_dayId, await _factory.QueryAsync(db => db.Shifts.Where(s => s.Id == shiftId).Select(s => s.BusinessDayId).SingleAsync()));

        // ---- Sales, a void and a refund ----
        await SellAsync(shiftId, "takeaway", "cash");
        await SellAsync(shiftId, "takeaway", "card");
        var s3 = await SellAsync(shiftId, "takeaway", "instapay");
        await Ok<ConfirmElectronicPaymentResult>(_client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(s3.OrderId, _cashierId)));
        var s4 = await SellAsync(shiftId, "takeaway", "instapay");
        await Ok<CancelPendingElectronicPaymentResult>(_client.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment",
            new CancelPendingElectronicPaymentRequest(s4.OrderId, "التحويل لم يصل", _cashierId, "not_prepared")));
        var s5 = await SellAsync(shiftId, "takeaway", "cash");
        await Ok<RefundOrderResult>(_client.PostAsJsonAsync("/api/orders/refund",
            new RefundOrderRequest($"ref-{Guid.NewGuid():N}", s5.OrderId, 4_000, "صنف ناقص", _cashierId, null, null)));

        // ---- Delivery, an undercollection classified with evidence, the driver's settlement ----
        var driverA = (await Ok<DriverDto>(_client.PostAsJsonAsync("/api/delivery/drivers", new CreateDriverRequest($"req-{Guid.NewGuid():N}", "سائق أ", _dispatcherId)))).Id;
        var d1 = await SellAsync(shiftId, "delivery", "cash");
        var d2 = await SellAsync(shiftId, "delivery", "cash");
        Assert.Equal((11_000L, 11_000L), (d1.TotalPiastres, d2.TotalPiastres));
        await DeliverAsync(d1.OrderId, driverA);
        var shortDelivery = await DeliverAsync(d2.OrderId, driverA, collected: 9_000);
        await Ok<CollectionShortfallDto>(As(_managerId, $"/api/delivery/{shortDelivery}/collection-shortfall/classify",
            new ClassifyCollectionShortfallRequest($"cls-{Guid.NewGuid():N}", shortDelivery, "approved_discount", "خصم للعميل بموافقة المالك - إيصال 12", _managerId)));
        await Ok<SettleDriverCustodyResult>(_client.PostAsJsonAsync($"/api/delivery/drivers/{driverA}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driverA, 18_000, null, _dispatcherId, null, null)));

        // ---- Corrections: expense, safe drop, supplier invoice, treasury transfer ----
        var expense = await Ok<AddExpenseResult>(_client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", shiftId, "نظافة ومطهرات", 5_000, "منظفات", "tre-drawer", _cashierId, null, null)));
        await Ok<ReverseExpenseResult>(As(_managerId, $"/api/expenses/{expense.ExpenseId}/reverse",
            new ReverseExpenseRequest($"rev-{Guid.NewGuid():N}", expense.ExpenseId, "كُتب 50 وصُرف 5", _managerId, null, null, "not_paid", 4_500)));
        var drop = await Ok<SafeDropResult>(_client.PostAsJsonAsync("/api/treasury/safe-drop",
            new SafeDropRequest($"drop-{Guid.NewGuid():N}", shiftId, 20_000, "تأمين", _cashierId, null, null)));
        await Ok<CorrectTreasuryMovementResult>(As(_managerId, $"/api/treasury/movements/{drop.MovementId}/correct",
            new CorrectTreasuryMovementRequest($"corr-{Guid.NewGuid():N}", drop.MovementId, "كُتب 200 وأُودع 20", _managerId, true, 18_000)));
        var supplier = (await Ok<AddSupplierResult>(_client.PostAsJsonAsync("/api/suppliers",
            new AddSupplierRequest($"req-{Guid.NewGuid():N}", "مورد الدواجن", null, null, null, null, _managerId, null, null)))).SupplierId;
        var invoice = await Ok<PurchaseDto>(_client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest(
            $"req-{Guid.NewGuid():N}", supplier, "INV-1", new[] { new PurchaseLineRequest(_itemId, 10_000, 3_000) }, 0, "credit", _managerId, null, null, null)));
        await Ok<SupplierCreditResult>(As(_managerId, $"/api/purchases/{invoice.Id}/credits",
            new RecordSupplierCreditRequest($"cr-{Guid.NewGuid():N}", invoice.Id, "supplier_credit", 5_000, "خصم كمية", _managerId)));
        var transfer = await Ok<TransferResult>(As(_managerId, "/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-bank", 100_000, "إيداع بنكي", _managerId, null, null)));
        await Ok<CorrectTreasuryMovementResult>(As(_managerId, $"/api/treasury/movements/{transfer.MovementId}/correct",
            new CorrectTreasuryMovementRequest($"corr-{Guid.NewGuid():N}", transfer.MovementId, "لم يُودَع", _managerId, true)));

        // ---- Shift close: expected cash, counted, handed over, confirmed by the receiver ----
        var prepared = await Ok<PrepareShiftCloseResult>(_client.PostAsJsonAsync("/api/shifts/prepare-close", new PrepareShiftCloseRequest(shiftId, 81_500)));
        Assert.Equal((81_500L, 0L), (prepared.ExpectedCashPiastres, prepared.DifferencePiastres));
        var close = await Ok<CloseShiftResult>(_client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", shiftId, _cashierId, 81_500, null, null, null, 0, _managerId)));
        Assert.Equal(("pending", 81_500L, 0L), (close.HandoverReceiptStatus, close.HandedOverPiastres, close.DifferencePiastres));
        var receipt = await Ok<ShiftHandoverReceiptDto>(As(_managerId, $"/api/shifts/{shiftId}/handover/acknowledge",
            new AcknowledgeShiftHandoverRequest($"ack-{Guid.NewGuid():N}", shiftId, _managerId, 81_500, null)));
        Assert.Equal("acknowledged", receipt.Status);

        // ---- The owner's report over the overnight span (orders are dated today, the expense by yesterday's business day) ----
        var report = (await _client.GetFromJsonAsync<OwnerFinancialReportDto>(
            $"/api/reports/owner-financial?actorUserId={_managerId}&from={Yesterday:yyyy-MM-dd}&to={Today:yyyy-MM-dd}"))!;
        var b = report.SalesBridge;
        Assert.Equal((62_000L, 4_000L, 58_000L), (b.GrossSalesPiastres, b.RefundsMadePiastres, b.NetSalesPiastres));
        Assert.Equal((40_000L, 20_000L, 0L, 0L, 0L), (b.ReceivedThroughTreasuryPiastres, b.CodSettledPiastres, b.CodHeldByDriversPiastres, b.CodNotYetCollectedPiastres, b.PendingElectronicPiastres));
        Assert.Equal((0L, 0L, 2_000L, 0L), (b.ShortfallUnresolvedPiastres, b.ShortfallReceivablePiastres, b.ShortfallRealisedPiastres, b.ResidualPiastres));

        var flow = report.MoneyFlow;
        Assert.Equal((58_000L, 38_000L, 20_000L), (flow.CustomerReceiptsPiastres, flow.CustomerReceiptsCashPiastres, flow.CustomerReceiptsElectronicPiastres));
        Assert.Equal((0L, 0), (flow.ResidualPiastres, flow.UnclassifiedMovementCount));
        Assert.Equal(1_053_500, flow.ClosingBalancePiastres);
        Assert.Equal(4_500, flow.Lines.Single(l => l.Category == "ExpenseCorrection").InflowPiastres);
        Assert.Equal(118_000, flow.Lines.Single(l => l.Category == "EntryCorrection").MovedInternallyPiastres);
        Assert.Equal(4_000, flow.Lines.Single(l => l.Category == "CustomerRefund").OutflowPiastres);

        Assert.Equal(58_000, Kpi(report, "net_sales").AmountPiastres);
        Assert.Equal(1_033_500, Kpi(report, "balance_cash_total").AmountPiastres);
        Assert.Equal(0, Kpi(report, "balance_drawer").AmountPiastres);
        Assert.Equal(20_000, Kpi(report, "balance_electronic_total").AmountPiastres);
        Assert.Equal(0, Kpi(report, "cash_held_by_drivers").AmountPiastres);
        Assert.Equal(2_000, Kpi(report, "driver_compensation").AmountPiastres);
        Assert.Equal(500, Kpi(report, "expenses_net").AmountPiastres);
        Assert.Equal(25_000, Kpi(report, "supplier_outstanding").AmountPiastres);
        Assert.Equal(0, Kpi(report, "pending_electronic").AmountPiastres);
        Assert.Equal(0, Kpi(report, "handover_pending").AmountPiastres);
        Assert.Equal(0, Kpi(report, "shortfall_unresolved").AmountPiastres);
        var operating = Kpi(report, "operating_result_estimate");
        Assert.Equal((35_500L, "estimate"), (operating.AmountPiastres!.Value, operating.Status));

        // ---- Business day close: nothing blocks, and the frozen snapshot carries the same day ----
        foreach (var item in new[] { "close-shifts", "close-drawer", "close-delivery" })
            await Accept(_client.PostAsJsonAsync($"/api/business-day/{_dayId}/checklist/{item}", new CompleteChecklistItemRequest(_dayId, item, true, _managerId)));
        var closeResponse = await _client.PostAsJsonAsync($"/api/business-day/{_dayId}/close",
            new CloseBusinessDayRequest($"close-{Guid.NewGuid():N}", _dayId, _managerId, null, null));
        Assert.True(closeResponse.IsSuccessStatusCode, await closeResponse.Content.ReadAsStringAsync());
        var day = await _factory.QueryAsync(db => db.BusinessDays.AsNoTracking().SingleAsync(d => d.Id == _dayId));
        Assert.Equal("Closed", day.Status);
        var snapshot = await _factory.QueryAsync(db => db.BusinessDayCloseSnapshots.AsNoTracking().SingleAsync(s => s.BusinessDayId == _dayId && s.Authoritative));
        var reconciliation = System.Text.Json.JsonSerializer.Deserialize<BusinessDayReconciliationDto>(snapshot.ReconciliationJson)!;
        Assert.Equal((58_000L, 500L), (reconciliation.Sales.NetSalesPiastres, reconciliation.ExpensesTotalPiastres));
        // The snapshot's treasury is the DAY's net movement per account (by BusinessDayId), not a balance: the capital was
        // put in outside the day. Main: −50,000 float + 20,000 drop − 18,000 correction − 100,000 + 100,000 transfer and its
        // correction + 81,500 handover = 33,500; the drawer nets to 0, as every closed day's must.
        Assert.Equal((0L, 33_500L, 10_000L, 10_000L), (reconciliation.Treasury.DrawerPiastres, reconciliation.Treasury.MainPiastres,
            reconciliation.Treasury.BankPiastres, reconciliation.Treasury.InstaPayPiastres));
    }
}
