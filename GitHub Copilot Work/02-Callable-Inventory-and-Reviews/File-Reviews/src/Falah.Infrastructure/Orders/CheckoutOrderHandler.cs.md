# Deep Per-Callable Source Audit: `CheckoutOrderHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs)
- **Role:** Primary Order Creation, Checkout Orchestration, Pricing Resolution & Payment Settle Dispatcher
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:30:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 15 (1 Constructor, 6 Methods, 8 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `CheckoutOrderHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:31:5:ConstructorDeclaration`
- **Signature:** `public CheckoutOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, MenuPricingService pricing, InventoryDeductionService inventory, IAuditLog auditLog)`
- **Line Span:** Lines 31–38
- **Actual Behavior & Inputs:**
  Receives and stores DI dependencies into private readonly fields: `_db` (`FalahDbContext`), `_approvalRegistry` (`IApprovalRegistry`), `_pricing` (`MenuPricingService`), `_inventory` (`InventoryDeductionService`), and `_auditLog` (`IAuditLog`).
- **Callers, Dependencies & Side Effects:**
  - Invoked by ASP.NET Core DI container (scoped lifetime) during API route dispatch for `POST /api/orders/checkout`.
  - Side effects: None (field assignments).
- **Trust Boundaries & Failure Modes:**
  Internal application boundary. Throws `ArgumentNullException` if any required dependency fails DI resolution.
- **Financial Integrity Assessment:**
  Neutral. Assembles required services for pricing, ledger persistence, approval authorization, inventory deduction, and audit logging.
- **Classification:** Non-trivial infrastructure wiring.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:40:5:MethodDeclaration`
- **Signature:** `public async Task<CheckoutOrderResult> HandleAsync(CheckoutOrderRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 40–290
- **Actual Behavior & Inputs:**
  Receives `CheckoutOrderRequest` containing checkout parameters: `CheckoutRequestId`, `CashierId`, `ShiftId`, `OrderType`, `PaymentMethod`, `Items`, `DiscountPiastres`, `DeliveryFeePiastres`, `CompAmountPiastres`, `CompApprovalId`, `CompApproverUserId`, `CompReason`, `AmountPaidPiastres`, `SplitPayments`, `WalletProvider`, delivery details, and customer info. Orchestrates:
  1. *Fast Idempotent Replay Check (Lines 44–45):* Calls `FindExistingAsync(request.CheckoutRequestId, ct)`. If an order exists, immediately returns `ToResult(existing, wasReplayed: true)`.
  2. *Shift & Cashier Existence Checks (Lines 47–55):* Reads `Shifts` and `Users` with `.AsNoTracking()`.
  3. *Pricing Resolution (Lines 56–59):* Resolves menu prices, modifiers, and bundle pricing via `_pricing.ResolveAsync`.
  4. *Tax Rule Lookup (Lines 60–64):* Fetches active `TaxRule` with `.AsNoTracking()`.
  5. *Delivery & Pickup Field Gate (Lines 80–100):* Validates address requirements for `Delivery` and `Pickup`, calls `CheckoutRules.AssertRequestedPickupTime`.
  6. *Permission Authorization (Lines 107–113):* Asserts cashier holds `pos` permission, and if `discount.IsPositive`, asserts `orders_discount` permission.
  7. *Manager Comp Approval Gate (Lines 116–140):* If `compAmount.IsPositive`, validates comp amount against pre-tax value, validates reason code, fetches approver from `Users`, and calls `AuthorizationPolicy.ValidateApproval(_approvalRegistry, "orders_comp", approval)`.
  8. *Domain Plan Construction (Lines 142–143):* Invokes pure domain `CheckoutRules.BuildPlan(...)` to calculate totals, change due, payable total, and deferred settlement flags.
  9. *Database Transaction (Lines 145–281):* Begins explicit transaction `await _db.Database.BeginTransactionAsync(ct)`.
  10. *Financial Journal Start (Lines 148–154):* If `!plan.DefersAllMoney`, generates `saleKey = $"sale-{request.CheckoutRequestId}"` and executes `journal.TryStart(saleKey, "sale")`.
  11. *Sequence & Entity Creation (Lines 156–193):* Allocates next sequence number (`OrderSequenceService.AllocateNextAsync`), builds domain `Order.Create(...)`, maps to `OrderEntity`, populates structured delivery fields, and adds to `_db.Orders`.
  12. *Audit Trail (Lines 195–216):* Appends comp or discount audit events to `_auditLog`.
  13. *Delivery Entity & Print Job (Lines 223–246):* Inserts `DeliveryEntity` and enqueues `PrintJobEntity` for delivery orders.
  14. *Inventory Deduction (Lines 251):* Deducts stock via `_inventory.DeductForOrderAsync`.
  15. *Treasury Movement (Lines 253–276):* If `!plan.DefersAllMoney`:
      - If `PaymentMethod.Multi`: loops over splits; for non-electronic splits (`Cash`, `Card`), generates `TreasuryMovementFactory.BuildSaleMovement(...)`.
      - If single non-deferred method (`Cash`, `Card`): generates single `TreasuryMovement`.
      - Calls `new EfFinancialJournal(_db).Complete(saleKey)`.
  16. *Persistence & Commit (Lines 278–280):* Calls `_db.SaveChangesAsync(ct)` and `transaction.CommitAsync(ct)`.
  17. *Catch Block (Lines 282–289):* On `DbUpdateException` or `CheckoutConflictException`, rolls back transaction, checks `FindExistingAsync`, returns replay if found, or throws `CheckoutConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/orders/checkout` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `MenuPricingService`, `InventoryDeductionService`, `IAuditLog`, `CheckoutRules`, `PricingRules`, `AuthorizationPolicy`, `TaxEngine`, `OrderSequenceService`, `TreasuryMovementFactory`, `EfFinancialJournal`.
  - Side effects: Inserts into `Orders`, `OrderItems`, `TreasuryMovements`, `FinancialJournals`, `Deliveries`, `PrintJobs`, `AuditEvents`, updates `InventoryStocks` and `OrderSequences`.
- **Critical Financial Integrity Flaws:**

  > [!CRITICAL]
  > **Vulnerability `FIN-001` (Split Payment Idempotency Key Collision):**
  > At lines 148–154:
  > ```csharp
  > var saleKey = $"sale-{request.CheckoutRequestId}";
  > if (!plan.DefersAllMoney)
  > {
  >     var journal = new EfFinancialJournal(_db);
  >     if (!journal.TryStart(saleKey, "sale"))
  >         throw new CheckoutConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
  > }
  > ...
  > if (!plan.DefersAllMoney)
  > {
  >     ...
  >     new EfFinancialJournal(_db).Complete(saleKey);
  > }
  > ```
  > When an order is checked out using `PaymentMethod.Multi` with a split between Cash and an electronic method (e.g. 50 EGP Cash + 50 EGP InstaPay), `plan.DefersAllMoney` evaluates to `false` because the cash split settles immediately. `CheckoutOrderHandler` reserves and completes `sale-{request.CheckoutRequestId}` in `FinancialJournals`.
  > Subsequently, when the customer's electronic payment arrives and the cashier calls `POST /api/orders/{id}/confirm-electronic`, [`ConfirmElectronicPaymentHandler.cs:75-78`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75-L78) executes:
  > ```csharp
  > var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
  > var journal = new EfFinancialJournal(_db);
  > if (!journal.TryStart(saleKey, "sale"))
  >     throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل...");
  > ```
  > `journal.TryStart` FAILS because `sale-{CheckoutRequestId}` is already recorded as `Completed`. `ConfirmElectronicPaymentHandler` rolls back and throws `ConfirmConflictException`. **The electronic payment leg of any split-payment order can NEVER be confirmed in the database!** The order is permanently stuck in `ElectronicPaymentStatus = "Pending"`.

  > [!CRITICAL]
  > **Vulnerability `FIN-015` (Shift Status Checked Outside Transaction with `AsNoTracking`):**
  > At lines 47–50:
  > ```csharp
  > var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  >     ?? throw new ShiftNotOpenException("الوردية غير موجودة.");
  > if (shift.Status != nameof(ShiftStatus.Open))
  >     throw new ShiftNotOpenException("يجب فتح وردية قبل تسجيل أي عملية بيع.");
  > ```
  > This check executes **BEFORE** `BeginTransactionAsync(ct)` at line 145 and uses `AsNoTracking()`.
  > A race condition exists: If a supervisor or blind close routine executes `POST /api/shifts/{id}/close` while `CheckoutOrderHandler` is resolving menu pricing, calculating taxes, deducting inventory, or validating approvals (between lines 56 and 145), the shift is closed and reconciled in the database. When `CheckoutOrderHandler` reaches line 145, it enters the transaction and commits the order with `ShiftId = request.ShiftId` and posts a cash `TreasuryMovement` to `tre-drawer` for the closed shift!
  > Because the shift was already reconciled and closed, this newly inserted cash sale is **omitted from that shift's closing reconciliation report**, creating an untracked discrepancy between physical drawer cash and shift settlement totals.

  > [!HIGH]
  > **Vulnerability `FIN-016` (Unvalidated Client-Supplied `DeliveryFeePiastres` Bypasses Discount/Comp Authorization):**
  > At line 66:
  > ```csharp
  > var deliveryFee = Money.FromPiastres(request.DeliveryFeePiastres);
  > ```
  > `request.DeliveryFeePiastres` is accepted directly from the client request payload without validation.
  > `Money.FromPiastres(long piastres)` allows negative numbers.
  > In `CheckoutRules.BuildPlan`:
  > `PayableTotal = subtotal + tax + deliveryFee - discount - comp;`
  > If a cashier submits a negative `DeliveryFeePiastres` (e.g. `-5000` piastres = -50 EGP), the payable total of the order is reduced or zeroed out.
  > This completely circumvents:
  > 1. Cashier permission checks for `orders_discount` (lines 112–113).
  > 2. Manager approval requirements for `orders_comp` (lines 125–138).
  > 3. Audit trail logging for discounts and comp (lines 197–216).
  > Even for positive values, `DeliveryFeePiastres` is not verified against configured delivery service partner rates or zone distance matrices.

- **Concurreny & Transaction Locking:**
  - Uses `_db.Database.BeginTransactionAsync(ct)` (default ReadCommitted).
  - Sequence allocation (`OrderSequenceService.AllocateNextAsync`) uses database update lock semantics.
  - Catch block handles `DbUpdateException` and `CheckoutConflictException` for replay recovery, but relies on optimistic catch rather than pessimistic locks for pre-checks.
- **Tests & Missing Regressions:**
  - Existing tests: `tests/Falah.Server.IntegrationTests/CheckoutVerticalSliceTests.cs` (verifies happy path takeaway, dine-in removal, and delivery creation).
  - Missing regressions:
    1. Integration test performing split payment (`cash` + `instapay`) checkout, followed by calling `POST /api/orders/confirm-electronic-payment` to prove `FIN-001` failure.
    2. Concurrency test closing a shift concurrently during order checkout to prove `FIN-015`.
    3. Unit/Integration test submitting negative `DeliveryFeePiastres` to verify discount bypass `FIN-016`.
- **Classification:** Core transaction orchestrator with critical financial vulnerabilities (`FIN-001`, `FIN-015`, `FIN-016`).

---

### 3. Shift Lookup Lambda (`s => s.Id == request.ShiftId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:47:73:SimpleLambdaExpression`
- **Signature:** `s => s.Id == request.ShiftId`
- **Line Span:** Line 47
- **Actual Behavior & Inputs:**
  Expression predicate passed to `_db.Shifts.AsNoTracking().FirstOrDefaultAsync(...)`. Filters shifts by primary key `request.ShiftId`.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `ShiftNotOpenException("الوردية غير موجودة.")` if null.
- **Financial Integrity Assessment:**
  Validates shift existence. Subject to stale read race condition `FIN-015` as analyzed above.
- **Classification:** Query filter lambda.

---

### 4. Cashier Lookup Lambda (`u => u.Id == request.CashierId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:52:78:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.CashierId`
- **Line Span:** Line 52
- **Actual Behavior & Inputs:**
  Filters `_db.Users.AsNoTracking()` to retrieve the cashier's user record for role/permission evaluation.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `InvalidOperationException("الكاشير غير موجود.")` if missing.
- **Financial Integrity Assessment:**
  Ensures only authentic registered users can be designated as order creators.
- **Classification:** Query filter lambda.

---

### 5. Pricing Item Projection Lambda (`r => r.Item`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:58:42:SimpleLambdaExpression`
- **Signature:** `r => r.Item`
- **Line Span:** Line 58
- **Actual Behavior & Inputs:**
  Projects `ResolvedOrderItem` results from `_pricing.ResolveAsync` to pure domain `OrderItem` instances.
- **Callers, Dependencies & Side Effects:**
  In-memory LINQ projection. Pure mapping.
- **Classification:** In-memory transformation lambda.

---

### 6. Active Tax Rule Lookup Lambda (`t => t.Enabled`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:60:83:SimpleLambdaExpression`
- **Signature:** `t => t.Enabled`
- **Line Span:** Line 60
- **Actual Behavior & Inputs:**
  Filters `_db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Enabled, ct)`.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. If no enabled rule exists, tax defaults to null (zero tax).
- **Financial Integrity Assessment:**
  Ensures tax calculation adheres to currently enabled tenant tax policy.
- **Classification:** Query filter lambda.

---

### 7. Split Payment Projection Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:72:21:SimpleLambdaExpression`
- **Signature:** `s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.Method), Money.FromPiastres(s.AmountPiastres))`
- **Line Span:** Line 72
- **Actual Behavior & Inputs:**
  Transforms client split requests (`CheckoutSplitRequest`) into domain `PaymentSplit` records.
- **Callers, Dependencies & Side Effects:**
  In-memory LINQ projection. Throws `ArgumentException` if method string is unrecognized.
- **Financial Integrity Assessment:**
  Creates split value objects for domain validation in `CheckoutRules.BuildPlan`. Note that validation of split sums matching total is performed by domain rules.
- **Classification:** In-memory transformation lambda.

---

### 8. Comp Approver Lookup Lambda (`u => u.Id == request.CompApproverUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:127:83:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.CompApproverUserId`
- **Line Span:** Line 127
- **Actual Behavior & Inputs:**
  Filters `_db.Users.AsNoTracking()` to locate the manager user whose ID was supplied in `request.CompApproverUserId`.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync` when `compAmount.IsPositive`. Throws `DomainException("المعتمد غير موجود.")` if missing.
- **Financial Integrity Assessment:**
  Guarantees that the approver recorded on the comped order is an existing user in the system before calling `AuthorizationPolicy.ValidateApproval`.
- **Classification:** Query filter lambda.

---

### 9. `FindExistingAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:292:5:MethodDeclaration`
- **Signature:** `private async Task<OrderEntity?> FindExistingAsync(string checkoutRequestId, CancellationToken ct)`
- **Line Span:** Lines 292–294
- **Actual Behavior & Inputs:**
  Queries `_db.Orders.AsNoTracking().Include(o => o.Items)` searching for an existing order row where `o.CheckoutRequestId == checkoutRequestId`.
- **Callers, Dependencies & Side Effects:**
  - Called at line 44 for fast pre-transaction idempotency short-circuit.
  - Called at line 285 in the catch block to verify whether a failed transaction or lock race was due to an already-committed identical order.
  - Dependencies: `FalahDbContext`, EF Core navigation include.
  - Side effects: None (read-only query).
- **Trust Boundaries & Failure Modes:**
  Relies on database unique index on `Orders.CheckoutRequestId`. If an order was previously committed, returns the full entity.
- **Financial Integrity Assessment:**
  Critical idempotency guard preventing double-billing on network disconnects or retries.
- **Classification:** Database query helper.

---

### 10. Items Navigation Include Lambda (`o => o.Items`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:293:49:SimpleLambdaExpression`
- **Signature:** `o => o.Items`
- **Line Span:** Line 293
- **Actual Behavior & Inputs:**
  EF Core navigation property expression passed to `.Include(...)` to eagerly load child `OrderItemEntity` records.
- **Classification:** ORM mapping lambda.

---

### 11. CheckoutRequestId Filter Lambda (`o => o.CheckoutRequestId == checkoutRequestId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:294:34:SimpleLambdaExpression`
- **Signature:** `o => o.CheckoutRequestId == checkoutRequestId`
- **Line Span:** Line 294
- **Actual Behavior & Inputs:**
  EF Core SQL translation predicate for `FirstOrDefaultAsync`.
- **Classification:** Query filter lambda.

---

### 12. `ToActor`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:296:5:MethodDeclaration`
- **Signature:** `private static Actor ToActor(UserEntity user)`
- **Line Span:** Line 296
- **Actual Behavior & Inputs:**
  Delegates directly to `OrderMapper.ToActor(user)` to convert database `UserEntity` into domain `Actor`.
- **Classification:** Pure mapping helper.

---

### 13. `ToEntity(Order)`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:298:5:MethodDeclaration`
- **Signature:** `private static OrderEntity ToEntity(Order order)`
- **Line Span:** Line 298
- **Actual Behavior & Inputs:**
  Delegates directly to `OrderMapper.ToEntity(order)` to convert domain `Order` aggregate into persistent `OrderEntity`.
- **Classification:** Pure mapping helper.

---

### 14. `ToEntity(TreasuryMovement)`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:300:5:MethodDeclaration`
- **Signature:** `private static TreasuryMovementEntity ToEntity(TreasuryMovement movement)`
- **Line Span:** Line 300
- **Actual Behavior & Inputs:**
  Delegates to `OrderMapper.ToEntity(movement)` to map domain `TreasuryMovement` value object to database `TreasuryMovementEntity`.
- **Classification:** Pure mapping helper.

---

### 15. `ToResult`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:302:5:MethodDeclaration`
- **Signature:** `private static CheckoutOrderResult ToResult(OrderEntity entity, bool wasReplayed)`
- **Line Span:** Lines 302–304
- **Actual Behavior & Inputs:**
  Constructs and returns `CheckoutOrderResult` with order ID, order number, sequence number, totals, payment status, electronic payment status, and replay flag.
- **Classification:** DTO factory method.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-001`** | **CRITICAL** | `CheckoutOrderHandler.cs:148-154` & `ConfirmElectronicPaymentHandler.cs:75-78` | Electronic leg of split-payment order can NEVER be confirmed due to idempotency key collision on `sale-{CheckoutRequestId}`. | Scope idempotency keys to payment leg/operation (e.g. `sale-{CheckoutRequestId}-cash` vs `sale-{CheckoutRequestId}-electronic`), or allow `ConfirmElectronicPaymentHandler` to append confirmation under `confirm-{orderId}` instead of re-attempting `TryStart` on the parent sale key. |
| **`FIN-015`** | **CRITICAL** | `CheckoutOrderHandler.cs:47-50` | Shift status is checked outside transaction with `AsNoTracking()`. Orders can commit cash to shifts closed mid-flight. | Re-validate shift status inside `transaction` using `SELECT ... WITH (UPDLOCK, ROWLOCK)` or check shift status before commit, rejecting orders submitted to closed shifts. |
| **`FIN-016`** | **HIGH** | `CheckoutOrderHandler.cs:66` | Negative or arbitrary `DeliveryFeePiastres` accepted without validation, bypassing discount/comp gates. | Enforce `DeliveryFeePiastres >= 0` and validate against official delivery fee tables; require `orders_discount` or manager approval if delivery fee is discounted or zeroed. |
