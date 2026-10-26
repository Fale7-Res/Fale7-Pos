# Detailed Implementation Specifications & Remediation Blueprints

- **Total Actionable Specifications:** **100**
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Governance State:** `REOPENED_EVIDENCE_BASED_AUDIT_IN_PROGRESS`
- **External Verification Gate Status:** `READY_FOR_EXTERNAL_CODEX_VERIFICATION`
- **Audit Mode:** Pure Forensic Blueprint Specification (Zero Application Code Modified)
- **Target Audience:** Implementation Engineers, System Architects & QA Automation Specialists
- **Specification Standard:** Atomic, test-driven, source-referenced blueprints with exact acceptance gates.

---

## Executive Implementation Matrix

| Priority Level | Scope & Operational Risk | Count | Immediate Action Required |
| :--- | :--- | :---: | :--- |
| **P0_CRITICAL_STOP_SHIP** | Financial loss, hard crashes, till lockout, corrupted ledger entries | **9** | Immediate remediation before any production release |
| **P1_HIGH_CORE_OPERATIONS** | Daily settlement errors, shift closeout races, security bypasses, UI lockouts | **35** | Stage 1 Sprint: core transaction and workflow integrity |
| **P2_STABILITY_AND_MAINTAINABILITY** | Memory bloat, N+1 scans, background async errors, rounding drift | **56** | Stage 2 Sprint: performance optimization and code health |
| **TOTAL** | **Comprehensive Engineering Backlog** | **100** | **Full Audit Resolution** |

---

## 1. P0 CRITICAL STOP SHIP (9 Specifications)

> [!CAUTION]
> **STOP-SHIP MANDATE:** Deploying the application without remediating these 9 critical defects will result in direct till drawer cash discrepancies, split payment lockouts, silent supplier overpayments, or unrecoverable journal collisions.


### Spec: FIN-001 — Mixed cash/electronic checkout cannot confirm its electronic leg
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs), [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs)

**Root Cause Diagnosis:**  
CheckoutOrderHandler.cs (line 148) posts the cash tender leg using idempotency key 'sale-{CheckoutRequestId}'. When ConfirmElectronicPaymentHandler.cs (line 75) attempts to post the deferred electronic split leg, it attempts to insert into EfFinancialJournal using the exact same key 'sale-{CheckoutRequestId}', throwing a unique constraint violation.

**Operational Impact:**  
Split payment completion failure. Cash is accepted and deposited into the drawer, but the electronic leg (InstaPay/Card) can never be confirmed on the server; the order remains stuck in pending payment state.

**Engineering Implementation Blueprint:**  
Key journal entries by stable leg identity, e.g. 'sale-{CheckoutRequestId}-{TenderType}'. Transition pending electronic leg atomically to confirmed.

**Required Verification & Regression Tests:**
- Mixed cash+InstaPay and card+Wallet checkout, confirmation, retry, and concurrent confirm; verify exact tender totals and one movement per leg.
- Replay same request and verify stable movement identity.

**Acceptance Criteria:**
- Each tender leg can complete independently and exactly once.
- Confirmed electronic state and its movement commit atomically; channel totals reconcile.

---

### Spec: FIN-002 — Cancelling pending mixed electronic payment hides collected cash without reversal
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs)

**Root Cause Diagnosis:**  
Order.cs:CancelElectronicPayment sets order Status to Cancelled without issuing a reversal journal entry for cash tender legs that were already deposited into the till drawer at checkout.

**Operational Impact:**  
Treasury divergence. Drawer cash remains physically and logically deposited, but order disappears from sales reports, causing shift drawer reconciliation discrepancies.

**Engineering Implementation Blueprint:**  
When cancelling a mixed tender order, check if any cash leg was deposited and atomically post a reversal journal movement to the drawer.

**Required Verification & Regression Tests:**
- Cancel cash+electronic and card+electronic orders; reconcile treasury, operational cash, and reports.
- Race cancel against confirm and require one terminal tender outcome.
- Verify refund only when funds are actually returned.

**Acceptance Criteria:**
- Cancelled order/payment state preserves every collected tender until a matching reversal exists.
- Order, tender, treasury, and report totals reconcile after either winning race.

---

### Spec: FIN-003 — Refund can debit money for an order that was never collected
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs)

**Root Cause Diagnosis:**  
Order.cs:ApplyRefund and RefundOrderHandler.cs do not verify that the order was actually settled (IsPaid == true) before approving a cash refund.

**Operational Impact:**  
Cash drain. An operator can issue a cash refund against an unpaid Cash-On-Delivery order or pending InstaPay order, draining drawer funds for money never collected.

**Engineering Implementation Blueprint:**  
Enforce invariant in Order.ApplyRefund that order must be fully collected (IsPaid == true) and refund amount cannot exceed collected tender.

**Required Verification & Regression Tests:**
- Unpaid COD, pending electronic, confirmed paid, partial collected, and unsupported mixed cases.
- Race refund with cancel/confirm and concurrent refunds; rejected cases create no refund/movement and balances remain unchanged.

**Acceptance Criteria:**
- Cumulative refunds never exceed the order’s actually settled amount per tender leg.
- Rejected unpaid refund leaves no partial writes; valid refund atomically records refund and outflow.

---

### Spec: FIN-005 — Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs)

**Root Cause Diagnosis:**  
PayPurchaseHandler.cs (lines 59-74) reads supplier invoice balance without UPDLOCK or serializable isolation, allowing concurrent transactions to each read the remaining balance before either updates it.

**Operational Impact:**  
Supplier overpayment and invoice balance corruption under concurrent settlement clicks.

**Engineering Implementation Blueprint:**  
Execute invoice settlement within an explicit Serializable transaction or apply pessimistic locking (UPDLOCK, ROWLOCK) on the purchasing invoice record.

**Required Verification & Regression Tests:**
- Barrier: two different 80 settlements against outstanding 100, both same account and different accounts; at most one succeeds, total <=100 and cache equals ledger.
- Same request replay returns committed result.

**Acceptance Criteria:**
- For each invoice, committed settlements never exceed current outstanding.
- Cached paid/outstanding fields match ledger under serialized settlements.
- Retries remain idempotent and deadlock policy is bounded.

---

### Spec: FIN-016 — Unvalidated signed delivery fee can bypass intended discount and comp authorization
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Application/Orders/CheckoutOrderRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/CheckoutOrderRequest.cs), [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
CheckoutOrderHandler.cs (line 142) accepts request.DeliveryFeePiastres directly from client payload without verifying against DeliveryService.DefaultFee or checking manager authorization.

**Operational Impact:**  
Delivery fee tampering. A client can supply negative or zero delivery fee to discount an order without triggering manager discount authorization.

**Engineering Implementation Blueprint:**  
Validate delivery fee server-side against configured delivery rates or require manager override for non-standard delivery fees.

**Required Verification & Regression Tests:**
- Negative fee with ordinary POS actor; overflow boundaries; assert no writes.
- Positive/zero fee, tax, approved comp, COD and legitimate discounts.

**Acceptance Criteria:**
- Invalid signed fee and arithmetic overflow rejected before any order, stock, journal or treasury write.
- Valid positive/zero fees and approved discount/comp routes retain policy behavior.

---

### Spec: FIN-MULTI-ELEC-001 — Split Payment Electronic Split Ceiling Restriction
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs)

**Root Cause Diagnosis:**  
CheckoutRules.SelectElectronicLeg assumes at most one electronic leg in a split payment, discarding all subsequent electronic legs via .First().

**Operational Impact:**  
Secondary electronic payments are not tracked as pending claims, leaving order in partially confirmed state and losing money reconciliation trace.

**Engineering Implementation Blueprint:**  
Refactor Order and CheckoutPlan to support IReadOnlyList<ElectronicPayment> or enforce single-electronic split limit during checkout preflight.

**Required Verification & Regression Tests:**
- Checkout split payment with InstaPay and Wallet legs; verify both legs are tracked in order electronic payments collection.

**Acceptance Criteria:**
- Multi-tender orders with multiple electronic splits preserve all electronic claims without truncation.

---

### Spec: SEC-001 — JSON actor casing mismatch permits body-selected authorization identity
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Application/Users/UserManagementRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Users/UserManagementRequests.cs), [`src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs), [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
In AuthenticationMiddleware.cs (lines 86-133), FindActorMismatchAsync uses case-sensitive TryGetProperty("actorUserId", out _) against lowercase string names. An incoming JSON payload with PascalCase 'ActorUserId' silently bypasses the mismatch check. Minimal API model binding in Program.cs (lines 1951-1958) binds case-insensitively into UpdateUserPermissionsRequest.ActorUserId. UpdateUserPermissionsHandler.cs (lines 32-36) queries DB for request.ActorUserId and authorizes the operation using that user's permissions instead of context.ActorId() from the verified bearer session token.

**Operational Impact:**  
Source-Proven: Complete privilege escalation. Any authenticated user holding a valid session token (even cashier or low-privilege employee) can grant themselves arbitrary permissions (including users_manage, orders_refund, all) by supplying 'ActorUserId': 'usr-admin' in PascalCase on POST /api/users/{id}/permissions. (Prior claim of arbitrary JSON Actor object injection was inaccurate and is refuted by wire DTO definitions).

**Engineering Implementation Blueprint:**  
1. In AuthenticationMiddleware.cs, parse request body using case-insensitive property name matching (e.g. EnumerateObject() with StringComparison.OrdinalIgnoreCase).
2. Refactor Program.cs endpoint and UpdateUserPermissionsHandler to eliminate ActorUserId from wire DTOs and bind authorized caller identity exclusively from context.ActorId() established by token session resolution.

**Required Verification & Regression Tests:**
- Submit POST /api/users/{targetId}/permissions with token for low-privilege cashier and body containing 'ActorUserId': 'usr-admin'; assert HTTP 403 Forbidden.
- Verify case variations ('actorUserId', 'ActorUserId', 'ACTORUSERID') all trigger actor mismatch rejection in middleware.
- Verify UpdateUserPermissionsHandler authorizes against authenticated session user even if body attempts to provide ActorUserId.

**Acceptance Criteria:**
- AuthenticationMiddleware rejects any mismatched actor identity regardless of JSON key casing.
- Handlers never read or trust client-supplied actor user ID from request body.

---

### Spec: SEC-004 — Shift Close Identity Spoofing via Unchecked ClosedByUserId
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs), [`src/Falah.Application/Shifts/CloseShiftRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Shifts/CloseShiftRequest.cs), [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs)

**Root Cause Diagnosis:**  
AuthenticationMiddleware.ActorFields defines ['actorUserId', 'cashierId', 'createdByUserId', 'openedByUserId', 'reportedByUserId'] but omits 'closedByUserId'. When CloseShiftRequest is processed, the middleware performs no mismatch check against the authenticated user token, permitting arbitrary identity spoofing during shift closure.

**Operational Impact:**  
Any authenticated till operator can unilaterally close shifts belonging to other cashiers, assigning cash discrepancies, drawer shortages, or uncollected delivery tickets to innocent colleagues.

**Engineering Implementation Blueprint:**  
Add 'closedByUserId' to ActorFields in AuthenticationMiddleware.cs, or inject HttpContext.ActorId() directly in Minimal API endpoint binding.

**Required Verification & Regression Tests:**
- Send CloseShiftRequest with ClosedByUserId differing from authenticated token; assert HTTP 403 Forbidden is returned.
- Verify legitimate shift close succeeds when ClosedByUserId matches token identity.

**Acceptance Criteria:**
- Shift close requires caller identity matching authenticated bearer token.

---

### Spec: SEC-008 — Session Token Replacement & Identity Confusion in PIN Replay Proof
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P0_CRITICAL_STOP_SHIP`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs), [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs)

**Root Cause Diagnosis:**  
LoginByPinAsync has the side effect of mutating the shared ApiClient session token. Using it as a credential verification probe overwrites the logged-in operator's token.

**Operational Impact:**  
Session hijacking and identity confusion: subsequent till operations are executed under the credential being verified rather than the active cashier's identity.

**Engineering Implementation Blueprint:**  
Implement dedicated POST /api/auth/verify-pin endpoint that validates PIN without issuing or mutating session tokens.

**Required Verification & Regression Tests:**
- Replay retained PIN change request; assert active session token on ApiClient remains unchanged.

**Acceptance Criteria:**
- PIN verification during offline request replay does not alter client session credentials.

---

## 2. P1 HIGH CORE OPERATIONS (35 Specifications)

> [!WARNING]
> **OPERATIONAL INTEGRITY SPRINT:** These 35 defects compromise operational reliability, cause daily shift close discrepancies, allow potential authorization casing bypasses, or trigger inventory and delivery custody anomalies during standard store operations.


### Spec: FIN-004 — Historical cash refund omitted from current-shift expected cash
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Domain/Orders/RefundRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs), [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs), [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs)

**Root Cause Diagnosis:**  
Drawer mixes lifetime order net amounts with selected operational movement types and does not allocate refunds by OperationalShiftId.

**Operational Impact:**  
A cash refund executed during current shift B for a sale from shift A may reduce lifetime/order net while the drawer reconciliation for B omits the refund outflow or allocates it to the original sale; expected cash and actual drawer diverge.

**Engineering Implementation Blueprint:**  
Compute drawer flow from posted operational cash movements or join refund rows by OperationalShiftId; avoid double deduction for same-shift refunds and preserve immutable closed shift snapshots. Separate original sale period from refund execution period.

**Required Verification & Regression Tests:**
- Historical shift A sale refunded in B; verify B actual/expected and final close balance reconcile.
- Same-shift partial cash refund counted exactly once.
- Historic noncash refund and cross-business-day reports preserve attribution; idempotent retry.

**Acceptance Criteria:**
- Each refund cash outflow appears once in the shift/business period in which it was executed.
- Historical closed shift snapshot remains immutable; current operational drawer reconciles to movements.

---

### Spec: FIN-006 — Generic payroll transition marks run Paid without paying treasury or employee ledger
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs), [`src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs)

**Root Cause Diagnosis:**  
TransitionPayrollHandler.cs permits generic status transition of an approved payroll run directly to 'Paid' without generating corresponding treasury journal disbursements or employee ledger settlement entries.

**Operational Impact:**  
Payroll status marked Paid without actual money leaving treasury or settling employee balances.

**Engineering Implementation Blueprint:**  
Disallow transition to Paid via generic TransitionPayrollHandler. Require execution exclusively through PayPayrollHandler which generates atomic treasury and ledger entries.

**Required Verification & Regression Tests:**
- Generic Approved->Paid is rejected and writes nothing.
- Dedicated pay atomically creates expected movement and each settlement.
- Actor without payroll_pay cannot set Paid through generic route.
- Concurrent cancel/pay results in exactly one consistent terminal state.

**Acceptance Criteria:**
- Paid status is equivalent to completed committed payout effects.
- No API path can mark paid without required payroll_pay authorization and all financial rows.

---

### Spec: FIN-008 — Restore pending-journal check does not exclude active or newly accepted financial work
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs)

**Root Cause Diagnosis:**  
Pending journal staged and completed before common SaveChanges cannot reliably reveal in-flight work; a count snapshot is not writer exclusion. Restore does not put API/print/background writers into maintenance barrier.

**Operational Impact:**  
Restore’s pending-journal check does not exclude active or newly accepted writes. Restore may enter SINGLE_USER/replace while acknowledged financial operation or background writer is in flight; recovery of latest acknowledged work is not assured by the current check.

**Engineering Implementation Blueprint:**  
Use a database-backed maintenance admission lease respected by all mutating handlers and background workers; drain in-flight operations, pause workers, take safety backup, restore and verify, clear stale pools/caches/sessions as designed, then resume only after durable outcome recording.

**Required Verification & Regression Tests:**
- Pure coordinator test: after maintenance lease acquired, new writes are rejected and prior writes drained.
- Dedicated disposable instance restore with approved fault injection and proof of last acknowledged sale recovery.
- Exercise job/API writers and restore failures; never use user-active database.

**Acceptance Criteria:**
- Restore cannot begin until writers are excluded/drained.
- Every accepted operation is either included in pre-restore safety point or has an explicit replay/recovery record.
- Failure leaves a known healthy database and maintenance mode controlled.

