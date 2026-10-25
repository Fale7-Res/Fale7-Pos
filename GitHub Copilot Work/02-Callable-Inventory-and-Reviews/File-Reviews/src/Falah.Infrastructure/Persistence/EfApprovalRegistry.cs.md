# src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs

- **Path:** `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs`
- **Component:** `Falah.Infrastructure.Persistence.EfApprovalRegistry`
- **Assembly:** `Falah.Infrastructure`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `b5028b69a7df5c0e5101466ad51f2c5b84f15089e8d572d71e4ff7b8dc5e0bce`
- **Auditor:** Security and Authorization Auditor
- **Total Callables:** 11
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## 1. Architectural & Domain Responsibility Overview

`EfApprovalRegistry` is the persistent database implementation of `IApprovalRegistry` in Fale7 POS. In restaurant operations, cashiers frequently encounter privileged operations requiring manager overrides—such as line discounts, item voids, order cancellations, refunds, or administrative actions. 

When a manager approves an action, the system issues a cryptographically random token (`ApprovalId = "appr-" + Guid.NewGuid()`), which is recorded in the database. Unlike ephemeral in-memory registries (used in unit test suites), `EfApprovalRegistry` persists approvals to SQLite/PostgreSQL so that an approval survives server restarts or network retries between issuance (`POST /api/approvals/issue`) and subsequent consumption by the cashier's transaction.

### Key Security & Concurrency Invariants:
1. **Separation of Issuance and Consumption:** Issuance and redemption occur in distinct HTTP requests across different client sessions (or the same terminal). The entity tracks `Consumed` (boolean flag) to enforce single-use semantics.
2. **SEC-006 (Missing TTL Expiration):** The entity records `ApprovedAtUtc`, but neither `Find`, `IsConsumed`, nor `MarkConsumed` enforces a maximum time-to-live (TTL). Once granted, an approval token remains valid indefinitely until consumed, presenting an authorization replay window.
3. **SEC-007 (Double-Spending Race Condition):** `MarkConsumed` operates without database-level concurrency tokens (`RowVersion` / `[Timestamp]`) and without pessimistic locking (`UPDLOCK, HOLDLOCK`). Under concurrent execution, two simultaneous requests presenting the same `ApprovalId` can race, evaluate `entity.Consumed == false`, mutate `entity.Consumed = true`, and successfully commit, double-spending a single manager authorization.
4. **Deferred Persistence Semantics:** `Register` and `MarkConsumed` mutate EF change tracking state on `_db.ManagerApprovals` but do not call `_db.SaveChangesAsync()`. Persistence depends strictly on the calling orchestration context committing the unit of work.

---

## 2. Comprehensive Callable Inventory

| Callable ID | Canonical Symbol ID | Line Span | Classification |
| :--- | :--- | :--- | :--- |
| `CAL-EAR-001` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:19:5:ConstructorDeclaration` | [19](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L19) | Constructor (DI Hook) |
| `CAL-EAR-002` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:21:5:MethodDeclaration` | [21-31](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L21-L31) | Substantive (Approval Registration) |
| `CAL-EAR-003` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:33:5:MethodDeclaration` | [33-39](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L33-L39) | Substantive (Lookup & Domain Mapping) |
| `CAL-EAR-004` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:35:64:SimpleLambdaExpression` | [35](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L35) | Local Change Tracker Filter Predicate |
| `CAL-EAR-005` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:36:67:SimpleLambdaExpression` | [36](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L36) | Remote Database Filter Predicate |
| `CAL-EAR-006` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:41:5:MethodDeclaration` | [41-43](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L41-L43) | Substantive (State Verification Query) |
| `CAL-EAR-007` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:42:52:SimpleLambdaExpression` | [42](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L42) | Local Change Tracker Filter Predicate |
| `CAL-EAR-008` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:43:67:SimpleLambdaExpression` | [43](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L43) | Remote Database Filter Predicate |
| `CAL-EAR-009` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:45:5:MethodDeclaration` | [45-53](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L45-L53) | Substantive (Consumption State Mutation) |
| `CAL-EAR-010` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:47:64:SimpleLambdaExpression` | [47](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L47) | Local Change Tracker Filter Predicate |
| `CAL-EAR-011` | `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:48:52:SimpleLambdaExpression` | [48](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L48) | Tracked Database Filter Predicate |

