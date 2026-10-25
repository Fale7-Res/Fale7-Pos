# Deep Per-Callable Source Audit: `OpenShiftHandler.cs`

- **File Path:** [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs)
- **Role:** Shift Opening & Float Allocation Orchestrator
- **Author:** Financial & Database Integrity Specialist
- **Audit Date:** 2026-10-25T00:25:00Z
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Total In-Scope Callables:** 10 (1 Constructor, 2 Methods, 7 Lambdas)

---

## Callable Inventory & Detailed Behavioral Audit

### 1. `OpenShiftHandler` Constructor
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:31:5:ConstructorDeclaration`
- **Signature:** `public OpenShiftHandler(FalahDbContext db)`
- **Line Span:** Lines 31–31
- **Actual Behavior & Inputs:**
  Accepts the EF Core `FalahDbContext` instance via dependency injection and assigns it to the private readonly field `_db`.
- **Callers, Dependencies & Side Effects:**
  - Invoked by the ASP.NET Core DI container (scoped lifetime) when instantiating endpoint handlers in `Program.cs`.
  - Dependencies: `FalahDbContext`.
  - Side effects: None (pure assignment).
- **Trust Boundaries & Failure Modes:**
  Trust boundary is the internal application DI container. Throws `ArgumentNullException` if DI resolution fails or if `db` is null.
- **Financial Integrity Assessment:**
  Neutral. Sets up the persistence context for all subsequent shift transactions.
- **Tests & Verification:**
  Verified in integration test harnesses (`ShiftsVerticalSliceTests.cs`).
- **Classification:** Non-trivial infrastructure wiring (sets database boundary).

---

### 2. `HandleAsync`
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:33:5:MethodDeclaration`
- **Signature:** `public async Task<OpenShiftResult> HandleAsync(OpenShiftRequest request, CancellationToken ct = default)`
- **Line Span:** Lines 33–110
- **Actual Behavior & Inputs:**
  Receives `OpenShiftRequest` containing `ShiftName`, `OpenedByUserId`, `OpeningCashPiastres`, and `RequestId`. Orchestrates:
  1. Fast idempotent replay check via `OpenRequestId`.
  2. User existence verification and permission authorization (`AuthorizationPolicy.AuthorizeShiftOpen`).
  3. Pre-transaction check ensuring no shift is currently `Status == 'Open'`.
  4. Domain entity creation via `Shift.Open(...)`.
  5. If `OpeningCashPiastres > 0`: requires an open business day (`BusinessDayGate.RequireOpenAsync`) and checks source balance on `tre-main`.
  6. Begins explicit database transaction.
  7. Inserts `ShiftEntity` and optional `TreasuryMovement` (`shift_opening_float`, `tre-main` -> `tre-drawer`).
  8. Enqueues a `PrintJob` for the shift opening receipt.
  9. Commits the transaction and returns `OpenShiftResult`.
- **Callers, Dependencies & Side Effects:**
  - Callers: `POST /api/shifts/open` API endpoint, Cashier UI shift-opening dialog.
  - Dependencies: `FalahDbContext`, `AuthorizationPolicy`, `Shift`, `BusinessDayGate`, `TreasuryBalanceQuery`, `FinancialRules`, `TreasuryMovementFactory`, `DocumentPrintJobs`.
  - Side effects: Database writes to `Shifts`, `TreasuryMovements`, `PrintJobs`. Physical cash transfer from Main Treasury to Drawer.
- **Transaction & Locking Flaw Analysis (FIN-011):**
  > [!CRITICAL]
  > **Vulnerability `FIN-011` Confirmed:** Lines 53–54 execute:
  > ```csharp
  > var mainMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, TreasuryAccounts.Main, ct);
  > FinancialRules.AssertProtectedSourceBalance(mainMovements, TreasuryAccounts.Main, openingCash);
  > ```
  > This call takes SQL Server `WITH (UPDLOCK, ROWLOCK, HOLDLOCK)` locks, but executes **BEFORE** `await using var transaction = await _db.Database.BeginTransactionAsync(ct);` at line 69! Because it runs in autocommit mode outside any transaction, the locks are released immediately upon query completion. A concurrent transaction (such as an expense payment, supplier purchase payout, or another shift float) can drain `tre-main` between lines 54 and 69, resulting in negative account balances or overdraft.
- **Drawer & Ledger Integrity Risks:**
  - If `OpeningCashPiastres == 0`, no treasury movement is generated and no business day is required. This is by domain design, allowing zero-float shifts.
  - Double-open concurrency: If two requests race past the pre-check at line 42, the database filtered unique index on `Status='Open'` catches the loser at line 94, triggering rollback and catch-block replay/error translation.
- **Tests & Missing Regressions:**
  - Existing: `ShiftsVerticalSliceTests.cs:OpenShift_WithValidFloat_CreatesShiftAndTreasuryMovement`.
  - Missing: Concurrency test racing `OpenShiftHandler` against concurrent `AddExpenseHandler` spending from `tre-main` to prove lock gap `FIN-011`.
- **Classification:** Core orchestration method with critical financial concurrency vulnerability.

---

