# Deep Per-Callable Source Audit: `RefundOrderHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs)
- **Role:** Order Refund Orchestrator, Ledger Reversal & Shift Treasury Allocation Handler
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:40:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 14 (1 Constructor, 4 Methods, 9 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `RefundOrderHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:38:5:ConstructorDeclaration`
- **Signature:** `public RefundOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Line Span:** Lines 38–43
- **Actual Behavior & Inputs:**
  Receives and assigns `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via DI constructor injection to private readonly fields.
- **Callers, Dependencies & Side Effects:**
  - Invoked by ASP.NET Core DI container (scoped lifetime) during endpoint invocation for `POST /api/orders/refund`.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:45:5:MethodDeclaration`
- **Signature:** `public async Task<RefundOrderResult> HandleAsync(RefundOrderRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 45–152
- **Actual Behavior & Inputs:**
  Receives `RefundOrderRequest(RefundRequestId, OrderId, AmountPiastres, Reason, CashierId, ManagerApprovalId, ManagerApproverUserId)`. Orchestrates:
  1. *Fast Idempotency Replay (Lines 47–50):* Derives `refundKey = $"refund-{request.RefundRequestId}"`. Queries `FindExistingAsync(refundKey, ct)`. If existing refund row found, returns `ToResultAsync(existing, wasReplayed: true, ct)`.
  2. *Cashier Existence Check (Lines 52–55):* Queries `Users` with `.AsNoTracking()`.
  3. *Transaction & Pessimistic Order Lock (Lines 56–63):* Begins explicit transaction `await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct)` and locks the order row:
     `SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}`
     Includes `Items` and `Refunds`.
  4. *Authorization & Manager Approval Check (Lines 67–76):* If `request.ManagerApprovalId` is provided, fetches manager user and constructs `ManagerApproval`. Invokes `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, cashier, "orders_refund", approval, requireApproval: true)`.
  5. *Refund Amount Clamping & Validation (Lines 78–84):* Calculates `refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount)`. Clamps requested amount via `RefundRules.ClampRefundAmount`. Asserts `safeAmount.IsPositive`.
  6. *Protected Treasury Balance Guard (Lines 91–93):* Resolves source treasury account (`TreasuryMovementFactory.PaymentAccountFor(order.PaymentMethod)`). Locks account movements via `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, refundSourceAccount, ct)` and asserts available balance via `FinancialRules.AssertProtectedSourceBalance`.
  7. *Operational Shift Resolution (Lines 95–101):* Fetches `originalShiftEntity` and `activeShiftEntity`. Resolves operational shift via `RefundRules.ResolveOperationalShift(...)`.
  8. *Movement & Domain Mutation (Lines 103–121):*
     - Builds `TreasuryMovement` (`BuildRefundMovement`).
     - Constructs domain `OrderRefund`.
     - Calls domain `order.ApplyRefund(refund)`.
     - Updates entity `Status`, `RefundAmountPiastres`, increments `Version`.
     - Adds `OrderRefundEntity` and `TreasuryMovementEntity` to `_db`.
  9. *Audit Trail (Lines 123–128):* Appends refund event to `_auditLog` (classified as `critical` if amount >= `LargeRefundThresholdPiastres` [5,000 EGP]).
  10. *Commit & Persistence (Lines 130–135):* Calls `_db.SaveChangesAsync(ct)` and `transaction.CommitAsync(ct)`. Returns `ToResult(refundEntity, orderEntity, wasReplayed: false)`.
  11. *Catch & Contention Recovery (Lines 137–151):* On `DbUpdateException`, `DbUpdateConcurrencyException`, or `BalanceLockContention.IsContention(ex)`, rolls back transaction. If contention detected, calls `BalanceLockContention.AwaitSiblingAsync`. Returns replay if found or throws `RefundConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/orders/refund` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`, `AuthorizationPolicy`, `RefundRules`, `FinancialRules`, `TreasuryBalanceQuery`, `TreasuryMovementFactory`, `OrderMapper`, `BalanceLockContention`.
  - Side effects: Modifies `Orders`, inserts into `OrderRefunds`, `TreasuryMovements`, `AuditEvents`. Deducts money from drawer/main treasury.
- **Critical Financial Integrity Flaws:**

  > [!CRITICAL]
  > **Vulnerability `FIN-003` (Cash Extraction via Refund on Unpaid Orders):**
  > At lines 78–84:
  > ```csharp
  > var order = OrderMapper.ToDomain(orderEntity);
  > var refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount);
  > var safeAmount = RefundRules.ClampRefundAmount(Money.FromPiastres(request.AmountPiastres), refundable);
  > if (!safeAmount.IsPositive) throw new DomainException("لا يوجد مبلغ متبقٍ للاسترجاع.");
  > ```
  > And inside domain [`Order.cs:ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220-L232):
  > ```csharp
  > if (Status == OrderStatus.Cancelled) throw new DomainException("لا يمكن استرجاع طلب ملغي.");
  > if (PaymentMethod == PaymentMethod.Multi) throw new DomainException("مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي.");
  > var refundableBefore = Total - RefundAmount;
  > ...
  > ```
  > **CRITICAL DEFECT:** Neither `RefundOrderHandler` nor domain `Order.ApplyRefund` ever asserts `order.IsPaid == true`!
  > If an order is created as Delivery (Cash On Delivery) or has an unconfirmed electronic payment, `order.IsPaid` is `false`. The customer has **NOT PAID** any money to the restaurant.
  > Because `Total` is positive and `RefundAmount` is zero, `RefundRules.RefundableRemaining` returns the full order total.
  > `RefundOrderHandler` checks the pooled balance of `tre-drawer` (funded by opening float and other customer sales), passes the balance check at line 93, creates a refund `TreasuryMovement` taking real cash out of `tre-drawer`, and hands cash to the cashier or driver for an unpaid order!
  > This is a severe cash leakage and fraud vector allowing anyone with refund privileges to siphon cash from the drawer using unpaid orders.

  > [!CRITICAL]
  > **Vulnerability `FIN-004` (Cross-Shift Refund Discrepancy & Shift Reconciliation Omission):**
  > At lines 99–106:
  > ```csharp
  > var shiftPolicy = RefundRules.ResolveOperationalShift(
  >     order.ShiftId, Enum.Parse<ShiftStatus>(originalShiftEntity.Status), activeShiftEntity?.Id, order.PaymentMethod);
  > ...
  > var movement = TreasuryMovementFactory.BuildRefundMovement(..., shiftPolicy.OperationalShiftId!, ...);
  > ```
  > When an order from a closed shift (Shift A) is refunded during a currently active shift (Shift B), `shiftPolicy.OperationalShiftId` is set to Shift B. Cash is physically disbursed from Shift B's cash drawer.
  > `OrderRefundEntity` persists both `ShiftId` (Shift A, original order) and `OperationalShiftId` (Shift B, refund execution).
  > **HOWEVER**, in [`ShiftReconciliation.cs:33`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L33):
  > ```csharp
  > var refunds = await _db.OrderRefunds.AsNoTracking()
  >     .Where(r => r.ShiftId == shiftId)
  >     .ToListAsync(ct);
  > ```
  > Shift reconciliation queries `r.ShiftId == shiftId` instead of `r.OperationalShiftId == shiftId`!
  > When Shift B is closed, Shift B's expected drawer cash calculation **completely omits this cash refund**! The drawer is physically short by `safeAmount`, but the system calculates expected cash without subtracting the refund, falsely accusing the Shift B cashier of an unexplained cash shortage!
  > Meanwhile, Shift A was already closed and reconciled in the past, so the refund is never deducted from any closed shift report.

