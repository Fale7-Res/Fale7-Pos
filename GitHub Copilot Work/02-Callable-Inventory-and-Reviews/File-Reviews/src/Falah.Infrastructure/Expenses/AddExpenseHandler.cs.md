# Deep Per-Callable Source Audit: `AddExpenseHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs)
- **Role:** Operational Expense Recording, Ledger Disbursement & Voucher Printing Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T01:20:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 10 (1 Constructor, 2 Methods, 7 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `AddExpenseHandler` Constructor
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:29:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 29–34
- **Signature:** `public AddExpenseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via DI constructor injection and assigns them to private readonly fields `_db`, `_approvalRegistry`, and `_auditLog`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by the ASP.NET Core DI container (scoped lifetime) during API route dispatch for `POST /api/expenses`.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:36:5:MethodDeclaration`
- **Exact Line Range:** Lines 36–151
- **Signature:** `public async Task<AddExpenseResult> HandleAsync(AddExpenseRequest request, CancellationToken ct = default)`
- **Actual Behavior & Inputs:**
  Receives `AddExpenseRequest(RequestId, Category, AmountPiastres, Description, ShiftId, PaymentAccountId, CreatedByUserId, ManagerApprovalId, ManagerApproverUserId)`. Orchestrates:
  1. *Fast Idempotency Pre-Check (Lines 38–40):* Derives `expenseId = $"exp-{request.RequestId}"`. Queries `_db.Expenses.AsNoTracking()`. If found, returns `ToResult(existing, wasReplayed: true)`.
  2. *Account & Request Invariants (Lines 42–47):* Determines `paidFromDrawer = request.PaymentAccountId == TreasuryAccounts.Drawer`. Enforces account is in `TreasuryAccounts.Protected` (`tre-drawer`, `tre-main`, `tre-bank`, `tre-instapay`). Validates positive amount, non-empty description, and non-empty category.
  3. *User & Shift Gates (Lines 49–56):* Fetches `createdByUser` via `_db.Users.AsNoTracking()`. Fetches `shiftEntity` via `_db.Shifts.AsNoTracking()`. Asserts `shiftEntity.Status == nameof(ShiftStatus.Open)`.
  4. *Authorization Branching (Lines 57–67):*
     - If `paidFromDrawer` and `ManagerApprovalId` provided, validates approval.
     - Calls `AuthorizationPolicy.AuthorizeExpense(_approvalRegistry, actor, paidFromDrawer, approval)`. For `tre-main`, manager approval fallback is disallowed by design; caller must hold `expenses_add_main`.
  5. *Pre-Transaction Business Day Gate (Line 69):* Executes `var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);` outside transaction.
  6. *Transaction Initiation (Line 70):* Begins explicit database transaction `await _db.Database.BeginTransactionAsync(ct)`.
  7. *Pessimistic Source Balance Lock (Lines 77–78):* Locks all movements of `request.PaymentAccountId` using `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`). Asserts source balance via `FinancialRules.AssertProtectedSourceBalance`.
  8. *Financial Journal Idempotency (Lines 80–83):* Derives `idempotencyKey = $"expense-{expenseId}"`. Executes `journal.TryStart(idempotencyKey, "expense")`. Throws `ExpenseConflictException` if duplicate.
  9. *Movement, Expense Entity & Print Job (Lines 85–128):*
     - Builds `TreasuryMovement` (`expense_payment`, source account -> null destination, tagged with `expenseId` and `request.ShiftId`).
     - Inserts `ExpenseEntity` with business date, category, amount, user, shift, and movement link.
     - Appends critical or normal audit event to `_auditLog` (critical if amount >= `LargeExpenseThresholdPiastres` [5,000 EGP]).
     - Completes journal (`journal.Complete(idempotencyKey)`).
     - Enqueues expense voucher `PrintJobEntity` via `DocumentPrintJobs.Build`.
  10. *Commit & Persistence (Lines 129–131):* Saves changes and commits transaction. Returns `ToResult(expenseEntity, wasReplayed: false)`.
  11. *Catch Block & Balance Contention Recovery (Lines 133–150):* On `DbUpdateException`, `ExpenseConflictException`, or `BalanceLockContention.IsContention(ex)`, rolls back transaction. Awaits sibling commit via `BalanceLockContention.AwaitSiblingAsync`. Returns replayed entity or throws `ExpenseConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/expenses` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`, `TreasuryAccounts`, `AuthorizationPolicy`, `BusinessDayGate`, `TreasuryBalanceQuery`, `FinancialRules`, `TreasuryMovementFactory`, `EfFinancialJournal`, `OrderMapper`, `DocumentPrintJobs`, `BalanceLockContention`.
  - Side effects: Modifies `Expenses`, `TreasuryMovements`, `FinancialJournals`, `PrintJobs`, `AuditEvents`. Deducts real funds from drawer or main treasury.
- **Critical Financial Integrity Flaws:**

  > [!WARNING]
  > **Vulnerability `FIN-GATE-001` (Business Day Gate Outside Transaction Scope):**
  > At line 69:
  > ```csharp
  > var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  > await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  > ```
  > `BusinessDayGate.RequireOpenAsync(_db, ct)` runs *outside* the transaction with `AsNoTracking()`. If a business day is closed concurrently during manager approval verification, the expense transaction proceeds and stamps the movement and expense entity with a closed business day ID.

  > [!WARNING]
  > **Shift Status Check Outside Transaction:**
  > At lines 53–55:
  > ```csharp
  > var shiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  >     ?? throw new InvalidOperationException("الوردية غير موجودة.");
  > if (shiftEntity.Status != nameof(ShiftStatus.Open)) throw new DomainException("يجب فتح وردية قبل تسجيل مصروف.");
  > ```
  > Like `CheckoutOrderHandler`, this check executes outside the transaction with `AsNoTracking()`. A concurrent shift closure between lines 55 and 70 allows drawer expenses to be committed against an already-closed shift, altering the closed shift's cash totals post-reconciliation.

- **Concurrency & Locking Assessment:**
  - Source account balance is locked with `UPDLOCK, HOLDLOCK` inside the transaction. Prevents concurrent expenses from draining the drawer or main treasury into a negative balance.
  - Sibling contention handling via `BalanceLockContention.AwaitSiblingAsync`.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/ExpensesVerticalSliceTests.cs` (verifies drawer expense, main treasury expense, approval authorization, balance exhaustion rejection, and print voucher creation).
  - Missing Regression: Concurrency test closing a shift concurrently while an expense is being authorized to verify that the expense cannot be attributed to a closed shift.
