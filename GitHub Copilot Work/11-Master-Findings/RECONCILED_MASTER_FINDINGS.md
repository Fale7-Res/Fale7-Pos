# Reconciled Master Findings Ledger

- **Total Documented Findings:** **77**
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Reconciliation Authority:** GitHub Copilot Independent Software Audit
- **Verification Standard:** Source-grounded, callable-traced, independent challenge verification.

---

## 1. Finding Dispositions Summary

| Disposition | Count | Percentage | Description |
| :--- | :--- | :--- | :--- |
| **INDEPENDENTLY_CORROBORATED** | **25** | 32.5% | Categorized and verified disposition |
| **CORROBORATED_WITH_QUALIFICATIONS** | **24** | 31.2% | Categorized and verified disposition |
| **REQUIRES_RUNTIME_VERIFICATION** | **21** | 27.3% | Categorized and verified disposition |
| **REQUIRES_OWNER_BUSINESS_DECISION** | **5** | 6.5% | Categorized and verified disposition |
| **INDEPENDENTLY_CORROBORATED_AND_CORRECTED** | **2** | 2.6% | Categorized and verified disposition |

---

## 2. Findings Summary by Category and Severity

| Category | High | Medium | Low | Total |
| :--- | :--- | :--- | :--- | :--- |
| **FINANCIAL_AND_DATABASE_INTEGRITY** | 16 | 25 | 4 | **45** |
| **ORDER_LIFECYCLE_AND_WORKFLOW** | 1 | 1 | 0 | **2** |
| **PRINTING_AND_HARDWARE** | 2 | 3 | 1 | **6** |
| **SECURITY_AND_ACCESS_CONTROL** | 2 | 1 | 0 | **3** |
| **UI_UX_AND_DESKTOP_CLIENT_STATE** | 11 | 10 | 0 | **21** |
| **TOTAL** | **32** | **40** | **5** | **77** |

---

## Financial And Database Integrity

### FIN-001: Mixed cash/electronic checkout cannot confirm its electronic leg
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Orders/CheckoutRules.cs` (lines [[69, 73]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[253, 275]]), `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs` (lines [[75, 84]]), `src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs` (lines [[24, 38]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[253, 275]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[253, 275]])
- **Root Cause:** CheckoutOrderHandler.cs (line 148) posts the cash tender leg using idempotency key 'sale-{CheckoutRequestId}'. When ConfirmElectronicPaymentHandler.cs (line 75) attempts to post the deferred electronic split leg, it attempts to insert into EfFinancialJournal using the exact same key 'sale-{CheckoutRequestId}', throwing a unique constraint violation.
- **Impact:** Split payment completion failure. Cash is accepted and deposited into the drawer, but the electronic leg (InstaPay/Card) can never be confirmed on the server; the order remains stuck in pending payment state.
- **Remediation Plan:** Key journal entries by stable leg identity, e.g. 'sale-{CheckoutRequestId}-{TenderType}'. Transition pending electronic leg atomically to confirmed.
- **Required Regression Tests:**
  - Mixed cash+InstaPay and card+Wallet checkout, confirmation, retry, and concurrent confirm; verify exact tender totals and one movement per leg.
  - Replay same request and verify stable movement identity.
- **Acceptance Criteria:**
  - Each tender leg can complete independently and exactly once.
  - Confirmed electronic state and its movement commit atomically; channel totals reconcile.

---

### FIN-002: Cancelling pending mixed electronic payment hides collected cash without reversal
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Orders/Order.cs` (lines [[200, 213]]), `src/Falah.Domain/Orders/OrderPaymentQueries.cs` (lines [[29, 47]]), `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs` (lines [[39, 57]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[255, 267]])
- **Root Cause:** Order.cs:CancelElectronicPayment sets order Status to Cancelled without issuing a reversal journal entry for cash tender legs that were already deposited into the till drawer at checkout.
- **Impact:** Treasury divergence. Drawer cash remains physically and logically deposited, but order disappears from sales reports, causing shift drawer reconciliation discrepancies.
- **Remediation Plan:** When cancelling a mixed tender order, check if any cash leg was deposited and atomically post a reversal journal movement to the drawer.
- **Required Regression Tests:**
  - Cancel cash+electronic and card+electronic orders; reconcile treasury, operational cash, and reports.
  - Race cancel against confirm and require one terminal tender outcome.
  - Verify refund only when funds are actually returned.
- **Acceptance Criteria:**
  - Cancelled order/payment state preserves every collected tender until a matching reversal exists.
  - Order, tender, treasury, and report totals reconcile after either winning race.

---

### FIN-003: Refund can debit money for an order that was never collected
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Orders/Order.cs` (lines [[220, 233]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[102, 120]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[102, 120]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[102, 120]])
- **Root Cause:** Order.cs:ApplyRefund and RefundOrderHandler.cs do not verify that the order was actually settled (IsPaid == true) before approving a cash refund.
- **Impact:** Cash drain. An operator can issue a cash refund against an unpaid Cash-On-Delivery order or pending InstaPay order, draining drawer funds for money never collected.
- **Remediation Plan:** Enforce invariant in Order.ApplyRefund that order must be fully collected (IsPaid == true) and refund amount cannot exceed collected tender.
- **Required Regression Tests:**
  - Unpaid COD, pending electronic, confirmed paid, partial collected, and unsupported mixed cases.
  - Race refund with cancel/confirm and concurrent refunds; rejected cases create no refund/movement and balances remain unchanged.
- **Acceptance Criteria:**
  - Cumulative refunds never exceed the order’s actually settled amount per tender leg.
  - Rejected unpaid refund leaves no partial writes; valid refund atomically records refund and outflow.

---

### FIN-004: Historical cash refund omitted from current-shift expected cash
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Orders/OrderPaymentQueries.cs` (lines [[31, 51]]), `src/Falah.Domain/Orders/RefundRules.cs` (lines [[39, 43]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[95, 110]]), `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs` (lines [[42, 68]])
- **Root Cause:** Drawer mixes lifetime order net amounts with selected operational movement types and does not allocate refunds by OperationalShiftId.
- **Impact:** A cash refund executed during current shift B for a sale from shift A may reduce lifetime/order net while the drawer reconciliation for B omits the refund outflow or allocates it to the original sale; expected cash and actual drawer diverge.
- **Remediation Plan:** Compute drawer flow from posted operational cash movements or join refund rows by OperationalShiftId; avoid double deduction for same-shift refunds and preserve immutable closed shift snapshots. Separate original sale period from refund execution period.
- **Required Regression Tests:**
  - Historical shift A sale refunded in B; verify B actual/expected and final close balance reconcile.
  - Same-shift partial cash refund counted exactly once.
  - Historic noncash refund and cross-business-day reports preserve attribution; idempotent retry.
- **Acceptance Criteria:**
  - Each refund cash outflow appears once in the shift/business period in which it was executed.
  - Historical closed shift snapshot remains immutable; current operational drawer reconciles to movements.

---

### FIN-005: Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs` (lines [[75, 111]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[277, 305]]), `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs` (lines [[95, 115]]), `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs` (lines [[95, 115]]), `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs` (lines [[95, 115]]), `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs` (lines [[95, 115]]), `src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs` (lines [[75, 111]]), `src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs` (lines [[75, 111]])
- **Root Cause:** PayPurchaseHandler.cs (lines 59-74) reads supplier invoice balance without UPDLOCK or serializable isolation, allowing concurrent transactions to each read the remaining balance before either updates it.
- **Impact:** Supplier overpayment and invoice balance corruption under concurrent settlement clicks.
- **Remediation Plan:** Execute invoice settlement within an explicit Serializable transaction or apply pessimistic locking (UPDLOCK, ROWLOCK) on the purchasing invoice record.
- **Required Regression Tests:**
  - Barrier: two different 80 settlements against outstanding 100, both same account and different accounts; at most one succeeds, total <=100 and cache equals ledger.
  - Same request replay returns committed result.
- **Acceptance Criteria:**
  - For each invoice, committed settlements never exceed current outstanding.
  - Cached paid/outstanding fields match ledger under serialized settlements.
  - Retries remain idempotent and deadlock policy is bounded.

---

### FIN-006: Generic payroll transition marks run Paid without paying treasury or employee ledger
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[12, 26]]), `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs` (lines [[65, 78]]), `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs` (lines [[25, 58]])
- **Root Cause:** TransitionPayrollHandler.cs permits generic status transition of an approved payroll run directly to 'Paid' without generating corresponding treasury journal disbursements or employee ledger settlement entries.
- **Impact:** Payroll status marked Paid without actual money leaving treasury or settling employee balances.
- **Remediation Plan:** Disallow transition to Paid via generic TransitionPayrollHandler. Require execution exclusively through PayPayrollHandler which generates atomic treasury and ledger entries.
- **Required Regression Tests:**
  - Generic Approved->Paid is rejected and writes nothing.
  - Dedicated pay atomically creates expected movement and each settlement.
  - Actor without payroll_pay cannot set Paid through generic route.
  - Concurrent cancel/pay results in exactly one consistent terminal state.
- **Acceptance Criteria:**
  - Paid status is equivalent to completed committed payout effects.
  - No API path can mark paid without required payroll_pay authorization and all financial rows.

---

### FIN-007: Backup filenames collide within a second and WITH FORMAT can overwrite prior backup
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Infrastructure/Backup/CreateBackupHandler.cs` (lines [[76, 84]]), `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs` (lines [[127, 151]]), `src/Falah.Infrastructure/Backup/CreateBackupHandler.cs` (lines [[76, 84]]), `src/Falah.Infrastructure/Backup/CreateBackupHandler.cs` (lines [[76, 84]])
- **Root Cause:** Second-resolution timestamp treated as unique filename; no GUID/exclusive reservation. Retention reasons over records not unique physical paths.
- **Impact:** Two backup creations within timestamp resolution can target the same physical filename; later WITH FORMAT may overwrite a previously recorded backup or fail, while retention may count multiple records for one path.
- **Remediation Plan:** Use collision-resistant immutable backup identity and exclusive create/no-overwrite destination semantics; preserve record-to-file identity. Retention should validate distinct canonical paths and never delete a file still referenced by a retained record.
- **Required Regression Tests:**
  - Parallel same-time filename generation returns distinct IDs/paths.
  - Existing destination collision refuses overwrite.
  - Retention with duplicate path references preserves all retained valid backups.
  - Later isolated SQL/media test only with explicit approval.
- **Acceptance Criteria:**
  - Each successful backup record resolves to a unique immutable artifact.
  - A collision cannot silently overwrite a prior artifact; retention cannot delete a retained target.

---

### FIN-008: Restore pending-journal check does not exclude active or newly accepted financial work
- **Severity:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[148, 167]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[275, 279]]), `src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs` (lines [[29, 47]]), `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[148, 167]]), `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[148, 167]]), `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[148, 167]])
- **Root Cause:** Pending journal staged and completed before common SaveChanges cannot reliably reveal in-flight work; a count snapshot is not writer exclusion. Restore does not put API/print/background writers into maintenance barrier.
- **Impact:** Restore’s pending-journal check does not exclude active or newly accepted writes. Restore may enter SINGLE_USER/replace while acknowledged financial operation or background writer is in flight; recovery of latest acknowledged work is not assured by the current check.
- **Remediation Plan:** Use a database-backed maintenance admission lease respected by all mutating handlers and background workers; drain in-flight operations, pause workers, take safety backup, restore and verify, clear stale pools/caches/sessions as designed, then resume only after durable outcome recording.
- **Required Regression Tests:**
  - Pure coordinator test: after maintenance lease acquired, new writes are rejected and prior writes drained.
  - Dedicated disposable instance restore with approved fault injection and proof of last acknowledged sale recovery.
  - Exercise job/API writers and restore failures; never use user-active database.
- **Acceptance Criteria:**
  - Restore cannot begin until writers are excluded/drained.
  - Every accepted operation is either included in pre-restore safety point or has an explicit replay/recovery record.
  - Failure leaves a known healthy database and maintenance mode controlled.

---

### FIN-009: Concurrent different open-day requests create multiple active business days
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/BusinessDay/BusinessDayRules.cs`, `src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs` (lines [[22, 29]]), `src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs` (lines [[34, 55]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[137, 141]])
- **Root Cause:** Single-active invariant protected only by read check; no shared singleton lock or unique active-day representation.
- **Impact:** Concurrent requests with distinct open-day IDs can both pass read-before-write admission, leaving multiple active business days; queries/gates selecting first active day may attach new finance to inconsistent periods.
- **Remediation Plan:** Represent single active ownership with a database-enforced singleton/unique active-state key acquired transactionally by open and reopen. Reconcile existing duplicate active rows explicitly before constraint migration; make read gate fail closed if invariant is corrupt.
- **Required Regression Tests:**
  - Concurrent opens with distinct IDs: exactly one active day; same request replay returns same day.
  - Race reopen versus open; exactly one active day.
  - Corrupt duplicate fixture makes gate fail closed and emits diagnosable error.
- **Acceptance Criteria:**
  - At most one active day exists across app processes.
  - Open/reopen admission and all active-day readers use the same ownership invariant.

---

### FIN-010: Collected sales report drops immediate money from pending mixed payments
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/Orders/OrderPaymentQueries.cs` (lines [[34, 47]]), `src/Falah.Domain/Reports/SalesRules.cs` (lines [[55, 70]]), `tests/Falah.Domain.Tests/CheckoutRulesTests.cs` (lines [[109, 124]])
- **Root Cause:** Any pending electronic leg triggers subtraction of whole order total, instead of pending leg amount; report contradicts own cash channel totals.
- **Impact:** Sales report may subtract full order amount while an electronic leg is pending even though immediate cash/card leg was collected, understating collected sales/revenue and contradicting channel reconciliation.
- **Remediation Plan:** Compute pending collection as the pending leg amount(s) and separately report gross/net sale recognition from tender settlement timing. Do not infer collection solely from order IsPaid or subtract whole order for partial pending tender.
- **Required Regression Tests:**
  - Mixed cash/electronic and card/electronic pending: collected amount equals immediate leg and pending equals deferred leg.
  - After confirm, totals conserve order amount across channels.
  - Compare report totals to treasury movements and payment query for each state.
- **Acceptance Criteria:**
  - For each order, collected plus pending amounts reconcile to eligible tender total; no collected leg disappears while another awaits confirmation.

---