- **Concurrency, Locking & Contention Handling:**
  - Row locking on `Orders` via `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` inside `IsolationLevel.ReadCommitted`.
  - Row locking on `TreasuryMovements` via `LoadAccountMovementsForUpdateAsync` inside the transaction.
  - Sibling contention backoff via `BalanceLockContention.AwaitSiblingAsync`.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs` (covers full refund, partial refund, clamping, retry idempotency, concurrent same-key races, permission rejection, and source account exhaustion).
  - Missing Regressions:
    1. Attempting to refund an unpaid delivery order (`IsPaid == false`) — currently missing, allowing `FIN-003` to pass.
    2. Cross-shift refund integration test verifying that a refund executed in Shift B for an order from Shift A correctly reduces Shift B's expected drawer cash in `ShiftReconciliation` (`FIN-004`).
- **Classification:** Core financial handler with critical vulnerabilities (`FIN-003`, `FIN-004`).

---

### 3. Cashier Lookup Lambda (`u => u.Id == request.CashierId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:52:78:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.CashierId`
- **Line Span:** Line 52
- **Actual Behavior & Inputs:**
  Filters `_db.Users.AsNoTracking()` to retrieve cashier entity. Throws `InvalidOperationException("الكاشير غير موجود.")` if missing.
- **Classification:** Query filter lambda.

---

### 4. Items Navigation Include Lambda (`o => o.Items`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:61:26:SimpleLambdaExpression`
- **Signature:** `o => o.Items`
- **Line Span:** Line 61
- **Actual Behavior & Inputs:**
  Loads `Items` navigation on the locked `OrderEntity`.
