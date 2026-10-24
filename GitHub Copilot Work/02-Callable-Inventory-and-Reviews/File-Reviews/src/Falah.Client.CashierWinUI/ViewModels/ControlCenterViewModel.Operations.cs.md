# src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c15784c1887efa81f6fa2310c3d2a6f8061b609a7a4b76abb8a8e1663e8d8583`; worktree SHA256 `c15784c1887efa81f6fa2310c3d2a6f8061b609a7a4b76abb8a8e1663e8d8583`; LF-equivalent `True`.

Actual role: Business-day open/close/reopen, exception review and generic pending operation delegate.

## Function/resource review

- `OnSelectedHistoryDayChanged` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:16`: Starts selection load only when editable/not loading; SelectDay refuses other dialogs and snapshots selected ID, avoids switching current day during retained financial writes.

- `CanOpenDay` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:25`: Requires completed valid history load, permission and no existing active day across Days; CanClose shared base requires no server blockers. Server must re-evaluate atomically because client snapshot can age.

- `OpenDayAction` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:67`: Only opens action whose explicit gate true; resets reason/date. Open date invariant yyyy-MM-dd optional; reopening requires reason. No implicit DB operation merely opening form.

- `SubmitDayActionAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:78`: Creates immutable typed requests with Guid for fresh open/close/reopen; if pending calls RunOperation rather than rebuilding. Close sends no approval here because CanClose requires real permission; server authoritative.

- `SubmitExceptionAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:129`: Requires review permission and mandatory reason; known options acknowledged/resolved. Immutable request retained by delegate.

- `SetOperation` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:93`: Stores closed-over request in _pendingOperation delegate only, no journal. Power loss loses identity for day financial close/open/reopen/checklist/exception operations; parent can merge UI-001 extension.

- `RunOperationAsync` at `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Operations.cs:81`: Unknown history sticky across later definite reject, first definite rejection clears and reloads, success clears dialogs and reloads; finally pending session registration through IsBusy change. HasPendingOperation includes durable _safeDropRequest but this retry runner returns if delegate null, so generic RetryOperation cannot retry recovered safe drop itself; separate safe-drop submit does.

## Tests and limitations

ControlCenterWriteSafetyTests cover same-session fake HTTP, review separately. Missing day process recovery and recovered safe-drop generic retry reachability.

## Recommendations (not implemented)

Persist typed day/exception operations, make retry dispatch handle each actual pending owner, use server blocker/permission truth.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
