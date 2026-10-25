# Deep Per-Callable Source Audit: `ShiftReconciliation.cs`

- **File Path:** [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs)
- **Role:** Shift Cash Drawer Expected Cash & Handover Breakdown Calculator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:30:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 14 (2 Methods, 12 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `ComputeExpectedCashAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:30:5:MethodDeclaration`
- **Signature:** `public static async Task<Money> ComputeExpectedCashAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct)`
- **Line Span:** Lines 30–31
- **Actual Behavior & Inputs:**
  Convenience helper method that delegates to `ComputeBreakdownAsync(db, shiftId, openingCash, ct)` and extracts the `.ExpectedCash` property from the resulting `ShiftCashBreakdown` record struct.
- **Callers, Dependencies & Side Effects:**
  - Callers: [`CloseShiftHandler.HandleAsync:96`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs#L96), [`PrepareShiftCloseHandler.HandleAsync:43`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/PrepareShiftCloseHandler.cs#L43).
  - Dependencies: `ComputeBreakdownAsync`.
  - Side effects: None (read-only query).
- **Financial Integrity Assessment:**
  Directly impacts the blind-close reconciliation figure used to compute discrepancies (cash shortages/overages) and determine manager approval requirements.
- **Classification:** Facade query method.

---

### 2. `ComputeBreakdownAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:42:5:MethodDeclaration`
- **Signature:** `public static async Task<ShiftCashBreakdown> ComputeBreakdownAsync(FalahDbContext db, string shiftId, Money openingCash, CancellationToken ct)`
- **Line Span:** Lines 42–81
- **Actual Behavior & Inputs:**
  Receives `db`, `shiftId`, `openingCash`, and `ct`. Computes all cash inflow and outflow components for a shift's cash drawer:
  1. Queries all `Orders` where `ShiftId == shiftId` including `Items` and `Refunds`.
  2. Converts entities to domain objects via `OrderMapper.ToDomain`.
  3. Computes `netCashSales` via `OrderPaymentQueries.CashSalesFromOrders` (aggregating `Total - RefundAmount` on paid cash orders).
  4. Computes `cashRefunds` via `OrderPaymentQueries.CashRefundsFromOrders`.
  5. Sums `Expenses` where `ShiftId == shiftId && PaidFromDrawer == true`.
  6. Queries posted `TreasuryMovements` for `shiftId` matching `cash_in`, `safe_drop`, or `DrawerWithdrawalTypes` (`cash_out`, `employee_advance`, `employee_withdrawal`).
  7. Calculates `expected` cash via `FinancialRules.CalculateExpectedDrawerCash`:
     `Expected = OpeningCash + NetCashSales - DrawerExpenses + CashIn - (Withdrawals + SafeDrops)`.
  8. Returns `ShiftCashBreakdown` record.
- **Callers, Dependencies & Side Effects:**
  - Callers: `ComputeExpectedCashAsync`, `PrintJobQueryService.ComposeShiftCloseSlipAsync`.
  - Dependencies: `FalahDbContext`, `OrderPaymentQueries`, `FinancialRules`, `ShiftCashBreakdown`.
  - Side effects: Read-only database queries across 3 tables (`Orders`, `Expenses`, `TreasuryMovements`).
- **Critical Financial Integrity Flaw (FIN-004):**
  > [!CRITICAL]
  > **Vulnerability `FIN-004` Confirmed:** Lines 44–50 query `db.Orders.Where(o => o.ShiftId == shiftId)`.
  > If a cash refund is issued during the current shift (Shift B) for an order originally placed in a prior shift (Shift A):
  > - [`RefundOrderHandler.cs:95-106`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L95-L106) records the refund against the original order (`order.ShiftId = Shift A`), while tagging the actual cash `TreasuryMovement` with the active shift (`Shift B`).
  > - Physical cash is paid out of Shift B's cash drawer.
  > - However, `ComputeBreakdownAsync` on Shift B only inspects orders belonging to Shift B!
  > - Furthermore, line 59 filters movements for `m.Type == "cash_in" || m.Type == "safe_drop" || DrawerWithdrawalTypes.Contains(m.Type)`.
  > - `DrawerWithdrawalTypes` (`line 28`) is defined as `{"cash_out", "employee_advance", "employee_withdrawal"}` and **EXCLUDES** `"refund"`!
  > - Consequently, the cash refund paid out of Shift B is **NEVER SUBTRACTED** from Shift B's expected cash!
  > - Shift B closes with an artificial cash shortage exactly equal to the refunded cash, potentially triggering false fraud investigations or unjustified disciplinary deductions against the innocent cashier.
- **Classification:** Core financial calculation method with historical cross-shift refund omission defect.

---

### 3. Orders Include Items Lambda (`o => o.Items`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:45:22:SimpleLambdaExpression`
- **Signature:** `o => o.Items`
- **Line Span:** Line 45
- **Actual Behavior & Inputs:**
  Navigation property selector passed to EF Core `.Include(o => o.Items)`.
- **Financial Integrity Assessment:**
  Ensures line items are loaded so `OrderMapper.ToDomain` can construct complete `Order` aggregates without throwing null references.
- **Classification:** EF navigation inclusion lambda.

---

### 4. Orders Include Refunds Lambda (`o => o.Refunds`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:45:44:SimpleLambdaExpression`
- **Signature:** `o => o.Refunds`
- **Line Span:** Line 45
- **Actual Behavior & Inputs:**
  Navigation property selector passed to EF Core `.Include(o => o.Refunds)`.
- **Financial Integrity Assessment:**
  Ensures refund entities attached to the shift's orders are populated so `OrderPaymentQueries` can calculate refund amounts.
- **Classification:** EF navigation inclusion lambda.

---

### 5. Orders Shift Filter Lambda (`o => o.ShiftId == shiftId`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:46:20:SimpleLambdaExpression`
- **Signature:** `o => o.ShiftId == shiftId`
- **Line Span:** Line 46
- **Actual Behavior & Inputs:**
  SQL WHERE clause filter restricting orders to those whose parent `ShiftId` equals `shiftId`.
- **Financial Integrity Assessment:**
  Locus of `FIN-004`. Because it strictly filters on `Order.ShiftId`, any refund executed in this shift for a prior shift's order is filtered out.
- **Classification:** Query filter predicate.

---

### 6. Drawer Expenses Filter Lambda (`e => e.ShiftId == shiftId && e.PaidFromDrawer`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:56:20:SimpleLambdaExpression`
- **Signature:** `e => e.ShiftId == shiftId && e.PaidFromDrawer`
- **Line Span:** Line 56
- **Actual Behavior & Inputs:**
  Filters `_db.Expenses.AsNoTracking()` to select only expenses associated with this shift AND marked as `PaidFromDrawer == true`.
- **Financial Integrity Assessment:**
  Correctly isolates drawer expenses from main treasury or electronic expenses, ensuring main office expenses do not artificially depress drawer expected cash.
- **Classification:** Query filter predicate.

---

### 7. Expense Amount Projection Lambda (`e => e.AmountPiastres`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:57:23:SimpleLambdaExpression`
- **Signature:** `e => e.AmountPiastres`
- **Line Span:** Line 57
- **Actual Behavior & Inputs:**
  Selector passed to `.SumAsync(...)` to calculate aggregate drawer expense piastres directly in SQL Server.
- **Financial Integrity Assessment:**
  Database-side sum ensures performance and numeric precision.
- **Classification:** SQL aggregate projection lambda.

---

### 8. Treasury Movements Filter Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:60:20:SimpleLambdaExpression`
- **Signature:** `m => m.ShiftId == shiftId && m.Status == nameof(MovementStatus.Posted) && (m.Type == "cash_in" || m.Type == "safe_drop" || DrawerWithdrawalTypes.Contains(m.Type))`
- **Line Span:** Lines 60–61
- **Actual Behavior & Inputs:**
  Filters `_db.TreasuryMovements.AsNoTracking()` for movements posted to this shift representing manual cash inflows, safe drops, and drawer withdrawals.
- **Financial Integrity Assessment:**
  Critical omission: Omits `m.Type == "refund"`. If a cash refund was issued from this shift's drawer, the treasury movement is posted with `ShiftId == shiftId` and `Type == "refund"`, but this filter ignores it.
- **Classification:** Query filter predicate.

---

### 9. In-Memory Cash In Filter Lambda (`m => m.Type == "cash_in"`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:63:57:SimpleLambdaExpression`
- **Signature:** `m => m.Type == "cash_in"`
- **Line Span:** Line 63
- **Actual Behavior & Inputs:**
  Filters the in-memory `movements` list for cash-in additions.
- **Classification:** In-memory collection filter.

---

### 10. Cash In Sum Lambda (`m => m.AmountPiastres`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:63:87:SimpleLambdaExpression`
- **Signature:** `m => m.AmountPiastres`
- **Line Span:** Line 63
- **Actual Behavior & Inputs:**
  Selects piastre amounts of `cash_in` movements for summation into `cashIn`.
- **Classification:** In-memory summation projection.

---

### 11. In-Memory Withdrawals Filter Lambda (`m => DrawerWithdrawalTypes.Contains(m.Type)`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:64:62:SimpleLambdaExpression`
- **Signature:** `m => DrawerWithdrawalTypes.Contains(m.Type)`
- **Line Span:** Line 64
- **Actual Behavior & Inputs:**
  Filters `movements` for types matching `DrawerWithdrawalTypes` (`cash_out`, `employee_advance`, `employee_withdrawal`).
- **Classification:** In-memory collection filter.

---

### 12. Withdrawals Sum Lambda (`m => m.AmountPiastres`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:64:111:SimpleLambdaExpression`
- **Signature:** `m => m.AmountPiastres`
- **Line Span:** Line 64
- **Actual Behavior & Inputs:**
  Selects piastre amounts of withdrawal movements for summation into `withdrawals`.
- **Classification:** In-memory summation projection.

---

### 13. In-Memory Safe Drops Filter Lambda (`m => m.Type == "safe_drop"`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:65:60:SimpleLambdaExpression`
- **Signature:** `m => m.Type == "safe_drop"`
- **Line Span:** Line 65
- **Actual Behavior & Inputs:**
  Filters `movements` for safe drops transferred from the drawer to the drop safe.
- **Classification:** In-memory collection filter.

---

### 14. Safe Drops Sum Lambda (`m => m.AmountPiastres`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs:65:92:SimpleLambdaExpression`
- **Signature:** `m => m.AmountPiastres`
- **Line Span:** Line 65
- **Actual Behavior & Inputs:**
  Selects piastre amounts of safe drop movements for summation into `safeDrops`.
- **Classification:** In-memory summation projection.