---

## 3. Deep Source-Grounded Callable Analysis

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:19:5:ConstructorDeclaration

- **Syntax Type:** `ConstructorDeclaration`
- **Method Signature:** `public EfApprovalRegistry(FalahDbContext db)`
- **Line Span:** 19
- **Primary Responsibility:** Injects scoped EF DbContext instance for approval entity persistence.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:21:5:MethodDeclaration

- **Syntax Type:** `MethodDeclaration`
- **Method Signature:** `public void Register(ManagerApproval approval)`
- **Line Span:** 21-31
- **Primary Responsibility:** Persists a newly granted manager approval entity into the `ManagerApprovals` DbSet.

#### Implementation Analysis:
```csharp
public void Register(ManagerApproval approval) => _db.ManagerApprovals.Add(new ManagerApprovalEntity
{
    ApprovalId = approval.ApprovalId,
    ApproverUserId = approval.ApproverUserId,
    ApproverName = approval.ApproverName,
    ApproverRole = approval.ApproverRole,
    Action = approval.Action,
    Reason = approval.Reason,
    ApprovedAtUtc = approval.ApprovedAtUtc,
    Consumed = false,
});
```

#### Security & Integrity Assessment:
1. **Audit Attribution:** Captures approver credentials (`ApproverUserId`, `ApproverName`, `ApproverRole`), the targeted action (`Action`), reason string, and creation timestamp.
2. **Initial Consumption State:** Defaults `Consumed = false`.
3. **No Explicit TTL Persisted:** Does not define a `ValidUntilUtc` field. The approval remains permanently spendable until marked consumed.
4. **Change Tracker State:** Attaches the entity in `EntityState.Added`. Committing requires an external `_db.SaveChangesAsync()` call.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:33:5:MethodDeclaration

- **Syntax Type:** `MethodDeclaration`
- **Method Signature:** `public ManagerApproval? Find(string approvalId)`
- **Line Span:** 33-39
- **Primary Responsibility:** Retrieves an approval by its identifier and maps the database entity to the domain `ManagerApproval` record.

#### Implementation Analysis:
```csharp
public ManagerApproval? Find(string approvalId)
{
    var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
        ?? _db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId);
    return entity is null ? null
        : new ManagerApproval(entity.ApprovalId, entity.ApproverUserId, entity.ApproverName, entity.ApproverRole, entity.ApprovedAtUtc, entity.Action, entity.Reason);
}
```

#### Security Analysis:
- Checks `_db.ManagerApprovals.Local` first (in case registered in current unit-of-work), falling back to committed rows using `AsNoTracking()` to avoid unnecessary change-tracker pollution.
- Returns immutable domain record `ManagerApproval`.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:35:64:SimpleLambdaExpression

- **Syntax Type:** `SimpleLambdaExpression`
- **Method Signature:** `a => a.ApprovalId == approvalId`
- **Line Span:** 35
- **Primary Responsibility:** LINQ predicate querying local change tracker cache for pending approval entities.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:36:67:SimpleLambdaExpression

- **Syntax Type:** `SimpleLambdaExpression`
- **Method Signature:** `a => a.ApprovalId == approvalId`
- **Line Span:** 36
- **Primary Responsibility:** LINQ predicate executing database query to locate committed approval record.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:41:5:MethodDeclaration

- **Syntax Type:** `MethodDeclaration`
- **Method Signature:** `public bool IsConsumed(string approvalId)`
- **Line Span:** 41-43
- **Primary Responsibility:** Inspects whether a specific approval token has already been marked as consumed.

