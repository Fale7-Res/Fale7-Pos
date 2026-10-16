# src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `63eced5b523342ededc72f205db0a82b23c2aea1bfc041eeb52de6444cf99645`; worktree SHA256 `a880d1ded757372d47ee1b1976bb1b61de63d1bb7531e72de7d43118314918f6`; LF-equivalent `True`.

Actual role: Create accounts and change/reset PIN with retained requests/retry.

## Function/resource review

- `RoleOptions` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:41`: Real cashier/manager/owner values; owner emergency uniqueness delegated server. Role selected uses observable value and help, no local secret backdoor.

- `OpenCreateUser` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:80`: Requires catalog+permission and editable; resets fields, builds actor-grantable options. CloseUserDialog clears all PIN text when allowed; pending request cannot be closed.

- `ValidatePin` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:102`: Checks confirmation equality then actual Domain PinHasher.AssertValidPin, catches only DomainException. No client hashing mistaken for server proof.

- `CreateUserAsync` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:110`: Fresh name/role/PIN validation and no unauthorized grant; stable CreateUserRequest with ID, actual actor. Durable JSON store serializes raw PIN field; sensitive persistence source evidence sent to parent security reviewer, no runtime secret accessed.

- `ChangePinAsync` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:148`: Fresh self action requires CurrentPin; admin reset forbids owner unless self. Retained self retry calls shared Api.LoginByPinAsync(new PIN) to prove intended change; contrary to comment it mutates HttpClient bearer even on different-user proof. Session.CurrentUser remains unchanged, identity confusion candidate sent to security lead.

- `RetryUserActionAsync` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:183`: Dispatches first pending create/PIN/permissions/active without transient dialog/selection requirement; correct reachable concept compared UI-005. Keys global users.* not per logged-in actor, need explicit ownership and journal confidentiality.

- `RefreshActions` at `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs:69`: Raises derived edit/create/change/dialog/labels; session pending updated only OnIsBusyChanged, so startup restored request may not block global nav yet.

## Tests and limitations

No credentials entered, saved or printed by reviewer. Need fake API verify token remains unchanged for successful/wrong-user proof, recovered other operator ownership and sensitive journal mitigation tests.

## Recommendations (not implemented)

Use isolated credential proof HTTP/token-free method that cannot replace session; versioned protected journal for sensitive request; reconcile server idempotency after self PIN change.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
