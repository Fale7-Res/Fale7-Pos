# src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`
- **Component:** `Falah.Infrastructure.Users.UpdateUserPermissionsHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `796cc8d48d0b1224441c6028cd98cbdd10abae53896a4b591ce04c86e146fe55`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 7
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`UpdateUserPermissionsHandler` executes administrative permission modifications for existing user accounts in Fale7 POS. It handles `POST /api/users/{id}/permissions`, applying explicit grant and revoke deltas while enforcing domain immutability, privilege escalation prevention, and system survival guarantees.

### Key Security & Transactional Mechanisms:
1. **Serializable Transaction Isolation:** Initiates an explicit database transaction with `System.Data.IsolationLevel.Serializable` to guarantee that concurrent permission revocations across multiple administrators cannot race past `AssertUserAdministrationSurvives`.
2. **Owner Account Immutability:** Invokes `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تعديل صلاحيات")` to block modification of the owner account's privileges.
3. **Privilege Escalation Containment:** Calls `UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, grant)` to prevent administrators from granting permissions they do not possess.
4. **Surviving Administrator Guarantee:** If the target user would lose `users_manage` capability, the handler counts other active administrators in the database and calls `AssertUserAdministrationSurvives`.
5. **Critical Vulnerability Grounding (SEC-001):** The handler trusts `request.ActorUserId` directly from the client request DTO (`_db.Users.FirstOrDefaultAsync(u => u.Id == request.ActorUserId)`). Bypassing `FindActorMismatchAsync` via PascalCase casing enables unprivileged users to execute this handler under administrator identity.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-UP-001` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:22:5:ConstructorDeclaration` | [22-26](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L22-L26) | Constructor (DI Hook) |
| `CAL-UP-002` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28:5:MethodDeclaration` | [28-75](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L28-L75) | Substantive (Security Boundary) |
| `CAL-UP-003` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:32:76:SimpleLambdaExpression` | [32](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L32) | Lambda Predicate |
| `CAL-UP-004` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:37:58:SimpleLambdaExpression` | [37](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L37) | Lambda Predicate |
| `CAL-UP-005` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:57:24:SimpleLambdaExpression` | [57](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L57) | Lambda Predicate |
| `CAL-UP-006` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:58:25:SimpleLambdaExpression` | [58](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L58) | Lambda Projection |
| `CAL-UP-007` | `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:62:46:SimpleLambdaExpression` | [62](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L62) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:22:5:ConstructorDeclaration
- **Exact Line Range:** Lines 22–26
- **Signature:** `public UpdateUserPermissionsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Actual Behavior & Inputs:** Captures `FalahDbContext` for persistence and transactions, and `IApprovalRegistry` for `users_manage` evaluation.

---

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28:5:MethodDeclaration
- **Exact Line Range:** Lines 28–75
- **Signature:** `public async Task HandleAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. **Serializable Transaction:** Begins transaction with `IsolationLevel.Serializable`.
  2. **Actor Resolution:** Loads `actorUser` using untrusted `request.ActorUserId`. Verifies `users_manage` permission via `AuthorizationPolicy.AuthorizeCommand`.
  3. **Target Verification:** Queries `target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)`.
  4. **Owner Protection:** Evaluates `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تعديل صلاحيات")`.
  5. **Catalog & Escalation Checks:** Validates permission keys against `PermissionCatalog`, and asserts `UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, grant)`.
  6. **Permission Mutation:** Computes `current` set by unioning `grant` and removing `revoke`.
  7. **Admin Survival Verification:** If target no longer satisfies `CanManageUsers(current)`, queries all other active users and counts how many retain user management authority. Enforces `AssertUserAdministrationSurvives(remainingCount)`.
  8. **Audit & Commit:** Writes `ControlAuditEventEntity` (`Action = "permissions_changed"`), saves changes, and commits transaction.
- **VULNERABILITY FORENSICS (SEC-001):**
  - The handler contains zero references to `HttpContext` or `context.ActorId()`.
  - An attacker sending `{"ActorUserId": "<owner-id>", "Grant": ["all"], ...}` in PascalCase bypasses middleware checks, loads the owner account as `actorUser`, and grants `"all"` to their own account.
- **Test Coverage:** Tested in `UserManagementVerticalSliceTests.Updating_permissions_fails_when_granting_unheld_permissions`.

---

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:32:76:SimpleLambdaExpression
- **Exact Line Range:** Line 32
- **Signature:** `u => u.Id == request.ActorUserId`
- **Actual Behavior:** LINQ expression locating the acting user record.

---

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:37:58:SimpleLambdaExpression
- **Exact Line Range:** Line 37
- **Signature:** `u => u.Id == request.UserId`
- **Actual Behavior:** LINQ expression locating the target user record to be modified.

---

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:57:24:SimpleLambdaExpression
- **Exact Line Range:** Line 57
- **Signature:** `u => u.Active && u.Id != target.Id`
- **Actual Behavior:** LINQ expression filtering candidate administrators to other active users excluding the target.

---

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:58:25:SimpleLambdaExpression
- **Exact Line Range:** Line 58
- **Signature:** `u => u.Permissions`
- **Actual Behavior:** LINQ expression projecting the comma-separated permissions string for SQL evaluation.

---

### Callable: src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:62:46:SimpleLambdaExpression
- **Exact Line Range:** Line 62
- **Signature:** `permissions => UserManagementRules.CanManageUsers(permissions.Split(...))`
- **Actual Behavior:** In-memory LINQ predicate parsing permissions to determine if an administrator retains `users_manage` or `"all"`.

---

## 4. Verification & Recommendations

1. **Eliminate SEC-001 Exposure:** Ensure caller identity is derived from verified server session (`context.ActorId()`), preventing identity injection.
2. **Static Verification:** Verified complete against `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `796cc8d48d0b1224441c6028cd98cbdd10abae53896a4b591ce04c86e146fe55`
- **Callable ID Checked:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:22:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28:5:MethodDeclaration`, `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:32:76:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:37:58:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:57:24:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:58:25:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:62:46:SimpleLambdaExpression` (7 of 7 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs` (77 lines).
  2. Verified transactional concurrency: Line 31 begins transaction with `IsolationLevel.Serializable`, preventing race conditions during concurrent administrator privilege revocations.
  3. Confirmed interaction with `SEC-001`: Line 32 resolves the acting administrator directly from untrusted `request.ActorUserId`. When combined with the case-sensitivity bypass in `AuthenticationMiddleware`, this allows an unprivileged caller to execute permission changes under an administrator identity.
  4. Verified owner protection and surviving administrator check: `AssertTargetIsNotOwner` (line 39) and `AssertUserAdministrationSurvives` (line 61) prevent stripping privileges from the owner or revoking the last administrator.
  5. All 7 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `796cc8d48d0b1224441c6028cd98cbdd10abae53896a4b591ce04c86e146fe55`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `796cc8d48d0b1224441c6028cd98cbdd10abae53896a4b591ce04c86e146fe55` against `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs` (77 lines).
2. Concur with L1 on `IsolationLevel.Serializable` and interaction with `SEC-001`.
3. Additional L2 Challenger Assessment:
   - Line 32 binds `actorUser` from `request.ActorUserId`. When combined with PascalCase casing bypass in `AuthenticationMiddleware`, an unprivileged user can impersonate an administrator and escalate their own privileges.
   - However, within the handler domain, `AssertTargetIsNotOwner` (line 39) blocks tampering with owner permissions, and `AssertUserAdministrationSurvives` (line 61) prevents revoking the last remaining administrator under `Serializable` transaction isolation.
4. All 7 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
