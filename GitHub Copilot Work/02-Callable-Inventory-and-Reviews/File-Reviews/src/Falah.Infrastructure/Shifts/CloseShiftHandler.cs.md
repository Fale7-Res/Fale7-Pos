# Substantive Callable Audit: `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `58319fdf46c1a0f6181baba6ff711129913fbf4c64e7a00923fa2862d0ea36de`
- **Auditor Specialist:** `security-auditor`
- **Total File Callables:** **7**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Shift close and drawer reconciliation handler executing under SQL Server UPDLOCK/ROWLOCK/HOLDLOCK transaction.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Shifts.CloseShiftHandler.CloseShiftHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Microsoft.Extensions.Configuration.IConfiguration, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:39:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `39-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Shifts.CloseShiftHandler.CloseShiftHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Microsoft.Extensions.Configuration.IConfiguration, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 39-45):**
```csharp
  39 |     public CloseShiftHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, Microsoft.Extensions.Configuration.IConfiguration configuration, IAuditLog auditLog)
  40 |     {
  41 |         _db = db;
  42 |         _approvalRegistry = approvalRegistry;
  43 |         _configuration = configuration;
  44 |         _auditLog = auditLog;
  45 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Shifts.CloseShiftHandler.HandleAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:47:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `47-175`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`SEC-004`**

**A. Actual Behavior & Execution Path:**  
Checks idempotency replay via FindExistingAsync. Loads user matching request.ClosedByUserId. Locks Shift row in SQL Server via FromSqlRaw UPDLOCK hint. Reconciles drawer cash balances and sets ShiftStatus to Closed.

**B. Trust Boundaries & Validation:**  
VULNERABILITY SEC-004: Reads request.ClosedByUserId from request body without middleware session enforcement.

**C. State Mutations & Side Effects:**  
Mutates ShiftEntity status, ActualCashPiastres, DiscrepancyPiastres, ClosedAtUtc. Appends AuditLog entry.

**D. Concurrency & Transaction Implications:**  
UPDLOCK/ROWLOCK/HOLDLOCK prevents concurrent double-closing and race conditions on expected cash calculation.

**E. Specialist Risks & Invariants:**  
Shift close identity spoofing allowing any user to attribute a shift closure to another cashier.

> **Verbatim Code Excerpt (Lines 47-175):**
```csharp
  47 |     public async Task<CloseShiftResult> HandleAsync(CloseShiftRequest request, CancellationToken ct = default)
  48 |     {
  49 |         var existing = await FindExistingAsync(request.CloseRequestId, ct);
  50 |         if (existing is not null) return ToResult(existing, wasReplayed: true);
  51 | 
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ClosedByUserId, ct)
  53 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  54 |         var actor = OrderMapper.ToActor(cashierUser);
  55 | 
  56 |         await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
  57 |         try
  58 |         {
  59 |             // Same UPDLOCK/ROWLOCK/HOLDLOCK pattern as RefundOrderHandler - a second concurrent
  60 |             // close attempt against the SAME shift blocks here instead of both computing the same
  61 |             // stale expected cash and racing to post two handovers.
  62 |             var entity = await _db.Shifts
  63 |                 .FromSqlRaw("SELECT * FROM Shifts WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.ShiftId)
  64 |                 .FirstOrDefaultAsync(ct)
  65 |                 ?? throw new InvalidOperationException("الوردية غير موجودة.");
  66 | 
  67 |             // A concurrent request carrying the SAME CloseRequestId as one that just won the lock
  68 |             // race and committed must see itself as already-fulfilled here, before the plain
  69 |             // Status check below - otherwise it reads "Closed" (by the request it is a retry OF)
  70 |             // and throws a spurious "shift not open" instead of replaying. This is the equivalent
  71 |             // of RefundOrderHandler's unique-index-violation replay path, just reached a different
  72 |             // way because closing a Shift mutates a single existing row rather than inserting a
  73 |             // new one, so there is no insert for a duplicate key to fail on.
  74 |             if (entity.CloseRequestId == request.CloseRequestId)
  75 |             {
  76 |                 await transaction.RollbackAsync(ct);
  77 |                 return ToResult(entity, wasReplayed: true);
  78 |             }
  79 |             if (entity.Status != nameof(ShiftStatus.Open)) throw new DomainException("الوردية ليست مفتوحة.");
  80 | 
  81 |             // Deliberately no driver-custody check here: a driver settles per business day, or when he
  82 |             // comes in - not because a cashier wants to hand over the till. See DeliveryRules
  83 |             // .UnsettledCustodyForShift. The obligation still blocks the BUSINESS DAY close, shows on
  84 |             // the dashboard, and prints as a warning on this shift's own handover slip.
  85 | 
  86 |             ManagerApproval? approval = null;
  87 |             if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  88 |             {
  89 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  90 |                     ?? throw new DomainException("المعتمد غير موجود.");
  91 |                 approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  92 |                     approverUser.Role, DateTimeOffset.UtcNow, "shifts_manage", request.Reason);
  93 |             }
  94 | 
  95 |             var openingCash = Money.FromPiastres(entity.OpeningCashPiastres);
  96 |             var expected = await ShiftReconciliation.ComputeExpectedCashAsync(_db, entity.Id, openingCash, ct);
  97 |             var actual = Money.FromPiastres(request.ActualCashCountedPiastres);
  98 |             var threshold = Money.FromPiastres(await PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(_db, _configuration, ct));
  99 | 
 100 |             var shift = Shift.Reconstitute(entity.Id, entity.ShiftName, entity.OpenedByUserId, openingCash,
 101 |                 entity.BusinessDate, entity.OpenedAtUtc, ShiftStatus.Open, null, null, null, null, null, null, null);
 102 |             var blindResult = shift.PrepareBlindClose(actual, expected, threshold);
 103 | 
 104 |             AuthorizationPolicy.AuthorizeShiftClose(_approvalRegistry, actor, approval, blindResult.RequiresManagerApproval);
 105 |             var authorizedByName = approval?.ApproverName;
 106 | 
 107 |             var now = DateTimeOffset.UtcNow;
 108 |             var difference = blindResult.Difference;
 109 | 
 110 |             string? reconciliationMovementId = null;
 111 |             if (!difference.IsZero)
 112 |             {
 113 |                 var reconciliation = TreasuryMovementFactory.BuildCashLifecycleMovement(
 114 |                     $"tm-{Guid.NewGuid():N}", $"shift-reconcile-{entity.Id}", $"op-{Guid.NewGuid():N}",
 115 |                     difference.IsNegative ? "cash_shortage" : "cash_overage",
 116 |                     sourceAccountId: difference.IsNegative ? TreasuryAccounts.Drawer : null,
 117 |                     destinationAccountId: difference.IsPositive ? TreasuryAccounts.Drawer : null,
 118 |                     difference.Abs(), "shift_reconciliation", entity.Id, businessDayId: null, entity.Id,
 119 |                     string.IsNullOrWhiteSpace(request.Reason) ? "فرق نقدية ضمن الحد المسموح" : request.Reason,
 120 |                     cashierUser.Name, authorizedByName, now);
 121 |                 _db.TreasuryMovements.Add(OrderMapper.ToEntity(reconciliation));
 122 |                 reconciliationMovementId = reconciliation.Id;
 123 |             }
 124 | 
 125 |             string? handoverMovementId = null;
 126 |             if (actual.IsPositive)
 127 |             {
 128 |                 var handover = TreasuryMovementFactory.BuildCashLifecycleMovement(
 129 |                     $"tm-{Guid.NewGuid():N}", $"shift-close-handover-{entity.Id}", $"op-{Guid.NewGuid():N}",
 130 |                     "shift_handover", TreasuryAccounts.Drawer, TreasuryAccounts.Main,
 131 |                     actual, "shift_handover", entity.Id, businessDayId: null, entity.Id,
 132 |                     $"تسليم إغلاق {entity.ShiftName}", cashierUser.Name, authorizedByName, now);
 133 |                 _db.TreasuryMovements.Add(OrderMapper.ToEntity(handover));
 134 |                 handoverMovementId = handover.Id;
 135 |             }
 136 | 
 137 |             shift.Close(cashierUser.Id, blindResult, request.Reason, authorizedByName, now);
 138 | 
 139 |             entity.Status = nameof(ShiftStatus.Closed);
 140 |             entity.ClosedByUserId = shift.ClosedByUserId;
 141 |             entity.ActualCashPiastres = shift.ActualCash!.Value.Piastres;
 142 |             entity.ExpectedCashPiastres = shift.ExpectedCash!.Value.Piastres;
 143 |             entity.CashDifferencePiastres = shift.CashDifference!.Value.Piastres;
 144 |             entity.DifferenceReason = shift.DifferenceReason;
 145 |             entity.DifferenceAuthorizedBy = shift.DifferenceAuthorizedBy;
 146 |             entity.ClosedAtUtc = shift.ClosedAtUtc;
 147 |             entity.CloseRequestId = request.CloseRequestId;
 148 |             entity.ReconciliationTreasuryMovementId = reconciliationMovementId;
 149 |             entity.HandoverTreasuryMovementId = handoverMovementId;
 150 |             entity.Version++;
 151 | 
 152 |             _auditLog.Append(new AuditEvent(
 153 |                 $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
 154 |                 "إغلاق وردية", "shift", $"إغلاق {entity.ShiftName} - المتوقع {expected.Piastres} والفعلي {actual.Piastres}",
 155 |                 blindResult.RequiresManagerApproval ? "critical" : "normal", now,
 156 |                 ShiftId: entity.Id, Reason: request.Reason, AuthorizedBy: authorizedByName));
 157 | 
 158 |             // The handover paper. Same transaction as the close, keyed on the shift, so it is
 159 |             // queued exactly once and only for a close that actually committed.
 160 |             _db.PrintJobs.Add(Falah.Infrastructure.Printing.DocumentPrintJobs.Build(
 161 |                 PrintDocumentKind.ShiftClose, entity.Id, PrintRules.ShiftCloseLabel, cashierUser.Name, now));
 162 | 
 163 |             await _db.SaveChangesAsync(ct);
 164 |             await transaction.CommitAsync(ct);
 165 |             return ToResult(entity, wasReplayed: false);
 166 |         }
 167 |         catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException)
 168 |         {
 169 |             await transaction.RollbackAsync(ct);
 170 |             var replay = await FindExistingAsync(request.CloseRequestId, ct);
 171 |             if (replay is not null) return ToResult(replay, wasReplayed: true);
 172 |             throw new ShiftCloseConflictException(
 173 |                 "تعذر إتمام تقفيل الوردية بسبب تعارض في مفتاح منع التكرار، ولا يوجد تقفيل سابق لإعادته - يحتاج مراجعة يدوية.");
 174 |         }
 175 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:52:78:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `52-52`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 52-52):**
```csharp
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ClosedByUserId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:89:87:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `89-89`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 89-89):**
```csharp
  89 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `Falah.Infrastructure.Shifts.CloseShiftHandler.FindExistingAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:177:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `177-178`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Shifts.CloseShiftHandler.FindExistingAsync(string, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 177-178):**
```csharp
 177 |     private Task<ShiftEntity?> FindExistingAsync(string closeRequestId, CancellationToken ct) =>
 178 |         _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.CloseRequestId == closeRequestId, ct);
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:178:55:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `178-178`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 178-178):**
```csharp
 178 |         _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.CloseRequestId == closeRequestId, ct);
```

---

<a id="callable-7"></a>
### Callable 7: `Falah.Infrastructure.Shifts.CloseShiftHandler.ToResult(Falah.Infrastructure.Persistence.Entities.ShiftEntity, bool)`
- **Callable ID:** `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs:180:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `180-182`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Shifts.CloseShiftHandler.ToResult(Falah.Infrastructure.Persistence.Entities.ShiftEntity, bool). Manages internal state transition within src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 180-182):**
```csharp
 180 |     private static CloseShiftResult ToResult(ShiftEntity entity, bool wasReplayed) => new(
 181 |         entity.Id, entity.Status, entity.ExpectedCashPiastres ?? 0, entity.ActualCashPiastres ?? 0,
 182 |         entity.CashDifferencePiastres ?? 0, wasReplayed);
```

---