---

### Spec: FIN-011 — Shift opening and purchase receipt check protected account balance before their transactions
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs), [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs), [`src/Falah.Infrastructure/Treasury/TreasuryMapper.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/TreasuryMapper.cs)

**Root Cause Diagnosis:**  
UPDLOCK/HOLDLOCK loader invoked before BeginTransaction; statement-end implicit transaction releases lock, balance snapshot stale at spending. SingleOpen index protects only shift rows.

**Operational Impact:**  
Shift opening float and/or purchase receipt may authorize against stale Main account balance because protective lock query occurs before transaction; competing outflow may spend same available funds, potentially allowing negative pooled account or failed/partial admission.

**Engineering Implementation Blueprint:**  
Start transaction before protected account balance load/check and hold lock through float/receipt writes; establish consistent account/shift/invoice lock order; make retries safe and preserve real concurrency errors.

**Required Verification & Regression Tests:**
- Barrier-controlled open-float versus withdrawal/expense/purchase distinct requests; account never negative and exactly feasible operation set commits.
- Same open request replay and single-active-shift invariant still hold.
- Verify purchase receipt document/inventory/financial rows atomic.

**Acceptance Criteria:**
- Protected balance read and spend are in one transaction with effective lock lifetime.
- No successful concurrent commits exceed available balance; rejected command has no partial effects.

---

### Spec: FIN-012 — Attendance correction credits do not enter calculated payroll
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs)

**Root Cause Diagnosis:**  
Correction records signed Adjustment entries; payroll includes Adjustment debits only, with no credit earnings bucket.

**Operational Impact:**  
A positive attendance correction recorded as Adjustment credit may be ignored by payroll’s included earnings/deduction categories, so the corrected employee period net can remain lower than ledger balance.

**Engineering Implementation Blueprint:**  
Reconcile both credit and debit sides of Adjustment into payroll net; preserve correction attribution and original earning basis if required; keep payroll freeze semantics explicit.

**Required Verification & Regression Tests:**
- Payroll input with prior Deduction debit plus correcting Adjustment credit restores expected net.
- Attendance correction followed by payroll calculation and report agrees with employee ledger.
- Correction before run creation and after run freeze follow documented policy.

**Acceptance Criteria:**
- Every eligible period employee-ledger credit/debit included exactly once in payroll liability.
- Payroll line and ledger are consistent; post-freeze correction follows explicit reopen/adjustment policy.

---

### Spec: FIN-014 — Restore success followed by MULTI_USER failure is classified as database not replaced
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs)

**Root Cause Diagnosis:**  
databaseWasReplaced becomes true only after RESTORE and MULTI_USER both return. If restore159 succeeded and MULTI_USER164 throws/cancels, catch releases mode best-effort but leaves false flag and Failed status.

**Operational Impact:**  
Operator may be told restore failed after database replacement; RecordOutcome false branch does not re-add disappeared restore row or clear killed connection pool. Incorrect outcome can encourage a dangerous retry. Actual failure at this precise boundary has not been produced.

**Engineering Implementation Blueprint:**  
Track restore replacement separately from recovery/availability state; persist explicit replaced-but-recovery-incomplete outcome outside replaced database. Resolve uncertain SQL acknowledgments through a recovery protocol; do not auto-retry destructive restore.

**Required Verification & Regression Tests:**
- Fake coordinator injects failure/cancellation after RESTORE but before/during MULTI_USER.
- Approved disposable-instance fault injection verifies replaced database is never reported untouched and no automatic retry occurs.

**Acceptance Criteria:**
- Restore command outcome distinguishes database replacement from return to MULTI_USER/availability; partial success is durably recorded outside replaced DB.
- Injected post-RESTORE mode failure never reports database untouched and never triggers automatic retry.

---

### Spec: FIN-015 — Checkout can commit after its previously open shift has completed handover
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Orders/InventoryDeductionService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/InventoryDeductionService.cs), [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs), [`src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Persistence/OrderSequenceService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/OrderSequenceService.cs), [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs), [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Open shift predicate read before checkout transaction is not revalidated/held through commit; closing uses a different correctly locked Shift update that checkout does not perform.

**Operational Impact:**  
Recorded closed shift expected/actual/handover exclude newly committed cash order while order and posted drawer collection reference that closed shift; new order time can be later than ClosedAtUtc. Stock can also be consumed when configured. No cash loss, fraud, exploit, or accepted real write observed.

**Engineering Implementation Blueprint:**  
Serialize checkout admission and shift close with a shared transaction-held Shift row protocol: lock/revalidate Open before writes, hold through commit; alternatively use a documented Closing/drain process. Preserve closed snapshots.

**Required Verification & Regression Tests:**
- Barrier after checkout shift read and before transaction; close then resume checkout: reject with zero order/payment/stock effects.
- Reverse ordering: checkout admission held; close waits and includes sale.
- Same-key replay after close remains nonmutating.

**Acceptance Criteria:**
- Fresh checkout and shift close serialize through shared shift admission; every accepted checkout is included in handover or rejected with no effects.
- Same-key replay after close remains idempotent and nonmutating.

---

### Spec: FIN-019 — Pending mixed delivery includes already collected payment leg in driver custody
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Application/Delivery/DeliveryRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Delivery/DeliveryRequests.cs), [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs), [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs), [`src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml), [`src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml), [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs), [`src/Falah.Domain/Delivery/DeliveryStatus.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryStatus.cs), [`src/Falah.Domain/Financial/FinancialRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/FinancialRules.cs), [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/ElectronicPayment.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/ElectronicPayment.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs), [`src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs), [`src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs), [`src/Falah.Infrastructure/Delivery/MarkDeliveryReadyHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/MarkDeliveryReadyHandler.cs), [`src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs), [`src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Orders/OrderEnumMapping.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderEnumMapping.cs), [`src/Falah.Infrastructure/Orders/OrderMapper.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderMapper.cs), [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Delivery responsibility collapses per-leg payment collection into one whole-order IsPaid flag and Total-refund. Mixed pending order is partially collected, but modeled as wholly uncollected for driver custody.

**Operational Impact:**  
Overstated driver collection/custody by already posted immediate leg; possible duplicate customer collection or artificial driver variance/approval requirement. No claim actual customer payment, cash theft, SQL acceptance or persisted double revenue was reproduced.

**Engineering Implementation Blueprint:**  
Use per-tender-leg collection status/amount when deriving driver custody. Do not reinterpret pending electronic receivable as driver cash; block dispatch or provide an explicit owner-approved conversion/recovery workflow.

**Required Verification & Regression Tests:**
- Mixed immediate cash/card plus pending electronic dispatch; assert custody does not include collected leg or silently convert pending claim.
- All-immediate Multi and cash COD positive controls; explicit conversion policy only if approved.

**Acceptance Criteria:**
- Driver custody equals only currently collectible amounts under documented payment contract; pending electronic leg is never silently converted to cash owed.
- Block or explicitly convert pending leg only under owner-approved flow, with atomic custody/movement/reporting.

---

### Spec: FIN-031 — Payroll Advance Calculation Ledger Entry Type Case Mismatch in Uncalled Domain Method (Latent Defect)
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs)

**Root Cause Diagnosis:**  
PayrollRules.OutstandingAdvance compares ledger entry type against lowercase string "advance" while C# infrastructure persists PascalCase enum nameof(LedgerEntryType.Advance). Counterevidence inspection reveals that PayrollRules.OutstandingAdvance is NOT called by any production handler (CreatePayrollHandler hardcodes RequestedAdvanceDeduction to Money.Zero, and PayPayrollHandler does not query advances). The method is currently latent dead code with no active production caller.

**Operational Impact:**  
Latent defect. There is no active current financial loss from this method in production because it is uncalled. However, if any future payroll enhancement attempts to invoke PayrollRules.OutstandingAdvance to calculate outstanding employee advances, it will silently return 0, causing complete omission of advance deductions. Active production payroll advance issues stem from FIN-034 (missing ledger schema column and unposted recoveries).

**Engineering Implementation Blueprint:**  
Update PayrollRules.OutstandingAdvance to check string equality using StringComparison.OrdinalIgnoreCase or match against nameof(LedgerEntryType.Advance) / nameof(LedgerEntryType.PayrollSettlement).

**Required Verification & Regression Tests:**
- Create employee cash advance with Type="Advance" (Debit=500 EGP); call PayrollRules.OutstandingAdvance; assert outstanding advance equals 500 EGP.
- Add settlement with Type="PayrollSettlement" (AdvanceDeduction=200 EGP); assert outstanding advance reduces to 300 EGP.

**Acceptance Criteria:**
- PayrollRules.OutstandingAdvance recognizes both PascalCase and lowercase ledger entry type strings.

---

### Spec: FIN-033 — BusinessClock DST Spring-Forward Transition Causes ArgumentException on Business Date Boundary Calculation
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Common/BusinessClock.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs)

**Root Cause Diagnosis:**  
UtcRangeForBusinessDate passes midnight (00:00:00) directly to TimeZoneInfo.ConvertTimeToUtc without verifying whether the time falls within a daylight saving time spring-forward gap.

**Operational Impact:**  
Unhandled System.ArgumentException crashes all API endpoints, reports, and background jobs that calculate date ranges for the last Friday of April (and the preceding Thursday when computing range end).

**Engineering Implementation Blueprint:**  
Before calling ConvertTimeToUtc, check CairoTimeZone.IsInvalidTime(localMidnight). If true, adjust forward by the DST gap (e.g. 1 hour to 01:00:00) or calculate the UTC instant using TimeZoneInfo.GetUtcOffset with gap tolerance.

**Required Verification & Regression Tests:**
- Call BusinessClock.UtcRangeForBusinessDate with DateOnly(2024, 4, 26) and DateOnly(2025, 4, 25); assert method returns valid UTC range without throwing System.ArgumentException.
- Call BusinessClock.UtcRangeForBusinessDate with the preceding Thursday; assert method returns valid UTC range without throwing ArgumentException.

**Acceptance Criteria:**
- BusinessClock.UtcRangeForBusinessDate handles DST spring-forward boundary dates without throwing System.ArgumentException and maintains continuous UTC coverage.

---

### Spec: FIN-034 — Payroll Advance Deduction Missing from Employee Ledger Posting and Ledger Schema
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs), [`src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs), [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs)

**Root Cause Diagnosis:**  
EmployeeLedgerEntryEntity lacks an AdvanceDeduction column and PayPayrollHandler only posts net settlement debit without recording the advance deduction component into the employee ledger.

**Operational Impact:**  
Outstanding employee advance balance is never credited or reduced in the ledger when payroll is disbursed; ledger reports continue to show the advance as unpaid, leading to double deductions or accounting discrepancies.

**Engineering Implementation Blueprint:**  
Add an AdvanceDeductionPiastres column to EmployeeLedgerEntryEntity and update PayPayrollHandler to record the advance deduction amount on the posted settlement entry or post a distinct advance recovery entry.

**Required Verification & Regression Tests:**
- Execute payroll run with an employee having advance deduction; execute PayPayrollHandler; assert EmployeeLedgerEntry records advance deduction and reduces outstanding advance.

**Acceptance Criteria:**
- Payroll settlement postings in EmployeeLedger record both net payable payout and advance deduction recovery.

---

### Spec: FIN-CAND-CLOSEDAY-ADMISSION-001 — Previously admitted treasury deposit can post after authoritative business-day close
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs), [`src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs), [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs), [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs), [`src/Falah.Infrastructure/Treasury/DepositHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/DepositHandler.cs)

**Root Cause Diagnosis:**  
Financial write admission is validated outside its monetary transaction and no shared day-state lock, revision predicate, or revalidation is visible in the selected path. Close snapshot and blocker reads therefore do not source-demonstrably drain operations that already passed the gate.

**Operational Impact:**  
Predicted post-snapshot movement/journal attached to a now-closed day, potentially diverging from its authoritative close-time snapshot if post-close activity is forbidden. This is not an observed SQL interleaving or loss.

**Engineering Implementation Blueprint:**  
Confirm whether financial writes are forbidden after close. If yes, coordinate day admission in the database: acquire a transaction-held lock/revision on the business-day row and revalidate Open within each financial write transaction; close takes the same lock before blocker evaluation/snapshot and waits for already-admitted writes. Define cross-process semantics, retry policy, and consistent lock order. If late postings are allowed, define how frozen snapshot reconciliation and subsequent deltas are represented instead of silently treating snapshot as current ledger.

**Required Verification & Regression Tests:**
- SQL-backed barrier test pauses deposit after initial lookup; close wins; resumed deposit must reject with zero journal/movement.
- Reverse schedule: admitted deposit commits first and close waits/includes it in snapshot.
- Cover other financial commands using the same admission contract plus reopen/new-day and idempotent replay.
- Run controlled multi-process concurrency test under the supported provider/isolation level; verify no stale admission slips.

**Acceptance Criteria:**
- A documented policy distinguishes immutable close-time snapshot from permitted subsequent postings.
- Under forbid-after-close policy, every monetary write serializes/revalidates against close inside the database transaction; close snapshot covers all writes admitted before close.
- Concurrency tests prove both orderings and absence of partial or post-close financial effects; lock order avoids deadlocks.

---

### Spec: FIN-CAND-EMPLOYEE-PAYROLL-OVERLAP-001 — Standalone employee settlements remain payable through frozen period payroll
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.Pay.cs), [`src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs), [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs), [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs), [`tests/Falah.Server.IntegrationTests/PayEmployeeVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PayEmployeeVerticalSliceTests.cs), [`tests/Falah.Server.IntegrationTests/PayrollVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/PayrollVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
PayrollRules computes payroll from earning categories and named deductions but ignores standalone Settlement debits. PayEmployee reduces current balance, while CreatePayroll freezes period net and PayPayroll pays that frozen amount without revalidating outstanding employee balance.

**Operational Impact:**  
If standalone Settlement is a prepayment of earnings already included in the same frozen payroll, paying both paths can disburse the same obligation twice; source flow shows this mechanism but does not establish the accounting policy or an actual duplicate payout.

**Engineering Implementation Blueprint:**  
Owner decision first. If settlements prepay same-period earnings, derive remaining liability from an explicit period/earning obligation and apply settlements both before payroll calculation and after run freeze; if the two paths are mutually exclusive, enforce atomic exclusion on both. Preserve frozen-run, replay, audit, source-balance, and historical-record behavior.

**Required Verification & Regression Tests:**
- Create earnings, settle same obligation standalone, then create/approve/pay payroll; total disbursement must satisfy owner-approved invariant.
- Create/approve payroll, apply standalone settlement, then pay frozen payroll; same invariant.
- Unrelated-period settlement must not be netted.
- Repeat and concurrent requests with distinct IDs; validate exactly-once employee ledger and treasury effects in approved disposable SQL Server integration DB.

**Acceptance Criteria:**
- Written owner policy defines relationship between standalone Settlement and period payroll obligation.
- Tests implement that policy across both sequence orders, period boundaries, retries and concurrent requests.
- No duplicate obligation disbursement if policy treats Settlement as prepayment; otherwise distinct obligation is explicitly represented and reported.

---

### Spec: FIN-CAND-PAYROLL-001 — Concurrent pay/cancel can overwrite payroll terminal status
- **Severity:** `HIGH` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs), [`src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs), [`src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs)

**Root Cause Diagnosis:**  
Pay and cancel authorize from independent Approved snapshots; no shared run-level transaction lock, compare-and-swap state predicate, or concurrency token is visible. Payment protects source-account balance, not PayrollRun state.

**Operational Impact:**  
Predicted inconsistent final state: a payroll can be financially paid while status becomes Cancelled, or payment can proceed after cancellation. Downstream reporting/recreation decisions become unreliable. Do not claim duplicate payment reproduced; same-run payment idempotency is counterevidence to duplicate pay on replay.

**Engineering Implementation Blueprint:**  
Use one database-coordinated PayrollRun state transition primitive for pay/cancel (transaction-held run-row lock or optimistic concurrency token/conditional Approved-to-claimed update). Payment must acquire/claim Approved within its transaction before movement/ledger posting; cancellation must perform mutually exclusive conditional transition. A losing operation must produce no movement, journal, settlement, paid-line, or status side effects. Preserve idempotency, filtered live-period index, and consistent treasury/run lock ordering.