- **Classification:** Core expense disbursement orchestrator with pre-transaction gate gaps (`FIN-GATE-001`).

---

### 3. Expense Pre-Check Lookup Predicate (`e => e.Id == expenseId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:39:78:SimpleLambdaExpression`
- **Exact Line Range:** Line 39
- **Signature:** `e => e.Id == expenseId`
- **Actual Behavior & Inputs:**
  Expression predicate for fast idempotent replay lookup on `_db.Expenses.AsNoTracking()`.
- **Classification:** Query filter lambda.

---

### 4. Creator User Lookup Predicate (`u => u.Id == request.CreatedByUserId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:49:80:SimpleLambdaExpression`
- **Exact Line Range:** Line 49
- **Signature:** `u => u.Id == request.CreatedByUserId`
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to load the user submitting the expense.
- **Classification:** Query filter lambda.

---

### 5. Shift Lookup Predicate (`s => s.Id == request.ShiftId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:53:79:SimpleLambdaExpression`
- **Exact Line Range:** Line 53
- **Signature:** `s => s.Id == request.ShiftId`
- **Actual Behavior & Inputs:**
  Queries `_db.Shifts.AsNoTracking()` to verify that the shift exists and is currently open.
- **Classification:** Query filter lambda.

---

### 6. Manager Approver Lookup Predicate (`u => u.Id == request.ManagerApproverUserId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:60:83:SimpleLambdaExpression`
- **Exact Line Range:** Line 60
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to load the approver user when manager approval is supplied for drawer expenses.
- **Classification:** Query filter lambda.

---

### 7. Catch Block Replay Predicate (`e => e.Id == expenseId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:137:80:SimpleLambdaExpression`
- **Exact Line Range:** Line 137
- **Signature:** `e => e.Id == expenseId`
- **Actual Behavior & Inputs:**
  Filters `_db.Expenses.AsNoTracking()` in the catch block to determine if a sibling transaction already committed this expense.
- **Classification:** Query filter lambda.

---

### 8. Contention Await Sibling Callback
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:143:21:ParenthesizedLambdaExpression`
- **Exact Line Range:** Line 143
- **Signature:** `async () => await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct)`
- **Actual Behavior & Inputs:**
  Asynchronous polling callback passed to `BalanceLockContention.AwaitSiblingAsync`.
- **Classification:** Concurrency synchronization lambda.

---

### 9. Sibling Await Polling Predicate (`e => e.Id == expenseId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:143:87:SimpleLambdaExpression`
- **Exact Line Range:** Line 143
- **Signature:** `e => e.Id == expenseId`
- **Actual Behavior & Inputs:**
  Predicate used inside `AwaitSiblingAsync` loop.
- **Classification:** Query filter lambda.

---

### 10. `ToResult`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:153:5:MethodDeclaration`
- **Exact Line Range:** Lines 153–155
- **Signature:** `private static AddExpenseResult ToResult(ExpenseEntity entity, bool wasReplayed)`
- **Actual Behavior & Inputs:**
  Maps `ExpenseEntity` to `AddExpenseResult(Id, Category, AmountPiastres, PaymentAccountId, PaidFromDrawer, TreasuryMovementId, WasReplayed)`.
