namespace Falah.Infrastructure.Persistence.Entities;

/// <summary>Ported from OperationalChecklistItem - a fixed, small catalog seeded once (see
/// FalahDbContext's HasData), matching the reference app's own hardcoded checklistItems array
/// exactly. There is no admin CRUD for this catalog in either system - editing it is a deployment-
/// time change, not a runtime one.</summary>
public class ChecklistItemEntity
{
    public string Id { get; set; } = default!;
    public string Label { get; set; } = default!;
    /// <summary>"Opening" or "Closing" - which ceremony this item belongs to. Only Closing items
    /// that are both Active and Required can ever block a business day close.</summary>
    public string Phase { get; set; } = default!;
    public bool Required { get; set; }
    public bool Active { get; set; }
    public int SortOrder { get; set; }
}

/// <summary>One checklist item's completion state for one business day - composite-keyed so
/// completing the same item twice for the same day is a plain upsert, never a duplicate row.
/// Reopening a business day clears its Closing-phase completions (see ReopenBusinessDayHandler),
/// exactly like the reference app's own reopenBusinessDay - a closing checklist must be redone
/// before the day can close again.</summary>
public class ChecklistCompletionEntity
{
    public string BusinessDayId { get; set; } = default!;
    public string ItemId { get; set; } = default!;
    public bool Completed { get; set; }
    public DateTimeOffset? CompletedAtUtc { get; set; }
    public string? CompletedBy { get; set; }
}

/// <summary>Ported from DailyCloseSnapshot - the frozen, reproducible record of one close attempt.
/// Reconciliation/Exceptions/Checklist are stored as JSON exactly like Order.SplitPaymentsJson
/// elsewhere in this codebase, since they are archival read-backs of a point-in-time computation,
/// never queried by their own fields. Authoritative=false on every snapshot except the day's current
/// one - closing the same day again (after a reopen) produces a NEW snapshot at the next
/// CloseRevision without ever deleting the previous one, preserving full history.</summary>
public class BusinessDayCloseSnapshotEntity
{
    /// <summary>Deterministic from the close RequestId ("snapshot-{RequestId}") - the exact same
    /// idempotency pattern as every other financial/audit command in this codebase, so a retried
    /// close request can never mint two snapshots for the one close attempt it represents.</summary>
    public string Id { get; set; } = default!;
    public string BusinessDayId { get; set; } = default!;
    public int CloseRevision { get; set; }
    public bool Authoritative { get; set; }
    public string? PreviousSnapshotId { get; set; }
    public DateOnly BusinessDate { get; set; }
    public DateTimeOffset GeneratedAtUtc { get; set; }
    public string ClosedBy { get; set; } = default!;
    public string OperationId { get; set; } = default!;
    public string ReconciliationJson { get; set; } = default!;
    public string ExceptionsJson { get; set; } = default!;
    public string ChecklistJson { get; set; } = default!;
}

/// <summary>Ported from the reference app's separate `exceptionStates` map (Record&lt;id,
/// {status,actor,reason,timestamp}&gt;) - control exceptions themselves are never stored (they are
/// recomputed fresh from live data on every read, see ControlCenterRules.BuildExceptions); only a
/// human's acknowledge/resolve action against one is persisted, keyed by that exception's own
/// deterministic id so the overlay always lines back up with whichever exception it was raised
/// against, even across recomputations.</summary>
public class ControlExceptionStateEntity
{
    public string Id { get; set; } = default!;
    public string Status { get; set; } = default!;
    public string Actor { get; set; } = default!;
    public string Reason { get; set; } = default!;
    public DateTimeOffset TimestampUtc { get; set; }
}

/// <summary>Ported from ControlAuditEvent - a narrower, purpose-built trail for rare, high-stakes
/// administrative actions this system has committed to always auditing from evidence: business-day
/// open/close/reopen and exception review (Action values business_day_*/exception_*), and - added
/// alongside the user-management slice - user/permission changes (user_created/user_deactivated/
/// user_reactivated/pin_changed/permissions_changed). Distinct from the general-purpose AuditEvent
/// shape in Falah.Domain (row 15 of IMPLEMENTATION_MATRIX.md), which remains domain-only/unwired and
/// covers the FULL breadth of state changes (orders, shifts, delivery, cash, print); this table is
/// deliberately the narrower "who changed the control plane itself" trail, not a replacement for it.</summary>
public class ControlAuditEventEntity
{
    public string Id { get; set; } = default!;
    public string Action { get; set; } = default!;
    public string Actor { get; set; } = default!;
    public string EntityId { get; set; } = default!;
    public long? AmountPiastres { get; set; }
    public string? OperationId { get; set; }
    public string? ApprovedBy { get; set; }
    public string? Reason { get; set; }
    /// <summary>Only set on a business_day_close_blocked event - the exact CloseBlocker codes that
    /// stopped the close, serialized as a JSON string array.</summary>
    public string? BlockerCodesJson { get; set; }
    public DateTimeOffset CreatedAtUtc { get; set; }
}

/// <summary>Ported from CashDenominationCount in enterprise-types.ts / saveCashCount in
/// enterprise-store.tsx - a blind physical count of the drawer broken down by Egyptian pound
/// denomination, finalized once per (businessDayId, id) and never revised in place (a real gap
/// found during the legacy-parity audit: this feature had no C# equivalent at all). RowsJson is a
/// JSON-serialized array of BusinessDayRules.DenominationRow - a fixed, small, display-only
/// breakdown with no other table ever querying into it, so a JSON column is proportionate rather
/// than a full child table.</summary>
public class CashDenominationCountEntity
{
    public string Id { get; set; } = default!;
    public string BusinessDayId { get; set; } = default!;
    public string? ShiftId { get; set; }
    public string RowsJson { get; set; } = default!;
    public long TotalPiastres { get; set; }
    public long ExpectedPiastres { get; set; }
    public long DifferencePiastres { get; set; }
    public string CountedBy { get; set; } = default!;
    public DateTimeOffset CreatedAtUtc { get; set; }
    public DateTimeOffset? FinalizedAtUtc { get; set; }
}
