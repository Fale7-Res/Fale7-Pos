namespace Falah.Infrastructure.Treasury;

using Falah.Domain.Common;
using Falah.Domain.Financial;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

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
    /// Reads an account's movements in order to SPEND from it, holding a lock on those rows until
    /// the caller's transaction commits.
    ///
    /// Every money-moving command reads this account's balance, decides the balance is sufficient,
    /// and then writes. Without the lock, several concurrent commands each read the SAME balance,
    /// each decide independently that it covers them, and each write - so an account holding 30
    /// can pay out three separate transfers of 20 and finish at -30. Idempotency does not help
    /// here: these are genuinely different operations, not a retry of one, and every existing
    /// protection in this system deduplicates by request key.
    ///
    /// HOLDLOCK is a range lock, which matters because the danger is a concurrent INSERT of a new
    /// movement for this account, not an update of an existing one - a plain row lock would leave
    /// exactly the gap being closed. The caller must already be inside a transaction; the lock is
    /// released when that transaction ends.
    /// </summary>
    public static async Task<IReadOnlyList<TreasuryMovement>> LoadAccountMovementsForUpdateAsync(
        FalahDbContext db, string accountId, CancellationToken ct) =>
        (await db.TreasuryMovements
            .FromSqlRaw(
                "SELECT * FROM TreasuryMovements WITH (UPDLOCK, HOLDLOCK) " +
                "WHERE Status = {0} AND (SourceAccountId = {1} OR DestinationAccountId = {1})",
                nameof(MovementStatus.Posted), accountId)
            .AsNoTracking()
            .ToListAsync(ct))
        .Select(TreasuryMapper.ToDomain).ToList();
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