**Required Verification & Regression Tests:**
- Barrier: pay reads Approved, cancel commits first; payment claim must fail and leave zero financial rows/line effects.
- Barrier: cancellation reads Approved, payment claims/commits first; cancellation must fail and Paid status/effects remain consistent.
- Repeat same idempotency key and concurrent duplicate pay; only one movement and one settlement per line.
- Exercise cancellation and replacement-run creation against filtered live-period unique index.
- Assert state and all side effects atomically under production provider and supported isolation.

**Acceptance Criteria:**
- At most one of pay/cancel succeeds for a given Approved version.
- Paid state is committed atomically with movement, journal, line settlements and audit; Cancelled state cannot coexist with those effects from the losing race.
- Lost race yields explicit conflict/reload response and no partial writes.
- Existing retry idempotency and one-live-run policy remain intact.

---

### Spec: FLOW-001 — Requested two-stage pickup preparation and final payment lifecycle is absent
- **Severity:** `HIGH_REQUIREMENT_GAP` | **Category:** `ORDER_LIFECYCLE_AND_WORKFLOW` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Application/Orders/CheckoutOrderRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/CheckoutOrderRequest.cs), [`src/Falah.Application/Orders/OrderDetailsDto.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/OrderDetailsDto.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Creation, stock consumption and payment checkout share one use case (CheckoutOrderHandler); only Delivery Cash has explicit deferred cash rule. No state machine exists for pay-on-collection pickup orders.

**Operational Impact:**  
Operational gap: Kitchen cannot be notified to prepare pickup orders before payment has been taken in full, blocking customer phone/advance pickup workflows.

**Engineering Implementation Blueprint:**  
Design separate pickup fulfilment state and payment state with server-authoritative transitions (two-stage reserve-and-prep -> pay-and-handover).

**Required Verification & Regression Tests:**
- Recommend lifecycle acceptance tests: unpaid reservation queues kitchen once, payment later posts once, handover only after policy gate, cancellation/restock/close blockers, network replay at each stage.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: INV-PERF-001 — In-Memory Full Table Pull on StockMovements in ListInventoryItemsAsync
- **Severity:** `MEDIUM` | **Category:** `INVENTORY_AND_COSTING` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs)

**Root Cause Diagnosis:**  
Full table read into RAM instead of server-side SQL GROUP BY aggregation.

**Operational Impact:**  
Severe memory spikes, thread blocking, and database timeouts as stock movements accumulate.

**Engineering Implementation Blueprint:**  
Perform GROUP BY and conditional SUM at the database level via EF Core / SQL query.

**Required Verification & Regression Tests:**
- Inspect SQL generated by ListInventoryItemsAsync; assert GROUP BY InventoryItemId is executed on SQL Server.

**Acceptance Criteria:**
- Stock balance calculation aggregates in SQL database engine rather than loading full table into memory.

---

### Spec: INV-SYNC-001 — Inventory Deduction / Order Refund Restock Synchronization Gap
- **Severity:** `MEDIUM` | **Category:** `INVENTORY_AND_COSTING` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs), [`src/Falah.Infrastructure/Orders/InventoryDeductionService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/InventoryDeductionService.cs)

**Root Cause Diagnosis:**  
RefundOrderHandler does not interact with InventoryDeductionService or emit Return/Restock movements when an order is cancelled or refunded.

**Operational Impact:**  
Stock balances diverge permanently from physical inventory whenever items are refunded or returned.

**Engineering Implementation Blueprint:**  
Introduce restock movement option in RefundOrderHandler to credit inventory upon refund of sellable items.

**Required Verification & Regression Tests:**
- Refund order with sellable items; assert Return StockMovements are recorded with Direction = In.

**Acceptance Criteria:**
- Refund workflow optionally synchronizes returned physical inventory back into stock balances.

---

### Spec: PRT-001 — Two machines can submit the same pending kitchen job
- **Severity:** `HIGH` | **Category:** `PRINTING_AND_HARDWARE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs), [`src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs), [`src/Falah.PrintAgent/PrintJobProcessor.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs), [`src/Falah.PrintAgent/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs), [`src/Falah.PrintAgent/UnreportedAttemptStore.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/UnreportedAttemptStore.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
CreatePrintJobsHandler.cs generates job ID using request.RequestId, and PrintJobs table in FalahDbContext lacks a composite unique constraint on (OrderId, Route).

**Operational Impact:**  
Duplicate kitchen and receipt tickets printed when multiple stations submit print requests for the same order.

**Engineering Implementation Blueprint:**  
Add composite unique index on (OrderId, Route, DocumentKind) in PrintJobEntity configuration, or ensure idempotent deduplication at database level.

**Required Verification & Regression Tests:**
- Existing PrintAgentTests cover route selection and one-agent report retry only; add concurrent two-agent claim/fencing test, and replacement-agent unknown-outcome recovery.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: PRT-005 — Printer verification tool can delete preexisting restaurant tickets
- **Severity:** `HIGH` | **Category:** `PRINTING_AND_HARDWARE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.PrintAgent/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs), [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs), [`tools/verify-windows-printer-transport.ps1`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tools/verify-windows-printer-transport.ps1)

**Root Cause Diagnosis:**  
Generic document-name matching replaces exact submission ownership; ignored pause result invalidates no-paper guarantee.

**Operational Impact:**  
Tool pauses printer without checking success, sends generic Falah ticket, then removes every job with DocumentName Falah ticket and resumes queue.

**Engineering Implementation Blueprint:**  
Use dedicated test queue and exact returned job ID, verify pause state, never delete by document name; keep all live printer operations gated.

**Required Verification & Regression Tests:**
- Add fake queue fixture preserving preexisting IDs, failure-to-pause abort, failed-cleanup no-resume policy. Only isolated queue integration after approval.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: SEC-002 — PIN-bearing user-management requests are durably stored as unencrypted JSON
- **Severity:** `MEDIUM` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Application/Users/UserManagementRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Users/UserManagementRequests.cs), [`src/Falah.Client.CashierWinUI/App.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/App.xaml.cs), [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs), [`src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs), [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs)

**Root Cause Diagnosis:**  
FileRetainedRequestStore.cs in CashierWinUI and CallCenterWinUI writes raw in-flight request payloads to %LOCALAPPDATA%\Falah\pending-operations\{station}\{ownerKey}.json as unencrypted plaintext JSON. User creation, PIN changes, and manager overrides that run while offline or mid-dispatch persist plaintext PINs on disk.

**Operational Impact:**  
Workstation credential exposure. Anyone with read access to the local machine's user directory (%LOCALAPPDATA%) can read persistent and in-flight employee and manager PINs in plaintext.

**Engineering Implementation Blueprint:**  
Encrypt retained request payloads using Windows Data Protection API (ProtectedData.Protect with CurrentUser scope) prior to file write. Mask or redact PIN properties from retained request diagnostic envelopes.

**Required Verification & Regression Tests:**
- Execute CreateUser or PIN change request in CashierWinUI while offline; inspect persisted JSON in %LOCALAPPDATA%; assert payload is ciphertext and PIN is unreadable in plaintext.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: SEC-003 — Authenticated employee reads expose complete sensitive profiles without employee permission or self-access restriction
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs), [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs), [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
GET /api/employees and GET /api/employees/{employeeId} in Program.cs (lines 685-687) have no permission policy attribute. EmployeeQueryService.cs (lines 81-85) unconditionally projects all database columns (NationalId, DailyWagePiastres, MonthlySalaryPiastres, OvertimeRatePiastres, FingerprintId, Address, Phone) into EmployeeDto.

**Operational Impact:**  
Internal data exposure. Any authenticated terminal session (including frontline cashiers, waiters, delivery drivers) can query the exact salaries, overtime rates, national IDs, and home addresses of all restaurant staff.

**Engineering Implementation Blueprint:**  
Introduce PublicEmployeeSummaryDto for operational cashier selection. Restrict GET /api/employees full profile to callers possessing 'employees_manage' or 'payroll_view' permissions.

**Required Verification & Regression Tests:**
- Query GET /api/employees with cashier bearer token; assert response contains only public summary fields and omits salary/national ID.
- Query GET /api/employees with manager bearer token; assert full profile is returned.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: SEC-006 — Manager Approval Tokens Lack Expiration (TTL) Check
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs), [`src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Approvals/IssueManagerApprovalHandler.cs)

**Root Cause Diagnosis:**  
ValidateApproval has no TTL check on ApprovedAtUtc. A manager approval issued for an aborted action can be stockpiled and replayed days or weeks later.

**Operational Impact:**  
Delayed unauthorized refunds, expense withdrawals, or comped orders executed outside real-time managerial oversight.

**Engineering Implementation Blueprint:**  
Pass DateTimeOffset nowUtc into AuthorizationPolicy.ValidateApproval and reject approvals older than a configurable TTL (e.g., 15 minutes).

**Required Verification & Regression Tests:**
- Validate approval issued 16 minutes prior; assert DomainException('انتهت صلاحية اعتماد المدير').
- Validate approval within 15 minutes; assert success.

**Acceptance Criteria:**
- Manager approvals expire after 15 minutes if unconsumed.

---

### Spec: SEC-009 — PIN Uniqueness Check-Then-Act Race Condition
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Users/PinUniquenessChecker.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/PinUniquenessChecker.cs), [`src/Falah.Infrastructure/Users/CreateUserHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/CreateUserHandler.cs)

**Root Cause Diagnosis:**  
PIN uniqueness check is performed in application memory across salted PBKDF2 hashes without an atomic database isolation lock.

**Operational Impact:**  
Duplicate PINs across active accounts, leading to non-deterministic login resolution where LoginByPin authenticates the first matching account.

**Engineering Implementation Blueprint:**  
Wrap user creation and PIN modification inside a transaction with serializable isolation or table lock, or maintain a deterministic blind-index hash.

**Required Verification & Regression Tests:**
- Execute concurrent CreateUser requests with identical PINs; assert second request fails with DomainException('رمز PIN مستخدم بالفعل').

**Acceptance Criteria:**
- Concurrent user creation or PIN modification cannot assign colliding PINs.

---

### Spec: SEC-011 — User Reactivation Missing PIN Uniqueness Verification
- **Severity:** `HIGH` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Infrastructure/Users/SetUserActiveHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs)

**Root Cause Diagnosis:**  
SetUserActiveHandler reactivates account without checking if candidate PIN is currently in use by another active account.

**Operational Impact:**  
Duplicate active PINs; operator authentication collision.

**Engineering Implementation Blueprint:**  
In SetUserActiveHandler, if request.Active is true, verify PIN uniqueness or require PIN reset upon reactivation.

**Required Verification & Regression Tests:**
- Deactivate user A; create user B with user A's PIN; reactivate user A; assert reactivation requires PIN reset or fails with collision error.

**Acceptance Criteria:**
- Account reactivation validates that stored credentials do not collide with active accounts.

---

### Spec: UI-001 — Primary cashier and treasury money requests lose original retry identity on process loss
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs), [`src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs), [`src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs), [`src/Falah.Client.CashierWinUI/Services/AppSession.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/AppSession.cs), [`src/Falah.Client.CashierWinUI/App.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/App.xaml.cs), [`tests/Falah.Client.Tests/CloseoutRegressionTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Client.Tests/CloseoutRegressionTests.cs), [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs)

**Root Cause Diagnosis:**  
Normal close/edit gates and same-process fixed keys cannot preserve immutable submitted DTO/key across forced termination. Primary financial partials bypass existing durable helper.

**Operational Impact:**  
No reliable station-owned recovery question for original checkout/refund/expense/treasury action; manually repeating with new key can create duplicate real operations. Actual duplicate ledger writes NOT tested.

**Engineering Implementation Blueprint:**  
Persist exact versioned typed financial request and owner before dispatch; expose recovery by original identity, block replacement until outcome known; no fixes performed.

**Required Verification & Regression Tests:**
- Simulate committed-but-lost-response with fake transport, reconstruct actual cashier VM/session with store, assert exact request ID/payload recovered before any new action.
- Repeat for refund, expense, safe drop, drawer transfer, shift open/close and treasury operations; assert server ledger unaffected on replay in isolated integration fixture.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-003 — Requested pickup kitchen-before-payment stage is absent from active client workflow
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs), [`src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs)

**Root Cause Diagnosis:**  
Pickup is a detail variant of common checkout, not separate persisted preparation/awaiting-payment state with stage-aware primary command.

**Operational Impact:**  
Client cannot perform owner-requested Stage A kitchen ticket then later Stage B checkout on same persisted unpaid pickup. Server/Domain end-to-end corroboration required before claiming global architecture absence.

**Engineering Implementation Blueprint:**  
Design explicit persisted pickup preparation/payment lifecycle using existing backend/domain capabilities where possible; implementation requires separate authorization.

**Required Verification & Regression Tests:**
- Create pickup preparation without payment in isolated backend test; stable ID and no payment movement; initial kitchen exactly once.
- Resume same order, collect final money, receipt once; operational pending list hides terminal paid/completed while history remains.
- Concurrent stage clicks and ambiguous print/payment responses retain stable identities.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-005 — Recovered electronic confirmation/cancellation has no reachable cashier retry context
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs), [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml)

**Root Cause Diagnosis:**  
Retained DTO loads but transient LastCheckoutResult does not; all retry reachability assumes that result. Main session sees unresolved op and blocks lock/navigation/normal close.

**Operational Impact:**  
Cashier can become frozen after restart even though retained original order ID exists. Correct production reconciliation must not simply delete journal. Potential cross-user context confusion because keys are station-global.

**Engineering Implementation Blueprint:**  
Recover operation order context from persisted request or present a standalone pending-action recovery panel independent of transient checkout result.

**Required Verification & Regression Tests:**
- Seed fake store with actual ConfirmElectronicPaymentRequest/CancelPendingElectronicPaymentRequest, create fresh AppSession/current user/Cashier VM, assert reachable retry command and UI visible; retry exact order identity.
- Confirm no new checkout, result discard or owner change until original outcome resolved; test another operator sees appropriate recovery ownership.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-007 — Late CallCenter customer lookup can overwrite a different phone/new order
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs), [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs)

**Root Cause Diagnosis:**  
No requested-phone equality/epoch check after await; async lookup completion mutates latest mutable form regardless of source identity.

**Operational Impact:**  
Wrong customer name/address offered under another phone; operator may dispatch wrong destination and upsert mismatched customer data. No real PII or DB writes used to reproduce.

**Engineering Implementation Blueprint:**  
Snapshot normalized phone and monotonic form generation; discard stale responses/cancel on change/reset; clear incompatible resolved fields safely.

**Required Verification & Regression Tests:**
- Deterministic fake HTTP delayed A/B in reverse completion order; assert final name/addresses match B.
- Begin lookup then ClearOrder, release response; new form stays blank.
- Lookup failure 404 vs timeout must show correct status; changing phone should invalidate old customer/address selections.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-012 — CallCenter customer-copy reprint requests kitchen preparation routes again
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Application/Printing/PrintingRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Printing/PrintingRequests.cs), [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs)

**Root Cause Diagnosis:**  
CallCenter omitted explicit false flag added to corresponding cashier reprint path.

**Operational Impact:**  
Potential duplicate charcoal/grill preparation work and food waste, in addition to requested customer copy. Backend route suppression cross-checked by printing peer as parent reports; parent should attach final handler evidence.

**Engineering Implementation Blueprint:**  
Send false for customer-only reprint; expose explicit separately confirmed kitchen reprint only if requirement exists.

**Required Verification & Regression Tests:**
- CallCenter reprint fake API asserts IncludeKitchenRoutes false.
- Isolated print-job handler fixture with grill/charcoal items asserts customer copy only for follow-up; explicit kitchen reprint separate labeled action.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-014 — Successful restore is reported incomplete because UI expects Completed instead of Succeeded
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs), [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs)

**Root Cause Diagnosis:**  
UI compares result status to wrong literal Completed; server stores enum Succeeded.

**Operational Impact:**  
UI tells operator restore did not complete after database already replaced, leaves restore target shown and can invite repeating dangerous operation. No restore executed by reviewer.