### FIN-011: Shift opening and purchase receipt check protected account balance before their transactions
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` (lines [[73, 97]]), `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs` (lines [[77, 86]]), `src/Falah.Infrastructure/Treasury/TreasuryMapper.cs` (lines [[39, 49]]), `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs` (lines [[77, 86]]), `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs` (lines [[77, 86]])
- **Root Cause:** UPDLOCK/HOLDLOCK loader invoked before BeginTransaction; statement-end implicit transaction releases lock, balance snapshot stale at spending. SingleOpen index protects only shift rows.
- **Impact:** Shift opening float and/or purchase receipt may authorize against stale Main account balance because protective lock query occurs before transaction; competing outflow may spend same available funds, potentially allowing negative pooled account or failed/partial admission.
- **Remediation Plan:** Start transaction before protected account balance load/check and hold lock through float/receipt writes; establish consistent account/shift/invoice lock order; make retries safe and preserve real concurrency errors.
- **Required Regression Tests:**
  - Barrier-controlled open-float versus withdrawal/expense/purchase distinct requests; account never negative and exactly feasible operation set commits.
  - Same open request replay and single-active-shift invariant still hold.
  - Verify purchase receipt document/inventory/financial rows atomic.
- **Acceptance Criteria:**
  - Protected balance read and spend are in one transaction with effective lock lifetime.
  - No successful concurrent commits exceed available balance; rejected command has no partial effects.

---

### FIN-012: Attendance correction credits do not enter calculated payroll
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[95, 109]]), `src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs` (lines [[92, 117]])
- **Root Cause:** Correction records signed Adjustment entries; payroll includes Adjustment debits only, with no credit earnings bucket.
- **Impact:** A positive attendance correction recorded as Adjustment credit may be ignored by payroll’s included earnings/deduction categories, so the corrected employee period net can remain lower than ledger balance.
- **Remediation Plan:** Reconcile both credit and debit sides of Adjustment into payroll net; preserve correction attribution and original earning basis if required; keep payroll freeze semantics explicit.
- **Required Regression Tests:**
  - Payroll input with prior Deduction debit plus correcting Adjustment credit restores expected net.
  - Attendance correction followed by payroll calculation and report agrees with employee ledger.
  - Correction before run creation and after run freeze follow documented policy.
- **Acceptance Criteria:**
  - Every eligible period employee-ledger credit/debit included exactly once in payroll liability.
  - Payroll line and ledger are consistent; post-freeze correction follows explicit reopen/adjustment policy.

---

### FIN-013: Owner dashboard adds outgoing stock instead of subtracting it
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs` (lines [[297, 314]])
- **Root Cause:** OwnerDashboardQueryService.cs calculates inventory stock movements by summing QuantityMilliUnits without multiplying by -1 when Direction == Out.
- **Impact:** Inventory stock overstatement on owner dashboard. Waste and sales increase stock totals rather than decreasing them.
- **Remediation Plan:** Apply proper sign negation: (m.Direction == StockMovementDirection.Out ? -m.QuantityMilliUnits : m.QuantityMilliUnits).
- **Required Regression Tests:**
  - Seed positive-magnitude In and Out movements plus count adjustments/waste; dashboard balance and low-stock set equal inventory overview.
  - Cover zero/negative normalized inputs and multiple movement kinds.
- **Acceptance Criteria:**
  - Dashboard stock projection equals canonical inventory balance for every item.
  - Out never increases available quantity; count corrections are treated per shared domain rule.

---

### FIN-014: Restore success followed by MULTI_USER failure is classified as database not replaced
- **Severity:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[211, 234]]), `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[211, 234]]), `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[211, 234]])
- **Root Cause:** databaseWasReplaced becomes true only after RESTORE and MULTI_USER both return. If restore159 succeeded and MULTI_USER164 throws/cancels, catch releases mode best-effort but leaves false flag and Failed status.
- **Impact:** Operator may be told restore failed after database replacement; RecordOutcome false branch does not re-add disappeared restore row or clear killed connection pool. Incorrect outcome can encourage a dangerous retry. Actual failure at this precise boundary has not been produced.
- **Remediation Plan:** Track restore replacement separately from recovery/availability state; persist explicit replaced-but-recovery-incomplete outcome outside replaced database. Resolve uncertain SQL acknowledgments through a recovery protocol; do not auto-retry destructive restore.
- **Required Regression Tests:**
  - Fake coordinator injects failure/cancellation after RESTORE but before/during MULTI_USER.
  - Approved disposable-instance fault injection verifies replaced database is never reported untouched and no automatic retry occurs.
- **Acceptance Criteria:**
  - Restore command outcome distinguishes database replacement from return to MULTI_USER/availability; partial success is durably recorded outside replaced DB.
  - Injected post-RESTORE mode failure never reports database untouched and never triggers automatic retry.

---

### FIN-015: Checkout can commit after its previously open shift has completed handover
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Domain/Financial/TreasuryMovement.cs` (lines [[32, 60]]), `src/Falah.Domain/Orders/CheckoutRules.cs` (lines [[84, 114]]), `src/Falah.Domain/Orders/Order.cs` (lines [[66, 110]]), `src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs` (lines [[23, 29]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[248, 279]]), `src/Falah.Infrastructure/Orders/InventoryDeductionService.cs` (lines [[77, 97]]), `src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs` (lines [[22, 44]]), `src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs` (lines [[64, 89]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[116, 129]]), `src/Falah.Infrastructure/Persistence/OrderSequenceService.cs` (lines [[19, 28]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[125, 164]]), `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs` (lines [[30, 68]]), `src/Falah.Server/Program.cs` (lines [[1370, 1379]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[248, 279]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[248, 279]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[248, 279]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[125, 164]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[125, 164]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[116, 129]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[116, 129]]), `src/Falah.Infrastructure/Orders/InventoryDeductionService.cs` (lines [[77, 97]]), `src/Falah.Infrastructure/Orders/InventoryDeductionService.cs` (lines [[77, 97]]), `src/Falah.Server/Program.cs` (lines [[1370, 1379]]), `src/Falah.Server/Program.cs` (lines [[1370, 1379]])
- **Root Cause:** Open shift predicate read before checkout transaction is not revalidated/held through commit; closing uses a different correctly locked Shift update that checkout does not perform.
- **Impact:** Recorded closed shift expected/actual/handover exclude newly committed cash order while order and posted drawer collection reference that closed shift; new order time can be later than ClosedAtUtc. Stock can also be consumed when configured. No cash loss, fraud, exploit, or accepted real write observed.
- **Remediation Plan:** Serialize checkout admission and shift close with a shared transaction-held Shift row protocol: lock/revalidate Open before writes, hold through commit; alternatively use a documented Closing/drain process. Preserve closed snapshots.
- **Required Regression Tests:**
  - Barrier after checkout shift read and before transaction; close then resume checkout: reject with zero order/payment/stock effects.
  - Reverse ordering: checkout admission held; close waits and includes sale.
  - Same-key replay after close remains nonmutating.
- **Acceptance Criteria:**
  - Fresh checkout and shift close serialize through shared shift admission; every accepted checkout is included in handover or rejected with no effects.
  - Same-key replay after close remains idempotent and nonmutating.

---

### FIN-016: Unvalidated signed delivery fee can bypass intended discount and comp authorization
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Orders/CheckoutOrderRequest.cs` (lines [[22, 38]]), `src/Falah.Domain/Common/Money.cs` (lines [[13, 17], [31, 34]]), `src/Falah.Domain/Orders/CheckoutRules.cs` (lines [[35, 39], [99, 121]]), `src/Falah.Domain/Orders/Order.cs` (lines [[66, 110]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[65, 73], [102, 143], [249, 280]]), `src/Falah.Server/Program.cs` (lines [[1370, 1393]])
- **Root Cause:** CheckoutOrderHandler.cs (line 142) accepts request.DeliveryFeePiastres directly from client payload without verifying against DeliveryService.DefaultFee or checking manager authorization.
- **Impact:** Delivery fee tampering. A client can supply negative or zero delivery fee to discount an order without triggering manager discount authorization.
- **Remediation Plan:** Validate delivery fee server-side against configured delivery rates or require manager override for non-standard delivery fees.
- **Required Regression Tests:**
  - Negative fee with ordinary POS actor; overflow boundaries; assert no writes.
  - Positive/zero fee, tax, approved comp, COD and legitimate discounts.
- **Acceptance Criteria:**
  - Invalid signed fee and arithmetic overflow rejected before any order, stock, journal or treasury write.
  - Valid positive/zero fees and approved discount/comp routes retain policy behavior.

---

### FIN-017: Financial approval provenance can retain unvalidated shift presenter or discard verified driver variance approver
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Source References:** `src/Falah.Application/Shifts/CloseShiftRequest.cs` (lines [[10, 17]]), `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs` (lines [[444, 496]]), `src/Falah.Domain/Authorization/ApprovalRegistry.cs` (lines [[106, 111]]), `src/Falah.Domain/Shifts/Shift.cs` (lines [[103, 115]]), `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` (lines [[147, 173]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[113, 156]]), `tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs` (lines [[756, 777]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[113, 156]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[113, 156]]), `src/Falah.Domain/Authorization/ApprovalRegistry.cs` (lines [[106, 111]]), `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` (lines [[147, 173]]), `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` (lines [[147, 173]]), `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` (lines [[147, 173]]), `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` (lines [[147, 173]]), `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs` (lines [[444, 496]]), `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs` (lines [[444, 496]]), `src/Falah.Domain/Authorization/ApprovalRegistry.cs` (lines [[106, 111]]), `src/Falah.Domain/Authorization/ApprovalRegistry.cs` (lines [[106, 111]]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [[113, 156]])
- **Root Cause:** Financial attribution derives from presentation or earlier base gate rather than the actual verified relevant authorization result. Original shift evidence and related driver variance variant are preserved separately.
- **Impact:** False attribution to a person who did not issue approval; no money change or otherwise forbidden operation authorized by this defect alone.
- **Remediation Plan:** Carry verified gate result, actor, and actual approver identity separately to all financial rows/audit; preserve OR authorization policy and approval reason semantics.
- **Required Regression Tests:**
  - Direct-permission below-threshold unnecessary presenter; above-threshold approved variance; verify actor/approver on movement, close event, settlement and audit.
  - Wrong action/consumed approval and permission-only actor controls.
- **Acceptance Criteria:**
  - Persisted approver attribution is the verified approver for the actual gate; actor and approver are distinct fields where required.
  - Below-threshold optional presentation cannot be misattributed; above-threshold approval still enforces approval action, reason, and amount.

---

### FIN-018: Negative receipt quantity and unit cost can post stock reduction with supplier debt
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Application/Purchasing/PurchasingRequests.cs` (lines [[15, 29]]), `src/Falah.Domain/Inventory/PurchasingRules.cs` (lines [[77, 98]]), `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs` (lines [[20, 33]]), `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` (lines [[121, 175]]), `src/Falah.Server/Program.cs` (lines [[2182, 2187]]), `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` (lines [[121, 175]]), `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` (lines [[121, 175]]), `src/Falah.Domain/Inventory/PurchasingRules.cs` (lines [[77, 98]]), `src/Falah.Domain/Inventory/PurchasingRules.cs` (lines [[77, 98]])
- **Root Cause:** Aggregate payment validation substituted for per-line positive receipt quantity/nonnegative purchase cost validation.
- **Impact:** Incorrect stock reduction labelled receipt and positive supplier debt for invalid purchase; unauthorized privilege escalation not claimed.
- **Remediation Plan:** Validate direction-specific positive receipt quantities and nonnegative costs before posting. If returns are required, implement a separate explicit return/credit-note flow rather than encoding them as receipt negatives.
- **Required Regression Tests:**
  - Negative qty with positive unit cost; positive qty with negative cost; both-negative combination; assert no stock/debt mutation.
  - Positive receipt and distinct authorized return flow.
- **Acceptance Criteria:**
  - Receipt rejects unsupported negative quantity/unit-cost combinations before posting stock or supplier debt.
  - Valid positive receipts and any approved return/credit workflow remain coherent and atomic.

---

### FIN-019: Pending mixed delivery includes already collected payment leg in driver custody
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Delivery/DeliveryRequests.cs` (lines [[71, 76]]), `src/Falah.Client.CashierWinUI/Services/ApiClient.cs` (lines [[58, 65], [631, 632]]), `src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs` (lines [[66, 77], [134, 134], [174, 198], [208, 215], [221, 242], [297, 307], [319, 356]]), `src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml` (lines [[76, 79], [103, 107]]), `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml` (lines [[119, 140]]), `src/Falah.Domain/Delivery/DeliveryRules.cs` (lines [[9, 21], [32, 37], [83, 89]]), `src/Falah.Domain/Delivery/DeliveryStatus.cs` (lines [[13, 31]]), `src/Falah.Domain/Financial/FinancialRules.cs` (lines [[14, 25]]), `src/Falah.Domain/Financial/TreasuryMovement.cs` (lines [[32, 62]]), `src/Falah.Domain/Orders/CheckoutRules.cs` (lines [[44, 78], [88, 119]]), `src/Falah.Domain/Orders/ElectronicPayment.cs` (lines [[6, 10]]), `src/Falah.Domain/Orders/Order.cs` (lines [[100, 109]]), `src/Falah.Domain/Orders/OrderPaymentQueries.cs` (lines [[26, 55]]), `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs` (lines [[36, 137]]), `src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs` (lines [[14, 29]]), `src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs` (lines [[40, 47], [58, 92]]), `src/Falah.Infrastructure/Delivery/MarkDeliveryReadyHandler.cs` (lines [[26, 62]]), `src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs` (lines [[45, 144]]), `src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs` (lines [[35, 187]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[80, 90], [144, 176], [223, 229], [253, 275]]), `src/Falah.Infrastructure/Orders/OrderEnumMapping.cs` (lines [[58, 65]]), `src/Falah.Infrastructure/Orders/OrderMapper.cs` (lines [[118, 122]]), `src/Falah.Server/AuthenticationMiddleware.cs` (lines [[29, 84]]), `src/Falah.Server/Program.cs` (lines [[251, 251], [1224, 1250], [1286, 1314]])
- **Root Cause:** Delivery responsibility collapses per-leg payment collection into one whole-order IsPaid flag and Total-refund. Mixed pending order is partially collected, but modeled as wholly uncollected for driver custody.
- **Impact:** Overstated driver collection/custody by already posted immediate leg; possible duplicate customer collection or artificial driver variance/approval requirement. No claim actual customer payment, cash theft, SQL acceptance or persisted double revenue was reproduced.
- **Remediation Plan:** Use per-tender-leg collection status/amount when deriving driver custody. Do not reinterpret pending electronic receivable as driver cash; block dispatch or provide an explicit owner-approved conversion/recovery workflow.
- **Required Regression Tests:**
  - Mixed immediate cash/card plus pending electronic dispatch; assert custody does not include collected leg or silently convert pending claim.
  - All-immediate Multi and cash COD positive controls; explicit conversion policy only if approved.
