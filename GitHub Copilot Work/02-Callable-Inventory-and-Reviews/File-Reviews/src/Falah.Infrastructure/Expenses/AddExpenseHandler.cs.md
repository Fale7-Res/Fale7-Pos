# Deep Per-Callable Source Audit: `AddExpenseHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs)
- **Role:** Operating Expense Recording & Cash Disbursement Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:35:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 10 (1 Constructor, 2 Methods, 7 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `AddExpenseHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:29:5:ConstructorDeclaration`
- **Signature:** `public AddExpenseHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Line Span:** Lines 29–34
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via DI. Assigns them to private fields.
- **Callers, Dependencies & Side Effects:**
  Invoked by ASP.NET Core DI container when resolving endpoint dependencies.
- **Financial Integrity Assessment:**
  Ensures that approval registry and tamper-evident audit logger are wired into every expense disbursement.
- **Classification:** Non-trivial infrastructure wiring.

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:36:5:MethodDeclaration`
- **Signature:** `public async Task<AddExpenseResult> HandleAsync(AddExpenseRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 36–151
- **Actual Behavior & Inputs:**
  Receives `AddExpenseRequest` containing `RequestId`, `ShiftId`, `PaymentAccountId`, `Category`, `AmountPiastres`, `Description`, `CreatedByUserId`, `ManagerApprovalId`, `ManagerApproverUserId`.
  Execution pipeline:
  1. Computes deterministic `expenseId = $"exp-{request.RequestId}"` and performs idempotent replay check.
  2. Asserts account is in `TreasuryAccounts.Protected` (`tre-main`, `tre-drawer`, `tre-bank`, `tre-instapay`).
  3. Validates positive amount and non-empty category/description.
  4. Resolves creating user; asserts active shift exists and is `Status == 'Open'`.
  5. Authorizes expense via `AuthorizationPolicy.AuthorizeExpense`. (Drawer expenses allow manager PIN approval; main treasury expenses require direct operator permission).
  6. Enforces open business day via `BusinessDayGate.RequireOpenAsync`.
  7. **Transaction & Concurrency Control:** Starts database transaction at line 70.
  8. Inside transaction, invokes `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` with `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` and asserts protected account balance via `FinancialRules.AssertProtectedSourceBalance`.
  9. Starts journal entry: `journal.TryStart($"expense-{expenseId}", "expense")`.
  10. Posts `TreasuryMovement` (`expense_payment` out of `PaymentAccountId`).
  11. Inserts `ExpenseEntity` and logs `AuditEvent` (critical if >= `ControlCenterRules.LargeExpenseThresholdPiastres`).
  12. Completes journal entry and queues an Expense voucher print job.
  13. Commits transaction and returns `AddExpenseResult`.
  14. Catches `DbUpdateException`, `ExpenseConflictException`, and `BalanceLockContention`. Replays if already committed or awaits concurrent sibling commit.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/expenses` endpoint, Cashier UI "تسجيل مصروف" dialog.
  - Side effects: Writes to `Expenses`, `TreasuryMovements`, `FinancialJournalEntries`, `AuditEvents`, `PrintJobs`. Deducts liquid cash from drawer or main treasury.
- **Financial Concurrency Analysis:**
  - **Pessimistic Balance Locking:** Unlike `OpenShiftHandler` and `ReceivePurchaseHandler`, `AddExpenseHandler` correctly initiates `BeginTransactionAsync` (line 70) **BEFORE** calling `LoadAccountMovementsForUpdateAsync` (line 77). The `UPDLOCK, HOLDLOCK` locks are held until transaction commit, successfully preventing concurrent account overdraft!
  - **Shift State Validation Gap:** Lines 53–55 read `shiftEntity.Status == 'Open'` outside the transaction with `AsNoTracking()`. If a manager closes the shift and completes cash handover concurrently, an expense can be paid from `tre-drawer` after handover has already drained the till, leaving a negative physical drawer balance upon restart.
- **Tests & Missing Regressions:**
  - Existing: `ExpensesVerticalSliceTests.cs:AddExpense_WithValidDrawer_DebitsDrawerAndCreatesMovement`.
  - Missing: Concurrency test racing `AddExpenseHandler` against `CloseShiftHandler` to verify whether drawer expenses can post after shift closure begins.
- **Classification:** Core financial orchestration method with proper balance locking but pre-transaction shift check gap.

---

### 3. Replay Check Lambda (`e => e.Id == expenseId`)
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:39:78:SimpleLambdaExpression`
- **Signature:** `e => e.Id == expenseId`
- **Line Span:** Line 39
- **Actual Behavior & Inputs:**
  Predicate for `_db.Expenses.AsNoTracking().FirstOrDefaultAsync(...)` checking if `expenseId` already exists.
- **Financial Integrity Assessment:**
  Prevents duplicate expense disbursement on network retries before acquiring locks.
- **Classification:** Idempotency filter predicate.

---

### 4. User Lookup Lambda (`u => u.Id == request.CreatedByUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:49:80:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.CreatedByUserId`
- **Line Span:** Line 49
- **Actual Behavior & Inputs:**
  Loads user entity by ID to construct `Actor` for authorization.
- **Classification:** Query filter lambda.

---

### 5. Shift Lookup Lambda (`s => s.Id == request.ShiftId`)
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:53:79:SimpleLambdaExpression`
- **Signature:** `s => s.Id == request.ShiftId`
- **Line Span:** Line 53
- **Actual Behavior & Inputs:**
  Filters `_db.Shifts.AsNoTracking()` to check existence and ensure `Status == 'Open'`.
- **Financial Integrity Assessment:**
  Pre-transaction check. Subject to race condition if shift closes concurrently.
- **Classification:** State invariant validation predicate.

---

### 6. Manager Approver Lookup Lambda (`u => u.Id == request.ManagerApproverUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:60:83:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Line Span:** Line 60
- **Actual Behavior & Inputs:**
  Loads manager user entity to validate approval signature for drawer expense authorization.
- **Classification:** Query filter lambda.

---

### 7. Catch Block Replay Lambda (`e => e.Id == expenseId`)
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:137:80:SimpleLambdaExpression`
- **Signature:** `e => e.Id == expenseId`
- **Line Span:** Line 137
- **Actual Behavior & Inputs:**
  Re-queries for the expense upon catching `DbUpdateException` to verify if a concurrent request with the same ID succeeded.
- **Classification:** Exception recovery predicate.

---

### 8. Contention Retry Delegate Lambda
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:143:21:ParenthesizedLambdaExpression`
- **Signature:** `async () => await _db.Expenses.AsNoTracking().FirstOrDefaultAsync(e => e.Id == expenseId, ct)`
- **Line Span:** Line 143
- **Actual Behavior & Inputs:**
  Asynchronous polling delegate passed to `BalanceLockContention.AwaitSiblingAsync`. Repeatedly attempts to load the expense if lock contention occurred.
- **Financial Integrity Assessment:**
  Prevents spurious 500 errors when identical requests arrive concurrently at microsecond intervals.
- **Classification:** Retry polling closure.

---

### 9. Contention Retry Inner Lambda (`e => e.Id == expenseId`)
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:143:87:SimpleLambdaExpression`
- **Signature:** `e => e.Id == expenseId`
- **Line Span:** Line 143
- **Actual Behavior & Inputs:**
  Inner filter predicate within the contention retry polling delegate.
- **Classification:** Predicate lambda inside retry closure.

---

### 10. `ToResult`
- **Symbol ID:** `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs:153:5:MethodDeclaration`
- **Signature:** `private static AddExpenseResult ToResult(ExpenseEntity entity, bool wasReplayed)`
- **Line Span:** Lines 153–155
- **Actual Behavior & Inputs:**
  Maps `ExpenseEntity` properties to `AddExpenseResult` DTO.
- **Financial Integrity Assessment:**
  Pure projection without side effects.
- **Classification:** Pure DTO mapping method.
