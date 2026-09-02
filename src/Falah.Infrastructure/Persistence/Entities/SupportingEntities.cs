namespace Falah.Infrastructure.Persistence.Entities;

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
    /// <summary>
    /// Matches the existing (reference) Falah app exactly: the lock/login screen identifies a user
    /// by PIN alone (no separate username entry) - `Pin` must be unique among active users for that
    /// lookup to be unambiguous. Stored in plain text for now, same as the reference app; a real
    /// hash is a hardening TODO, not a behavioral migration gap.
    /// </summary>
    public string Pin { get; set; } = default!;
}
