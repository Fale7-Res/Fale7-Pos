namespace Falah.Application.BusinessDay;

/// <summary>RequestId is minted once per "close business day" button press and must be reused
/// unchanged by a retry - the resulting snapshot's own id is deterministic from it
/// ("snapshot-{RequestId}"), so a retried close request can never mint a second snapshot for the
/// same close attempt. A genuinely NEW close (after a reopen) is a different call with a new
/// RequestId, not a retry.</summary>
public sealed record CloseBusinessDayRequest(
    string RequestId, string BusinessDayId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record CloseBlockerDto(string Code, string EntityType, string EntityId, string Message);

public sealed record BusinessDayCloseSnapshotDto(
    string Id, string BusinessDayId, int CloseRevision, bool Authoritative, string? PreviousSnapshotId,
    string BusinessDate, DateTimeOffset GeneratedAtUtc, string ClosedBy, string OperationId,
    string ReconciliationJson, string ExceptionsJson, string ChecklistJson, bool WasReplayed = false);

/// <summary>ReopenRequestId lets a retried reopen (network retry of the same click) recognize itself
/// - see BusinessDayEntity.ReopenRequestId's own doc comment for why a naive retry would otherwise
/// fail BusinessDayRules.Reopen's "day must be Closed" guard.</summary>
public sealed record ReopenBusinessDayRequest(
    string RequestId, string BusinessDayId, string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record BusinessDayDto(
    string Id, string BusinessDate, string Status, int CloseRevision, string? SnapshotId,
    string? ReopenReason, string? ReopenedBy, DateTimeOffset? ReopenedAtUtc, string? ReopenApprovedBy);

public sealed record ChecklistItemStatusDto(string Id, string Label, string Phase, bool Required, bool Completed, string? CompletedBy, DateTimeOffset? CompletedAtUtc);

public sealed record CompleteChecklistItemRequest(string BusinessDayId, string ItemId, bool Completed, string ActorUserId);

public sealed record ControlExceptionDto(
    string Id, string Category, string Severity, string Message, string EntityType, string EntityId,
    string Status, string? Actor, string? Reason, DateTimeOffset? TimestampUtc);

public sealed record SetExceptionStatusRequest(string ExceptionId, string Status, string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record ControlAuditEventDto(
    string Id, string Action, string Actor, string EntityId, long? AmountPiastres, string? OperationId,
    string? ApprovedBy, string? Reason, IReadOnlyList<string>? BlockerCodes, DateTimeOffset CreatedAtUtc);

/// <summary>Ported from saveCashCount in lib/enterprise-store.tsx - a blind physical denomination
/// count, finalized once per Id (a retry of the same Id is a harmless no-op, matching legacy's own
/// `if(existing)return existing`). Gated on the dedicated `cash_count` permission (distinct from
/// `business_day_close`) - a real gap found during the legacy-parity audit, since this feature had
/// no C# equivalent at all.</summary>
public sealed record SaveCashCountRequest(
    string Id, string BusinessDayId, string? ShiftId, IReadOnlyList<DenominationRowInput> Rows,
    long ExpectedPiastres, string ActorUserId);

public sealed record DenominationRowInput(long DenominationPiastres, long Quantity);

public sealed record DenominationRowDto(long DenominationPiastres, long Quantity, long TotalPiastres);

public sealed record CashDenominationCountDto(
    string Id, string BusinessDayId, string? ShiftId, IReadOnlyList<DenominationRowDto> Rows,
    long TotalPiastres, long ExpectedPiastres, long DifferencePiastres, string CountedBy,
    DateTimeOffset CreatedAtUtc, DateTimeOffset? FinalizedAtUtc, bool WasReplayed = false);

public sealed class BusinessDayCloseBlockedException : Exception
{
    public IReadOnlyList<CloseBlockerDto> Blockers { get; }
    public BusinessDayCloseBlockedException(IReadOnlyList<CloseBlockerDto> blockers)
        : base("لا يمكن إغلاق يوم العمل قبل معالجة الموانع المالية الحرجة.") => Blockers = blockers;
}
