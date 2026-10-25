# src/Falah.Infrastructure/Users/SetUserActiveHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs`
- **Component:** `Falah.Infrastructure.Users.SetUserActiveHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`SetUserActiveHandler` handles user account deactivation and reactivation in Fale7 POS. It enforces critical domain invariants to ensure administrative continuity and account security:
- **Owner Account Immunity:** The owner account can never be deactivated or reactivated via standard administrative commands ([SetUserActiveHandler.cs#L38](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L38)).
- **Self-Lockout Prevention:** An administrator cannot deactivate their own user account ([SetUserActiveHandler.cs#L41-L42](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L41-L42)).
- **Surviving Administrator Guarantee:** Prevents deactivation of the last active account holding `users_manage` permissions, preventing permanent till administrative lockouts ([SetUserActiveHandler.cs#L49-L60](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L49-L60)).
- **Transaction Atomicity:** Executes under `System.Data.IsolationLevel.Serializable` to eliminate concurrency race conditions where two administrators deactivate each other simultaneously.
- **Auditability:** Automatically records `user_deactivated` or `user_reactivated` in `_db.ControlAuditEvents`.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-SUA-001` | Constructor | `public SetUserActiveHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)` | 21-25 | Substantive (DI Hook) |
| `CAL-SUA-002` | `HandleAsync` | `public async Task HandleAsync(SetUserActiveRequest request, CancellationToken ct = default)` | 27-70 | Substantive (Security & Transaction Core) |
| `CAL-SUA-003` | `HandleAsync::<lambda_actor_lookup>` | `u => u.Id == request.ActorUserId` | 31 | Generated / Linq Expression |
| `CAL-SUA-004` | `HandleAsync::<lambda_target_lookup>` | `u => u.Id == request.UserId` | 36 | Generated / Linq Expression |
| `CAL-SUA-005` | `HandleAsync::<lambda_other_admins_filter>` | `u => u.Active && u.Id != target.Id` | 53 | Generated / Linq Expression |
| `CAL-SUA-006` | `HandleAsync::<lambda_permissions_selector>` | `u => u.Permissions` | 54 | Generated / Linq Expression |
| `CAL-SUA-007` | `HandleAsync::<lambda_survivor_predicate>` | `permissions => UserManagementRules.CanManageUsers(...)` | 58 | Generated / Linq Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-SUA-001: Constructor `SetUserActiveHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Line Span:** [21-25](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L21-L25)
- **Actual Behavior & Inputs:** Injects `FalahDbContext` and `IApprovalRegistry`.
- **Callers & Dependencies:** Injected via ASP.NET Core DI in Minimal API endpoint `POST /api/users/{id}/active` ([Program.cs#L1915](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1915)).
- **Side Effects:** Stores references in private fields `_db` and `_approvalRegistry`.
- **Classification Justification:** Substantive dependency wiring.

---

### CAL-SUA-002: `HandleAsync(SetUserActiveRequest request, CancellationToken ct = default)`
- **Line Span:** [27-70](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L27-L70)
- **Actual Behavior & Execution Flow:**
  1. **Transaction Initialization:** Begins a database transaction with `System.Data.IsolationLevel.Serializable` to guarantee phantom-free survivor counting.
  2. **Actor Resolution:** Fetches the actor entity from `_db.Users.AsNoTracking()` matching `request.ActorUserId`. Throws `InvalidOperationException("المستخدم غير موجود.")` if not found.
  3. **Authorization Check:** Authorizes `users_manage` via `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage")`.
  4. **Target Resolution:** Fetches the mutable target user row from `_db.Users` matching `request.UserId`. Throws `InvalidOperationException("المستخدم المستهدف غير موجود.")` if missing.
  5. **Owner Protection Check:** Executes `UserManagementRules.AssertTargetIsNotOwner(target.Role, request.Active ? "إعادة تفعيل" : "تعطيل")`. Throws `DomainException` if target has role `owner`.
  6. **Self-Deactivation Check:** If `request.UserId == request.ActorUserId && !request.Active`, throws `DomainException("لا يمكنك تعطيل حسابك الخاص.")`.
  7. **Idempotency Early Return:** If `target.Active == request.Active`, immediately returns without performing database writes or audit logging (idempotent no-op).
  8. **Surviving Administrator Verification:** If deactivating an active user holding administrative capability (`UserManagementRules.CanManageUsers(target.Permissions)`):
     - Queries all other active users in the database: `_db.Users.AsNoTracking().Where(u => u.Active && u.Id != target.Id).Select(u => u.Permissions).ToListAsync(ct)`.
     - Counts how many of those users retain `users_manage` or `"all"`.
     - Asserts `UserManagementRules.AssertUserAdministrationSurvives(count)`: throws `DomainException` if remaining administrator count is <= 0.
  9. **Mutation & Audit:**
     - Sets `target.Active = request.Active`.
     - Creates `ControlAuditEventEntity` (`Action = request.Active ? "user_reactivated" : "user_deactivated"`, `Actor = actorUser.Name`, `EntityId = target.Id`, `Reason = request.Reason`).
     - Appends audit entity to `_db.ControlAuditEvents`.
  10. **Commit:** Commits transaction via `_db.SaveChangesAsync(ct)` and `transaction.CommitAsync(ct)`.
