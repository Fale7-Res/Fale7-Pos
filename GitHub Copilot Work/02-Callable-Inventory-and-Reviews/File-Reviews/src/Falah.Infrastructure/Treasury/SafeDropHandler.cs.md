# Substantive Callable Audit: `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `601f29ab9abe3dd644137bbf20383cf21ab60e040ff79c55a5b2c37cff292b76`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **9**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

`SafeDropHandler` handles the skimming of surplus cash from the cashier drawer into the restaurant safe (`tre-safe`) mid-shift. It enforces the `safe_drop` permission, validates positive amount, requires an active open business day, and records an append-only `TreasuryMovement` under database transaction locking.

### Critical Vulnerability Tracing: Finding `FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`
On lines 45-46:
```csharp
_ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
    ?? throw new InvalidOperationException("الوردية غير موجودة.");
```
The handler verifies that the referenced `Shift` exists, but does **NOT** check that `s.Status == ShiftStatus.Open`.
If a safe drop command commits after a shift is already closed (due to network delay, terminal retry, or operator mistake), the cash movement is attributed to a closed shift. This retroactively mutates the financial totals of the closed shift and causes discrepancies on reprinted handover slips.

*(Note: Prior draft mistakenly cited FIN-013, which belongs to inventory queries in `OwnerDashboardQueryService`. Correct canonical finding is `FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`.)*

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

### Callable 1: `SafeDropHandler(...)` (Constructor)
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:26:31:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `26-31`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`
- **A. Actual Behavior & Execution Path:** Injects `FalahDbContext`, `IApprovalRegistry`, and `IAuditLog`.
- **B. Trust Boundaries & Validation:** Internal DI container.
- **C. State Mutations & Side Effects:** Assigns private fields. Zero external state mutations.
- **D. Concurrency & Transaction Implications:** Thread-safe constructor.
- **E. Specialist Risks & Invariants:** None.

```csharp
    public SafeDropHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
    {
        _db = db;
        _approvalRegistry = approvalRegistry;
        _auditLog = auditLog;
    }
```

---

### Callable 2: `HandleAsync(SafeDropRequest request, CancellationToken ct)`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:33:117:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `33-117`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`**
- **A. Actual Behavior & Execution Path:**
  1. Checks idempotency replay via `FindExistingAsync("safe-drop-" + request.DropRequestId, ct)`.
  2. Asserts amount > 0 and reason is non-empty.
  3. Validates creator user, resolves manager approval if required, and authorizes command.
  4. Requires active business day via `BusinessDayGate.RequireOpenAsync`.
  5. Opens transaction. Loads drawer movements for update via `TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync`.
  6. Asserts drawer has sufficient balance (`AssertProtectedSourceBalance`).
  7. Inserts `TreasuryMovement` (`tre-drawer` -> `tre-safe`) and appends audit log event.
  8. Commits transaction and returns `SafeDropResult`.
- **B. Trust Boundaries & Validation:** Receives `SafeDropRequest` from cashier till.
- **C. State Mutations & Side Effects:** Inserts `TreasuryMovementEntity`, inserts `ControlAuditEventEntity`, commits EF Core transaction.
- **D. Concurrency & Transaction Implications:** Uses row-locking on account balance to prevent concurrent drawer balance overdrafts. Catches lock contention and retries sibling replay.
- **E. Specialist Risks & Invariants:**
  Does not check `s.Status == ShiftStatus.Open`. Allows post-close cash movements to attach to a closed shift.

