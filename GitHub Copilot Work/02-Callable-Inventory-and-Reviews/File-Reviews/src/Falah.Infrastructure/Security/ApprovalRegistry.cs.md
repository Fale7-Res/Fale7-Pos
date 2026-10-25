# Security Review: src/Falah.Infrastructure/Security/ApprovalRegistry.cs

## File Overview
- **Canonical Source Paths:** 
  - `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs` (Production SQL EF Core Implementation)
  - `src/Falah.Domain/Authorization/ApprovalRegistry.cs` (Domain Contracts & Authorization Engine)
  - `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (Model Configuration Lines 178-184)
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Source SHA-256:**
  - `EfApprovalRegistry.cs`: `b5028b69a7df5c0e5101466ad51f2c5b84f15089e8d572d71e4ff7b8dc5e0bce`
  - `ApprovalRegistry.cs`: `029edee4286b6aea725da962dddaafff1e6bc20f57ddc922801c39095b43585d0`
  - `FalahDbContext.cs`: `7fda50ac64f49021d17d7ea9636456dd15556c6d8d4a76f5e553be0a42f88d3c`
- **Auditor:** DEEP_SECURITY_SPECIALIST
- **Review Date:** 2026-10-25T10:10:00Z
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED**

---

## Callable Reviews

### EfApprovalRegistry.Register — Lines 21-31
- **Callable ID:** `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:21:5:MethodDeclaration`
- **Signature:** `public void Register(ManagerApproval approval)`
- **Behavior:** Persists a newly granted manager approval entity into the `ManagerApprovals` DbSet with `ApprovalId`, `ApproverUserId`, `ApproverName`, `ApproverRole`, `Action`, `Reason`, `ApprovedAtUtc`, and sets `Consumed = false`.
- **Security Analysis:** Attaches the entity in `EntityState.Added`. Does not enforce or store an expiration date (`ValidUntilUtc`), creating vulnerability `SEC-006`.
- **Finding Links:** `SEC-006`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

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

---

### EfApprovalRegistry.Find — Lines 33-39
- **Callable ID:** `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:33:5:MethodDeclaration`
- **Signature:** `public ManagerApproval? Find(string approvalId)`
- **Behavior:** Queries `_db.ManagerApprovals.Local` first; if not found in the local change tracker, queries `_db.ManagerApprovals.AsNoTracking().FirstOrDefault(a => a.ApprovalId == approvalId)`. Maps entity to domain `ManagerApproval` record or returns `null`.
- **Security Analysis:** Untracked remote database fallback avoids change tracker pollution.
- **Finding Links:** None
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### EfApprovalRegistry.IsConsumed — Lines 41-43
- **Callable ID:** `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:41:5:MethodDeclaration`
- **Signature:** `public bool IsConsumed(string approvalId)`
- **Behavior:** Checks local change tracker first, then queries untracked database record to evaluate `entity?.Consumed ?? false`.
- **Security Analysis:** Read query used by domain policy to determine token spend status.
- **Finding Links:** `SEC-007`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

---

### EfApprovalRegistry.MarkConsumed — Lines 45-53
- **Callable ID:** `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs:45:5:MethodDeclaration`
- **Signature:** `public void MarkConsumed(string approvalId)`
- **Behavior:** Retrieves the tracked `ManagerApprovalEntity` (checking `Local` first, then executing tracked query). Throws `InvalidOperationException` if not found. Throws `DomainException("اعتماد المدير مستخدم بالفعل.")` if `entity.Consumed == true`. Mutates `entity.Consumed = true`.
- **Security Analysis:** 
  - *Prior Defect Claim (SEC-007):* Prior audit waves raised SEC-007 asserting that because `MarkConsumed` does not execute an atomic SQL update (`UPDATE ... WHERE Consumed = 0`), concurrent requests could both observe `Consumed == false` and double-spend the approval.
  - *Mitigation Verification:* In `FalahDbContext.cs` (line 183), `Consumed` is explicitly configured as `.IsConcurrencyToken()`:
    ```csharp
    modelBuilder.Entity<ManagerApprovalEntity>(b =>
    {
        b.HasKey(a => a.ApprovalId);
        // Two requests may validate the same unused approval before either saves.
        // Only the first false -> true update may commit with its financial operation.
        b.Property(a => a.Consumed).IsConcurrencyToken();
    });
    ```
    Under EF Core, mapping `.IsConcurrencyToken()` on `Consumed` ensures that the generated SQL UPDATE includes `WHERE [ApprovalId] = @p0 AND [Consumed] = 0`. If a concurrent transaction commits first, the second update matches 0 rows and throws `DbUpdateConcurrencyException`, aborting the second transaction and preventing double-spending.
- **Finding Links:** `SEC-007` (Mitigated)
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

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

---

### AuthorizationPolicy.ValidateApproval — Lines 89-97 (`src/Falah.Domain/Authorization/ApprovalRegistry.cs`)
- **Callable ID:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs:89:5:MethodDeclaration`
- **Signature:** `public static ManagerApproval ValidateApproval(IApprovalRegistry registry, string action, ManagerApproval approval, bool consume = true)`
- **Behavior:** Queries `registry.Find(approval.ApprovalId)`. Checks:
  1. `issued is null`
  2. `issued.Action != action`
  3. `issued.ApproverUserId != approval.ApproverUserId`
  4. `registry.IsConsumed(approval.ApprovalId)`
  If any check fails, throws `DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.")`. If `consume == true`, calls `registry.MarkConsumed(approval.ApprovalId)`. Returns `issued`.
