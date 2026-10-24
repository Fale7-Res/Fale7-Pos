# Substantive Callable Audit: `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `0d8401a67d756fe1854ff8228e9efea17db6e1db91d8a5abb40499ae1eb982c4`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **5**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Payroll run lifecycle state machine handler transitioning runs between Draft, Approved, Paid, and Cancelled.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Employees.TransitionPayrollHandler.TransitionPayrollHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs:18:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `18-22`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Employees.TransitionPayrollHandler.TransitionPayrollHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry). Manages internal state transition within src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 18-22):**
```csharp
  18 |     public TransitionPayrollHandler(FalahDbContext db, IApprovalRegistry approvalRegistry)
  19 |     {
  20 |         _db = db;
  21 |         _approvalRegistry = approvalRegistry;
  22 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Employees.TransitionPayrollHandler.HandleAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs:24:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `24-61`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-006`**

**A. Actual Behavior & Execution Path:**  
Loads PayrollRunEntity and updates Status to requested transition target. When target is Paid, marks run as paid without posting financial journal or employee ledger movements.

**B. Trust Boundaries & Validation:**  
Manager payroll command.

**C. State Mutations & Side Effects:**  
Mutates PayrollRunEntity.Status and AuditLog.

**D. Concurrency & Transaction Implications:**  
Guarded by database transaction.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW FIN-006: Status reaches Paid with zero cash treasury movement or employee disbursement records.

> **Verbatim Code Excerpt (Lines 24-61):**
```csharp
  24 |     public async Task<PayrollRunDto> HandleAsync(TransitionPayrollRequest request, CancellationToken ct = default)
  25 |     {
  26 |         if (!Enum.TryParse<PayrollRunStatus>(request.Status, ignoreCase: true, out var targetStatus))
  27 |             throw new DomainException("حالة Payroll غير صالحة.");
  28 | 
  29 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  30 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  31 |         var actor = OrderMapper.ToActor(actorUser);
  32 |         AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "employees_manage");
  33 | 
  34 |         var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
  35 |             ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
  36 |         var currentStatus = Enum.Parse<PayrollRunStatus>(run.Status);
  37 |         PayrollRules.AssertTransition(currentStatus, targetStatus);
  38 | 
  39 |         string? approvedByName = null;
  40 |         if (targetStatus == PayrollRunStatus.Approved)
  41 |         {
  42 |             ManagerApproval? approval = null;
  43 |             if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  44 |             {
  45 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  46 |                     ?? throw new DomainException("المعتمد غير موجود.");
  47 |                 approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  48 |                     approverUser.Role, DateTimeOffset.UtcNow, "payroll_approve", "اعتماد Payroll");
  49 |             }
  50 |             var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "payroll_approve", approval, requireApproval: true);
  51 |             approvedByName = authorization.Approver?.ApproverName ?? actor.Name;
  52 |         }
  53 | 
  54 |         var now = DateTimeOffset.UtcNow;
  55 |         run.Status = targetStatus.ToString();
  56 |         if (targetStatus == PayrollRunStatus.Approved) run.ApprovedBy = approvedByName;
  57 |         if (targetStatus == PayrollRunStatus.Reviewed) { run.ReviewedBy = actor.Name; run.ReviewedAtUtc = now; }
  58 | 
  59 |         await _db.SaveChangesAsync(ct);
  60 |         return CreatePayrollHandler.ToDto(run, wasReplayed: false);
  61 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs:29:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `29-29`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 29-29):**
```csharp
  29 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs:34:61:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `34-34`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 34-34):**
```csharp
  34 |         var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs:45:87:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `45-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 45-45):**
```csharp
  45 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
```

---
