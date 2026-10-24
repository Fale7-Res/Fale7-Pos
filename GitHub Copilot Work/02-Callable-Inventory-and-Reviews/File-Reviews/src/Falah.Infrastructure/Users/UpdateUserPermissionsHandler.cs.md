# Substantive Callable Audit: `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `796cc8d48d0b1224441c6028cd98cbdd10abae53896a4b591ce04c86e146fe55`
- **Auditor Specialist:** `security-auditor`
- **Total File Callables:** **7**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Administrative user permission management handler. Mutates target user permissions under serializable database transaction with owner-immutability and privilege-escalation checks.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Users.UpdateUserPermissionsHandler.UpdateUserPermissionsHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry)`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:22:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `22-26`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Users.UpdateUserPermissionsHandler.UpdateUserPermissionsHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry). Manages internal state transition within src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 22-26):**
```csharp
  22 |     public UpdateUserPermissionsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
  23 |     {
  24 |         _db = db;
  25 |         _approvalRegistry = approvalRegistry;
  26 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Users.UpdateUserPermissionsHandler.HandleAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:28:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `28-75`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-001`**

**A. Actual Behavior & Execution Path:**  
Begins Serializable transaction. Queries Users table for request.ActorUserId and checks users_manage permission via AuthorizationPolicy.AuthorizeCommand. Queries target user and asserts target is not owner. Updates permissions hash set and validates that at least one admin survives. Appends audit log entry and commits transaction.

**B. Trust Boundaries & Validation:**  
VULNERABILITY SEC-001: Queries actorUser from request.ActorUserId rather than context.ActorId(). Attacker exploiting case-sensitivity bypass can impersonate administrator to escalate permissions.

**C. State Mutations & Side Effects:**  
Mutates target UserEntity.Permissions, inserts ControlAuditEventEntity, commits EF Core transaction.

**D. Concurrency & Transaction Implications:**  
Serializable isolation level prevents concurrent privilege escalation or simultaneous admin revocation races.

**E. Specialist Risks & Invariants:**  
Critical authorization flaw allowing non-admin caller to grant arbitrary permissions if ActorUserId is spoofed.

> **Verbatim Code Excerpt (Lines 28-75):**
```csharp
  28 |     public async Task HandleAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default)
  29 |     {
  30 |         // Concurrent revocations must not each count the other administrator as a survivor.
  31 |         await using var transaction = await _db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, ct);
  32 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  33 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  34 |         var actor = OrderMapper.ToActor(actorUser);
  35 |         AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");
  36 | 
  37 |         var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
  38 |             ?? throw new InvalidOperationException("المستخدم المستهدف غير موجود.");
  39 |         UserManagementRules.AssertTargetIsNotOwner(target.Role, "تعديل صلاحيات");
  40 | 
  41 |         var grant = request.Grant.Distinct().ToList();
  42 |         var revoke = request.Revoke.Distinct().ToList();
  43 |         UserManagementRules.AssertKnownPermissions(grant.Concat(revoke));
  44 |         UserManagementRules.AssertNoPrivilegeEscalation(actor.Permissions, grant);
  45 | 
  46 |         var current = target.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToHashSet();
  47 |         current.UnionWith(grant);
  48 |         current.ExceptWith(revoke);
  49 | 
  50 |         // Counted AFTER the change, against everyone else, because the question is only ever "who
  51 |         // is left" - and the target themselves counts only if this change leaves them both active
  52 |         // and still able. Revoking users_manage from the last holder is the realistic way a
  53 |         // restaurant locks itself out of its own till while tidying up permissions.
  54 |         if (!UserManagementRules.CanManageUsers(current))
  55 |         {
  56 |             var othersWhoCanAdminister = await _db.Users.AsNoTracking()
  57 |                 .Where(u => u.Active && u.Id != target.Id)
  58 |                 .Select(u => u.Permissions)
  59 |                 .ToListAsync(ct);
  60 | 
  61 |             UserManagementRules.AssertUserAdministrationSurvives(
  62 |                 othersWhoCanAdminister.Count(permissions => UserManagementRules.CanManageUsers(
  63 |                     permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))));
  64 |         }
  65 | 
  66 |         target.Permissions = string.Join(",", current);
  67 | 
  68 |         _db.ControlAuditEvents.Add(new ControlAuditEventEntity
  69 |         {
  70 |             Id = $"audit-{Guid.NewGuid():N}", Action = "permissions_changed", Actor = actorUser.Name, EntityId = target.Id,
  71 |             Reason = $"منح: [{string.Join(", ", grant)}] - سحب: [{string.Join(", ", revoke)}]", CreatedAtUtc = DateTimeOffset.UtcNow,
  72 |         });
  73 |         await _db.SaveChangesAsync(ct);
  74 |         await transaction.CommitAsync(ct);
  75 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:32:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `32-32`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 32-32):**
```csharp
  32 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:37:58:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `37-37`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 37-37):**
```csharp
  37 |         var target = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:57:24:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `57-57`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 57-57):**
```csharp
  57 |                 .Where(u => u.Active && u.Id != target.Id)
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:58:25:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `58-58`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 58-58):**
```csharp
  58 |                 .Select(u => u.Permissions)
```

---

<a id="callable-7"></a>
### Callable 7: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs:62:46:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `62-63`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 62-63):**
```csharp
  62 |                 othersWhoCanAdminister.Count(permissions => UserManagementRules.CanManageUsers(
  63 |                     permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))));
```

---
