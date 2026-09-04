namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from BusinessDay in the reference app. Open-only was the original scope of this
/// entity (see git history / IMPLEMENTATION_MATRIX.md row 6's own note); close/reopen fields were
/// added once Falah.Domain.BusinessDay.BusinessDayRules (already ported and tested) was finally
/// wired end to end. ClosedAtUtc doubles as "closing started at" - this system's close command is
/// always synchronous (open/reopened -> closing -> closed happens within one request, never left
/// sitting in an intermediate "closing" persisted state), so a separate ClosingStartedAtUtc column
/// would always hold the exact same value and was deliberately not added.</summary>
public class BusinessDayEntity
{
    public string Id { get; set; } = default!;
    public DateOnly BusinessDate { get; set; }
    public string OpenedBy { get; set; } = default!;
    public DateTimeOffset OpenedAtUtc { get; set; }
    public string Status { get; set; } = default!;
    public int CloseRevision { get; set; }
    public string? SnapshotId { get; set; }
    public string? ReopenReason { get; set; }
    public string? ReopenedBy { get; set; }
    public string OpenRequestId { get; set; } = default!;

    public DateTimeOffset? ClosedAtUtc { get; set; }
    public string? ClosedBy { get; set; }
    /// <summary>The close snapshot/timestamp/closer that were authoritative BEFORE this reopen -
    /// preserved so a day's full close/reopen/close history stays readable even after it is closed
    /// again under a brand new snapshot.</summary>
    public string? PreviousSnapshotId { get; set; }
    public DateTimeOffset? PreviousClosedAtUtc { get; set; }
    public string? PreviousClosedBy { get; set; }
    public DateTimeOffset? ReopenedAtUtc { get; set; }
    public string? ReopenApprovalId { get; set; }
    public string? ReopenApprovedBy { get; set; }
    /// <summary>The RequestId of the reopen that most recently succeeded - lets a retried reopen
    /// call (network retry of a same-task double click) recognize itself and return the already-
    /// reopened day instead of failing BusinessDayRules.Reopen's own "day must be Closed" guard,
    /// which a naive retry would otherwise trip since the day is no longer Closed by the time the
    /// retry arrives.</summary>
    public string? ReopenRequestId { get; set; }
}

/// <summary>Ported from the Expense shape in the reference app's lib/types.ts / addExpense.
/// Id is itself the idempotency key ("exp-{requestId}"), matching expenses.some(item => item.id
/// === expenseId) in the original - not a separate generated id.</summary>
public class ExpenseEntity
{
    public string Id { get; set; } = default!;
    public string? BusinessDayId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public string Category { get; set; } = default!;
    public long AmountPiastres { get; set; }
    public string Description { get; set; } = default!;
    public string CreatedByUserId { get; set; } = default!;
    public string ShiftId { get; set; } = default!;
    public bool PaidFromDrawer { get; set; }
    public string PaymentAccountId { get; set; } = default!;
    public string OperationId { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string? TreasuryMovementId { get; set; }
    public string? ApprovedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

public class ShiftEntity
{
    public string Id { get; set; } = default!;
    public string ShiftName { get; set; } = default!;
    public string OpenedByUserId { get; set; } = default!;
    public string? ClosedByUserId { get; set; }
    public long OpeningCashPiastres { get; set; }
    public long? ActualCashPiastres { get; set; }
    public long? ExpectedCashPiastres { get; set; }
    public long? CashDifferencePiastres { get; set; }
    public string? DifferenceReason { get; set; }
    public string? DifferenceAuthorizedBy { get; set; }
    public string Status { get; set; } = default!;
    public DateOnly BusinessDate { get; set; }
    public DateTimeOffset OpenedAtUtc { get; set; }
    public DateTimeOffset? ClosedAtUtc { get; set; }
    public string OpenRequestId { get; set; } = default!;
    /// <summary>Minted once per "confirm close" button press, same idempotency contract as
    /// OpenRequestId - null until the shift is actually closed.</summary>
    public string? CloseRequestId { get; set; }
    /// <summary>Movement ids for the two postings a close can make (see ShiftCloseHandler) - kept
    /// on the shift itself so a replay can report them without re-deriving anything.</summary>
    public string? ReconciliationTreasuryMovementId { get; set; }
    public string? HandoverTreasuryMovementId { get; set; }
    public uint Version { get; set; }
}

public class TreasuryMovementEntity
{
    public string Id { get; set; } = default!;
    public string IdempotencyKey { get; set; } = default!;
    public string OperationId { get; set; } = default!;
    public string Type { get; set; } = default!;
    public string? SourceAccountId { get; set; }
    public string? DestinationAccountId { get; set; }
    public long AmountPiastres { get; set; }
    public string ReferenceType { get; set; } = default!;
    public string ReferenceId { get; set; } = default!;
    public string? BusinessDayId { get; set; }
    public string? ShiftId { get; set; }
    public string Reason { get; set; } = default!;
    public string CreatedBy { get; set; } = default!;
    public string? ApprovedBy { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
    public string Status { get; set; } = default!;
}

public enum JournalEntryStatus { Pending, Complete, Failed, RecoveryRequired }

public class FinancialJournalEntryEntity
{
    /// <summary>The idempotency key IS the primary key - the whole point of this table is a fast,
    /// DB-enforced "has this exact business event already been started" check that survives a
    /// crash or restart, with no window for a race to slip past it.</summary>
    public string IdempotencyKey { get; set; } = default!;
    public string Kind { get; set; } = default!;
    public string Status { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Single-row table: the one central counter every order number is allocated from,
/// regardless of which device's request created the order.</summary>
public class OrderSequenceEntity
{
    public int Id { get; set; }
    public long LastAllocated { get; set; }
}

public class UserEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string Role { get; set; } = default!;
    public bool Active { get; set; }
    /// <summary>Comma-separated permission list, or the literal "all" - small and simple enough
    /// not to warrant its own join table yet.</summary>
    public string Permissions { get; set; } = default!;
    /// <summary>PBKDF2-SHA256 hash of the PIN (Falah.Domain.Authorization.PinHasher) - the lock/login
    /// screen still identifies a user by PIN alone (no username), but the PIN itself is never stored
    /// or compared in plain text. The reference app's own plaintext Pin column existed here until
    /// migration AddUserPinHashColumns + a one-time UserPinBackfill run, then DropPlaintextUserPin
    /// removed it for good.</summary>
    public string PinHash { get; set; } = default!;
    public string PinSalt { get; set; } = default!;
    public int PinIterations { get; set; }
}
