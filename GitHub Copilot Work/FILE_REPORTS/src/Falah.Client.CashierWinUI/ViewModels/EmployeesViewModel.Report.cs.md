# src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Report.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0bee0c4affb97767885d4c3d6483263af7c66ee4e4ea203da9c3495ba91f45d4`; worktree SHA256 `0bee0c4affb97767885d4c3d6483263af7c66ee4e4ea203da9c3495ba91f45d4`; LF-equivalent `True`.

Actual role: Employee financial report date presets, real line query and stale-read protection.

## Function/resource review

- `ResolveReportRange` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Report.cs:51`: Explicit BusinessClock.Today Cairo, last7today-6,last10-9,monthfirst; custom DateOnly of picker; inverted range sorted. All-time remaining ledger balance documented separate from period flow.

- `OnSelectedReportPeriodChanged` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Report.cs:56`: Updates custom/range and starts read if visible; custom date changes launch read but range label only explicitly updated inside loader. Good no silent DB mutation.

- `OpenReportAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Report.cs:81`: Requires editable state and reports_view; defaults real today. CloseReport increments version and clears data/error, invalidates pending response.

- `LoadReportAsync` at `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Report.cs:60`: Each read snapshots version, only current response publishes and clears busy; protects date switch and closed report. Does not cancel network calls, so rapid picker changes still generate many wasted requests. No database writes.

## Tests and limitations

Need delayed report/version and preset boundaries tests, no native rendered table measurement. Correct good pattern for UI-007 comparison.

## Recommendations (not implemented)

Reuse generation pattern for customer/recipe/payroll reads; add cancellation/debounce for large repeated queries while preserving latest state.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