- **Callers & Dependencies:**
  - Invoked by Minimal API route `POST /api/users/{id}/active` ([Program.cs#L1915](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1915)).
  - Dependencies: `AuthorizationPolicy`, `UserManagementRules`, `OrderMapper`, `_db.Users`, `_db.ControlAuditEvents`.
- **Side Effects:**
  - Mutates `target.Active` flag in the SQL database.
  - Inserts row into `ControlAuditEvents` table.
  - Deactivating a user immediately invalidates all their active sessions via `SessionService.ResolveAsync` checks.
- **Trust Boundaries & Security Vulnerabilities:**
  - **CRITICAL VULNERABILITY (SEC-001):** The handler loads actor identity from `request.ActorUserId` instead of `context.ActorId()`. An authenticated attacker using PascalCase `"ActorUserId": "usr-admin"` bypasses `AuthenticationMiddleware.FindActorMismatchAsync`. Furthermore:
    - Because `request.ActorUserId` is set to the admin's ID while `request.UserId` is the attacker's ID, the self-deactivation guard `request.UserId == request.ActorUserId` evaluates to `false`!
    - An unprivileged user can deactivate administrators (provided at least one other administrator survives).
  - **Audit Trail Spoofing:** `ControlAuditEventEntity.Actor = actorUser.Name` records the name of the spoofed actor, masking the actual user initiating the deactivation.
- **Concurrency & Failure Modes:**
  - The `Serializable` transaction level prevents concurrent race conditions where two administrators simultaneously attempt to deactivate each other. Under SQL Server, key-range and table locks ensure one transaction commits while the second either blocks and fails survivor validation, or aborts with a deadlock victim error.
- **Test Coverage:**
  - *Covered:* `UserManagementVerticalSliceTests.Deactivating_the_owner_is_rejected`, `UserManagementVerticalSliceTests.An_admin_cannot_deactivate_their_own_account`, `UserManagementVerticalSliceTests.Deactivating_the_last_administrator_is_refused`, `UserManagementVerticalSliceTests.Deactivating_a_user_revokes_their_login_immediately`, `UserManagementVerticalSliceTests.Reactivating_a_deactivated_user_allows_them_to_log_in_again`.
  - *Missing Tests:* Missing test verifying that serializable transaction aborts/retries under concurrent lock contention without corrupting survivor counts; missing test for SEC-001 exploit attempt.

---

### CAL-SUA-003: `HandleAsync::<lambda_actor_lookup>` (`u => u.Id == request.ActorUserId`)
- **Line:** [31](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L31)
- **Actual Behavior & Inputs:** Expression predicate for `FirstOrDefaultAsync` resolving the acting user from the untrusted request model.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-SUA-004: `HandleAsync::<lambda_target_lookup>` (`u => u.Id == request.UserId`)
- **Line:** [36](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L36)
- **Actual Behavior & Inputs:** Expression predicate for `FirstOrDefaultAsync` resolving the tracked target user entity.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-SUA-005: `HandleAsync::<lambda_other_admins_filter>` (`u => u.Active && u.Id != target.Id`)
- **Line:** [53](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L53)
- **Actual Behavior & Inputs:** Expression predicate for `Where` selecting active users whose ID is not the target user being deactivated.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL query filter.

---

### CAL-SUA-006: `HandleAsync::<lambda_permissions_selector>` (`u => u.Permissions`)
- **Line:** [54](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L54)
- **Actual Behavior & Inputs:** Expression projection for `Select` extracting only the permissions column to minimize data transfer from SQL Server.
- **Classification Justification:** Generated LINQ expression tree compiled to SQL column projection.

---

### CAL-SUA-007: `HandleAsync::<lambda_survivor_predicate>` (`permissions => UserManagementRules.CanManageUsers(...)`)
- **Line:** [58](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs#L58)
- **Actual Behavior & Inputs:** In-memory LINQ predicate evaluating whether each candidate user's parsed permission set contains `"all"` or `UserManagementPermission` (`"users_manage"`).
- **Classification Justification:** In-memory delegate function.

---

## 4. Verification & Recommendations

1. **Remediation for SEC-001:** Enforce that `request.ActorUserId` is derived strictly from the session context (`context.ActorId()`) in `Program.cs`.
2. **Remediation for Audit Identity:** Capture and log the authenticated user ID directly from `HttpContext` rather than the body DTO.
3. **Verification:** Static source audit confirmed; no application source modified.
