namespace Falah.Infrastructure.Treasury;

using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System.Data;

/// <summary>Loads just the posted movements that touch one account, for FinancialRules'
/// balance-derivation functions - never the whole table.</summary>
public static class TreasuryBalanceQuery
{
    /// <summary>Reads an account's movements for DISPLAY. Takes no lock, because a balance being
    /// shown on a screen has no decision hanging on it.</summary>
    public static async Task<IReadOnlyList<TreasuryMovement>> LoadAccountMovementsAsync(FalahDbContext db, string accountId, CancellationToken ct) =>
        (await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == nameof(MovementStatus.Posted) && (m.SourceAccountId == accountId || m.DestinationAccountId == accountId))
            .ToListAsync(ct))
        .Select(TreasuryMapper.ToDomain).ToList();

    /// <summary>
    /// Takes the account's spending lock and returns its posted balance, in order to SPEND from it.
    /// The lock is held until the caller's transaction ends.
    ///
    /// Every money-moving command reads this account's balance, decides the balance is sufficient,
    /// and then writes. Without the lock, several concurrent commands each read the SAME balance,
    /// each decide independently that it covers them, and each write - so an account holding 30
    /// can pay out three separate transfers of 20 and finish at -30. Idempotency does not help
    /// here: these are genuinely different operations, not a retry of one, and every existing
    /// protection in this system deduplicates by request key.
    ///
    /// PERF-0001 (audit 2026-11-18). This used to read every posted movement the account ever had
    /// WITH (UPDLOCK, HOLDLOCK). The account columns are not indexed, so that range lock covered the
    /// whole table: every checkout's own movement insert waited behind a refund, a safe drop or an
    /// expense, and the wait grew with the history. Now the spenders of one account queue on an
    /// application lock (exclusive, owned by the transaction), and the balance is a SUM read under
    /// READ_COMMITTED_SNAPSHOT (set at startup): it takes no row locks, so sales keep posting, and
    /// it sees every spend committed before this lock was granted. Money only ever arriving while
    /// the lock is held cannot make the balance insufficient, so inflows need no lock. Nothing in the
    /// system flips a posted movement to Reversed; a correction is a new movement that takes this lock.
    /// </summary>
    /// <summary>An account's posted balance for a WARNING or a display - no lock, no decision to spend. Owner,
    /// 2026-11-24: the till warns when its drawer passes the safe-drop limit.</summary>
    public static async Task<Money> ReadBalanceAsync(FalahDbContext db, string accountId, CancellationToken ct)
    {
        var posted = nameof(MovementStatus.Posted);
        var totals = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == posted && (m.SourceAccountId == accountId || m.DestinationAccountId == accountId))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                In = g.Sum(m => m.DestinationAccountId == accountId ? m.AmountPiastres : 0L),
                Out = g.Sum(m => m.SourceAccountId == accountId ? m.AmountPiastres : 0L),
            })
            .FirstOrDefaultAsync(ct);
        return totals is null ? Money.Zero : Money.FromPiastres(checked(totals.In - totals.Out));
    }

    public static async Task<Money> LockAndReadBalanceAsync(FalahDbContext db, string accountId, CancellationToken ct)
    {
        if (db.Database.CurrentTransaction is null) throw new InvalidOperationException("A balance check before spending requires a transaction.");
        var result = new SqlParameter("@balanceLockResult", SqlDbType.Int) { Direction = ParameterDirection.Output };
        var resource = new SqlParameter("@balanceLockResource", SqlDbType.NVarChar, 255) { Value = "FalahAccount:" + accountId };
        await db.Database.ExecuteSqlRawAsync(
            "EXEC @balanceLockResult = sys.sp_getapplock @Resource = @balanceLockResource, @LockMode = N'Exclusive', " +
            "@LockOwner = N'Transaction', @LockTimeout = " + LockTimeouts.Milliseconds,
            new object[] { result, resource }, ct);
        if (result.Value is not int code || code < 0) throw new BalanceLockTimeoutException(accountId, result.Value as int?);

        var posted = nameof(MovementStatus.Posted);
        var totals = await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == posted && (m.SourceAccountId == accountId || m.DestinationAccountId == accountId))
            .GroupBy(_ => 1)
            .Select(g => new
            {
                In = g.Sum(m => m.DestinationAccountId == accountId ? m.AmountPiastres : 0L),
                Out = g.Sum(m => m.SourceAccountId == accountId ? m.AmountPiastres : 0L),
            })
            .FirstOrDefaultAsync(ct);
        return totals is null ? Money.Zero : Money.FromPiastres(checked(totals.In - totals.Out));
    }
}

/// <summary>The account's spending lock was not granted in time (or SQL Server chose this request as a
/// deadlock victim). Nothing was written; BalanceLockContention treats it like any other lost race.</summary>
public sealed class BalanceLockTimeoutException : DomainException
{
    public BalanceLockTimeoutException(string accountId, int? code) : base(BalanceLockContention.RetryMessage)
    {
        AccountId = accountId;
        LockResult = code;
    }

    public string AccountId { get; }
    public int? LockResult { get; }
}

internal static class TreasuryMapper
{
    public static TreasuryMovement ToDomain(TreasuryMovementEntity e) => new(
        e.Id, e.IdempotencyKey, e.OperationId, e.Type, e.SourceAccountId, e.DestinationAccountId,
        Money.FromPiastres(e.AmountPiastres), e.ReferenceType, e.ReferenceId, e.BusinessDayId, e.ShiftId,
        e.Reason, e.CreatedBy, e.ApprovedBy, e.CreatedAtUtc, Enum.Parse<MovementStatus>(e.Status));
}

/// <summary>Known account ids this slice accepts. There is no Account aggregate/persistence yet
/// (docs/IMPLEMENTATION_MATRIX.md row 6) - these five are the fixed rows the reference app has
/// always had, validated here as a closed set rather than trusted from the request.</summary>
internal static class TreasuryAccountValidation
{
    private static readonly IReadOnlySet<string> AllAccounts = new HashSet<string>
    {
        TreasuryAccounts.Main, TreasuryAccounts.Drawer, TreasuryAccounts.Bank, TreasuryAccounts.InstaPay, TreasuryAccounts.Wallet,
    };

    /// <summary>Deposits/withdrawals only ever touch these three - never the drawer (operational
    /// cash, not owner capital) or wallet - mirrors ExternalFundingInput/OwnerWithdrawalInput.</summary>
    public static readonly IReadOnlySet<string> ExternalCapitalAccounts = new HashSet<string>
    {
        TreasuryAccounts.Main, TreasuryAccounts.Bank, TreasuryAccounts.InstaPay,
    };

    public static readonly IReadOnlySet<string> ValidDepositTypes = new HashSet<string>
    {
        "owner_deposit", "capital_injection", "other_external_inflow",
    };

    public static bool IsKnownAccount(string accountId) => AllAccounts.Contains(accountId);
}
