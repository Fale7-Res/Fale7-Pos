# Substantive Callable Audit: `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `cac1f0a97612efb966710e6a47e323c2fff06b342c688f419298e34e18c34a0b`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **11**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Electronic payment settlement handler confirming pending card/InstaPay/wallet tender legs after checkout. Reconciles electronic leg into TreasuryMovement and EfFinancialJournal.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.ConfirmElectronicPaymentHandler(Falah.Infrastructure.Persistence.FalahDbContext)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:22:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `22-22`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.ConfirmElectronicPaymentHandler(Falah.Infrastructure.Persistence.FalahDbContext). Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 22-22):**
```csharp
  22 |     public ConfirmElectronicPaymentHandler(FalahDbContext db) => _db = db;
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:24:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `24-97`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-001`**

**A. Actual Behavior & Execution Path:**  
Locks order row with UPDLOCK/ROWLOCK/HOLDLOCK. Verifies order is unpaid with pending electronic leg. Calls order.ConfirmElectronicPayment(). Constructs saleKey = 'sale-{orderEntity.CheckoutRequestId}' and calls journal.TryStart(saleKey, 'sale'). Throws ConfirmConflictException if key exists.

**B. Trust Boundaries & Validation:**  
Cashier confirmation request with reference transaction ID.

**C. State Mutations & Side Effects:**  
Updates OrderEntity.ElectronicPaymentStatus, OrderEntity.IsPaid, appends TreasuryMovementEntity, commits journal.

**D. Concurrency & Transaction Implications:**  
UPDLOCK prevents race conditions on confirmation.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW FIN-001: Because CheckoutOrderHandler claimed 'sale-{CheckoutRequestId}', journal.TryStart returns false! The electronic leg can NEVER be confirmed.

> **Verbatim Code Excerpt (Lines 24-97):**
```csharp
  24 |     public async Task<ConfirmElectronicPaymentResult> HandleAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
  25 |     {
  26 |         var preCheck = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
  27 |             ?? throw new InvalidOperationException("الطلب غير موجود.");
  28 |         if (preCheck.ElectronicPaymentStatus == "confirmed")
  29 |             return await BuildReplayResultAsync(preCheck, ct);
  30 | 
  31 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
  32 |             ?? throw new InvalidOperationException("الكاشير غير موجود.");
  33 | 
  34 |         // Ported from confirmElectronicPayment in lib/store.tsx: `pos` or `cash_drawer_sale`, and
  35 |         // an active shift, both required before any money is confirmed/posted - a real gap found
  36 |         // during the legacy-parity audit, since neither check existed here at all: any caller who
  37 |         // knew a valid orderId + any existing cashierId could confirm a claimed electronic payment
  38 |         // and post real money with zero authorization.
  39 |         var actor = OrderMapper.ToActor(cashierUser);
  40 |         if (!AuthorizationPolicy.HasPermission(actor, "pos") && !AuthorizationPolicy.HasPermission(actor, "cash_drawer_sale"))
  41 |             throw new DomainException("ليس لديك صلاحية تأكيد التحصيل الإلكتروني.");
  42 |         if (!await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == "Open", ct))
  43 |             throw new DomainException("يجب فتح وردية قبل تأكيد التحصيل.");
  44 | 
  45 |         // The initial read below takes an UPDLOCK (plus ROWLOCK/HOLDLOCK) hint - SQL Server's
  46 |         // idiomatic "select for update" - so a second concurrent confirm on the same order blocks
  47 |         // right there instead of both reading "still pending" and both trying to post the
  48 |         // movement. Plain IsolationLevel.Serializable was deliberately avoided: on SQL Server it
  49 |         // takes locks progressively rather than upfront, which can deadlock two concurrent
  50 |         // read-then-write transactions on the same row instead of simply queuing one behind the
  51 |         // other the way SQLite's BEGIN IMMEDIATE did.
  52 |         await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
  53 |         try
  54 |         {
  55 |             var orderEntity = await _db.Orders
  56 |                 .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
  57 |                 .Include(o => o.Items).Include(o => o.Refunds)
  58 |                 .FirstOrDefaultAsync(ct)
  59 |                 ?? throw new InvalidOperationException("الطلب غير موجود.");
  60 | 
  61 |             if (orderEntity.ElectronicPaymentStatus == "confirmed")
  62 |             {
  63 |                 await transaction.RollbackAsync(ct);
  64 |                 return await BuildReplayResultAsync(orderEntity, ct);
  65 |             }
  66 | 
  67 |             var order = OrderMapper.ToDomain(orderEntity);
  68 |             order.ConfirmElectronicPayment(); // throws DomainException if there is no pending leg
  69 |             var leg = order.ElectronicPayment!;
  70 | 
  71 |             orderEntity.ElectronicPaymentStatus = leg.Status.ToWireString();
  72 |             orderEntity.IsPaid = order.IsPaid;
  73 |             orderEntity.Version++;
  74 | 
  75 |             var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
  76 |             var journal = new EfFinancialJournal(_db);
  77 |             if (!journal.TryStart(saleKey, "sale"))
  78 |                 throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل رغم أن الدفع لم يكن مؤكدًا - يحتاج مراجعة يدوية.");
  79 | 
  80 |             var movement = TreasuryMovementFactory.BuildSaleMovement(
  81 |                 $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", orderEntity.CheckoutRequestId, orderEntity.Id,
  82 |                 leg.Method, leg.Amount, orderEntity.BusinessDayId, orderEntity.ShiftId, cashierUser.Name, DateTimeOffset.UtcNow);
  83 |             _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
  84 |             journal.Complete(saleKey);
  85 | 
  86 |             await _db.SaveChangesAsync(ct);
  87 |             await transaction.CommitAsync(ct);
  88 |             return new ConfirmElectronicPaymentResult(orderEntity.Id, orderEntity.ElectronicPaymentStatus, orderEntity.IsPaid, movement.Id, WasReplayed: false);
  89 |         }
  90 |         catch (Exception ex) when (ex is DbUpdateException or DbUpdateConcurrencyException or ConfirmConflictException)
  91 |         {
  92 |             await transaction.RollbackAsync(ct);
  93 |             var reloaded = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
  94 |             if (reloaded?.ElectronicPaymentStatus == "confirmed") return await BuildReplayResultAsync(reloaded, ct);
  95 |             throw new ConfirmConflictException("تعذر تأكيد الدفع الإلكتروني بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
  96 |         }
  97 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:26:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `26-26`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 26-26):**
```csharp
  26 |         var preCheck = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:31:78:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `31-31`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 31-31):**