```csharp
    public async Task<SafeDropResult> HandleAsync(SafeDropRequest request, CancellationToken ct = default)
    {
        var idempotencyKey = $"safe-drop-{request.DropRequestId}";
        var existing = await FindExistingAsync(idempotencyKey, ct);
        if (existing is not null) return ToResult(existing, wasReplayed: true);

        if (request.AmountPiastres <= 0) throw new DomainException("مبلغ الإيداع الآمن يجب أن يكون أكبر من صفر.");
        if (string.IsNullOrWhiteSpace(request.Reason)) throw new DomainException("سبب الإيداع الآمن مطلوب.");

        var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
            ?? throw new InvalidOperationException("المستخدم غير موجود.");
        var actor = OrderMapper.ToActor(createdByUser);
        _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
            ?? throw new InvalidOperationException("الوردية غير موجودة.");

        ManagerApproval? approval = null;
        if (!string.IsNullOrEmpty(request.ManagerApprovalId))
        {
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
                ?? throw new DomainException("المعتمد غير موجود.");
            approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
                approverUser.Role, DateTimeOffset.UtcNow, "safe_drop", request.Reason);
        }
        var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "safe_drop", approval, requireApproval: true);

        var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
        await using var transaction = await _db.Database.BeginTransactionAsync(ct);
        try
        {
            // Read under a lock, inside the transaction, immediately before spending.
            // Outside it, concurrent commands each read the same balance, each decide it covers
            // them, and each write - which is how an account holding 30 paid out three separate
            // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
            var drawerMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, TreasuryAccounts.Drawer, ct);
            FinancialRules.AssertProtectedSourceBalance(drawerMovements, TreasuryAccounts.Drawer, Money.FromPiastres(request.AmountPiastres));

            var journal = new EfFinancialJournal(_db);
            if (!journal.TryStart(idempotencyKey, "safe_drop"))
                throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");

            var now = DateTimeOffset.UtcNow;
            var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
                $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "safe_drop",
                // The destination is the whole point of a safe drop: cash leaves the till and goes
                // INTO the main treasury. Passing null here meant the drawer fell and nothing rose -
                // the money left the books entirely - while the audit line below said, and still
                // says, that it went to the main treasury. A movement that disagrees with its own
                // audit record is worse than either being wrong alone.
                TreasuryAccounts.Drawer, TreasuryAccounts.Main, Money.FromPiastres(request.AmountPiastres),
                "safe_drop", request.DropRequestId, businessDayId, request.ShiftId,
                request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);

            var entity = OrderMapper.ToEntity(movement);
            _db.TreasuryMovements.Add(entity);
            journal.Complete(idempotencyKey);

            _auditLog.Append(new AuditEvent(
                $"audit-{Guid.NewGuid():N}", createdByUser.Id, createdByUser.Name, createdByUser.Role,
                "إيداع آمن", "cash", $"إيداع {request.AmountPiastres} قرش من الدرج إلى الخزينة الرئيسية - {request.Reason}",
                "normal", now, ShiftId: request.ShiftId, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
                OperationId: movement.OperationId, ReferenceId: entity.Id));

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return ToResult(entity, wasReplayed: false);
        }
        catch (Exception ex) when (ex is DbUpdateException or TreasuryConflictException
                                   || Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
        {
            await transaction.RollbackAsync(ct);
            var replay = await FindExistingAsync(idempotencyKey, ct);
            // The loser of a balance-lock race carries the same request id as the sibling that
            // won it, and that sibling may not have committed yet at this instant. Looking once
            // and giving up turns "somebody else is doing it" into an error.
            if (replay is null && Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                replay = await Falah.Infrastructure.Treasury.BalanceLockContention.AwaitSiblingAsync(
                    async () => await FindExistingAsync(idempotencyKey, ct), ct);
            if (replay is not null) return ToResult(replay, wasReplayed: true);
            // Losing the balance-lock race wrote nothing at all, so telling the operator to go and
            // review a half-finished movement would send them looking for something that is not there.
            if (Falah.Infrastructure.Treasury.BalanceLockContention.IsContention(ex))
                throw new TreasuryConflictException(Falah.Infrastructure.Treasury.BalanceLockContention.RetryMessage);
            throw new TreasuryConflictException("تعذر تسجيل الإيداع الآمن بسبب تعارض في مفتاح منع التكرار، ولا يوجد إيداع سابق لإعادته - يحتاج مراجعة يدوية.");
        }
    }
```

