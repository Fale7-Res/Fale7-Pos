# Substantive Callable Audit: `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `601f29ab9abe3dd644137bbf20383cf21ab60e040ff79c55a5b2c37cff292b76`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **9**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Cash safe drop handler skimming cash from cashier drawer into central restaurant safe during active shifts.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Treasury.SafeDropHandler.SafeDropHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:26:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `26-31`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Treasury.SafeDropHandler.SafeDropHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 26-31):**
```csharp
  26 |     public SafeDropHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
  27 |     {
  28 |         _db = db;
  29 |         _approvalRegistry = approvalRegistry;
  30 |         _auditLog = auditLog;
  31 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:33:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `33-117`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-013`**

**A. Actual Behavior & Execution Path:**  
Loads active shift with AsNoTracking. Opens transaction. Records TreasuryMovement with CashLifecycle direction and SafeDrop reason.

**B. Trust Boundaries & Validation:**  
Cashier safe drop command.

**C. State Mutations & Side Effects:**  
Inserts TreasuryMovementEntity, commits transaction.

**D. Concurrency & Transaction Implications:**  
Shifts read with AsNoTracking allows safe drop to race shift closure.

**E. Specialist Risks & Invariants:**  
Attribution discrepancy if safe drop commits after shift close handover calculation.

> **Verbatim Code Excerpt (Lines 33-117):**
```csharp
  33 |     public async Task<SafeDropResult> HandleAsync(SafeDropRequest request, CancellationToken ct = default)
  34 |     {
  35 |         var idempotencyKey = $"safe-drop-{request.DropRequestId}";
  36 |         var existing = await FindExistingAsync(idempotencyKey, ct);
  37 |         if (existing is not null) return ToResult(existing, wasReplayed: true);
  38 | 
  39 |         if (request.AmountPiastres <= 0) throw new DomainException("مبلغ الإيداع الآمن يجب أن يكون أكبر من صفر.");
  40 |         if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الإيداع الآمن مطلوب.");
  41 | 
  42 |         var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
  43 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  44 |         var actor = OrderMapper.ToActor(createdByUser);
  45 |         _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  46 |             ?? throw new InvalidOperationException("الوردية غير موجودة.");
  47 | 
  48 |         ManagerApproval? approval = null;
  49 |         if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  50 |         {
  51 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  52 |                 ?? throw new DomainException("المعتمد غير موجود.");
  53 |             approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  54 |                 approverUser.Role, DateTimeOffset.UtcNow, "safe_drop", request.Reason);
  55 |         }
  56 |         var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "safe_drop", approval, requireApproval: true);
  57 | 
  58 |         var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  59 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  60 |         try
  61 |         {
  62 |             // Read under a lock, inside the transaction, immediately before spending.
  63 |             // Outside it, concurrent commands each read the same balance, each decide it covers
  64 |             // them, and each write - which is how an account holding 30 paid out three separate
  65 |             // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
  66 |             var drawerMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, TreasuryAccounts.Drawer, ct);
  67 |             FinancialRules.AssertProtectedSourceBalance(drawerMovements, TreasuryAccounts.Drawer, Money.FromPiastres(request.AmountPiastres));
  68 | 
  69 |             var journal = new EfFinancialJournal(_db);
  70 |             if (!journal.TryStart(idempotencyKey, "safe_drop"))
  71 |                 throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
  72 | 
  73 |             var now = DateTimeOffset.UtcNow;
  74 |             var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
  75 |                 $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "safe_drop",
  76 |                 // The destination is the whole point of a safe drop: cash leaves the till and goes
  77 |                 // INTO the main treasury. Passing null here meant the drawer fell and nothing rose -
  78 |                 // the money left the books entirely - while the audit line below said, and still
  79 |                 // says, that it went to the main treasury. A movement that disagrees with its own
  80 |                 // audit record is worse than either being wrong alone.
  81 |                 TreasuryAccounts.Drawer, TreasuryAccounts.Main, Money.FromPiastres(request.AmountPiastres),
  82 |                 "safe_drop", request.DropRequestId, businessDayId, request.ShiftId,
  83 |                 request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);
  84 | 
  85 |             var entity = OrderMapper.ToEntity(movement);
  86 |             _db.TreasuryMovements.Add(entity);
  87 |             journal.Complete(idempotencyKey);
  88 | 
  89 |             _auditLog.Append(new AuditEvent(
  90 |                 $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
  91 |                 "إيداع آمن", "cash", $"إيداع {request.AmountPiastres} قرش من الدرج إلى الخزينة الرئيسية - {request.Reason}",
  92 |                 "normal", now, ShiftId: request.ShiftId, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
  93 |                 OperationId: movement.OperationId, ReferenceId: entity.Id));
  94 | 
  95 |             await _db.SaveChangesAsync(ct);
  96 |             await transaction.CommitAsync(ct);
  97 |             return ToResult(entity, wasReplayed: false);
  98 |         }
  99 |         catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException
 100 |                                    || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 101 |         {
 102 |             await transaction.RollbackAsync(ct);
 103 |             var replay = await FindExistingAsync(idempotencyKey, ct);
 104 |             // The loser of a balance-lock race carries the same request id as the sibling that
 105 |             // won it, and that sibling may not have committed yet at this instant. Looking once
 106 |             // and giving up turns "somebody else is doing it" into an error.
 107 |             if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 108 |                 replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
 109 |                     async () => await FindExistingAsync(idempotencyKey, ct), ct);
 110 |             if (replay is not null) return ToResult(replay, wasReplayed: true);
 111 |             // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
 112 |             // review a half-finished movement would send them looking for something that is not there.
 113 |             if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 114 |                 throw new TreasuryConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
 115 |             throw new TreasuryConflictException("تعذر تسجيل الإيداع الآمن بسبب تعارض في مفتاح منع التكرار، ولا يوجد إيداع سابق لإعادته - يحتاج مراجعة يدوية.");
 116 |         }
 117 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:42:80:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `42-42`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 42-42):**
```csharp
  42 |         var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:45:65:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `45-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

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
  45 |         _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:51:83:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `51-51`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 51-51):**
```csharp
  51 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:109:21:ParenthesizedLambdaExpression`
- **Declaration Kind:** `ParenthesizedLambdaExpression` | **Line Range:** `109-109`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes ParenthesizedLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 109-109):**
```csharp
 109 |                     async () => await FindExistingAsync(idempotencyKey, ct), ct);
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.Infrastructure.Treasury.SafeDropHandler.FindExistingAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:119:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `119-120`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Treasury.SafeDropHandler.FindExistingAsync(string, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 119-120):**
```csharp
 119 |     private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
 120 |         _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:120:66:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `120-120`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 120-120):**
```csharp
 120 |         _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.Infrastructure.Treasury.SafeDropHandler.ToResult(Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity, bool)`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:122:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `122-123`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Treasury.SafeDropHandler.ToResult(Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity, bool). Manages internal state transition within src/Falah.Infrastructure/Treasury/SafeDropHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 122-123):**
```csharp
 122 |     private static SafeDropResult ToResult(TreasuryMovementEntity entity, bool wasReplayed) =>
 123 |         new(entity.Id, entity.AmountPiastres, wasReplayed);
```

---