**Engineering Implementation Blueprint:**  
Use shared enum/contract success vocabulary and avoid treating generic HTTP success as outcome proof; maintain strict restore controls.

**Required Verification & Regression Tests:**
- Fake API returns successful DTO Status Succeeded; assert success note, target closed and no false RestoreError.
- Blocked/Failed records must remain reported non-success; no production DB restore needed for client contract test.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-016 — Management shift actions abandon unknown outcomes and generate a new request identity per retry
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs)

**Root Cause Diagnosis:**  
Separate management presentation did not adopt cashier immutable pending/outcomeUnknown protocol; only own busy booleans, no session registration or retained key.

**Operational Impact:**  
Cannot safely reconcile original shift operation; new attempt may be rejected by single-open/already-closed server guards or mislead state, and user can abandon in-flight money context. Actual duplicate server shift transaction not claimed.

**Engineering Implementation Blueprint:**  
Share durable operation coordination between both shift presentations and register global pending before dispatch.

**Required Verification & Regression Tests:**
- Fake commit-lost-response, retry asserts identical typed DTO/key and immutable counted/opening cash.
- Root TryClose/Lock/Shell.Navigate blocked while management open/close busy or unresolved.
- First definite rejection permits correction/new attempt, unknown requires same attempt reconciliation.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-017 — Recovered payroll transition or payment cannot reach its retry command
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs)

**Root Cause Diagnosis:**  
Durable DTO recovery is not accompanied by run selection/pay-dialog reconstruction; all routes to restore context blocked by same pending state.

**Operational Impact:**  
Retained financial operation stranded; ordinary page cannot reconcile original status/payment. Unlike create-run retained branch, no standalone retry in this VM.

**Engineering Implementation Blueprint:**  
Recover run/visible action from stored request or expose generic pending-action verification independent of transient selection.

**Required Verification & Regression Tests:**
- Fake retained transition/pay DTO, construct fresh VM/session, assert explicit retry available without selecting new run/account.
- Read original target by stored identity and show original intended action; exact DTO resends; no new payment until resolved.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-018 — Late recipe-detail response can replace another recipe selection before editing
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs)

**Root Cause Diagnosis:**  
No version/selected-ID validation around asynchronous recipe detail publishing.

**Operational Impact:**  
Wrong ingredient list/recipe identity can be edited and saved, affecting future inventory consumption/costing. Server authorizes a valid recipe update but cannot infer operator intended different selected row. Runtime write not tested.

**Engineering Implementation Blueprint:**  
Use monotonic detail generation and validate again after every await; freeze target identity across edit/save.

**Required Verification & Regression Tests:**
- Fake delayed A/B reverse completion asserts Detail.RecipeId equals SelectedRecipe key and StartEdit uses B.
- Close/change selection while reading must invalidate old response/finally flags.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-020 — PIN-change retry proof replaces shared bearer despite preserving visible user identity
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs), [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs)

**Root Cause Diagnosis:**  
LoginByPinAsync has bearer installation side effect, but proof caller assumes read-only credential test and does not reconcile session.CurrentUser.

**Operational Impact:**  
HTTP authenticated identity may diverge from UI/current actor; subsequent requests can fail or run under unintended principal depending server actor enforcement. Parent security reviewer owns final exploitability/severity validation; no real login performed.

**Engineering Implementation Blueprint:**  
Use isolated non-session-mutating proof client/endpoint and explicit owner context; coordinate with security report to avoid duplicate final finding.

**Required Verification & Regression Tests:**
- Fake login proof changes token identity tootheruser, assert root session and bearer remain original after failed proof.
- Recovered self-PIN request owned by another actor must be visibly routed/blocked; no session switch as side effect.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-021 — Late product-image completion changes save target beneath another product form
- **Severity:** `HIGH` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P1_HIGH_CORE_OPERATIONS`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs), [`src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml)

**Root Cause Diagnosis:**  
Image completion unconditionally overwrites shared editor identity captured before await. IsImageBusy only disables image controls; ReloadAsync reloads catalogs but does not reset/rebind visible form or editing identity.

**Operational Impact:**  
Save targets product A with B field values/variants. Can corrupt menu pricing, preparation route and recipe association despite valid authorized backend request. No product mutation executed by reviewer.

**Engineering Implementation Blueprint:**  
Keep immutable operation target independent from current editor; only update visible state when selection generation still matches. Coordinate browse/save with image busy and invalidate late completions on close/kind/new.

**Required Verification & Regression Tests:**
- Controlled image TaskCompletionSource: A upload pending, SelectRow(B), complete A; editor target and form must remain B, A image catalog updated separately.
- Repeat with delete, New, SetKind and Close/reopen; no stale completion alters current draft.
- Assert saved product ID matches displayed selected item after every async completion.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

## 3. P2 STABILITY AND MAINTAINABILITY (56 Specifications)

> [!NOTE]
> **STABILITY & CODE HEALTH SPRINT:** These 56 items address memory bloat, unindexed high-volume tables, background exception swallowing, UI control center race conditions, and precision rounding drift.


### Spec: FIN-007 — Backup filenames collide within a second and WITH FORMAT can overwrite prior backup
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Backup/CreateBackupHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/CreateBackupHandler.cs), [`src/Falah.Infrastructure/Backup/ScheduledBackupService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/ScheduledBackupService.cs)

**Root Cause Diagnosis:**  
Second-resolution timestamp treated as unique filename; no GUID/exclusive reservation. Retention reasons over records not unique physical paths.

**Operational Impact:**  
Two backup creations within timestamp resolution can target the same physical filename; later WITH FORMAT may overwrite a previously recorded backup or fail, while retention may count multiple records for one path.

**Engineering Implementation Blueprint:**  
Use collision-resistant immutable backup identity and exclusive create/no-overwrite destination semantics; preserve record-to-file identity. Retention should validate distinct canonical paths and never delete a file still referenced by a retained record.

**Required Verification & Regression Tests:**
- Parallel same-time filename generation returns distinct IDs/paths.
- Existing destination collision refuses overwrite.
- Retention with duplicate path references preserves all retained valid backups.
- Later isolated SQL/media test only with explicit approval.

**Acceptance Criteria:**
- Each successful backup record resolves to a unique immutable artifact.
- A collision cannot silently overwrite a prior artifact; retention cannot delete a retained target.

---

### Spec: FIN-009 — Concurrent different open-day requests create multiple active business days
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs), [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs), [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs)

**Root Cause Diagnosis:**  
Single-active invariant protected only by read check; no shared singleton lock or unique active-day representation.

**Operational Impact:**  
Concurrent requests with distinct open-day IDs can both pass read-before-write admission, leaving multiple active business days; queries/gates selecting first active day may attach new finance to inconsistent periods.

**Engineering Implementation Blueprint:**  
Represent single active ownership with a database-enforced singleton/unique active-state key acquired transactionally by open and reopen. Reconcile existing duplicate active rows explicitly before constraint migration; make read gate fail closed if invariant is corrupt.

**Required Verification & Regression Tests:**
- Concurrent opens with distinct IDs: exactly one active day; same request replay returns same day.
- Race reopen versus open; exactly one active day.
- Corrupt duplicate fixture makes gate fail closed and emits diagnosable error.

**Acceptance Criteria:**
- At most one active day exists across app processes.
- Open/reopen admission and all active-day readers use the same ownership invariant.

---

### Spec: FIN-010 — Collected sales report drops immediate money from pending mixed payments
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Domain/Reports/SalesRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs), [`tests/Falah.Domain.Tests/CheckoutRulesTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Domain.Tests/CheckoutRulesTests.cs)

**Root Cause Diagnosis:**  
Any pending electronic leg triggers subtraction of whole order total, instead of pending leg amount; report contradicts own cash channel totals.

**Operational Impact:**  
Sales report may subtract full order amount while an electronic leg is pending even though immediate cash/card leg was collected, understating collected sales/revenue and contradicting channel reconciliation.

**Engineering Implementation Blueprint:**  
Compute pending collection as the pending leg amount(s) and separately report gross/net sale recognition from tender settlement timing. Do not infer collection solely from order IsPaid or subtract whole order for partial pending tender.

**Required Verification & Regression Tests:**
- Mixed cash/electronic and card/electronic pending: collected amount equals immediate leg and pending equals deferred leg.
- After confirm, totals conserve order amount across channels.
- Compare report totals to treasury movements and payment query for each state.

**Acceptance Criteria:**
- For each order, collected plus pending amounts reconcile to eligible tender total; no collected leg disappears while another awaits confirmation.

---

### Spec: FIN-013 — Owner dashboard adds outgoing stock instead of subtracting it
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs)

**Root Cause Diagnosis:**  
OwnerDashboardQueryService.cs calculates inventory stock movements by summing QuantityMilliUnits without multiplying by -1 when Direction == Out.

**Operational Impact:**  
Inventory stock overstatement on owner dashboard. Waste and sales increase stock totals rather than decreasing them.

**Engineering Implementation Blueprint:**  
Apply proper sign negation: (m.Direction == StockMovementDirection.Out ? -m.QuantityMilliUnits : m.QuantityMilliUnits).

**Required Verification & Regression Tests:**
- Seed positive-magnitude In and Out movements plus count adjustments/waste; dashboard balance and low-stock set equal inventory overview.
- Cover zero/negative normalized inputs and multiple movement kinds.

**Acceptance Criteria:**
- Dashboard stock projection equals canonical inventory balance for every item.
- Out never increases available quantity; count corrections are treated per shared domain rule.

---

### Spec: FIN-017 — Financial approval provenance can retain unvalidated shift presenter or discard verified driver variance approver
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Shifts/CloseShiftRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Shifts/CloseShiftRequest.cs), [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs), [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs), [`src/Falah.Domain/Shifts/Shift.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Shifts/Shift.cs), [`src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs), [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs), [`tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
Financial attribution derives from presentation or earlier base gate rather than the actual verified relevant authorization result. Original shift evidence and related driver variance variant are preserved separately.

**Operational Impact:**  
False attribution to a person who did not issue approval; no money change or otherwise forbidden operation authorized by this defect alone.

**Engineering Implementation Blueprint:**  
Carry verified gate result, actor, and actual approver identity separately to all financial rows/audit; preserve OR authorization policy and approval reason semantics.

**Required Verification & Regression Tests:**
- Direct-permission below-threshold unnecessary presenter; above-threshold approved variance; verify actor/approver on movement, close event, settlement and audit.
- Wrong action/consumed approval and permission-only actor controls.

**Acceptance Criteria:**
- Persisted approver attribution is the verified approver for the actual gate; actor and approver are distinct fields where required.
- Below-threshold optional presentation cannot be misattributed; above-threshold approval still enforces approval action, reason, and amount.

---

### Spec: FIN-018 — Negative receipt quantity and unit cost can post stock reduction with supplier debt
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Purchasing/PurchasingRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Purchasing/PurchasingRequests.cs), [`src/Falah.Domain/Inventory/PurchasingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PurchasingRules.cs), [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs), [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Aggregate payment validation substituted for per-line positive receipt quantity/nonnegative purchase cost validation.

**Operational Impact:**  
Incorrect stock reduction labelled receipt and positive supplier debt for invalid purchase; unauthorized privilege escalation not claimed.

**Engineering Implementation Blueprint:**  
Validate direction-specific positive receipt quantities and nonnegative costs before posting. If returns are required, implement a separate explicit return/credit-note flow rather than encoding them as receipt negatives.

**Required Verification & Regression Tests:**
- Negative qty with positive unit cost; positive qty with negative cost; both-negative combination; assert no stock/debt mutation.
- Positive receipt and distinct authorized return flow.

**Acceptance Criteria:**
- Receipt rejects unsupported negative quantity/unit-cost combinations before posting stock or supplier debt.
- Valid positive receipts and any approved return/credit workflow remain coherent and atomic.

---

### Spec: FIN-020 — First stock receipt can race unit-change admission and reinterpret movement history
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Inventory/InventoryItemRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Inventory/InventoryItemRequests.cs), [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs), [`src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs), [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs), [`src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Immutable-unit predicate is evaluated before write without coordinating the first stock-movement admission. InventoryItem mapping244 is primary key only, no concurrency token; movement FK protects existence, not unit interpretation.

**Operational Impact:**  
Historical bare milliunit movement is reinterpreted under UnitB, contrary explicit InventoryRules40-47 no-unit-snapshot invariant. Current stock display joins current Unit, and valuation/recipe conversions can become semantically inconsistent; no measured monetary loss or actual SQL schedule executed.

**Engineering Implementation Blueprint:**  
Coordinate unit edits and first movement admission through a shared DB transaction lock/revision invariant used by both edit and every movement writer. Version only works if both paths participate.

**Required Verification & Regression Tests:**
- Barrier first movement admission vs unit change; ensure at most one incompatible operation commits.
- Sequential post-first-movement unit change rejection and legitimate same-unit metadata update.

**Acceptance Criteria:**
- A unit change and first movement cannot both pass against same pre-movement state; one is rejected or serialized.
- All readers continue to interpret historical movements using stable unit semantics.

---

### Spec: FIN-021 — Accepted numeric Inclusive tax mode is persisted raw and consumed as Exclusive
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Pricing/TaxRuleRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Pricing/TaxRuleRequests.cs), [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs), [`src/Falah.Domain/Financial/TaxRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TaxRule.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Validation discards parsed TaxMode and persists noncanonical raw string; three amount consumers silently map every nonliteralInclusive value toExclusive, including accepted definedInclusive numeric0.

**Operational Impact:**  
Future selected enabled tax computation can add tax to a base intended inclusive under its accepted enum interpretation. Legitimate settings authority and noncanonical input required; no actual charge or SQL acceptance reproduced.

**Engineering Implementation Blueprint:**  
Persist parsed TaxMode canonical enum value, reject undefined/unsupported forms, and make consumers use canonical parsed mode. Keep historical order tax snapshots frozen.

**Required Verification & Regression Tests:**
- Create/update tax mode canonical and numeric/invalid enum forms; compare checkout and estimate at nonzero rate.
- Ensure old orders frozen.

**Acceptance Criteria:**
- Accepted tax mode is parsed and persisted canonically; every consumer computes same intended inclusive/exclusive treatment.
- Historical frozen orders unchanged; new checkout and estimate agree under enabled nonzero rate.

---

### Spec: FIN-022 — Accepted case-insensitive compensation type is stored raw but wage/payroll consumers require canonical names
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs), [`src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs), [`src/Falah.Domain/Employees/Employee.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs), [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs), [`src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs), [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs), [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Shared compensation enum validation accepts multiple spellings but persistence retainsrawstring, while two financial consumers use exactname comparison instead of the parsedmeaning.

**Operational Impact:**  
Accepted case-variant compensation metadata can omit automatic daily wage credits or classify a monthly salary as credit-based; positive configured wage and noncanonical accepted input required. Shipped editor emits canonical names. No actual payout/loss reproduced.

**Engineering Implementation Blueprint:**  
Persist parsed CompensationType canonical value and use parsed enum in attendance/payroll consumers; validate defined supported types at API boundary.

**Required Verification & Regression Tests:**
- Case variants for Daily/Monthly/Hourly on attendance and payroll; malformed/undefined strings rejected.
- Canonical values and manual wage posting remain correct.

**Acceptance Criteria:**
- Every accepted compensation value is persisted in one canonical representation used consistently by attendance and payroll.
- Daily/monthly/Hourly cases calculate only intended credits; resaving canonical data does not change financial meaning.

---

### Spec: FIN-023 — Attendance checkout reconstructs check-in on checkout UTC date and can omit overtime
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Common/BusinessClock.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs), [`src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs), [`src/Falah.Infrastructure/Settings/SettingsQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Settings/SettingsQueryService.cs)

**Root Cause Diagnosis:**  
Loss of check-in UTC date, followed by reconstruction from checkout UTC date despite selecting attendance by Cairo date.

**Operational Impact:**  
Wrong hours and possible omission of otherwise eligible automatic overtime; no actual payment/loss or payroll record measured. Distinct from FIN012 correction-credit classification and FIN022 compensation enum interpretation.

**Engineering Implementation Blueprint:**  
Retain full check-in date/time and compute elapsed interval from that persisted event under documented timezone and repeated-punch policy; do not reconstruct from checkout date.

