# src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`
- **Component:** `Falah.Infrastructure.Users.UpdateUserPermissionsHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`UpdateUserPermissionsHandler` processes requests to alter permission assignments for existing users in Fale7 POS. It encapsulates domain security rules:
- **Owner Account Immutability:** Precludes modification of the `owner` account permissions under any circumstance ([UserManagementRules.AssertTargetIsNotOwner](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L20-L24)).
- **Privilege Escalation Guard:** Guarantees that an administrator cannot grant permissions that they do not personally hold ([UserManagementRules.AssertNoPrivilegeEscalation](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L29-L35)). Note: Revoking permissions is deliberately unrestricted by escalation checks.
- **Administrative Lockout Prevention:** Enforces that revoking `users_manage` or `"all"` cannot eliminate the last active administrator in the restaurant ([UserManagementRules.AssertUserAdministrationSurvives](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs#L57-L63)).
- **Transactional Integrity:** Enforces `System.Data.IsolationLevel.Serializable` to guarantee phantom-free survivor verification under concurrent administrative requests.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-UUP-001` | Constructor | `public UpdateUserPermissionsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)` | 22-26 | Substantive (DI Hook) |
| `CAL-UUP-002` | `HandleAsync` | `public async Task HandleAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default)` | 28-75 | Substantive (Security & Transaction Core) |
| `CAL-UUP-003` | `HandleAsync::<lambda_actor>` | `u => u.Id == request.ActorUserId` | 32 | Generated / Linq Expression |
| `CAL-UUP-004` | `HandleAsync::<lambda_target>` | `u => u.Id == request.UserId` | 37 | Generated / Linq Expression |
| `CAL-UUP-005` | `HandleAsync::<lambda_survivor_filter>` | `u => u.Active && u.Id != target.Id` | 57 | Generated / Linq Expression |
| `CAL-UUP-006` | `HandleAsync::<lambda_survivor_select>` | `u => u.Permissions` | 58 | Generated / Linq Expression |
| `CAL-UUP-007` | `HandleAsync::<lambda_survivor_count>` | `permissions => UserManagementRules.CanManageUsers(...)` | 62 | Generated / Linq Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-UUP-001
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:22:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 22–26
- **Signature:** `UpdateUserPermissionsHandler(FalahDbContext, IApprovalRegistry)`
- **Linked Findings:** `SEC-001`: Constructor `UpdateUserPermissionsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Line Span:** [22-26](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L22-L26)
- **Actual Behavior & Inputs:** Stores injected EF Core database context `db` and manager approval registry `approvalRegistry` into private fields `_db` and `_approvalRegistry`.
- **Callers & Dependencies:** Injected by ASP.NET Core DI in Minimal API route `POST /api/users/{id}/permissions` ([Program.cs#L1951](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1951)).
- **Side Effects:** Binds dependencies to request scope.
- **Classification Justification:** Substantive dependency injection setup.

---

### CAL-UUP-002
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28:5:MethodDeclaration`
- **Exact Line Range:** Lines 28–75
- **Signature:** `HandleAsync(UpdateUserPermissionsRequest, CancellationToken)`
- **Linked Findings:** `SEC-001`: `HandleAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default)`
- **Line Span:** [28-75](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L28-L75)
- **Actual Behavior & Execution Flow:**
  1. **Transaction Initialization:** Begins a database transaction with `System.Data.IsolationLevel.Serializable` to lock the `Users` table and prevent phantom insertions or concurrent revocations from miscounting surviving administrators.
  2. **Actor Resolution:** Queries `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)`. Throws `InvalidOperationException("المستخدم غير موجود.")` if not found.
  3. **Authorization Check:** Maps `actorUser` to domain `Actor` and evaluates `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage")`. Throws `DomainException` if actor lacks `users_manage` or `"all"`.
  4. **Target Resolution:** Queries `_db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)`. Throws `InvalidOperationException("المستخدم المستهدف غير موجود.")` if missing.
  5. **Owner Protection Invariant:** Invokes `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تعديل صلاحيات")`. Throws `DomainException` if target account is the owner.
  6. **Input Normalization & Escalation Check:**
     - De-duplicates `Grant` and `Revoke` collections via `.Distinct().ToList()`.
     - Validates that all granted and revoked permission keys exist in `PermissionCatalog.Keys` (`AssertKnownPermissions`).
     - Evaluates `UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, grant)`: ensures the actor possesses every permission being granted (or holds `"all"`).
  7. **Set Operations:**
     - Converts current comma-delimited `target.Permissions` string into a `HashSet<string>`.
     - Executes `UnionWith(grant)` and `ExceptWith(revoke)`.
  8. **Surviving Administrator Assertion:**
     - If the mutated permission set for `target` no longer contains `users_manage` or `"all"`:
       - Queries all other active users in the database (`u.Active && u.Id != target.Id`), projecting `u.Permissions`.
       - Computes how many of those accounts retain administrative authority.
       - Invokes `UserManagementRules.AssertUserAdministrationSurvives(count)`. Throws `DomainException` if count <= 0.
  9. **State Mutation & Audit:**
     - Updates `target.Permissions = string.Join(",", current)`.
     - Appends `ControlAuditEventEntity` (`Action = "permissions_changed"`, `Actor = actorUser.Name`, `EntityId = target.Id`, `Reason = $"منح: [{...}] - سحب: [{...}]"`).
  10. **Commit:** Persists changes via `await _db.SaveChangesAsync(ct)` and commits `await transaction.CommitAsync(ct)`.