- **Acceptance Criteria:**
  - Driver custody equals only currently collectible amounts under documented payment contract; pending electronic leg is never silently converted to cash owed.
  - Block or explicitly convert pending leg only under owner-approved flow, with atomic custody/movement/reporting.

---

### FIN-020: First stock receipt can race unit-change admission and reinterpret movement history
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Application/Inventory/InventoryItemRequests.cs` (lines [[14, 26]]), `src/Falah.Domain/Inventory/InventoryRules.cs` (lines [[40, 47]]), `src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs` (lines [[74, 100]]), `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs` (lines [[20, 34]]), `src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs` (lines [[24, 76]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[245, 255]]), `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` (lines [[60, 100], [165, 178], [206, 209]]), `src/Falah.Server/Program.cs` (lines [[909, 916]])
- **Root Cause:** Immutable-unit predicate is evaluated before write without coordinating the first stock-movement admission. InventoryItem mapping244 is primary key only, no concurrency token; movement FK protects existence, not unit interpretation.
- **Impact:** Historical bare milliunit movement is reinterpreted under UnitB, contrary explicit InventoryRules40-47 no-unit-snapshot invariant. Current stock display joins current Unit, and valuation/recipe conversions can become semantically inconsistent; no measured monetary loss or actual SQL schedule executed.
- **Remediation Plan:** Coordinate unit edits and first movement admission through a shared DB transaction lock/revision invariant used by both edit and every movement writer. Version only works if both paths participate.
- **Required Regression Tests:**
  - Barrier first movement admission vs unit change; ensure at most one incompatible operation commits.
  - Sequential post-first-movement unit change rejection and legitimate same-unit metadata update.
- **Acceptance Criteria:**
  - A unit change and first movement cannot both pass against same pre-movement state; one is rejected or serialized.
  - All readers continue to interpret historical movements using stable unit semantics.

---

### FIN-021: Accepted numeric Inclusive tax mode is persisted raw and consumed as Exclusive
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Source References:** `src/Falah.Application/Pricing/TaxRuleRequests.cs` (lines [[10, 16]]), `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs` (lines [[142, 151]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs` (lines [[122, 127]]), `src/Falah.Domain/Financial/TaxRule.cs` (lines [[5, 7], [14, 29]]), `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` (lines [[60, 63]]), `src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs` (lines [[34, 42], [86, 99], [116, 117]]), `src/Falah.Server/Program.cs` (lines [[1647, 1666]])
- **Root Cause:** Validation discards parsed TaxMode and persists noncanonical raw string; three amount consumers silently map every nonliteralInclusive value toExclusive, including accepted definedInclusive numeric0.
- **Impact:** Future selected enabled tax computation can add tax to a base intended inclusive under its accepted enum interpretation. Legitimate settings authority and noncanonical input required; no actual charge or SQL acceptance reproduced.
- **Remediation Plan:** Persist parsed TaxMode canonical enum value, reject undefined/unsupported forms, and make consumers use canonical parsed mode. Keep historical order tax snapshots frozen.
- **Required Regression Tests:**
  - Create/update tax mode canonical and numeric/invalid enum forms; compare checkout and estimate at nonzero rate.
  - Ensure old orders frozen.
- **Acceptance Criteria:**
  - Accepted tax mode is parsed and persisted canonically; every consumer computes same intended inclusive/exclusive treatment.
  - Historical frozen orders unchanged; new checkout and estimate agree under enabled nonzero rate.

---

### FIN-022: Accepted case-insensitive compensation type is stored raw but wage/payroll consumers require canonical names
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Source References:** `src/Falah.Application/Employees/EmployeeRequests.cs` (lines [[5, 20]]), `src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs` (lines [[38, 43], [146, 151], [199, 225]]), `src/Falah.Domain/Employees/Employee.cs` (lines [[5, 7]]), `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[83, 109]]), `src/Falah.Infrastructure/Employees/AttendanceHandlers.cs` (lines [[26, 34], [72, 98]]), `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs` (lines [[54, 65]]), `src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs` (lines [[26, 49], [79, 100]]), `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs` (lines [[81, 85]]), `src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs` (lines [[28, 39], [66, 70]]), `src/Falah.Server/Program.cs` (lines [[667, 681], [783, 786]])
- **Root Cause:** Shared compensation enum validation accepts multiple spellings but persistence retainsrawstring, while two financial consumers use exactname comparison instead of the parsedmeaning.
- **Impact:** Accepted case-variant compensation metadata can omit automatic daily wage credits or classify a monthly salary as credit-based; positive configured wage and noncanonical accepted input required. Shipped editor emits canonical names. No actual payout/loss reproduced.
- **Remediation Plan:** Persist parsed CompensationType canonical value and use parsed enum in attendance/payroll consumers; validate defined supported types at API boundary.
- **Required Regression Tests:**
  - Case variants for Daily/Monthly/Hourly on attendance and payroll; malformed/undefined strings rejected.
  - Canonical values and manual wage posting remain correct.
- **Acceptance Criteria:**
  - Every accepted compensation value is persisted in one canonical representation used consistently by attendance and payroll.
  - Daily/monthly/Hourly cases calculate only intended credits; resaving canonical data does not change financial meaning.

---

### FIN-023: Attendance checkout reconstructs check-in on checkout UTC date and can omit overtime
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/Common/BusinessClock.cs` (lines [[23, 29]]), `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[55, 60]]), `src/Falah.Infrastructure/Employees/AttendanceHandlers.cs` (lines [[26, 34], [49, 79], [110, 151]]), `src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs` (lines [[59, 80]]), `src/Falah.Infrastructure/Settings/SettingsQueryService.cs` (lines [[21, 30]])
- **Root Cause:** Loss of check-in UTC date, followed by reconstruction from checkout UTC date despite selecting attendance by Cairo date.
- **Impact:** Wrong hours and possible omission of otherwise eligible automatic overtime; no actual payment/loss or payroll record measured. Distinct from FIN012 correction-credit classification and FIN022 compensation enum interpretation.
- **Remediation Plan:** Retain full check-in date/time and compute elapsed interval from that persisted event under documented timezone and repeated-punch policy; do not reconstruct from checkout date.
- **Required Regression Tests:**
  - Same UTC and cross-Cairo-date check-in/out examples; work hours/overtime against approved time zone rule.
  - Repeated check-ins establish first/last punch rule.
- **Acceptance Criteria:**
  - Checkout time is compared against original check-in date/time policy, preserving timezone/day boundary; eligible overtime calculation matches approved rule.
  - Repeated check-in/checkout policy is explicit; same-UTC-day positive control remains correct.

---

### FIN-024: Manual daily-wage and overtime commands admit negative earning credits
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Employees/AttendanceRequests.cs` (lines [[29, 47]]), `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs` (lines [[319, 333]]), `src/Falah.Domain/Employees/EmployeeRules.cs` (lines [[30, 33]]), `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[95, 109]]), `src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs` (lines [[33, 60], [77, 108], [125, 156]]), `src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs` (lines [[28, 40], [57, 75]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[447, 452]]), `src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs` (lines [[12, 40], [75, 86]]), `src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs` (lines [[1173, 1242]]), `src/Falah.Server/Program.cs` (lines [[709, 718], [748, 778]])
- **Root Cause:** Signed custom wage/rate is used as earning Credit without command-specific sign validation; positive finitehours do not ensure positive earning.
- **Impact:** An otherwise authorized accepted command can construct negative DailyWage/Overtime credit, reducing employee factbalance and categorized earnings instead of explicit deduction. No actual SQL write/payroll loss verified.
- **Remediation Plan:** Reject negative command-specific wage/rate and overflow before posting; define zero policy. Do not globally ban signed Money or clamp negative earnings into zero.
- **Required Regression Tests:**
  - Negative custom daily rate/overtime rate and negative configured wage; overflow bounds; zero-policy control.
  - Same-key retry and manual explicit deduction controls.
- **Acceptance Criteria:**
  - Negative earning rate/amount and unsupported overflow reject before ledger write; accepted zero behavior follows explicit policy.
  - Manual deductions remain explicit and valid; valid wage/overtime/extra-shift replay remains idempotent.

---

### FIN-025: Manual ledger accepts undefined numeric types excluded from named financial categories
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Employees/EmployeeRequests.cs` (lines [[35, 45]]), `src/Falah.Domain/Employees/Employee.cs` (lines [[17, 22]]), `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[95, 109]]), `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs` (lines [[37, 56], [68, 90]]), `src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs` (lines [[28, 40], [57, 75]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[447, 452]]), `src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs` (lines [[12, 40], [75, 86]]), `src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs` (lines [[1173, 1242]]), `src/Falah.Server/Program.cs` (lines [[709, 718], [748, 778]])
- **Root Cause:** Enum.TryParse result is canonicalized without Enum.IsDefined/supported-category admission; undefined numeric strings remain unnamed numeric types.
- **Impact:** Constructed ledger facts can affect all-entry balance but fall outside explicit report/payroll namecategories. This is unsupported data-shape acceptance, not a claim every legitimate named credit belongs in payroll. No API/SQLacceptance test.
- **Remediation Plan:** Require Enum.IsDefined plus an explicit supported-type/credit-debit policy before manual ledger posting; preserve valid named types and classify legacy unsupported values explicitly.
- **Required Regression Tests:**
  - Defined numeric enum vs named control; unsupported numeric 999; explicit credit/debit category reporting.
  - Balance, payroll and report reconciliation after accepted entry.
- **Acceptance Criteria:**
  - Only defined and explicitly supported ledger enum members/types can be posted through manual API.
  - Unsupported numeric strings reject or are explicitly classified before balance/report/payroll effects; canonical named controls remain valid.

---

### FIN-026: Distinct employee credit requests can persist and display stale running-balance snapshots
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml` (lines [[179, 187]]), `src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs` (lines [[15, 49]]), `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs` (lines [[37, 56], [68, 90]]), `src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs` (lines [[75, 104]]), `src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs` (lines [[33, 60], [77, 108], [125, 156]]), `src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs` (lines [[28, 40], [57, 75]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[447, 452]])
- **Root Cause:** BalanceAfter snapshot computed from readsum before Save without cross-command employee serialization; own pendingDelta and same-key dedup do not cover different writers.
- **Impact:** Source schedule retains both positive credit facts but can display wrong statement runningbalance. Fact-derived GetBalance remains correct; protected payout locks are counterevidence. No actual SQLrace/UI observation/moneyoverdraft proved.
- **Remediation Plan:** Either serialize all ledger appends on one employee anchor with consistent lock order, or compute statement running balance from ordered immutable facts and retire stale BalanceAfter as authority. Define deterministic timestamp tie-breaker.
- **Required Regression Tests:**
  - Barrier concurrent distinct positive credit appends with no rows and existing balance; verify facts and statement snapshots.
  - Same-key retry, rollback/restart, same-command pending rows, payout lock ordering, equal timestamp tie.
- **Acceptance Criteria:**
  - Concurrent distinct credit appends preserve correct recomputed employee balance and statement running-balance order.
  - Same-key replay, pending same-command rows, and locked payout remain correct; tie ordering is deterministic.

---

### FIN-027: Frozen business-day financial snapshots omit the corresponding live-report permission gate
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs` (lines [[13, 16]]), `src/Falah.Application/Reports/ReportingDtos.cs` (lines [[32, 36]]), `src/Falah.Client.CashierWinUI/Services/ApiClient.cs` (lines [[301, 303], [317, 323]]), `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs` (lines [[19, 33], [120, 134]]), `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs` (lines [[84, 135]]), `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml` (lines [[101, 137]]), `src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs` (lines [[34, 38], [54, 64], [87, 120], [162, 165]]), `src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs` (lines [[42, 53]]), `src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs` (lines [[69, 98]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[137, 141], [400, 405]]), `src/Falah.Server/AuthenticationMiddleware.cs` (lines [[31, 88]]), `src/Falah.Server/Program.cs` (lines [[251, 251], [421, 428], [2013, 2018], [2282, 2289]])
- **Root Cause:** Frozen report payload endpoint/client path uses session-only admission instead of live reports_view/all policy.
- **Impact:** Authenticated actor lacking report permission can receive financial report JSON from snapshot endpoints under the inspected source contract. Different from employee report or identity-binding issues; parent owns dedup/security disposition.
- **Remediation Plan:** Apply reports_view to latest/history financial snapshot reads using verified authenticated actor; if broader archive access is intended, define a distinct explicit permission rather than reusing control-center permission.
- **Required Regression Tests:**
  - Disposable authenticated requests with and without reports_view for latest/history snapshot routes.
  - Client control-center-only access sees no financial figures; permitted report viewer still accesses.
- **Acceptance Criteria:**
  - Every frozen financial snapshot read uses authenticated session actor plus reports_view (or a separately owner-approved explicit archive permission).
  - Unauthorized session receives no financial snapshot payload; authorized reader can still access permitted history.

---

### FIN-028: Spent manager approval prevents acknowledgement replay of committed overtime and extra-shift credits
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Application/Employees/AttendanceRequests.cs` (lines [[37, 47]]), `src/Falah.Client.CashierWinUI/Services/ApiClient.cs` (lines [[398, 404]]), `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs` (lines [[34, 55]]), `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs` (lines [[367, 378]]), `src/Falah.Domain/Authorization/ApprovalRegistry.cs` (lines [[69, 99]]), `src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs` (lines [[125, 156]]), `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs` (lines [[33, 53]]), `src/Falah.Server/Program.cs` (lines [[759, 778]]), `src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs` (lines [[125, 156]]), `src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs` (lines [[125, 156]]), `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs` (lines [[367, 378]]), `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs` (lines [[367, 378]])
- **Root Cause:** RecordOvertime/RecordExtraShift validate one-use delegated approval before deterministic ledger receipt lookup. Successful first SaveChanges persists approval consumption; retry with same saved approval rejects before ledger dedupe/replay.
- **Impact:** Existing financial credit remains committed but client cannot confirm it with original request. Retained request remains pending after subsequent422, preventing normal completion of that submitted action. This is a recovery/acknowledgement failure, not proven duplicate payment or loss of financial facts.
- **Remediation Plan:** After validating actor/session and binding the durable request to target and payload, resolve an existing committed receipt before consuming approval again. Keep fresh-operation approval and atomic persistence.
- **Required Regression Tests:**
  - Manager-approved overtime/extra shift success then exact retry in fresh context after response loss; verify no second approval spend and same receipt.
  - Mismatched actor/employee/amount conflict; concurrent same-key SQL test only in approved disposable DB.
