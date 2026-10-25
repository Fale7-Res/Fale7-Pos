# src/Falah.Infrastructure/Users/ChangePinHandler.cs

- **Path:** `src/Falah.Infrastructure/Users/ChangePinHandler.cs`
- **Component:** `Falah.Infrastructure.Users.ChangePinHandler`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `92efa770a8ddbc53fe25ffb287a3d6344c052ad2cf863e6c879e7370eaa63b99`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 4
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`ChangePinHandler` manages PIN credential updates for staff and managers in Fale7 POS. It supports two distinct authorization pathways:
1. **Self-Service PIN Change (`ActorUserId == UserId`):** The user updates their own PIN by presenting and proving their current PIN (`PinHasher.Verify`).
2. **Administrative Reset (`ActorUserId != UserId`):** An administrator resets another staff member's PIN without knowing the existing PIN. Gated on `users_manage` permission.

### Security Invariants & Vulnerability Forensics:
- **Owner Account Immunity:** Administrative resets unconditionally refuse to target the `owner` account via `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ")`. The owner is permitted to update their own PIN via self-service.
- **PIN Change Session Invalidation Gap:** When a user's PIN is changed, the handler mutates `target.PinHash`, `target.PinSalt`, and `target.PinIterations`, but **does NOT revoke active sessions in `_db.UserSessions`**. A compromised session token remains valid for up to 16 hours after a user changes their PIN.
- **Reliance on Untrusted `request.ActorUserId` (SEC-001):** The handler determines whether the operation is self-service or admin reset based on `request.ActorUserId == request.UserId`. Exploitation of SEC-001 allows identity manipulation.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-CP-001` | `src/Falah.Infrastructure/Users/ChangePinHandler.cs:24:5:ConstructorDeclaration` | [24-28](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L24-L28) | Constructor (DI Hook) |
| `CAL-CP-002` | `src/Falah.Infrastructure/Users/ChangePinHandler.cs:30:5:MethodDeclaration` | [30-65](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L30-L65) | Substantive (Security Boundary) |
| `CAL-CP-003` | `src/Falah.Infrastructure/Users/ChangePinHandler.cs:32:58:SimpleLambdaExpression` | [32](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L32) | Lambda Predicate |
| `CAL-CP-004` | `src/Falah.Infrastructure/Users/ChangePinHandler.cs:44:80:SimpleLambdaExpression` | [44](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/ChangePinHandler.cs#L44) | Lambda Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Users/ChangePinHandler.cs:24:5:ConstructorDeclaration
- **Exact Line Range:** Lines 24–28
- **Signature:** `public ChangePinHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Actual Behavior & Inputs:** Captures `FalahDbContext` for reading/writing users and audit records, and `IApprovalRegistry` for administrative permission evaluation.
- **Concurrency & Lifecycle:** Scoped service registered per request.

---

### Callable: src/Falah.Infrastructure/Users/ChangePinHandler.cs:30:5:MethodDeclaration
- **Exact Line Range:** Lines 30–65
- **Signature:** `public async Task HandleAsync(ChangePinRequest request, CancellationToken ct = default)`
- **Actual Behavior & Execution Path:**
  1. **Target Lookup:** Queries `target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)`. Throws `InvalidOperationException("المستخدم غير موجود.")` if missing.
  2. **Branching Authorization:**
     - **Branch A: Self-Service (`request.ActorUserId == request.UserId`):**
       - Asserts `request.CurrentPin` is not empty.
       - Validates old PIN: `PinHasher.Verify(request.CurrentPin, target.PinHash, target.PinSalt, target.PinIterations)`. Throws `DomainException("رمز PIN الحالي غير صحيح.")` if verification fails.
       - Sets `actorName = target.Name`.
     - **Branch B: Administrative Reset (`request.ActorUserId != request.UserId`):**
       - Queries `actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)`.
       - Enforces permission: `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "users_manage")`.
       - Enforces owner protection: `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ")`.
       - Sets `actorName = actorUser.Name`.
  3. **Uniqueness Check:** Calls `await PinUniquenessChecker.AssertNotInUseAsync(_db, request.NewPin, excludeUserId: target.Id, ct)`.
  4. **Hashing & Storage:**
     - Computes `hashed = PinHasher.Hash(request.NewPin)`.
     - Updates `target.PinHash`, `target.PinSalt`, and `target.PinIterations`.
     - Appends audit event: `Action = "user_pin_changed"`.
     - Commits via `await _db.SaveChangesAsync(ct)`.
