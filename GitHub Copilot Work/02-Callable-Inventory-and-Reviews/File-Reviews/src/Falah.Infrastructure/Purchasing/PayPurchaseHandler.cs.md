# Deep Per-Callable Source Audit: `PayPurchaseHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs)
- **Role:** Post-Receipt Supplier Invoice Settlement & Payment Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:50:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 16 (1 Constructor, 1 Method, 14 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `PayPurchaseHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:27:5:ConstructorDeclaration`
- **Signature:** `public PayPurchaseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Line Span:** Lines 27–32
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via dependency injection and stores them into private readonly fields `_db`, `_approvalRegistry`, and `_auditLog`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by the ASP.NET Core DI container (scoped lifetime) when routing `POST /api/purchases/{id}/pay`.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:34:5:MethodDeclaration`
- **Signature:** `public async Task<PurchaseDto> HandleAsync(PayPurchaseRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 34–149
- **Actual Behavior & Inputs:**
  Receives `PayPurchaseRequest(PurchaseId, AmountPiastres, PaymentMethod, ActorUserId, RequestId, ManagerApprovalId, ManagerApproverUserId)`. Orchestrates:
  1. *Purchase Lookup (Lines 36–39):* Queries `_db.Purchases.Include(p => p.Lines).Include(p => p.Payments)` for `request.PurchaseId`. Rejects if missing or if `Status == "cancelled"`.
  2. *Idempotency Pre-Check (Lines 41–43):* Derives `idempotencyKey = $"purchase-payment-{purchase.Id}-{request.RequestId}"`. Queries `_db.PurchasePayments.AsNoTracking()`. If found, short-circuits via `ReceivePurchaseHandler.ToDto(purchase, wasReplayed: true)`.
  3. *Actor & Permission Authorization (Lines 45–57):* Validates `actorUser` existence. If `request.ManagerApprovalId` is provided, fetches approver user and validates approval. Calls `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "purchasing_manage", approval, requireApproval: true)`.
  4. *Unprotected Outstanding Balance Derivation (Lines 59–68):* Fetches all `SupplierLedgerEntries` for `purchase.SupplierId` with `.AsNoTracking()`. Computes `outstanding` balance via `PurchasingRules.SupplierOutstandingFromLedger`. Asserts `PurchasingRules.AssertPurchasePayment(amount, outstanding)` and `amount.IsPositive`.
  5. *Payment Account & Business Day Gate (Lines 69–71):* Maps `request.PaymentMethod` to treasury account via `PurchasingRules.PurchasePaymentAccountFor`. Requires open business day via `BusinessDayGate.RequireOpenAsync`.
  6. *Transaction Initiation (Line 73):* Begins explicit transaction `await _db.Database.BeginTransactionAsync(ct)`.
  7. *Treasury Balance Lock (Lines 80–81):* Locks source treasury account movements with `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` and validates sufficient funds via `FinancialRules.AssertProtectedSourceBalance`.
  8. *Financial Journal Lock (Lines 83–85):* Enforces idempotency via `EfFinancialJournal.TryStart(idempotencyKey, "purchase_payment")`.
  9. *Treasury & Ledger Entities Mutation (Lines 87–121):*
     - Generates `TreasuryMovement` (`purchase_payment`) from source account.
     - Decrements `RemainingAmountPiastres` and increments `PaidPiastres`. Updates `PaymentStatus` and `Status` to `"paid"` or `"partially_paid"`.
     - Appends `PurchasePaymentEntity`.
     - Appends `SupplierLedgerEntryEntity` (`supplier_payment`, credit `amount.Piastres`).
     - Appends audit event to `_auditLog`.
     - Completes financial journal (`journal.Complete(idempotencyKey)`).
  10. *Commit & Persistence (Lines 122–124):* Calls `_db.SaveChangesAsync(ct)` and `transaction.CommitAsync(ct)`. Returns `ReceivePurchaseHandler.ToDto(purchase, wasReplayed: false)`.
  11. *Catch Block & Balance Contention Recovery (Lines 126–148):* On `DbUpdateException`, `PurchasingConflictException`, or `BalanceLockContention.IsContention(ex)`, rolls back transaction. Awaits sibling commit via `BalanceLockContention.AwaitSiblingAsync`. Reloads purchase entity and returns replay or throws `PurchasingConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/purchases/{id}/pay` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`, `AuthorizationPolicy`, `PurchasingRules`, `BusinessDayGate`, `TreasuryBalanceQuery`, `FinancialRules`, `TreasuryMovementFactory`, `EfFinancialJournal`, `BalanceLockContention`, `ReceivePurchaseHandler`.
  - Side effects: Modifies `Purchases`, inserts `PurchasePayments`, `TreasuryMovements`, `SupplierLedgerEntries`, `FinancialJournals`, `AuditEvents`. Deducts real funds from treasury.