- **Callers & Dependencies:**
  - Invoked exclusively by Minimal API endpoint `POST /api/users/{id}/permissions` ([Program.cs#L1951-L1958](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1951-L1958)).
  - Dependencies: `AuthorizationPolicy`, `UserManagementRules`, `OrderMapper`, `_db.Users`, `_db.ControlAuditEvents`.
- **Side Effects:**
  - Updates `target.Permissions` column in SQL database.
  - Inserts row into `ControlAuditEvents` table.
- **Trust Boundaries & Security Forensics (SEC-001):**
  - **The Wire Identity Flaw:** The handler reads `request.ActorUserId` directly from the request model.
  - **Casing Bypass Mechanism:**
    1. In `AuthenticationMiddleware.cs#L114`, `document.RootElement.TryGetProperty("actorUserId", out _)` performs an exact ordinal case-sensitive lookup.
    2. A client with any valid bearer token submits a JSON payload with `"ActorUserId": "usr-admin"` (PascalCase).
    3. The middleware check returns `null` (mismatch check bypassed).
    4. Minimal API parameter binding executes with default `PropertyNameCaseInsensitive = true`, populating `request.ActorUserId` with `"usr-admin"`.
    5. In `UpdateUserPermissionsHandler.cs#L32-L35`, `actorUser` resolves to the administrator account.
    6. `AuthorizationPolicy.AuthorizeCommand` passes.
    7. `AssertNoPrivilegeEscalation` compares the requested `grant` permissions against `"usr-admin"`'s permissions (which includes `"all"`), successfully bypassing escalation barriers.
    8. **Impact:** An unprivileged cashier account can grant itself `"all"` or `"users_manage"`.
    9. **Audit Trail Forgery:** Line 70 sets `Actor = actorUser.Name`. The resulting audit event records that the administrator changed the permissions, leaving zero forensic trace of the cashier in `ControlAuditEvents`.
- **Concurrency & Failure Modes:**
  - The `Serializable` isolation level prevents concurrent race conditions where two administrators revoke each other's permissions simultaneously. Under SQL Server, serializable locks prevent dirty reads and phantom rows; if a race occurs, one transaction wins and the other either encounters a deadlock exception (handled by caller retry) or aborts when survivor validation fails.
- **Test Coverage:**
  - *Covered:* `UserManagementVerticalSliceTests.Actor_cannot_grant_permissions_they_do_not_hold`, `UserManagementVerticalSliceTests.Revoking_users_manage_from_last_administrator_is_refused`, `UserManagementVerticalSliceTests.Owner_permissions_cannot_be_modified`.
  - *Missing Tests:* Missing test asserting that supplying PascalCase `"ActorUserId"` or uppercase `"ACTORUSERID"` fails with 403 Forbidden at the middleware layer; missing test for concurrent serializable transaction conflict recovery.

---

### CAL-UUP-003
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:32:76:SimpleLambdaExpression`
- **Exact Line Range:** Lines 32–32
- **Signature:** `u => u.Id == request.ActorUserId`
- **Linked Findings:** `SEC-001` to CAL-UUP-007: Expression Lambdas & Delegates
- **CAL-UUP-003 (`u => u.Id == request.ActorUserId`):** Primary key lookup for actor record.
- **CAL-UUP-004 (`u => u.Id == request.UserId`):** Entity lookup for target record being updated.
- **CAL-UUP-005 (`u => u.Active && u.Id != target.Id`):** Filter querying active users excluding the target user during survivor verification.
- **CAL-UUP-006 (`u => u.Permissions`):** Projection selecting only the `Permissions` string column from SQL Server.
- **CAL-UUP-007 (`permissions => UserManagementRules.CanManageUsers(...)`):** In-memory delegate counting active survivors.
- **Classification Justification:** Generated LINQ expressions and delegate predicates.

---

## 4. Verification & Recommendations

1. **Remediation for SEC-001:** Remove `ActorUserId` from `UpdateUserPermissionsRequest` or overwrite it at the endpoint layer with `context.ActorId()` before invoking `HandleAsync`.
2. **Audit Logging Fix:** Pass `context.ActorId()` or authenticated session identity directly into the handler to ensure the real actor is recorded in `ControlAuditEventEntity`.
3. **Verification:** Static source audit confirmed; no application source modified.

- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:37:58:SimpleLambdaExpression` (Lines 37–37, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:57:24:SimpleLambdaExpression` (Lines 57–57, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:58:25:SimpleLambdaExpression` (Lines 58–58, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:62:46:SimpleLambdaExpression` (Lines 62–63, `SimpleLambdaExpression`)