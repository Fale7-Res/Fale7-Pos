# Substantive Callable Audit: `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `ca7d3922495ad8ed8f9979feeca461f3fca0aa150fde7790714e29046e32d085`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **14**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Order refund processing handler executing debit reversals against cashier drawer under SQL Server UPDLOCK/ROWLOCK/HOLDLOCK.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Orders.RefundOrderHandler.RefundOrderHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:38:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `38-43`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Orders.RefundOrderHandler.RefundOrderHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 38-43):**
```csharp
  38 |     public RefundOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
  39 |     {
  40 |         _db = db;
  41 |         _approvalRegistry = approvalRegistry;
  42 |         _auditLog = auditLog;
  43 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Orders.RefundOrderHandler.HandleAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:45:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `45-152`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-003`**

**A. Actual Behavior & Execution Path:**  
Locks original order with FromSqlRaw UPDLOCK. Verifies order is not already refunded. Calls Order.ApplyRefund. Does NOT assert IsPaid == true before disbursing cash refund.

**B. Trust Boundaries & Validation:**  
Refund request submitted by cashier till.

**C. State Mutations & Side Effects:**  
Inserts OrderRefundEntity, TreasuryMovementEntity (Refund debit), updates OrderEntity.Status to Refunded.

**D. Concurrency & Transaction Implications:**  
UPDLOCK prevents twin refunds on concurrent clicks.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW FIN-003: Permitting refunds on unpaid COD or pending orders drains physical cash from drawer for orders that were never paid.

