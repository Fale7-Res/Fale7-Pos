# Security and hidden-behavior audit — partial manual continuation

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. **IN PROGRESS**. This is not a complete repository security verdict. The plugin launch is blocked; ordinary static review and independent challenges continue. No application fixes or live security mutations were performed.

## SEC-001 — Actor identity check and DTO binding disagree on JSON property casing

Severity: **HIGH**. Confidence: **Confirmed parser/middleware mechanism and independently corroborated source path; full live privilege mutation NOT EXECUTED**. Preconditions: an authenticated low-privilege user, a known active sufficiently privileged user ID, and an affected endpoint that authorizes a body-selected actor. No manager PIN or existing privileged bearer is assumed known. Not an anonymous authentication bypass.

Entry-to-sink evidence:

- `src/Falah.Server/AuthenticationMiddleware.cs`, `FindActorMismatchAsync`: JsonElement.TryGetProperty checks only exact lower-camel-case ActorFields names. `InvokeAsync` still resolves a real session and puts its user in HttpContext.Items; this does not rewrite the DTO actor.
- `src/Falah.Application/Users/UserManagementRequests.cs`, `UpdateUserPermissionsRequest`: contains ActorUserId supplied in the body.
- `src/Falah.Server/Program.cs:1951–1957`, permissions endpoint: changes only target UserId, forwards the actor field unchanged to the handler.
- `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28–44`: loads actor by request.ActorUserId and authorizes that row's permissions, rather than the session identity. It then correctly restricts grants to that selected actor's permissions, which does not protect against selecting a different privileged actor.
- Independent reviewer found no repository server JSON override, global endpoint filter or used ActorId() accessor that repairs this mismatch. Exact source references/counterarguments: [independent challenge](../../Sub-Agents/Handoffs/security-casing-challenge.md).

Safe verification actually executed: [harness source](../../Verification-Tools/SecurityHarness/Program.cs) links the baseline middleware and baseline UserManagementRequests.cs unchanged. Using real Microsoft.AspNetCore.Http.Json.JsonOptions on net8.0, the normal `actorUserId` mismatch returns 403; alternate property casing passes to the delegate and binds the privileged placeholder actor. Three branch assertions pass. SessionService and user objects are explicit stubs; the real permissions handler and database are **not** invoked. [Saved result](middleware-case-harness-results.json).

Impact: the traced path can evaluate a privileged actor's authorization while the actual bearer belongs to a restricted user. Expected consequences include unauthorized grants to a non-owner target and incorrect audit attribution, within handler-specific constraints. Broader affected routes must be enumerated individually. Active-session verification, owner immutability and no-grant-beyond-actor checks remain real counterevidence against broader claims; they do not bind the selected actor to the bearer.

Recommended solution, not implemented: derive actor identity exclusively from authenticated context at command boundaries; remove client authority over actor selection. A temporary case-insensitive comparison must also reject duplicate/colliding actor keys and all model-binding forms, but structural server-owned identity is stronger. Required regression: full Minimal API pipeline tests with restricted real test identity and separately privileged fixture actor, all casing variants, duplicate JSON keys, missing/null/nested fields, and valid same-actor requests. Any database-backed reproduction needs an explicitly approved disposable harness, not the owner's active test database.

## UI-020 cross-check — PIN retry may replace transport identity

Existing canonical UI ID is retained, avoiding a duplicate security count. Parent independently read `UsersViewModel.Actions.cs:147–178` and `ApiClient.cs:157–171`: recovered self-change proof calls ordinary LoginByPinAsync, which overwrites the shared bearer before checking whether returned identity matches the pending request. AppSession identity is not updated here. A process-wide stable retained-request key can outlive user/session changes.

Confirmed side effect: a successful proof login replaces HttpClient authorization even when later UI logic does not intend a login transition. Security impact beyond identity desynchronization depends on which user's pending operation is recovered and middleware checks; do not claim an arbitrary account takeover from this alone. No real PIN, login or runtime request was used. Recommend a non-session-mutating proof endpoint/transport or an explicit authenticated recovery flow, identity-scoped pending metadata, and fake-HTTP tests that assert both bearer and AppSession identities remain aligned.

## SEC-002 candidate — PIN-bearing durable request payloads are written as plain JSON

Severity: MEDIUM candidate; confidence high for source serialization, deployment exposure unverified. `UsersViewModel.Actions.cs:13–14` supplies stable durable keys for CreateUserRequest/ChangePinRequest. `RetainedRequest.cs:33–40` saves before send. `RetainedRequestStore.cs:59–74` JSON serializes the entire request and writes it under per-station LocalApplicationData. The DTO includes initial/current/new PIN fields. No cryptographic protection or credential exclusion appears in that serialization path.

Impact requires local file access or backup/collection of the journal while it exists; Windows profile ACLs are relevant protection, and known outcomes clear the record. Ambiguous outcomes can retain it. No actual pending file, credential value or runtime log was read. Recommend a credential-free recovery protocol, minimizing retention and approved per-user protected storage where unavoidable. Test that credential values never occur in journal bytes and verify ambiguous recovery without persisting authentication secrets. Do not upload or inspect live journal contents as a shortcut.

## UI lock lifecycle observation — not yet a complete vulnerability finding

Cashier MainViewModel.Lock at82 and CallCenter MainViewModel.Lock at57 return to a login view without calling ApiClient.LogoutAsync. A server logout implementation exists. This proves local presentation does not itself revoke the old bearer; it does not prove that an unauthenticated person can invoke hidden UI commands. Further callback/session-generation and actual lock-control analysis remains required. No old bearer was collected or replayed.

## Unfinished security coverage

No exhaustive verdict yet on SQL injection, authorization across every endpoint, session expiry/revocation and PIN hashing, manager approvals, LAN transport, backup/restore permissions, malicious timers, hidden startup behavior, native interop, dependency compromise or exfiltration. Search hits and file inventories will be labelled unreviewed until traced. No claim that the system is secure or contains a deliberate backdoor is justified. Existing financial/printing findings remain separately classified; full master deduplication and mandatory evidence schema validation are pending.
