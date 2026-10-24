# src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0f7aca8ebf32a8b423ec7145e4f2e7856ebc8e03d528310ed6fa3a53c721eb73`; worktree SHA256 `0f7aca8ebf32a8b423ec7145e4f2e7856ebc8e03d528310ed6fa3a53c721eb73`; LF-equivalent `True`.

Actual role: Real reconciliation/snapshot reads and durable safe-drop action.

## Function/resource review

- `LoadReconciliationAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs:28`: Requires reports_view; failed fetch sets null but no dedicated error, then ReconciliationDifference returns0 through null-coalesce. XAML may display0for unavailable data unless gated; inspect parent binding before claiming.

- `OpenSafeDrop` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs:48`: Requires editable active day, resets inputs/PIN; HasOpenShiftForDrop reads shared session rather than querying at open. Submit guards actual ShiftId presence; server validates freshness.

- `SubmitSafeDropAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs:72`: Recovered request sent before current form checks, valid identity-preserving recovery. Fresh amount positive/reason/approval; uses actual current shift and actor.

- `CompleteSafeDropAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs:75`: Durable helper, rotate key on success, action amount from server, reload day. Constructor base actually restores dialog/request fields and session pending, better than several other management consumers.

- `SnapshotReconciliation` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs:125`: Deserializes persisted JSON each property read without JsonException catch/version contract; malformed/older snapshot can throw during binding. Does not recompute historic snapshot intentionally.

- `LoadSnapshotAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs:128`: Sets latest snapshot null on failed query, no dedicated reason, unknown/no snapshot conflated. ResolveApproval helper is one real PIN approval endpoint, no local identity trust.

## Tests and limitations

Missing malformed snapshot JSON/native binding recovery and unavailable reconciliation rendering; no DB writes/restore tested.

## Recommendations (not implemented)

Distinguish unavailable vs zero snapshot data and deserialize once with safe error handling; preserve working recovered safe-drop context.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