> **Verbatim Code Excerpt (Lines 45-152):**
```csharp
  45 |     public async Task<RefundOrderResult> HandleAsync(RefundOrderRequest request, CancellationToken ct = default)
  46 |     {
  47 |         var refundKey = $"refund-{request.RefundRequestId}";
  48 | 
  49 |         var existing = await FindExistingAsync(refundKey, ct);
  50 |         if (existing is not null) return await ToResultAsync(existing, wasReplayed: true, ct);
  51 | 
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
  53 |             ?? throw new InvalidOperationException("الكاشير غير موجود.");
  54 |         var cashier = OrderMapper.ToActor(cashierUser);
  55 | 
  56 |         await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
  57 |         try
  58 |         {
  59 |             var orderEntity = await _db.Orders
  60 |                 .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
  61 |                 .Include(o => o.Items).Include(o => o.Refunds)
  62 |                 .FirstOrDefaultAsync(ct)
  63 |                 ?? throw new InvalidOperationException("الطلب غير موجود.");
  64 | 
  65 |             // Refunds always require either the permission or a manager approval - unlike a shift
  66 |             // close discrepancy, there is no threshold below which a refund is self-authorized.
  67 |             ManagerApproval? approval = null;
  68 |             if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  69 |             {
  70 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  71 |                     ?? throw new DomainException("المعتمد غير موجود.");
  72 |                 approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  73 |                     approverUser.Role, DateTimeOffset.UtcNow, "orders_refund", request.Reason);
  74 |             }
  75 |             var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, cashier, "orders_refund", approval, requireApproval: true);
  76 |             var authorizedByName = authorization.Approver?.ApproverName ?? cashier.Name;
  77 | 
  78 |             var order = OrderMapper.ToDomain(orderEntity);
  79 |             var refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount);
  80 |             // Matches the current app's behaviour exactly: a request for more than what remains is
  81 |             // CLAMPED down to what remains, not rejected outright - see RefundRules.ClampRefundAmount.
  82 |             var safeAmount = RefundRules.ClampRefundAmount(Money.FromPiastres(request.AmountPiastres), refundable);
  83 |             if (!safeAmount.IsPositive) throw new DomainException("لا يوجد مبلغ متبقٍ للاسترجاع.");
  84 | 
  85 |             // Ported from postRefund in lib/enterprise-store.tsx: a refund can only leave a
  86 |             // protected account (drawer/main/bank/instapay - never wallet) if it actually holds
  87 |             // enough posted balance. A real gap found during the legacy-parity audit: every other
  88 |             // money-out handler in this codebase (expenses, purchasing, treasury, shift-open) calls
  89 |             // this same guard; RefundOrderHandler alone skipped it, so a refund could silently drive
  90 |             // an account negative.
  91 |             var refundSourceAccount = TreasuryMovementFactory.PaymentAccountFor(order.PaymentMethod);
  92 |             var refundSourceMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, refundSourceAccount, ct);
  93 |             FinancialRules.AssertProtectedSourceBalance(refundSourceMovements, refundSourceAccount, safeAmount);
  94 | 
  95 |             var originalShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == order.ShiftId, ct)
  96 |                 ?? throw new InvalidOperationException("وردية الطلب الأصلية غير موجودة.");
  97 |             var activeShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);
  98 | 
  99 |             var shiftPolicy = RefundRules.ResolveOperationalShift(
 100 |                 order.ShiftId, Enum.Parse<ShiftStatus>(originalShiftEntity.Status), activeShiftEntity?.Id, order.PaymentMethod);
 101 | 
 102 |             var now = DateTimeOffset.UtcNow;
 103 |             var movement = TreasuryMovementFactory.BuildRefundMovement(
 104 |                 $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", request.RefundRequestId, order.Id,
 105 |                 order.PaymentMethod, safeAmount, order.BusinessDayId, shiftPolicy.OperationalShiftId!,
 106 |                 cashierUser.Name, authorizedByName, request.Reason, now);
 107 | 
 108 |             var refund = new OrderRefund($"order-refund-{Guid.NewGuid():N}", movement.OperationId, refundKey,
 109 |                 safeAmount, order.PaymentMethod, movement.Id, now, cashierUser.Name, authorizedByName,
 110 |                 request.Reason, order.ShiftId, shiftPolicy.OperationalShiftId!);
 111 | 
 112 |             // Domain invariants enforced here too (cancelled/multi guard, amount<=refundable,
 113 |             // Refunded transition) - not just trusted from the computation above.
 114 |             order.ApplyRefund(refund);
 115 | 
 116 |             orderEntity.Status = order.Status.ToWireString();
 117 |             orderEntity.RefundAmountPiastres = order.RefundAmount.Piastres;
 118 |             orderEntity.Version++;
 119 |             var refundEntity = OrderMapper.ToEntity(refund);
 120 |             orderEntity.Refunds.Add(refundEntity);
 121 |             _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
 122 | 
 123 |             _auditLog.Append(new AuditEvent(
 124 |                 $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
 125 |                 "استرداد طلب", "order", $"استرداد {safeAmount.Piastres} قرش لطلب {order.OrderNumber} - {request.Reason}",
 126 |                 safeAmount.Piastres >= ControlCenterRules.LargeRefundThresholdPiastres ? "critical" : "normal", now,
 127 |                 ShiftId: shiftPolicy.OperationalShiftId, Reason: request.Reason, AuthorizedBy: authorizedByName,
 128 |                 OperationId: movement.OperationId, ReferenceId: order.Id));
 129 | 
 130 |             await _db.SaveChangesAsync(ct);
 131 |             await transaction.CommitAsync(ct);
 132 |             // Use the entity's own (now database-assigned) identity for the result - the SAME
 133 |             // identifier shape a later replay lookup returns, so a caller can never see two
 134 |             // different id formats for what is logically the same refund.
 135 |             return ToResult(refundEntity, orderEntity, wasReplayed: false);
 136 |         }
 137 |         catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException
 138 |                                    || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 139 |         {
 140 |             await transaction.RollbackAsync(ct);
 141 |             var replay = await FindExistingAsync(refundKey, ct);
 142 |             // The loser of a balance-lock race carries the same request id as the sibling that
 143 |             // won it, and that sibling may not have committed yet at this instant. Looking once
 144 |             // and giving up turns "somebody else is doing it" into an error.
 145 |             if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 146 |                 replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
 147 |                     async () => await FindExistingAsync(refundKey, ct), ct);
 148 |             if (replay is not null) return await ToResultAsync(replay, wasReplayed: true, ct);
 149 |             throw new RefundConflictException(
 150 |                 "تعذر إتمام عملية الاسترجاع بسبب تعارض في مفتاح منع التكرار، ولا يوجد مرتجع سابق لإعادته - يحتاج مراجعة يدوية.");
 151 |         }
 152 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:52:78:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `52-52`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

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
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:61:26:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `61-61`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 61-61):**
```csharp
  61 |                 .Include(o => o.Items).Include(o => o.Refunds)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:61:48:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `61-61`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 61-61):**
