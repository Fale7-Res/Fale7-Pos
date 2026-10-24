# src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11`; worktree SHA256 `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11`; LF-equivalent `True`.

Actual role: Management active shift/history/details and document reprint.

## Function/resource review

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:120`: No IsLoading entry guard/cancellation/version; overlapping loads may overwrite current shift/session with stale result. Treats ANY current shift failure as null, clears shared ShiftId/Name silently even network error; can make cashier UI wrongly think no shift despite server open.

- `ReprintAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:82`: Fresh Guid every press; correct typed slip document and real actor, says queued not paper confirmed. Unknown result not retained and no global pending gate, retry can enqueue extra slip. No actual printers invoked.

- `FormatCairo` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:63`: Correct explicit Cairo conversion for timestamps unlike Header clock local timezone.

- `ShiftRow` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:73`: Maps real summary fields, status Closed exact case. CashDifference null becomes0then IsBalanced true for closed record lacking counted difference; potential misleading balanced unknown state, contract null semantics require server DTO proof.

- `CanOpenShift` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs:14`: Uses actual shifts_manage/own; history reports_view, main balance treasury_manage. No OnIsOpeningShiftChanged/OnIsClosingShiftChanged session gate anywhere three partials: same-session safety gap source-confirmed UI-016.

## Tests and limitations

Native management layout probe and backend financial tests separate; need failed current shift fetch keeps/labels unknown, overlapping-load ordering, ambiguous reprint identity.

## Recommendations (not implemented)

Share reliable shift/session refresh error contract and operation coordinator; do not convert missing numeric history to matched count without evidence.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
