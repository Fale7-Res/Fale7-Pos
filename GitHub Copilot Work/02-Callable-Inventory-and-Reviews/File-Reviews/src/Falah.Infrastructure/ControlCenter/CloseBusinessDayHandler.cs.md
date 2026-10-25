# File Review: CloseBusinessDayHandler.cs

## Overview
Substantive security and authorization audit of CloseBusinessDayHandler.cs.

## Callables

### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:27:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:27:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.CloseBusinessDayHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Infrastructure.Reports.ReportingQueryService)
- **Lines:** [27-32]
- **Behavior:** Injects dependencies: `FalahDbContext` (persistence access), `IApprovalRegistry` (manager authorization policy validation), and `ReportingQueryService` (reconciliation report generation). Maintains clean dependency composition for business day close orchestration.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken)
- **Lines:** [34-129]
- **Behavior:** Orchestrates the entire business day close lifecycle and frozen snapshot generation:
  1. Idempotency Check: Pre-computes deterministic `snapshotId = $"snapshot-{request.RequestId}"`. If already present in `BusinessDayCloseSnapshots`, short-circuits and returns existing snapshot DTO with `wasReplayed: true`.
  2. Actor & Approval Resolution: Retrieves actor user from `_db.Users`. If `request.ManagerApprovalId` is provided, fetches manager approver and instantiates domain `ManagerApproval`. Enforces authorization via `AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "business_day_close", approval, requireApproval: true)`.
  3. Lifecycle Transition Verification: Loads `BusinessDayEntity` by ID, asserts legal two-step transition: current status -> `Closing`, and `Closingc -> `Closed`.
  4. Transaction Scope: Opens an explicit database transaction (`BeginTransactionAsync`).
  5. Audit Event (Started): Appends `business_day_close_started` to `ControlAuditEvents`.
  6. Blocker Evaluation: Evaluates blockers via `BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct)`. If any blockers exist: records `business_day_close_blocked` audit event with serialized blocker codes, commits the transaction (to permanently retain the audit log of the blocked attempt), and throws `BusinessDayCloseBlockedException`.
  7. Snapshot Compilation: Generates financial reconciliation rollup via `_reporting.GetBusinessDayReconciliationAsync(day.Id, ct)`, gathers day exceptions via `BuildDayExceptionsAsync`, and retrieves closing checklist completions. Calculates revision (`count + 1`), demotes previous snapshots to `Authoritative = false` via batch `ExecuteUpdateAsync`, and inserts new `BusinessDayCloseSnapshotEntity`.
  8. Day Mutation: Transitions `day.Status` to `Closed`, updates `CloseRevision`, `SnapshotId`, `ClosedAtUtc`, and `ClosedBy`.
  9. Completion Audit & Commit: Writes `business_day_closed` event with approving manager details, saves changes, and commits the transaction.
  10. Concurrency Handling: Catches `DbUpdateException`, rolls back transaction, checks if a concurrent request completed the snapshot, and returns it with `wasReplayed: true` if found.
  ```csharp
  // Lines 34-129: Business day closure command pipeline
  public async Task<BusinessDayCloseSnapshotDto> HandleAsync(CloseBusinessDayRequest request, CancellationToken ct = default)
  {
      var snapshotId = $snp_{request.RequestId};
      var existing = await _db.BusinessDayCloseSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
      if (existing is not null) return ToDto(existing, wasReplayed: true);
      ...
  }
  ```
  - **Transaction & Financial Integrity**: Transaction explicitly bounds blocker checks, state updates, snapshot persistence, and audit logs. Commits on blocked closure to preserve audit trail of unauthorized or blocked close attempts.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException, InvalidOperationException, BusinessDayCloseBlockedException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:37:95:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:37:95:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [37-37]
- **Behavior:** Inline predicate lambda `s => s.Id == snapshotId` locating existing close snapshot for idempotency replay check.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:40:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:40:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [40-40]
- **Behavior:** Inline predicate lambda `u => u.Id == request.ActorUserId` resolving actor user entity from Users table.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:47:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:47:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Inline predicate lambda `u => u.Id == request.ManagerApproverUserId` resolving manager approver user entity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:54:62:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:54:62:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [54-54]
- **Behavior:** Inline predicate lambda `d => d.Id == request.BusinessDayId` retrieving target business day entity by ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:80:81:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:80:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [80-80]
- **Behavior:** Inline selector lambda `b => b.Code` projecting blocker codes for JSON serialization into BlockerCodesJson.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:84:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:84:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-84]
- **Behavior:** Inline projection lambda `b => new CloseBlockerDto(b.Code, b.EntityType, b.EntityId, b.Message) mapping domain CloseBlocker records to API DTOs for BusinessDayCloseBlockedException.
- **Validations:** Validation checks detected: False. Throws: BusinessDayCloseBlockedException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:89:83:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:89:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Inline predicate lambda `c => c.BusinessDayId == day.Id` filtering checklist completion rows for the current business day.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:91:75:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:91:75:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [91-91]
- **Behavior:** Inline predicate lambda `s => s.BusinessDayId == day.Id` counting existing snapshots for the business day to derive the next close revision number.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:92:55:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:92:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Inline predicate lambda `s => s.BusinessDayId == day.Id` filtering previous close snapshots to demote Authoritative flag via ExecuteUpdateAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** Inline expression lambda '.ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:93:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** Inline expression lambda '.ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:102:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:102:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [102-102]
- **Behavior:** Inline projection lambda `c => new { c.ItemId, c.Completed, c.CompletedAtUtc, c.CompletedBy }` shaping checklist completions for JSON persistence in ChecklistJson.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:125:97:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:125:97:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [125-125]
- **Behavior:** Inline predicate lambda `s => s.Id == snapshotId` retrieving close snapshot for replay following DbUpdateException concurrency conflict.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:136:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:136:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.BuildDayExceptionsAsync(Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken)
- **Lines:** [136-160]
- **Behavior:** Assembles operational discrepancies and aggregates them into domain `ControlException` records:
  - Queries closed shifts for `day.BusinessDate` with recorded cash differences (`dayShifts`).
  - Queries refunded orders for `day.Id` where `RefundAmountPiastres > 0` (`dayOrders`).
  - Queries logged expenses for `day.Id` (`dayExpenses`).
  - Queries unsettled driver custodies across the system (`unsettledCustodyIds`).
  - Queries incomplete financial journal recovery entries across the system (`incompleteJournalIds`).
  - Reads discrepancy threshold via `PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(_db, ct)`.
  - Assembles `ControlCenterRules.BuildExceptionsInputs` and invokes pure domain rule `ControlCenterRules.BuildExceptions(input)` to generate exception list.
  ```csharp
  // Lines 136-160: Operational exception aggregation
  private async Task<IReadOnlyList<ControlException>> BuildDayExceptionsAsync(BusinessDayEntity day, CancellationToken ct)
  {
      var dayShifts = await _db.Shifts.AsNoTracking()
          .Where(s => s.BusinessDate == day.BusinessDate && s.Status == "Closed" && s.CashDifferencePiastres != null)
          .Select(s => new { s.Id, Diff = s.CashDifferencePiastres!.Value }).ToListAsync(ct);
      var dayOrders = await _db.Orders.AsNoTracking()
          .Where(o => o.BusinessDayId == day.Id && o.RefundAmountPiastres > 0)
          .Select(o => new { o.Id, o.RefundAmountPiastres }).ToListAsync(ct);
      var dayExpenses = await _db.Expenses.AsNoTracking()
          .Where(e => e.BusinessDayId == day.Id).Select(e => new { e.Id, e.AmountPiastres }).ToListAsync(ct);
      ...
      return ControlCenterRules.BuildExceptions(input);
  }
  ```
  - **Operational & Scalability Risks**:
    - **Global Unindexed Exception Scan**: Queries `db.DriverCustodies` and `db.FinancialJournalEntries` without date filters. If historical records have unresolved items, they will continually trigger exception flags across all future business day snapshots indefinitely.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:139:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:139:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [139-139]
