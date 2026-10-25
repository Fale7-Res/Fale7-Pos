# Deep Per-Callable Source Audit: `WithdrawalHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs)
- **Role:** Owner Capital Withdrawal & Treasury Outflow Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T01:05:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 8 (1 Constructor, 3 Methods, 4 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `WithdrawalHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:23:5:ConstructorDeclaration`
- **Signature:** `public WithdrawalHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Line Span:** Lines 23–28
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via DI constructor injection and stores them into private readonly fields `_db`, `_approvalRegistry`, and `_auditLog`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by ASP.NET Core DI container (scoped lifetime) during API route dispatch for `POST /api/treasury/withdraw`.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:30:5:MethodDeclaration`
- **Signature:** `public async Task<WithdrawalResult> HandleAsync(WithdrawalRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 30–109
- **Actual Behavior & Inputs:**
  Receives `WithdrawalRequest(WithdrawalRequestId, AmountPiastres, SourceAccountId, Reason, Reference, CreatedByUserId, ManagerApprovalId, ManagerApproverUserId)`. Orchestrates:
  1. *Idempotency Pre-Check (Lines 32–34):* Derives `idempotencyKey = $"owner-withdrawal-{request.WithdrawalRequestId}"`. Queries `FindExistingAsync`. Returns `ToResult(existing, wasReplayed: true)` if found.
  2. *Account & Request Validation (Lines 36–39):* Enforces that `SourceAccountId` must belong to `TreasuryAccountValidation.ExternalCapitalAccounts` (`tre-main`, `tre-bank`, `tre-instapay`; rejects `tre-drawer` and `tre-wallet`). Asserts positive amount and non-empty reason.
  3. *Actor Authorization & Manager Approval Gate (Lines 41–53):* Validates `createdByUser`. If `request.ManagerApprovalId` is provided, fetches approver user and validates approval. Invokes `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "treasury_manage", approval, requireApproval: true)`.
  4. *Business Day Gate (Line 55):* Calls `BusinessDayGate.RequireOpenAsync(_db, ct)` outside transaction.
  5. *Database Transaction Initiation (Line 56):* Begins explicit transaction `await _db.Database.BeginTransactionAsync(ct)`.
  6. *Pessimistic Source Balance Lock (Lines 63–64):* Locks all posted movements touching `request.SourceAccountId` using `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`). Asserts source balance via `FinancialRules.AssertProtectedSourceBalance`.
  7. *Financial Journal Lock (Lines 66–68):* Initiates idempotency lock via `EfFinancialJournal.TryStart(idempotencyKey, "owner_withdrawal")`.
  8. *Movement & Audit Log Entries (Lines 70–85):*
     - Constructs domain `TreasuryMovement` (`owner_withdrawal`, source account -> null destination).
     - Appends `TreasuryMovementEntity`.
     - Completes financial journal (`journal.Complete(idempotencyKey)`).
     - Appends critical audit event to `_auditLog` (classified as `"critical"` due to direct capital extraction).
  9. *Commit & Persistence (Lines 87–89):* Saves changes and commits transaction. Returns `ToResult(entity, wasReplayed: false)`.
  10. *Catch Block & Contention Recovery (Lines 91–108):* On `DbUpdateException`, `TreasuryConflictException`, or `BalanceLockContention.IsContention(ex)`, rolls back transaction. Awaits sibling commit via `BalanceLockContention.AwaitSiblingAsync`. Returns replayed entity or throws `TreasuryConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/treasury/withdraw` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`, `TreasuryAccountValidation`, `AuthorizationPolicy`, `BusinessDayGate`, `TreasuryBalanceQuery`, `FinancialRules`, `TreasuryMovementFactory`, `EfFinancialJournal`, `OrderMapper`, `BalanceLockContention`.
  - Side effects: Inserts into `TreasuryMovements`, `FinancialJournals`, `AuditEvents`. Permanently withdraws business capital.