- **Classification:** Pure mapping helper.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-GATE-001`** | **MEDIUM** | `AddExpenseHandler.cs:69` | `BusinessDayGate.RequireOpenAsync` executes outside transaction with `AsNoTracking()`. Expenses can commit against a business day that closed mid-flight. | Re-validate business day status inside the transaction using `SELECT ... WITH (UPDLOCK)` on `BusinessDays` before committing movements. |
| **`FIN-SHIFT-002`** | **MEDIUM** | `AddExpenseHandler.cs:53-55` | Shift open status is verified outside transaction with `AsNoTracking()`. Concurrent shift closure allows expenses to be attributed to a closed shift. | Re-validate shift status inside transaction with `SELECT ... WITH (UPDLOCK)` on `Shifts`. |

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-26T02:50:00Z
- **Source SHA-256 Verified:** `02f51c8d0598d6772894850b3f02638a5cf57769d75033a62d78a9cf7a69f3a1`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Independent Code-Level Findings & Per-Callable Rationale:**
  1. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:29:5:ConstructorDeclaration`:
     - *Verified Lines 29–34*: DI constructor injecting `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog`. Assigns private readonly fields, no mutations or side effects. Verdict: **ACCEPTED**.
  2. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:36:5:MethodDeclaration`:
     - *Verified Lines 36–151*: `HandleAsync(AddExpenseRequest, CancellationToken)` orchestrates expense disbursement and financial ledger posting.
     - *Pre-Checks & Validation*: Idempotency pre-check via `Expenses` using `exp-{request.RequestId}`. Verifies protected account membership (`tre-drawer`, `tre-main`, `tre-bank`, `tre-instapay`). Enforces positive amount, non-empty description, and non-empty category.
     - *Authorization Gates*: Differentiates drawer vs main treasury expenses: main treasury expenses require direct `expenses_add_main` permission with no approval fallback; drawer expenses permit manager approval fallback via `expenses_add_drawer`.
     - *Concurrency & Financial Balance Integrity*: Explicit transaction begins at line 70. Acquires pessimistic lock via `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`) preventing concurrent overdraft. Financial journal idempotency enforced via `EfFinancialJournal.TryStart`. Records `TreasuryMovement`, `ExpenseEntity`, audit event, and voucher `PrintJobEntity`.
     - *Verified Findings Confirmed*:
       - `FIN-GATE-001` (Medium): `BusinessDayGate.RequireOpenAsync` executes outside transaction at line 69 with `AsNoTracking()`. Concurrent business day closure allows expense attribution to closed day.
       - `FIN-SHIFT-002` (Medium): Shift open status checked at lines 53–55 outside transaction.
     - *Contention Resilience*: Catch block intercepts `BalanceLockContention` and uses `AwaitSiblingAsync` to await concurrent sibling commit before failing.
     - Verdict: **ACCEPTED** (with verified architectural findings `FIN-GATE-001` and `FIN-SHIFT-002`).
  3. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:39:78:SimpleLambdaExpression`:
     - *Verified Line 39*: `e => e.Id == expenseId` on `_db.Expenses.AsNoTracking()`. Idempotency pre-check lookup. Verdict: **ACCEPTED**.
  4. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:49:80:SimpleLambdaExpression`:
     - *Verified Line 49*: `u => u.Id == request.CreatedByUserId` on `_db.Users.AsNoTracking()`. Submitting user lookup. Verdict: **ACCEPTED**.
  5. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:53:79:SimpleLambdaExpression`:
     - *Verified Line 53*: `s => s.Id == request.ShiftId` on `_db.Shifts.AsNoTracking()`. Shift lookup. Verdict: **ACCEPTED**.
  6. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:60:83:SimpleLambdaExpression`:
     - *Verified Line 60*: `u => u.Id == request.ManagerApproverUserId` on `_db.Users.AsNoTracking()`. Approver lookup. Verdict: **ACCEPTED**.
  7. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:137:80:SimpleLambdaExpression`:
     - *Verified Line 137*: `e => e.Id == expenseId` on `_db.Expenses.AsNoTracking()`. Catch-block replay check. Verdict: **ACCEPTED**.
  8. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:143:21:ParenthesizedLambdaExpression`:
     - *Verified Line 143*: `async () => await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct)` polling delegate passed to `BalanceLockContention.AwaitSiblingAsync`. Verdict: **ACCEPTED**.
  9. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:143:87:SimpleLambdaExpression`:
     - *Verified Line 143*: `e => e.Id == expenseId` query predicate within polling delegate. Verdict: **ACCEPTED**.
  10. `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:153:5:MethodDeclaration`:
      - *Verified Lines 153–155*: `ToResult(ExpenseEntity entity, bool wasReplayed)` maps entity fields to immutable `AddExpenseResult`. Zero side effects. Verdict: **ACCEPTED**.

