# src/Falah.Client.Cashier/ViewModels/MainViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `df67ef07edbc787c31f367396f0a511105278abab0a5d8b5da8c71d49c0bce37`; worktree SHA256 `df67ef07edbc787c31f367396f0a511105278abab0a5d8b5da8c71d49c0bce37`; LF-equivalent `True`.

Actual role: WPF login/cashier switch and online polling.

## Function/resource review

- `MainViewModel` at `src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:12`: Constructs fixed session/header, shows login,5secDispatcherTimer and immediate ping; no Stop/dispose and overlapping10sectimeout polls possible. No native window close safe gate.

- `Lock` at `src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:41`: Unconditionally creates new login, abandoning current cashier VM/cart/pending. ShowCashier creates fresh VM, session shift reused but drafts not; lacks active WinUI per-user retained draft.

- `ShowCashier` at `src/Falah.Client.Cashier/ViewModels/MainViewModel.cs:47`: No server active shift discovery after fresh process, session ShiftId initiallynull; cannot automatically find existing server shift through this flow. No backend calls except menu if local session shift already set.

## Tests and limitations

WPF reference not runtime-tested; no hidden persistence beyond normal DispatcherTimer.

## Recommendations (not implemented)

Document unsupported/reference scope, avoid using this executable as real POS; active architecture WinUI backend remains existing.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
