namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// P1/P5: the owner's financial report reconciles sales with money received, and money received with every posted
/// movement, to the piastre - and agrees with the dashboard and the cash-flow report it sits beside.
///
/// One trading day, in piastres. Main is funded with 10,000,000 (capital, not a sale). Eight orders of 10,000:
///   1 cash, 2 card, 3 InstaPay never confirmed, 4 InstaPay confirmed,
///   5 COD delivered and settled by driver A, 6 COD delivered and still with driver B,
///   7 COD not dispatched yet, 8 COD delivered by driver B with only 8,000 collected (approved, not classified).
/// Plus: the shift opens on a 50,000 float (main -> drawer), 30,000 is transferred main -> bank, and a 5,000 drawer
/// expense typed by mistake is reversed as a posting error.
///
/// Bridge: 80,000 gross = 30,000 through the treasury (1, 2, 4) + 10,000 settled (5) + 18,000 held by drivers (6, and
/// 8,000 of 8) + 10,000 not collected (7) + 10,000 pending (3) + 2,000 unresolved shortfall (8); residual 0.
/// Customer receipts: 30,000 sales + 10,000 driver remittance = 40,000 (20,000 cash, 20,000 electronic). The funding,
/// the float, the transfer and the correction are none of them customer money.
/// </summary>
public class OwnerFinancialReportTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _dispatcherId = default!;
    private string _managerId = default!;
    private string _shiftId = default!;
    private string _productId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer");
        _dispatcherId = await _factory.SeedUserAsync("موظف التوصيل", "dispatcher", "delivery_manage");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync(10_000_000);
        await _factory.SeedDeliveryServiceAsync("svc-1", 0);
        _productId = await _factory.SeedProductAsync(10_000);   // no recipe: food cost unknown
        _shiftId = (await (await Ok(_client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50_000)))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private static async Task<HttpResponseMessage> Ok(Task<HttpResponseMessage> call)
    {
        var r = await call;
        Assert.True(r.IsSuccessStatusCode, $"{r.RequestMessage?.RequestUri?.AbsolutePath} -> {(int)r.StatusCode} {await r.Content.ReadAsStringAsync()}");
        return r;
    }

    private async Task<string> SellAsync(string orderType, string payment)
    {
        var delivery = orderType == "delivery";
        var result = (await (await Ok(_client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: orderType, PaymentMethod: payment,
            Items: new[] { new CheckoutItemRequest(_productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: delivery ? 0 : 10_000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: delivery ? "عميل" : null, CustomerPhone: delivery ? "01000000000" : null,
            DeliveryAddress: null, DeliveryArea: delivery ? "الحي" : null, BusinessDayId: null,
            DeliveryStreet: delivery ? "شارع" : null, DeliveryServiceId: delivery ? "svc-1" : null, DeliveryServiceName: delivery ? "توصيل سريع" : null))))
            .Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        return result.OrderId;
    }

    private Task<string> DeliveryOf(string orderId) =>
        _factory.QueryAsync(db => db.Deliveries.Where(d => d.OrderId == orderId).Select(d => d.Id).FirstAsync());

    private async Task<string> DriverAsync(string name) =>
        (await (await Ok(_client.PostAsJsonAsync("/api/delivery/drivers",
            new CreateDriverRequest($"req-{Guid.NewGuid():N}", name, _dispatcherId)))).Content.ReadFromJsonAsync<DriverDto>())!.Id;

    private async Task DeliverAsync(string orderId, string driverId, long? collected = null)
    {
        var deliveryId = await DeliveryOf(orderId);
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/ready", new MarkDeliveryReadyRequest(deliveryId, _dispatcherId, null, null)));
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/assign",
            new AssignDeliveryRequest($"req-{Guid.NewGuid():N}", deliveryId, driverId, _dispatcherId, null, null)));
        string? approvalId = null, approverId = null;
        if (collected is not null)
        {
            var approval = (await (await Ok(_factory.PostAsSessionOfAsync(_client, _dispatcherId, "/api/approvals/issue",
                new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "delivery_settlement_approve", "دفع أقل"))))
                .Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
            (approvalId, approverId) = (approval.ApprovalId, approval.ApproverUserId);
        }
        await Ok(_client.PostAsJsonAsync($"/api/delivery/{deliveryId}/result", new RecordDeliveryResultRequest(
            $"req-{Guid.NewGuid():N}", deliveryId, "delivered", collected, collected is null ? null : "العميل دفع أقل", null, _dispatcherId,
            approvalId, approverId, null)));
    }

    private async Task<OwnerFinancialReportDto> ReportAsync() =>
        (await _client.GetFromJsonAsync<OwnerFinancialReportDto>(
            $"/api/reports/owner-financial?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;

    private static FinancialKpiDto Kpi(OwnerFinancialReportDto r, string key) => r.Sections.SelectMany(s => s.Kpis).Single(k => k.Key == key);

    private async Task TradeTheDayAsync()
    {
        await SellAsync("takeaway", "cash");
        await SellAsync("takeaway", "card");
        await SellAsync("takeaway", "instapay");
        var confirmed = await SellAsync("takeaway", "instapay");
        await Ok(_client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(confirmed, _cashierId)));

        var driverA = await DriverAsync("سائق أ");
        var driverB = await DriverAsync("سائق ب");
        var settled = await SellAsync("delivery", "cash");
        var held = await SellAsync("delivery", "cash");
        await SellAsync("delivery", "cash");                       // never dispatched
        var shortPaid = await SellAsync("delivery", "cash");
        await DeliverAsync(settled, driverA);
        await DeliverAsync(held, driverB);
        await DeliverAsync(shortPaid, driverB, collected: 8_000);
        await Ok(_client.PostAsJsonAsync($"/api/delivery/drivers/{driverA}/settle",
            new SettleDriverCustodyRequest($"req-{Guid.NewGuid():N}", driverA, 10_000, null, _dispatcherId, null, null)));

        await Ok(_factory.PostAsSessionOfAsync(_client, _managerId, "/api/treasury/transfer",
            new TransferRequest($"tr-{Guid.NewGuid():N}", "tre-main", "tre-bank", 30_000, "إيداع بنكي", _managerId, null, null)));
        var expense = (await (await Ok(_client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", _shiftId, "نظافة ومطهرات", 5_000, "منظفات", "tre-drawer", _cashierId, null, null))))
            .Content.ReadFromJsonAsync<AddExpenseResult>())!.ExpenseId;
        await Ok(_factory.PostAsSessionOfAsync(_client, _managerId, $"/api/expenses/{expense}/reverse",
            new ReverseExpenseRequest($"rev-{Guid.NewGuid():N}", expense, "مكرر", _managerId, null, null, "not_paid")));
    }

    [Fact]
    public async Task Sales_are_followed_to_where_their_money_is_and_nothing_is_left_unexplained()
    {
        await TradeTheDayAsync();
        var b = (await ReportAsync()).SalesBridge;

        Assert.Equal(80_000, b.GrossSalesPiastres);
        Assert.Equal(30_000, b.ReceivedThroughTreasuryPiastres);
        Assert.Equal(10_000, b.CodSettledPiastres);
        Assert.Equal(18_000, b.CodHeldByDriversPiastres);
        Assert.Equal(10_000, b.CodNotYetCollectedPiastres);
        Assert.Equal(10_000, b.PendingElectronicPiastres);
        Assert.Equal(2_000, b.ShortfallUnresolvedPiastres);
        Assert.Equal(0, b.ShortfallRealisedPiastres);
        Assert.Equal(0, b.ResidualPiastres);
        Assert.Equal(80_000, b.NetSalesPiastres);
    }

    [Fact]
    public async Task Only_customer_money_counts_as_collected_and_every_movement_reconciles_to_the_balances()
    {
        await TradeTheDayAsync();
        var report = await ReportAsync();
        var flow = report.MoneyFlow;

        Assert.Equal(40_000, flow.CustomerReceiptsPiastres);
        Assert.Equal(20_000, flow.CustomerReceiptsCashPiastres);
        Assert.Equal(20_000, flow.CustomerReceiptsElectronicPiastres);
        Assert.Equal(80_000, flow.InternalTransfersPiastres);   // 50,000 float + 30,000 to the bank
        Assert.Equal(0, flow.ResidualPiastres);
        Assert.Equal(0, flow.UnclassifiedMovementCount);
        Assert.Equal(10_040_000, flow.ClosingBalancePiastres);
        Assert.Equal(10_000_000, flow.Lines.Single(l => l.Category == "ExternalFunding").InflowPiastres);
        Assert.Equal(5_000, flow.Lines.Single(l => l.Category == "ExpenseCorrection").InflowPiastres);
        Assert.False(flow.Lines.Single(l => l.Category == "ExpenseCorrection").CountsAsCustomerReceipt);
        Assert.Equal(40_000, Kpi(report, "customer_receipts").AmountPiastres);

        // The same money, account by account, in the cash-flow report.
        var cashFlow = (await _client.GetFromJsonAsync<CashFlowReportDto>(
            $"/api/reports/cash-flow?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;
        Assert.Equal(flow.ClosingBalancePiastres, cashFlow.Accounts.Sum(a => a.ClosingPiastres));
        var balances = report.Sections.Single(s => s.Key == "balances").Kpis;
        Assert.Equal(cashFlow.Accounts.Sum(a => a.ClosingPiastres),
            balances.Where(k => k.Key is "balance_cash_total" or "balance_electronic_total").Sum(k => k.AmountPiastres!.Value));
    }

    [Fact]
    public async Task It_agrees_with_the_dashboard_and_never_shows_unknown_profit_as_zero()
    {
        await TradeTheDayAsync();
        var report = await ReportAsync();
        var dashboard = (await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;

        Assert.Equal(dashboard.Cash.NetSalesPiastres, Kpi(report, "net_sales").AmountPiastres);
        Assert.Equal(dashboard.Obligations.SupplierOutstandingPiastres, Kpi(report, "supplier_outstanding").AmountPiastres);
        Assert.Equal(18_000, Kpi(report, "cash_held_by_drivers").AmountPiastres);
        Assert.Equal(10_000, Kpi(report, "pending_electronic").AmountPiastres);
        Assert.Equal(0, Kpi(report, "expenses_net").AmountPiastres);
        Assert.Equal(5_000, Kpi(report, "expense_corrections").AmountPiastres);

        // The product has no recipe: its food cost is unknown, so profit is not computed - and not reported as 0.
        var operating = Kpi(report, "operating_result_estimate");
        Assert.Null(operating.AmountPiastres);
        Assert.Equal("unavailable", operating.Status);
        Assert.Equal("unavailable", Kpi(report, "gross_profit_estimate").Status);

        // The unresolved shortfall is reported as unresolved, and counted among the open financial exceptions.
        var unresolved = Kpi(report, "shortfall_unresolved");
        Assert.Equal((2_000L, 1, "partial"), (unresolved.AmountPiastres!.Value, unresolved.Count!.Value, unresolved.Status));
        Assert.True(Kpi(report, "open_financial_exceptions").Count >= 1);
        Assert.All(report.Sections.SelectMany(s => s.Kpis), k => Assert.False(string.IsNullOrWhiteSpace(k.Definition)));
        Assert.All(report.Sections.SelectMany(s => s.Kpis), k => Assert.False(string.IsNullOrWhiteSpace(k.Sources)));
    }
}