- **Critical Financial Integrity Flaws:**

  > [!CRITICAL]
  > **Vulnerability `FIN-005` (Supplier Invoice Overpayment Concurrency Race):**
  > At lines 36–37 and 59–66:
  > ```csharp
  > var purchase = await _db.Purchases.Include(p => p.Lines).Include(p => p.Payments)
  >     .FirstOrDefaultAsync(p => p.Id == request.PurchaseId, ct)
  >     ?? throw new InvalidOperationException("فاتورة الشراء غير موجودة.");
  > ...
  > var ledgerEntries = await _db.SupplierLedgerEntries.AsNoTracking()
  >     .Where(e => e.SupplierId == purchase.SupplierId).ToListAsync(ct);
  > var outstanding = PurchasingRules.SupplierOutstandingFromLedger(
  >     ledgerEntries.Select(e => new SupplierLedgerEntry(...)),
  >     purchase.SupplierId, purchase.Id);
  > var amount = Money.FromPiastres(request.AmountPiastres);
  > PurchasingRules.AssertPurchasePayment(amount, outstanding);
  > ...
  > await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  > ```
  > **FLAW MECHANISM:**
  > 1. Neither `Purchases` nor `SupplierLedgerEntries` are queried with pessimistic locks (`UPDLOCK`, `ROWLOCK`, `HOLDLOCK`).
  > 2. Both queries run **OUTSIDE** the database transaction (lines 36 and 59 occur before `BeginTransactionAsync` at line 73).
  > 3. Concurrency race: Suppose a purchase invoice has an outstanding balance of 10,000 EGP (1,000,000 piastres). Two operators (or two browser tabs/automated integrations) simultaneously submit payments of 10,000 EGP with different `RequestId`s (Request 1 and Request 2).
  >    - Request 1 reads `outstanding = 1,000,000 piastres` and validates.
  >    - Request 2 reads `outstanding = 1,000,000 piastres` and validates.
  >    - Request 1 enters transaction, locks treasury balance, pays 10,000 EGP, sets `purchase.PaidPiastres = 1,000,000`, `purchase.RemainingAmountPiastres = 0`, inserts `PurchasePayment` and `SupplierLedgerEntry`, and commits.
  >    - Request 2 now enters transaction. It locks treasury balance, executes lines 95–98:
  >      ```csharp
  >      var remaining = outstanding - amount; // outstanding was evaluated as 1,000,000 before Request 1 committed!
  >      purchase.PaidPiastres += amount.Piastres;
  >      purchase.RemainingAmountPiastres = remaining.Piastres;
  >      ```
  >      Because EF Core change tracking on `purchase` was loaded before Request 1 committed, Request 2 writes another 10,000 EGP payment, another `SupplierLedgerEntry`, and another `TreasuryMovement` draining an additional 10,000 EGP from `tre-main`!
  >    - **Result:** The supplier is paid 20,000 EGP on a 10,000 EGP invoice (100% overpayment), treasury is drained by an extra 10,000 EGP, and the supplier ledger reflects an erroneous credit balance.
  > 4. The doc comment on line 18 claims: *"Outstanding is always read fresh from the supplier ledger... so a stale in-memory value can never let a payment overshoot what is genuinely still owed."* Under concurrent access, reading without an explicit lock inside a transaction completely invalidates this guarantee.

- **Concurrency & Locking Assessment:**
  - Treasury movement locking: Properly uses `LoadAccountMovementsForUpdateAsync` inside the transaction to protect source cash from negative overdraft.
  - Sibling contention handling: Robust backoff via `BalanceLockContention.AwaitSiblingAsync`.
  - Missing lock: No lock on `Purchases` row or `SupplierLedgerEntries` range, creating vulnerability `FIN-005`.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/PurchasingVerticalSliceTests.cs` (covers happy path full/partial payments, approval requirements, and replay idempotency).
  - Missing Regression: Concurrency test firing two simultaneous `PayPurchaseRequest` calls against the same invoice totaling more than outstanding balance to assert serialization and rejection of the second payment (`FIN-005`).
- **Classification:** Core financial settlement handler with critical concurrency overpayment vulnerability (`FIN-005`).

---

### 3. Lines Navigation Include Lambda (`p => p.Lines`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:36:52:SimpleLambdaExpression`
- **Signature:** `p => p.Lines`
- **Line Span:** Line 36
- **Actual Behavior & Inputs:**
  Loads `PurchaseLines` child collection on `Purchases`.
- **Classification:** ORM mapping lambda.

---

### 4. Payments Navigation Include Lambda (`p => p.Payments`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:36:74:SimpleLambdaExpression`
- **Signature:** `p => p.Payments`
- **Line Span:** Line 36
- **Actual Behavior & Inputs:**
  Loads `PurchasePayments` child collection on `Purchases`.
- **Classification:** ORM mapping lambda.

---

### 5. Purchase Lookup Predicate (`p => p.Id == request.PurchaseId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:37:34:SimpleLambdaExpression`
- **Signature:** `p => p.Id == request.PurchaseId`
- **Line Span:** Line 37
- **Actual Behavior & Inputs:**
  Filters `_db.Purchases` by primary key `request.PurchaseId`.
