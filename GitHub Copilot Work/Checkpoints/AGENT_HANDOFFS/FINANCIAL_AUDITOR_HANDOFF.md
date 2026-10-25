# Financial & Database Integrity Specialist Audit Report & Handoff

- **Audit Authority:** Financial & Database Integrity Specialist
- **Audit Target:** Fale7 POS Backend Architecture & Domain Invariants
- **Date & UTC Timestamp:** 2026-10-24T18:15:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current HEAD Commit:** `9beff52a6c9c6c4f7244cbd46dc449da91745582`
- **Scope Compliance:** **AUDIT DOCUMENTATION ONLY — ZERO APPLICATION SOURCE OR TEST CODE MODIFIED IN `src/` OR `tests/`**
- **Handoff Target:** `GitHub Copilot Work/Checkpoints/AGENT_HANDOFFS/FINANCIAL_AUDITOR_HANDOFF.md`

---

## 1. Executive Summary & Audit Mandate

As the designated Financial & Database Integrity Specialist for Fale7 POS, a deep static and architectural audit was conducted across the entire financial surface of the application. The audit encompassed all **16 core financial workflows**:
1. Order Checkout & Immediate Sale Journaling
2. Split Tender (Cash + Electronic Tender)
3. Electronic Payment Confirmation & Post-Checkout Settlement
4. Pending Electronic Payment Voiding / Cancellation
5. Order Refunds & Debit Reversals
6. Cross-Shift Cash Refund Drawer Reconciliation
7. Shift Float & Opening Drawer Authorization
8. Cash Drawer Blind Closing & Handover Reconciliation
9. Business Day Opening & Multi-Terminal Concurrency
10. Treasury Movements: Safe Drops, Operating Expenses, & Internal Transfers
11. Supplier Purchasing & Inbound Stock Receipt
12. Supplier Invoice Debt Settlement & Concurrency Locking
13. Inventory Valuation, Waste Accounting, & Outbound Stock Tracking
14. Employee Time & Attendance Corrections
15. Employee Credit / Advance Management & Standalone Cashouts
16. Payroll Run Lifecycle: Generation, State Transition, & Payout Disbursement

