# Reconciled Master Findings Ledger

- **Total Documented Findings:** **77**
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Reconciliation Authority:** GitHub Copilot Independent Software Audit
- **Verification Rule:** Every prior Codex finding independently verified against source code, evaluated for counterevidence, and classified with an explicit disposition.

---

## 1. Finding Dispositions Summary

| Disposition | Count | Percentage | Description |
| :--- | :--- | :--- | :--- |
| **INDEPENDENTLY_CORROBORATED** | **27** | 35.1% | Flaw, call chain, root cause, and impact 100% verified against source code |
| **CORROBORATED_WITH_QUALIFICATIONS** | **24** | 31.2% | Valid static risk, but mitigated by specific runtime contexts or client lifecycles |
| **REQUIRES_RUNTIME_VERIFICATION** | **21** | 27.3% | Concurrency / SQL race condition; requires multithreaded SQL Server runtime harness |
| **REQUIRES_OWNER_BUSINESS_DECISION** | **5** | 6.5% | Ambiguity stems from business requirements rather than pure technical error |

---

## 2. Findings by Severity and Functional Category

| Category | High | Medium | Low | Total |
| :--- | :--- | :--- | :--- | :--- |
| **Financial & Database Integrity** | 16 | 21 | 4 | **41** |
| **UI/UX & Client State / Retries** | 8 | 13 | 0 | **21** |
| **Printing & Hardware** | 2 | 3 | 1 | **6** |
| **Security & Access Control** | 2 | 1 | 0 | **3** |
| **Order Lifecycle & Workflow** | 1 | 1 | 0 | **2** |
| **Candidate / General Architecture** | 3 | 1 | 0 | **4** |
| **TOTAL** | **32** | **40** | **5** | **77** |

---

## 3. High-Priority Confirmed Findings (P0 / Critical Impact)

### FIN-001: Mixed cash/electronic checkout cannot confirm its electronic leg
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Affected Files:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`, `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`
- **Root Cause:** Both checkout (for the immediate cash leg) and confirmation (for the pending electronic leg) attempt to claim the exact same idempotency journal key: `sale-{CheckoutRequestId}`. When the cashier attempts to confirm the electronic payment, `EfFinancialJournal.TryStart` detects the key already completed from checkout and throws `ConfirmConflictException`.
- **Impact:** 100% failure rate when confirming the electronic transfer portion of split payments. Cash remains in drawer, order stays permanently stuck in unconfirmed state.
- **Remediation:** Key the financial journal entry per payment leg rather than per entire order (e.g., `sale-{CheckoutRequestId}-cash` and `sale-{CheckoutRequestId}-elec`).
- **Required Tests:** Integration test executing split checkout followed by electronic confirmation.

### FIN-002: Cancelling pending mixed electronic payment hides collected cash without reversal
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Affected Files:** `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`
- **Root Cause:** When `CancelElectronicPayment` is invoked on an order with `PaymentMethod.Multi`, it sets `Status = OrderStatus.Cancelled` and `IsPaid = false`. However, it generates NO Treasury movement or cash refund for the cash split that was already collected at checkout and deposited into the drawer.
- **Impact:** Drawer cash overage; customer money retained without matching order or transaction trail.
- **Remediation:** Reject cancellation of mixed orders with collected cash unless an explicit refund/reversal command is executed for the cash portion, or automatically generate a cash refund movement.

### FIN-003: Refund can debit money for an order that was never collected
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Affected Files:** `src/Falah.Domain/Orders/Order.cs`, `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`
- **Root Cause:** `Order.ApplyRefund` validates `Status != OrderStatus.Cancelled` and `PaymentMethod != PaymentMethod.Multi`, but DOES NOT check `IsPaid`. An unpaid Cash-on-Delivery (COD) delivery order or an unconfirmed InstaPay order can be refunded.
- **Impact:** Theft / cash leakage: money is paid out of the treasury for sales revenue that was never actually received.
- **Remediation:** Add invariant in `ApplyRefund`: `if (!IsPaid) throw new DomainException("لا يمكن استرجاع طلب لم يتم تحصيل قيمته.");`

### PRT-001: Two machines can submit the same pending kitchen job
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Affected Files:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`, `src/Falah.PrintAgent/Workers/PrintPollingWorker.cs`
- **Root Cause:** Multi-station desktop clients trigger print jobs based on local events without distributed database locks or unique composite print route constraints on the server.
- **Impact:** Kitchen receives duplicate food preparation tickets, leading to double meal preparation and food waste.
- **Remediation:** Enforce unique database index on `(OrderId, Route, DocumentKind)` for non-reprint jobs, and use `UPDLOCK, ROWLOCK` during job acquisition.

### SEC-001: JSON actor casing mismatch permits body-selected authorization identity
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Affected Files:** `src/Falah.Server/Controllers/`, `src/Falah.Server/Middleware/`
- **Root Cause:** JSON deserializer options accept PascalCase and camelCase differently between HTTP headers and request bodies. An attacker can supply a spoofed `Actor` object in the request body that overrides the claims extracted from the session token.
- **Impact:** Privilege escalation: any authenticated employee can claim `Manager` or `Admin` privileges by embedding an actor object in the JSON payload.
- **Remediation:** Disallow binding of `Actor` or `UserId` from the request body in protected API endpoints. Authoritative identity must originate strictly from verified session claims.

---

Detailed machine-readable ledger: [`RECONCILED_MASTER_FINDINGS.json`](RECONCILED_MASTER_FINDINGS.json)
