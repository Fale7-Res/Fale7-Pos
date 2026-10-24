# Substantive Callable Audit: `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `34ed4f90f854e21b20c18b1c708754d010dc9b3313c648c5b29be1af08844306`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **16**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Supplier purchasing payment settlement handler disbursing cash or bank transfers against supplier debt invoices.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Purchasing.PayPurchaseHandler.PayPurchaseHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:27:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `27-32`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Purchasing.PayPurchaseHandler.PayPurchaseHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 27-32):**
```csharp
  27 |     public PayPurchaseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
  28 |     {
  29 |         _db = db;
  30 |         _approvalRegistry = approvalRegistry;
  31 |         _auditLog = auditLog;
  32 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Purchasing.PayPurchaseHandler.HandleAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:34:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `34-149`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-005`**

**A. Actual Behavior & Execution Path:**  
Reads supplier invoice balance without UPDLOCK/serializable transaction. Verifies payment amount <= remaining balance. Deducts cash from drawer/treasury and commits payment.

**B. Trust Boundaries & Validation:**  
Operator supplier payment command.

**C. State Mutations & Side Effects:**  
Inserts PurchasePaymentEntity, TreasuryMovementEntity, updates PurchaseEntity.PaidAmountPiastres.

**D. Concurrency & Transaction Implications:**  
Lack of row lock allows concurrent payments to race and overpay supplier invoices.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW FIN-005: Concurrency race condition allowing double payments beyond outstanding invoice balance.

> **Verbatim Code Excerpt (Lines 34-149):**
```csharp
  34 |     public async Task<PurchaseDto> HandleAsync(PayPurchaseRequest request, CancellationToken ct = default)
  35 |     {
  36 |         var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
  37 |             .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct)
  38 |             ?? throw new InvalidOperationException("فاتورة الشراء غير موجودة.");
  39 |         if (purchase.Status == "cancelled") throw new DomainException("فاتورة الشراء غير متاحة للسداد.");
  40 | 
  41 |         var idempotencyKey = $"purchase-payment-{purchase.Id}-{request.RequestId}";
  42 |         var existingPayment = await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
  43 |         if (existingPayment is not null) return ReceivePurchaseHandler.ToDto(purchase, wasReplayed: true);
  44 | 
  45 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  46 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  47 |         var actor = OrderMapper.ToActor(actorUser);
  48 | 
  49 |         ManagerApproval? approval = null;
  50 |         if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  51 |         {
  52 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  53 |                 ?? throw new DomainException("المعتمد غير موجود.");
  54 |             approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  55 |                 approverUser.Role, DateTimeOffset.UtcNow, "purchasing_manage", purchase.InvoiceNumber);
  56 |         }
  57 |         var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "purchasing_manage", approval, requireApproval: true);
  58 | 
  59 |         var ledgerEntries = await _db.SupplierLedgerEntries.AsNoTracking()
  60 |             .Where(e => e.SupplierId == purchase.SupplierId).ToListAsync(ct);
  61 |         var outstanding = PurchasingRules.SupplierOutstandingFromLedger(
  62 |             ledgerEntries.Select(e => new SupplierLedgerEntry(e.SupplierId, e.PurchaseId, e.ReferenceId, Money.FromPiastres(e.DebitPiastres), Money.FromPiastres(e.CreditPiastres))),
  63 |             purchase.SupplierId, purchase.Id);
  64 | 
  65 |         var amount = Money.FromPiastres(request.AmountPiastres);
  66 |         PurchasingRules.AssertPurchasePayment(amount, outstanding);
  67 |         if (!amount.IsPositive) throw new DomainException("مبلغ السداد يجب أن يكون أكبر من صفر.");
  68 | 
  69 |         var accountId = PurchasingRules.PurchasePaymentAccountFor(request.PaymentMethod)
  70 |             ?? throw new DomainException("يجب تحديد حساب دفع فعلي.");
  71 |         var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  72 | 
  73 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  74 |         try
  75 |         {
  76 |             // Read under a lock, inside the transaction, immediately before spending.
  77 |             // Outside it, concurrent commands each read the same balance, each decide it covers
  78 |             // them, and each write - which is how an account holding 30 paid out three separate
  79 |             // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
  80 |             var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, accountId, ct);
  81 |             FinancialRules.AssertProtectedSourceBalance(accountMovements, accountId, amount);
  82 | 
  83 |             var journal = new Falah.Infrastructure.Persistence.EfFinancialJournal(_db);
  84 |             if (!journal.TryStart(idempotencyKey, "purchase_payment"))
  85 |                 throw new PurchasingConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
  86 | 
  87 |             var now = DateTimeOffset.UtcNow;
  88 |             var operationId = $"op-{Guid.NewGuid():N}";
  89 |             var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
  90 |                 $"tm-{Guid.NewGuid():N}", idempotencyKey, operationId, "purchase_payment",
  91 |                 accountId, destinationAccountId: null, amount, "purchase_payment", purchase.Id, businessDayId, shiftId: null,
  92 |                 $"سداد مورد للفاتورة {purchase.InvoiceNumber ?? purchase.Id}", actorUser.Name, authorization.Approver?.ApproverName, now);
  93 |             _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
  94 | 
  95 |             var remaining = outstanding - amount;
  96 |             purchase.PaidPiastres += amount.Piastres;
  97 |             purchase.RemainingAmountPiastres = remaining.Piastres;
  98 |             purchase.PaymentStatus = remaining.Piastres == 0 ? "paid" : "partially_paid";
  99 |             purchase.Status = purchase.PaymentStatus;
 100 |             purchase.Payments.Add(new PurchasePaymentEntity
 101 |             {
 102 |                 Id = $"payment-{request.RequestId}", IdempotencyKey = idempotencyKey, PurchaseId = purchase.Id,
 103 |                 SupplierId = purchase.SupplierId, AmountPiastres = amount.Piastres, PaymentAccountId = accountId,
 104 |                 TreasuryMovementId = movement.Id, OperationId = operationId, CreatedBy = actorUser.Name,
 105 |                 ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
 106 |             });
 107 | 
 108 |             _db.SupplierLedgerEntries.Add(new SupplierLedgerEntryEntity
 109 |             {
 110 |                 Id = $"ledger-{idempotencyKey}", IdempotencyKey = idempotencyKey, SupplierId = purchase.SupplierId,
 111 |                 Type = "supplier_payment", DebitPiastres = 0, CreditPiastres = amount.Piastres, PurchaseId = purchase.Id,
 112 |                 ReferenceId = purchase.Id, Reason = "سداد لاحق لفاتورة مشتريات", CreatedBy = actorUser.Name,
 113 |                 OperationId = operationId, TreasuryMovementId = movement.Id, CreatedAtUtc = now,
 114 |             });
 115 | 
 116 |             _auditLog.Append(new AuditEvent(
 117 |                 $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
 118 |                 "سداد مورد", "cash", $"سداد {amount.Piastres} قرش لفاتورة {purchase.Id} من {accountId}",
 119 |                 "normal", now, AuthorizedBy: authorization.Approver?.ApproverName, OperationId: operationId, ReferenceId: purchase.Id));
 120 | 
 121 |             journal.Complete(idempotencyKey);
 122 |             await _db.SaveChangesAsync(ct);
 123 |             await transaction.CommitAsync(ct);
 124 |             return ReceivePurchaseHandler.ToDto(purchase, wasReplayed: false);
 125 |         }
 126 |         catch (Exception ex) when (ex is DbUpdateException or PurchasingConflictException
 127 |                                    || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 128 |         {
 129 |             await transaction.RollbackAsync(ct);
 130 |             var replay = await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
 131 |             // The loser of a balance-lock race carries the same request id as the sibling that
 132 |             // won it, and that sibling may not have committed yet at this instant. Looking once
 133 |             // and giving up turns "somebody else is doing it" into an error.
 134 |             if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 135 |                 replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
 136 |                     async () => await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct), ct);
 137 |             if (replay is not null)
 138 |             {
 139 |                 var refreshed = await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
 140 |                     .FirstAsync(p => p.Id == purchase.Id, ct);
 141 |                 return ReceivePurchaseHandler.ToDto(refreshed, wasReplayed: true);
 142 |             }
 143 |             // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
 144 |             // review a half-finished movement would send them looking for something that is not there.
 145 |             if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
 146 |                 throw new PurchasingConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
 147 |             throw new PurchasingConflictException("تعذر تسجيل سداد المورد بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
 148 |         }
 149 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:36:52:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `36-36`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 36-36):**
```csharp
  36 |         var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:36:74:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `36-36`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 36-36):**