```csharp
  31 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:42:55:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `42-42`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

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
  42 |         if (!await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == "Open", ct))
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:57:26:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `57-57`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

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
  57 |                 .Include(o => o.Items).Include(o => o.Refunds)
```

---

<a id="callable-7"></a>
### Callable 7: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:57:48:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `57-57`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

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
  57 |                 .Include(o => o.Items).Include(o => o.Refunds)
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:93:80:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `93-93`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 93-93):**
```csharp
  93 |             var reloaded = await _db.Orders.AsNoTracking().FirstOrDefaultAsync(o => o.Id == request.OrderId, ct);
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.BuildReplayResultAsync(Falah.Infrastructure.Persistence.Entities.OrderEntity, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:99:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `99-105`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.BuildReplayResultAsync(Falah.Infrastructure.Persistence.Entities.OrderEntity, CancellationToken). Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 99-105):**
```csharp
  99 |     private async Task<ConfirmElectronicPaymentResult> BuildReplayResultAsync(OrderEntity order, CancellationToken ct)
 100 |     {
 101 |         var movementId = await _db.TreasuryMovements.AsNoTracking()
 102 |             .Where(m => m.ReferenceId == order.Id && m.Type == "sale_collection")
 103 |             .Select(m => m.Id).FirstOrDefaultAsync(ct);
 104 |         return new ConfirmElectronicPaymentResult(order.Id, order.ElectronicPaymentStatus!, order.IsPaid, movementId, WasReplayed: true);
 105 |     }
```

---

<a id="callable-10"></a>
### Callable 10: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:102:20:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `102-102`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 102-102):**
```csharp
 102 |             .Where(m => m.ReferenceId == order.Id && m.Type == "sale_collection")
```

---

<a id="callable-11"></a>
### Callable 11: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:103:21:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `103-103`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 103-103):**
```csharp
 103 |             .Select(m => m.Id).FirstOrDefaultAsync(ct);
```

---