- **Financial Integrity & Concurrency Evaluation:**
  - **Transaction Placement of Balance Locks (Properly Placed vs `FIN-011`):**
    Unlike the defective pre-check in `OpenShiftHandler.cs:53` and `ReceivePurchaseHandler.cs:77` where `LoadAccountMovementsForUpdateAsync` ran *before* `BeginTransactionAsync`, `WithdrawalHandler` executes `LoadAccountMovementsForUpdateAsync` at line 63 **INSIDE** the transaction started at line 56. The SQL Server `WITH (UPDLOCK, HOLDLOCK)` range locks remain active until commit or rollback at lines 88/94.
  - **Capital Boundary Enforcement:**
    Strictly forbids withdrawing cash directly from `tre-drawer` (cashier's operational till) or `tre-wallet`. Cashiers cannot masquerade till shortages as owner withdrawals.
  - **Pre-Transaction Business Day Gate Race:**
    At line 55, `BusinessDayGate.RequireOpenAsync(_db, ct)` runs *outside* the transaction with `AsNoTracking()`. If a business day is closed concurrently during manager approval validation, the withdrawal transaction proceeds and records an outflow stamped with a closed business day ID.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs` (covers owner withdrawal happy path, drawer/wallet account rejection, approval requirement, concurrent same-request replay, and balance deduction).
  - Missing Regression: Concurrency test racing an owner withdrawal against an expense payout on `tre-main` when balance is sufficient for only one operation, proving serialized rejection of the second transaction without negative balance overdraft.
- **Classification:** Core treasury withdrawal orchestrator with robust internal transaction locking and capital account validation.

---

### 3. Payer User Lookup Predicate (`u => u.Id == request.CreatedByUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:41:80:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.CreatedByUserId`
- **Line Span:** Line 41
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to load creator identity.
- **Classification:** Query filter lambda.

---

### 4. Manager Approver Lookup Predicate (`u => u.Id == request.ManagerApproverUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:48:83:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Line Span:** Line 48
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to retrieve manager approver for approval verification.
- **Classification:** Query filter lambda.

---

### 5. Contention Await Sibling Callback
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:101:21:ParenthesizedLambdaExpression`
- **Signature:** `async () => await FindExistingAsync(idempotencyKey, ct)`
- **Line Span:** Line 101
- **Actual Behavior & Inputs:**
  Asynchronous polling callback passed to `BalanceLockContention.AwaitSiblingAsync`.
- **Classification:** Concurrency synchronization lambda.

---

### 6. `FindExistingAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:111:5:MethodDeclaration`
- **Signature:** `private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct)`
- **Line Span:** Lines 111–112
- **Actual Behavior & Inputs:**
  Queries `_db.TreasuryMovements.AsNoTracking()` by `m.IdempotencyKey == idempotencyKey`.
- **Classification:** Database query helper.

---

### 7. Idempotency Key Filter Predicate (`m => m.IdempotencyKey == idempotencyKey`)
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:112:66:SimpleLambdaExpression`
- **Signature:** `m => m.IdempotencyKey == idempotencyKey`
- **Line Span:** Line 112
- **Actual Behavior & Inputs:**
  Filters `TreasuryMovements` by idempotency key string.
- **Classification:** Query filter lambda.

---

### 8. `ToResult`
- **Symbol ID:** `src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs:114:5:MethodDeclaration`
- **Signature:** `private static WithdrawalResult ToResult(TreasuryMovementEntity entity, bool wasReplayed)`
- **Line Span:** Lines 114–115
- **Actual Behavior & Inputs:**
  Maps `TreasuryMovementEntity` to `WithdrawalResult(Id, AmountPiastres, SourceAccountId, WasReplayed)`.
- **Classification:** Pure mapping helper.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-GATE-001`** | **MEDIUM** | `WithdrawalHandler.cs:55` | `BusinessDayGate.RequireOpenAsync` executes outside transaction with `AsNoTracking()`. Withdrawals can execute against a business day that closed mid-flight. | Re-validate business day status inside the transaction using `SELECT ... WITH (UPDLOCK)` on `BusinessDays` before committing movements. |
