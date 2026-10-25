# Deep Per-Callable Source Audit: `ConfirmElectronicPaymentHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs)
- **Role:** Electronic Payment Confirmation & Deferred Treasury Movement Settlement Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:35:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 11 (1 Constructor, 2 Methods, 8 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `ConfirmElectronicPaymentHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:22:5:ConstructorDeclaration`
- **Signature:** `public ConfirmElectronicPaymentHandler(FalahDbContext db)`
- **Line Span:** Line 22
- **Actual Behavior & Inputs:**
  Receives EF Core `FalahDbContext` via dependency injection and assigns it to private readonly field `_db`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by DI container for `POST /api/orders/confirm-electronic-payment`.
  - Dependencies: `FalahDbContext`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:24:5:MethodDeclaration`
- **Signature:** `public async Task<ConfirmElectronicPaymentResult> HandleAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 24–97
- **Actual Behavior & Inputs:**
  Receives `ConfirmElectronicPaymentRequest(string OrderId, string CashierId)`. Orchestrates:
  1. *Fast Pre-Check (Lines 26–29):* Queries `_db.Orders.AsNoTracking()` for `request.OrderId`. If already confirmed (`ElectronicPaymentStatus == "confirmed"`), delegates to `BuildReplayResultAsync(preCheck, ct)`.
  2. *Cashier & Shift Gates (Lines 31–43):* Queries `_db.Users.AsNoTracking()` for `request.CashierId`. Asserts cashier holds `pos` or `cash_drawer_sale` permission via `AuthorizationPolicy.HasPermission`. Asserts at least one shift is `Status == "Open"`.
  3. *Transaction & Pessimistic Row Lock (Lines 52–60):* Begins transaction with `IsolationLevel.ReadCommitted` and executes raw SQL query:
     `SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}`
     Eagerly includes `Items` and `Refunds`.
  4. *In-Transaction Idempotency Guard (Lines 61–65):* If `orderEntity.ElectronicPaymentStatus == "confirmed"`, rolls back and returns replay result.
  5. *Domain Mutation (Lines 67–73):* Maps entity to domain via `OrderMapper.ToDomain(orderEntity)`, calls `order.ConfirmElectronicPayment()`, extracts `leg = order.ElectronicPayment!`, updates entity `ElectronicPaymentStatus`, `IsPaid`, and increments `Version`.
  6. *Financial Journal Collision Point (Lines 75–78):* Computes `var saleKey = $"sale-{orderEntity.CheckoutRequestId}";` and calls `journal.TryStart(saleKey, "sale")`. If false, throws `ConfirmConflictException`.
  7. *Treasury Movement Creation (Lines 80–84):* Calls `TreasuryMovementFactory.BuildSaleMovement(...)`, adds to `_db.TreasuryMovements`, and marks journal entry complete (`journal.Complete(saleKey)`).
  8. *Commit & Persistence (Lines 86–88):* Saves changes and commits transaction.
  9. *Catch & Replay Recovery (Lines 90–96):* Catches `DbUpdateException`, `DbUpdateConcurrencyException`, or `ConfirmConflictException`. Rolls back transaction, checks if order was committed as confirmed, and returns replay or rethrows `ConfirmConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/orders/confirm-electronic-payment` API endpoint.
  - Dependencies: `FalahDbContext`, `AuthorizationPolicy`, `OrderMapper`, `Order`, `TreasuryMovementFactory`, `EfFinancialJournal`.
  - Side effects: Updates `Orders` (`ElectronicPaymentStatus`, `IsPaid`, `Version`), inserts `TreasuryMovements` (`sale_collection`), inserts/updates `FinancialJournals`.
- **Critical Financial Integrity Flaws:**

  > [!CRITICAL]
  > **Vulnerability `FIN-001` (Split Payment Idempotency Key Lockout):**
  > Lines 75–78 execute:
  > ```csharp
  > var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
  > var journal = new EfFinancialJournal(_db);
  > if (!journal.TryStart(saleKey, "sale"))
  >     throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل رغم أن الدفع لم يكن مؤكدًا - يحتاج مراجعة يدوية.");
  > ```
  > As discovered in [`CheckoutOrderHandler.cs:148-154`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L148-L154), when an order is created with `PaymentMethod.Multi` with split cash + electronic payments, `plan.DefersAllMoney` is `false`. `CheckoutOrderHandler` immediately calls `journal.TryStart($"sale-{request.CheckoutRequestId}", "sale")` and completes it for the cash split.
  > When `ConfirmElectronicPaymentHandler` subsequently executes for that order, `journal.TryStart` returns `false` because `sale-{orderEntity.CheckoutRequestId}` already exists in status `Completed`.
  > Line 78 throws `ConfirmConflictException`. In the catch block (lines 90–96), the transaction is rolled back. `reloaded.ElectronicPaymentStatus` is NOT `confirmed`, so it throws `ConfirmConflictException` to the client.
  > **Result:** Electronic confirmation of split-payment orders fails permanently with HTTP 409 Conflict. The electronic funds cannot be recorded, and the order remains uncompleted.

  > [!HIGH]
  > **Shift Attribution vs Open Shift Discrepancy:**
  > At line 42, the handler asserts that *any* shift in the restaurant is open:
  > `if (!await _db.Shifts.AsNoTracking().AnyAsync(s => s.Status == "Open", ct))`
  > But at line 82, the resulting `TreasuryMovement` is attributed to `orderEntity.ShiftId`:
  > ```csharp
  > var movement = TreasuryMovementFactory.BuildSaleMovement(
  >     $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", orderEntity.CheckoutRequestId, orderEntity.Id,
  >     leg.Method, leg.Amount, orderEntity.BusinessDayId, orderEntity.ShiftId, cashierUser.Name, DateTimeOffset.UtcNow);
  > ```
  > If an order was opened during Shift A (e.g. afternoon shift), but the electronic confirmation arrives during Shift B (evening shift, where Shift A is already closed and reconciled), the money is posted with `ShiftId = Shift A`. Shift A's closed books are altered post-reconciliation, and Shift B's cashiers who actually verified and received the payment get no shift credit!

- **Concurrency & Locking Evaluation:**
  - Row-level locking on `Orders`: Excellent implementation using `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` inside `IsolationLevel.ReadCommitted`. This guarantees that concurrent confirmations on the same order do not execute simultaneously and prevents race conditions on `Order.Version` and `IsPaid`.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/ConfirmElectronicPaymentTests.cs` (verifies single-method instapay confirm, retry replay, and concurrent confirm serialization).
  - Missing Regression: Integration test checking out a split payment (`cash` + `instapay`) and attempting confirmation via `ConfirmElectronicPaymentHandler` — currently absent, which allowed `FIN-001` to remain undetected.
- **Classification:** Core payment confirmation orchestrator with critical split-payment defect (`FIN-001`).

---

### 3. Pre-Check Order Lookup Lambda (`o => o.Id == request.OrderId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:26:76:SimpleLambdaExpression`
- **Signature:** `o => o.Id == request.OrderId`
- **Line Span:** Line 26
- **Actual Behavior & Inputs:**
  Expression predicate for `_db.Orders.AsNoTracking().FirstOrDefaultAsync(...)`.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `InvalidOperationException("الطلب غير موجود.")` if order does not exist.
- **Classification:** Query filter lambda.

---

### 4. Cashier Lookup Lambda (`u => u.Id == request.CashierId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:31:78:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.CashierId`
- **Line Span:** Line 31
- **Actual Behavior & Inputs:**
  Filters `_db.Users.AsNoTracking()` to retrieve the confirming cashier.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `InvalidOperationException("الكاشير غير موجود.")` if missing.
- **Classification:** Query filter lambda.

---

### 5. Active Shift Check Lambda (`s => s.Status == "Open"`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:42:55:SimpleLambdaExpression`
- **Signature:** `s => s.Status == "Open"`
- **Line Span:** Line 42
- **Actual Behavior & Inputs:**
  Filters `_db.Shifts.AsNoTracking().AnyAsync(...)` to ensure operations only occur during an active shift.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `DomainException("يجب فتح وردية قبل تأكيد التحصيل.")` if no shift is open.
- **Classification:** Query filter lambda.

---

### 6. Items Navigation Include Lambda (`o => o.Items`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:57:26:SimpleLambdaExpression`
- **Signature:** `o => o.Items`
- **Line Span:** Line 57
- **Actual Behavior & Inputs:**
  Loads `Items` navigation on the locked `OrderEntity`.
- **Classification:** ORM mapping lambda.

---

### 7. Refunds Navigation Include Lambda (`o => o.Refunds`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:57:48:SimpleLambdaExpression`
- **Signature:** `o => o.Refunds`
- **Line Span:** Line 57
- **Actual Behavior & Inputs:**
  Loads `Refunds` navigation on the locked `OrderEntity` for complete domain reconstruction in `OrderMapper.ToDomain`.
- **Classification:** ORM mapping lambda.

---

### 8. Catch Block Reload Lambda (`o => o.Id == request.OrderId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:93:80:SimpleLambdaExpression`
- **Signature:** `o => o.Id == request.OrderId`
- **Line Span:** Line 93
- **Actual Behavior & Inputs:**
  Reloads the order without tracking after transaction rollback to determine if a concurrent transaction succeeded in confirming the payment.
- **Classification:** Query filter lambda.

---

### 9. `BuildReplayResultAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:99:5:MethodDeclaration`
- **Signature:** `private async Task<ConfirmElectronicPaymentResult> BuildReplayResultAsync(OrderEntity order, CancellationToken ct)`
- **Line Span:** Lines 99–105
- **Actual Behavior & Inputs:**
  Accepts confirmed `OrderEntity`. Queries `_db.TreasuryMovements.AsNoTracking()` to locate the posted `sale_collection` movement associated with the order. Returns `ConfirmElectronicPaymentResult` with `WasReplayed = true`.
- **Callers, Dependencies & Side Effects:**
  - Invoked at line 29 (fast pre-check) and line 64 / line 94 (post-lock or post-catch replay).
  - Dependencies: `FalahDbContext`.
  - Side effects: None (read query).
- **Subtle Multi-Split Movement Query Defect:**
  At lines 101–103:
  ```csharp
  var movementId = await _db.TreasuryMovements.AsNoTracking()
      .Where(m => m.ReferenceId == order.Id && m.Type == "sale_collection")
      .Select(m => m.Id).FirstOrDefaultAsync(ct);
  ```
  If an order was a split payment with both cash and electronic components, there are **two** `sale_collection` movements for `order.Id`: the initial cash movement posted at checkout, and the electronic movement posted on confirmation. `FirstOrDefaultAsync` has no `OrderBy` or method filter and can nondeterministically return the cash movement ID instead of the electronic movement ID in replay responses!
- **Classification:** Idempotent replay query method.

---

### 10. Replay Movement Filter Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:102:20:SimpleLambdaExpression`
- **Signature:** `m => m.ReferenceId == order.Id && m.Type == "sale_collection"`
- **Line Span:** Line 102
- **Actual Behavior & Inputs:**
  Filters `TreasuryMovements` by `ReferenceId == order.Id` and `Type == "sale_collection"`.
- **Classification:** Query filter lambda.

---

### 11. Replay Movement ID Selector Lambda (`m => m.Id`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs:103:21:SimpleLambdaExpression`
- **Signature:** `m => m.Id`
- **Line Span:** Line 103
- **Actual Behavior & Inputs:**
  Projects `TreasuryMovementEntity` to its primary key string.
- **Classification:** In-memory / SQL projection lambda.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-001`** | **CRITICAL** | `ConfirmElectronicPaymentHandler.cs:75-78` | Electronic confirmation of split payments is completely impossible; throws `ConfirmConflictException` due to journal key reuse. | Change confirmation idempotency key to `sale-confirm-{orderEntity.Id}` or `sale-{orderEntity.CheckoutRequestId}-electronic`, decoupling it from checkout's primary journal entry. |
| **`FIN-ATTR-001`** | **HIGH** | `ConfirmElectronicPaymentHandler.cs:82` | Electronic payment confirmed during a subsequent shift posts `TreasuryMovement` to the order's original `ShiftId`, corrupting closed shift reconciliation. | Attribute electronic collection movement to the currently active shift (`activeShift.Id`), or record operational shift linkage matching `RefundOrderHandler`'s `RefundRules.ResolveOperationalShift` design. |
| **`FIN-REPLAY-002`** | **MEDIUM** | `ConfirmElectronicPaymentHandler.cs:101-103` | Replay query on split orders returns arbitrary movement ID (cash vs electronic) due to unordered `FirstOrDefaultAsync`. | Filter replay query by payment method or movement reference subtype: `.Where(m => m.ReferenceId == order.Id && m.Type == "sale_collection" && (m.Method == "instapay" || m.Method == "wallet"))`. |
