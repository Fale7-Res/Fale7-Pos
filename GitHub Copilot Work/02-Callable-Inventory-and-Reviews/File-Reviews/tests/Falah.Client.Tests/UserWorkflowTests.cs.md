# tests/Falah.Client.Tests/UserWorkflowTests.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `5940ea22aac2610bbdf11898c61191ceb2757082551e016dfd30e5a25cee3838`; worktree SHA256 `5940ea22aac2610bbdf11898c61191ceb2757082551e016dfd30e5a25cee3838`; LF-equivalent `True`.

Actual role: Synthetic HTTP user-management immutability, privilege display and PIN retry tests.

## Function/resource review

- `Handler(` at `tests/Falah.Client.Tests/UserWorkflowTests.cs:11`: Session fixture routesGET to synthetic users/permissions and nonGET to callback. Json creates actual DTO-shaped body, not real token-bearing auth session.

- `User_creation_retains_the_original_identity_pin_and_permissions_after_lost_confirmation(` at `tests/Falah.Client.Tests/UserWorkflowTests.cs:26`: Edits fields after first lost response; compares3 identical serialized creates across422/success, blocks selection/close, clears PIN after success. Never constructs new VM or reads real secrets.

- `Existing_user_writes_retry_the_original_decision_and_target(` at `tests/Falah.Client.Tests/UserWorkflowTests.cs:58`: Theory permission/active changes retain target/decision with2 identical bodies and blocked switch/create; fake200 confirms.

- `Lost_self_pin_confirmation_is_reconciled_to_the_same_user_without_rewriting_the_pin(` at `tests/Falah.Client.Tests/UserWorkflowTests.cs:81`: Proof endpoint returns UserSummaryDto without access token. Asserts one write and one proof, cleared fields and current user unchanged. Therefore does not exercise token replacement in real AuthSessionResponse path.

- `A_pin_proof_for_a_different_user_does_not_confirm_the_pending_change(` at `tests/Falah.Client.Tests/UserWorkflowTests.cs:109`: Wrong-ID proof remains pending and retries original body; CurrentUser unchanged asserted. Bearer Authorization header not asserted, and proof response lacks token, leaving UI020 real token mutation uncovered.

- `Administrative_pin_reset_cannot_target_the_owner_and_creation_cannot_grant_unheld_permissions(` at `tests/Falah.Client.Tests/UserWorkflowTests.cs:128`: Manager users_manage/pos blocked owner PIN reset and new all permission; zero fixture writes verifies client gating. Does not establish server authorization or TOCTOU account change checks.

## Tests and limitations

SOURCE_REVIEW_ONLY, NOT_EXECUTED. PIN strings are fixture literals, no actual credential report. Missing token-bearing proof, fresh-session pending request, secure journal storage and concurrent account switch.

## Recommendations (not implemented)

Add real authentication response shape/header assertions and restart context tests; independently server-enforce all privilege rules.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