- **Classification:** Query filter lambda.

---

### 6. Idempotency Pre-Check Predicate (`p => p.IdempotencyKey == idempotencyKey`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:42:93:SimpleLambdaExpression`
- **Signature:** `p => p.IdempotencyKey == idempotencyKey`
- **Line Span:** Line 42
- **Actual Behavior & Inputs:**
  Filters `PurchasePayments` with `AsNoTracking()` to check for prior payment execution.
- **Classification:** Query filter lambda.

---

### 7. Actor User Lookup Predicate (`u => u.Id == request.ActorUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:45:76:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ActorUserId`
- **Line Span:** Line 45
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to verify payer identity.
- **Classification:** Query filter lambda.

---

### 8. Manager Approver Lookup Predicate (`u => u.Id == request.ManagerApproverUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:52:83:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Line Span:** Line 52
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to locate manager approver.
- **Classification:** Query filter lambda.

---

### 9. Supplier Ledger Entries Filter Predicate (`e => e.SupplierId == purchase.SupplierId`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:60:20:SimpleLambdaExpression`
- **Signature:** `e => e.SupplierId == purchase.SupplierId`
- **Line Span:** Line 60
- **Actual Behavior & Inputs:**
  Loads all ledger entries for the purchase's supplier to evaluate outstanding balances. Executed outside transaction without locks (`FIN-005`).
- **Classification:** Query filter lambda.

---

### 10. Supplier Ledger Entry Projection Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:62:34:SimpleLambdaExpression`
- **Signature:** `e => new SupplierLedgerEntry(e.SupplierId, e.PurchaseId, e.ReferenceId, Money.FromPiastres(e.DebitPiastres), Money.FromPiastres(e.CreditPiastres))`
- **Line Span:** Line 62
- **Actual Behavior & Inputs:**
  Maps `SupplierLedgerEntryEntity` database rows to domain `SupplierLedgerEntry` records.
- **Classification:** In-memory transformation lambda.

---

### 11. Catch Block Replay Predicate (`p => p.IdempotencyKey == idempotencyKey`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:130:88:SimpleLambdaExpression`
- **Signature:** `p => p.IdempotencyKey == idempotencyKey`
- **Line Span:** Line 130
- **Actual Behavior & Inputs:**
  Checks if a payment row was committed by a winning concurrent transaction.
- **Classification:** Query filter lambda.

---

### 12. Contention Await Sibling Callback
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:136:21:ParenthesizedLambdaExpression`
- **Signature:** `async () => await _db.PurchasePayments.AsNoTracking().FirstOrDefaultAsync(p => p.IdempotencyKey == idempotencyKey, ct)`
- **Line Span:** Line 136
- **Actual Behavior & Inputs:**
  Asynchronous polling callback passed to `BalanceLockContention.AwaitSiblingAsync`.
- **Classification:** Concurrency synchronization lambda.

---

### 13. Sibling Await Polling Predicate (`p => p.IdempotencyKey == idempotencyKey`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:136:95:SimpleLambdaExpression`
- **Signature:** `p => p.IdempotencyKey == idempotencyKey`
- **Line Span:** Line 136
- **Actual Behavior & Inputs:**
  Predicate used inside `AwaitSiblingAsync` loop.
- **Classification:** Query filter lambda.

---

### 14. Catch Block Reload Lines Navigation Lambda (`p => p.Lines`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:139:76:SimpleLambdaExpression`
- **Signature:** `p => p.Lines`
- **Line Span:** Line 139
- **Actual Behavior & Inputs:**
  Navigation include on purchase entity reload after rollback.
- **Classification:** ORM mapping lambda.

---

### 15. Catch Block Reload Payments Navigation Lambda (`p => p.Payments`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:139:98:SimpleLambdaExpression`
- **Signature:** `p => p.Payments`
- **Line Span:** Line 139
- **Actual Behavior & Inputs:**
  Navigation include on purchase entity reload after rollback.
- **Classification:** ORM mapping lambda.

---

### 16. Catch Block Purchase Reload Predicate (`p => p.Id == purchase.Id`)
- **Symbol ID:** `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs:140:33:SimpleLambdaExpression`
- **Signature:** `p => p.Id == purchase.Id`
- **Line Span:** Line 140
- **Actual Behavior & Inputs:**
  Loads fresh un-tracked purchase row with updated balances for replay response.
- **Classification:** Query filter lambda.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-005`** | **CRITICAL** | `PayPurchaseHandler.cs:36-37, 59-66, 73` | Invoice balance and supplier ledger are read outside database transaction without `UPDLOCK`/`HOLDLOCK`, permitting concurrent overpayment and treasury over-disbursement beyond invoice total. | Move `_db.Purchases` and `_db.SupplierLedgerEntries` queries **INSIDE** the transaction and lock the purchase row using `SELECT ... WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}` before calculating outstanding balance. |
