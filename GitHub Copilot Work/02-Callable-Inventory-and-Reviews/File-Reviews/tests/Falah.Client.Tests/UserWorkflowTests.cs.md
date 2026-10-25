# File Review: tests/Falah.Client.Tests/UserWorkflowTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/UserWorkflowTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:13:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:13:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Test transport mock delegating HTTP requests directly to a captured `Func<HttpRequestMessage, Task<HttpResponseMessage>>` callback. Allows synchronous or asynchronous interception of client requests, inspection of payloads, and controlled simulation of connection drops (`HttpRequestException`) or error status codes without network I/O.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Json(object, System.Net.HttpStatusCode)
- **Lines:** [15-15]
- **Behavior:** Test helper factory creating an `HttpResponseMessage` containing serialized JSON content (`JsonContent.Create(value)`) and the specified HTTP status code (defaults to `HttpStatusCode.OK`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [18-23]
- **Behavior:** Test fixture factory initializing an `AppSession` with a mocked `ApiClient`. Intercepts GET requests to `/api/users/all` (returning owner and target user DTOs) and permissions catalogs, routing all mutation requests (POST/PUT) to the caller-supplied `write` callback. Seeds `CurrentUser` as the Owner (`actor`, role `owner`, permission `all`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:19:54:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:19:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [19-22]
- **Behavior:** Inline routing lambda: for GET requests, returns mock JSON for `/api/users/all` or permissions catalogs; for mutation requests, delegates to `write(r)`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:25:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:25:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.User_creation_retains_the_original_identity_pin_and_permissions_after_lost_confirmation()
- **Lines:** [25-53]
- **Behavior:** End-to-end integration test asserting user creation idempotency and form locking during network failures.
  - Scenario: Error path & recovery. Attempt 1 throws `HttpRequestException("lost")` (simulating dropped response post-commit); attempt 2 returns 422 UnprocessableEntity; attempt 3 succeeds.
  - Production code exercised: `UsersViewModel.CreateUserCommand`, `UsersViewModel.OpenCreateUserCommand`, `UsersViewModel.CloseUserDialogCommand`, `UsersViewModel.SelectUserCommand`, `AppSession.HasUnresolvedOperation`.
  - Assertions: Verifies `HasUnresolvedOperation` is asserted on connection drop; closing the dialog or selecting another user is blocked while operation is unresolved; altering form fields (`NewUserName = "changed"`, `NewUserRole = "owner"`, `NewUserPin = "bad"`, `pos = false`) does not contaminate the replayed request body; all 3 payloads sent to the server are identical (`bodies[0] == bodies[1] == bodies[2]`); upon confirmation, dialog closes and PIN fields are sanitized to empty strings.
  - Gaps: Edge case of server returning conflict (409) due to duplicate username is not exercised here; concurrent creation requests on different tabs/threads not simulated.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:29:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:29:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [29-35]
- **Behavior:** Test mock handler for user creation: records serialized request bodies into `bodies` collection. Throws `HttpRequestException("lost")` on 1st request, returns 422 UnprocessableEntity on 2nd, and returns 200 OK with `CreateUserResult("created", true)` on 3rd request.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:39:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:39:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [39-39]
- **Behavior:** Inline expression lambda 'vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = true;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:46:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:46:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [46-46]
- **Behavior:** Inline expression lambda 'vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = false;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:50:59:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:50:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [50-50]
- **Behavior:** Inline assertion predicate `body => Assert.Equal(bodies[0], body)` verifying that every captured HTTP payload matches the initial request byte-for-byte.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:55:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Existing_user_writes_retry_the_original_decision_and_target(bool)
- **Lines:** [55-78]
- **Behavior:** Theory testing write safety for existing user operations (parameterized by `permissions`: `true` tests `SavePermissionsCommand`, `false` tests `ToggleActiveCommand`).
  - Scenario: Error path & recovery. The initial mutation experiences a connection loss (`HttpRequestException`), followed by a successful retry via `RetryUserActionCommand`.
  - Production code exercised: `UsersViewModel.SavePermissionsCommand`, `UsersViewModel.ToggleActiveCommand`, `UsersViewModel.RetryUserActionCommand`, `UsersViewModel.SelectUserCommand`, `UsersViewModel.OpenCreateUserCommand`.
  - Assertions: Verifies `HasUnresolvedOperation` is flagged upon network drop; switching target users or opening create dialog while an action is unresolved is blocked; modifying permission toggles in UI while unresolved does not change retry payload; retry resubmits exact original payload (`bodies[0] == bodies[1]`); `HasUnresolvedOperation` resets on completion.
  - Gaps: Missing test for concurrent status deactivation while the target user has an active cashier shift; missing test for role modification edge cases.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:61:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:61:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [61-66]
- **Behavior:** Test mock handler for user update actions: records serialized request bodies into `bodies`. Throws `HttpRequestException("lost")` on the first call, and returns `HttpStatusCode.OK` on subsequent calls.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:68:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:68:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [68-68]
- **Behavior:** Inline expression lambda 'vm.Permissions.Single(p => p.Key == "pos").IsGranted = false;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:74:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:74:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** Inline expression lambda 'vm.Permissions.Single(p => p.Key == "pos").IsGranted = true;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Lost_self_pin_confirmation_is_reconciled_to_the_same_user_without_rewriting_the_pin()
- **Lines:** [80-106]
- **Behavior:** Asserts self PIN change recovery and reconciliation via PIN authentication probe.
  - Scenario: Error path & probe reconciliation. The write mutation (`ChangePinCommand`) throws `HttpRequestException("committed, confirmation lost")`. The client recovers on retry by testing the new PIN against `/api/auth/login-by-pin`.
  - Production code exercised: `UsersViewModel.OpenPinChangeCommand`, `UsersViewModel.ChangePinCommand`, `UsersViewModel.RetryUserActionCommand`, `UsersViewModel.CloseUserDialogCommand`, `AppSession.CurrentUser`.
  - Assertions: Verifies `HasUnresolvedOperation` is true and `IsPinChangeOpen` remains true (modal close blocked); modifying UI fields to invalid/different PINs (`CurrentPin = "changed"`, `NewPin = "bad"`) does not alter reconciliation credentials; retry dispatches 1 write and 1 proof request; login probe receives the original new PIN ("2468"); session reconciles to `Owner.Id`; PIN text properties are cleared from memory.
  - Gaps: Does not test reconciliation failure if server rejects the new PIN on login-by-pin (e.g. write actually aborted on database deadlock).
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:84:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:84:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-93]
- **Behavior:** Test mock handler for self PIN change: intercepts `/api/auth/login-by-pin` reconciliation requests, asserts the proof PIN is `"2468"`, and returns `UserSummaryDto`. For mutation requests, increments `writes` counter and throws `HttpRequestException`.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:108:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:108:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.A_pin_proof_for_a_different_user_does_not_confirm_the_pending_change()
- **Lines:** [108-125]
- **Behavior:** Asserts security defense against cross-user identity confusion during PIN change reconciliation.
  - Scenario: Security boundary & error path. A PIN proof request accidentally or maliciously authenticates as a different user (`Target` instead of `Owner`).
  - Production code exercised: `UsersViewModel.ChangePinCommand`, `UsersViewModel.RetryUserActionCommand`, identity reconciliation guard in `AppSession`.
  - Assertions: Verifies `HasUnresolvedOperation` remains true; `IsPinChangeOpen` remains true; mutation is retried against the server rather than assuming success; both write attempts send identical payloads; `CurrentUser.Id` remains `Owner.Id`.
  - Gaps: Missing test for server returning 401 Unauthorized or token revocation during reconciliation.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:112:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:112:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [112-118]
- **Behavior:** Test mock handler for mismatched user reconciliation: returns `UserSummaryDto` for `Target` when `/api/auth/login-by-pin` is queried; records write bodies, throwing on 1st write attempt and returning 422 UnprocessableEntity on 2nd.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:127:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:127:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Administrative_pin_reset_cannot_target_the_owner_and_creation_cannot_grant_unheld_permissions()
- **Lines:** [127-140]
- **Behavior:** Asserts privilege escalation guardrails in `UsersViewModel`.
  - Scenario: Negative security test. A user with `manager` role holding `users_manage` and `pos` attempts to (1) open PIN change against the owner, and (2) grant `all` permissions to a new user.
  - Production code exercised: `UsersViewModel.OpenPinChangeCommand`, `UsersViewModel.OpenCreateUserCommand`, `UsersViewModel.CreateUserCommand`.
  - Assertions: Verifies `OpenPinChangeCommand` is rejected (`IsPinChangeOpen == false`); attempting to create a user with permissions not possessed by the actor is blocked client-side without dispatching HTTP requests (`writes == 0`); sets `vm.ActionError`.
  - Gaps: Does not verify that the server-side API also rejects unheld permissions if the client-side check is bypassed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:131:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:131:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [131-131]
- **Behavior:** Test mock handler tracking mutation requests: increments `writes` counter and returns `HttpStatusCode.OK`. Used to assert that zero writes are sent to the network.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:137:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:137:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [137-137]
- **Behavior:** Inline expression lambda 'vm.NewUserPermissions.Single(p => p.Key == "all").IsGranted = true;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