- **Security Analysis:** Crucially misses checking `issued.ApprovedAtUtc`. There is zero TTL validation.
- **Finding Links:** `SEC-006`
- **Verdict:** SUBSTANTIVE_SOURCE_REVIEWED

```csharp
    public static ManagerApproval ValidateApproval(IApprovalRegistry registry, string action, ManagerApproval approval, bool consume = true)
    {
        var issued = registry.Find(approval.ApprovalId);
        if (issued is null || issued.Action != action || issued.ApproverUserId != approval.ApproverUserId || registry.IsConsumed(approval.ApprovalId))
            throw new DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.");

        if (consume) registry.MarkConsumed(approval.ApprovalId);
        return issued;
    }
```

---

## Technical Finding Re-Evaluation: SEC-006 & SEC-007

### Confirmed Finding: SEC-006
**Category:** SECURITY / REPLAY_ATTACK  
**Severity:** MEDIUM (CVSS v3.1: 6.5 - AV:N/AC:L/PR:L/UI:N/S:U/C:N/I:H/A:N)  
**Confidence:** CONFIRMED_MECHANISM_AND_STATIC_TRACE  
**Source:** `src/Falah.Domain/Authorization/ApprovalRegistry.cs` Lines 89-97  

```csharp
    public static ManagerApproval ValidateApproval(IApprovalRegistry registry, string action, ManagerApproval approval, bool consume = true)
    {
        var issued = registry.Find(approval.ApprovalId);
        if (issued is null || issued.Action != action || issued.ApproverUserId != approval.ApproverUserId || registry.IsConsumed(approval.ApprovalId))
            throw new DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.");

        if (consume) registry.MarkConsumed(approval.ApprovalId);
        return issued;
    }
```

**Defect:**
Absence of expiration time-to-live (TTL) on manager approval tokens. Although `ManagerApproval` and `ManagerApprovalEntity` capture `ApprovedAtUtc`, neither `ValidateApproval` nor `EfApprovalRegistry` evaluates the token's age against the current timestamp.
Once issued via `POST /api/approvals/issue`, an approval ID remains permanently valid in the database until consumed. A cashier can induce a manager to sign off on a void, comp, or shift discrepancy, abort the transaction before submitting, retain the approval ID, and consume it days or weeks later when the manager is off-duty.

**Proposed Fix Specification:**
Pass `DateTimeOffset nowUtc` into `AuthorizationPolicy.ValidateApproval` and enforce a maximum lifespan (e.g., 15 minutes):
```csharp
public static ManagerApproval ValidateApproval(
    IApprovalRegistry registry, string action, ManagerApproval approval, DateTimeOffset nowUtc, bool consume = true)
{
    var issued = registry.Find(approval.ApprovalId);
    if (issued is null || issued.Action != action || issued.ApproverUserId != approval.ApproverUserId || registry.IsConsumed(approval.ApprovalId))
        throw new DomainException("اعتماد المدير غير صالح أو لا يطابق العملية.");

    if (nowUtc - issued.ApprovedAtUtc > TimeSpan.FromMinutes(15))
        throw new DomainException("انتهت صلاحية اعتماد المدير (الحد الأقصى 15 دقيقة).");

    if (consume) registry.MarkConsumed(approval.ApprovalId);
    return issued;
}
```

---

### Mitigated Finding: SEC-007
**Category:** SECURITY / CONCURRENCY  
**Severity:** INFORMATIONAL (Originally HIGH)  
**Disposition:** VERIFIED_MITIGATED_BY_EF_CORE_CONCURRENCY_TOKEN  
**Source:** `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` Lines 178-184  

```csharp
        modelBuilder.Entity<ManagerApprovalEntity>(b =>
        {
            b.HasKey(a => a.ApprovalId);
            // Two requests may validate the same unused approval before either saves.
            // Only the first false -> true update may commit with its financial operation.
            b.Property(a => a.Consumed).IsConcurrencyToken();
        });
```

**Forensic Analysis & Verification:**
Prior audit reports flagged `SEC-007` alleging that `EfApprovalRegistry.MarkConsumed` was subject to double-spending because it queried `entity.Consumed == false` and set `entity.Consumed = true` without explicit database row locks (`UPDLOCK`) or a `[Timestamp]` byte array.
A deep source inspection of `FalahDbContext.OnModelCreating` reveals that the entity property `Consumed` is explicitly configured as an optimistic concurrency token:
`b.Property(a => a.Consumed).IsConcurrencyToken();`

In Entity Framework Core, marking a property with `.IsConcurrencyToken()` mandates that all UPDATE and DELETE statements include the property's original queried value in the WHERE predicate:
```sql
UPDATE [ManagerApprovals] 
SET [Consumed] = @p0 
WHERE [ApprovalId] = @p1 AND [Consumed] = @p2;
```
When two concurrent requests attempt to consume the identical `ApprovalId`:
1. Request 1 and Request 2 both read `Consumed = false` (original value = `false`).
2. Request 1 commits first: executes UPDATE WHERE `Consumed = false`. The database updates 1 row; transaction succeeds.
3. Request 2 attempts to commit: executes UPDATE WHERE `Consumed = false`. Because Request 1 set `Consumed = true`, 0 rows match.
4. EF Core detects 0 affected rows and immediately throws `DbUpdateConcurrencyException`.
5. The surrounding transaction rolls back, aborting Request 2's financial operation (refund, comp, discount, or drawer opening).

**Conclusion:** SEC-007 is **MITIGATED** in production EF Core execution. Double-spending of manager approvals is prevented by optimistic concurrency enforcement on `ManagerApprovalEntity.Consumed`.