**Required Verification & Regression Tests:**
- Same UTC and cross-Cairo-date check-in/out examples; work hours/overtime against approved time zone rule.
- Repeated check-ins establish first/last punch rule.

**Acceptance Criteria:**
- Checkout time is compared against original check-in date/time policy, preserving timezone/day boundary; eligible overtime calculation matches approved rule.
- Repeated check-in/checkout policy is explicit; same-UTC-day positive control remains correct.

---

### Spec: FIN-024 — Manual daily-wage and overtime commands admit negative earning credits
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Employees/AttendanceRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/AttendanceRequests.cs), [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs), [`src/Falah.Domain/Employees/EmployeeRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/EmployeeRules.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs), [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs), [`src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Signed custom wage/rate is used as earning Credit without command-specific sign validation; positive finitehours do not ensure positive earning.

**Operational Impact:**  
An otherwise authorized accepted command can construct negative DailyWage/Overtime credit, reducing employee factbalance and categorized earnings instead of explicit deduction. No actual SQL write/payroll loss verified.

**Engineering Implementation Blueprint:**  
Reject negative command-specific wage/rate and overflow before posting; define zero policy. Do not globally ban signed Money or clamp negative earnings into zero.

**Required Verification & Regression Tests:**
- Negative custom daily rate/overtime rate and negative configured wage; overflow bounds; zero-policy control.
- Same-key retry and manual explicit deduction controls.

**Acceptance Criteria:**
- Negative earning rate/amount and unsupported overflow reject before ledger write; accepted zero behavior follows explicit policy.
- Manual deductions remain explicit and valid; valid wage/overtime/extra-shift replay remains idempotent.

---

### Spec: FIN-025 — Manual ledger accepts undefined numeric types excluded from named financial categories
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs), [`src/Falah.Domain/Employees/Employee.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs), [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs), [`src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Enum.TryParse result is canonicalized without Enum.IsDefined/supported-category admission; undefined numeric strings remain unnamed numeric types.

**Operational Impact:**  
Constructed ledger facts can affect all-entry balance but fall outside explicit report/payroll namecategories. This is unsupported data-shape acceptance, not a claim every legitimate named credit belongs in payroll. No API/SQLacceptance test.

**Engineering Implementation Blueprint:**  
Require Enum.IsDefined plus an explicit supported-type/credit-debit policy before manual ledger posting; preserve valid named types and classify legacy unsupported values explicitly.

**Required Verification & Regression Tests:**
- Defined numeric enum vs named control; unsupported numeric 999; explicit credit/debit category reporting.
- Balance, payroll and report reconciliation after accepted entry.

**Acceptance Criteria:**
- Only defined and explicitly supported ledger enum members/types can be posted through manual API.
- Unsupported numeric strings reject or are explicitly classified before balance/report/payroll effects; canonical named controls remain valid.

---

### Spec: FIN-026 — Distinct employee credit requests can persist and display stale running-balance snapshots
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml), [`src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs), [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs), [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs), [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs), [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs)

**Root Cause Diagnosis:**  
BalanceAfter snapshot computed from readsum before Save without cross-command employee serialization; own pendingDelta and same-key dedup do not cover different writers.

**Operational Impact:**  
Source schedule retains both positive credit facts but can display wrong statement runningbalance. Fact-derived GetBalance remains correct; protected payout locks are counterevidence. No actual SQLrace/UI observation/moneyoverdraft proved.

**Engineering Implementation Blueprint:**  
Either serialize all ledger appends on one employee anchor with consistent lock order, or compute statement running balance from ordered immutable facts and retire stale BalanceAfter as authority. Define deterministic timestamp tie-breaker.

**Required Verification & Regression Tests:**
- Barrier concurrent distinct positive credit appends with no rows and existing balance; verify facts and statement snapshots.
- Same-key retry, rollback/restart, same-command pending rows, payout lock ordering, equal timestamp tie.

**Acceptance Criteria:**
- Concurrent distinct credit appends preserve correct recomputed employee balance and statement running-balance order.
- Same-key replay, pending same-command rows, and locked payout remain correct; tie ordering is deterministic.

---

### Spec: FIN-027 — Frozen business-day financial snapshots omit the corresponding live-report permission gate
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs), [`src/Falah.Application/Reports/ReportingDtos.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/ReportingDtos.cs), [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs), [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs), [`src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml), [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs), [`src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs), [`src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Frozen report payload endpoint/client path uses session-only admission instead of live reports_view/all policy.

**Operational Impact:**  
Authenticated actor lacking report permission can receive financial report JSON from snapshot endpoints under the inspected source contract. Different from employee report or identity-binding issues; parent owns dedup/security disposition.

**Engineering Implementation Blueprint:**  
Apply reports_view to latest/history financial snapshot reads using verified authenticated actor; if broader archive access is intended, define a distinct explicit permission rather than reusing control-center permission.

**Required Verification & Regression Tests:**
- Disposable authenticated requests with and without reports_view for latest/history snapshot routes.
- Client control-center-only access sees no financial figures; permitted report viewer still accesses.

**Acceptance Criteria:**
- Every frozen financial snapshot read uses authenticated session actor plus reports_view (or a separately owner-approved explicit archive permission).
- Unauthorized session receives no financial snapshot payload; authorized reader can still access permitted history.

---

### Spec: FIN-028 — Spent manager approval prevents acknowledgement replay of committed overtime and extra-shift credits
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Employees/AttendanceRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/AttendanceRequests.cs), [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs), [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs), [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs), [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs), [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs), [`src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
RecordOvertime/RecordExtraShift validate one-use delegated approval before deterministic ledger receipt lookup. Successful first SaveChanges persists approval consumption; retry with same saved approval rejects before ledger dedupe/replay.

**Operational Impact:**  
Existing financial credit remains committed but client cannot confirm it with original request. Retained request remains pending after subsequent422, preventing normal completion of that submitted action. This is a recovery/acknowledgement failure, not proven duplicate payment or loss of financial facts.

**Engineering Implementation Blueprint:**  
After validating actor/session and binding the durable request to target and payload, resolve an existing committed receipt before consuming approval again. Keep fresh-operation approval and atomic persistence.

**Required Verification & Regression Tests:**
- Manager-approved overtime/extra shift success then exact retry in fresh context after response loss; verify no second approval spend and same receipt.
- Mismatched actor/employee/amount conflict; concurrent same-key SQL test only in approved disposable DB.

**Acceptance Criteria:**
- A committed approval-backed overtime/extra-shift request can be acknowledged on exact same actor/target/payload retry without consuming another approval.
- Mismatched same key conflicts; fresh operation still requires valid authorization; no duplicate ledger credit.

---

### Spec: FIN-029 — Owner dashboard stock projection ignores outbound movement direction
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs), [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs), [`src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs), [`src/Falah.Infrastructure/Orders/InventoryDeductionService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/InventoryDeductionService.cs), [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs), [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs), [`tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
OwnerDashboardQueryService.LoadStockAsync groups stock movements and sums QuantityMilliUnits without applying Direction. Stock movement writers store positive magnitudes with Direction separately; canonical inventory projections subtract Out movements.

**Operational Impact:**  
Dashboard stock balances can be overstated after consumption, waste, packaging usage, or outbound count adjustments. Low-stock rows/counts can be hidden. Example: In 5,000 plus Out 4,000 yields dashboard 9,000 while correct balance is 1,000.

**Engineering Implementation Blueprint:**  
Use shared direction-aware stock balance projection or apply Direction sign in dashboard query; keep inventory overview as parity oracle.

**Required Verification & Regression Tests:**
- Integration compare dashboard/inventory seeded In+Out and outbound-only; low-stock parity; positive in/out count adjustment.

**Acceptance Criteria:**
- For seeded In 5000 and Out 4000, dashboard and inventory overview both show 1000 and agree on low-stock status.
- Outbound-only and count-adjustment movements preserve direction-aware parity.

---

### Spec: FIN-030 — Arithmetic Overflow Exception in Money.Abs() on long.MinValue
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs)

**Root Cause Diagnosis:**  
Direct invocation of Math.Abs(Piastres) without handling long.MinValue boundary edge condition in Money.Abs().

**Operational Impact:**  
Unhandled System.OverflowException crashes execution if a long.MinValue piastres value is encountered.

**Engineering Implementation Blueprint:**  
Add guard in Money.Abs(): if (Piastres == long.MinValue) throw new DomainException("Cannot compute absolute value of minimum long piastres"); or clamp safely.

**Required Verification & Regression Tests:**
- Assert.Throws<DomainException>(() => Money.FromPiastres(long.MinValue).Abs()); or assert bounded behavior without unhandled OverflowException.

**Acceptance Criteria:**
- Money.Abs() handles or rejects long.MinValue safely without unhandled System.OverflowException.

---

### Spec: FIN-032 — Unvalidated Negative Delivery Service Fee Allows Artificially Reduced Order Payable Total
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Delivery/DeliveryServiceHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/DeliveryServiceHandlers.cs), [`src/Falah.Application/Delivery/DeliveryServiceRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Delivery/DeliveryServiceRequests.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs)

**Root Cause Diagnosis:**  
Delivery service creation and update endpoints lack validation asserting FeePiastres >= 0, and CheckoutRules does not guard against negative delivery fee amounts.

**Operational Impact:**  
Negative delivery service fees can be injected to artificially deflate order payable totals, leading to cash drawer shortages and corrupted financial journal entries.

**Engineering Implementation Blueprint:**  
Add validation in CreateDeliveryServiceHandler, UpdateDeliveryServiceHandler, and DeliveryServiceRequests validating FeePiastres >= 0; add domain assertion in CheckoutRules asserting deliveryFee.IsPositive || deliveryFee.IsZero.

**Required Verification & Regression Tests:**
- Submit CreateDeliveryServiceRequest with FeePiastres = -100; assert validation fails with DomainException or 400 Bad Request.
- Invoke CheckoutRules.ComputePayableTotal with negative delivery fee; assert DomainException is thrown.

**Acceptance Criteria:**
- Delivery service fees cannot be negative anywhere in the domain or persistence layer.

---

### Spec: FIN-035 — BusinessDayCloseEvaluator Performs Global Unbounded Table Scans Blocking Business Day Close on Historical Data
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs)

**Root Cause Diagnosis:**  
BusinessDayCloseEvaluator queries shifts, driver custodies, and financial journal entries without predicate scoping to the target business day or date.

**Operational Impact:**  
An unclosed shift or outstanding custody from an old date permanently blocks closing any subsequent business day; queries degrade as tables grow.

**Engineering Implementation Blueprint:**  
Add WHERE predicates restricting shifts and driver custodies to the specific business day being closed (e.g. s.BusinessDate == day.BusinessDate or s.BusinessDayId == day.Id), and scope journal checks to the relevant window.

**Required Verification & Regression Tests:**
- Seed an unclosed shift on date D-1 and an open shift on date D; close date D shift; call EvaluateAsync for date D; assert unclosed shift from D-1 does not block day close for date D.

**Acceptance Criteria:**
- BusinessDayCloseEvaluator only evaluates blockers relevant to the target business day and executes bounded queries.

---

### Spec: FIN-036 — PurchasingQueryService Performs Unbounded Full Table Scan on Supplier Ledger Entries in ListSuppliersAsync
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs)

**Root Cause Diagnosis:**  
ListSuppliersAsync fetches the entire SupplierLedgerEntries table into memory instead of computing supplier ledger balance aggregations via SQL GROUP BY in the database.

**Operational Impact:**  
Memory consumption scales linearly O(N) with the entire historical purchasing ledger; causes out-of-memory crashes and severe database contention as supplier ledger entries grow.

**Engineering Implementation Blueprint:**  
Replace the full-table memory load with an EF Core query that groups by SupplierId and aggregates Debit and Credit sums directly in SQL Server.

**Required Verification & Regression Tests:**
- Seed 10,000 SupplierLedgerEntries across 50 suppliers; verify ListSuppliersAsync generates a grouped SQL query and executes in bounded memory without loading all entity instances into memory.

**Acceptance Criteria:**
- ListSuppliersAsync does not load full ledger table into memory and derives supplier totals via database aggregation.

---

### Spec: FIN-037 — Multi-Payment Order Refund Inability in Cashier UI and Backend Domain Exception on Treasury Allocation
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs), [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs), [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs)

**Root Cause Diagnosis:**  
The Cashier UI hard-disables refunds for orders with PaymentMethod == "multi", and TreasuryMovementFactory lacks treasury allocation rules for multi-method refunds.

**Operational Impact:**  
Cashiers cannot process legitimate returns for customers who paid using mixed tenders (e.g. Cash + InstaPay); backend throws DomainException if invoked directly.

**Engineering Implementation Blueprint:**  
Obtain restaurant owner business decision on multi-payment refund settlement rules (e.g., refund to cash drawer vs original tender pro-rata). Update UI to allow refund initiation and extend RefundOrderHandler and TreasuryMovementFactory to allocate multi-payment refund legs.

**Required Verification & Regression Tests:**
- Submit refund on multi-payment order with designated tender refund legs; verify appropriate treasury accounts are debited/credited according to owner business policy.

**Acceptance Criteria:**
- Multi-payment orders can be refunded according to agreed owner business policies without throwing unhandled DomainException.

---

### Spec: FIN-CAND-BUSINESS-DAY-OPEN-RACE-001 — Distinct concurrent business-day opens can pass pre-insert active-state validation
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs), [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs), [`src/Falah.Infrastructure/Persistence/Migrations/20260903154545_AddBusinessDay.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260903154545_AddBusinessDay.cs), [`src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs)

**Root Cause Diagnosis:**  
Active-day admission is a read-check-insert invariant. OpenBusinessDayHandler reads current active rows and invokes BusinessDayRules.Open before a separate insert; there is no shared transaction/conditional state claim, while the schema has uniqueness only by request ID.

**Operational Impact:**  
More than one active business day may be persisted, making day attribution, financial close blockers/snapshots, and active-day lookups ambiguous. No actual duplicate day or accounting loss has been reproduced.

**Engineering Implementation Blueprint:**  
Confirm the invariant scope. If one active BusinessDay is intended, enforce it at the same persistence boundary as the single-open-shift invariant (provider-appropriate active-row uniqueness or a serialized admission row/lock covering Open and Reopen), and make losing admission return a stable conflict without partial side effects. Preserve same-request idempotency. Do not rely solely on a pre-insert read.

**Required Verification & Regression Tests:**
- Barrier-controlled concurrent opens with two distinct RequestIds: at most one active day commits; loser receives a conflict and no second row is created.
- Same RequestId replay returns the original day without a second row.
- Concurrent reopen of one day and open of another cannot create two active days if global invariant is confirmed.
- Sequential open while Open, Closing, or Reopened rejects; after valid close, a new day opens.
- Run against supported SQL Server provider and two independent DbContext/process instances; assert database-level invariant.

**Acceptance Criteria:**
- A documented owner policy establishes the active-day scope (global or per defined partition).
- If global single-active-day remains intended, every Open/Reopen transition is serialized or constrained by database-enforced uniqueness so concurrent instances cannot persist more than one active day.
- A losing concurrency attempt leaves no partial state and the existing same-request replay contract remains intact.

---

### Spec: FIN-CAND-CANCELLED-LOOKUP-PAYMENT-LABEL-001 — Call Center labels a cancelled unpaid electronic order as collectible on delivery
- **Severity:** `LOW` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs), [`src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml), [`src/Falah.Client.CallCenterWinUI/Converters/Converters.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Converters/Converters.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs), [`src/Falah.Infrastructure/Orders/OrderMapper.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderMapper.cs), [`src/Falah.Application/Orders/OrderDetailsDto.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/OrderDetailsDto.cs)

**Root Cause Diagnosis:**  
The payment display infers collect-on-delivery solely from IsPaid=false after special-casing only pending electronic status; it ignores order cancellation and cancelled electronic status.

**Operational Impact:**  
Conflicting operational message can misinform staff/customer or driver about collection due. Visible Cancelled status mitigates. No actual collection/ledger loss proven.