- **Classification:** ORM mapping lambda.

---

### 5. Refunds Navigation Include Lambda (`o => o.Refunds`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:61:48:SimpleLambdaExpression`
- **Signature:** `o => o.Refunds`
- **Line Span:** Line 61
- **Actual Behavior & Inputs:**
  Loads existing `Refunds` navigation collection on `OrderEntity` for domain aggregate construction.
- **Classification:** ORM mapping lambda.

---

### 6. Manager Approver Lookup Lambda (`u => u.Id == request.ManagerApproverUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:70:87:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Line Span:** Line 70
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to find the manager approving the refund. Throws `DomainException("المعتمد غير موجود.")` if not found.
- **Classification:** Query filter lambda.

---

### 7. Original Shift Lookup Lambda (`s => s.Id == order.ShiftId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:95:91:SimpleLambdaExpression`
- **Signature:** `s => s.Id == order.ShiftId`
- **Line Span:** Line 95
- **Actual Behavior & Inputs:**
  Queries `_db.Shifts.AsNoTracking()` to load the shift during which the order was originally placed. Throws `InvalidOperationException("وردية الطلب الأصلية غير موجودة.")` if missing.
- **Classification:** Query filter lambda.

---

### 8. Active Shift Lookup Lambda (`s => s.Status == nameof(ShiftStatus.Open)`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:97:89:SimpleLambdaExpression`
- **Signature:** `s => s.Status == nameof(ShiftStatus.Open)`
- **Line Span:** Line 97
- **Actual Behavior & Inputs:**
  Queries `_db.Shifts.AsNoTracking()` to locate any currently open shift.
- **Classification:** Query filter lambda.

---

### 9. Contention Await Sibling Lambda (`async () => await FindExistingAsync(refundKey, ct)`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:147:21:ParenthesizedLambdaExpression`
- **Signature:** `async () => await FindExistingAsync(refundKey, ct)`
- **Line Span:** Line 147
- **Actual Behavior & Inputs:**
  Asynchronous polling callback passed to `BalanceLockContention.AwaitSiblingAsync`. Repeatedly invokes `FindExistingAsync` while awaiting the commit of a concurrent sibling transaction holding balance locks.
- **Classification:** Concurrency synchronization lambda.

---