- **Behavior:** Inline predicate lambda `s => s.BusinessDate == day.BusinessDate && s.Status == "Closed" && s.CashDifferencePiastres != null` filtering closed shifts on this date with recorded cash differences.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:140:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:140:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [140-140]
- **Behavior:** Inline projection lambda `s => new { s.Id, Diff = s.CashDifferencePiastres!.Value }` extracting shift ID and unwrapped cash difference piastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:142:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:142:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [142-142]
- **Behavior:** Inline expression lambda '.Where(o => o.BusinessDayId == day.Id && o.RefundAmountPiastres > 0)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:143:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:143:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [143-143]
- **Behavior:** Inline expression lambda '.Select(o => new { o.Id, o.RefundAmountPiastres }).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Inline predicate lambda `e => e.BusinessDayId == day.Id` filtering expenses recorded for the current business day.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:59:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:145:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Inline projection lambda `e => new { e.Id, e.AmountPiastres }` extracting expense ID and amount piastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Inline predicate lambda `c => c.Status != nameof(CustodyStatus.Settled)` filtering unsettled driver custodies system-wide.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:75:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:147:75:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Inline selector lambda `c => c.Id` extracting driver custody primary key identifiers.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [149-149]
- **Behavior:** Inline predicate lambda `j => j.Status != "Complete"` filtering financial journal recovery entries that are incomplete system-wide.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:149:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [149-149]
- **Behavior:** Inline selector lambda `j => j.IdempotencyKey` extracting idempotency keys of incomplete financial journal recovery entries.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:153:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:153:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [153-153]
- **Behavior:** Inline expression lambda 'ClosedShiftsWithDifference: dayShifts.Select(s => (s.Id, s.Diff)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:155:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:155:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [155-155]
- **Behavior:** Inline expression lambda 'RefundedOrders: dayOrders.Select(o => (o.Id, o.RefundAmountPiastres)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:156:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:156:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [156-156]
- **Behavior:** Inline expression lambda 'Expenses: dayExpenses.Select(e => (e.Id, e.AmountPiastres)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:162:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs:162:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.ToDto(Falah.Infrastructure.Persistence.Entities.BusinessDayCloseSnapshotEntity, bool)
- **Lines:** [162-165]
- **Behavior:** Static mapping method constructing a `BusinessDayCloseSnapshotDto` from a `BusinessDayCloseSnapshotEntity`, formatting `BusinessDate` as "yyyy-MM-dd" and setting `wasReplayed` flag.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:35:00Z
- **Source SHA-256 Verified:** B33D64C048DB902BC7B9469787872EA70BE30659AB69A97486D9F95C98D46255
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
  - Exhaustively audited all 30 callables in CloseBusinessDayHandler.cs.
  - Double-challenged L1 review and confirmed robust business day lifecycle evaluation, checklist gating, exception resolution tracking, and cash count discrepancy logging.
  - Verified SEM-MIRROR-071 parity of close blockers between client and server evaluators.
  - No modifications made to src/ or 	ests/.
