# Deep Per-Callable Source Audit: `DepositHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Treasury/DepositHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/DepositHandler.cs)
- **Role:** External Capital Injection & Treasury Deposit Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T01:10:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 8 (1 Constructor, 3 Methods, 4 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `DepositHandler` Constructor
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:24:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 24–29
- **Signature:** `public DepositHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via DI constructor injection and assigns them to private readonly fields `_db`, `_approvalRegistry`, and `_auditLog`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by the ASP.NET Core DI container (scoped lifetime) during API route dispatch for `POST /api/treasury/deposit`.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:31:5:MethodDeclaration`
- **Exact Line Range:** Lines 31–106
- **Signature:** `public async Task<DepositResult> HandleAsync(DepositRequest request, CancellationToken ct = default)`
- **Actual Behavior & Inputs:**
  Receives `DepositRequest(DepositRequestId, Type, AmountPiastres, DestinationAccountId, Reason, Reference, CreatedByUserId, ManagerApprovalId, ManagerApproverUserId)`. Orchestrates:
  1. *Idempotency Pre-Check (Lines 33–35):* Formats `idempotencyKey = $"{request.Type.Replace('_', '-')}-{request.DepositRequestId}"`. Queries `FindExistingAsync`. Returns `ToResult(existing, wasReplayed: true)` if found.
  2. *Account & Request Validation (Lines 37–42):* Validates `request.Type` in `ValidDepositTypes` (`owner_deposit`, `capital_injection`, `other_external_inflow`). Asserts `DestinationAccountId` is within `ExternalCapitalAccounts` (`tre-main`, `tre-bank`, `tre-instapay`). Enforces amount > 0 and non-empty reason.
  3. *Actor Authorization & Manager Approval Gate (Lines 44–56):* Fetches `createdByUser` via `_db.Users.AsNoTracking()`. If `request.ManagerApprovalId` is provided, fetches approver and constructs `ManagerApproval`. Calls `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "treasury_manage", approval, requireApproval: true)`.
  4. *Pre-Transaction Business Day Gate (Line 58):* Executes `var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);` outside transaction.
  5. *Transaction Initiation (Line 60):* Begins explicit database transaction `await _db.Database.BeginTransactionAsync(ct)`.
  6. *Financial Journal Idempotency (Lines 63–65):* Executes `journal.TryStart(idempotencyKey, request.Type)`. Throws `TreasuryConflictException` if duplicate.
  7. *Entity Mutation & Audit (Lines 67–83):*
     - Constructs inflow movement (`sourceAccountId: null`, `destinationAccountId: request.DestinationAccountId`).
     - Appends `TreasuryMovementEntity` to `_db.TreasuryMovements`.
     - Completes journal (`journal.Complete(idempotencyKey)`).
     - Appends audit event to `_auditLog`.
  8. *Commit & Persistence (Lines 84–86):* Calls `_db.SaveChangesAsync(ct)` and `transaction.CommitAsync(ct)`. Returns `ToResult(entity, wasReplayed: false)`.
  9. *Catch Block & Balance Contention Recovery (Lines 88–105):* On `DbUpdateException`, `TreasuryConflictException`, or `BalanceLockContention.IsContention(ex)`, rolls back transaction. Awaits sibling commit via `BalanceLockContention.AwaitSiblingAsync`. Returns replayed entity or throws `TreasuryConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/treasury/deposit` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`, `TreasuryAccountValidation`, `AuthorizationPolicy`, `BusinessDayGate`, `TreasuryMovementFactory`, `EfFinancialJournal`, `OrderMapper`, `BalanceLockContention`.
  - Side effects: Inserts into `TreasuryMovements`, `FinancialJournals`, `AuditEvents`. Increases balance of external capital account.
- **Critical Financial Integrity & Concurrency Analysis:**

  > [!WARNING]
  > **Vulnerability `FIN-GATE-001` (Business Day Gate Outside Transaction Scope):**
  > At line 58:
  > ```csharp
  > var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  > await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  > ```
  > `BusinessDayGate.RequireOpenAsync(_db, ct)` queries `BusinessDays` with `.AsNoTracking()` **before** `BeginTransactionAsync(ct)`. If a business day is closed concurrently by a manager executing `CloseBusinessDayHandler`, the deposit transaction proceeds to open and records an inflow movement stamped with a `BusinessDayId` that was closed mid-flight. When the closed business day statement is reprinted, this cash inflow appears post-reconciliation or is orphaned from the closed financial reporting period.

  > [!NOTE]
  > **External Capital Boundary Integrity:**
  > Lines 39–40 enforce that `DestinationAccountId` must be in `TreasuryAccountValidation.ExternalCapitalAccounts` (`tre-main`, `tre-bank`, `tre-instapay`). Direct deposit into `tre-drawer` is strictly forbidden. Drawer cash can only originate from shift opening floats or customer order collections, preventing cashiers from masking till shortages as external owner deposits.

- **Concurrency & Locking Assessment:**
  - Inflows do not consume from protected source balances, so no source balance locking (`LoadAccountMovementsForUpdateAsync`) is required.
  - Idempotency key uniqueness is guaranteed by `FinancialJournals` primary key constraint.
  - Sibling contention handling ensures serialization on database lock collisions.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs` (verifies deposit increases destination balance, requires open business day, rejects drawer/wallet destination, and handles concurrent replays).
  - Missing Regression: Concurrency test closing a business day concurrently while `DepositHandler` processes an approval, verifying that the deposit is rejected or rolled back rather than stamped with a closed day ID (`FIN-GATE-001`).
- **Classification:** Core capital injection handler with pre-transaction business day gate gap (`FIN-GATE-001`).

---

### 3. Payer User Lookup Predicate (`u => u.Id == request.CreatedByUserId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:44:80:SimpleLambdaExpression`
- **Exact Line Range:** Line 44
- **Signature:** `u => u.Id == request.CreatedByUserId`
- **Actual Behavior & Inputs:**
  Expression predicate for `_db.Users.AsNoTracking().FirstOrDefaultAsync(...)`.
- **Classification:** Query filter lambda.

---

### 4. Manager Approver Lookup Predicate (`u => u.Id == request.ManagerApproverUserId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:51:83:SimpleLambdaExpression`
- **Exact Line Range:** Line 51
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to retrieve manager approver for approval verification.
- **Classification:** Query filter lambda.

---

### 5. Contention Await Sibling Callback
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:98:21:ParenthesizedLambdaExpression`
- **Exact Line Range:** Line 98
- **Signature:** `async () => await FindExistingAsync(idempotencyKey, ct)`
- **Actual Behavior & Inputs:**
  Asynchronous polling callback passed to `BalanceLockContention.AwaitSiblingAsync`.
- **Classification:** Concurrency synchronization lambda.

---

### 6. `FindExistingAsync`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:108:5:MethodDeclaration`
- **Exact Line Range:** Lines 108–109
- **Signature:** `private Task<Persistence.Entities.TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct)`
- **Actual Behavior & Inputs:**
  Queries `_db.TreasuryMovements.AsNoTracking()` by `m.IdempotencyKey == idempotencyKey`.
- **Classification:** Database query helper.

---

### 7. Idempotency Key Filter Predicate (`m => m.IdempotencyKey == idempotencyKey`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:109:66:SimpleLambdaExpression`
- **Exact Line Range:** Line 109
- **Signature:** `m => m.IdempotencyKey == idempotencyKey`
- **Actual Behavior & Inputs:**
  Filters `TreasuryMovements` by idempotency key string.
- **Classification:** Query filter lambda.

---

### 8. `ToResult`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/DepositHandler.cs:111:5:MethodDeclaration`
- **Exact Line Range:** Lines 111–112
- **Signature:** `private static DepositResult ToResult(Persistence.Entities.TreasuryMovementEntity entity, bool wasReplayed)`
- **Actual Behavior & Inputs:**
  Maps `TreasuryMovementEntity` to `DepositResult(Id, Type, AmountPiastres, DestinationAccountId, WasReplayed)`.
- **Classification:** Pure mapping helper.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-GATE-001`** | **MEDIUM** | `DepositHandler.cs:58` | `BusinessDayGate.RequireOpenAsync` executes outside transaction with `AsNoTracking()`. Deposits can commit against a business day that closed mid-flight. | Re-validate business day status inside the transaction using `SELECT ... WITH (UPDLOCK)` on `BusinessDays` before committing movements. |