- **Acceptance Criteria:**
  - A committed approval-backed overtime/extra-shift request can be acknowledged on exact same actor/target/payload retry without consuming another approval.
  - Mismatched same key conflicts; fresh operation still requires valid authorization; no duplicate ledger credit.

---

### FIN-029: Owner dashboard stock projection ignores outbound movement direction
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/Inventory/InventoryRules.cs` (lines [[50, 52]]), `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs` (lines [[388, 430]]), `src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs` (lines [[32, 149]]), `src/Falah.Infrastructure/Orders/InventoryDeductionService.cs` (lines [[48, 96]]), `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` (lines [[165, 175]]), `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs` (lines [[297, 315]]), `tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs` (lines [[123, 150]])
- **Root Cause:** OwnerDashboardQueryService.LoadStockAsync groups stock movements and sums QuantityMilliUnits without applying Direction. Stock movement writers store positive magnitudes with Direction separately; canonical inventory projections subtract Out movements.
- **Impact:** Dashboard stock balances can be overstated after consumption, waste, packaging usage, or outbound count adjustments. Low-stock rows/counts can be hidden. Example: In 5,000 plus Out 4,000 yields dashboard 9,000 while correct balance is 1,000.
- **Remediation Plan:** Use shared direction-aware stock balance projection or apply Direction sign in dashboard query; keep inventory overview as parity oracle.
- **Required Regression Tests:**
  - Integration compare dashboard/inventory seeded In+Out and outbound-only; low-stock parity; positive in/out count adjustment.
- **Acceptance Criteria:**
  - For seeded In 5000 and Out 4000, dashboard and inventory overview both show 1000 and agree on low-stock status.
  - Outbound-only and count-adjustment movements preserve direction-aware parity.

---

### FIN-CAND-CASHCOUNT-001: Unvalidated denomination and unchecked arithmetic can corrupt finalized count values
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs` (lines [[47, 51]]), `src/Falah.Domain/Audit/AuditEvent.cs`, `src/Falah.Domain/Authorization/Actor.cs`, `src/Falah.Domain/Authorization/ApprovalRegistry.cs`, `src/Falah.Domain/Authorization/ManagerApproval.cs`, `src/Falah.Domain/Authorization/PermissionCatalog.cs`, `src/Falah.Domain/Authorization/PinHasher.cs`, `src/Falah.Domain/Authorization/SessionToken.cs`, `src/Falah.Domain/Authorization/UserManagementRules.cs`, `src/Falah.Domain/Backup/BackupRules.cs`, `src/Falah.Domain/BusinessDay/BusinessDayRules.cs` (lines [[125, 135]]), `src/Falah.Domain/Common/BusinessClock.cs`, `src/Falah.Domain/Common/DomainException.cs`, `src/Falah.Domain/Common/Money.cs`, `src/Falah.Domain/ControlCenter/ControlCenterRules.cs`, `src/Falah.Domain/Customers/Customer.cs`, `src/Falah.Domain/Delivery/DeliveryRecords.cs`, `src/Falah.Domain/Delivery/DeliveryRules.cs`, `src/Falah.Domain/Delivery/DeliveryStatus.cs`, `src/Falah.Domain/Employees/Attendance.cs`, `src/Falah.Domain/Employees/Employee.cs`, `src/Falah.Domain/Employees/EmployeeRules.cs`, `src/Falah.Domain/Financial/FinancialRules.cs`, `src/Falah.Domain/Financial/PaymentMethod.cs`, `src/Falah.Domain/Financial/TaxRule.cs`, `src/Falah.Domain/Financial/TreasuryMovement.cs`, `src/Falah.Domain/Idempotency/FinancialJournal.cs`, `src/Falah.Domain/Inventory/CostingRules.cs`, `src/Falah.Domain/Inventory/InventoryRecords.cs`, `src/Falah.Domain/Inventory/InventoryRules.cs`, `src/Falah.Domain/Inventory/PrepRecipeLine.cs`, `src/Falah.Domain/Inventory/PurchasingRules.cs`, `src/Falah.Domain/Menu/ProductImageRules.cs`, `src/Falah.Domain/Orders/CheckoutRules.cs`, `src/Falah.Domain/Orders/ElectronicPayment.cs`, `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Domain/Orders/OrderEnums.cs`, `src/Falah.Domain/Orders/OrderItem.cs`, `src/Falah.Domain/Orders/OrderPaymentQueries.cs`, `src/Falah.Domain/Orders/OrderRefund.cs`, `src/Falah.Domain/Orders/OrderValueBreakdown.cs`, `src/Falah.Domain/Orders/RefundRules.cs`, `src/Falah.Domain/Payroll/PayrollRules.cs`, `src/Falah.Domain/Pricing/ItemPricingRules.cs`, `src/Falah.Domain/Pricing/PriceRule.cs`, `src/Falah.Domain/Pricing/PricingRules.cs`, `src/Falah.Domain/Printing/EscPosTicketRenderer.cs`, `src/Falah.Domain/Printing/PrintRecords.cs`, `src/Falah.Domain/Printing/PrintRules.cs`, `src/Falah.Domain/Printing/TicketLayout.cs`, `src/Falah.Domain/Purchasing/PayablesAging.cs`, `src/Falah.Domain/Reports/ProfitabilityRules.cs`, `src/Falah.Domain/Reports/SalesRules.cs`, `src/Falah.Domain/Reports/TrendRules.cs`, `src/Falah.Domain/Shifts/Shift.cs`, `src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs` (lines [[29, 64]]), `src/Falah.Server/Program.cs` (lines [[488, 500]]), `src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs` (lines [[29, 64]])
- **Root Cause:** Arithmetic and domain validation are not bounded at the finalization boundary; persisted audit data records the derived result without enforcing the supported denomination set or representable totals.
- **Impact:** Could finalize and audit a mathematically incorrect cash total/difference. Existing evidence does not show direct treasury posting, shift settlement, or close-blocker override; do not characterize as cash loss.
- **Remediation Plan:** Owner must first decide whether ExpectedPiastres is a client-supplied comparison target or must be derived from a server-authoritative snapshot. Then whitelist supported positive denominations and bound each quantity/aggregate; use checked multiply, sum, and difference and convert overflow/invalid rows to explicit domain rejection. Preserve agreed negative-quantity compatibility semantics or reject only by explicit policy.
- **Required Regression Tests:**
  - Domain tests for every allowed denomination, unsupported/zero/negative denomination, maximum quantity, multiplication overflow, aggregate overflow, and difference overflow.
  - Authorized handler test proves invalid input creates neither count nor audit record; valid input persists exact values atomically.
  - Endpoint test verifies invalid input maps to a deliberate client error.
  - Verify whether finalized cash-count data participates in close reconciliation and that any such use is server-validated.
  - Build/run arithmetic behavior under the actual net8 deployment configuration.
- **Acceptance Criteria:**
  - All accepted values fall within explicit denomination and monetary bounds; no wraparound reaches persisted count/audit.
  - Rejected values produce no partial count/audit writes.
  - ExpectedPiastres authority and negative quantity semantics are documented and covered.
  - Any reconciliation consumer uses the validated result or independently recomputes the expected value.

---

### FIN-CAND-CLOSE-SNAPSHOT-REVISION-001: Close snapshot revision and authoritative-row admission lack an explicit shared invariant
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Source References:** `src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs` (lines [[34, 38], [54, 64], [87, 120], [162, 165]]), `src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs` (lines [[42, 53]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[137, 141], [400, 405]])
- **Root Cause:** Read/status admission outside transaction plus Count+1/demotion lacks explicit perday uniqueness/state-token enforcement in inspected source.
- **Impact:** Source lacks explicit common day admission lock/state token and schema uniqueness for archival invariants; duplicate-current/same-revision outcome remains a qualified provider-dependent candidate, not reproduced corruption.
- **Remediation Plan:** Do not implement a concurrency fix based on assumed SQL ordering. First test provider schedule; if admission gap manifests, use common day lock/state predicate plus explicit snapshot uniqueness/authority invariant.
- **Required Regression Tests:**
  - Barrier two distinct close IDs around count/demotion in disposable SQL Server; capture EF DML and locks.
  - Verify authoritative row, revisions and day pointer across both serialization outcomes/history.
- **Acceptance Criteria:**
  - Disposable SQL Server controlled schedule establishes actual DML/lock outcome before code fix is chosen.
  - After any accepted close ordering there is exactly one authoritative snapshot, coherent day pointer, and valid monotone unique revision; losing close leaves no partial mutation.

---