#### Implementation Analysis:
```csharp
public bool IsConsumed(string approvalId) =>
    (_db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
        ?? _db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId))?.Consumed ?? false;
```

#### Security Analysis:
- Returns `false` if entity is not found (`?.Consumed ?? false`). This means a non-existent approval returns `IsConsumed == false`, but subsequent authorization checks in `AuthorizationPolicy.AuthorizeCommand` will fail when `Find(approvalId)` returns `null`.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:42:52:SimpleLambdaExpression

- **Syntax Type:** `SimpleLambdaExpression`
- **Method Signature:** `a => a.ApprovalId == approvalId`
- **Line Span:** 42
- **Primary Responsibility:** Predicate for local change-tracker check in `IsConsumed`.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:43:67:SimpleLambdaExpression

- **Syntax Type:** `SimpleLambdaExpression`
- **Method Signature:** `a => a.ApprovalId == approvalId`
- **Line Span:** 43
- **Primary Responsibility:** Predicate for untracked database query in `IsConsumed`.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:45:5:MethodDeclaration

- **Syntax Type:** `MethodDeclaration`
- **Method Signature:** `public void MarkConsumed(string approvalId)`
- **Line Span:** 45-53
- **Primary Responsibility:** Transitions an approval token to consumed state, throwing `DomainException` if already spent.

#### Implementation Analysis:
```csharp
public void MarkConsumed(string approvalId)
{
    var entity = _db.ManagerApprovals.Local.FirstOrDefault(a => a.ApprovalId == approvalId)
        ?? _db.ManagerApprovals.FirstOrDefault(a => a.ApprovalId == approvalId) // tracked - must be mutable
        ?? throw new InvalidOperationException("الاعتماد غير موجود لتحديده كمستهلك.");
    if (entity.Consumed)
        throw new Falah.Domain.Common.DomainException("اعتماد المدير مستخدم بالفعل.");
    entity.Consumed = true;
}
```

#### Security Vulnerability Breakdown:
1. **Double-Spending Race (SEC-007):**
   - The query fetches `entity` with standard read locks.
   - If two cashiers or two requests simultaneously present `approvalId` (e.g. across two browser tabs or two till terminals for refunding two orders), both execute line 47/48, both evaluate `entity.Consumed == false` at line 50, both set `entity.Consumed = true` at line 52, and both call `SaveChangesAsync()`.
   - Without an optimistic concurrency row version or a pessimistic `UPDLOCK`, EF Core executes:
     `UPDATE ManagerApprovals SET Consumed = 1 WHERE ApprovalId = @id;`
   - Both transactions succeed, allowing one approval to authorize two sensitive operations.
2. **Missing Atomicity:**
   - `MarkConsumed` mutates the entity in memory but relies on the surrounding request scope to execute `SaveChangesAsync()`. If the surrounding operation encounters an unhandled exception before saving changes, the approval remains unconsumed in the database.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:47:64:SimpleLambdaExpression

- **Syntax Type:** `SimpleLambdaExpression`
- **Method Signature:** `a => a.ApprovalId == approvalId`
- **Line Span:** 47
- **Primary Responsibility:** Predicate checking local tracker for the entity to mark consumed.

---

### Callable: src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:48:52:SimpleLambdaExpression

- **Syntax Type:** `SimpleLambdaExpression`
- **Method Signature:** `a => a.ApprovalId == approvalId`
- **Line Span:** 48
- **Primary Responsibility:** Tracked database query predicate loading the mutable entity to transition its `Consumed` flag.

---

## 4. Security Findings & Recommendations

### Summary Table

| ID | Severity | Category | Description | Status |
| :--- | :--- | :--- | :--- | :--- |
| `SEC-006` | Medium | Authorization / Replay | Missing TTL on Manager Approvals allows tokens to be replayed indefinitely until consumed | Discovered in Wave 3 / Confirmed in Wave 6 |
| `SEC-007` | Medium | Concurrency / Double-Spending | Non-atomic check-and-consume in `MarkConsumed` allows double-spending under concurrent requests | Discovered in Wave 3 / Confirmed in Wave 6 |