```csharp
  36 |         var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:37:34:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `37-37`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

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
  37 |             .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct)
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:42:93:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `42-42`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

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
  42 |         var existingPayment = await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
```

---

<a id="callable-7"></a>
### Callable 7: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:45:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `45-45`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

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
  45 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:52:83:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `52-52`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

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
  52 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
```

---

<a id="callable-9"></a>
### Callable 9: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:60:20:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `60-60`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 60-60):**
```csharp
  60 |             .Where(e => e.SupplierId == purchase.SupplierId).ToListAsync(ct);
```

---

<a id="callable-10"></a>
### Callable 10: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:62:34:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `62-62`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 62-62):**
```csharp
  62 |             ledgerEntries.Select(e => new SupplierLedgerEntry(e.SupplierId, e.PurchaseId, e.ReferenceId, Money.FromPiastres(e.DebitPiastres), Money.FromPiastres(e.CreditPiastres))),
```

---

<a id="callable-11"></a>
### Callable 11: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:130:88:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `130-130`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 130-130):**
```csharp
 130 |             var replay = await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct);
```

---

<a id="callable-12"></a>
### Callable 12: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:136:21:ParenthesizedLambdaExpression`
- **Declaration Kind:** `ParenthesizedLambdaExpression` | **Line Range:** `136-136`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ParenthesizedLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 136-136):**
```csharp
 136 |                     async () => await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct), ct);
```

---

<a id="callable-13"></a>
### Callable 13: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:136:95:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `136-136`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 136-136):**
```csharp
 136 |                     async () => await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct), ct);
```

---

<a id="callable-14"></a>
### Callable 14: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:139:76:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `139-139`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 139-139):**
```csharp
 139 |                 var refreshed = await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
```

---

<a id="callable-15"></a>
### Callable 15: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:139:98:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `139-139`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 139-139):**
```csharp
 139 |                 var refreshed = await _db.Purchases.AsNoTracking().Include(p => p.Lines).Include(p => p.Payments)
```

---

<a id="callable-16"></a>
### Callable 16: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:140:33:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `140-140`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 140-140):**
```csharp
 140 |                     .FirstAsync(p => p.Id == purchase.Id, ct);
```

---
