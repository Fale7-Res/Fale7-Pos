# src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `8f1e681a0c405d42f40fd9d0f721b75e0276fb497d4c14e4620325ffad8e7d9c`; worktree SHA256 `10e6ac543011f2b5cae23d8aaad9ebed7e82e66514301050037f460c82e09b46`; LF-equivalent `True`.

Actual role: Management-page shift open dialog and actual open command.

## Function/resource review

- `OpenOpenShiftModalAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs:39`: Loads real settings suggestion and optional main balance; caps suggested float to actual visible balance. No busy state during prefetch; double open or navigation can leave late modal on detached view, no money moved yet.

- `CloseOpenShiftModal` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs:71`: Unconditional close even while IsOpeningShift; no unresolved write guard. Main page/session does not register pending busy, shell can leave/lock while open request in flight.

- `ConfirmOpenShiftAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs:74`: Positive/nonnegative money validation, own busy guard, creates new Guid RequestId every invocation. On transport ambiguity displays error but no frozen DTO/key retained. Retry can use edited amount/new key, unlike primary cashier flow UI-001. Exact server single-active-shift constraint can reject duplicate open but cannot reconstruct original result from changed identity.

- `MainTreasuryIsEmpty` at `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs:34`: Correct permission gate for balance display; domain insufficient-balance string masked for users without treasury visibility. Fragile localization substring match is not security boundary, server error output also matters.

## Tests and limitations

No concrete reconstructed or same-session unknown-result management open test observed yet; parent financial peer notified UI-016.

## Recommendations (not implemented)

Use same durable immutable operation coordination as primary cashier; prevent close/navigation/user switch through unknown financial outcome; retain exact key.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