### 10. `FindExistingAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:154:5:MethodDeclaration`
- **Signature:** `private Task<OrderRefundEntity?> FindExistingAsync(string refundKey, CancellationToken ct)`
- **Line Span:** Lines 154–155
- **Actual Behavior & Inputs:**
  Queries `_db.OrderRefunds.AsNoTracking()` by `r.IdempotencyKey == refundKey`.
- **Callers, Dependencies & Side Effects:**
  - Invoked at line 49 (fast replay pre-check), line 141 (post-rollback replay lookup), and line 147 (contention polling).
  - Dependencies: `FalahDbContext`.
  - Side effects: None.
- **Classification:** Database query helper.

---

### 11. Idempotency Key Filter Lambda (`r => r.IdempotencyKey == refundKey`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:155:61:SimpleLambdaExpression`
- **Signature:** `r => r.IdempotencyKey == refundKey`
- **Line Span:** Line 155
- **Actual Behavior & Inputs:**
  Filters `OrderRefunds` by unique idempotency key string.
- **Classification:** Query filter lambda.

---

### 12. `ToResultAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:157:5:MethodDeclaration`
- **Signature:** `private async Task<RefundOrderResult> ToResultAsync(OrderRefundEntity refund, bool wasReplayed, CancellationToken ct)`
- **Line Span:** Lines 157–161
- **Actual Behavior & Inputs:**
  Queries parent `Orders` entity via `.FirstAsync(o => o.Id == refund.OrderId, ct)` and constructs `RefundOrderResult` via `ToResult`.
- **Classification:** DTO query & mapping method.

---

### 13. Replay Order Lookup Lambda (`o => o.Id == refund.OrderId`)
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:159:64:SimpleLambdaExpression`
- **Signature:** `o => o.Id == refund.OrderId`
- **Line Span:** Line 159
- **Actual Behavior & Inputs:**
  Filters `_db.Orders.AsNoTracking()` by primary key to retrieve the parent order for refund replay.
- **Classification:** Query filter lambda.

---

### 14. `ToResult`
- **Symbol ID:** `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs:163:5:MethodDeclaration`
- **Signature:** `private static RefundOrderResult ToResult(OrderRefundEntity refund, OrderEntity order, bool wasReplayed)`
- **Line Span:** Lines 163–165
- **Actual Behavior & Inputs:**
  Creates `RefundOrderResult` containing refund ID, order ID, refund amount, updated order status, remaining refundable balance (`order.TotalPiastres - order.RefundAmountPiastres`), treasury movement ID, and `wasReplayed` boolean.
- **Classification:** Pure mapping helper.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-003`** | **CRITICAL** | `RefundOrderHandler.cs:78-84` & `Order.cs:220-232` | Cash can be refunded from the drawer for unpaid orders (uncollected COD delivery or pending electronic payments), enabling physical cash theft. | Assert `if (!order.IsPaid) throw new DomainException("لا يمكن استرجاع طلب غير مدفوع.");` in both `RefundOrderHandler.cs` and `Order.ApplyRefund`. |
| **`FIN-004`** | **CRITICAL** | `RefundOrderHandler.cs:99-106` & `ShiftReconciliation.cs:33` | Cross-shift refunds take cash from current drawer (`OperationalShiftId`) but `ShiftReconciliation` filters on `ShiftId`, causing false cash shortages for cashiers. | Update `ShiftReconciliation.cs` to filter on `r.OperationalShiftId == shiftId` for cash drawer deductions, ensuring the active drawer reconciliation reflects cross-shift refunds. |
| **`FIN-002`** | **HIGH** | `Order.cs:200-214` & `Order.cs:223` | Cancelling pending electronic legs of split orders leaves cash portion deposited without ledger reversal; multi-payment orders are permanently blocked from refund. | Implement split-order partial cancellation / ledger reversal mechanism or disallow `CancelElectronicPayment` on multi-split orders without simultaneous cash refund movement. |