### FIN-CAND-CLOSEDAY-ADMISSION-001: Previously admitted treasury deposit can post after authoritative business-day close
- **Severity:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs` (lines [[14, 29]]), `src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs` (lines [[25, 57]]), `src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs` (lines [[64, 121]]), `src/Falah.Infrastructure/Reports/ReportingQueryService.cs` (lines [[164, 208]]), `src/Falah.Infrastructure/Treasury/DepositHandler.cs` (lines [[56, 85]])
- **Root Cause:** Financial write admission is validated outside its monetary transaction and no shared day-state lock, revision predicate, or revalidation is visible in the selected path. Close snapshot and blocker reads therefore do not source-demonstrably drain operations that already passed the gate.
- **Impact:** Predicted post-snapshot movement/journal attached to a now-closed day, potentially diverging from its authoritative close-time snapshot if post-close activity is forbidden. This is not an observed SQL interleaving or loss.
- **Remediation Plan:** Confirm whether financial writes are forbidden after close. If yes, coordinate day admission in the database: acquire a transaction-held lock/revision on the business-day row and revalidate Open within each financial write transaction; close takes the same lock before blocker evaluation/snapshot and waits for already-admitted writes. Define cross-process semantics, retry policy, and consistent lock order. If late postings are allowed, define how frozen snapshot reconciliation and subsequent deltas are represented instead of silently treating snapshot as current ledger.
- **Required Regression Tests:**
  - SQL-backed barrier test pauses deposit after initial lookup; close wins; resumed deposit must reject with zero journal/movement.
  - Reverse schedule: admitted deposit commits first and close waits/includes it in snapshot.
  - Cover other financial commands using the same admission contract plus reopen/new-day and idempotent replay.
  - Run controlled multi-process concurrency test under the supported provider/isolation level; verify no stale admission slips.
- **Acceptance Criteria:**
  - A documented policy distinguishes immutable close-time snapshot from permitted subsequent postings.
  - Under forbid-after-close policy, every monetary write serializes/revalidates against close inside the database transaction; close snapshot covers all writes admitted before close.
  - Concurrency tests prove both orderings and absence of partial or post-close financial effects; lock order avoids deadlocks.

---

### FIN-CAND-CORRECTION-BASIS-001: Attendance corrections revalue old effects at current rates instead of posted monetary basis
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[29, 32], [55, 60]]), `src/Falah.Infrastructure/Employees/AttendanceHandlers.cs` (lines [[138, 147]]), `src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs` (lines [[15, 18], [39, 51], [82, 117]]), `src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs` (lines [[31, 49]]), `src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs` (lines [[95, 100]]), `src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs` (lines [[32, 53]])
- **Root Cause:** Correction reads current employee DailyWage/current Settings then recomputes BOTH old and new overtime/penalty. It never reads frozen actual original amount/rate or cumulative prior correction effect for delta. Row+ledger serialization and immutable retry audit are real controls, but do not restore old monetary basis.
- **Impact:** Potential over/under reconciliation relative to actual posted effect; authoritative correction valuation policy unresolved. No actual employee loss, runtime record, payout or SQL acceptance verified.
- **Remediation Plan:** After owner decision, calculate correction delta against actual posted original facts and cumulative linked adjustments, append only; preserve idempotency, locks, and freeze rules. Do not mint historical credits automatically.
- **Required Regression Tests:**
  - Original fixed-rate overtime then settings changed then correction; assert owner-approved reversal basis.
  - Change multiplier/grace/workday, penalty to zero, sequential corrections/return to original hours, replay and frozen/paid period controls.
- **Acceptance Criteria:**
  - Owner-approved correction basis is documented; adjustments reconcile original posted facts and prior corrections without accidental over/under reversal.
  - Same-key retry and interleaved sequential corrections remain atomic, auditable, and consistent with frozen/paid period policy.

---

### FIN-CAND-EMPLOYEE-PAYROLL-OVERLAP-001: Standalone employee settlements remain payable through frozen period payroll
- **Severity:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs`, `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`, `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[83, 110]]), `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs` (lines [[42, 63], [65, 88]]), `src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs` (lines [[16, 30], [74, 107], [113, 140]]), `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs` (lines [[84, 108], [136, 163]]), `src/Falah.Server/Program.cs`, `tests/Falah.Server.IntegrationTests/PayEmployeeVerticalSliceTests.cs`, `tests/Falah.Server.IntegrationTests/PayrollVerticalSliceTests.cs`, `src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs` (lines [[16, 30], [74, 107], [113, 140]]), `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs` (lines [[42, 63], [65, 88]]), `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs` (lines [[84, 108], [136, 163]])
- **Root Cause:** PayrollRules computes payroll from earning categories and named deductions but ignores standalone Settlement debits. PayEmployee reduces current balance, while CreatePayroll freezes period net and PayPayroll pays that frozen amount without revalidating outstanding employee balance.
- **Impact:** If standalone Settlement is a prepayment of earnings already included in the same frozen payroll, paying both paths can disburse the same obligation twice; source flow shows this mechanism but does not establish the accounting policy or an actual duplicate payout.
- **Remediation Plan:** Owner decision first. If settlements prepay same-period earnings, derive remaining liability from an explicit period/earning obligation and apply settlements both before payroll calculation and after run freeze; if the two paths are mutually exclusive, enforce atomic exclusion on both. Preserve frozen-run, replay, audit, source-balance, and historical-record behavior.
- **Required Regression Tests:**
  - Create earnings, settle same obligation standalone, then create/approve/pay payroll; total disbursement must satisfy owner-approved invariant.
  - Create/approve payroll, apply standalone settlement, then pay frozen payroll; same invariant.
  - Unrelated-period settlement must not be netted.
  - Repeat and concurrent requests with distinct IDs; validate exactly-once employee ledger and treasury effects in approved disposable SQL Server integration DB.
- **Acceptance Criteria:**
  - Written owner policy defines relationship between standalone Settlement and period payroll obligation.
  - Tests implement that policy across both sequence orders, period boundaries, retries and concurrent requests.
  - No duplicate obligation disbursement if policy treats Settlement as prepayment; otherwise distinct obligation is explicitly represented and reported.

---

### FIN-CAND-PACKAGING-COVERAGE-001: Unknown once-per-order packaging cost does not invalidate aggregate profit completeness
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Source References:** `src/Falah.Domain/Audit/AuditEvent.cs`, `src/Falah.Domain/Authorization/Actor.cs`, `src/Falah.Domain/Authorization/ApprovalRegistry.cs`, `src/Falah.Domain/Authorization/ManagerApproval.cs`, `src/Falah.Domain/Authorization/PermissionCatalog.cs`, `src/Falah.Domain/Authorization/PinHasher.cs`, `src/Falah.Domain/Authorization/SessionToken.cs`, `src/Falah.Domain/Authorization/UserManagementRules.cs`, `src/Falah.Domain/Backup/BackupRules.cs`, `src/Falah.Domain/BusinessDay/BusinessDayRules.cs`, `src/Falah.Domain/Common/BusinessClock.cs`, `src/Falah.Domain/Common/DomainException.cs`, `src/Falah.Domain/Common/Money.cs`, `src/Falah.Domain/ControlCenter/ControlCenterRules.cs`, `src/Falah.Domain/Customers/Customer.cs`, `src/Falah.Domain/Delivery/DeliveryRecords.cs`, `src/Falah.Domain/Delivery/DeliveryRules.cs`, `src/Falah.Domain/Delivery/DeliveryStatus.cs`, `src/Falah.Domain/Employees/Attendance.cs`, `src/Falah.Domain/Employees/Employee.cs`, `src/Falah.Domain/Employees/EmployeeRules.cs`, `src/Falah.Domain/Financial/FinancialRules.cs`, `src/Falah.Domain/Financial/PaymentMethod.cs`, `src/Falah.Domain/Financial/TaxRule.cs`, `src/Falah.Domain/Financial/TreasuryMovement.cs`, `src/Falah.Domain/Idempotency/FinancialJournal.cs`, `src/Falah.Domain/Inventory/CostingRules.cs` (lines [[181, 190]]), `src/Falah.Domain/Inventory/InventoryRecords.cs`, `src/Falah.Domain/Inventory/InventoryRules.cs`, `src/Falah.Domain/Inventory/PrepRecipeLine.cs`, `src/Falah.Domain/Inventory/PurchasingRules.cs`, `src/Falah.Domain/Menu/ProductImageRules.cs`, `src/Falah.Domain/Orders/CheckoutRules.cs`, `src/Falah.Domain/Orders/ElectronicPayment.cs`, `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Domain/Orders/OrderEnums.cs`, `src/Falah.Domain/Orders/OrderItem.cs`, `src/Falah.Domain/Orders/OrderPaymentQueries.cs`, `src/Falah.Domain/Orders/OrderRefund.cs`, `src/Falah.Domain/Orders/OrderValueBreakdown.cs`, `src/Falah.Domain/Orders/RefundRules.cs`, `src/Falah.Domain/Payroll/PayrollRules.cs`, `src/Falah.Domain/Pricing/ItemPricingRules.cs`, `src/Falah.Domain/Pricing/PriceRule.cs`, `src/Falah.Domain/Pricing/PricingRules.cs`, `src/Falah.Domain/Printing/EscPosTicketRenderer.cs`, `src/Falah.Domain/Printing/PrintRecords.cs`, `src/Falah.Domain/Printing/PrintRules.cs`, `src/Falah.Domain/Printing/TicketLayout.cs`, `src/Falah.Domain/Purchasing/PayablesAging.cs`, `src/Falah.Domain/Reports/ProfitabilityRules.cs` (lines [[35, 46]]), `src/Falah.Domain/Reports/SalesRules.cs`, `src/Falah.Domain/Reports/TrendRules.cs`, `src/Falah.Domain/Shifts/Shift.cs`, `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs` (lines [[205, 223]]), `src/Falah.Infrastructure/Reports/ReportingQueryService.cs` (lines [[399, 423]])
- **Root Cause:** Completeness boolean uses dish-line counters while food-cost total also includes separately accumulated order-level packaging; packaging unknown state is not passed to profitability completeness calculation.
- **Impact:** Profitability result can be numerically computed and marked complete although packaging costs are excluded; reports can overstate gross/operating profit. This concerns report completeness, not ledger posting or realized cash loss.
- **Remediation Plan:** Carry an explicit aggregate cost-coverage/completeness signal across dish and order-packaging components into profitability calculation and DTO, preserving distinct dish counts and per-order packaging cardinality. Keep missing names. Do not convert packaging unknowns into fabricated sold dish-line counts.
- **Required Regression Tests:**
  - Known dish plus unknown active packaging returns IsComplete=false and does not present the aggregate profit as fully costed.
  - Known packaging adds exact configured cost once per order and retains true completeness.
  - No active packaging rule remains known optional zero.
  - Multiple dishes in one order versus multiple orders preserves packaging count/cost cardinality.
  - DTO/API and UI communicate incomplete state consistently alongside missing item names.
- **Acceptance Criteria:**
  - IsComplete is false whenever any sold dish or applicable packaging component is unknown.
  - The amount excludes unknown cost but is visibly and machine-readably partial; counts retain their documented units.
  - Known/optional-zero cases preserve existing results and packaging charges are applied exactly once per applicable order.

---

### FIN-CAND-PAYROLL-001: Concurrent pay/cancel can overwrite payroll terminal status
- **Severity:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/Payroll/PayrollRules.cs` (lines [[13, 27]]), `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs` (lines [[62, 87], [110, 162]]), `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs` (lines [[24, 60]]), `src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs` (lines [[5, 20]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[460, 477]])
- **Root Cause:** Pay and cancel authorize from independent Approved snapshots; no shared run-level transaction lock, compare-and-swap state predicate, or concurrency token is visible. Payment protects source-account balance, not PayrollRun state.
- **Impact:** Predicted inconsistent final state: a payroll can be financially paid while status becomes Cancelled, or payment can proceed after cancellation. Downstream reporting/recreation decisions become unreliable. Do not claim duplicate payment reproduced; same-run payment idempotency is counterevidence to duplicate pay on replay.
- **Remediation Plan:** Use one database-coordinated PayrollRun state transition primitive for pay/cancel (transaction-held run-row lock or optimistic concurrency token/conditional Approved-to-claimed update). Payment must acquire/claim Approved within its transaction before movement/ledger posting; cancellation must perform mutually exclusive conditional transition. A losing operation must produce no movement, journal, settlement, paid-line, or status side effects. Preserve idempotency, filtered live-period index, and consistent treasury/run lock ordering.
- **Required Regression Tests:**
  - Barrier: pay reads Approved, cancel commits first; payment claim must fail and leave zero financial rows/line effects.
  - Barrier: cancellation reads Approved, payment claims/commits first; cancellation must fail and Paid status/effects remain consistent.
  - Repeat same idempotency key and concurrent duplicate pay; only one movement and one settlement per line.
  - Exercise cancellation and replacement-run creation against filtered live-period unique index.
  - Assert state and all side effects atomically under production provider and supported isolation.
- **Acceptance Criteria:**
  - At most one of pay/cancel succeeds for a given Approved version.
  - Paid state is committed atomically with movement, journal, line settlements and audit; Cancelled state cannot coexist with those effects from the losing race.
  - Lost race yields explicit conflict/reload response and no partial writes.
  - Existing retry idempotency and one-live-run policy remain intact.

---

### FIN-CAND-PREPGRAPH-001: Concurrent opposite prep edges can evade pretransaction cycle admission
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/Inventory/CostingRules.cs` (lines [[97, 127]]), `src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs` (lines [[84, 109]]), `src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs` (lines [[84, 109]]), `src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs` (lines [[84, 109]]), `src/Falah.Domain/Inventory/CostingRules.cs` (lines [[97, 127]]), `src/Falah.Domain/Inventory/CostingRules.cs` (lines [[97, 127]])
- **Root Cause:** Cycle validation is a shared-graph predicate read before the transaction, while each transaction writes only one recipe; no database-level serialization/revalidation of the graph-wide invariant is visible.
- **Impact:** Predicted active prep-recipe cycle can be persisted despite save-time cycle rejection; costing becomes Unknown/incomplete for affected paths and report reliability falls. No direct treasury, inventory movement, or ledger posting from recipe save is evidenced.
- **Remediation Plan:** Coordinate graph mutations with a transaction-scoped shared graph revision/lock, then revalidate against the current graph before persisting. Alternatively use serializable predicate validation with bounded retry and consistent cross-process semantics. Retain defensive Unknown cycle guard for existing/legacy data.
- **Required Regression Tests:**
  - Barrier-controlled opposite-edge concurrent save: at most one can commit; loser receives stable cycle/conflict error.
  - Two unrelated acyclic concurrent recipe saves both succeed if product policy permits.
  - Sequential cycle rejection remains.
  - Legacy cyclic fixture terminates and returns Unknown/incomplete, with report not presenting complete cost.
  - Exercise recipe replacement and output identity changes under same graph coordination.
- **Acceptance Criteria:**
  - The active prep graph remains acyclic after every successful save, including concurrent saves through multiple server processes.
  - A race loser causes no partial recipe/line replacement.
  - Unrelated valid edits retain expected concurrency; bounded retry/conflict behavior is documented.
  - Costing defense remains finite and conservative for pre-existing invalid data.

---

### FIN-CAND-REFUND-REPLAY-001: Overlapping full-refund replay can reject after same-key sibling committed
- **Severity:** `LOW` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs` (lines [[87, 121]]), `src/Falah.Domain/Orders/RefundRules.cs` (lines [[34, 42]]), `src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs` (lines [[75, 84]]), `src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs` (lines [[59, 71]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[154, 165]]), `tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs` (lines [[131, 150]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[154, 165]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[154, 165]]), `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` (lines [[154, 165]]), `src/Falah.Domain/Orders/RefundRules.cs` (lines [[34, 42]]), `src/Falah.Domain/Orders/RefundRules.cs` (lines [[34, 42]]), `src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs` (lines [[75, 84]]), `src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs` (lines [[75, 84]]), `src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs` (lines [[59, 71]]), `src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs` (lines [[59, 71]]), `tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs` (lines [[131, 150]]), `tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs` (lines [[131, 150]]), `tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs` (lines [[131, 150]])
- **Root Cause:** Durable refund-key lookup happens before parent order serialization, with no repeat lookup after sibling full refund changes refundable state; DomainException cap rejection is not handled by saved-receipt recovery catch.
- **Impact:** Conditional misleading failure for one concurrent duplicate acknowledgement; later sequential same-key retry recovers. No new financial movement, direct moneyloss, overrefund or permanentclientloop established.
- **Remediation Plan:** Recheck same-key durable refund receipt after acquiring order lock and before cap rejection; preserve actor/order/payload binding, cap and funds rules. Do not broadly treat DomainException as replay.
- **Required Regression Tests:**
  - Overlapping full-amount same-key refund: second request begins lookup before first commit, then after lock sees same committed receipt.
  - Existing partial same-key concurrency control; distinct-key over-cap, mismatch binding and failed-first sibling.
- **Acceptance Criteria:**
  - After order lock, exact same-key sibling commit is returned as replay without second refund or misleading cap error.
  - Distinct-key refund cap and mismatched order/key binding remain enforced; failed sibling does not fabricate success.

---