### Primary Audit Findings Summary
Every one of the 16 primary financial vulnerability items (`FIN-001` through `FIN-016`) was independently verified against baseline source code. In particular, the 7 priority vulnerabilities flagged for deep source verification:
- **`FIN-001` (CRITICAL / HIGH):** Split payment electronic confirmation is completely bricked due to an idempotency key collision on `sale-{CheckoutRequestId}` in [`ConfirmElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75-L79).
- **`FIN-002` (CRITICAL / HIGH):** Cancelling a pending electronic payment via [`Order.cs:CancelElectronicPayment`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200-L214) voids the order and excludes it from sales while permanently abandoning the already-collected cash split in the drawer without ledger reversal.
- **`FIN-003` (CRITICAL / HIGH):** [`Order.cs:ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220-L232) and [`RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L78-L115) fail to assert `IsPaid`, allowing cashiers to drain physical cash from the drawer for unpaid COD or pending electronic orders.
- **`FIN-005` (HIGH):** [`PayPurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs#L36-L73) reads supplier invoice balances outside transaction and without `UPDLOCK`, permitting concurrent distinct requests to overpay the supplier.
- **`FIN-006` (CRITICAL / HIGH):** [`TransitionPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L26-L59) admits status transition to `Paid` without executing treasury disbursements or employee ledger settlement debits.
- **`FIN-013` (MEDIUM / HIGH):** [`OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L301-L304) sums raw `QuantityMilliUnits` without negating `Direction == Out`, adding waste and consumption to stock totals instead of subtracting them.
- **`FIN-016` (HIGH):** [`CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L66) directly trusts unvalidated signed `request.DeliveryFeePiastres`, allowing client manipulation to bypass manager comp/discount authorizations.

---

## 2. In-Depth Source Chains for Priority Findings

### 2.1 FIN-001: Split Payment Idempotency Key Collision

- **Vulnerability ID:** `FIN-001`
- **Severity:** HIGH / CRITICAL
- **File Paths:**
  - [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L148-L154) (Lines 148–154, 253–276)
  - [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75-L79) (Lines 75–79)
  - [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L22-L45) (Lines 22–45)

#### Exact Execution Flow & Failure Mechanism
1. A customer places a split order with both Cash and Electronic tender (e.g., 50 EGP Cash, 150 EGP via InstaPay).
2. In [`CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L148-L154):
   ```csharp
   var saleKey = $"sale-{request.CheckoutRequestId}";
   if (!plan.DefersAllMoney)
   {
       var journal = new EfFinancialJournal(_db);
       if (!journal.TryStart(saleKey, "sale"))
           throw new CheckoutConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
   }
   ```
   Because the order contains immediate cash, `plan.DefersAllMoney` evaluates to `false`.
3. The journal entry `sale-{CheckoutRequestId}` is inserted with status `Pending`.
4. In [`CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L253-L276), the cash treasury movement is persisted, and the journal entry is completed:
   ```csharp
   new EfFinancialJournal(_db).Complete(saleKey);
   ```
   The order is saved with `IsPaid = false` and `ElectronicPaymentStatus = "pending"`. The database now contains a completed row in `FinancialJournalEntries` with `IdempotencyKey = $"sale-{request.CheckoutRequestId}"`.
5. Later, the customer completes the electronic transfer. The cashier clicks "Confirm Payment", which invokes [`ConfirmElectronicPaymentHandler.HandleAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L24).
6. In [`ConfirmElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75-L79):
   ```csharp
   var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
   var journal = new EfFinancialJournal(_db);
   if (!journal.TryStart(saleKey, "sale"))
       throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل رغم أن الدفع لم يكن مؤكدًا - يحتاج مراجعة يدوية.");
   ```
7. [`EfFinancialJournal.TryStart`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L24-L26) checks:
   ```csharp
   if (_db.FinancialJournalEntries.Local.Any(e => e.IdempotencyKey == idempotencyKey)
       || _db.FinancialJournalEntries.Any(e => e.IdempotencyKey == idempotencyKey))
       return false;
   ```
   The key `sale-{orderEntity.CheckoutRequestId}` already exists from step 3. `TryStart` returns `false`.
8. `ConfirmElectronicPaymentHandler` throws `ConfirmConflictException`. The transaction rolls back.
9. **Impact:** The electronic leg of ANY split payment can **NEVER** be confirmed. The payment is permanently locked in `pending`, no electronic treasury movement is ever recorded, and the order cannot transition to `IsPaid = true`.

---

### 2.2 FIN-002: Pending Electronic Payment Cancellation Unreversed Cash Split

- **Vulnerability ID:** `FIN-002`
- **Severity:** HIGH / CRITICAL
- **File Paths:**
  - [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200-L214) (Lines 200–214)
  - [`src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs#L48-L70) (Lines 48–70)
  - [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L255-L267) (Lines 255–267)

#### Exact Execution Flow & Failure Mechanism
1. A split order is checked out with 100 EGP Cash and 100 EGP InstaPay.
2. In `CheckoutOrderHandler.cs`, lines 255–267, a `TreasuryMovement` of type `sale_collection` is created and committed to the cash drawer (`tre-drawer`) for the 100 EGP cash leg.
3. The customer fails to execute the InstaPay transfer and walks away or requests cancellation.
4. The cashier executes `CancelPendingElectronicPaymentHandler`.
5. [`Order.CancelElectronicPayment`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200-L214) executes:
   ```csharp
   Status = OrderStatus.Cancelled;
   IsPaid = false;
   ElectronicPayment = leg with { Status = ElectronicPaymentStatus.Cancelled };
   CancelledAtUtc = nowUtc;
   CancelReason = reason.Trim();
   CancelledBy = cancelledBy;
   ```
   The code comment on line 195 states: *"no Treasury movement exists to reverse (checkout deferred it entirely)"*.
   This assumption is true ONLY for 100% electronic orders, but **FALSE** for split orders!
6. [`CancelPendingElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs#L48-L70) updates `orderEntity.Status = "cancelled"` and commits. It performs **no check** for existing posted treasury movements and executes **no treasury reversal or refund**.
7. Because the order is now `Status = "cancelled"`, it is excluded from sales reports (`orders.Where(o => o.Status != "cancelled")`), but the 100 EGP cash movement remains posted to `tre-drawer`.
8. When the shift closes, physical cash and drawer movements are orphaned from any valid sale, causing irreconcilable discrepancies. Furthermore, [`Order.ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L222) blocks refunds on cancelled orders (`if (Status == OrderStatus.Cancelled) throw`), making it impossible to disburse or refund the cash through standard UI workflows.

---

### 2.3 FIN-003: Uncollected Order Refund Cash Drain Loophole

- **Vulnerability ID:** `FIN-003`
- **Severity:** HIGH / CRITICAL
- **File Paths:**
  - [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220-L232) (Lines 220–232)
  - [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L78-L121) (Lines 78–121)
  - [`src/Falah.Domain/Orders/RefundRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs#L20-L28)

#### Exact Execution Flow & Failure Mechanism
1. A delivery order is checked out as Cash-on-Delivery (COD), or an order is checked out with pending electronic tender.
2. Under `CheckoutRules.BuildPlan`, `plan.DefersAllMoney` is `true` for COD delivery. At checkout, `Order.IsPaid` is set to `false`. Zero money has been collected; zero treasury movements exist for this order.
3. A cashier or rogue operator submits `POST /api/orders/{id}/refund` targeting this unpaid order.
4. In [`RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L78-L84):
   ```csharp
   var order = OrderMapper.ToDomain(orderEntity);
   var refundable = RefundRules.RefundableRemaining(order.Total, order.RefundAmount);
   var safeAmount = RefundRules.ClampRefundAmount(Money.FromPiastres(request.AmountPiastres), refundable);
   if (!safeAmount.IsPositive) throw new DomainException("لا يوجد مبلغ متبقٍ للاسترجاع.");
   ```
   `RefundableRemaining` computes `order.Total - order.RefundAmount`. It **never inspects** whether any amount was actually collected or whether `order.IsPaid == true`.
5. Lines 91–93 check `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` and `FinancialRules.AssertProtectedSourceBalance`. This check only verifies that the *pooled drawer account* contains sufficient cash from *other customers' orders*.
6. In [`Order.cs:ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220-L232):
   ```csharp
   if (Status == OrderStatus.Cancelled) throw new DomainException("لا يمكن استرجاع طلب ملغي.");
   if (PaymentMethod == PaymentMethod.Multi) throw new DomainException("مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي.");

   var refundableBefore = Total - RefundAmount;
   if (refund.Amount.IsZero || refund.Amount.IsNegative || refund.Amount > refundableBefore)
       throw new DomainException("مبلغ المرتجع غير صالح.");

   _refunds.Add(refund);
   RefundAmount += refund.Amount;
   if (RefundAmount == Total) Status = OrderStatus.Refunded;
   ```
   There is **no check** that `IsPaid == true`.
7. `RefundOrderHandler` posts an outbound `TreasuryMovement` debiting `tre-drawer` and hands real physical currency to the recipient.
8. **Impact:** Direct cash leakage from the register. An unpaid order of 500 EGP can be "refunded" for 500 EGP in physical drawer cash without a single piastre ever having entered the business.

---

### 2.4 FIN-005: Concurrent Distinct Supplier Invoice Payments Overpayment Race

- **Vulnerability ID:** `FIN-005`
- **Severity:** HIGH
- **File Paths:**
  - [`src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs#L36-L73) (Lines 36–73, 95–99)
  - [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L277-L305)

#### Exact Execution Flow & Failure Mechanism
1. A supplier invoice exists with 1,000 EGP outstanding debt.
2. Two separate management clients (Terminal A and Terminal B) submit concurrent settlement requests for this invoice:
   - Request A: RequestId = `req-A`, Amount = 800 EGP. IdempotencyKey = `purchase-payment-{id}-req-A`.
   - Request B: RequestId = `req-B`, Amount = 800 EGP. IdempotencyKey = `purchase-payment-{id}-req-B`.
3. In [`PayPurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs#L36-L66):
   ```csharp
   var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
       .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct);
   ...
   var ledgerEntries = await _db.SupplierLedgerEntries.AsNoTracking()
       .Where(e => e.SupplierId == purchase.SupplierId).ToListAsync(ct);
   var outstanding = PurchasingRules.SupplierOutstandingFromLedger(...);
   PurchasingRules.AssertPurchasePayment(amount, outstanding);
   ```
   Notice that this read is performed:
   - Completely outside the database transaction (transaction starts at line 73).
   - Without any pessimistic table/row locks (`WITH (UPDLOCK, HOLDLOCK)` is absent).
   - With `AsNoTracking()` on `SupplierLedgerEntries`.
4. Both Request A and Request B execute lines 36–66 concurrently. Both read `outstanding = 1,000 EGP`.
5. Since 800 <= 1,000, `PurchasingRules.AssertPurchasePayment` passes for both.
6. Both enter line 73: `await using var transaction = await _db.Database.BeginTransactionAsync(ct);`.
7. Line 80 locks the treasury account (`tre-main`), but does **not** lock the `Purchases` row or `SupplierLedgerEntries` table.
8. Request A commits first: pays 800 EGP, sets `purchase.PaidPiastres = 800`, `RemainingAmountPiastres = 200`.
9. Request B commits second: pays 800 EGP, sets `purchase.PaidPiastres = 800 + 800 = 1,600`, and computes `remaining = 1,000 - 800 = 200` based on its stale read!
10. **Impact:** The business pays 1,600 EGP on a 1,000 EGP invoice (overpayment of 600 EGP). Furthermore, the purchase cache shows `RemainingAmountPiastres = 200` while `PaidPiastres = 1,600` on a 1,000 EGP invoice, corrupting the database audit trail and supplier ledger.

---

### 2.5 FIN-006: Generic Payroll Transition Missing Treasury / Ledger Disbursement

- **Vulnerability ID:** `FIN-006`
- **Severity:** HIGH / CRITICAL
- **File Paths:**
  - [`src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L26-L61) (Lines 26–61)
  - [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L40-L75) (Lines 40–75)
  - [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L18-L32)

#### Exact Execution Flow & Failure Mechanism
1. A payroll run is generated and approved (`Status = "Approved"`).
2. The intended payout handler is [`PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs), which requires `payroll_pay` permission, validates an open business day, locks the treasury account, validates available funds, posts a `TreasuryMovement` debiting `tre-main`/`tre-bank`, and records `payroll_settlement` debits for each employee line in `EmployeeLedgerEntries`.
3. However, the API also exposes `POST /api/payroll/{id}/transition` routed to [`TransitionPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs).
4. An operator with only `employees_manage` permission submits `POST /api/payroll/{id}/transition` with `request.Status = "Paid"`.
5. In [`TransitionPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L26-L59):
   ```csharp
   if (!Enum.TryParse<PayrollRunStatus>(request.Status, ignoreCase: true, out var targetStatus))
       throw new DomainException("حالة Payroll غير صالحة.");
   ...
   var currentStatus = Enum.Parse<PayrollRunStatus>(run.Status);
   PayrollRules.AssertTransition(currentStatus, targetStatus);
   ...
   run.Status = targetStatus.ToString();
   await _db.SaveChangesAsync(ct);
   ```
6. `PayrollRules.AssertTransition` permits `Approved -> Paid`.
7. `run.Status` is directly updated to `"Paid"` in the database.
8. **Disbursement Failure:**
   - Zero treasury movements are created.
   - Zero employee ledger debits are posted; employee running balances remain owed.
   - No payment account is debited or validated.
9. **Deadlock State:** If someone subsequently attempts to legitimately disburse funds via [`PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L64-L74):
   ```csharp
   if (run.Status == nameof(PayrollRunStatus.Paid))
   {
       if (await LoadReplayAsync() is { } settledConcurrently) return settledConcurrently;
   }
   if (run.Status != nameof(PayrollRunStatus.Approved))
       throw new DomainException("يجب اعتماد مسير الرواتب قبل الدفع.");
   ```
   `LoadReplayAsync` searches for a `TreasuryMovement` with key `payroll-payment-{runId}`. Because no movement was ever created, it returns `null`. Line 73 then throws `"يجب اعتماد مسير الرواتب قبل الدفع."`.
10. **Impact:** The payroll run is permanently trapped in `Paid` without actual disbursement, and cannot be legitimately paid out through the system.

---

### 2.6 FIN-013: Owner Dashboard Stock Summation Sign Inversion

- **Vulnerability ID:** `FIN-013` (Corroborating `FIN-029`)
- **Severity:** MEDIUM / HIGH
- **File Paths:**
  - [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L297-L315) (Lines 297–315)
  - [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs#L50-L52) (Lines 50–52)
  - [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L391-L403) (Lines 391–403)

#### Exact Execution Flow & Failure Mechanism
1. Stock movements in Fale7 POS record a positive magnitude `QuantityMilliUnits` alongside a `Direction` (`StockDirection.In` or `StockDirection.Out`).
2. In domain logic ([`InventoryRules.StockBalanceMilliUnits`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs#L50-L52)) and inventory queries ([`InventoryQueryService.cs:402`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L402)):
   ```csharp
   var balance = itemMovements.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits);
   ```
3. However, in [`OwnerDashboardQueryService.cs:LoadStockAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L301-L304):
   ```csharp
   var balances = await _db.StockMovements.AsNoTracking()
       .GroupBy(m => m.InventoryItemId)
       .Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })
       .ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);
   ```
4. **Sign Inversion:** The query executes `g.Sum(m => m.QuantityMilliUnits)` in SQL without filtering or conditioning on `m.Direction`.
5. Suppose an ingredient (e.g. Meat) has:
   - Purchase receipt: 20,000 milliUnits (`Direction = In`).
   - Order consumption: 15,000 milliUnits (`Direction = Out`).
   - Waste / spoilage: 3,000 milliUnits (`Direction = Out`).
6. Real inventory balance: `20,000 - 15,000 - 3,000 = 2,000` milliUnits.
7. Owner Dashboard calculation: `20,000 + 15,000 + 3,000 = 38,000` milliUnits.
8. If `MinimumMilliUnits` is 5,000 milliUnits:
   - Reality: Stock is 2,000 (critically low, stockout imminent).
   - Dashboard: Reports 38,000 (shows abundant stock, omitting it from `lowStockItems`).
9. **Impact:** Restaurant owners viewing the executive dashboard are deceived into believing kitchen stock levels are plentiful when ingredients are actually exhausted, preventing timely replenishment.

---

### 2.7 FIN-016: Client-Controlled Delivery Fee Discount / Comp Bypass

- **Vulnerability ID:** `FIN-016`
- **Severity:** HIGH
- **File Paths:**
  - [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L66,L110-L144) (Lines 66, 110–144)
  - [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L35-L39,L97) (Lines 35–39, 97)
  - [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs)

#### Exact Execution Flow & Failure Mechanism
1. Authorization policies strictly guard discounts and comps at checkout:
   - [`CheckoutOrderHandler.cs:112`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L112): `discount.IsPositive` requires `orders_discount` permission.
   - [`CheckoutOrderHandler.cs:116`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L116): `compAmount.IsPositive` requires `orders_comp` permission, valid `CompReasonCode`, and authenticated `ManagerApproval`.
2. However, line 66 directly instantiates:
   ```csharp
   var deliveryFee = Money.FromPiastres(request.DeliveryFeePiastres);
   ```
   There is **no validation** that `request.DeliveryFeePiastres >= 0`, nor is the fee verified against the registered `DeliveryServiceEntity.FeePiastres`.
3. In [`CheckoutRules.ComputePayableTotal`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L35-L39):
   ```csharp
   public static Money ComputePayableTotal(Money cartTotal, Money deliveryFee, Money compAmount)
   {
       var result = cartTotal + deliveryFee - compAmount;
       return result.IsNegative ? Money.Zero : result;
   }
   ```
4. Because `Money` supports signed values, a cashier submitting `request.DeliveryFeePiastres = -5000` (-50 EGP) on a 150 EGP order reduces `PayableTotal` to 100 EGP.
5. Submitting `request.DeliveryFeePiastres = -15000` on a 150 EGP order reduces `PayableTotal` to 0 EGP.
6. Under `CheckoutRules.DefersAllMoney`:
   ```csharp
   var collectsNothing = !payableTotal.IsPositive;
   return collectsNothing || ...
   ```
   `DefersAllMoney` evaluates to `true`.
7. The order commits to the database, deducts physical ingredients from inventory, creates a kitchen print job, and requires zero cash tender — all while completely bypassing `orders_discount`, `orders_comp`, and manager approval verification.

---

## 3. Deep Source Verification of Findings FIN-004 to FIN-015

| ID | Title | Key Source Locations | Primary Failure Mechanism |
| :--- | :--- | :--- | :--- |
| **FIN-004** | Historical Cash Refund Omitted from Shift Expected Cash | [`ShiftReconciliation.cs:44-68`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L44-L68), [`RefundOrderHandler.cs:95-106`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L95-L106) | Shift reconciliation only queries orders where `o.ShiftId == currentShiftId`. A cash refund issued during Shift B for an order placed in Shift A is paid from Shift B's cash drawer, but is ignored in Shift B's expected cash calculation. Shift B closes with an artificial cash shortage. |
| **FIN-007** | Backup Filename Collision Overwrite | [`CreateBackupHandler.cs:55-80`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/CreateBackupHandler.cs#L55-L80) | Backup filename timestamp uses `yyyyMMdd-HHmmss` (second resolution). Two backups triggered within the same second collide on file path, and SQL Server `BACKUP DATABASE ... WITH FORMAT` unconditionally overwrites the existing file. |
| **FIN-008** | Restore Active Financial Work Check Race | [`RestoreBackupHandler.cs:85-86, 150-154`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L85-L86) | Handler checks `FinancialJournalEntries.CountAsync(j => j.Status != "Complete")` without locking the application or stopping ingress. New financial transactions arriving immediately after this check are abruptly killed and rolled back by `ALTER DATABASE SET SINGLE_USER WITH ROLLBACK IMMEDIATE`. |
| **FIN-009** | Concurrent Open-Day Multi-Active Insertion | [`OpenBusinessDayHandler.cs:35-55`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L35-L55), [`FalahDbContext.cs:137-141`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137-L141) | No transaction, no table lock, and no filtered database unique index on `Status == 'Open'`. Concurrent requests with distinct `RequestId`s both see no active day and both insert active business days. |
| **FIN-010** | Sales Report Drops Immediate Money on Mixed Tender | [`SalesRules.cs:55-68`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs#L55-L68) | On an order with split cash and pending electronic payment, `SalesRules` treats `o.Total` as `PendingElectronicSales` instead of `o.ElectronicPayment.Amount`. It subtracts the entire order total, dropping the collected cash from `CollectedSales`. |
| **FIN-011** | Protected Balance Pre-Check Outside Transaction | [`OpenShiftHandler.cs:53-69`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L53-L69), [`ReceivePurchaseHandler.cs:77-97`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L77-L97) | `LoadAccountMovementsForUpdateAsync` executes `SELECT ... WITH (UPDLOCK, HOLDLOCK)` BEFORE `BeginTransactionAsync`. The locks are released immediately on query completion, allowing concurrent transactions to spend the same funds before the transaction starts. |
| **FIN-012** | Attendance Correction Credits Omitted from Payroll | [`PayrollRules.cs:95-101`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L95-L101), [`CorrectAttendanceHandler.cs:99-118`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L99-L118) | Correcting an attendance penalty or adding overtime posts an `Adjustment` entry with `Credit > 0`. `CalculatePayrollLines` sums `Debit` on Adjustments (deductions) but completely ignores `Credit` on Adjustments, depriving employees of legitimate compensation. |
| **FIN-014** | Successful Restore Misclassified as Failure | [`RestoreBackupHandler.cs:161-193, 213-220`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L161-L193) | `databaseWasReplaced` is set to `true` only after `SET MULTI_USER`. If `RESTORE DATABASE` succeeds but `SET MULTI_USER` encounters an error, the catch block leaves `databaseWasReplaced = false`, fails to clear the connection pool, and reports `Status = Failed` to the user despite the database being overwritten. |
| **FIN-015** | Post-Handover Checkout Commit to Closed Shift | [`CheckoutOrderHandler.cs:47-50, 145`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L47-L50), [`CloseShiftHandler.cs:62-79`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs#L62-L79) | `CheckoutOrderHandler` checks `shift.Status == Open` outside transaction with `AsNoTracking()`. If a manager closes the shift and completes cash handover while checkout prepares pricing/tax, checkout proceeds to commit cash sales into the closed shift. |

---

## 4. Double-Entry Ledger & Concurrency Architecture Review

### 4.1 EfFinancialJournal Evaluation
In [`EfFinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs):
- **Mechanism:** Operates on the ambient `DbContext`. Inserts a `FinancialJournalEntryEntity` with `Status = "Pending"`, marked `Complete` at the end of the transaction.
- **Strengths:** Primary key constraint on `IdempotencyKey` guarantees that duplicate inserts within the same database fail with a unique key violation.
- **Flaws:**
  - Idempotency keys are composite strings formatted by convention rather than strongly-typed schemas (e.g. `$"sale-{request.CheckoutRequestId}"`).
  - As proven in **FIN-001**, reusing the prefix `sale-` across distinct lifecycle events (`Checkout` vs `ConfirmElectronicPayment`) causes fatal collisions.
  - Ingestion keys should incorporate the event type: `$"sale-init-{CheckoutRequestId}"` vs `$"sale-elec-confirm-{OrderId}"`.

### 4.2 Treasury Balance & Locking Invariants
In [`TreasuryBalanceQuery.cs:LoadAccountMovementsForUpdateAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/TreasuryBalanceQuery.cs):
```csharp
SELECT * FROM TreasuryMovements WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE (SourceAccountId = {0} OR DestinationAccountId = {0})
```
- **Finding:** While the SQL hint `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` is correct for SQL Server pessimistic locking, it is **rendered useless** when invoked outside an active `IDbContextTransaction`.
- As proven in **FIN-011**, both [`OpenShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L53) and [`ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L77) execute this query prior to calling `_db.Database.BeginTransactionAsync(ct)`. Under default auto-commit connection semantics, the lock is acquired and immediately dropped before the subsequent statement executes.

---

## 5. Comprehensive Architectural Remediation Plan

The following remediation specifications are documented for implementation by development agents:

### 5.1 Remediation for FIN-001 (Split Payment Idempotency)
- In [`ConfirmElectronicPaymentHandler.cs:75`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75):
  Change the financial journal key from `$"sale-{orderEntity.CheckoutRequestId}"` to:
  ```csharp
  var saleKey = $"confirm-electronic-{orderEntity.Id}";
  ```
- Update the replay lookup and exception handling accordingly.

### 5.2 Remediation for FIN-002 (Pending Electronic Cancellation Reversal)
- In [`Order.cs:CancelElectronicPayment`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200):
  When `PaymentMethod == PaymentMethod.Multi`, require explicit handling of the cash leg.
- In [`CancelPendingElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs):
  Inspect existing `TreasuryMovements` for `orderEntity.Id`. If a posted cash `sale_collection` exists, automatically generate a corresponding `sale_reversal` or `refund` treasury movement debiting `tre-drawer` and returning cash to the customer before marking the order cancelled.

### 5.3 Remediation for FIN-003 (Uncollected Order Refund Guard)
- In [`Order.cs:ApplyRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220):
  Add domain invariant assertion:
  ```csharp
  if (!IsPaid) throw new DomainException("لا يمكن استرجاع طلب لم يتم تحصيله بعد.");
  ```
- In [`RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs):
  Assert `orderEntity.IsPaid == true` before performing any balance checks or movement generation.

### 5.4 Remediation for FIN-004 (Cross-Shift Cash Refund Allocation)
- In [`ShiftReconciliation.cs:ComputeBreakdownAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L59):
  Add `"refund"` to `DrawerWithdrawalTypes`:
  ```csharp
  private static readonly string[] DrawerWithdrawalTypes = { "cash_out", "employee_advance", "employee_withdrawal", "refund" };
  ```
  Ensure cash refunds originating from this shift's drawer are deducted from expected cash regardless of the order's original shift.

### 5.5 Remediation for FIN-005 (Supplier Payment Concurrency Lock)
- In [`PayPurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs#L36):
  Move `_db.Database.BeginTransactionAsync` to the top of the handler.
  Query the purchase row using:
  ```csharp
  var purchase = await _db.Purchases
      .FromSqlRaw("SELECT * FROM Purchases WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.PurchaseId)
      .Include(p => p.Payments)
      .FirstOrDefaultAsync(ct);
  ```
  Compute outstanding debt inside the locked transaction.

### 5.6 Remediation for FIN-006 (Generic Payroll Status Transition)
- In [`TransitionPayrollHandler.cs:26`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L26):
  Explicitly forbid `targetStatus == PayrollRunStatus.Paid`:
  ```csharp
  if (targetStatus == PayrollRunStatus.Paid)
      throw new DomainException("لا يمكن تغيير حالة المسير إلى مدفوع مباشرة؛ يجب استخدام أمر صرف الرواتب (PayPayroll).");
  ```

### 5.7 Remediation for FIN-007 (Backup Filename Collision)
- In [`CreateBackupHandler.cs:56`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/CreateBackupHandler.cs#L56):
  Append a high-resolution sub-second timestamp or GUID to the backup file name:
  ```csharp
  var fileName = $"falah-{databaseName}-{now:yyyyMMdd-HHmmss-fff}-{Guid.NewGuid():N[..6]}.bak";
  ```

### 5.8 Remediation for FIN-008 & FIN-014 (Restore Safety & Outcome Classification)
- For `FIN-008`: Add a global application maintenance gate that halts incoming HTTP requests before database teardown.
- For `FIN-014` in [`RestoreBackupHandler.cs:159`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L159):
  Set `databaseWasReplaced = true;` immediately after `await restore.ExecuteNonQueryAsync(ct);` succeeds, before attempting `SET MULTI_USER`.

### 5.9 Remediation for FIN-009 (Business Day Database Constraint)
- In [`FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137):
  Add a filtered unique index in EF Core configuration and database migration:
  ```csharp
  b.HasIndex(d => d.Status).IsUnique().HasFilter("[Status] IN ('Open', 'Closing', 'Reopened')");
  ```

### 5.10 Remediation for FIN-010 (Sales Report Split Leg Normalization)
- In [`SalesRules.cs:55`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs#L55):
  Calculate pending electronic amounts from the electronic split leg amount, not the whole order total:
  ```csharp
  var pendingElectronicSales = Money.FromPiastres(sales.Sum(o =>
      o.ElectronicPayment is { Status: ElectronicPaymentStatus.Pending } leg ? leg.Amount.Piastres : 0));
  ```

### 5.11 Remediation for FIN-011 (Transaction Scope for Protected Balances)
- In [`OpenShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L53-L69) and [`ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L77-L97):
  Move `await using var transaction = await _db.Database.BeginTransactionAsync(ct);` to execute **BEFORE** calling `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync`.

### 5.12 Remediation for FIN-012 (Attendance Correction Credits in Payroll)
- In [`PayrollRules.cs:100`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L100):
  Include `Adjustment` credits in bonus/adjustments:
  ```csharp
  var adjustments = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "Adjustment").Sum(e => e.Credit.Piastres));
  ...
  var net = baseAmount + overtime + extraShifts + bonuses + adjustments - deductions - emp.RequestedAdvanceDeduction;
  ```

### 5.13 Remediation for FIN-013 (Stock Outbound Movement Negation)
- In [`OwnerDashboardQueryService.cs:303`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L303):
  Conditionally negate outbound quantities:
  ```csharp
  .Select(g => new {
      InventoryItemId = g.Key,
      Balance = g.Sum(m => m.Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits)
  })
  ```

### 5.14 Remediation for FIN-015 (Checkout Shift Status Revalidation)
- In [`CheckoutOrderHandler.cs:146`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L146):
  Inside the database transaction, lock and re-validate shift status:
  ```csharp
  var lockedShift = await _db.Shifts
      .FromSqlRaw("SELECT * FROM Shifts WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.ShiftId)
      .FirstOrDefaultAsync(ct) ?? throw new ShiftNotOpenException("الوردية غير موجودة.");
  if (lockedShift.Status != nameof(ShiftStatus.Open))
      throw new ShiftNotOpenException("تم إغلاق الوردية أثناء إعداد الطلب.");
  ```

### 5.15 Remediation for FIN-016 (Delivery Fee Server Validation)
- In [`CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L66):
  Assert that `request.DeliveryFeePiastres >= 0`.
  For delivery orders with `DeliveryServiceId`, assert that the fee equals the configured service fee, or require `orders_discount` / `orders_comp` manager approval if the delivery fee is waived or reduced.

### 5.16 Remediation for FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001 (Safe Drop Closed Shift Guard)
- In [`SafeDropHandler.cs:45`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L45):
  Assert that the referenced shift is currently `Open`:
  ```csharp
  var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
      ?? throw new InvalidOperationException("الوردية غير موجودة.");
  if (shift.Status != nameof(ShiftStatus.Open))
      throw new DomainException("لا يمكن تسجيل إيداع آمن لوردية مغلقة. يجب أن تكون الوردية مفتوحة.");
  ```

### 5.17 Remediation for FIN-GATE-001 (In-Transaction Business Day Gate Verification)
- In [`DepositHandler.cs:58`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/DepositHandler.cs#L58), [`SafeDropHandler.cs:58`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L58), and [`AddExpenseHandler.cs:69`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs#L69):
  Move `BusinessDayGate.RequireOpenAsync` or re-validate inside the active transaction with `SELECT ... WITH (UPDLOCK)` on `BusinessDays` before committing movements.

### 5.18 Remediation for FIN-LSP-001 (Liskov Substitution in EfFinancialJournal)
- In [`EfFinancialJournal.cs:56`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L56):
  Implement `Find(string idempotencyKey)` to query `_db.FinancialJournalEntries` and map to `FinancialJournalEntry` instead of throwing `NotSupportedException`.

---

## 6. Specialist Wave 4 Audit Addendum & Invariant Mapping

During Wave 4 forensic evaluation of Treasury & Idempotency handlers, the following additional findings were confirmed:

| Finding ID | Severity | Root Cause Code Span | Impact | Remediation Status |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`** | **CRITICAL** | [`SafeDropHandler.cs:17-19, 45-46`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L17-L46) | Safe drop allows attributing drawer skimming movements to already-closed shifts, retroactively modifying printed shift reconciliations and creating false shortages in the active shift. | Documented in review file; remediation 5.16 defined. |
| **`FIN-GATE-001`** | **MEDIUM** | [`DepositHandler.cs:58`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/DepositHandler.cs#L58), [`SafeDropHandler.cs:58`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L58), [`AddExpenseHandler.cs:69`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs#L69) | `BusinessDayGate.RequireOpenAsync` executed with `.AsNoTracking()` outside transaction; movements commit under closed business day IDs. | Documented in review files; remediation 5.17 defined. |
| **`FIN-LSP-001`** | **MEDIUM** | [`EfFinancialJournal.cs:56-57`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L56-L57) | `IFinancialJournal.Find` throws `NotSupportedException` in production EF Core implementation, breaking polymorphic contract with `InMemoryFinancialJournal`. | Documented in review file; remediation 5.18 defined. |

---

## 7. Specialist Waves 5 & 6 Audit Addendum & Schema Forensics

### 7.1 Specialist Wave 5: Domain Financial Rules & Mirrored UI ViewModels
Wave 5 audited 42 callables across core domain calculation rules and cashier/shift UI ViewModels:
- [`CheckoutRules.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Domain/Orders/CheckoutRules.cs.md) (15 callables): Confirmed `FIN-MULTI-ELEC-001` (split payment electronic split ceiling restriction).
- [`FinancialRules.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Domain/Financial/FinancialRules.cs.md) (11 callables): Verified drawer reconciliation formula, safe drop ceilings, and holdlock queries.
- [`TaxRule.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Domain/Pricing/TaxRule.cs.md) (4 callables): Verified integer basis points VAT calculations without decimal drift.
- [`OrdersViewModel.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Client.CallCenterWinUI/ViewModels/OrdersViewModel.cs.md) (6 callables) & [`ShiftsViewModel.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Client.CallCenterWinUI/ViewModels/ShiftsViewModel.cs.md) (6 callables): Verified UI client-side validation gates, blind close cash denomination collection, and shift float inputs.

### 7.2 Specialist Wave 6: Core EF Core DbContext & Database Schema Audit (`FalahDbContext.cs`)
Wave 6 conducted an exhaustive, per-callable database and relational schema audit of [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs) (212 callables: 1 Constructor, 1 Method, 210 entity configuration lambdas across 58 entity blocks).
Output review generated: [`GitHub Copilot Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Infrastructure/Persistence/FalahDbContext.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Infrastructure/Persistence/FalahDbContext.cs.md).

#### Core Schema Findings Confirmed in Wave 6:
1. **Integer Piastres Currency Architecture:** All monetary amounts across the database schema (`Orders`, `OrderItems`, `Shifts`, `TreasuryMovements`, `Expenses`, `Purchases`, `PurchaseLines`, `SupplierLedger`, `EmployeeLedger`, `PayrollRuns`, `PayrollLines`) are represented as 64-bit signed integers (`long`), denominated in Piastres (1 EGP = 100 Piastres). Floating-point errors and decimal scale drift are completely eliminated.
2. **Defect `PRT-001` Confirmed (Hardware Ticket Duplication):** Lines 365–377 define `PrintJobEntity` with `b.HasIndex(j => j.CreateRequestId);` (non-unique) and lack a composite unique index on `(OrderId, Route)`. Network retries or re-dispatches create duplicate kitchen and customer receipts.
3. **Defect `FIN-009` Confirmed (Active Business Day Concurrency Window):** Lines 137–141 define `BusinessDayEntity` with `b.HasIndex(d => d.OpenRequestId).IsUnique();`. Unlike `ShiftEntity` (which enforces single-open concurrency via `[Status] = 'Open'`), `BusinessDayEntity` lacks a filtered unique index on `[Status] = 'Open'`, allowing racing requests to open multiple concurrent business days.
4. **Defects `FIN-005` & `FIN-006` Confirmed (Missing Concurrency Tokens):** While `OrderEntity`, `ShiftEntity`, and `DeliveryEntity` define `b.Property(x => x.Version).IsConcurrencyToken()`, `PurchaseEntity` and `PayrollRunEntity` lack concurrency tokens, permitting lost updates during concurrent payments or status transitions.
5. **Deduplication Success:** Verified that `PayrollRunEntity` includes `IX_PayrollRuns_Period_Live` filtered unique index on `Period` where `[Status] <> 'Cancelled'`, preventing concurrent double payroll runs.

---

### 7.3 Specialist Wave 7: Analytical Reporting & Financial Statement Integrity (`ReportingQueryService.cs`)
Wave 7 conducted an exhaustive, per-callable financial and reporting integrity audit of [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs) (202 callables: 1 Constructor, 25 Methods, 174 query/aggregation lambdas across 26 method blocks).
Output review generated: [`GitHub Copilot Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Infrastructure/Reports/ReportingQueryService.cs.md`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/GitHub%20Copilot%20Work/02-Callable-Inventory-and-Reviews/File-Reviews/src/Falah.Infrastructure/Reports/ReportingQueryService.cs.md) (4,158 lines).

#### Core Reporting Findings Confirmed in Wave 7:
1. **Daily Statement & Revenue Hierarchy Calculations:** Verified that `GetSalesReportAsync` correctly breaks down revenue into `ListSales`, `OffersValue`, `CompsValue`, `ManualDiscounts`, `GrossSales`, `NetSales`, and `CollectedSales`. Dates are mapped to Cairo midnight boundaries via `BusinessDateRange.ToUtcRange()`. Empty shift strings (`&shiftId=`) are safely guarded with `!string.IsNullOrEmpty(shiftId)`.
2. **Delivery Driver Cash Segregation (`LoadOrderIdsWithCashOutsideRestaurantAsync`):** Cash-on-delivery orders without settled custody are excluded from `CollectedSales` (`CashWithDrivers`), ensuring that money currently in transit with drivers is never reported as cash held by the restaurant till. Driver cash settlements post directly to `tre-main` (not `tre-drawer`).
3. **Historical Tax Immutability (`GetTaxReportAsync`):** Taxes aggregate exclusively from frozen order snapshots (`TaxAmountPiastres`, `TaxableBasePiastres`), preventing future tax policy mutations from altering historical accounting statements.
4. **Non-Offsetting Cash Drawer Discrepancies (`GetBusinessDayReconciliationAsync`):** Cashier shortages and overages are summed independently across shifts (`shortage` = sum of negative variances, `overage` = sum of positive variances). An overage on Shift 1 never offsets or conceals a shortage on Shift 2.
5. **Defect `FIN-010` Confirmed (Split Payment Whole-Order Pending Deduction):** In split payment orders with unconfirmed electronic tender, the pending deduction subtracts `o.Total.Piastres` (the entire order amount), incorrectly deducting the cash split portion already physically collected in the drawer until electronic confirmation.
6. **Defect `FIN-004` Confirmed (Accrual vs Cash-Basis Refund Timing Divergence):** `GetSalesReportAsync` attributes refunds to the original order's sale date, while `GetRefundActivityAsync` and shift drawer reconciliations attribute refunds to the operational shift that physically disbursed the cash payout.

---

## 8. Verification Status & Final Handoff Sign-off

- **Workstream Status:** **AUDIT INVESTIGATION COMPLETE — ALL 16 FINANCIAL WORKFLOWS & WAVES 1-7 FULLY AUDITED & DOCUMENTED**
- **Total Callables Audited Across Waves 1–7:** **641 Callables** across **23 Critical Files**
- **Code Modifications:** **0 files modified in `src/` or `tests/` (100% clean audit compliance)**
- **Next Phase:** Implementation of architectural remediations by authorized implementation agents in an isolated branch with dedicated disposable SQL Server container verification.

*Report compiled and certified by Financial & Database Integrity Specialist.*



# Financial Auditor Handoff - Wave 8

**Date**: 2026-10-25  
**Auditor**: Financial Integrity Auditor (Fale7 POS)  
**Status**: Wave 8 Complete (112 Callables Audited Across 2 Files)

---

## Completed Audits

1. **`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs` (64 callables)**:
   - Exhaustive callable-by-callable audit completed and documented in artifact storage.
   - **Critical Finding (FIN-013)**: Identified sign inversion in `LoadStockAsync` where `QuantityMilliUnits` is summed unsigned (`g.Sum(m => m.QuantityMilliUnits)`), causing outbound stock movements (`Direction == "Out"`, usage, waste) to add to inventory balance rather than reducing it. Contrast noted with `InventoryQueryService`.
   - **OpenExceptionCount vs In-Memory**: Verified `ControlExceptionStates` table query correctly reflects persisted open control exceptions.
   - **Currency & Periods**: Confirmed all monetary aggregation uses integer Piastres (`long`) and `BusinessDateRange.ToUtcRange()`.

2. **`src/Falah.Infrastructure/Reports/DailyStatementQueryService.cs` (48 callables)**:
   - Exhaustive callable-by-callable audit completed and documented in artifact storage.
   - **Reconciliation & Variance**: Analyzed cash vs electronic reconciliation, tender breakdowns, tax aggregation, and shift drawer variance calculations (`BuildShiftTotals`).

---

## Strict Rule Compliance
- **Audit Documentation Only**: Zero changes were made to `src/` or `tests/`. All reviews are fully documented in artifacts.
