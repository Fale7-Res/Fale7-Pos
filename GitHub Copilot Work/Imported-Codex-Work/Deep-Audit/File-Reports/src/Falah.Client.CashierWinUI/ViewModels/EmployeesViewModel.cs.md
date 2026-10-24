# src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `ab1cb0a573412633b4c33fce3282e37859163da8cecc156ff2df507fa8f468d8`; worktree SHA256 `04f70fc8f689932e34bc5cdf5f9ee1b5c04dbdb35a053f6ed2719a17cfd0eb7b`; LF-equivalent `True`.

Actual role: Roster/balances/ledger and wage/overtime/extra-shift/cash-out actions.

## Function/resource review

- `EmployeesViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:19`: Owns real editor and Saved reload; caches _all real roster. Every employee balance fetched separately after list and settings: N+2sequential calls, noticeable large roster/10second timeouts, no batch endpoint in this UI.

- `ReloadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:29`: On balance failure stops and LoadError set; list cleared first so no false partial rows; overtime rate stays previous/default if settings read fails silently, preview may wrong while server rate authoritative.

- `ReloadLedgerAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:131`: Version+row identity guard after fetch, Close increments version; correct delayed read isolation. Distinguishes empty from failed and allows read retry. AttendanceWorkflowTests cover these good paths.

- `OpenAction` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:177`: Sets known kind, employee, default amount/2hours, one request ID. Exposes no hard delete of employee or ledger. Requires shift only for physical drawer advance/withdrawal; earning accrual separate.

- `SubmitCashOutAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:290`: Retries any existing typed pending first; fresh requires active employee context, shift when drawer, reason. Real approval. Daily wage has no client request ID in DTO, server idempotency must reconcile actual business key. Overtime and extra shift stable once per modal.

- `CompleteCashOutAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:293`: Four completion methods each call correct durable helper/typed API; FinishAction records server outcome note, closes target and reloads. Source journal exists but constructor restores no CashOutEmployee dialog; public load blocked while any pending, all row actions blocked, so native retry reachability must be checked.

- `CanEditAction` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:77`: Blocks all5helpers pending, session registration only OnIsBusyChanged; initial reconstructed helper state not registered globally. Source-level recovery mismatch family, not proof duplicate actual salary.

- `ResolveApprovalAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs:313`: Single real manager PIN exchange when user lacks employees_ledger. Role page employees_manage separate permission may need approval, appropriate fallback.

## Tests and limitations

Need reconstructed each money-operation actual UI retry target, batch roster scaling and overtime preview missing-settings behavior. No financial write/employee data read from live backend.

## Recommendations (not implemented)

Hydrate recovered action or show generic retry independent row; register pending immediately; batch/cancel roster reads; preserve earned accrual vs actual disbursement distinctions.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
