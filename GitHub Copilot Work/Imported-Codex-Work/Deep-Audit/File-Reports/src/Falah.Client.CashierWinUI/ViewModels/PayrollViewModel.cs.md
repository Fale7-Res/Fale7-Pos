# src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `432a62de6fa95cb0d7e1eef8280deaf8f22d3dc88543d8999678c66c2634ddfa`; worktree SHA256 `8cf4e9f6ec47b2165413bc6ec1583f9261d08db7f950bdc75b3ee84fc8fe31a0`; LF-equivalent `True`.

Actual role: Payroll run list/line display/create/transition/pay with retained DTOs.

## Function/resource review

- `PayrollLineRow` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:46`: Uses actual base/additions/deductions/advance/net; distinguishes unpaid/paid, preserves accounting labels rather than blending advances with payments. Display aggregates use unchecked long; server outputs expected bounded but validation separate.

- `ReloadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:155`: Reads actual runs, selects previous/newest; public Load refuses if CanEditAction false or payment dialog. Recovered transition/pay DTO makes CanEditAction false before list load, leaving SelectedRun null with no recovery context.

- `LoadLinesAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:147`: Version increments and clears lines; discards stale response before EnsureEmployeeNamesAsync, but no version check after awaiting name read. Slow employee-name response allows old run lines appended into newer selection, potentially misleading payment amount display.

- `EnsureEmployeeNamesAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:194`: Caches names after first successful roster fetch, no cancellation/version. Failure intentionally uses raw IDs; repeated failures repeat request per run. Nested await race above not covered by initial version guard.

- `CreateRunAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:228`: YYYY-MM regex validates month, may accept Unicode \d year but server must enforce period. Pending create retry reachable independently of form/list; stable Guid frozen durable request.

- `AdvanceRunAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:267`: CanAdvance depends NextStatus SelectedRun; recovered transition no selection => unavailable. Same-process unknown transition rechecks authoritative run monotonic StatusRank to reconcile keyless endpoint. Good intent, runtime source-only; UI label uses current NextStatusLabel not captured target.

- `PayRunAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:332`: Requires IsPayOpen before checking recovered _payRequest; after restart false, OpenPay requires CanEditAction false so cannot restore. Source recovery dead-end analogous UI-005. Fresh account required and typed known accounts, server pays actual stored run authoritative total.

- `OnIsBusyChanged` at `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:26`: Registers pending only busy transitions; constructor recovers helpers but does not register initial session gate. Root nav can leave recovered page even though locally blocked. No explicit generic RetryAll recovery command in this file.

## Tests and limitations

Need concrete recovered transition/pay reconstruction test, delayed employee names cross-run version test, new period Unicode validity and server payment contract. No payroll writes executed.

## Recommendations (not implemented)

Recover target run/dialog from original DTO with standalone retry; recheck generation after every await; protected per-owner pending registry.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
