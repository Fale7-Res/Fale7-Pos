# Security Review: src/Falah.Infrastructure/Users/SetUserActiveHandler.cs

## File Overview
- **Canonical Source Path:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Source SHA-256:** `a8e7093c5b410adee29d6fece0c4f15cc532cf10cb686a37dc05e79aba80d9e7`
- **Auditor:** DEEP_SECURITY_SPECIALIST
- **Review Date:** 2026-10-25T10:10:00Z
- **Total Callables:** 7
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## Callable Reviews

### SetUserActiveHandler..ctor — Lines 21-25
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:21:5:ConstructorDeclaration`
- **Signature:** `public SetUserActiveHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)`
- **Behavior:** Injects and captures the database context `_db` and domain approval registry `_approvalRegistry` in private readonly fields.
- **Security Analysis:** Captures dependency-injected services required for transaction isolation and authorization policy evaluation.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SetUserActiveHandler.HandleAsync — Lines 27-70
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:27:5:MethodDeclaration`
- **Signature:** `public async Task HandleAsync(SetUserActiveRequest request, CancellationToken ct = default)`
- **Behavior:** 
  1. Opens an explicit database transaction with `System.Data.IsolationLevel.Serializable`.
  2. Fetches the calling actor user (`_db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId)`) and authorizes the command against `users_manage` permission via `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage")`.
  3. Retrieves the target user (`_db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId)`).
  4. Enforces owner immunity (`UserManagementRules.AssertTargetIsNotOwner(target.Role, ...)`).
  5. Prevents self-deactivation (`if (request.UserId == request.ActorUserId && !request.Active) throw ...`).
  6. Returns early if `target.Active == request.Active` (idempotent no-op).
  7. When deactivating an administrator, checks that at least one other active user retains user management permissions (`UserManagementRules.AssertUserAdministrationSurvives(...)`).
  8. Updates `target.Active = request.Active`, appends an audit event (`user_reactivated` or `user_deactivated`) to `ControlAuditEvents`, and commits the transaction.
- **Security Analysis:** 
  - *Strengths:* Enforces unconditional owner account immunity, serializable transaction isolation against surviving admin lockout, and mandatory audit logging.
  - *Reactivation Gap (SEC-011):* Does not verify whether the reactivated user's PIN collides with any currently active user.
  - *Session Revocation Gap (SEC-012):* Deactivating a user does not mark active sessions in `_db.UserSessions` as revoked. (Mitigated in flight by `SessionService.ResolveAsync`).
- **Finding Links:** `SEC-011`, `SEC-012`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

```csharp
    public async Task HandleAsync(SetUserActiveRequest request, CancellationToken ct = default)
    {
        // Keep the surviving-administrator check and the update in one serializable transaction.
        await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
        var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(actorUser);
        AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");

        var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
            ?? throw new InvalidOperationException("المستخدم المستهدف غير موجود.");
        UserManagementRules.AssertTargetIsNotOwner(target.Role, request.Active ? "إعادة تفعيل" : "تعطيل");
        // Ported from the reference app's own `u.id !== currentUser.id` guard on this exact button -
        // an admin can never lock themselves out by deactivating their own account.
        if (request.UserId == request.ActorUserId && !request.Active)
            throw new DomainException("لا يمكنك تعطيل حسابك الخاص.");

        if (target.Active == request.Active) return; // idempotent no-op - already in the requested state

        // The self-deactivation guard above covers the common case, but an actor acting on a one-off
        // manager approval rather than on a permission of their own can still deactivate the last
        // real administrator and close the door behind them.
        if (!request.Active && UserManagementRules.CanManageUsers(
                target.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            var othersWhoCanAdminister = await _db.Users.AsNoTracking()
                .Where(u => u.Active && u.Id != target.Id)
                .Select(u => u.Permissions)
                .ToListAsync(ct);

            UserManagementRules.AssertUserAdministrationSurvives(
                othersWhoCanAdminister.Count(permissions => UserManagementRules.CanManageUsers(
                    permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))));
        }

        target.Active = request.Active;
        _db.ControlAuditEvents.Add(new Falah.Infrastructure.Persistence.Entities.ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = request.Active ? "user_reactivated" : "user_deactivated",
            Actor = actorUser.Name, EntityId = target.Id, Reason = request.Reason, CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
```

---

