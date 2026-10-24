# src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `013787b64dabe18c525420c1ffc1d925149183a5ae04f0f9863ae2c4d49bc483`; worktree SHA256 `013787b64dabe18c525420c1ffc1d925149183a5ae04f0f9863ae2c4d49bc483`; LF-equivalent `True`.

Actual role: CallCenter root PIN/intake navigation, polling and close/lock safety.

## Function/resource review

- `MainViewModel` at `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:10`: Shows login then5second timer plus immediate Ping. No single-flight guard: timeout10seconds can overlap health requests. No recovered operation summary unlike cashier; actual desk restore happens after login.

- `CheckHealthNowAsync` at `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:32`: Late result suppressed only after Stop; overlapping results may reverse apparent online state. No cancellation to outstanding HTTP.

- `TryClose` at `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:43`: Reads session draft unknown/busy gate and InfoBar flag; normal close safety only, power loss relies journal.

- `Stop` at `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:49`: Stops health/header timers, no outstanding initialization cancellation.

- `Lock` at `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:57`: If no unresolved write shows login only; CurrentUser/bearer not cleared and Api.LogoutAsync uncalled. Parent security lead cross-checking intended lock/revoke contract.

- `OnLoggedIn` at `src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs:66`: async void event handler gates pos permission then sets CurrentView and awaits InitializeAsync. If operator locks while read initialization outstanding, old completion still updates VM/header/session. No login generation captured and thrown initialization exceptions escape async void outside LoginViewModel catch.

## Tests and limitations

Current plain test timer is no-op; cannot validate native overlap/focus. Missing logout endpoint invocation on lock, initialization after lock/user switch and async exception tests.

## Recommendations (not implemented)

Use cancellable/generation-bound initialization, safe task event bridge and real token revoke lifecycle; keep unknown order close gate.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
