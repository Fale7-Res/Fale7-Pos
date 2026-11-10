namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// The owner dashboard's profitability and cash lines are built from the right inputs.
///
/// Three inputs were wrong. Wage cost added up every earning posted in the period but ignored the
/// deductions posted against them, so a late-penalty or a docked day still counted as labour the
/// restaurant paid for. Profit was computed on sales INCLUDING VAT, which the restaurant collects for
/// the tax authority and does not keep - every taxed sale inflated gross profit by its tax. And the
/// "settled from drivers" line summed driver settlement movements without asking whether they were
/// still posted, unlike every other treasury figure.
/// </summary>
public class ProfitabilityInputsTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:Automatic:Enabled"] = "false",
        });
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    [Fact]
    public async Task Wage_cost_is_the_earnings_posted_less_the_deductions_posted()
    {
        await SeedLedgerAsync("DailyWage", credit: 20000, debit: 0);
        await SeedLedgerAsync("Deduction", credit: 0, debit: 5000);

        var dashboard = await DashboardAsync();

        Assert.Equal(15000, dashboard.Profitability.WagesPiastres);
    }

    [Fact]
    public async Task Profit_is_computed_on_sales_excluding_the_vat_collected_on_them()
    {
        await SeedTaxRuleAsync(rateBasisPoints: 1400);   // 14 %, added on top of the price
        var shiftId = await OpenShiftAsync();
        await CheckoutCashAsync(shiftId, priceBeforeTax: 10000, amountPaid: 11400);

        var dashboard = await DashboardAsync();

        // What the customer paid, VAT included, is still what net sales reports.
        Assert.Equal(11400, dashboard.Cash.NetSalesPiastres);
        // What the restaurant keeps is what profit is measured on.
        Assert.Equal(10000, dashboard.Profitability.NetSalesPiastres);
    }

    [Fact]
    public async Task Only_posted_driver_settlements_count_as_settled_from_drivers()
    {
        await SeedMovementAsync("driver_settlement", 7000, status: "Reversed");
        await SeedMovementAsync("driver_settlement", 3000, status: "Posted");

        var dashboard = await DashboardAsync();

        Assert.Equal(3000, dashboard.Cash.SettledFromDriversPiastres);
    }

    // ---------- Helpers ----------

    private async Task<OwnerDashboardDto> DashboardAsync() =>
        (await _client.GetFromJsonAsync<OwnerDashboardDto>(
            $"/api/reports/owner-dashboard?actorUserId={_managerId}&businessDate={BusinessClock.Today():yyyy-MM-dd}"))!;

    private async Task SeedLedgerAsync(string type, long credit, long debit)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.EmployeeLedgerEntries.Add(new EmployeeLedgerEntryEntity
        {
            Id = $"led-{Guid.NewGuid():N}", EmployeeId = "emp-test", Type = type, Description = type,
            CreditPiastres = credit, DebitPiastres = debit, RecordedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedTaxRuleAsync(int rateBasisPoints)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.TaxRules.Add(new TaxRuleEntity
        {
            Id = $"tax-{Guid.NewGuid():N}", Name = "ضريبة القيمة المضافة", RateBasisPoints = rateBasisPoints,
            Mode = "Exclusive", Enabled = true,
        });
        await db.SaveChangesAsync();
    }

    private async Task SeedMovementAsync(string type, long amount, string status)
    {
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.TreasuryMovements.Add(new TreasuryMovementEntity
        {
            Id = $"tm-{Guid.NewGuid():N}", IdempotencyKey = $"seed-{Guid.NewGuid():N}", OperationId = "op-seed",
            Type = type, DestinationAccountId = "tre-main", AmountPiastres = amount,
            ReferenceType = "delivery_trip", ReferenceId = "driver-seed", Reason = "seed",
            CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow, Status = status,
        });
        await db.SaveChangesAsync();
    }

    private async Task<string> OpenShiftAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 0));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private async Task CheckoutCashAsync(string shiftId, long priceBeforeTax, long amountPaid)
    {
        var productId = await _factory.SeedProductAsync(priceBeforeTax);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: amountPaid, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null));
        response.EnsureSuccessStatusCode();
        var result = (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
        Assert.Equal(amountPaid, result.TotalPiastres);
    }
}
