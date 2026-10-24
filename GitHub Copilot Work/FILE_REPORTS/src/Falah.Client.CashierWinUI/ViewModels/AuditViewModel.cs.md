# src/Falah.Client.CashierWinUI/ViewModels/AuditViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `385a1783a3f55cb890a61ec79b0c7ccc4467ceed1b9f5beace22ddf8bdf7c29e`; worktree SHA256 `385a1783a3f55cb890a61ec79b0c7ccc4467ceed1b9f5beace22ddf8bdf7c29e`; LF-equivalent `True`.

Actual role: Read-only append-only history filter screen.

## Function/resource review

- `Categories` at `src/Falah.Client.CashierWinUI/ViewModels/AuditViewModel.cs:36`: Hardcodes8categories while comment asserts server actual taxonomy; backend may emit more categories. All-categories still exposes others if query no filter. Severities critical/warning/normal similarly explicit list, full vocabulary cross-check pending.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/AuditViewModel.cs:56`: Coalesces filter changes with selected category/severity snapshots; discards old result/failure and loops requested filter. Clears Events before fetch and correctly separates loading/error/empty. No edit/delete method on UI.

- `NotifyReadState` at `src/Falah.Client.CashierWinUI/ViewModels/AuditViewModel.cs:64`: Raises HasNoEvents/HasEvents after load state; delegates server filtering/limit default200. No paging/continuation makes older audit records beyond returned cap unreachable through this screen.

## Tests and limitations

AttendanceWorkflowTests meaningful delayed filter test reviewed; no server append-only tamper-proof property established from read-only UI.

## Recommendations (not implemented)

Add paginated query/date range and complete catalog from server where needed; preserve unfiltered view and safe error distinctions.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