**Engineering Implementation Blueprint:**  
Make payment text state-aware: cancelled orders should show cancelled/no collection, pending transfers stay unconfirmed, settled orders stay paid, and collect-on-delivery is reserved for active unpaid orders that satisfy COD semantics. Confirm the preferred Arabic copy with owner if wording is material.

**Required Verification & Regression Tests:**
- Projection test for cancelled + unpaid + electronic status cancelled: no collect-at-delivery label.
- Controls for active COD, pending electronic, and paid orders.
- Call Center lookup view-model test asserts status/payment rows do not conflict.

**Acceptance Criteria:**
- No cancelled order is described as collectible at delivery.
- Active COD remains collectible; pending electronic remains unconfirmed; paid remains paid.
- No database/financial movement is introduced by the presentation correction.

---

### Spec: FIN-CAND-CASHCOUNT-001 — Unvalidated denomination and unchecked arithmetic can corrupt finalized count values
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs), [`src/Falah.Domain/Audit/AuditEvent.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Audit/AuditEvent.cs), [`src/Falah.Domain/Authorization/Actor.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/Actor.cs), [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs), [`src/Falah.Domain/Authorization/ManagerApproval.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ManagerApproval.cs), [`src/Falah.Domain/Authorization/PermissionCatalog.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PermissionCatalog.cs), [`src/Falah.Domain/Authorization/PinHasher.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PinHasher.cs), [`src/Falah.Domain/Authorization/SessionToken.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/SessionToken.cs), [`src/Falah.Domain/Authorization/UserManagementRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs), [`src/Falah.Domain/Backup/BackupRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Backup/BackupRules.cs), [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs), [`src/Falah.Domain/Common/BusinessClock.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs), [`src/Falah.Domain/Common/DomainException.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/DomainException.cs), [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs), [`src/Falah.Domain/ControlCenter/ControlCenterRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/ControlCenter/ControlCenterRules.cs), [`src/Falah.Domain/Customers/Customer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Customers/Customer.cs), [`src/Falah.Domain/Delivery/DeliveryRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRecords.cs), [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs), [`src/Falah.Domain/Delivery/DeliveryStatus.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryStatus.cs), [`src/Falah.Domain/Employees/Attendance.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Attendance.cs), [`src/Falah.Domain/Employees/Employee.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs), [`src/Falah.Domain/Employees/EmployeeRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/EmployeeRules.cs), [`src/Falah.Domain/Financial/FinancialRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/FinancialRules.cs), [`src/Falah.Domain/Financial/PaymentMethod.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/PaymentMethod.cs), [`src/Falah.Domain/Financial/TaxRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TaxRule.cs), [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs), [`src/Falah.Domain/Idempotency/FinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Idempotency/FinancialJournal.cs), [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs), [`src/Falah.Domain/Inventory/InventoryRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRecords.cs), [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs), [`src/Falah.Domain/Inventory/PrepRecipeLine.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PrepRecipeLine.cs), [`src/Falah.Domain/Inventory/PurchasingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PurchasingRules.cs), [`src/Falah.Domain/Menu/ProductImageRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Menu/ProductImageRules.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/ElectronicPayment.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/ElectronicPayment.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Domain/Orders/OrderEnums.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderEnums.cs), [`src/Falah.Domain/Orders/OrderItem.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderItem.cs), [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Domain/Orders/OrderRefund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderRefund.cs), [`src/Falah.Domain/Orders/OrderValueBreakdown.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderValueBreakdown.cs), [`src/Falah.Domain/Orders/RefundRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Domain/Pricing/ItemPricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs), [`src/Falah.Domain/Pricing/PriceRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PriceRule.cs), [`src/Falah.Domain/Pricing/PricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PricingRules.cs), [`src/Falah.Domain/Printing/EscPosTicketRenderer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/EscPosTicketRenderer.cs), [`src/Falah.Domain/Printing/PrintRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRecords.cs), [`src/Falah.Domain/Printing/PrintRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs), [`src/Falah.Domain/Printing/TicketLayout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/TicketLayout.cs), [`src/Falah.Domain/Purchasing/PayablesAging.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Purchasing/PayablesAging.cs), [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs), [`src/Falah.Domain/Reports/SalesRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs), [`src/Falah.Domain/Reports/TrendRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/TrendRules.cs), [`src/Falah.Domain/Shifts/Shift.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Shifts/Shift.cs), [`src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Arithmetic and domain validation are not bounded at the finalization boundary; persisted audit data records the derived result without enforcing the supported denomination set or representable totals.

**Operational Impact:**  
Could finalize and audit a mathematically incorrect cash total/difference. Existing evidence does not show direct treasury posting, shift settlement, or close-blocker override; do not characterize as cash loss.

**Engineering Implementation Blueprint:**  
Owner must first decide whether ExpectedPiastres is a client-supplied comparison target or must be derived from a server-authoritative snapshot. Then whitelist supported positive denominations and bound each quantity/aggregate; use checked multiply, sum, and difference and convert overflow/invalid rows to explicit domain rejection. Preserve agreed negative-quantity compatibility semantics or reject only by explicit policy.

**Required Verification & Regression Tests:**
- Domain tests for every allowed denomination, unsupported/zero/negative denomination, maximum quantity, multiplication overflow, aggregate overflow, and difference overflow.
- Authorized handler test proves invalid input creates neither count nor audit record; valid input persists exact values atomically.
- Endpoint test verifies invalid input maps to a deliberate client error.
- Verify whether finalized cash-count data participates in close reconciliation and that any such use is server-validated.
- Build/run arithmetic behavior under the actual net8 deployment configuration.

**Acceptance Criteria:**
- All accepted values fall within explicit denomination and monetary bounds; no wraparound reaches persisted count/audit.
- Rejected values produce no partial count/audit writes.
- ExpectedPiastres authority and negative quantity semantics are documented and covered.
- Any reconciliation consumer uses the validated result or independently recomputes the expected value.

---

### Spec: FIN-CAND-CLOSE-SNAPSHOT-REVISION-001 — Close snapshot revision and authoritative-row admission lack an explicit shared invariant
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs), [`src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs)

**Root Cause Diagnosis:**  
Read/status admission outside transaction plus Count+1/demotion lacks explicit perday uniqueness/state-token enforcement in inspected source.

**Operational Impact:**  
Source lacks explicit common day admission lock/state token and schema uniqueness for archival invariants; duplicate-current/same-revision outcome remains a qualified provider-dependent candidate, not reproduced corruption.

**Engineering Implementation Blueprint:**  
Do not implement a concurrency fix based on assumed SQL ordering. First test provider schedule; if admission gap manifests, use common day lock/state predicate plus explicit snapshot uniqueness/authority invariant.

**Required Verification & Regression Tests:**
- Barrier two distinct close IDs around count/demotion in disposable SQL Server; capture EF DML and locks.
- Verify authoritative row, revisions and day pointer across both serialization outcomes/history.

**Acceptance Criteria:**
- Disposable SQL Server controlled schedule establishes actual DML/lock outcome before code fix is chosen.
- After any accepted close ordering there is exactly one authoritative snapshot, coherent day pointer, and valid monotone unique revision; losing close leaves no partial mutation.

---

### Spec: FIN-CAND-CORRECTION-BASIS-001 — Attendance corrections revalue old effects at current rates instead of posted monetary basis
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs), [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs), [`src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs), [`src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs), [`src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs)

**Root Cause Diagnosis:**  
Correction reads current employee DailyWage/current Settings then recomputes BOTH old and new overtime/penalty. It never reads frozen actual original amount/rate or cumulative prior correction effect for delta. Row+ledger serialization and immutable retry audit are real controls, but do not restore old monetary basis.

**Operational Impact:**  
Potential over/under reconciliation relative to actual posted effect; authoritative correction valuation policy unresolved. No actual employee loss, runtime record, payout or SQL acceptance verified.

**Engineering Implementation Blueprint:**  
After owner decision, calculate correction delta against actual posted original facts and cumulative linked adjustments, append only; preserve idempotency, locks, and freeze rules. Do not mint historical credits automatically.

**Required Verification & Regression Tests:**
- Original fixed-rate overtime then settings changed then correction; assert owner-approved reversal basis.
- Change multiplier/grace/workday, penalty to zero, sequential corrections/return to original hours, replay and frozen/paid period controls.

**Acceptance Criteria:**
- Owner-approved correction basis is documented; adjustments reconcile original posted facts and prior corrections without accidental over/under reversal.
- Same-key retry and interleaved sequential corrections remain atomic, auditable, and consistent with frozen/paid period policy.

---

### Spec: FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001 — Delivery report field labeled expected unsettled custody aggregates collected amount instead
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs), [`src/Falah.Application/Reports/ReportingDtos.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/ReportingDtos.cs), [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs), [`src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs), [`src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs), [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs), [`src/Falah.Application/Reports/OwnerDashboardDtos.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/OwnerDashboardDtos.cs), [`src/Falah.Client.CashierWinUI/Views/ReportsView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/ReportsView.xaml), [`src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml), [`tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs), [`tests/Falah.Server.IntegrationTests/DailyStatementVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/DailyStatementVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
Delivery report field labeled expected unsettled custody aggregates collected amount instead

**Operational Impact:**  
If the DTO/UI contract means expected cash at driver risk, the report underreports that exposure while delivery is in transit and diverges from the owner dashboard. If it intentionally means cash already collected but not settled, the aggregation may be deliberate but DTO name and tests are misleading. No loss, duplicate payment, or ledger corruption is established.

**Engineering Implementation Blueprint:**  
Confirm report contract. Existing DTO name, owner dashboard, and test comments support ExpectedPiastres: if that is the intended meaning, sum c.ExpectedPiastres. If the intended metric is collected-but-unsettled, rename the DTO/display and retain/add a distinct expected-at-risk metric so the two values are not conflated.

**Required Verification & Regression Tests:**
- Delivery query test asserting ExpectedUnsettledCustody aggregates unremitted in-flight orders only.
- End-of-shift reconciliation test asserting remitted cash is excluded from courier custody balance.

**Acceptance Criteria:**
- DTO field name and source aggregate express the same amount.
- Owner dashboard and delivery report agree for the same non-settled custody cohort when they claim to show expected custody.
- In-transit and variance cases are covered by tests; no real printer or financial operation is needed.
- Historical statement temporal semantics are documented separately.
- Before delivery, delivery report and owner dashboard agree on expected cash responsibility for the same open custody cohort while collected remains zero.

---

### Spec: FIN-CAND-NEGATIVE-VARIANT-PRICE-001 — Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs), [`src/Falah.Application/Menu/MenuManagementRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Menu/MenuManagementRequests.cs), [`src/Falah.Infrastructure/Menu/ProductHandlers.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Menu/ProductHandlers.cs), [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs), [`src/Falah.Domain/Pricing/ItemPricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs), [`src/Falah.Infrastructure/Orders/MenuPricingService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/MenuPricingService.cs), [`src/Falah.Domain/Orders/OrderItem.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderItem.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Persistence/Migrations/20260902120327_InitialCreate.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260902120327_InitialCreate.cs), [`tests/Falah.Server.IntegrationTests/MenuManagementVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/MenuManagementVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed

**Operational Impact:**  
Potential undercharge or zero-payable order with stock consumption; actual financial incidence and owner policy are unverified.

**Engineering Implementation Blueprint:**  
Confirm whether zero-priced variants are allowed. If prices must be positive, centralize the invariant at server command boundaries and checkout resolution, and evaluate a DB check constraint after legacy-row inventory. Preserve signed Money for legitimate ledger deltas.

**Required Verification & Regression Tests:**
- Domain validation test asserting ProductVariant.PricePiastres >= 0 on create/update.
- Checkout integration test submitting item with negative price asserting rejection with HTTP 422.

**Acceptance Criteria:**
- Owner-approved variant price domain is enforced server-side and at checkout.
- Invalid variant configuration cannot create underpriced/free stock-consuming checkout.
- No financial side effect occurs on rejection.
- Valid positive variants and approved prices remain compatible.

---

### Spec: FIN-CAND-PACKAGING-COVERAGE-001 — Unknown once-per-order packaging cost does not invalidate aggregate profit completeness
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Audit/AuditEvent.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Audit/AuditEvent.cs), [`src/Falah.Domain/Authorization/Actor.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/Actor.cs), [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs), [`src/Falah.Domain/Authorization/ManagerApproval.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ManagerApproval.cs), [`src/Falah.Domain/Authorization/PermissionCatalog.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PermissionCatalog.cs), [`src/Falah.Domain/Authorization/PinHasher.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PinHasher.cs), [`src/Falah.Domain/Authorization/SessionToken.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/SessionToken.cs), [`src/Falah.Domain/Authorization/UserManagementRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs), [`src/Falah.Domain/Backup/BackupRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Backup/BackupRules.cs), [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs), [`src/Falah.Domain/Common/BusinessClock.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs), [`src/Falah.Domain/Common/DomainException.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/DomainException.cs), [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs), [`src/Falah.Domain/ControlCenter/ControlCenterRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/ControlCenter/ControlCenterRules.cs), [`src/Falah.Domain/Customers/Customer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Customers/Customer.cs), [`src/Falah.Domain/Delivery/DeliveryRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRecords.cs), [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs), [`src/Falah.Domain/Delivery/DeliveryStatus.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryStatus.cs), [`src/Falah.Domain/Employees/Attendance.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Attendance.cs), [`src/Falah.Domain/Employees/Employee.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs), [`src/Falah.Domain/Employees/EmployeeRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/EmployeeRules.cs), [`src/Falah.Domain/Financial/FinancialRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/FinancialRules.cs), [`src/Falah.Domain/Financial/PaymentMethod.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/PaymentMethod.cs), [`src/Falah.Domain/Financial/TaxRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TaxRule.cs), [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs), [`src/Falah.Domain/Idempotency/FinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Idempotency/FinancialJournal.cs), [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs), [`src/Falah.Domain/Inventory/InventoryRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRecords.cs), [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs), [`src/Falah.Domain/Inventory/PrepRecipeLine.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PrepRecipeLine.cs), [`src/Falah.Domain/Inventory/PurchasingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PurchasingRules.cs), [`src/Falah.Domain/Menu/ProductImageRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Menu/ProductImageRules.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/ElectronicPayment.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/ElectronicPayment.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Domain/Orders/OrderEnums.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderEnums.cs), [`src/Falah.Domain/Orders/OrderItem.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderItem.cs), [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Domain/Orders/OrderRefund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderRefund.cs), [`src/Falah.Domain/Orders/OrderValueBreakdown.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderValueBreakdown.cs), [`src/Falah.Domain/Orders/RefundRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Domain/Pricing/ItemPricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs), [`src/Falah.Domain/Pricing/PriceRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PriceRule.cs), [`src/Falah.Domain/Pricing/PricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PricingRules.cs), [`src/Falah.Domain/Printing/EscPosTicketRenderer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/EscPosTicketRenderer.cs), [`src/Falah.Domain/Printing/PrintRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRecords.cs), [`src/Falah.Domain/Printing/PrintRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs), [`src/Falah.Domain/Printing/TicketLayout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/TicketLayout.cs), [`src/Falah.Domain/Purchasing/PayablesAging.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Purchasing/PayablesAging.cs), [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs), [`src/Falah.Domain/Reports/SalesRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs), [`src/Falah.Domain/Reports/TrendRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/TrendRules.cs), [`src/Falah.Domain/Shifts/Shift.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Shifts/Shift.cs), [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs), [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs)

**Root Cause Diagnosis:**  
Completeness boolean uses dish-line counters while food-cost total also includes separately accumulated order-level packaging; packaging unknown state is not passed to profitability completeness calculation.

**Operational Impact:**  
Profitability result can be numerically computed and marked complete although packaging costs are excluded; reports can overstate gross/operating profit. This concerns report completeness, not ledger posting or realized cash loss.

**Engineering Implementation Blueprint:**  
Carry an explicit aggregate cost-coverage/completeness signal across dish and order-packaging components into profitability calculation and DTO, preserving distinct dish counts and per-order packaging cardinality. Keep missing names. Do not convert packaging unknowns into fabricated sold dish-line counts.