### Remediation Recommendations:
1. **Prevent Double-Spending (Fix SEC-007):**
   - Add a `[Timestamp]` / `RowVersion` concurrency token property to `ManagerApprovalEntity` so EF Core throws `DbUpdateConcurrencyException` if another transaction updated the row concurrently.
   - Alternatively, perform an atomic conditional update:
     ```csharp
     var rowsAffected = await _db.Database.ExecuteSqlInterpolatedAsync(
         $"UPDATE ManagerApprovals SET Consumed = 1 WHERE ApprovalId = {approvalId} AND Consumed = 0");
     if (rowsAffected == 0)
         throw new DomainException("اعتماد المدير مستخدم بالفعل أو غير موجود.");
     ```
2. **Enforce TTL Expiration (Fix SEC-006):**
   - Introduce `ValidUntilUtc` (e.g. `ApprovedAtUtc.AddMinutes(15)`) or evaluate `DateTimeOffset.UtcNow - entity.ApprovedAtUtc > TimeSpan.FromMinutes(15)` in `Find` and `MarkConsumed`, rejecting expired approvals.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:52:00Z
- **Source SHA-256 Verified:** `b5028b69a7df5c0e5101466ad51f2c5b84f15089e8d572d71e4ff7b8dc5e0bce`
- **Callable ID Checked:** `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:19:5:ConstructorDeclaration`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:21:5:MethodDeclaration`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:33:5:MethodDeclaration`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:35:64:SimpleLambdaExpression`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:36:67:SimpleLambdaExpression`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:41:5:MethodDeclaration`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:42:52:SimpleLambdaExpression`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:43:67:SimpleLambdaExpression`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:45:5:MethodDeclaration`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:47:64:SimpleLambdaExpression`, `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:48:52:SimpleLambdaExpression` (11 of 11 callables checked)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs` (55 lines).
  2. Confirmed `SEC-006`: `ManagerApprovalEntity` stores only `ApprovedAtUtc`. Neither `Find`, `IsConsumed`, nor `MarkConsumed` validates a TTL or expiration timestamp. Granted manager approvals remain valid indefinitely until consumed.
  3. Confirmed `SEC-007`: In `MarkConsumed` (lines 45-53), the query loads `entity` using standard shared read semantics without pessimistic locks (`UPDLOCK`) or optimistic concurrency tokens (`RowVersion`). Two concurrent requests presenting the same `approvalId` can both evaluate `!entity.Consumed`, set `Consumed = true`, and commit, double-spending a single manager approval token.
  4. Verified local change-tracker fallback pattern across `Find`, `IsConsumed`, and `MarkConsumed`.
  5. All 11 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:53:17Z
- **Source SHA-256 Verified:** `b5028b69a7df5c0e5101466ad51f2c5b84f15089e8d572d71e4ff7b8dc5e0bce`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `b5028b69a7df5c0e5101466ad51f2c5b84f15089e8d572d71e4ff7b8dc5e0bce` against `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs` (55 lines).
2. Concur with L1 confirming `SEC-006` (absence of expiration TTL on approval tokens) and `SEC-007` (non-atomic check and consumption allowing double-spending).
3. Additional L2 Challenger Assessment:
   - In `MarkConsumed` (lines 45-53), the query loads `entity` using standard shared reads without `UPDLOCK` hints or optimistic concurrency tokens (`RowVersion`). Two concurrent requests presenting the same `approvalId` can both evaluate `!entity.Consumed`, set `Consumed = true`, and commit, allowing duplicate financial comps/refunds against a single approval token.
   - In `Register` (lines 21-31), `ApprovedAtUtc` is stored but never compared against a TTL window during `Find` or `IsConsumed`, enabling indefinite token stockpiling.
4. All 11 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
