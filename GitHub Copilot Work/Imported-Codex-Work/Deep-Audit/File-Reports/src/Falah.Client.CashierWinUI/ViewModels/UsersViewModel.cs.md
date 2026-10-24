# src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `df821afd3d2a61ce1b6e7fbf7fcd52c6ddd12ccb2f6a215ee611c17684e571bc`; worktree SHA256 `df821afd3d2a61ce1b6e7fbf7fcd52c6ddd12ccb2f6a215ee611c17684e571bc`; LF-equivalent `True`.

Actual role: User list, server permission vocabulary, grant/revoke and activation.

## Function/resource review

- `PermissionToggle` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:26`: Localized exact permission/category map with fallback to key; CanActorGrant true for existing held permission OR actor own permission allows revoke while preventing new unauthorized grant in normal UI.

- `RebuildToggles` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:90`: Uses actual fetched catalog and selected-user permissions; no invented backend privileges. Immutable per-entry CanActorGrant assumes login permissions fixed until next root login.

- `ReloadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:111`: Reads users+catalog sequential, keeps selected ID, clears list only after both valid. Recovered pending action makes public LoadAsync return early, so initial page list can stay blank while dedicated RetryUserAction must be visible.

- `CanEditSelected` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:81`: Requires users_manage, valid loaded selection, nonowner and no dialogs/pending. Server must independently enforce owner immunity, actor/session auth and escalation.

- `SavePermissionsAsync` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:149`: Computes grant/revoke diff against selected snapshot; uses durable request on unknown retry. Concurrent other admin edits between reads/writes resolved server-side; potential stale revoke semantics but explicit diff not full replacement.

- `ToggleActiveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.cs:185`: Retains exact target state (not repeated inversion) across ambiguity, reloads target after success. OnIsBusyChanged registers pending; constructor does not register recovered state before busy flag transition.

## Tests and limitations

UserWorkflowTests/ManagementWriteSafety relevant not yet all read. Missing global recovered request ownership/initial pending gate and actual native retry reachability.

## Recommendations (not implemented)

Use per-actor recovery context and initial session pending registration; authoritative server checks remain mandatory.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