**Required Verification & Regression Tests:**
- Known dish plus unknown active packaging returns IsComplete=false and does not present the aggregate profit as fully costed.
- Known packaging adds exact configured cost once per order and retains true completeness.
- No active packaging rule remains known optional zero.
- Multiple dishes in one order versus multiple orders preserves packaging count/cost cardinality.
- DTO/API and UI communicate incomplete state consistently alongside missing item names.

**Acceptance Criteria:**
- IsComplete is false whenever any sold dish or applicable packaging component is unknown.
- The amount excludes unknown cost but is visibly and machine-readably partial; counts retain their documented units.
- Known/optional-zero cases preserve existing results and packaging charges are applied exactly once per applicable order.

---

### Spec: FIN-CAND-PREPGRAPH-001 — Concurrent opposite prep edges can evade pretransaction cycle admission
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs), [`src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs)

**Root Cause Diagnosis:**  
Cycle validation is a shared-graph predicate read before the transaction, while each transaction writes only one recipe; no database-level serialization/revalidation of the graph-wide invariant is visible.

**Operational Impact:**  
Predicted active prep-recipe cycle can be persisted despite save-time cycle rejection; costing becomes Unknown/incomplete for affected paths and report reliability falls. No direct treasury, inventory movement, or ledger posting from recipe save is evidenced.

**Engineering Implementation Blueprint:**  
Coordinate graph mutations with a transaction-scoped shared graph revision/lock, then revalidate against the current graph before persisting. Alternatively use serializable predicate validation with bounded retry and consistent cross-process semantics. Retain defensive Unknown cycle guard for existing/legacy data.

**Required Verification & Regression Tests:**
- Barrier-controlled opposite-edge concurrent save: at most one can commit; loser receives stable cycle/conflict error.
- Two unrelated acyclic concurrent recipe saves both succeed if product policy permits.
- Sequential cycle rejection remains.
- Legacy cyclic fixture terminates and returns Unknown/incomplete, with report not presenting complete cost.
- Exercise recipe replacement and output identity changes under same graph coordination.

**Acceptance Criteria:**
- The active prep graph remains acyclic after every successful save, including concurrent saves through multiple server processes.
- A race loser causes no partial recipe/line replacement.
- Unrelated valid edits retain expected concurrency; bounded retry/conflict behavior is documented.
- Costing defense remains finite and conservative for pre-existing invalid data.

---

### Spec: FIN-CAND-REFUND-REPLAY-001 — Overlapping full-refund replay can reject after same-key sibling committed
- **Severity:** `LOW` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs), [`src/Falah.Domain/Orders/RefundRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs), [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs), [`src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs), [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs), [`tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
Durable refund-key lookup happens before parent order serialization, with no repeat lookup after sibling full refund changes refundable state; DomainException cap rejection is not handled by saved-receipt recovery catch.

**Operational Impact:**  
Conditional misleading failure for one concurrent duplicate acknowledgement; later sequential same-key retry recovers. No new financial movement, direct moneyloss, overrefund or permanentclientloop established.

**Engineering Implementation Blueprint:**  
Recheck same-key durable refund receipt after acquiring order lock and before cap rejection; preserve actor/order/payload binding, cap and funds rules. Do not broadly treat DomainException as replay.

**Required Verification & Regression Tests:**
- Overlapping full-amount same-key refund: second request begins lookup before first commit, then after lock sees same committed receipt.
- Existing partial same-key concurrency control; distinct-key over-cap, mismatch binding and failed-first sibling.

**Acceptance Criteria:**
- After order lock, exact same-key sibling commit is returned as replay without second refund or misleading cap error.
- Distinct-key refund cap and mismatched order/key binding remain enforced; failed sibling does not fabricate success.

---

### Spec: FIN-CAND-REPORT-RANGE-001 — Reversed report endpoints normalize sales but independently filter cost with contradictory date bounds
- **Severity:** `MEDIUM` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Audit/AuditEvent.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Audit/AuditEvent.cs), [`src/Falah.Domain/Authorization/Actor.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/Actor.cs), [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs), [`src/Falah.Domain/Authorization/ManagerApproval.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ManagerApproval.cs), [`src/Falah.Domain/Authorization/PermissionCatalog.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PermissionCatalog.cs), [`src/Falah.Domain/Authorization/PinHasher.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PinHasher.cs), [`src/Falah.Domain/Authorization/SessionToken.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/SessionToken.cs), [`src/Falah.Domain/Authorization/UserManagementRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/UserManagementRules.cs), [`src/Falah.Domain/Backup/BackupRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Backup/BackupRules.cs), [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs), [`src/Falah.Domain/Common/BusinessClock.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs), [`src/Falah.Domain/Common/DomainException.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/DomainException.cs), [`src/Falah.Domain/Common/Money.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs), [`src/Falah.Domain/ControlCenter/ControlCenterRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/ControlCenter/ControlCenterRules.cs), [`src/Falah.Domain/Customers/Customer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Customers/Customer.cs), [`src/Falah.Domain/Delivery/DeliveryRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRecords.cs), [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs), [`src/Falah.Domain/Delivery/DeliveryStatus.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryStatus.cs), [`src/Falah.Domain/Employees/Attendance.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Attendance.cs), [`src/Falah.Domain/Employees/Employee.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs), [`src/Falah.Domain/Employees/EmployeeRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/EmployeeRules.cs), [`src/Falah.Domain/Financial/FinancialRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/FinancialRules.cs), [`src/Falah.Domain/Financial/PaymentMethod.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/PaymentMethod.cs), [`src/Falah.Domain/Financial/TaxRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TaxRule.cs), [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs), [`src/Falah.Domain/Idempotency/FinancialJournal.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Idempotency/FinancialJournal.cs), [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs), [`src/Falah.Domain/Inventory/InventoryRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRecords.cs), [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs), [`src/Falah.Domain/Inventory/PrepRecipeLine.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PrepRecipeLine.cs), [`src/Falah.Domain/Inventory/PurchasingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PurchasingRules.cs), [`src/Falah.Domain/Menu/ProductImageRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Menu/ProductImageRules.cs), [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs), [`src/Falah.Domain/Orders/ElectronicPayment.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/ElectronicPayment.cs), [`src/Falah.Domain/Orders/Order.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`src/Falah.Domain/Orders/OrderEnums.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderEnums.cs), [`src/Falah.Domain/Orders/OrderItem.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderItem.cs), [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs), [`src/Falah.Domain/Orders/OrderRefund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderRefund.cs), [`src/Falah.Domain/Orders/OrderValueBreakdown.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderValueBreakdown.cs), [`src/Falah.Domain/Orders/RefundRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs), [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs), [`src/Falah.Domain/Pricing/ItemPricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs), [`src/Falah.Domain/Pricing/PriceRule.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PriceRule.cs), [`src/Falah.Domain/Pricing/PricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PricingRules.cs), [`src/Falah.Domain/Printing/EscPosTicketRenderer.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/EscPosTicketRenderer.cs), [`src/Falah.Domain/Printing/PrintRecords.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRecords.cs), [`src/Falah.Domain/Printing/PrintRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/PrintRules.cs), [`src/Falah.Domain/Printing/TicketLayout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/TicketLayout.cs), [`src/Falah.Domain/Purchasing/PayablesAging.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Purchasing/PayablesAging.cs), [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs), [`src/Falah.Domain/Reports/SalesRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs), [`src/Falah.Domain/Reports/TrendRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/TrendRules.cs), [`src/Falah.Domain/Shifts/Shift.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Shifts/Shift.cs), [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs), [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Range ordering handled inconsistently between normalized sales UTC interval and raw cost/date series consumers.

**Operational Impact:**  
Misleading cost/profit/report period, not a proven money posting or authorization bypass. Static financial report correctness candidate.

**Engineering Implementation Blueprint:**  
Normalize or reject reversed date endpoints once at shared request boundary and pass one interval to sales, cost, expenses, and date-series calculations.

**Required Verification & Regression Tests:**
- Pure forward/same/reversed date range and Days/DayCount consistency.
- Authorized seeded report nonzero sales/known recipe cost reversed endpoints; direct food-cost/expense endpoints use same contract.

**Acceptance Criteria:**
- Reversed endpoints either normalize once for every report series or are rejected consistently; sales, food cost, expenses, days and day count use same interval.
- Forward, same-day, reversed, and boundary-date reports agree across DTO and direct endpoints.

---

### Spec: FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001 — SafeDrop may attribute a later drawer movement to a closed shift and change its reprinted breakdown
- **Severity:** `LOW` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs), [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs), [`src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs), [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs), [`src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs), [`tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs), [`tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
SafeDrop validates only that the request ShiftId exists; it does not require the referenced shift to remain Open. It posts with the current open BusinessDayId plus that request ShiftId. ShiftReconciliation later selects Posted safe_drop rows by ShiftId. Reprinting a close slip recomputes detail from current rows but restores the stored expected, actual and difference headline.

**Operational Impact:**  
The closed shift close-slip component breakdown can reflect a later SafeDrop while its headline remains the original saved close values. This can reduce reconciliation clarity or attribute later custody to an earlier shift if that is not the intended policy.

**Engineering Implementation Blueprint:**  
- If prohibit: enforce shift-open state in a concurrency-safe transaction boundary shared with shift close.
- If allow: represent later movement as an explicit post-close adjustment/revision and distinguish it from the immutable close snapshot in reprints.

**Required Verification & Regression Tests:**
- Open BusinessDay and closed Shift: assert the owner-selected accepted/rejected outcome and posted movement ShiftId/BusinessDayId.
- After close and any allowed later SafeDrop, reprint the slip and assert documented headline/component version semantics.
- Race SafeDrop against CloseShift on the same shift using a disposable production-SQL-Server-provider test; assert a policy-valid serialized outcome.
- If later posting is allowed, verify the audit/reprint distinguishes a later adjustment from cash present at original close.

**Acceptance Criteria:**
- Owner decision is documented.
- Stored close headline retains historical meaning.
- Reprints either preserve close-time breakdown or clearly mark later adjustments.
- Concurrent close/post outcomes obey the chosen policy.

---

### Spec: FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001 — Legacy WPF refund submit does not preserve the same idempotency key after an ambiguous outcome
- **Severity:** `LOW` | **Category:** `FINANCIAL_AND_DATABASE_INTEGRITY` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs), [`src/Falah.Client.Cashier/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/Services/ApiClient.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs), [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs), [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs), [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs)

**Root Cause Diagnosis:**  
WPF SubmitRefundAsync creates a fresh RefundRequestId per invocation and WPF ApiClient does not expose transport ambiguity as OutcomeUnknown or retain the request for replay. WinUI keeps _pendingRefund and retries the same request ID during the current ViewModel lifetime.

**Operational Impact:**  
If WPF is supported/deployed, response loss after a successful partial refund can leave the operator without explicit unknown-outcome recovery; a subsequent submit generates a new key and may create a separate additional partial refund. Impact depends on deployment, transport outcome and operator action.

**Engineering Implementation Blueprint:**  
- If WPF is supported, route refund commands through explicit unknown-outcome state and retain/reconcile the same request ID before allowing a new submit. If WPF is retired, record that operational fact. Decide whether WinUI unresolved refund intent must survive restart; if so, persist a safe pending key or require authoritative refund lookup before another command. Preserve server amount and account guards.

**Required Verification & Regression Tests:**
- Inject response loss after server commit in WPF; retry must reuse the key or require server reconciliation before a new command.
- WinUI timeout/response loss then retry in same ViewModel must use the same ID and return one receipt.
- Restart after unresolved WinUI outcome must follow owner-selected recovery policy.
- A deliberate new partial refund with a distinct ID remains allowed when the order/account permit it.
- Retain same-key replay and distinct-key cumulative cap integration tests.

**Acceptance Criteria:**
- Owner documents whether WPF is supported or retired.
- Supported clients cannot encourage a fresh financial command before prior outcome is resolved.
- Same logical operation retains the idempotency key across retry; intentional new partial refunds remain distinct and auditable.

---

### Spec: FLOW-002 — Held electronic preparation is not automatically released on confirmation
- **Severity:** `MEDIUM` | **Category:** `ORDER_LIFECYCLE_AND_WORKFLOW` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Settings/SettingsRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Settings/SettingsRequests.cs), [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs), [`src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs), [`src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs), [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Suppression is a one-time checkout decision without durable preparation-release state or confirmation outbox event.

**Operational Impact:**  
HoldPreparationUntilPaymentConfirmed suppresses initial kitchen routes. Confirmation posts money/status but does not enqueue held preparation. Auto-print documentation directs operator to Order Lookup.

**Engineering Implementation Blueprint:**  
Explicitly communicate manual release now; future approved implementation should persist held state and transactionally enqueue release with stable key on confirmation.

**Required Verification & Regression Tests:**
- Add hold->confirm release exactly once, replay, mixed payment failure, cancellation-before-release and manual reprint tests.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: INV-FIN-001 — Uncosted Waste Coercion to Zero in Loss Summary
- **Severity:** `LOW` | **Category:** `INVENTORY_AND_COSTING` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs)

**Root Cause Diagnosis:**  
Null loss value on unpriced inventory item is coerced to zero.

**Operational Impact:**  
Understates financial spoilage figures in owner management reports.

**Engineering Implementation Blueprint:**  
Expose UnknownCostCount in LossSummaryDto to inform managers when loss values are incomplete.

**Required Verification & Regression Tests:**
- Record waste on uncosted item; verify LossSummaryDto returns UnknownCostCount > 0.

**Acceptance Criteria:**
- Inventory loss reporting explicitly distinguishes 0 EGP waste from unpriced uncosted waste.

---

### Spec: INV-ROUND-001 — CostingRules Yield Unit Conversion Uses Midpoint Banker's Rounding Causing Asymmetric Stock Deduction Truncation
- **Severity:** `MEDIUM` | **Category:** `INVENTORY_AND_COSTING` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs)

**Root Cause Diagnosis:**  
CostingRules.BaseQuantityMilliUnits applies default MidpointRounding.ToEven on single portion quantities before multiplying by order quantity, truncating fractional units.

**Operational Impact:**  
Asymmetric rounding truncation leads to cumulative inventory drift where physical stock and system stock diverge over time across high-volume recipe lines.

**Engineering Implementation Blueprint:**  
Compute total deducted base milliUnits across the entire ordered quantity using integer division with rounding: (line.QuantityMilliUnits * orderQuantity + ratio / 2) / ratio, or specify explicit MidpointRounding.AwayFromZero.

**Required Verification & Regression Tests:**
- Calculate stock deduction for 3 units of an item with a yield ratio of 3 and 1000 milliUnits declared; verify total deducted milliUnits equals exactly 1000 rather than 999.

**Acceptance Criteria:**
- Inventory stock deduction for yield-ratio recipe lines preserves exact aggregate integer milliUnits without rounding truncation.

---

### Spec: PRT-002 — Succeeded and print health collapse transport acceptance into physical completion
- **Severity:** `MEDIUM` | **Category:** `PRINTING_AND_HARDWARE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Printing/PrintingRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Printing/PrintingRequests.cs), [`src/Falah.Application/Reports/OwnerDashboardDtos.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/OwnerDashboardDtos.cs), [`src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs), [`src/Falah.PrintAgent/PrintJobProcessor.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs), [`src/Falah.PrintAgent/Transports.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Transports.cs), [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs), [`tools/pre-demo-check.ps1`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tools/pre-demo-check.ps1)

**Root Cause Diagnosis:**  
Boolean transport contract has no acknowledgement level or spooler submission ID; job status has no accepted/unknown/confirmed distinction.

**Operational Impact:**  
Processor sets Succeeded after SendAsync returns. File write, TCP buffer acceptance and Windows RAW submission all satisfy this. Server overview derives last success from that flag; owner DTO comment describes physical output.

**Engineering Implementation Blueprint:**  
Use distinct Simulated, Submitted, SpoolerQueued, Failed, OutcomeUnknown and PhysicallyConfirmed observations. Never drain real pending jobs to file as production completion.

**Required Verification & Regression Tests:**
- Existing success test verifies only fake transport Sent list. Add file-simulation status, paused spooler status under explicit authorization, and operator-confirmation tests.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: PRT-003 — Blocking spooler submission ignores cancellation and completion failures
- **Severity:** `MEDIUM` | **Category:** `PRINTING_AND_HARDWARE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.PrintAgent/AgentOptions.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/AgentOptions.cs), [`src/Falah.PrintAgent/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs), [`src/Falah.PrintAgent/Transports.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Transports.cs), [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs)

