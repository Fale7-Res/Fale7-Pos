# src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `01a20063ad42ea2c09228b5197682d9d6f806ec31e751b863b230078a362f38f`; worktree SHA256 `79a3002b009480d7197f057aedd49d82b6ab2a27646a6a7f1bd2e91796fbaae9`; LF-equivalent `True`.

Actual role: Day state, close blockers/checklist/exception reads and embedded count.

## Function/resource review

- `ControlCenterViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs:18`: Constructs CashCount; restores pending safe-drop fields/dialog and registers pending at construction, a concrete correct recovery pattern. Does not recover generic in-memory day operations.

- `ReloadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs:81`: Loads day history then selected/current/dayfallback; legitimate404without history yields no day. Blockers/checklist/exceptions sequentially read and replaced. LoadError suppresses HasBusinessDay/close, so stale blockers cannot falsely enable close after query failure.

- `CanClose` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs:67`: Requires active loaded day, permission and zero blocker snapshot; server still responsible final atomic checks. Read-only status explanatory, not bypass.

- `ToggleChecklistItemAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs:148`: Captures intended !item.Completed once, stores generic operation before send; safely retries exact intended boolean within process, but no disk persistence.

- `OpenCashCountAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs:43`: Opens embedded count for actual selected day; CashCount own permission. No dummy records invented.

## Tests and limitations

Missing cross-process day-operation recovery, snapshots errors and partial sequential read UI test; parent source checklist comparison ongoing.

## Recommendations (not implemented)

Use durable generic request record and actual recovery dialog; preserve error states distinct from no day and server-final close verdict.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
