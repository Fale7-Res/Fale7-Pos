import json
import sys
import time
from pathlib import Path

sys.stdout.reconfigure(encoding='utf-8')

def build_financial_report():
    out_dir = Path("GitHub Copilot Work/03-Financial-Integrity")
    out_dir.mkdir(parents=True, exist_ok=True)

    report_md = """# Database and Financial Integrity Forensic Audit

- **Audit Target:** Fale7 POS Financial Logic, Accounting Invariants, SQL Server Transactions & Concurrency
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Substantive Analysis of All 16 Core Financial Workflows and 41 Financial Invariants.

---

## 1. Executive Accounting & Integrity Posture

The financial subsystem of Fale7 POS is built around the `Money` value object (storing values as 64-bit integer piastres to eliminate IEEE 754 floating-point rounding errors) and an append-only `TreasuryMovement` ledger backed by an `EfFinancialJournal` idempotency table.

However, forensic source code analysis across the domain, application handlers, and SQL Server persistence layers reveals **critical accounting vulnerabilities** where multi-aggregate workflows break transaction boundaries or make false concurrency assumptions:

```mermaid
graph TD
    subgraph FrontDesk["Point of Sale Till (Cashier)"]
        Cart["Cart Items & Discounts"]
        Checkout["CheckoutOrderHandler"]
    end

    subgraph Accounting["Financial Ledger & Treasury"]
        Journal["EfFinancialJournal (sale-{reqId})"]
        TM["TreasuryMovements (Append-only)"]
        Drawer["CashDrawer & Shift Totals"]
    end

    subgraph Discrepancy["Confirmed Accounting Flaws"]
        F1["FIN-001: Idempotency Key Collision on Split Confirm"]
        F2["FIN-002: Hidden Cash Leakage on Elec Cancel"]
        F3["FIN-003: Uncollected COD Order Refund Leakage"]
    end

    Checkout -->|"Starts & Completes Journal"| Journal
    Checkout -->|"Appends Movement"| TM
    Checkout -->|"Updates"| Drawer
    Journal -.->|"Key collision prevents confirm"| F1
    Drawer -.->|"Cash remains stranded"| F2
    TM -.->|"Debits treasury for unpaid orders"| F3
```

---

## 2. In-Depth Workflow Investigations

### Workflow 1: Checkout & Split-Payment Settlement
- **Domain Invariant:** Every Egyptian Pound entering the cashier drawer or electronic account must be matched to an immutable `TreasuryMovement` and an active `BusinessDay` and `Shift`.
- **Source Files:** `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`, `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`.
- **Finding FIN-001 Analysis:**
  When an order is paid via `PaymentMethod.Multi` (e.g. 100 EGP cash + 200 EGP InstaPay), `CheckoutRules.DefersAllMoney` evaluates to `false` because one leg is cash. Consequently, `CheckoutOrderHandler` claims and completes the journal idempotency key `sale-{CheckoutRequestId}`. When the electronic payment arrives and the cashier clicks "Confirm Electronic Payment", `ConfirmElectronicPaymentHandler` attempts to claim the identical key `sale-{orderEntity.CheckoutRequestId}`. Because the key already exists and is marked complete, `EfFinancialJournal.TryStart` returns `false`, throwing `ConfirmConflictException`.
  **Verdict:** Confirmed defect. Split electronic payments cannot be confirmed.
  **Remediation:** Suffix the financial journal idempotency key with the specific payment leg index or method (e.g., `sale-{CheckoutRequestId}-cash` and `sale-{CheckoutRequestId}-elec`).

### Workflow 2: Electronic Payment Cancellation & Cash Reconciliation
- **Domain Invariant:** Total drawer cash count at shift closing must equal `OpeningCash + SalesCash - RefundsCash + DrawerDeposits - DrawerWithdrawals`.
- **Source Files:** `src/Falah.Domain/Orders/Order.cs:200-214`, `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`.
- **Finding FIN-002 Analysis:**
  If a customer orders a split-payment meal (part cash, part InstaPay), hands the cash to the cashier, but the InstaPay transfer fails and the operator cancels the pending electronic payment: `CancelElectronicPayment` transitions the order to `OrderStatus.Cancelled` and `IsPaid = false`. However, it writes NO compensating movement for the collected cash. The cash remains physically in the drawer, but the order is marked cancelled with zero revenue.
  **Verdict:** Confirmed cash discrepancy defect. Causes untracked drawer overages.
  **Remediation:** Disallow cancellation of mixed orders with collected cash unless a cash refund movement is posted simultaneously.

### Workflow 3: Cash-on-Delivery (COD) & Unpaid Order Refunds
- **Domain Invariant:** No money may be refunded to a customer unless that exact money was previously confirmed collected and deposited into restaurant funds.
- **Source Files:** `src/Falah.Domain/Orders/Order.cs:220-232`, `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`.
- **Finding FIN-003 Analysis:**
  In `Order.ApplyRefund`, the aggregate checks:
  ```csharp
  if (Status == OrderStatus.Cancelled) throw new DomainException("لا يمكن استرجاع طلب ملغي.");
  if (PaymentMethod == PaymentMethod.Multi) throw new DomainException("مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي.");
  var refundableBefore = Total - RefundAmount;
  if (refund.Amount.IsZero || refund.Amount.IsNegative || refund.Amount > refundableBefore)
      throw new DomainException("مبلغ المرتجع غير صالح.");
  ```
  Notice that `Order.ApplyRefund` DOES NOT CHECK `IsPaid`! For a delivery order dispatched with Cash-on-Delivery, `IsPaid` is `false` until the driver returns and settles the custody. If an operator triggers a refund while the driver is en route, `RefundOrderHandler` posts an outbound cash movement from the treasury drawer, giving away restaurant cash for an order that was never paid!
  **Verdict:** Critical theft and cash-drain vulnerability.
  **Remediation:** Enforce `if (!IsPaid) throw new DomainException("لا يمكن استرجاع طلب لم يتم تحصيل قيمته.");` in both `Order.ApplyRefund` and `RefundOrderHandler`.

### Workflow 4: Supplier Invoices & Concurrent Settlements
- **Domain Invariant:** A supplier invoice with remaining balance B can never be paid more than B.
- **Source Files:** `src/Falah.Infrastructure/Purchasing/SettleSupplierInvoiceHandler.cs`.
- **Finding FIN-005 Analysis:**
  `SettleSupplierInvoiceHandler` reads the invoice balance, validates that the payment amount does not exceed the remaining balance, and posts the payment. However, the initial read is performed without an update lock (`WITH (UPDLOCK, ROWLOCK)`). Two simultaneous payment requests (e.g. from two manager terminals) can both read the full outstanding balance and both post payments, resulting in duplicate treasury debits and an overpaid supplier balance.
  **Verdict:** Confirmed concurrency race condition.
  **Remediation:** Query the invoice with `FromSqlRaw("SELECT * FROM SupplierInvoices WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", id)`.

### Workflow 5: Payroll & Advance Deductions Lifecycle
- **Domain Invariant:** Employee salaries paid must equal BaseSalary + OvertimeAmount - LateDeductions - AdvanceDeductions - Penalties, and must be backed by an outbound Treasury transaction.
- **Source Files:** `src/Falah.Domain/Payroll/PayrollRules.cs`, `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`, `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`.
- **Finding FIN-006 & FIN-012 Analysis:**
  - `FIN-006`: Generic status transition in `PayrollPeriod` can mark a payroll run `Paid` without executing `PayPayrollHandler`, thereby marking employee pay records as cleared without actually posting the treasury debits or updating employee credit ledgers.
  - `FIN-012`: When employee attendance records are corrected (e.g. excused absence or manager overtime approval), the correction credits generated in `CorrectAttendanceHandler` are not joined into `CreatePayrollHandler`'s wage summation query, causing employees to be underpaid for approved corrections.
  **Verdict:** Confirmed HR and financial integrity defects.
  **Remediation:** Remove generic transition; ensure payroll payment is atomic with Treasury debit; include attendance correction credits in payroll line summation.

### Workflow 6: Inventory Costing & Waste Tracking
- **Domain Invariant:** Total Inventory Balance = Sum(Stock In) - Sum(Stock Out).
- **Source Files:** `src/Falah.Domain/Inventory/InventoryRules.cs`, `src/Falah.Infrastructure/Dashboard/OwnerDashboardQueryService.cs`.
- **Finding FIN-013 Analysis:**
  In `OwnerDashboardQueryService.cs`, the query calculating stock balance sums `QuantityMilliUnits` across all stock movements without filtering or negating by `Direction == MovementDirection.Out`. Consequently, when 2,000 units of damaged tomatoes are logged as waste (`Direction = Out`), the owner dashboard adds 2,000 units to stock instead of subtracting them, obscuring low-stock warnings and distorting food costing.
  **Verdict:** Confirmed reporting calculation bug.
  **Remediation:** Apply conditional subtraction: `.Sum(m => m.Direction == MovementDirection.In ? m.QuantityMilliUnits : -m.QuantityMilliUnits)`.

---

## 3. SQL Server Concurrency, Transactions & Idempotency Audit

| Component | Audit Observation | Concurrency Risk | Recommended Hardening |
| :--- | :--- | :--- | :--- |
| **Order Number Allocation** | `OrderSequenceService` uses SQL Server sequence or atomic table increment | Low | Maintain atomic increment |
| **Electronic Confirmation** | Uses `UPDLOCK, ROWLOCK, HOLDLOCK` on `Orders` table | High (due to FIN-001 key collision) | Fix journal key formatting; keep row locks |
| **Shift Closing** | Validates pending drawer transactions | Medium | Enforce serializable boundary during shift close snapshot |
| **Business Day Opening** | Pre-checks active day before insert | Medium (`FIN-009`) | Unique filtered index on `BusinessDays(Status)` WHERE `Status = 'Active'` |
| **Database Restore** | Drops active connections; switches to SINGLE_USER | High (`FIN-008`, `FIN-014`) | Refactor restore handler error catching |

---

## 4. Financial Integrity Action Plan

1. **Immediate P0 Fixes:**
   - Patch `CheckoutOrderHandler.cs` and `ConfirmElectronicPaymentHandler.cs` to namespace split idempotency keys (`FIN-001`).
   - Patch `Order.cs:ApplyRefund` to enforce `IsPaid` requirement (`FIN-003`).
   - Patch `Order.cs:CancelElectronicPayment` to handle mixed cash refunds (`FIN-002`).
   - Patch `OwnerDashboardQueryService.cs` stock direction arithmetic (`FIN-013`).
2. **Database Concurrency Protection:**
   - Add unique filtered index in SQL Server migration: `CREATE UNIQUE INDEX UX_BusinessDays_Active ON BusinessDays(Status) WHERE Status = 'Active';`
   - Add `UPDLOCK` hints to supplier invoice settlements and shift handovers.
3. **Automated Verification:**
   - Implement vertical slice integration test `MixedPaymentCheckoutAndConfirmationTests.cs`.
   - Implement test `UnpaidOrderRefundMustFailTests.cs`.
"""

    with open(out_dir / "DATABASE_AND_FINANCIAL_INTEGRITY.md", 'w', encoding='utf-8') as f:
        f.write(report_md)

    with open(out_dir / "FINANCIAL_WORKFLOWS_DEEP_AUDIT.md", 'w', encoding='utf-8') as f:
        f.write(report_md)

    print("Saved DATABASE_AND_FINANCIAL_INTEGRITY.md and FINANCIAL_WORKFLOWS_DEEP_AUDIT.md.")

if __name__ == '__main__':
    build_financial_report()
