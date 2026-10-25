# Deep Per-Callable Source Audit: `SafeDropHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs)
- **Role:** Mid-Shift Cash Drawer Skim & Safe Drop Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T01:15:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 9 (1 Constructor, 3 Methods, 5 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `SafeDropHandler` Constructor
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:26:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 26–31
- **Signature:** `public SafeDropHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)`
- **Actual Behavior & Inputs:**
  Receives `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog` via dependency injection and stores them into private readonly fields `_db`, `_approvalRegistry`, and `_auditLog`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by the ASP.NET Core DI container (scoped lifetime) during API route dispatch for `POST /api/treasury/safe-drop`.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`.
  - Side effects: None.
- **Classification:** Infrastructure wiring constructor.

---

### 2. `HandleAsync`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:33:5:MethodDeclaration`
- **Exact Line Range:** Lines 33–117
- **Signature:** `public async Task<SafeDropResult> HandleAsync(SafeDropRequest request, CancellationToken ct = default)`
- **Actual Behavior & Inputs:**
  Receives `SafeDropRequest(DropRequestId, ShiftId, AmountPiastres, Reason, CreatedByUserId, ManagerApprovalId, ManagerApproverUserId)`. Orchestrates:
  1. *Idempotency Pre-Check (Lines 35–37):* Derives `idempotencyKey = $"safe-drop-{request.DropRequestId}"`. Queries `FindExistingAsync`. Returns `ToResult(existing, wasReplayed: true)` if found.
  2. *Amount & Reason Invariants (Lines 39–40):* Asserts amount > 0 and non-empty reason.
  3. *User & Shift Existence Lookup (Lines 42–46):* Queries `Users` for `request.CreatedByUserId`. Queries `Shifts` for `request.ShiftId` — asserts existence ONLY, intentionally not asserting open status.
  4. *Actor Authorization & Manager Approval Gate (Lines 48–56):* If `request.ManagerApprovalId` is provided, fetches approver and validates approval. Invokes `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "safe_drop", approval, requireApproval: true)`.
  5. *Business Day Gate (Line 58):* Calls `BusinessDayGate.RequireOpenAsync(_db, ct)` outside transaction.
  6. *Transaction Initiation (Line 59):* Begins explicit database transaction `await _db.Database.BeginTransactionAsync(ct)`.
  7. *Pessimistic Drawer Balance Lock (Lines 66–67):* Locks all movements touching `tre-drawer` using `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` (`SELECT ... WITH (UPDLOCK, HOLDLOCK)`). Asserts available drawer cash via `FinancialRules.AssertProtectedSourceBalance`.
  8. *Financial Journal Idempotency (Lines 69–71):* Calls `EfFinancialJournal.TryStart(idempotencyKey, "safe_drop")`. Throws `TreasuryConflictException` if duplicate.
  9. *Movement & Audit Log Entities (Lines 73–93):*
     - Constructs cash lifecycle movement transfering money from `tre-drawer` to `tre-main` with `ShiftId = request.ShiftId`.
     - Appends `TreasuryMovementEntity` to `_db.TreasuryMovements`.
     - Completes financial journal (`journal.Complete(idempotencyKey)`).
     - Appends audit event to `_auditLog`.
  10. *Commit & Persistence (Lines 95–97):* Saves changes and commits transaction. Returns `ToResult(entity, wasReplayed: false)`.
  11. *Catch Block & Balance Contention Recovery (Lines 99–116):* On `DbUpdateException`, `TreasuryConflictException`, or `BalanceLockContention.IsContention(ex)`, rolls back transaction. Awaits sibling commit via `BalanceLockContention.AwaitSiblingAsync`. Returns replayed entity or throws `TreasuryConflictException`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/treasury/safe-drop` endpoint.
  - Dependencies: `FalahDbContext`, `IApprovalRegistry`, `IAuditLog`, `AuthorizationPolicy`, `BusinessDayGate`, `TreasuryBalanceQuery`, `FinancialRules`, `TreasuryMovementFactory`, `EfFinancialJournal`, `OrderMapper`, `BalanceLockContention`.
  - Side effects: Inserts into `TreasuryMovements`, `FinancialJournals`, `AuditEvents`. Decreases `tre-drawer` balance and increases `tre-main` balance.
- **Critical Financial Integrity Flaws:**

  > [!CRITICAL]
  > **Vulnerability `FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001` (Safe Drop Retroactive Attribution to Closed Shifts):**
  > At lines 17–19 and 45–46:
  > ```csharp
  > /// The shift referenced only needs to exist (a referential-integrity
  > /// check, matching how other handlers validate foreign ids) - the reference app does not itself
  > /// require that shift still be open, and this does not invent a stricter rule than that.
  > ...
  > _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  >     ?? throw new InvalidOperationException("الوردية غير موجودة.");
  > ```
  > And lines 81–83:
  > ```csharp
  > TreasuryAccounts.Drawer, TreasuryAccounts.Main, Money.FromPiastres(request.AmountPiastres),
  > "safe_drop", request.DropRequestId, businessDayId, request.ShiftId, ...
  > ```
  > **FLAW MECHANISM:**
  > 1. `SafeDropHandler` explicitly verifies *only* that the shift exists in the database. It **never asserts** `shift.Status == nameof(ShiftStatus.Open)`.
  > 2. If a cashier or manager submits a safe drop referencing a shift that was already closed (e.g. Shift 1, closed hours ago):
  >    - Physical cash is removed from the *current* operational drawer.
  >    - The resulting `TreasuryMovement` is inserted with `ShiftId = Shift 1`.
  >    - `tre-drawer` overall balance decreases and `tre-main` balance increases.
  > 3. **Disastrous Impact on Shift Reconciliations:**
  >    - In [`ShiftReconciliation.cs:22-26`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L22-L26), drawer reconciliation sums movements where `m.ShiftId == shiftId`.
  >    - For the already-closed Shift 1, its expected drawer cash is retroactively reduced by `request.AmountPiastres`. If Shift 1's report is re-read or reprinted, its historical cash numbers no longer match the physical cash counted and signed off at shift close!
  >    - For the *currently active* Shift 2 (where the cashier actually took money out of the till), Shift 2's reconciliation does **not** include this safe drop because `movement.ShiftId` was stamped as Shift 1. When the Shift 2 cashier closes their shift, their counted cash is short by `request.AmountPiastres`, resulting in an artificial cash shortage and false discrepancy penalty against the innocent cashier!
  > 4. **Remediation:** Enforce that the referenced shift must be currently open:
  >    ```csharp
  >    var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  >        ?? throw new InvalidOperationException("الوردية غير موجودة.");
  >    if (shift.Status != nameof(ShiftStatus.Open))
  >        throw new DomainException("لا يمكن تسجيل إيداع آمن لوردية مغلقة. يجب أن تكون الوردية مفتوحة.");
  >    ```

  > [!WARNING]
  > **Vulnerability `FIN-GATE-001` (Business Day Gate Outside Transaction Scope):**
  > At line 58:
  > ```csharp
  > var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  > await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  > ```
  > `BusinessDayGate.RequireOpenAsync` executes outside the transaction with `AsNoTracking()`. If a business day is closed concurrently during manager approval evaluation, the safe drop transaction proceeds to insert a movement stamped with a closed business day ID.

- **Concurrency & Locking Assessment:**
  - Source drawer balance is locked via `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync` inside the transaction. Prevents multiple concurrent drops from driving the drawer balance negative.
  - Sibling contention handling via `BalanceLockContention.AwaitSiblingAsync`.
- **Tests & Missing Regressions:**
  - Existing: `tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs:A_safe_drop_moves_the_cash_into_the_main_treasury_rather_than_out_of_the_books`.
  - Missing Regression: Integration test executing a safe drop against an already-closed shift to prove retroactive alteration of closed shift reconciliation (`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`).
- **Classification:** Core drawer management handler with critical closed-shift attribution defect (`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`).

---

### 3. Payer User Lookup Predicate (`u => u.Id == request.CreatedByUserId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:42:80:SimpleLambdaExpression`
- **Exact Line Range:** Line 42
- **Signature:** `u => u.Id == request.CreatedByUserId`
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to retrieve user executing the drop.
- **Classification:** Query filter lambda.

---

### 4. Shift Lookup Predicate (`s => s.Id == request.ShiftId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:45:65:SimpleLambdaExpression`
- **Exact Line Range:** Line 45
- **Signature:** `s => s.Id == request.ShiftId`
- **Actual Behavior & Inputs:**
  Queries `_db.Shifts.AsNoTracking()` to check shift existence without validating status (`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`).
- **Classification:** Query filter lambda.

---

### 5. Manager Approver Lookup Predicate (`u => u.Id == request.ManagerApproverUserId`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:51:83:SimpleLambdaExpression`
- **Exact Line Range:** Line 51
- **Signature:** `u => u.Id == request.ManagerApproverUserId`
- **Actual Behavior & Inputs:**
  Queries `_db.Users.AsNoTracking()` to load approver user.
- **Classification:** Query filter lambda.

---

### 6. Contention Await Sibling Callback
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:109:21:ParenthesizedLambdaExpression`
- **Exact Line Range:** Line 109
- **Signature:** `async () => await FindExistingAsync(idempotencyKey, ct)`
- **Actual Behavior & Inputs:**
  Asynchronous polling callback passed to `BalanceLockContention.AwaitSiblingAsync`.
- **Classification:** Concurrency synchronization lambda.

---

### 7. `FindExistingAsync`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:119:5:MethodDeclaration`
- **Exact Line Range:** Lines 119–120
- **Signature:** `private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct)`
- **Actual Behavior & Inputs:**
  Queries `_db.TreasuryMovements.AsNoTracking()` by `m.IdempotencyKey == idempotencyKey`.
- **Classification:** Database query helper.

---

### 8. Idempotency Key Filter Predicate (`m => m.IdempotencyKey == idempotencyKey`)
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:120:66:SimpleLambdaExpression`
- **Exact Line Range:** Line 120
- **Signature:** `m => m.IdempotencyKey == idempotencyKey`
- **Actual Behavior & Inputs:**
  Filters `TreasuryMovements` by idempotency key string.
- **Classification:** Query filter lambda.

---

### 9. `ToResult`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:122:5:MethodDeclaration`
- **Exact Line Range:** Lines 122–123
- **Signature:** `private static SafeDropResult ToResult(TreasuryMovementEntity entity, bool wasReplayed)`
- **Actual Behavior & Inputs:**
  Maps `TreasuryMovementEntity` to `SafeDropResult(Id, AmountPiastres, WasReplayed)`.
- **Classification:** Pure mapping helper.

---

## Comprehensive Vulnerability Summary & Fix Recommendations

| Finding ID | Severity | Root Cause Code Span | Impact | Recommended Remediation |
| :--- | :--- | :--- | :--- | :--- |
| **`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`** | **CRITICAL** | `SafeDropHandler.cs:17-19, 45-46` | Shift open status is deliberately ignored; safe drops can be stamped with a closed shift ID, falsifying closed shift reconciliations and creating false cash shortages in the active shift. | Require `shift.Status == nameof(ShiftStatus.Open)` and reject safe drop attempts against closed shifts. |
| **`FIN-GATE-001`** | **MEDIUM** | `SafeDropHandler.cs:58` | `BusinessDayGate.RequireOpenAsync` executes outside transaction with `AsNoTracking()`. Drops can commit against a business day that closed mid-flight. | Re-validate business day status inside the transaction using `SELECT ... WITH (UPDLOCK)` on `BusinessDays` before committing movements. |