### FIN-CAND-REPORT-RANGE-001: Reversed report endpoints normalize sales but independently filter cost with contradictory date bounds
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Domain/Audit/AuditEvent.cs`, `src/Falah.Domain/Authorization/Actor.cs`, `src/Falah.Domain/Authorization/ApprovalRegistry.cs`, `src/Falah.Domain/Authorization/ManagerApproval.cs`, `src/Falah.Domain/Authorization/PermissionCatalog.cs`, `src/Falah.Domain/Authorization/PinHasher.cs`, `src/Falah.Domain/Authorization/SessionToken.cs`, `src/Falah.Domain/Authorization/UserManagementRules.cs`, `src/Falah.Domain/Backup/BackupRules.cs`, `src/Falah.Domain/BusinessDay/BusinessDayRules.cs`, `src/Falah.Domain/Common/BusinessClock.cs` (lines [[47, 53], [65, 82]]), `src/Falah.Domain/Common/DomainException.cs`, `src/Falah.Domain/Common/Money.cs`, `src/Falah.Domain/ControlCenter/ControlCenterRules.cs`, `src/Falah.Domain/Customers/Customer.cs`, `src/Falah.Domain/Delivery/DeliveryRecords.cs`, `src/Falah.Domain/Delivery/DeliveryRules.cs`, `src/Falah.Domain/Delivery/DeliveryStatus.cs`, `src/Falah.Domain/Employees/Attendance.cs`, `src/Falah.Domain/Employees/Employee.cs`, `src/Falah.Domain/Employees/EmployeeRules.cs`, `src/Falah.Domain/Financial/FinancialRules.cs`, `src/Falah.Domain/Financial/PaymentMethod.cs`, `src/Falah.Domain/Financial/TaxRule.cs`, `src/Falah.Domain/Financial/TreasuryMovement.cs`, `src/Falah.Domain/Idempotency/FinancialJournal.cs`, `src/Falah.Domain/Inventory/CostingRules.cs`, `src/Falah.Domain/Inventory/InventoryRecords.cs`, `src/Falah.Domain/Inventory/InventoryRules.cs`, `src/Falah.Domain/Inventory/PrepRecipeLine.cs`, `src/Falah.Domain/Inventory/PurchasingRules.cs`, `src/Falah.Domain/Menu/ProductImageRules.cs`, `src/Falah.Domain/Orders/CheckoutRules.cs`, `src/Falah.Domain/Orders/ElectronicPayment.cs`, `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Domain/Orders/OrderEnums.cs`, `src/Falah.Domain/Orders/OrderItem.cs`, `src/Falah.Domain/Orders/OrderPaymentQueries.cs`, `src/Falah.Domain/Orders/OrderRefund.cs`, `src/Falah.Domain/Orders/OrderValueBreakdown.cs`, `src/Falah.Domain/Orders/RefundRules.cs`, `src/Falah.Domain/Payroll/PayrollRules.cs`, `src/Falah.Domain/Pricing/ItemPricingRules.cs`, `src/Falah.Domain/Pricing/PriceRule.cs`, `src/Falah.Domain/Pricing/PricingRules.cs`, `src/Falah.Domain/Printing/EscPosTicketRenderer.cs`, `src/Falah.Domain/Printing/PrintRecords.cs`, `src/Falah.Domain/Printing/PrintRules.cs`, `src/Falah.Domain/Printing/TicketLayout.cs`, `src/Falah.Domain/Purchasing/PayablesAging.cs`, `src/Falah.Domain/Reports/ProfitabilityRules.cs` (lines [[35, 63]]), `src/Falah.Domain/Reports/SalesRules.cs`, `src/Falah.Domain/Reports/TrendRules.cs`, `src/Falah.Domain/Shifts/Shift.cs`, `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs` (lines [[38, 70], [205, 225]]), `src/Falah.Infrastructure/Reports/ReportingQueryService.cs` (lines [[35, 48], [241, 246], [349, 363], [422, 424]]), `src/Falah.Server/Program.cs` (lines [[1976, 1983]])
- **Root Cause:** Range ordering handled inconsistently between normalized sales UTC interval and raw cost/date series consumers.
- **Impact:** Misleading cost/profit/report period, not a proven money posting or authorization bypass. Static financial report correctness candidate.
- **Remediation Plan:** Normalize or reject reversed date endpoints once at shared request boundary and pass one interval to sales, cost, expenses, and date-series calculations.
- **Required Regression Tests:**
  - Pure forward/same/reversed date range and Days/DayCount consistency.
  - Authorized seeded report nonzero sales/known recipe cost reversed endpoints; direct food-cost/expense endpoints use same contract.
- **Acceptance Criteria:**
  - Reversed endpoints either normalize once for every report series or are rejected consistently; sales, food cost, expenses, days and day count use same interval.
  - Forward, same-day, reversed, and boundary-date reports agree across DTO and direct endpoints.

---

### FIN-CAND-BUSINESS-DAY-OPEN-RACE-001: Distinct concurrent business-day opens can pass pre-insert active-state validation
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs` (lines [[27, 28], [34, 55]]), `src/Falah.Domain/BusinessDay/BusinessDayRules.cs` (lines [[22, 43]]), `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` (lines [[116, 127], [137, 141]]), `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs` (lines [[17, 27], [33, 55], [69, 96]]), `src/Falah.Infrastructure/Persistence/Migrations/20260903154545_AddBusinessDay.cs` (lines [[14, 38]]), `src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs`
- **Root Cause:** Active-day admission is a read-check-insert invariant. OpenBusinessDayHandler reads current active rows and invokes BusinessDayRules.Open before a separate insert; there is no shared transaction/conditional state claim, while the schema has uniqueness only by request ID.
- **Impact:** More than one active business day may be persisted, making day attribution, financial close blockers/snapshots, and active-day lookups ambiguous. No actual duplicate day or accounting loss has been reproduced.
- **Remediation Plan:** Confirm the invariant scope. If one active BusinessDay is intended, enforce it at the same persistence boundary as the single-open-shift invariant (provider-appropriate active-row uniqueness or a serialized admission row/lock covering Open and Reopen), and make losing admission return a stable conflict without partial side effects. Preserve same-request idempotency. Do not rely solely on a pre-insert read.
- **Required Regression Tests:**
  - Barrier-controlled concurrent opens with two distinct RequestIds: at most one active day commits; loser receives a conflict and no second row is created.
  - Same RequestId replay returns the original day without a second row.
  - Concurrent reopen of one day and open of another cannot create two active days if global invariant is confirmed.
  - Sequential open while Open, Closing, or Reopened rejects; after valid close, a new day opens.
  - Run against supported SQL Server provider and two independent DbContext/process instances; assert database-level invariant.
- **Acceptance Criteria:**
  - A documented owner policy establishes the active-day scope (global or per defined partition).
  - If global single-active-day remains intended, every Open/Reopen transition is serialized or constrained by database-enforced uniqueness so concurrent instances cannot persist more than one active day.
  - A losing concurrency attempt leaves no partial state and the existing same-request replay contract remains intact.

---

### FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001: SafeDrop may attribute a later drawer movement to a closed shift and change its reprinted breakdown
- **Severity:** `LOW` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs` (lines [45, 46, 58, 59, 74, 83]), `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs` (lines [59, 68]), `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs` (lines [181, 204]), `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` (lines [79, 84, 95, 98]), `src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs` (lines [56, 59]), `tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs` (lines [163, 187]), `tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs` (lines [90, 98])
- **Root Cause:** SafeDrop validates only that the request ShiftId exists; it does not require the referenced shift to remain Open. It posts with the current open BusinessDayId plus that request ShiftId. ShiftReconciliation later selects Posted safe_drop rows by ShiftId. Reprinting a close slip recomputes detail from current rows but restores the stored expected, actual and difference headline.
- **Impact:** The closed shift close-slip component breakdown can reflect a later SafeDrop while its headline remains the original saved close values. This can reduce reconciliation clarity or attribute later custody to an earlier shift if that is not the intended policy.
- **Remediation Plan:** ['If prohibit: enforce shift-open state in a concurrency-safe transaction boundary shared with shift close.', 'If allow: represent later movement as an explicit post-close adjustment/revision and distinguish it from the immutable close snapshot in reprints.']
- **Required Regression Tests:**
  - Open BusinessDay and closed Shift: assert the owner-selected accepted/rejected outcome and posted movement ShiftId/BusinessDayId.
  - After close and any allowed later SafeDrop, reprint the slip and assert documented headline/component version semantics.
  - Race SafeDrop against CloseShift on the same shift using a disposable production-SQL-Server-provider test; assert a policy-valid serialized outcome.
  - If later posting is allowed, verify the audit/reprint distinguishes a later adjustment from cash present at original close.
- **Acceptance Criteria:**
  - Owner decision is documented.
  - Stored close headline retains historical meaning.
  - Reprints either preserve close-time breakdown or clearly mark later adjustments.
  - Concurrent close/post outcomes obey the chosen policy.

---

### FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001: Legacy WPF refund submit does not preserve the same idempotency key after an ambiguous outcome
- **Severity:** `LOW` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`, `src/Falah.Client.Cashier/Services/ApiClient.cs`, `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`, `src/Falah.Client.CashierWinUI/Services/ApiClient.cs`, `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`, `src/Falah.Infrastructure/Persistence/FalahDbContext.cs`, `tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs`
- **Root Cause:** WPF SubmitRefundAsync creates a fresh RefundRequestId per invocation and WPF ApiClient does not expose transport ambiguity as OutcomeUnknown or retain the request for replay. WinUI keeps _pendingRefund and retries the same request ID during the current ViewModel lifetime.
- **Impact:** If WPF is supported/deployed, response loss after a successful partial refund can leave the operator without explicit unknown-outcome recovery; a subsequent submit generates a new key and may create a separate additional partial refund. Impact depends on deployment, transport outcome and operator action.
- **Remediation Plan:** ['If WPF is supported, route refund commands through explicit unknown-outcome state and retain/reconcile the same request ID before allowing a new submit. If WPF is retired, record that operational fact. Decide whether WinUI unresolved refund intent must survive restart; if so, persist a safe pending key or require authoritative refund lookup before another command. Preserve server amount and account guards.']
- **Required Regression Tests:**
  - Inject response loss after server commit in WPF; retry must reuse the key or require server reconciliation before a new command.
  - WinUI timeout/response loss then retry in same ViewModel must use the same ID and return one receipt.
  - Restart after unresolved WinUI outcome must follow owner-selected recovery policy.
  - A deliberate new partial refund with a distinct ID remains allowed when the order/account permit it.
  - Retain same-key replay and distinct-key cumulative cap integration tests.
- **Acceptance Criteria:**
  - Owner documents whether WPF is supported or retired.
  - Supported clients cannot encourage a fresh financial command before prior outcome is resolved.
  - Same logical operation retains the idempotency key across retry; intentional new partial refunds remain distinct and auditable.

---

### FIN-CAND-CANCELLED-LOOKUP-PAYMENT-LABEL-001: Call Center labels a cancelled unpaid electronic order as collectible on delivery
- **Severity:** `LOW` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs` (lines [[267, 279], [311, 334]]), `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml` (lines [[143, 171]]), `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs` (lines [[11, 22]]), `src/Falah.Domain/Orders/Order.cs` (lines [[200, 214]]), `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs` (lines [[31, 73]]), `src/Falah.Server/Program.cs` (lines [[1506, 1510]]), `src/Falah.Infrastructure/Orders/OrderMapper.cs` (lines [[24, 39]]), `src/Falah.Application/Orders/OrderDetailsDto.cs` (lines [[10, 23]])
- **Root Cause:** The payment display infers collect-on-delivery solely from IsPaid=false after special-casing only pending electronic status; it ignores order cancellation and cancelled electronic status.
- **Impact:** Conflicting operational message can misinform staff/customer or driver about collection due. Visible Cancelled status mitigates. No actual collection/ledger loss proven.
- **Remediation Plan:** Make payment text state-aware: cancelled orders should show cancelled/no collection, pending transfers stay unconfirmed, settled orders stay paid, and collect-on-delivery is reserved for active unpaid orders that satisfy COD semantics. Confirm the preferred Arabic copy with owner if wording is material.
- **Required Regression Tests:**
  - Projection test for cancelled + unpaid + electronic status cancelled: no collect-at-delivery label.
  - Controls for active COD, pending electronic, and paid orders.
  - Call Center lookup view-model test asserts status/payment rows do not conflict.
- **Acceptance Criteria:**
  - No cancelled order is described as collectible at delivery.
  - Active COD remains collectible; pending electronic remains unconfirmed; paid remains paid.
  - No database/financial movement is introduced by the presentation correction.

---

### FIN-CAND-NEGATIVE-VARIANT-PRICE-001: Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`, `src/Falah.Application/Menu/MenuManagementRequests.cs`, `src/Falah.Infrastructure/Menu/ProductHandlers.cs`, `src/Falah.Domain/Common/Money.cs`, `src/Falah.Domain/Pricing/ItemPricingRules.cs`, `src/Falah.Infrastructure/Orders/MenuPricingService.cs`, `src/Falah.Domain/Orders/OrderItem.cs`, `src/Falah.Domain/Orders/CheckoutRules.cs`, `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`, `src/Falah.Infrastructure/Persistence/FalahDbContext.cs`, `src/Falah.Infrastructure/Persistence/Migrations/20260902120327_InitialCreate.cs`, `tests/Falah.Server.IntegrationTests/MenuManagementVerticalSliceTests.cs`
- **Root Cause:** Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed
- **Impact:** Potential undercharge or zero-payable order with stock consumption; actual financial incidence and owner policy are unverified.
- **Remediation Plan:** Confirm whether zero-priced variants are allowed. If prices must be positive, centralize the invariant at server command boundaries and checkout resolution, and evaluate a DB check constraint after legacy-row inventory. Preserve signed Money for legitimate ledger deltas.
- **Required Regression Tests:**
  - Domain validation test asserting ProductVariant.PricePiastres >= 0 on create/update.
  - Checkout integration test submitting item with negative price asserting rejection with HTTP 422.
- **Acceptance Criteria:**
  - Owner-approved variant price domain is enforced server-side and at checkout.
  - Invalid variant configuration cannot create underpriced/free stock-consuming checkout.
  - No financial side effect occurs on rejection.
  - Valid positive variants and approved prices remain compatible.

---

### FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001: Delivery report field labeled expected unsettled custody aggregates collected amount instead
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Infrastructure/Reports/ReportingQueryService.cs`, `src/Falah.Application/Reports/ReportingDtos.cs`, `src/Falah.Domain/Delivery/DeliveryRules.cs`, `src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs`, `src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs`, `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`, `src/Falah.Application/Reports/OwnerDashboardDtos.cs`, `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml`, `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml`, `tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs`, `tests/Falah.Server.IntegrationTests/DailyStatementVerticalSliceTests.cs`
- **Root Cause:** Delivery report field labeled expected unsettled custody aggregates collected amount instead
- **Impact:** If the DTO/UI contract means expected cash at driver risk, the report underreports that exposure while delivery is in transit and diverges from the owner dashboard. If it intentionally means cash already collected but not settled, the aggregation may be deliberate but DTO name and tests are misleading. No loss, duplicate payment, or ledger corruption is established.
- **Remediation Plan:** Confirm report contract. Existing DTO name, owner dashboard, and test comments support ExpectedPiastres: if that is the intended meaning, sum c.ExpectedPiastres. If the intended metric is collected-but-unsettled, rename the DTO/display and retain/add a distinct expected-at-risk metric so the two values are not conflated.
- **Required Regression Tests:**
  - Delivery query test asserting ExpectedUnsettledCustody aggregates unremitted in-flight orders only.
  - End-of-shift reconciliation test asserting remitted cash is excluded from courier custody balance.
- **Acceptance Criteria:**
  - DTO field name and source aggregate express the same amount.
  - Owner dashboard and delivery report agree for the same non-settled custody cohort when they claim to show expected custody.
  - In-transit and variance cases are covered by tests; no real printer or financial operation is needed.
  - Historical statement temporal semantics are documented separately.
  - Before delivery, delivery report and owner dashboard agree on expected cash responsibility for the same open custody cohort while collected remains zero.

---

## Order Lifecycle And Workflow

### FLOW-001: Requested two-stage pickup preparation and final payment lifecycle is absent
- **Severity:** `HIGH_REQUIREMENT_GAP` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Orders/CheckoutOrderRequest.cs`, `src/Falah.Application/Orders/OrderDetailsDto.cs`, `src/Falah.Domain/Orders/CheckoutRules.cs`, `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`, `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`, `src/Falah.Server/Program.cs`
- **Root Cause:** Creation, stock consumption and payment checkout share one use case (CheckoutOrderHandler); only Delivery Cash has explicit deferred cash rule. No state machine exists for pay-on-collection pickup orders.
- **Impact:** Operational gap: Kitchen cannot be notified to prepare pickup orders before payment has been taken in full, blocking customer phone/advance pickup workflows.
- **Remediation Plan:** Design separate pickup fulfilment state and payment state with server-authoritative transitions (two-stage reserve-and-prep -> pay-and-handover).
- **Required Regression Tests:**
  - Recommend lifecycle acceptance tests: unpaid reservation queues kitchen once, payment later posts once, handover only after policy gate, cancellation/restock/close blockers, network replay at each stage.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### FLOW-002: Held electronic preparation is not automatically released on confirmation
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Settings/SettingsRequests.cs`, `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`, `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`, `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`, `src/Falah.Server/Program.cs`
- **Root Cause:** Suppression is a one-time checkout decision without durable preparation-release state or confirmation outbox event.
- **Impact:** HoldPreparationUntilPaymentConfirmed suppresses initial kitchen routes. Confirmation posts money/status but does not enqueue held preparation. Auto-print documentation directs operator to Order Lookup.
- **Remediation Plan:** Explicitly communicate manual release now; future approved implementation should persist held state and transactionally enqueue release with stable key on confirmation.
- **Required Regression Tests:**
  - Add hold->confirm release exactly once, replay, mixed payment failure, cancellation-before-release and manual reprint tests.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

## Printing And Hardware

### PRT-001: Two machines can submit the same pending kitchen job
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`, `src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs`, `src/Falah.PrintAgent/PrintJobProcessor.cs`, `src/Falah.PrintAgent/Program.cs`, `src/Falah.PrintAgent/UnreportedAttemptStore.cs`, `src/Falah.Server/Program.cs`
- **Root Cause:** CreatePrintJobsHandler.cs generates job ID using request.RequestId, and PrintJobs table in FalahDbContext lacks a composite unique constraint on (OrderId, Route).
- **Impact:** Duplicate kitchen and receipt tickets printed when multiple stations submit print requests for the same order.
- **Remediation Plan:** Add composite unique index on (OrderId, Route, DocumentKind) in PrintJobEntity configuration, or ensure idempotent deduplication at database level.
- **Required Regression Tests:**
  - Existing PrintAgentTests cover route selection and one-agent report retry only; add concurrent two-agent claim/fencing test, and replacement-agent unknown-outcome recovery.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### PRT-002: Succeeded and print health collapse transport acceptance into physical completion
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Printing/PrintingRequests.cs`, `src/Falah.Application/Reports/OwnerDashboardDtos.cs`, `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`, `src/Falah.PrintAgent/PrintJobProcessor.cs`, `src/Falah.PrintAgent/Transports.cs`, `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`, `tools/pre-demo-check.ps1`
- **Root Cause:** Boolean transport contract has no acknowledgement level or spooler submission ID; job status has no accepted/unknown/confirmed distinction.
- **Impact:** Processor sets Succeeded after SendAsync returns. File write, TCP buffer acceptance and Windows RAW submission all satisfy this. Server overview derives last success from that flag; owner DTO comment describes physical output.
- **Remediation Plan:** Use distinct Simulated, Submitted, SpoolerQueued, Failed, OutcomeUnknown and PhysicallyConfirmed observations. Never drain real pending jobs to file as production completion.
- **Required Regression Tests:**
  - Existing success test verifies only fake transport Sent list. Add file-simulation status, paused spooler status under explicit authorization, and operator-confirmation tests.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### PRT-003: Blocking spooler submission ignores cancellation and completion failures
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.PrintAgent/AgentOptions.cs`, `src/Falah.PrintAgent/Program.cs`, `src/Falah.PrintAgent/Transports.cs`, `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`
- **Root Cause:** Timeout configuration applies to HTTP/TCP/serial write but not full native spooler lifecycle; unmanaged result handling is incomplete.
- **Impact:** Windows SendAsync calls blocking Send and discards cancellation. EndPagePrinter/EndDocPrinter results are ignored. Serial Open/Flush also block without cooperative cancellation.
- **Remediation Plan:** Check finalization results, retain submission job ID, move blocking native submission to a controlled worker with bounded shutdown/uncertain outcome semantics.
- **Required Regression Tests:**
  - Add injectable spooler API tests for failure and hang, short writes already checked in implementation; separate integration verification from physical output.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### PRT-004: Agent never authenticates again after session loss
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.PrintAgent/PrintServerClient.cs`, `src/Falah.PrintAgent/Program.cs`, `src/Falah.Server/AuthenticationMiddleware.cs`
- **Root Cause:** No token-expiry or reauthentication recovery path; agent lifecycle assumes continuously sliding valid session.
- **Impact:** Login occurs once before poll loop. All subsequent 401 failures are logged and retried with same token indefinitely.
- **Remediation Plan:** Add controlled reauthentication and explicit offline/authentication alarm without losing attempt journal or blindly resending uncertain jobs.
- **Required Regression Tests:**
  - Add 401-then-authenticate recovery, deactivated-account terminal failure, and queued outcome reporting across reauth tests.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### PRT-005: Printer verification tool can delete preexisting restaurant tickets
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.PrintAgent/Program.cs`, `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`, `tools/verify-windows-printer-transport.ps1`
- **Root Cause:** Generic document-name matching replaces exact submission ownership; ignored pause result invalidates no-paper guarantee.
- **Impact:** Tool pauses printer without checking success, sends generic Falah ticket, then removes every job with DocumentName Falah ticket and resumes queue.
- **Remediation Plan:** Use dedicated test queue and exact returned job ID, verify pause state, never delete by document name; keep all live printer operations gated.
- **Required Regression Tests:**
  - Add fake queue fixture preserving preexisting IDs, failure-to-pause abort, failed-cleanup no-resume policy. Only isolated queue integration after approval.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### PRT-006: Saved printer width and receipt copy count do not control inspected print pipeline
- **Severity:** `LOW` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Settings/SettingsRequests.cs`, `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`, `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`, `src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs`, `src/Falah.PrintAgent/AgentOptions.cs`
- **Root Cause:** Persistence/UI configuration has no mapped consumer in render/dispatch implementation.
- **Impact:** Settings accepts PrinterWidth and NumberOfReceiptCopies, but auto-print creates one job per route and agent uses its own ImageWidthDots. Operators can save values that do not change output.
- **Remediation Plan:** Define per-station paper capability and copy semantics; explicitly label unsupported settings until implemented. Copies must not multiply kitchen prep accidentally.
- **Required Regression Tests:**
  - Add intended copy/width contract tests or disable unsupported setting UI.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

## Security And Access Control

### SEC-001: JSON actor casing mismatch permits body-selected authorization identity
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED_AND_CORRECTED`
- **Source References:** `src/Falah.Server/AuthenticationMiddleware.cs` (lines [[86, 133]]), `src/Falah.Server/Program.cs` (lines [[1951, 1958]]), `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs` (lines [[32, 36]]), `src/Falah.Application/Users/UserManagementRequests.cs` (lines [[1, 20]])
- **Root Cause:** In AuthenticationMiddleware.cs (lines 86-133), FindActorMismatchAsync uses case-sensitive TryGetProperty("actorUserId", out _) against lowercase string names. An incoming JSON payload with PascalCase 'ActorUserId' silently bypasses the mismatch check. Minimal API model binding in Program.cs (lines 1951-1958) binds case-insensitively into UpdateUserPermissionsRequest.ActorUserId. UpdateUserPermissionsHandler.cs (lines 32-36) queries DB for request.ActorUserId and authorizes the operation using that user's permissions instead of context.ActorId() from the verified bearer session token.
- **Impact:** Source-Proven: Complete privilege escalation. Any authenticated user holding a valid session token (even cashier or low-privilege employee) can grant themselves arbitrary permissions (including users_manage, orders_refund, all) by supplying 'ActorUserId': 'usr-admin' in PascalCase on POST /api/users/{id}/permissions. (Prior claim of arbitrary JSON Actor object injection was inaccurate and is refuted by wire DTO definitions).
- **Remediation Plan:** 1. In AuthenticationMiddleware.cs, parse request body using case-insensitive property name matching (e.g. EnumerateObject() with StringComparison.OrdinalIgnoreCase).
2. Refactor Program.cs endpoint and UpdateUserPermissionsHandler to eliminate ActorUserId from wire DTOs and bind authorized caller identity exclusively from context.ActorId() established by token session resolution.
- **Required Regression Tests:**
  - Submit POST /api/users/{targetId}/permissions with token for low-privilege cashier and body containing 'ActorUserId': 'usr-admin'; assert HTTP 403 Forbidden.
  - Verify case variations ('actorUserId', 'ActorUserId', 'ACTORUSERID') all trigger actor mismatch rejection in middleware.
  - Verify UpdateUserPermissionsHandler authorizes against authenticated session user even if body attempts to provide ActorUserId.
- **Acceptance Criteria:**
  - AuthenticationMiddleware rejects any mismatched actor identity regardless of JSON key casing.
  - Handlers never read or trust client-supplied actor user ID from request body.

---

