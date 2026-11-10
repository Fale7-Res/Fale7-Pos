namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Two things an owner needs every day that no report answered.
///
/// Where the money went, per account: the Daily Statement listed the day's treasury movements and
/// the all-time balances, but nothing said what each account held when the day started, what came
/// in and went out by kind, and what it held at the end - the figures a physical count is checked
/// against. And which electronic payments are still only claims: the dashboard counted them, but
/// not how much money they are or how long they have been waiting.
/// </summary>
public class CashFlowAndPendingTests : IAsyncLifetime
{
    private const long Funding = 100_000_00;

    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close", "expenses_add_drawer");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync(Funding);
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Each_account_shows_its_opening_its_ins_and_outs_by_kind_and_its_closing()
    {
        await SeedMovementYesterdayAsync("capital_injection", destination: "tre-bank", 20000);
        var shiftId = await OpenShiftAsync(50000);                    // main -> drawer 500
        await CheckoutAsync(shiftId, 10000, "cash");                   // -> drawer 100
        await ExpenseAsync(shiftId, 3000);                             // drawer -> out 30
        await CloseAsync(shiftId, 50000 + 10000 - 3000);               // drawer -> main 570

        var flow = await CashFlowAsync(BusinessClock.Today());

        var drawer = flow.Accounts.Single(a => a.AccountId == "tre-drawer");
        Assert.Equal(0, drawer.OpeningPiastres);
        Assert.Equal(60000, drawer.InflowPiastres);
        Assert.Equal(60000, drawer.OutflowPiastres);
        Assert.Equal(0, drawer.ClosingPiastres);
        Assert.Equal(3000, drawer.Lines.Single(l => l.MovementType == "expense_payment").OutflowPiastres);

        var main = flow.Accounts.Single(a => a.AccountId == "tre-main");
        Assert.Equal(Funding + 57000 - 50000, main.ClosingPiastres);
        Assert.Equal(main.OpeningPiastres + main.InflowPiastres - main.OutflowPiastres, main.ClosingPiastres);

        // A movement from before the day is opening balance, not today's flow.
        var bank = flow.Accounts.Single(a => a.AccountId == "tre-bank");
        Assert.Equal(20000, bank.OpeningPiastres);
        Assert.Equal(0, bank.InflowPiastres);
        Assert.Equal(20000, bank.ClosingPiastres);
    }

    [Fact]
    public async Task The_daily_statement_carries_the_same_cash_flow()
    {
        var shiftId = await OpenShiftAsync(50000);
        await CheckoutAsync(shiftId, 10000, "cash");

        var statement = await _client.GetFromJsonAsync<DailyStatementDto>(
            $"/api/reports/daily-statement?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}");
        var flow = await CashFlowAsync(BusinessClock.Today());

        Assert.NotNull(statement!.CashFlow);
        Assert.Equal(
            flow.Accounts.Select(a => (a.AccountId, a.ClosingPiastres)),
            statement.CashFlow!.Accounts.Select(a => (a.AccountId, a.ClosingPiastres)));
    }

    [Fact]
    public async Task Unconfirmed_electronic_payments_are_listed_with_their_amount_and_age()
    {
        var shiftId = await OpenShiftAsync(0);
        var pending = await CheckoutAsync(shiftId, 12000, "instapay");
        await CheckoutAsync(shiftId, 5000, "cash");

        var report = await _client.GetFromJsonAsync<PendingElectronicReportDto>(
            $"/api/reports/pending-electronic?actorUserId={_managerId}");

        Assert.Equal(1, report!.Count);
        Assert.Equal(12000, report.TotalPiastres);
        var payment = Assert.Single(report.Payments);
        Assert.Equal(pending.OrderId, payment.OrderId);
        Assert.Equal("instapay", payment.Method);
        Assert.True(payment.AgeMinutes >= 0);

        var dashboard = await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}");
        Assert.Equal(12000, dashboard!.Attention.PendingElectronicPiastres);
    }

    // ---------- Helpers ----------

    private async Task<CashFlowReportDto> CashFlowAsync(DateOnly date) =>
        (await _client.GetFromJsonAsync<CashFlowReportDto>($"/api/reports/cash-flow?actorUserId={_managerId}&businessDate={date:yyyy-MM-dd}"))!;

    private async Task SeedMovementYesterdayAsync(string type, string destination, long amount)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var (start, _) = BusinessClock.UtcRangeForBusinessDate(BusinessClock.Today().AddDays(-1));
        db.TreasuryMovements.Add(new TreasuryMovementEntity
        {
            Id = $"tm-{Guid.NewGuid():N}", IdempotencyKey = $"seed-{Guid.NewGuid():N}", OperationId = "op-seed",
            Type = type, DestinationAccountId = destination, AmountPiastres = amount,
            ReferenceType = "external_funding", ReferenceId = "seed", Reason = "seed",
            CreatedBy = "seed", CreatedAtUtc = start.AddHours(12), Status = "Posted",
        });
        await db.SaveChangesAsync();
    }

    private async Task<string> OpenShiftAsync(long openingCash)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, openingCash));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task<CheckoutOrderResult> CheckoutAsync(string shiftId, long totalPiastres, string paymentMethod)
    {
        var productId = await _factory.SeedProductAsync(totalPiastres);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: paymentMethod,
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: totalPiastres, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    private async Task ExpenseAsync(string shiftId, long amount)
    {
        var response = await _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest(
            $"exp-{Guid.NewGuid():N}", shiftId, "مستلزمات تشغيل", amount, "أكياس", "tre-drawer", _cashierId, null, null));
        response.EnsureSuccessStatusCode();
    }

    private async Task CloseAsync(string shiftId, long counted)
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close",
            new CloseShiftRequest($"creq-{Guid.NewGuid():N}", shiftId, _cashierId, counted, null, null, null));
        response.EnsureSuccessStatusCode();
    }
}