**Root Cause Diagnosis:**  
Timeout configuration applies to HTTP/TCP/serial write but not full native spooler lifecycle; unmanaged result handling is incomplete.

**Operational Impact:**  
Windows SendAsync calls blocking Send and discards cancellation. EndPagePrinter/EndDocPrinter results are ignored. Serial Open/Flush also block without cooperative cancellation.

**Engineering Implementation Blueprint:**  
Check finalization results, retain submission job ID, move blocking native submission to a controlled worker with bounded shutdown/uncertain outcome semantics.

**Required Verification & Regression Tests:**
- Add injectable spooler API tests for failure and hang, short writes already checked in implementation; separate integration verification from physical output.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: PRT-004 — Agent never authenticates again after session loss
- **Severity:** `MEDIUM` | **Category:** `PRINTING_AND_HARDWARE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.PrintAgent/PrintServerClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs), [`src/Falah.PrintAgent/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs), [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs)

**Root Cause Diagnosis:**  
No token-expiry or reauthentication recovery path; agent lifecycle assumes continuously sliding valid session.

**Operational Impact:**  
Login occurs once before poll loop. All subsequent 401 failures are logged and retried with same token indefinitely.

**Engineering Implementation Blueprint:**  
Add controlled reauthentication and explicit offline/authentication alarm without losing attempt journal or blindly resending uncertain jobs.

**Required Verification & Regression Tests:**
- Add 401-then-authenticate recovery, deactivated-account terminal failure, and queued outcome reporting across reauth tests.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: PRT-006 — Saved printer width and receipt copy count do not control inspected print pipeline
- **Severity:** `LOW` | **Category:** `PRINTING_AND_HARDWARE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Application/Settings/SettingsRequests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Settings/SettingsRequests.cs), [`src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs), [`src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs), [`src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs), [`src/Falah.PrintAgent/AgentOptions.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/AgentOptions.cs)

**Root Cause Diagnosis:**  
Persistence/UI configuration has no mapped consumer in render/dispatch implementation.

**Operational Impact:**  
Settings accepts PrinterWidth and NumberOfReceiptCopies, but auto-print creates one job per route and agent uses its own ImageWidthDots. Operators can save values that do not change output.

**Engineering Implementation Blueprint:**  
Define per-station paper capability and copy semantics; explicitly label unsupported settings until implemented. Copies must not multiply kitchen prep accidentally.

**Required Verification & Regression Tests:**
- Add intended copy/width contract tests or disable unsupported setting UI.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: SEC-005 — Unguarded Read Access to Payroll Runs, Backups, Treasury, and Purchases
- **Severity:** `MEDIUM` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Server/Program.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs)

**Root Cause Diagnosis:**  
Minimal API GET endpoints for payroll runs, backup metadata, treasury handovers, and supplier debts lack authorization policy filters or permission checks.

**Operational Impact:**  
Any authenticated low-privilege user can read sensitive business financial data including employee salaries, national IDs, treasury handovers, and supplier debt aging.

**Engineering Implementation Blueprint:**  
Apply RequirePermission(context, permission) filters to all sensitive read endpoints in Program.cs.

**Required Verification & Regression Tests:**
- Issue GET /api/payroll-runs with standard cashier bearer token; verify 403 Forbidden.
- Verify GET /api/backups requires backups_manage permission.

**Acceptance Criteria:**
- All sensitive financial reporting and backup routes enforce explicit permission checks.

---

### Spec: SEC-007 — Approval Double-Spending Concurrency Hazard (CHALLENGED WITH COUNTEREVIDENCE)
- **Severity:** `MEDIUM` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs), [`src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs)

**Root Cause Diagnosis:**  
Earlier static analysis overlooked FalahDbContext's Fluent API configuration where Consumed is registered as an EF Core concurrency token.

**Operational Impact:**  
Mitigated in production by EF Core DbUpdateConcurrencyException; however, client receives an unhandled concurrency exception instead of a clean domain error message.

**Engineering Implementation Blueprint:**  
Retain concurrency token in FalahDbContext, and catch DbUpdateConcurrencyException in command handlers to translate to DomainException('اعتماد المدير مستخدم بالفعل').

**Required Verification & Regression Tests:**
- Execute concurrent SaveChangesAsync marking the same ManagerApproval consumed; assert DbUpdateConcurrencyException is caught and translated to DomainException.

**Acceptance Criteria:**
- Manager approval double-spending is strictly blocked by database-level concurrency token.

---

### Spec: SEC-010 — High PBKDF2 Computational Cost on PIN Uniqueness Validation
- **Severity:** `MEDIUM` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Users/PinUniquenessChecker.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/PinUniquenessChecker.cs), [`src/Falah.Domain/Authorization/PinHasher.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/PinHasher.cs)

**Root Cause Diagnosis:**  
Sequential synchronous evaluation of high-iteration PBKDF2 hashes across all active staff on server thread pool.

**Operational Impact:**  
Denial of service / thread pool starvation during user management operations.

**Engineering Implementation Blueprint:**  
Use deterministic HMAC blind indexing with a server pepper for O(1) uniqueness lookup, reserving PBKDF2 only for credential storage.

**Required Verification & Regression Tests:**
- Benchmark AssertNotInUseAsync with 100 active user hashes; ensure execution completes under 50ms via blind indexing.

**Acceptance Criteria:**
- PIN uniqueness validation scales in O(1) time without linear PBKDF2 recomputation.

---

### Spec: SEC-012 — User Deactivation Omits Explicit Session Token Revocation
- **Severity:** `LOW` | **Category:** `SECURITY_AND_ACCESS_CONTROL` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Infrastructure/Users/SetUserActiveHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/SetUserActiveHandler.cs), [`src/Falah.Infrastructure/Authorization/SessionService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Authorization/SessionService.cs)

**Root Cause Diagnosis:**  
User deactivation modifies user entity status only, omitting explicit session revocation.

**Operational Impact:**  
Low immediate risk due to dynamic check; architectural risk of token survival across deactivation/reactivation cycles.

**Engineering Implementation Blueprint:**  
In SetUserActiveHandler, mark all active sessions for the user as revoked (RevokedAtUtc = now).

**Required Verification & Regression Tests:**
- Deactivate user; query UserSessions for userId; assert all active sessions have RevokedAtUtc populated.

**Acceptance Criteria:**
- Deactivating an account explicitly revokes all issued session tokens.

---

### Spec: UI-002 — Unselected expense and payment buttons combine literal white with dark-theme pale text
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/Services/ThemeResources.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ThemeResources.cs), [`src/Falah.Client.CashierWinUI/Styles/Colors.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Styles/Colors.xaml), [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml), [`src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml)

**Root Cause Diagnosis:**  
Instance background bindings override correctly-themed SecondaryButtonStyle/PaymentMethodButtonStyle with WhiteBrush; text continues themed.

**Operational Impact:**  
Near-white labels on white surface, computed source-token contrast ~1.22:1, makes unselected choices hard to distinguish. User visual symptom consistent; reviewer has no actual screenshot and does not claim rendered pixel measurement.

**Engineering Implementation Blueprint:**  
Use coherent semantic card/neutral background with themed primary text for unselected state, direct ThemeResource/visual states for live updates; selected accent uses suitable contrast foreground.

**Required Verification & Regression Tests:**
- Render all expense categories and all four payment methods selected/unselected in Light/Dark/System, unchanged and after live toggle; assert resolved text/background luminance and readable native screenshot.
- Check disabled/hover/pressed states separately, preserving stored category values.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-004 — Imperative/converter brush results do not track live per-window theme reliably
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/App.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/App.xaml), [`src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs), [`src/Falah.Client.CashierWinUI/MainWindow.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/MainWindow.xaml.cs), [`src/Falah.Client.CashierWinUI/Services/ThemeResources.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ThemeResources.cs)

**Root Cause Diagnosis:**  
Converter returns concrete Brush object; unlike ThemeResource expression, no dependency informs it theme changed. Explicit dictionary lookup also does not traverse merged dictionaries.

**Operational Impact:**  
Selected/unselected/status colors can remain from previous or system theme, inconsistent with other surface/text resources. Exact WinUI fallback lookup result not runtime-proved.

**Engineering Implementation Blueprint:**  
Prefer theme-bound visual states/resources; explicitly observe ActualThemeChanged and traverse actual dictionary topology if imperative lookup retained.

**Required Verification & Regression Tests:**
- Use real WinUI root RequestedTheme opposite system theme, compare ThemeResources.Get colors to descendant ThemeResource references.
- Toggle repeatedly without changing Text/ExpenseCategory/PaymentMethod; compare same realized objects and brushes.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-006 — A new pickup inherits previous order optional collection schedule
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs), [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs)

**Root Cause Diagnosis:**  
Order reset named ResetDelivery also owns shared pickup customer fields but omits pickup-specific schedule.

**Operational Impact:**  
New customer order carries previous collection appointment or later fails past-time validation, unless operator notices and clears/changes type.

**Engineering Implementation Blueprint:**  
Reset optional schedule with resolved new-order lifecycle and explicit cart cancellation; keep historic order review separate.

**Required Verification & Regression Tests:**
- Complete scheduled pickup using fake API, dismiss result, add next pickup; date/time and RequestedPickupAtUtc must be null until explicitly selected.
- Repeat ClearCart/StartNewOrder and historic pending-order review to ensure intentional preserved live draft schedule is not erased.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-009 — Printer status UI exposes only four of six implemented routes
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs)

**Root Cause Diagnosis:**  
Hardcoded status-route list lags shared print route vocabulary.

**Operational Impact:**  
No dedicated queue summary/filter for two routes; attention aggregate may still surface failed jobs. Not total invisibility claim.

**Engineering Implementation Blueprint:**  
Derive displayed route options from complete shared contract plus localized labels.

**Required Verification & Regression Tests:**
- Provide overview with six routes and each pending/failed/stuck; assert all selectable and counts true.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-010 — Workspace shortcut guard omits most open operational modals
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml), [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs)

**Root Cause Diagnosis:**  
Custom overlays do not centralize modal state and shortcut predicate only checks variant/payment/result.

**Operational Impact:**  
Potential underlying focus change, extra flyout or checkout sheet while another form open, risking confused data entry. Runtime keyboard reachability/focus path not executed.

**Engineering Implementation Blueprint:**  
Centralize IsAnyModalOpen plus accessible focus isolation and apply to all shortcuts.

**Required Verification & Regression Tests:**
- Actual WinUI keyboard F2/F3/F12 + Tab/ShiftTab/Escape for every modal, assert no background focus/action and only one modal active.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-011 — Discarded definitively rejected CallCenter reprint survives in journal
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs)

**Root Cause Diagnosis:**  
Safe discard memory path omits corresponding persistent store clear.

**Operational Impact:**  
Old rejected request reappears as ambiguous unresolved, can block fresh operation or queue an old reprint on retry when permissions/preconditions change. Does not prove duplicate sale.

**Engineering Implementation Blueprint:**  
Clear disk atomically when explicitly discarding known-rejected print; preserve unknown state separately.

**Required Verification & Regression Tests:**
- Persist definite rejected print, clear through actual UI VM, reconstruct operator desk and assert no pending print/file.
- Ambiguous failure must continue surviving same clear attempt.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-013 — Legacy WPF change display parses EGP tender as integer piastres
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs)

**Root Cause Diagnosis:**  
Display parses 100 as100 piastres or fails for100.00 while authoritative send converts to10000.

**Operational Impact:**  
Displayed change zero instead of10 EGP; server-recorded tender may still correct. Does not affect active WinUI CashInput change implementation.

**Engineering Implementation Blueprint:**  
If WPF remains supported, use same tested CashInput units and notifications; otherwise prevent accidental deployment.

**Required Verification & Regression Tests:**
- WPF ChangeDue test 90 EGP total with100 and100.00 tender both returns1000 piastres; Arabic/current-culture formats reviewed separately.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-015 — CallCenter lock/unlock retains old order-panel visual trees through cart subscription
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs), [`src/Falah.Client.CallCenterWinUI/Services/AppSession.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/AppSession.cs), [`src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs)

**Root Cause Diagnosis:**  
Long-lived cached VM Cart event holds every prior UserControl handler; subscriptions never removed on unload or DataContext replacement.

**Operational Impact:**  
Memory/native visual resource growth and repeated invisible callbacks over long shift; actual memory growth not measured.

**Engineering Implementation Blueprint:**  
Track subscribed VM, detach on change/unload, reattach once Loaded.

**Required Verification & Regression Tests:**
- Native repeated lock/unlock, weak-reference old panel and force test GC; only current panel should remain.
- DataContext A->B->A event subscription counts exactly1 and no callbacks on detached view.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-019 — Recipe yield conversion silently truncates fractional pieces-per-base-unit
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs)

**Root Cause Diagnosis:**  
MilliUnits parser accepts fractional unit then conversion to integer floor without checking remainder.

**Operational Impact:**  
Stored recipe ratio differs from typed value and changes ingredient usage/cost calculation on future sales. No real recipe updated.

**Engineering Implementation Blueprint:**  
Validate integer divisibility before division or use dedicated localized positive-integer parser.

**Required Verification & Regression Tests:**
- Enter1.5,2.999Arabic١٫٥; must reject with explicit integer requirement before HTTP, not send1/2.
- Whole numbers1,2and textual2.000 accepted exactly, enforce overflow bound.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-022 — Recipe ingredient picker truncates to60 choices without exposing its search input
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs), [`src/Falah.Client.CashierWinUI/Views/RecipesView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/RecipesView.xaml)

**Root Cause Diagnosis:**  
VM implemented bounded searchable choices but XAML exposes only ComboBox over current60 and no search binding to query full cached ingredients.

**Operational Impact:**  
Operator cannot directly find later ingredients through normal picker. Recipe may remain incomplete or user may choose wrong similar item; no actual recipe mutation tested.

**Engineering Implementation Blueprint:**  
Bind explicit ingredient search to IngredientSearchText or provide searchable full-list picker; disclose bounds only as presentation optimization.

**Required Verification & Regression Tests:**
- Load61+ active ingredients with nonechosen; entering target61 search should exposeit without adding unrelated ingredients.
- Exclude chosenIDs while searching fullcached inventory; clearing search should preserve selected choice and exact recipe lines.

**Acceptance Criteria:**
- I
- n
- v
- a
- r
- i
- a
- n
- t
- 
- h
- o
- l
- d
- s
- 
- u
- n
- d
- e
- r
- 
- r
- e
- t
- r
- i
- e
- s
- 
- a
- n
- d
- 
- c
- o
- n
- c
- u
- r
- r
- e
- n
- t
- 
- e
- x
- e
- c
- u
- t
- i
- o
- n
- s
- .

---

### Spec: UI-023 — EmployeesViewModel Sequentially Executes N+1 API Calls for Balances and Exits Early on First Failure Leaving Employee List Empty
- **Severity:** `MEDIUM` | **Category:** `UI_UX_AND_DESKTOP_CLIENT_STATE` | **Priority:** `P2_STABILITY_AND_MAINTAINABILITY`
- **Target Files:** [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs)

**Root Cause Diagnosis:**  
Sequential N+1 API balance calls in UI loading routine coupled with catastrophic early exit on single failure prior to applying display filter.

**Operational Impact:**  
A single network hiccup or unmapped employee ledger record results in the entire employee list disappearing from the UI, blocking shift attendance and payroll operations.

**Engineering Implementation Blueprint:**  
Introduce a bulk employee balance endpoint on the server, or fetch balances concurrently with Task.WhenAll and gracefully degrade individual failed balances while still displaying successfully loaded employees.

**Required Verification & Regression Tests:**
- Simulate employee balance API failure for 1 out of 10 employees; assert EmployeesViewModel displays remaining 9 employees and shows non-blocking warning rather than an empty list.

**Acceptance Criteria:**
- EmployeesViewModel displays all successfully loaded employees even if an individual employee balance lookup fails, and eliminates sequential N+1 network requests.

---