- **Trust Boundaries & Security Implications:**
  - **Absence of Session Revocation:** Fails to revoke active sessions in `_db.UserSessions` for `target.Id`. If an account's PIN was changed because it was compromised, the attacker's session remains active.
  - **Old PIN Verification:** Successfully prevents self-service PIN changes without proof of the existing PIN.
- **Test Coverage:** Tested in `UserManagementVerticalSliceTests.User_can_change_own_pin_with_current_pin` and `UserManagementVerticalSliceTests.Admin_cannot_reset_owner_pin`.

---

### Callable: src/Falah.Infrastructure/Users/ChangePinHandler.cs:32:58:SimpleLambdaExpression
- **Exact Line Range:** Line 32
- **Signature:** `u => u.Id == request.UserId`
- **Actual Behavior:** LINQ expression translating to SQL primary key lookup for the target user.

---

### Callable: src/Falah.Infrastructure/Users/ChangePinHandler.cs:44:80:SimpleLambdaExpression
- **Exact Line Range:** Line 44
- **Signature:** `u => u.Id == request.ActorUserId`
- **Actual Behavior:** LINQ expression translating to SQL primary key lookup for the acting user.

---

## 4. Verification & Recommendations

1. **Immediate Session Invalidation:** When updating `target.PinHash`, execute `UPDATE UserSessions SET RevokedAtUtc = @now WHERE UserId = @userId AND RevokedAtUtc IS NULL` to terminate existing sessions across all stations.
2. **Context Actor Binding:** Compare `request.ActorUserId` against `context.ActorId()` to eliminate identity spoofing vectors.
3. **Static Verification:** Verified complete against `src/Falah.Infrastructure/Users/ChangePinHandler.cs`. No application source modified.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:35:00Z
- **Source SHA-256 Verified:** `92efa770a8ddbc53fe25ffb287a3d6344c052ad2cf863e6c879e7370eaa63b99`
- **Callable ID Checked:** `src/Falah.Infrastructure/Users/ChangePinHandler.cs:24:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Users/ChangePinHandler.cs:30:5:MethodDeclaration`, `src/Falah.Infrastructure/Users/ChangePinHandler.cs:32:58:SimpleLambdaExpression`, `src/Falah.Infrastructure/Users/ChangePinHandler.cs:44:80:SimpleLambdaExpression` (4 of 4 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Users/ChangePinHandler.cs` (67 lines).
  2. Confirmed session invalidation gap: `ChangePinHandler` mutates target `PinHash`, `PinSalt`, and `PinIterations`, but completely fails to revoke active sessions in `_db.UserSessions`. An active session token continues to be accepted by `SessionService.ResolveAsync` even after a credential change.
  3. Verified owner immunity: Line 47 executes `UserManagementRules.AssertTargetIsNotOwner(target.Role, "تغيير رمز PIN لـ")` when `ActorUserId != UserId`, protecting the owner from administrative resets.
  4. Verified old PIN verification for self-service changes (`request.ActorUserId == request.UserId`).
  5. All 4 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:38:12Z
- **Source SHA-256 Verified:** `92efa770a8ddbc53fe25ffb287a3d6344c052ad2cf863e6c879e7370eaa63b99`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `92efa770a8ddbc53fe25ffb287a3d6344c052ad2cf863e6c879e7370eaa63b99` against `src/Falah.Infrastructure/Users/ChangePinHandler.cs` (67 lines).
2. Concur with L1 on owner immunity enforcement and active session revocation gap.
3. Additional L2 Challenger Assessment:
   - In `HandleAsync` (lines 30-66), self-service PIN changes require current PIN verification via PBKDF2.
   - Administrative PIN resets enforce `UserManagementRules.AssertTargetIsNotOwner(target.Role, ...)` at line 47, mathematically preventing rogue administrators from overriding the owner credential.
   - Omission of session invalidation on credential update confirmed.
4. All 4 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
