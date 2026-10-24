# src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `61485ee26cba0c6f32368c1690a1d6129bac90650cad048a8468d8d28bdabb0f`; worktree SHA256 `61485ee26cba0c6f32368c1690a1d6129bac90650cad048a8468d8d28bdabb0f`; LF-equivalent `True`.

Actual role: WPF reference fixed branding/user/shift and local clock.

## Function/resource review

- `HeaderViewModel` at `src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:14`: 30sectimer updates machine-local clock ar-EG; no Stop, registered handler owns header process lifetime. RestaurantName hardcoded, no live settings identity.

- `UpdateClock` at `src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:22`: Formats actual DateTime.Now date/time. Legitimate display timer, no destructive/time activation.

- `Refresh` at `src/Falah.Client.Cashier/ViewModels/HeaderViewModel.cs:55`: Notifies actual session role/name/shift; no CanLock or global unknown-operation reflection. No SQL/printing itself.

## Tests and limitations

NOT_APPLICABLE money transaction tests; actual WPF timer/branding not covered by WinUI header stubs.

## Recommendations (not implemented)

Reference-only documentation; production clock must align business timezone and actual restaurant settings.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
