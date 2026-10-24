# src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs

BusinessDay API contract review.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `d6d8f05a2959343b0824dcb3d4f1d57bd342469fc8e0bc57a539b3cfe08402d1`. Working SHA-256 `d6d8f05a2959343b0824dcb3d4f1d57bd342469fc8e0bc57a539b3cfe08402d1`. Exact bytes equal: True; LF-normalized equal: True.

Reviewed lines: 1-65.

Symbols indexed (index is not a claim every method was reviewed): CloseBusinessDayRequest@8, CloseBlockerDto@11, BusinessDayCloseSnapshotDto@13, ReopenBusinessDayRequest@21, BusinessDayDto@24, ChecklistItemStatusDto@28, CompleteChecklistItemRequest@30, ControlExceptionDto@32, SetExceptionStatusRequest@36, ControlAuditEventDto@38, SaveCashCountRequest@47, DenominationRowInput@51, DenominationRowDto@53, CashDenominationCountDto@55, BusinessDayCloseBlockedException@60.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 8: `public sealed record CloseBusinessDayRequest(`
- Line 11: `public sealed record CloseBlockerDto(string Code, string EntityType, string EntityId, string Message);`
- Line 13: `public sealed record BusinessDayCloseSnapshotDto(`
- Line 21: `public sealed record ReopenBusinessDayRequest(`
- Line 24: `public sealed record BusinessDayDto(`
- Line 28: `public sealed record ChecklistItemStatusDto(string Id, string Label, string Phase, bool Required, bool Completed, string? CompletedBy, DateTimeOffset? CompletedAtUtc);`
- Line 30: `public sealed record CompleteChecklistItemRequest(string BusinessDayId, string ItemId, bool Completed, string ActorUserId);`
- Line 32: `public sealed record ControlExceptionDto(`
- Line 36: `public sealed record SetExceptionStatusRequest(string ExceptionId, string Status, string Reason, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);`
- Line 38: `public sealed record ControlAuditEventDto(`
- Line 47: `public sealed record SaveCashCountRequest(`
- Line 51: `public sealed record DenominationRowInput(long DenominationPiastres, long Quantity);`
- Line 53: `public sealed record DenominationRowDto(long DenominationPiastres, long Quantity, long TotalPiastres);`
- Line 55: `public sealed record CashDenominationCountDto(`
- Line 63: `public BusinessDayCloseBlockedException(IReadOnlyList<CloseBlockerDto> blockers)`

Commands carry request IDs for lifecycle changes and snapshot metadata; cash counts carry denomination rows plus client expected amount. DTOs do not enforce nonnegative quantities, authoritative expected balance, or close consistency; handlers must. No transaction exists in the contract itself.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