### SEC-002: PIN-bearing user-management requests are durably stored as unencrypted JSON
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED_AND_CORRECTED`
- **Source References:** `src/Falah.Application/Users/UserManagementRequests.cs`, `src/Falah.Client.CashierWinUI/App.xaml.cs`, `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`, `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`, `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`
- **Root Cause:** FileRetainedRequestStore.cs in CashierWinUI and CallCenterWinUI writes raw in-flight request payloads to %LOCALAPPDATA%\Falah\pending-operations\{station}\{ownerKey}.json as unencrypted plaintext JSON. User creation, PIN changes, and manager overrides that run while offline or mid-dispatch persist plaintext PINs on disk.
- **Impact:** Workstation credential exposure. Anyone with read access to the local machine's user directory (%LOCALAPPDATA%) can read persistent and in-flight employee and manager PINs in plaintext.
- **Remediation Plan:** Encrypt retained request payloads using Windows Data Protection API (ProtectedData.Protect with CurrentUser scope) prior to file write. Mask or redact PIN properties from retained request diagnostic envelopes.
- **Required Regression Tests:**
  - Execute CreateUser or PIN change request in CashierWinUI while offline; inspect persisted JSON in %LOCALAPPDATA%; assert payload is ciphertext and PIN is unreadable in plaintext.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### SEC-003: Authenticated employee reads expose complete sensitive profiles without employee permission or self-access restriction
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source References:** `src/Falah.Application/Employees/EmployeeRequests.cs`, `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs`, `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`, `src/Falah.Server/AuthenticationMiddleware.cs`, `src/Falah.Server/Program.cs`
- **Root Cause:** GET /api/employees and GET /api/employees/{employeeId} in Program.cs (lines 685-687) have no permission policy attribute. EmployeeQueryService.cs (lines 81-85) unconditionally projects all database columns (NationalId, DailyWagePiastres, MonthlySalaryPiastres, OvertimeRatePiastres, FingerprintId, Address, Phone) into EmployeeDto.
- **Impact:** Internal data exposure. Any authenticated terminal session (including frontline cashiers, waiters, delivery drivers) can query the exact salaries, overtime rates, national IDs, and home addresses of all restaurant staff.
- **Remediation Plan:** Introduce PublicEmployeeSummaryDto for operational cashier selection. Restrict GET /api/employees full profile to callers possessing 'employees_manage' or 'payroll_view' permissions.
- **Required Regression Tests:**
  - Query GET /api/employees with cashier bearer token; assert response contains only public summary fields and omits salary/national ID.
  - Query GET /api/employees with manager bearer token; assert full profile is returned.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

## Ui Ux And Desktop Client State

### UI-001: Primary cashier and treasury money requests lose original retry identity on process loss
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs` (lines [[29, 56]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs` (lines [[401, 426]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs` (lines [[104, 124]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs` (lines [[16, 18]]), `src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs` (lines [[162, 164]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`, `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs`, `src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs`, `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`, `src/Falah.Client.CashierWinUI/Services/AppSession.cs`, `src/Falah.Client.CashierWinUI/App.xaml.cs`, `tests/Falah.Client.Tests/CloseoutRegressionTests.cs`, `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`
- **Root Cause:** Normal close/edit gates and same-process fixed keys cannot preserve immutable submitted DTO/key across forced termination. Primary financial partials bypass existing durable helper.
- **Impact:** No reliable station-owned recovery question for original checkout/refund/expense/treasury action; manually repeating with new key can create duplicate real operations. Actual duplicate ledger writes NOT tested.
- **Remediation Plan:** Persist exact versioned typed financial request and owner before dispatch; expose recovery by original identity, block replacement until outcome known; no fixes performed.
- **Required Regression Tests:**
  - Simulate committed-but-lost-response with fake transport, reconstruct actual cashier VM/session with store, assert exact request ID/payload recovered before any new action.
  - Repeat for refund, expense, safe drop, drawer transfer, shift open/close and treasury operations; assert server ledger unaffected on replay in isolated integration fixture.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-002: Unselected expense and payment buttons combine literal white with dark-theme pale text
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/Services/ThemeResources.cs` (lines [[18, 35]]), `src/Falah.Client.CashierWinUI/Styles/Colors.xaml` (lines [[235, 235]]), `src/Falah.Client.CashierWinUI/Views/CashierView.xaml` (lines [[313, 340]]), `src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml` (lines [[31, 42]])
- **Root Cause:** Instance background bindings override correctly-themed SecondaryButtonStyle/PaymentMethodButtonStyle with WhiteBrush; text continues themed.
- **Impact:** Near-white labels on white surface, computed source-token contrast ~1.22:1, makes unselected choices hard to distinguish. User visual symptom consistent; reviewer has no actual screenshot and does not claim rendered pixel measurement.
- **Remediation Plan:** Use coherent semantic card/neutral background with themed primary text for unselected state, direct ThemeResource/visual states for live updates; selected accent uses suitable contrast foreground.
- **Required Regression Tests:**
  - Render all expense categories and all four payment methods selected/unselected in Light/Dark/System, unchanged and after live toggle; assert resolved text/background luminance and readable native screenshot.
  - Check disabled/hover/pressed states separately, preserving stored category values.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-003: Requested pickup kitchen-before-payment stage is absent from active client workflow
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs` (lines [[337, 383]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs` (lines [[78, 107]]), `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs` (lines [[39, 42]])
- **Root Cause:** Pickup is a detail variant of common checkout, not separate persisted preparation/awaiting-payment state with stage-aware primary command.
- **Impact:** Client cannot perform owner-requested Stage A kitchen ticket then later Stage B checkout on same persisted unpaid pickup. Server/Domain end-to-end corroboration required before claiming global architecture absence.
- **Remediation Plan:** Design explicit persisted pickup preparation/payment lifecycle using existing backend/domain capabilities where possible; implementation requires separate authorization.
- **Required Regression Tests:**
  - Create pickup preparation without payment in isolated backend test; stable ID and no payment movement; initial kitchen exactly once.
  - Resume same order, collect final money, receipt once; operational pending list hides terminal paid/completed while history remains.
  - Concurrent stage clicks and ambiguous print/payment responses retain stable identities.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-004: Imperative/converter brush results do not track live per-window theme reliably
- **Severity:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Source References:** `src/Falah.Client.CashierWinUI/App.xaml` (lines [[8, 16]]), `src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs` (lines [[22, 30]]), `src/Falah.Client.CashierWinUI/MainWindow.xaml.cs` (lines [[61, 62]]), `src/Falah.Client.CashierWinUI/Services/ThemeResources.cs` (lines [[18, 35]])
- **Root Cause:** Converter returns concrete Brush object; unlike ThemeResource expression, no dependency informs it theme changed. Explicit dictionary lookup also does not traverse merged dictionaries.
- **Impact:** Selected/unselected/status colors can remain from previous or system theme, inconsistent with other surface/text resources. Exact WinUI fallback lookup result not runtime-proved.
- **Remediation Plan:** Prefer theme-bound visual states/resources; explicitly observe ActualThemeChanged and traverse actual dictionary topology if imperative lookup retained.
- **Required Regression Tests:**
  - Use real WinUI root RequestedTheme opposite system theme, compare ThemeResources.Get colors to descendant ThemeResource references.
  - Toggle repeatedly without changing Text/ExpenseCategory/PaymentMethod; compare same realized objects and brushes.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-005: Recovered electronic confirmation/cancellation has no reachable cashier retry context
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs` (lines [[149, 150]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs` (lines [[262, 271]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs` (lines [[22, 25]]), `src/Falah.Client.CashierWinUI/Views/CashierView.xaml` (lines [[164, 205]])
- **Root Cause:** Retained DTO loads but transient LastCheckoutResult does not; all retry reachability assumes that result. Main session sees unresolved op and blocks lock/navigation/normal close.
- **Impact:** Cashier can become frozen after restart even though retained original order ID exists. Correct production reconciliation must not simply delete journal. Potential cross-user context confusion because keys are station-global.
- **Remediation Plan:** Recover operation order context from persisted request or present a standalone pending-action recovery panel independent of transient checkout result.
- **Required Regression Tests:**
  - Seed fake store with actual ConfirmElectronicPaymentRequest/CancelPendingElectronicPaymentRequest, create fresh AppSession/current user/Cashier VM, assert reachable retry command and UI visible; retry exact order identity.
  - Confirm no new checkout, result discard or owner change until original outcome resolved; test another operator sees appropriate recovery ownership.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-006: A new pickup inherits previous order optional collection schedule
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs` (lines [[364, 364]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs` (lines [[131, 138]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs` (lines [[45, 49]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs` (lines [[132, 139]])
- **Root Cause:** Order reset named ResetDelivery also owns shared pickup customer fields but omits pickup-specific schedule.
- **Impact:** New customer order carries previous collection appointment or later fails past-time validation, unless operator notices and clears/changes type.
- **Remediation Plan:** Reset optional schedule with resolved new-order lifecycle and explicit cart cancellation; keep historic order review separate.
- **Required Regression Tests:**
  - Complete scheduled pickup using fake API, dismiss result, add next pickup; date/time and RequestedPickupAtUtc must be null until explicitly selected.
  - Repeat ClearCart/StartNewOrder and historic pending-order review to ensure intentional preserved live draft schedule is not erased.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-007: Late CallCenter customer lookup can overwrite a different phone/new order
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs` (lines [[217, 220]]), `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs` (lines [[217, 231]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs` (lines [[96, 102]])
- **Root Cause:** No requested-phone equality/epoch check after await; async lookup completion mutates latest mutable form regardless of source identity.
- **Impact:** Wrong customer name/address offered under another phone; operator may dispatch wrong destination and upsert mismatched customer data. No real PII or DB writes used to reproduce.
- **Remediation Plan:** Snapshot normalized phone and monotonic form generation; discard stale responses/cancel on change/reset; clear incompatible resolved fields safely.
- **Required Regression Tests:**
  - Deterministic fake HTTP delayed A/B in reverse completion order; assert final name/addresses match B.
  - Begin lookup then ClearOrder, release response; new form stays blank.
  - Lookup failure 404 vs timeout must show correct status; changing phone should invalidate old customer/address selections.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-009: Printer status UI exposes only four of six implemented routes
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs` (lines [[77, 86]])
- **Root Cause:** Hardcoded status-route list lags shared print route vocabulary.
- **Impact:** No dedicated queue summary/filter for two routes; attention aggregate may still surface failed jobs. Not total invisibility claim.
- **Remediation Plan:** Derive displayed route options from complete shared contract plus localized labels.
- **Required Regression Tests:**
  - Provide overview with six routes and each pending/failed/stuck; assert all selectable and counts true.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-010: Workspace shortcut guard omits most open operational modals
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/Views/CashierView.xaml` (lines [[43, 48]]), `src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs` (lines [[31, 54]])
- **Root Cause:** Custom overlays do not centralize modal state and shortcut predicate only checks variant/payment/result.
- **Impact:** Potential underlying focus change, extra flyout or checkout sheet while another form open, risking confused data entry. Runtime keyboard reachability/focus path not executed.
- **Remediation Plan:** Centralize IsAnyModalOpen plus accessible focus isolation and apply to all shortcuts.
- **Required Regression Tests:**
  - Actual WinUI keyboard F2/F3/F12 + Tab/ShiftTab/Escape for every modal, assert no background focus/action and only one modal active.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-011: Discarded definitively rejected CallCenter reprint survives in journal
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs` (lines [[86, 91]])
- **Root Cause:** Safe discard memory path omits corresponding persistent store clear.
- **Impact:** Old rejected request reappears as ambiguous unresolved, can block fresh operation or queue an old reprint on retry when permissions/preconditions change. Does not prove duplicate sale.
- **Remediation Plan:** Clear disk atomically when explicitly discarding known-rejected print; preserve unknown state separately.
- **Required Regression Tests:**
  - Persist definite rejected print, clear through actual UI VM, reconstruct operator desk and assert no pending print/file.
  - Ambiguous failure must continue surviving same clear attempt.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-012: CallCenter customer-copy reprint requests kitchen preparation routes again
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Application/Printing/PrintingRequests.cs` (lines [[12, 17]]), `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs` (lines [[337, 343]]), `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs` (lines [[316, 322]])
- **Root Cause:** CallCenter omitted explicit false flag added to corresponding cashier reprint path.
- **Impact:** Potential duplicate charcoal/grill preparation work and food waste, in addition to requested customer copy. Backend route suppression cross-checked by printing peer as parent reports; parent should attach final handler evidence.
- **Remediation Plan:** Send false for customer-only reprint; expose explicit separately confirmed kitchen reprint only if requirement exists.
- **Required Regression Tests:**
  - CallCenter reprint fake API asserts IncludeKitchenRoutes false.
  - Isolated print-job handler fixture with grill/charcoal items asserts customer copy only for follow-up; explicit kitchen reprint separate labeled action.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-013: Legacy WPF change display parses EGP tender as integer piastres
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs` (lines [[93, 95]])
- **Root Cause:** Display parses 100 as100 piastres or fails for100.00 while authoritative send converts to10000.
- **Impact:** Displayed change zero instead of10 EGP; server-recorded tender may still correct. Does not affect active WinUI CashInput change implementation.
- **Remediation Plan:** If WPF remains supported, use same tested CashInput units and notifications; otherwise prevent accidental deployment.
- **Required Regression Tests:**
  - WPF ChangeDue test 90 EGP total with100 and100.00 tender both returns1000 piastres; Arabic/current-culture formats reviewed separately.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-014: Successful restore is reported incomplete because UI expects Completed instead of Succeeded
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs` (lines [[84, 93]]), `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` (lines [[168, 168]])
- **Root Cause:** UI compares result status to wrong literal Completed; server stores enum Succeeded.
- **Impact:** UI tells operator restore did not complete after database already replaced, leaves restore target shown and can invite repeating dangerous operation. No restore executed by reviewer.
- **Remediation Plan:** Use shared enum/contract success vocabulary and avoid treating generic HTTP success as outcome proof; maintain strict restore controls.
- **Required Regression Tests:**
  - Fake API returns successful DTO Status Succeeded; assert success note, target closed and no false RestoreError.
  - Blocked/Failed records must remain reported non-success; no production DB restore needed for client contract test.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-015: CallCenter lock/unlock retains old order-panel visual trees through cart subscription
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs` (lines [[67, 67]]), `src/Falah.Client.CallCenterWinUI/Services/AppSession.cs` (lines [[23, 23]]), `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs` (lines [[16, 21]])
- **Root Cause:** Long-lived cached VM Cart event holds every prior UserControl handler; subscriptions never removed on unload or DataContext replacement.
- **Impact:** Memory/native visual resource growth and repeated invisible callbacks over long shift; actual memory growth not measured.
- **Remediation Plan:** Track subscribed VM, detach on change/unload, reattach once Loaded.
- **Required Regression Tests:**
  - Native repeated lock/unlock, weak-reference old panel and force test GC; only current panel should remain.
  - DataContext A->B->A event subscription counts exactly1 and no callbacks on detached view.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-016: Management shift actions abandon unknown outcomes and generate a new request identity per retry
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs` (lines [[126, 129]]), `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs` (lines [[71, 71]]), `src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs` (lines [[33, 33]])
- **Root Cause:** Separate management presentation did not adopt cashier immutable pending/outcomeUnknown protocol; only own busy booleans, no session registration or retained key.
- **Impact:** Cannot safely reconcile original shift operation; new attempt may be rejected by single-open/already-closed server guards or mislead state, and user can abandon in-flight money context. Actual duplicate server shift transaction not claimed.
- **Remediation Plan:** Share durable operation coordination between both shift presentations and register global pending before dispatch.
- **Required Regression Tests:**
  - Fake commit-lost-response, retry asserts identical typed DTO/key and immutable counted/opening cash.
  - Root TryClose/Lock/Shell.Navigate blocked while management open/close busy or unresolved.
  - First definite rejection permits correction/new attempt, unknown requires same attempt reconciliation.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-017: Recovered payroll transition or payment cannot reach its retry command
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs` (lines [[334, 334]])
- **Root Cause:** Durable DTO recovery is not accompanied by run selection/pay-dialog reconstruction; all routes to restore context blocked by same pending state.
- **Impact:** Retained financial operation stranded; ordinary page cannot reconcile original status/payment. Unlike create-run retained branch, no standalone retry in this VM.
- **Remediation Plan:** Recover run/visible action from stored request or expose generic pending-action verification independent of transient selection.
- **Required Regression Tests:**
  - Fake retained transition/pay DTO, construct fresh VM/session, assert explicit retry available without selecting new run/account.
  - Read original target by stored identity and show original intended action; exact DTO resends; no new payment until resolved.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-018: Late recipe-detail response can replace another recipe selection before editing
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs` (lines [[579, 581]])
- **Root Cause:** No version/selected-ID validation around asynchronous recipe detail publishing.
- **Impact:** Wrong ingredient list/recipe identity can be edited and saved, affecting future inventory consumption/costing. Server authorizes a valid recipe update but cannot infer operator intended different selected row. Runtime write not tested.
- **Remediation Plan:** Use monotonic detail generation and validate again after every await; freeze target identity across edit/save.
- **Required Regression Tests:**
  - Fake delayed A/B reverse completion asserts Detail.RecipeId equals SelectedRecipe key and StartEdit uses B.
  - Close/change selection while reading must invalidate old response/finally flags.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-019: Recipe yield conversion silently truncates fractional pieces-per-base-unit
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs` (lines [[562, 568]])
- **Root Cause:** MilliUnits parser accepts fractional unit then conversion to integer floor without checking remainder.
- **Impact:** Stored recipe ratio differs from typed value and changes ingredient usage/cost calculation on future sales. No real recipe updated.
- **Remediation Plan:** Validate integer divisibility before division or use dedicated localized positive-integer parser.
- **Required Regression Tests:**
  - Enter1.5,2.999Arabic١٫٥; must reject with explicit integer requirement before HTTP, not send1/2.
  - Whole numbers1,2and textual2.000 accepted exactly, enforce overflow bound.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-020: PIN-change retry proof replaces shared bearer despite preserving visible user identity
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs` (lines [[164, 167]]), `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs` (lines [[14, 14]])
- **Root Cause:** LoginByPinAsync has bearer installation side effect, but proof caller assumes read-only credential test and does not reconcile session.CurrentUser.
- **Impact:** HTTP authenticated identity may diverge from UI/current actor; subsequent requests can fail or run under unintended principal depending server actor enforcement. Parent security reviewer owns final exploitability/severity validation; no real login performed.
- **Remediation Plan:** Use isolated non-session-mutating proof client/endpoint and explicit owner context; coordinate with security report to avoid duplicate final finding.
- **Required Regression Tests:**
  - Fake login proof changes token identity tootheruser, assert root session and bearer remain original after failed proof.
  - Recovered self-PIN request owned by another actor must be visibly routed/blocked; no session switch as side effect.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-021: Late product-image completion changes save target beneath another product form
- **Severity:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs` (lines [[337, 339]]), `src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml` (lines [[376, 376]])
- **Root Cause:** Image completion unconditionally overwrites shared editor identity captured before await. IsImageBusy only disables image controls; ReloadAsync reloads catalogs but does not reset/rebind visible form or editing identity.
- **Impact:** Save targets product A with B field values/variants. Can corrupt menu pricing, preparation route and recipe association despite valid authorized backend request. No product mutation executed by reviewer.
- **Remediation Plan:** Keep immutable operation target independent from current editor; only update visible state when selection generation still matches. Coordinate browse/save with image busy and invalidate late completions on close/kind/new.
- **Required Regression Tests:**
  - Controlled image TaskCompletionSource: A upload pending, SelectRow(B), complete A; editor target and form must remain B, A image catalog updated separately.
  - Repeat with delete, New, SetKind and Close/reopen; no stale completion alters current draft.
  - Assert saved product ID matches displayed selected item after every async completion.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

### UI-022: Recipe ingredient picker truncates to60 choices without exposing its search input
- **Severity:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Source References:** `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs` (lines [[441, 441]]), `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml` (lines [[341, 343]])
- **Root Cause:** VM implemented bounded searchable choices but XAML exposes only ComboBox over current60 and no search binding to query full cached ingredients.
- **Impact:** Operator cannot directly find later ingredients through normal picker. Recipe may remain incomplete or user may choose wrong similar item; no actual recipe mutation tested.
- **Remediation Plan:** Bind explicit ingredient search to IngredientSearchText or provide searchable full-list picker; disclose bounds only as presentation optimization.
- **Required Regression Tests:**
  - Load61+ active ingredients with nonechosen; entering target61 search should exposeit without adding unrelated ingredients.
  - Exclude chosenIDs while searching fullcached inventory; clearing search should preserve selected choice and exact recipe lines.
- **Acceptance Criteria:**
  - Invariant holds under retries and concurrent executions.

---

Complete structured database: [`RECONCILED_MASTER_FINDINGS.json`](RECONCILED_MASTER_FINDINGS.json)