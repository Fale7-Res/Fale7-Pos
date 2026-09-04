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
    public static async Task<IReadOnlyList<TreasuryMovement>> LoadAccountMovementsAsync(FalahDbContext db, string accountId, CancellationToken ct) =>
        (await db.TreasuryMovements.AsNoTracking()
            .Where(m => m.Status == nameof(MovementStatus.Posted) && (m.SourceAccountId == accountId || m.DestinationAccountId == accountId))
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
