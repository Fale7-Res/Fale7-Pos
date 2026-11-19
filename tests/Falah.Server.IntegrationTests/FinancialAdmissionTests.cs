namespace Falah.Server.IntegrationTests;

using System.Data.Common;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Expenses;
using Falah.Application.Orders;
using Falah.Application.Purchasing;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

public class FinancialAdmissionTests : IAsyncLifetime
{
    private sealed class PauseBeforeTransaction : DbTransactionInterceptor
    {
        public bool Armed;
        private int _used;
        public TaskCompletionSource Reached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<InterceptionResult<DbTransaction>> TransactionStartingAsync(DbConnection connection,
            TransactionStartingEventData eventData, InterceptionResult<DbTransaction> result, CancellationToken ct = default)
        {
            if (Armed && Interlocked.CompareExchange(ref _used, 1, 0) == 0)
            {
                Reached.TrySetResult();
                await Release.Task.WaitAsync(ct);
            }
            return result;
        }
    }
    private FalahApiFactory _factory = default!;
    private WebApplicationFactory<Program> _host = default!;
    private HttpClient _client = default!;
    private readonly PauseBeforeTransaction _pause = new();
    private string _actor = default!, _day = default!;
    public async Task InitializeAsync()
    {
        _factory = new();
        _actor = await _factory.SeedUserAsync("المالك", "owner", "all");
        _day = await _factory.SeedOpenBusinessDayWithFundedMainAsync(100000);
        _host = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<DbContextOptions<FalahDbContext>>(); services.RemoveAll<FalahDbContext>();
            services.AddDbContext<FalahDbContext>((sp, options) => options
                .UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Falah"))
                .AddInterceptors(_pause));
        }));
        _client = _host.CreateClient();
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(_actor));
    }
    public Task DisposeAsync() { _pause.Release.TrySetResult(); _client.Dispose(); _host.Dispose(); _factory.Dispose(); return Task.CompletedTask; }
    private async Task<string> ShiftAsync() => (await (await _client.PostAsJsonAsync("/api/shifts/open",
        new OpenShiftRequest(Guid.NewGuid().ToString("N"), "اختبار", _actor, 10000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    private Task DrainMainAsync() => _factory.QueryAsync(async db =>
    {
        db.TreasuryMovements.Add(new TreasuryMovementEntity {
            Id = "drain-main", IdempotencyKey = "drain-main", OperationId = "drain-main", Type = "owner_withdrawal",
            SourceAccountId = "tre-main", AmountPiastres = 100000, ReferenceType = "test", ReferenceId = "test",
            Reason = "concurrent withdrawal", CreatedBy = "test", CreatedAtUtc = DateTimeOffset.UtcNow, Status = "Posted" });
        await db.SaveChangesAsync(); return true;
    });
    private async Task<HttpResponseMessage> RaceAsync(Func<Task<HttpResponseMessage>> send, Func<Task> otherCommit)
    {
        _pause.Armed = true;
        var response = send();
        try { await _pause.Reached.Task.WaitAsync(TimeSpan.FromSeconds(20)); await otherCommit(); }
        finally { _pause.Release.TrySetResult(); }
        return await response;
    }
    [Theory]
    [InlineData("sale")]
    [InlineData("expense")]
    [InlineData("safe-drop")]
    public async Task A_command_admitted_before_shift_close_rechecks_the_committed_shift(string kind)
    {
        var shift = await ShiftAsync();
        var product = await _factory.SeedProductAsync(1000);
        Func<Task<HttpResponseMessage>> send = kind switch {
            "sale" => () => _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
                "late-sale", _actor, shift, "takeaway", "cash", new[] { new CheckoutItemRequest(product, null, 1, null, null) },
                0, 0, 0, null, null, null, 1000, null, null, null, null, null, null, null, null)),
            "expense" => () => _client.PostAsJsonAsync("/api/expenses", new AddExpenseRequest("late-expense", shift,
                "مستلزمات تشغيل", 1000, "اختبار", "tre-drawer", _actor, null, null)),
            _ => () => _client.PostAsJsonAsync("/api/treasury/safe-drop", new SafeDropRequest("late-drop", shift, 1000, "اختبار", _actor, null, null))
        };
        var before = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());
        var result = await RaceAsync(send, () => _factory.QueryAsync(db => db.Shifts.Where(s => s.Id == shift)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Closed"))));
        Assert.Equal(kind == "sale" ? HttpStatusCode.Conflict : HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.Equal(before, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync()));
        Assert.False(await _factory.QueryAsync(db => db.Orders.AnyAsync(o => o.CheckoutRequestId == "late-sale")));
    }
    [Fact]
    public async Task A_deposit_cannot_post_after_the_business_day_it_read_has_closed()
    {
        var result = await RaceAsync(() => _client.PostAsJsonAsync("/api/treasury/deposit",
            new DepositRequest("late-deposit", "capital_injection", 1000, "tre-main", "اختبار", null, _actor, null, null)),
            () => _factory.QueryAsync(db => db.BusinessDays.Where(d => d.Id == _day).ExecuteUpdateAsync(d => d.SetProperty(x => x.Status, "Closed"))));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.TreasuryMovements.AnyAsync(m => m.IdempotencyKey == "capital-injection-late-deposit")));
    }
    [Fact]
    public async Task Opening_float_rechecks_funds_inside_its_transaction()
    {
        var result = await RaceAsync(() => _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest("late-open", "اختبار", _actor, 100000)), DrainMainAsync);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.Shifts.AnyAsync(s => s.OpenRequestId == "late-open")));
    }
    private async Task<(string Supplier, string Item)> PurchaseInputsAsync()
    {
        var item = (await _factory.SeedProductWithRecipeAsync(1000, 1000)).InventoryItemId;
        var result = await _client.PostAsJsonAsync("/api/suppliers", new AddSupplierRequest("supplier", "مورد", null, null, null, null, _actor, null, null));
        result.EnsureSuccessStatusCode();
        return ((await result.Content.ReadFromJsonAsync<AddSupplierResult>())!.SupplierId, item);
    }
    [Fact]
    public async Task A_purchase_receipt_rechecks_funds_inside_its_transaction()
    {
        var inputs = await PurchaseInputsAsync();
        var result = await RaceAsync(() => _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest("late-receipt", inputs.Supplier, null,
            new[] { new PurchaseLineRequest(inputs.Item, 1000, 100000) }, 100000, "cash", _actor, null, null, null)), DrainMainAsync);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.Purchases.AnyAsync(p => p.Id == "purchase-late-receipt")));
    }
    [Fact]
    public async Task A_supplier_payment_rechecks_the_remaining_debt_after_another_payment_commits()
    {
        var inputs = await PurchaseInputsAsync();
        var received = await _client.PostAsJsonAsync("/api/purchases", new ReceivePurchaseRequest("invoice", inputs.Supplier, null,
            new[] { new PurchaseLineRequest(inputs.Item, 1000, 1000) }, 0, "credit", _actor, null, null, null));
        received.EnsureSuccessStatusCode();
        var invoice = (await received.Content.ReadFromJsonAsync<PurchaseDto>())!;
        var request = new PayPurchaseRequest("first", invoice.Id, 600, "cash", _actor, null, null);
        var path = $"/api/purchases/{invoice.Id}/payments";
        var result = await RaceAsync(() => _client.PostAsJsonAsync(path, request), async () =>
        {
            using var other = _factory.CreateAuthenticatedClient();
            (await other.PostAsJsonAsync(path, request with { RequestId = "second" })).EnsureSuccessStatusCode();
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, result.StatusCode);
        Assert.Equal(600, await _factory.QueryAsync(db => db.PurchasePayments.Where(p => p.PurchaseId == invoice.Id).SumAsync(p => p.AmountPiastres)));
    }

    [Fact]
    public async Task Closing_a_day_waits_for_an_admitted_financial_transaction()
    {
        foreach (var item in new[] { "close-shifts", "close-drawer", "close-delivery" })
            (await _client.PostAsJsonAsync($"/api/business-day/{_day}/checklist/{item}",
                new Falah.Application.BusinessDay.CompleteChecklistItemRequest(_day, item, true, _actor))).EnsureSuccessStatusCode();
        Task<HttpResponseMessage>? close = null;
        await _factory.QueryAsync(async db =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync();
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_getapplock @Resource=N'FalahFinancialAdmission', @LockMode=N'Shared', @LockOwner=N'Transaction'");
            close = _client.PostAsJsonAsync($"/api/business-day/{_day}/close",
                new Falah.Application.BusinessDay.CloseBusinessDayRequest("queued-close", _day, _actor, null, null));
            try
            {
                await using var command = db.Database.GetDbConnection().CreateCommand();
                command.Transaction = Microsoft.EntityFrameworkCore.Storage.DbContextTransactionExtensions.GetDbTransaction(transaction);
                command.CommandText = "SELECT COUNT(*) FROM sys.dm_exec_requests WHERE database_id=DB_ID() AND wait_type LIKE 'LCK_M_%'";
                var deadline = DateTime.UtcNow.AddSeconds(15); var blocked = 0;
                while (DateTime.UtcNow < deadline && !close.IsCompleted && blocked == 0)
                {
                    blocked = Convert.ToInt32(await command.ExecuteScalarAsync());
                    if (blocked == 0) await Task.Delay(30);
                }
                Assert.True(blocked > 0, "Day close must wait until the financial admission transaction ends.");
            }
            finally { await transaction.RollbackAsync(); }
            return true;
        });
        (await close!).EnsureSuccessStatusCode();
    }


    [Fact]
    public async Task A_payroll_payment_cannot_revive_a_run_cancelled_before_its_transaction()
    {
        var employeeResponse = await _client.PostAsJsonAsync("/api/employees", new Falah.Application.Employees.CreateEmployeeRequest(
            "race-employee", "موظف", "race-code", "عمل", "01000000000", new DateOnly(2024, 1, 1), "Daily", 20000, 15000, null, false,
            "cashier", null, null, null, null, null, null, null, null, _actor));
        employeeResponse.EnsureSuccessStatusCode();
        var employee = (await employeeResponse.Content.ReadFromJsonAsync<Falah.Application.Employees.EmployeeDto>())!;
        await _factory.QueryAsync(async db => {
            db.PayrollRuns.Add(new PayrollRunEntity { Id = "pay-race", Period = "2026-11", Status = "Approved", CreatedBy = "test", CreatedAtUtc = DateTimeOffset.UtcNow });
            db.PayrollLines.Add(new PayrollLineEntity { Id = "pay-line", PayrollRunId = "pay-race", EmployeeId = employee.Id, BasePiastres = 1000, NetPayablePiastres = 1000, Status = "unpaid" });
            await db.SaveChangesAsync(); return true;
        });
        var response = await RaceAsync(() => _client.PostAsJsonAsync("/api/payroll-runs/pay-race/pay", new Falah.Application.Employees.PayPayrollRequest("pay-race", "tre-main", _actor, null, null)), async () => {
            using var other = _factory.CreateAuthenticatedClient();
            (await other.PostAsJsonAsync("/api/payroll-runs/pay-race/transition", new Falah.Application.Employees.TransitionPayrollRequest("pay-race", "Cancelled", _actor, null, null))).EnsureSuccessStatusCode();
        });
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.Equal("Cancelled", await _factory.QueryAsync(db => db.PayrollRuns.Where(r => r.Id == "pay-race").Select(r => r.Status).SingleAsync()));
        Assert.False(await _factory.QueryAsync(db => db.TreasuryMovements.AnyAsync(m => m.IdempotencyKey == "payroll-payment-pay-race")));
    }


    private async Task<CheckoutOrderResult> PendingElectronicAsync()
    {
        var shift = await ShiftAsync(); var product = await _factory.SeedProductAsync(1000);
        var response = await _client.PostAsJsonAsync("/api/orders/checkout", new CheckoutOrderRequest(
            "late-electronic", _actor, shift, "takeaway", "instapay", new[] { new CheckoutItemRequest(product, null, 1, null, null) },
            0, 0, 0, null, null, null, 1000, null, null, null, null, null, null, null, null));
        response.EnsureSuccessStatusCode(); return (await response.Content.ReadFromJsonAsync<CheckoutOrderResult>())!;
    }

    [Fact]
    public async Task Electronic_confirmation_rechecks_the_active_day_inside_its_transaction()
    {
        var order = await PendingElectronicAsync();
        var response = await RaceAsync(() => _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _actor)),
            () => _factory.QueryAsync(db => db.BusinessDays.Where(d => d.Id == _day).ExecuteUpdateAsync(s => s.SetProperty(d => d.Status, "Closed"))));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        Assert.False(await _factory.QueryAsync(db => db.TreasuryMovements.AnyAsync(m => m.ReferenceId == order.OrderId && m.Type == "sale_collection")));
    }

    [Fact]
    public async Task Late_electronic_collection_belongs_to_the_current_day_and_shift_while_the_order_keeps_its_sale_day()
    {
        var order = await PendingElectronicAsync();
        await _factory.QueryAsync(async db => {
            await db.Shifts.ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Closed"));
            await db.BusinessDays.Where(d => d.Id == _day).ExecuteUpdateAsync(s => s.SetProperty(x => x.Status, "Closed"));
            return true;
        });
        var currentDay = await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        var currentShift = await ShiftAsync();
        (await _client.PostAsJsonAsync("/api/orders/confirm-electronic-payment", new ConfirmElectronicPaymentRequest(order.OrderId, _actor))).EnsureSuccessStatusCode();
        var movement = await _factory.QueryAsync(db => db.TreasuryMovements.AsNoTracking().SingleAsync(m => m.ReferenceId == order.OrderId && m.Type == "sale_collection"));
        Assert.Equal(currentDay, movement.BusinessDayId); Assert.Equal(currentShift, movement.ShiftId);
        Assert.Equal(_day, await _factory.QueryAsync(db => db.Orders.Where(o => o.Id == order.OrderId).Select(o => o.BusinessDayId).SingleAsync()));
    }

}
