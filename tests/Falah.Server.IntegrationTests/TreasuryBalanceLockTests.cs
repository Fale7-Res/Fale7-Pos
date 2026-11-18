namespace Falah.Server.IntegrationTests;

using System.Net.Http.Json;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

/// <summary>
/// PERF-0001 (audit 2026-11-18). Every money-out command checks its account's balance before
/// spending. It used to read every posted movement the account ever had WITH (UPDLOCK, HOLDLOCK);
/// TreasuryMovements has no index on the account columns, so that range lock covered the whole table
/// and every till's checkout - whose own movement is an insert into it - waited behind a refund, a
/// safe drop or an expense. The check now serializes spenders of one account with an application
/// lock and sums the balance without holding row locks.
/// </summary>
public class TreasuryBalanceLockTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير القفل", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 50000)))
            .Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CheckoutOrderRequest> CashSaleAsync()
    {
        var productId = await _factory.SeedProductAsync(3000);
        return new CheckoutOrderRequest(
            CheckoutRequestId: $"req-{Guid.NewGuid():N}", CashierId: _cashierId, ShiftId: _shiftId,
            OrderType: "takeaway", PaymentMethod: "cash",
            Items: new[] { new CheckoutItemRequest(productId, null, 1, null, null) },
            DiscountPiastres: 0, DeliveryFeePiastres: 0, CompAmountPiastres: 0,
            CompApprovalId: null, CompApproverUserId: null, CompReason: null,
            AmountPaidPiastres: 3000, SplitPayments: null, WalletProvider: null,
            TableNumber: null, CustomerName: null, CustomerPhone: null, DeliveryAddress: null, DeliveryArea: null, BusinessDayId: null);
    }

    [Fact]
    public async Task A_balance_check_in_progress_on_the_drawer_does_not_hold_up_a_cash_sale()
    {
        var sale = await CashSaleAsync();
        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        Assert.StartsWith("FalahTest_", db.Database.GetDbConnection().Database);
        await using var spending = await db.Database.BeginTransactionAsync();
        var balance = await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LockAndReadBalanceAsync(db, "tre-drawer", default);
        Assert.Equal(50000, balance.Piastres);

        var checkout = _client.PostAsJsonAsync("/api/orders/checkout", sale);
        var finished = await Task.WhenAny(checkout, Task.Delay(TimeSpan.FromSeconds(6)));

        Assert.Same(checkout, finished);
        (await checkout).EnsureSuccessStatusCode();
        await spending.RollbackAsync();
    }

    [Fact]
    public async Task Two_spenders_of_one_account_take_turns_and_the_second_sees_the_first()
    {
        using var firstScope = _factory.Services.CreateScope();
        var first = firstScope.ServiceProvider.GetRequiredService<FalahDbContext>();
        await using var firstTx = await first.Database.BeginTransactionAsync();
        Assert.Equal(50000, (await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LockAndReadBalanceAsync(first, "tre-drawer", default)).Piastres);

        using var secondScope = _factory.Services.CreateScope();
        var second = secondScope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var secondRead = Task.Run(async () =>
        {
            await using var tx = await second.Database.BeginTransactionAsync();
            var seen = await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LockAndReadBalanceAsync(second, "tre-drawer", default);
            await tx.RollbackAsync();
            return seen.Piastres;
        });
        await Task.Delay(500);
        Assert.False(secondRead.IsCompleted); // waits for the first spender

        first.TreasuryMovements.Add(new Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity
        {
            Id = $"tm-{Guid.NewGuid():N}", IdempotencyKey = $"lock-test-{Guid.NewGuid():N}", OperationId = $"op-{Guid.NewGuid():N}",
            Type = "safe_drop", SourceAccountId = "tre-drawer", DestinationAccountId = "tre-main", AmountPiastres = 20000,
            ReferenceType = "test", ReferenceId = "lock-test", ShiftId = _shiftId, Reason = "اختبار", CreatedBy = "اختبار",
            CreatedAtUtc = DateTimeOffset.UtcNow, Status = "Posted",
        });
        await first.SaveChangesAsync();
        await firstTx.CommitAsync();

        Assert.Equal(30000, await secondRead);
    }
}