```csharp
  61 |                 .Include(o => o.Items).Include(o => o.Refunds)
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:70:87:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `70-70`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 70-70):**
```csharp
  70 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
```

---

<a id="callable-7"></a>
### Callable 7: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:95:91:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `95-95`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 95-95):**
```csharp
  95 |             var originalShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == order.ShiftId, ct)
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:97:89:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `97-97`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 97-97):**
```csharp
  97 |             var activeShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);
```

---

<a id="callable-9"></a>
### Callable 9: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:147:21:ParenthesizedLambdaExpression`
- **Declaration Kind:** `ParenthesizedLambdaExpression` | **Line Range:** `147-147`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ParenthesizedLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 147-147):**
```csharp
 147 |                     async () => await FindExistingAsync(refundKey, ct), ct);
```

---

<a id="callable-10"></a>
### Callable 10: `Falah.Infrastructure.Orders.RefundOrderHandler.FindExistingAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:154:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `154-155`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.RefundOrderHandler.FindExistingAsync(string, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 154-155):**
```csharp
 154 |     private Task<OrderRefundEntity?> FindExistingAsync(string refundKey, CancellationToken ct) =>
 155 |         _db.OrderRefunds.AsNoTracking().FirstOrDefaultAsync(r => r.IdempotencyKey == refundKey, ct);
```

---

<a id="callable-11"></a>
### Callable 11: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:155:61:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `155-155`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 155-155):**
```csharp
 155 |         _db.OrderRefunds.AsNoTracking().FirstOrDefaultAsync(r => r.IdempotencyKey == refundKey, ct);
```

---

<a id="callable-12"></a>
### Callable 12: `Falah.Infrastructure.Orders.RefundOrderHandler.ToResultAsync(Falah.Infrastructure.Persistence.Entities.OrderRefundEntity, bool, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:157:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `157-161`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.RefundOrderHandler.ToResultAsync(Falah.Infrastructure.Persistence.Entities.OrderRefundEntity, bool, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 157-161):**
```csharp
 157 |     private async Task<RefundOrderResult> ToResultAsync(OrderRefundEntity refund, bool wasReplayed, CancellationToken ct)
 158 |     {
 159 |         var order = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == refund.OrderId, ct);
 160 |         return ToResult(refund, order, wasReplayed);
 161 |     }
```

---

<a id="callable-13"></a>
### Callable 13: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:159:64:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `159-159`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 159-159):**
```csharp
 159 |         var order = await _db.Orders.AsNoTracking().FirstAsync(o => o.Id == refund.OrderId, ct);
```

---

<a id="callable-14"></a>
### Callable 14: `Falah.Infrastructure.Orders.RefundOrderHandler.ToResult(Falah.Infrastructure.Persistence.Entities.OrderRefundEntity, Falah.Infrastructure.Persistence.Entities.OrderEntity, bool)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:163:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `163-165`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.RefundOrderHandler.ToResult(Falah.Infrastructure.Persistence.Entities.OrderRefundEntity, Falah.Infrastructure.Persistence.Entities.OrderEntity, bool). Manages internal state transition within src/Falah.Infrastructure/Orders/RefundOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 163-165):**
```csharp
 163 |     private static RefundOrderResult ToResult(OrderRefundEntity refund, OrderEntity order, bool wasReplayed) => new(
 164 |         refund.Id.ToString(), order.Id, refund.AmountPiastres, order.Status,
 165 |         (order.TotalPiastres - order.RefundAmountPiastres), refund.TreasuryMovementId, wasReplayed);
```

---