---

### Callable 3: Lambda in `HandleAsync` (Line 42)
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:42:42:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `42-42`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:** EF Core query predicate `u => u.Id == request.CreatedByUserId`.
- **B. Trust Boundaries & Validation:** Internal database lookup.
- **C. State Mutations & Side Effects:** Pure predicate.
- **D. Concurrency & Transaction Implications:** AsNoTracking query.
- **E. Specialist Risks & Invariants:** None.

```csharp
        var createdByUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CreatedByUserId, ct)
```

---

### Callable 4: Lambda in `HandleAsync` (Line 45)
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:45:45:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `45-45`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001`**
- **A. Actual Behavior & Execution Path:** EF Core query predicate `s => s.Id == request.ShiftId`.
- **B. Trust Boundaries & Validation:** Internal database lookup.
- **C. State Mutations & Side Effects:** Pure predicate.
- **D. Concurrency & Transaction Implications:** Does not check shift open status or lock shift row.
- **E. Specialist Risks & Invariants:** Permits safe drops on closed shifts.

```csharp
        _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
```

---

### Callable 5: Lambda in `HandleAsync` (Line 51)
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:51:51:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `51-51`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:** EF Core query predicate `u => u.Id == request.ManagerApproverUserId`.
- **B. Trust Boundaries & Validation:** Internal database lookup.
- **C. State Mutations & Side Effects:** Pure predicate.
- **D. Concurrency & Transaction Implications:** AsNoTracking query.
- **E. Specialist Risks & Invariants:** None.

```csharp
            var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
```

---

### Callable 6: Lambda in `HandleAsync` (Line 109)
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:109:109:ParenthesizedLambdaExpression`
- **Declaration Kind:** `ParenthesizedLambdaExpression` | **Line Range:** `109-109`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`
- **A. Actual Behavior & Execution Path:** Async closure delegate passed to `BalanceLockContention.AwaitSiblingAsync`.
- **B. Trust Boundaries & Validation:** Internal concurrency helper.
- **C. State Mutations & Side Effects:** Re-executes `FindExistingAsync`.
- **D. Concurrency & Transaction Implications:** Awaits sibling transaction commit during balance lock race.
- **E. Specialist Risks & Invariants:** None.

```csharp
                    async () => await FindExistingAsync(idempotencyKey, ct), ct);
```

---

### Callable 7 & 8: `FindExistingAsync(string, CancellationToken)` & Lambda
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:119:120:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `119-120`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`
- **A. Actual Behavior & Execution Path:** Idempotency query checking `_db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct)`.
- **B. Trust Boundaries & Validation:** Internal database lookup.
- **C. State Mutations & Side Effects:** None; read-only query.
- **D. Concurrency & Transaction Implications:** Thread-safe.
- **E. Specialist Risks & Invariants:** None.

```csharp
    private Task<TreasuryMovementEntity?> FindExistingAsync(string idempotencyKey, CancellationToken ct) =>
        _db.TreasuryMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
```

---

### Callable 9: `ToResult(TreasuryMovementEntity, bool)`
- **Callable ID:** `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs:122:123:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `122-123`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`
- **A. Actual Behavior & Execution Path:** Pure projection method constructing `SafeDropResult(entity.Id, entity.AmountPiastres, wasReplayed)`.
- **B. Trust Boundaries & Validation:** Internal entity to DTO boundary.
- **C. State Mutations & Side Effects:** None.
- **D. Concurrency & Transaction Implications:** Thread-safe pure function.
- **E. Specialist Risks & Invariants:** None.

```csharp
    private static SafeDropResult ToResult(TreasuryMovementEntity entity, bool wasReplayed) =>
        new(entity.Id, entity.AmountPiastres, wasReplayed);
```
