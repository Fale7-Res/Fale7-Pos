namespace Falah.Application.Treasury;

/// <summary>External money entering the business from outside any tracked account - an owner
/// funding the till, injected capital, or another one-off inflow. DestinationAccountId is
/// intentionally never the drawer or wallet (mirrors the reference app's ExternalFundingInput:
/// deposits land in tre-main/tre-bank/tre-instapay, never treated as walk-in drawer cash).
/// DepositRequestId is minted once per submission and must be reused unchanged by a retry.</summary>
public sealed record DepositRequest(
    string DepositRequestId, string Type, long AmountPiastres, string DestinationAccountId,
    string Reason, string? Reference, string CreatedByUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record DepositResult(string MovementId, string Type, long AmountPiastres, string DestinationAccountId, bool WasReplayed);

/// <summary>The owner taking money out of the business. SourceAccountId is never the drawer
/// (operational cash) or wallet - mirrors the reference app's OwnerWithdrawalInput exactly.
/// WithdrawalRequestId is minted once per submission.</summary>
public sealed record WithdrawalRequest(
    string WithdrawalRequestId, long AmountPiastres, string SourceAccountId,
    string Reason, string? Reference, string CreatedByUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record WithdrawalResult(string MovementId, long AmountPiastres, string SourceAccountId, bool WasReplayed);

/// <summary>Moving money between two of the business's own tracked accounts - no money enters or
/// leaves the business. TransferRequestId is minted once per submission.</summary>
public sealed record TransferRequest(
    string TransferRequestId, string SourceAccountId, string DestinationAccountId, long AmountPiastres,
    string Reason, string CreatedByUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record TransferResult(string MovementId, long AmountPiastres, string SourceAccountId, string DestinationAccountId, bool WasReplayed);

/// <summary>A cashier moving excess drawer cash into the safe mid-shift, before the shift itself
/// closes - distinct from the shift-close handover. Always sourced from tre-drawer and tied to the
/// shift it was dropped from (the same shiftId feeds into that shift's own expected-cash formula
/// as a cashOut component - see ShiftReconciliation). DropRequestId is minted once per submission.</summary>
public sealed record SafeDropRequest(
    string DropRequestId, string ShiftId, long AmountPiastres, string Reason, string CreatedByUserId,
    string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record SafeDropResult(string MovementId, long AmountPiastres, bool WasReplayed);

/// <summary>One posted movement in the treasury ledger. Read-only projection of the same
/// TreasuryMovement rows every financial command already writes - there is no second "treasury
/// reporting" table, so the ledger a manager reads and the ledger the balances are derived from can
/// never disagree.</summary>
public sealed record TreasuryMovementDto(
    string Id, string Type, string? SourceAccountId, string? DestinationAccountId, long AmountPiastres,
    string ReferenceType, string ReferenceId, string? ShiftId, string Reason, string CreatedBy,
    string? ApprovedBy, DateTimeOffset CreatedAtUtc);

/// <summary>Balances for every fixed account in one read, so a treasury screen shows one consistent
/// picture rather than five separately-timed reads that can disagree mid-refresh.</summary>
public sealed record TreasuryAccountBalanceDto(string AccountId, long BalancePiastres);

public sealed class TreasuryConflictException : Exception
{
    public TreasuryConflictException(string message) : base(message) { }
}