### SetUserActiveHandler.<HandleAsync>b__2_0 (Actor Lookup Predicate) — Line 31
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:31:76:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ActorUserId`
- **Behavior:** Queries `Users` table by actor user ID with `AsNoTracking()`.
- **Security Analysis:** Resolves actor record to verify user management permissions.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SetUserActiveHandler.<HandleAsync>b__2_1 (Target Lookup Predicate) — Line 36
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:36:58:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.UserId`
- **Behavior:** Queries `Users` table by target user ID for tracking and updating activation status.
- **Security Analysis:** Loads mutable tracking entity for state change.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SetUserActiveHandler.<HandleAsync>b__2_2 (Surviving Admin Predicate) — Line 53
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:53:24:SimpleLambdaExpression`
- **Signature:** `u => u.Active && u.Id != target.Id`
- **Behavior:** Filters active users excluding the target user being deactivated.
- **Security Analysis:** Enforces administrator survival check to prevent total lockout.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SetUserActiveHandler.<HandleAsync>b__2_3 (Permission Projection) — Line 54
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:54:25:SimpleLambdaExpression`
- **Signature:** `u => u.Permissions`
- **Behavior:** Projects comma-delimited permissions string from `Users` table.
- **Security Analysis:** Minimizes I/O by fetching only permission strings.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### SetUserActiveHandler.<HandleAsync>b__2_4 (Surviving Admin Counter Predicate) — Lines 58-59
- **Callable ID:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs:58:46:SimpleLambdaExpression`
- **Signature:** `permissions => UserManagementRules.CanManageUsers(permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))`
- **Behavior:** In-memory predicate checking if any surviving user holds `users_manage` or wildcard `"all"`.
- **Security Analysis:** Guarantees business continuity and access control administrative integrity.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

## New Finding: SEC-011

**Canonical ID:** `SEC-011`  
**Category:** SECURITY / ACCESS_CONTROL  
**Severity:** MEDIUM (CVSS v3.1: 6.5 - AV:N/AC:L/PR:H/UI:N/S:U/C:H/I:H/A:N)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs` Lines 62-69  

```csharp
        target.Active = request.Active;
        _db.ControlAuditEvents.Add(new Falah.Infrastructure.Persistence.Entities.ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = request.Active ? "user_reactivated" : "user_deactivated",
            Actor = actorUser.Name, EntityId = target.Id, Reason = request.Reason, CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
```

**Defect:**
User account reactivation without PIN uniqueness verification. In `PinUniquenessChecker.cs` (line 19), PIN uniqueness is verified strictly against active users (`u => u.Active && u.Id != excludeUserId`). Consequently, when an account is deactivated, its PIN is released from the active uniqueness constraint.
During the period an employee account remains deactivated, an administrator can provision a new staff member or change an existing staff member's PIN to the exact same value previously assigned to the deactivated employee.
When `SetUserActiveHandler.HandleAsync` reactivates the employee (`request.Active = true`), it sets `target.Active = true` without executing any collision check against existing active users. This immediately introduces duplicate active PINs into the POS system, causing authentication ambiguity on cashier keypads and privilege confusion.

**Proposed Fix Specification:**
When `request.Active == true`, verify that the reactivated user's existing PIN does not collide with any currently active user before setting `target.Active = true`:
1. Query active users other than `target.Id`:
   ```csharp
   if (request.Active)
   {
       var activeUsers = await _db.Users.AsNoTracking()
           .Where(u => u.Active && u.Id != target.Id)
           .Select(u => new { u.PinHash, u.PinSalt, u.PinIterations })
           .ToListAsync(ct);
       // Note: Because candidate plaintext PIN is not available during reactivation, verify requires comparing hashes or forcing a PIN reset upon reactivation.
   }
   ```
2. Recommended Architecture: Require the administrator to provide a new, validated PIN when reactivating an account, or flag the reactivated user with `RequiresPinReset = true` and keep authentication locked until a unique PIN is established via `PinUniquenessChecker.AssertNotInUseAsync`.

---

## New Finding: SEC-012

**Canonical ID:** `SEC-012`  
**Category:** SECURITY / SESSION_MANAGEMENT  
**Severity:** LOW (CVSS v3.1: 3.8 - AV:N/AC:H/PR:H/UI:N/S:U/C:L/I:L/A:N)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs` Lines 62-69  

```csharp
        target.Active = request.Active;
        _db.ControlAuditEvents.Add(new Falah.Infrastructure.Persistence.Entities.ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = request.Active ? "user_reactivated" : "user_deactivated",
            Actor = actorUser.Name, EntityId = target.Id, Reason = request.Reason, CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await _db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
```

**Defect & Mitigation Analysis:**
Deactivation without explicit session token revocation. When `SetUserActiveHandler` deactivates a user (`request.Active = false`), it mutates `target.Active = false` but does not revoke existing active session records in `_db.UserSessions` (`RevokedAtUtc` is left `null`).
- *In-Flight Mitigation Verified:* In `SessionService.ResolveAsync` (lines 53-56), the server verifies `u.Id == session.UserId` and checks `if (user is null || !user.Active) return null;`. Therefore, as long as the user remains deactivated, any attempt to make an API request with that session bearer token is immediately rejected with HTTP 401 Unauthorized.
- *Residual Security Hazard:* Because session tokens are not permanently revoked in `_db.UserSessions`, dormant bearer tokens remain cryptographically valid. If the user account is subsequently reactivated (e.g. temporary suspension lifted) within the 16-hour session idle lifetime (`SessionToken.IdleLifetime`), the old bearer tokens instantly regain full access without requiring the employee to re-authenticate with their PIN.

**Proposed Fix Specification:**
In `SetUserActiveHandler.cs`, when `!request.Active`, explicitly revoke all existing active sessions for the deactivated user within the same database transaction:
```csharp
if (!request.Active)
{
    var activeSessions = await _db.UserSessions
        .Where(s => s.UserId == target.Id && s.RevokedAtUtc == null)
        .ToListAsync(ct);
    foreach (var session in activeSessions)
    {
        session.RevokedAtUtc = DateTimeOffset.UtcNow;
    }
}
```
This guarantees complete cryptographic session termination upon employee deactivation.