### 3. Replay Check Lambda (`s => s.OpenRequestId == request.RequestId`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:35:76:SimpleLambdaExpression`
- **Signature:** `s => s.OpenRequestId == request.RequestId`
- **Line Span:** Line 35
- **Actual Behavior & Inputs:**
  Predicate passed to EF Core `FirstOrDefaultAsync` on `_db.Shifts.AsNoTracking()`. Evaluates whether a shift with the specified client `RequestId` was already recorded.
- **Callers, Dependencies & Side Effects:**
  Invoked by EF Core SQL translation in `HandleAsync`. No side effects.
- **Trust Boundaries & Failure Modes:**
  Relies on database unique index on `OpenRequestId`. If matching row exists, `HandleAsync` short-circuits immediately.
- **Financial Integrity Assessment:**
  Protects against double-floating the drawer on network retries.
- **Classification:** Idempotency filter predicate.

---

### 4. User Auth Lookup Lambda (`u => u.Id == request.OpenedByUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:38:86:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.OpenedByUserId`
- **Line Span:** Line 38
- **Actual Behavior & Inputs:**
  Filters `_db.Users.AsNoTracking()` by `request.OpenedByUserId` to retrieve role and permission data.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `InvalidOperationException("المستخدم غير موجود.")` if not found.
- **Financial Integrity Assessment:**
  Enforces authentication before financial authorization.
- **Classification:** Query filter lambda.

---

### 5. Active Shift Pre-Check Lambda (`s => s.Status == nameof(ShiftStatus.Open)`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:42:54:SimpleLambdaExpression`
- **Signature:** `s => s.Status == nameof(ShiftStatus.Open)`
- **Line Span:** Line 42
- **Actual Behavior & Inputs:**
  Queries `_db.Shifts.AsNoTracking().AnyAsync(...)` to ensure no shift is currently open.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync`. Throws `ShiftAlreadyOpenException` if an open shift is detected.
- **Financial Integrity Assessment:**
  Fast-path rejection to prevent multiple open drawers. Does not eliminate race conditions without database unique constraint.
- **Classification:** State invariant validation predicate.

---

### 6. Float User Lookup Lambda (`u => u.Id == request.OpenedByUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:76:87:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.OpenedByUserId`
- **Line Span:** Line 76
- **Actual Behavior & Inputs:**
  Re-queries the `Users` table inside the transaction to get the user's name for `TreasuryMovement.CreatedBy`.
- **Callers, Dependencies & Side Effects:**
  Invoked inside `HandleAsync` transaction. Redundant read of previously fetched user.
- **Financial Integrity Assessment:**
  Ensures user provenance is recorded on the `TreasuryMovement`.
- **Classification:** Provenance data projection lambda.

---

### 7. Print Job User Lookup Lambda (`u => u.Id == request.OpenedByUserId`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:89:79:SimpleLambdaExpression`
- **Signature:** `u => u.Id == request.OpenedByUserId`
- **Line Span:** Line 89
- **Actual Behavior & Inputs:**
  Third lookup of `Users` table to populate the `openedBy` name for the physical shift open receipt.
- **Callers, Dependencies & Side Effects:**
  Invoked inside `HandleAsync` transaction.
- **Financial Integrity Assessment:**
  Ensures printed voucher displays the cashier's human-readable name.
- **Classification:** Audit metadata projection lambda.

---

### 8. Catch Block Replay Lambda (`s => s.OpenRequestId == request.RequestId`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:102:78:SimpleLambdaExpression`
- **Signature:** `s => s.OpenRequestId == request.RequestId`
- **Line Span:** Line 102
- **Actual Behavior & Inputs:**
  Executed in `catch (Exception ex)` when `DbUpdateException` occurs. Checks if the failed insert was due to a concurrent duplicate request with the same `OpenRequestId`.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync` catch block. Returns replay result if matching record exists.
- **Financial Integrity Assessment:**
  Guarantees idempotency recovery after database constraint collisions.
- **Classification:** Exception recovery predicate.

---

### 9. Catch Block Active Shift Lambda (`s => s.Status == nameof(ShiftStatus.Open)`)
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:106:58:SimpleLambdaExpression`
- **Signature:** `s => s.Status == nameof(ShiftStatus.Open)`
- **Line Span:** Line 106
- **Actual Behavior & Inputs:**
  Executed in catch block if replay was null. Verifies whether failure was caused by the filtered unique index on `Status = 'Open'`. If so, throws user-friendly `ShiftAlreadyOpenException`.
- **Callers, Dependencies & Side Effects:**
  Invoked in `HandleAsync` error path.
- **Financial Integrity Assessment:**
  Prevents unhandled database errors when concurrent shift opens collide.
- **Classification:** Error translation predicate.

---

### 10. `ToResult`
- **Symbol ID:** `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs:112:5:MethodDeclaration`
- **Signature:** `private static OpenShiftResult ToResult(ShiftEntity entity, bool wasReplayed)`
- **Line Span:** Lines 112–114
- **Actual Behavior & Inputs:**
  Accepts a `ShiftEntity` and `bool wasReplayed`. Constructs and returns `OpenShiftResult` DTO.
- **Callers, Dependencies & Side Effects:**
  Invoked by `HandleAsync` on normal completion and replay paths. Pure mapping function.
- **Financial Integrity Assessment:**
  Accurately conveys whether float was newly allocated or replayed.
- **Classification:** Pure DTO mapping method.
