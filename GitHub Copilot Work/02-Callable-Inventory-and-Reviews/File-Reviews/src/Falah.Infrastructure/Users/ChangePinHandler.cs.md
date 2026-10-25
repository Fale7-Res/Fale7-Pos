# src/Falah.Infrastructure/Users/ChangePinHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/ChangePinHandler.cs`
- **Component:** `Falah.Infrastructure.Users.ChangePinHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** Security and Authorization Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural Role & Responsibilities

`ChangePinHandler` executes PIN credential rotation in Fale7 POS. It supports two distinct operational flows:
1. **Self-Service PIN Change (`ActorUserId == UserId`):** A user updates their own PIN by providing their `CurrentPin`. Authentication relies on PBKDF2 verification against `target.PinHash` without requiring administrative permissions.
2. **Administrative PIN Reset (`ActorUserId != UserId`):** An administrator resets another user's PIN without knowing the existing PIN. Gated strictly on `users_manage` permissions, and unconditionally refuses resets against the `owner` account ([ChangePinHandler.cs#L47](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L47)).

---

## 2. Comprehensive Callable Inventory

| Callable ID | Name | Signature | Line Span | Classification |
| :--- | :--- | :--- | :--- | :--- |
| `CAL-CP-001` | Constructor | `public ChangePinHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)` | 24-28 | Substantive (DI Hook) |
| `CAL-CP-002` | `HandleAsync` | `public async Task HandleAsync(ChangePinRequest request, CancellationToken ct = default)` | 30-65 | Substantive (Security & Credential Core) |
| `CAL-CP-003` | `HandleAsync::<lambda_target>` | `u => u.Id == request.UserId` | 32 | Generated / Linq Expression |
| `CAL-CP-004` | `HandleAsync::<lambda_actor>` | `u => u.Id == request.ActorUserId` | 44 | Generated / Linq Expression |

---

## 3. Deep Source-Grounded Callable Analysis

### CAL-CP-001: Constructor `ChangePinHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Line Span:** [24-28](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L24-L28)
- **Actual Behavior & Inputs:** Captures `FalahDbContext` for persistence and `IApprovalRegistry` for manager authorization checks.
- **Callers & Dependencies:** Injected via ASP.NET Core DI in Minimal API endpoint `POST /api/users/{id}/change-pin` ([Program.cs#L1933](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1933)).
- **Classification Justification:** Substantive dependency injection setup.

---

### CAL-CP-002: `HandleAsync(ChangePinRequest request, CancellationToken ct = default)`
- **Line Span:** [30-65](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L30-L65)
- **Actual Behavior & Execution Flow:**
  1. **Target Lookup:** Queries `_db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)`. Throws `InvalidOperationException("المستخدم غير موجود.")` if missing.
  2. **Branching Authorization:**
     - **Branch A: Self-Service (`request.ActorUserId == request.UserId`):**
       - Asserts `request.CurrentPin` is not empty.
       - Evaluates `PinHasher.Verify(request.CurrentPin, target.PinHash, target.PinSalt, target.PinIterations)`. Throws `DomainException("رمز PIN الحالي غير صحيح.")` if verification fails.
       - Assigns `actorName = target.Name`.
     - **Branch B: Administrative Reset (`request.ActorUserId != request.UserId`):**
       - Queries `_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)`. Throws `InvalidOperationException("المستخدم غير موجود.")` if missing.
       - Enforces authorization: `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "users_manage")`.
       - Enforces owner immunity: `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ")`. Throws `DomainException` if attempting to reset the owner's PIN.
       - Assigns `actorName = actorUser.Name`.
  3. **PIN Uniqueness Verification:** Executes `await PinUniquenessChecker.AssertNotInUseAsync(_db, request.NewPin, excludeUserId: target.Id, ct)`.
  4. **Cryptographic Hashing:** Executes `var hashed = PinHasher.Hash(request.NewPin)`. Generates random 16-byte salt, 100,000 PBKDF2 iterations, and base64 hash.
  5. **State Mutation:** Updates `target.PinHash`, `target.PinSalt`, and `target.PinIterations`.
  6. **Audit Event Logging:** Appends `ControlAuditEventEntity` (`Action = "pin_changed"`, `Actor = actorName`, `EntityId = target.Id`). Conspicuously and deliberately excludes PIN values from audit data.
  7. **Commit:** Persists changes via `await _db.SaveChangesAsync(ct)`.
- **Callers & Dependencies:**
  - Invoked by Minimal API endpoint `POST /api/users/{id}/change-pin` ([Program.cs#L1933-L1949](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1933-L1949)). Rate-limited by `"login"` IP policy.
  - Dependencies: `PinHasher`, `PinUniquenessChecker`, `AuthorizationPolicy`, `UserManagementRules`, `_db.Users`, `_db.ControlAuditEvents`.
- **Side Effects:**
  - Updates PIN hash/salt/iteration columns in `Users` table.
  - Inserts row into `ControlAuditEvents`.
- **Critical Security Forensics & Deficiencies:**
  - **1. THE PIN CHANGE SESSION INVALIDATION GAP:**
    - Source lines 51-65 update `Users` and `ControlAuditEvents`, but **NEVER touch `_db.UserSessions`**.
    - In `SessionService.ResolveAsync(token)`, validation checks only whether the session token hash exists, is not revoked, is not expired, and `user.Active == true`. It never inspects when the user's PIN was changed.
    - **Confirmed Consequence:** If an employee's PIN is reset because their credentials were stolen or compromised, **all preexisting active bearer tokens across all devices remain valid and usable** until their sliding 24-hour expiration window lapses.
  - **2. SEC-001 PRIVILEGE ESCALATION VULNERABILITY:**
    - The handler relies on `request.ActorUserId`.
    - If a rogue cashier submits a request to `POST /api/users/{targetId}/change-pin` with JSON payload `{"ActorUserId": "usr-admin", "NewPin": "9999"}`, the PascalCase bypasses `AuthenticationMiddleware.FindActorMismatchAsync`.
    - The handler takes Branch B (Administrative Reset), queries the administrator, passes `users_manage` authorization, and overwrites the target user's PIN without knowing their current PIN.
  - **3. Audit Trail Spoofing:**
    - On the admin reset path, `actorName` is set to `actorUser.Name`. Under an SEC-001 exploit, the audit ledger records that the administrator changed the PIN.
- **Test Coverage:**
  - *Covered:* `UserManagementVerticalSliceTests.User_can_change_their_own_pin_with_correct_current_pin`, `UserManagementVerticalSliceTests.Changing_own_pin_fails_with_wrong_current_pin`, `UserManagementVerticalSliceTests.Admin_can_reset_another_users_pin_without_current_pin`, `UserManagementVerticalSliceTests.Admin_cannot_reset_owners_pin`.
  - *Missing Tests:* Missing regression test verifying session revocation on PIN change (currently failing/absent behavior); missing test for SEC-001 PascalCase injection on `/change-pin`.

---

### CAL-CP-003 & CAL-CP-004: Expression Lambdas
- **CAL-CP-003 (`u => u.Id == request.UserId`):** Primary key lookup for target user entity whose PIN is being rotated.
- **CAL-CP-004 (`u => u.Id == request.ActorUserId`):** Primary key lookup for actor user entity executing an administrative reset.
- **Classification Justification:** Generated LINQ expression trees compiled to SQL query filters.

---

## 4. Verification & Recommendations

1. **Remediation for Session Invalidation Gap:**
   In `ChangePinHandler.HandleAsync`, revoke all active sessions for `target.Id`:
   ```csharp
   var activeSessions = await _db.UserSessions
       .Where(s => s.UserId == target.Id && s.RevokedAtUtc == null)
       .ToListAsync(ct);
   foreach (var session in activeSessions)
   {
       session.RevokedAtUtc = DateTimeOffset.UtcNow;
   }
   ```
2. **Remediation for SEC-001:** Overwrite `request.ActorUserId` with `context.ActorId()` at the Minimal API layer in `Program.cs`.
3. **Verification:** Static source audit confirmed; no application source modified.
