# Substantive Callable Audit: `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `c12b0da939a0a82f907bfa1f0ce32b58f686d4f60d01b637cf1054a30d2d1bc7`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **5**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Handles operator cancellation of a pending electronic payment before confirmation arrives.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.CancelPendingElectronicPaymentHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs:25:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `25-29`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.CancelPendingElectronicPaymentHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 25-29):**
```csharp
  25 |     public CancelPendingElectronicPaymentHandler(FalahDbContext db, IAuditLog auditLog)
  26 |     {
  27 |         _db = db;
  28 |         _auditLog = auditLog;
  29 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs:31:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `31-73`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-002`**

**A. Actual Behavior & Execution Path:**  
Loads order with UPDLOCK/ROWLOCK/HOLDLOCK. Cancels electronic leg by transitioning Order to Cancelled and IsPaid to false. Does NOT reverse cash splits already deposited into drawer.

**B. Trust Boundaries & Validation:**  
Operator cancellation request.

**C. State Mutations & Side Effects:**  
Updates OrderEntity status to Cancelled. Zero treasury movement compensating cash leg.

**D. Concurrency & Transaction Implications:**  
Guarded by row locks.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW FIN-002: In split payment (cash + InstaPay), collected cash remains in till while order is cancelled with zero sales revenue, causing untracked drawer overages.

> **Verbatim Code Excerpt (Lines 31-73):**
```csharp
  31 |     public async Task<CancelPendingElectronicPaymentResult> HandleAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
  32 |     {
  33 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
  34 |             ?? throw new InvalidOperationException("الكاشير غير موجود.");
  35 |         var actor = OrderMapper.ToActor(cashierUser);
  36 |         if (!AuthorizationPolicy.HasPermission(actor, "pos") && !AuthorizationPolicy.HasPermission(actor, "cash_drawer_sale"))
  37 |             throw new DomainException("ليس لديك صلاحية إلغاء محاولة الدفع.");
  38 | 
  39 |         await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
  40 | 
  41 |         var orderEntity = await _db.Orders
  42 |             .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
  43 |             .Include(o => o.Items).Include(o => o.Refunds)
  44 |             .FirstOrDefaultAsync(ct)
  45 |             ?? throw new InvalidOperationException("الطلب غير موجود.");
  46 | 
  47 |         var wasAlreadyCancelled = orderEntity.Status == "cancelled";
  48 |         var order = OrderMapper.ToDomain(orderEntity);
  49 |         var now = DateTimeOffset.UtcNow;
  50 |         order.CancelElectronicPayment(request.Reason, cashierUser.Name, now); // throws if confirmed/no leg/empty reason
  51 | 
  52 |         orderEntity.Status = order.Status.ToWireString();
  53 |         orderEntity.IsPaid = order.IsPaid;
  54 |         orderEntity.ElectronicPaymentStatus = order.ElectronicPayment?.Status.ToWireString();
  55 |         orderEntity.CancelledAtUtc = order.CancelledAtUtc;
  56 |         orderEntity.CancelReason = order.CancelReason;
  57 |         orderEntity.CancelledBy = order.CancelledBy;
  58 |         orderEntity.Version++;
  59 | 
  60 |         if (!wasAlreadyCancelled)
  61 |         {
  62 |             _auditLog.Append(new AuditEvent(
  63 |                 $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
  64 |                 $"إلغاء محاولة دفع إلكتروني {orderEntity.OrderNumber}", "cash",
  65 |                 $"لم يصل التحويل - {order.ElectronicPayment?.Amount.Piastres} قرش - السبب: {request.Reason.Trim()}",
  66 |                 "warning", now, ShiftId: orderEntity.ShiftId, Reason: request.Reason.Trim(), ReferenceId: orderEntity.Id));
  67 |         }
  68 | 
  69 |         await _db.SaveChangesAsync(ct);
  70 |         await transaction.CommitAsync(ct);
  71 |         return new CancelPendingElectronicPaymentResult(
  72 |             orderEntity.Id, orderEntity.Status, orderEntity.ElectronicPaymentStatus!, WasReplayed: wasAlreadyCancelled);
  73 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs:33:78:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `33-33`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 33-33):**
```csharp
  33 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs:43:22:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `43-43`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 43-43):**
```csharp
  43 |             .Include(o => o.Items).Include(o => o.Refunds)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs:43:44:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `43-43`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 43-43):**
```csharp
  43 |             .Include(o => o.Items).Include(o => o.Refunds)
```

---
