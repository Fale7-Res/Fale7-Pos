# src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `137baa022eb1e158ae5ab7ae1b6e986cfe2bcd96853c091fd9d5d1d101db7929`; worktree SHA256 `137baa022eb1e158ae5ab7ae1b6e986cfe2bcd96853c091fd9d5d1d101db7929`; LF-equivalent `True`.

Actual role: Older WPF shift open flow.

## Function/resource review

- `OpenShiftAsync` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs:14`: Current culture decimal nonnegative parsed, rounds to piastres with overflow not bounded. No explicit busy guard/permission check inside method; generated async command usual concurrency helps native command invocation but direct calls/shared cross-state not coordinated. Fresh Guid per attempt, unknown result not retained; same safety problems as management UI-016 but reference client.

- `OpenShiftRequest` at `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Shift.cs:26`: After success session real ID/name from response, header refresh/menu load. Time-based label machine-local hour, no malicious scheduled behavior.

## Tests and limitations

WPF source not in current headless client tests; backend authorization/constraints independent.

## Recommendations (not implemented)

Prevent obsolete reference deployment or update only approved future maintenance with active-client safety parity.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
