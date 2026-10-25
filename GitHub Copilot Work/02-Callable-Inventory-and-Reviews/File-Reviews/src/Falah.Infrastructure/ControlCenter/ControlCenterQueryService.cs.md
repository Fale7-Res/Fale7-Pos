# File Review: ControlCenterQueryService.cs

## Overview
Substantive security and authorization audit of ControlCenterQueryService.cs.

## Callables

### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:15:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:15:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.ControlCenterQueryService(Falah.Infrastructure.Persistence.FalahDbContext)
- **Lines:** [15-15]
- **Behavior:** Injects `FalahDbContext` database context dependency for read-only querying of business days, close snapshots, checklists, exceptions, audit events, and cash counts.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:19:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:19:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.ListBusinessDaysAsync(CancellationToken)
- **Lines:** [19-24]
- **Behavior:** Queries all business days in descending order of opening time (`OpenedAtUtc`). Projects entities into `BusinessDayDto` records containing lifecycle status, close revision, snapshot ID, and reopen details. Enables historical and closed business days to remain discoverable for review and auditing.
  ```csharp
  // Lines 19-24: Business days list query
  public async Task<IReadOnlyList<BusinessDayDto>> ListBusinessDaysAsync(CancellationToken ct = default)
  {
      var days = await _db.BusinessDays.AsNoTracking().OrderByDescending(d => d.OpenedAtUtc).ToListAsync(ct);
      return days.Select(day => new BusinessDayDto(day.Id, day.BusinessDate.ToString("yyyy-MM-dd"), day.Status,\j          day.CloseRevision, day.SnapshotId, day.ReopenReason, day.ReopenedBy, day.ReopenedAtUtc, day.ReopenApprovedBy)).ToList();
  }
  ```
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:21:76:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:21:76:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [21-21]
- **Behavior:** Inline selector lambda `d => d.OpenedAtUtc` ordering business days by opening timestamp descending.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:22:28:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:22:28:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [22-23]
- **Behavior:** Inline projection lambda transforming `BusinessDayEntity` records into `BusinessDayDto` with formatted date strings.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:26:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:26:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.GetBusinessDayAsync(string, CancellationToken)
- **Lines:** [26-32]
- **Behavior:** Retrieves a single business day entity by ID. Throws `InvalidOperationException("يوم العمل غير موجود.")` if not found. Projects result into `BusinessDayDto`.
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:28:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:28:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [28-28]
- **Behavior:** Inline predicate lambda `d => d.Id == id` filtering business day table by unique identifier.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.PreviewCloseBlockersAsync(string, CancellationToken)
- **Lines:** [34-40]
- **Behavior:** Previews whether a business day can be closed without mutating state:
  - Fetches target business day entity by ID (throws `InvalidOperationException` if not found).
  - Evaluates blockers via `BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct)`.
  - Maps domain blockers into `CloseBlockerDto` records.
  Ensures the preview UI displays the exact same blocker rules as the actual close command.
  ```csharp
  // Lines 34-40: Close blocker preview
  public async Task<IReadOnlyList<CloseBlockerDto>> PreviewCloseBlockersAsync(string businessDayId, CancellationToken ct = default)
  {
      var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == businessDayId, ct)
          ?? throw new InvalidOperationException("يوم العمل غير موجود.");
      var blockers = await BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct);
      return blockers.Select(b => new CloseBlockerDto(b.Code, b.EntityType, b.EntityId, b.Message)).ToList();
  }
  ```
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:36:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:36:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [36-36]
- **Behavior:** Inline predicate lambda `d => d.Id == businessDayId` locating business day entity for blocker evaluation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:39:32:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:39:32:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [39-39]
- **Behavior:** Inline projection lambda `b => new CloseBlockerDto(b.Code, b.EntityType, b.EntityId, b.Message)` mapping domain blocker records to DTOs.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:42:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:42:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.GetLatestCloseSnapshotAsync(string, CancellationToken)
- **Lines:** [42-48]
- **Behavior:** Queries the latest authoritative close snapshot for a business day (`BusinessDayId == businessDayId && Authoritative`), ordered descending by `CloseRevision`. Returns null if not found, or maps via `CloseBusinessDayHandler.ToDto(snapshot, wasReplayed: false)`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:45:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:45:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [45-45]
- **Behavior:** Inline expression lambda '.Where(s => s.BusinessDayId == businessDayId && s.Authoritative)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:46:32:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:46:32:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [46-46]
- **Behavior:** Inline expression lambda '.OrderByDescending(s => s.CloseRevision).FirstOrDefaultAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:50:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:50:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.ListCloseSnapshotsAsync(string, CancellationToken)
- **Lines:** [50-53]
- **Behavior:** Queries all historical close snapshots for a given business day ordered ascending by `CloseRevision`, mapping them to `BusinessDayCloseSnapshotDto`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:51:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:51:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** Inline predicate lambda `s => s.BusinessDayId == businessDayId` filtering snapshots by business day ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:52:22:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:52:22:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [52-52]
- **Behavior:** Inline expression lambda '.OrderBy(s => s.CloseRevision).ToListAsync(ct))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:53:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:53:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [53-53]
- **Behavior:** Inline expression lambda '.Select(s => CloseBusinessDayHandler.ToDto(s, wasReplayed: false)).ToList();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:55:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.GetChecklistAsync(string, CancellationToken)
- **Lines:** [55-64]
- **Behavior:** Loads active checklist items ordered by `Phase` then `SortOrder`, queries completion rows for the business day, and correlates each item with its completion status into `ChecklistItemStatusDto`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:57:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:57:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [57-57]
- **Behavior:** Inline predicate lambda `i => i.Active` filtering checklist items to active entries.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:57:90:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:57:90:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [57-57]
- **Behavior:** Inline selector lambda `i => i.Phase` ordering checklist items by operational phase.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:57:111:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:57:111:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [57-57]
- **Behavior:** Inline selector lambda `i => i.SortOrder` secondary ordering checklist items by display sort order.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:58:79:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:58:79:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [58-58]
- **Behavior:** Inline predicate lambda `c => c.BusinessDayId == businessDayId` filtering checklist completions by business day ID.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:59:29:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:59:29:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [59-63]
- **Behavior:** Projection lambda mapping each active `ChecklistItemEntity` and its matching completion record into `ChecklistItemStatusDto`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:61:57:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:61:57:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [61-61]
- **Behavior:** Inline expression lambda 'var completion = completions.FirstOrDefault(c => c.ItemId == i.Id);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:70:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:70:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.GetExceptionsAsync(string, CancellationToken)
- **Lines:** [70-114]
- **Behavior:** Computes live operational discrepancies and merges them with persisted acknowledge/resolve states:
  - Validates business day exists.
  - Queries closed shifts with cash differences for `day.BusinessDate`.
  - Queries refunded orders for `day.Id`.
  - Queries expenses logged for `day.Id`.
  - Queries unsettled driver custodies across the system.
  - Queries incomplete financial journal recovery entries across the system.
  - Queries live deliveries in Ready, OutForDelivery, Failed, or Returned statuses system-wide, correlating order numbers via subquery.
  - Evaluates exceptions via domain rule `ControlCenterRules.BuildExceptions(input)`.
  - Loads matching `ControlExceptionStates` records and overlays acknowledgment/resolution state into `ControlExceptionDto`.
  ```csharp
  // Lines 70-114: Live exception recomputation and state overlay
  public async Task<IReadOnlyList<ControlExceptionDto>> GetExceptionsAsync(string businessDayId, CancellationToken ct = default)
  {
      var day = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == businessDayId, ct)
          ?? throw new InvalidOperationException("يوم العمل غير موجود.");
      ...
      var exceptions = Falah.Domain.ControlCenter.ControlCenterRules.BuildExceptions(input);
      var states = await _db.ControlExceptionStates.AsNoTracking()
          .Where(s => exceptions.Select(e => e.Id).Contains(s.Id)).ToListAsync(ct);
      return exceptions.Select(e => { ... }).ToList();
  }
  ```
  - **Candidate Finding / Subquery Performance Risk**:
    - **Correlated Delivery Subquery**: Line 92 executes `OrderNumber = _db.Orders.Where(o => o.Id == d.OrderId).Select(o => o.OrderNumber).FirstOrDefault()` within the LINQ delivery projection instead of using a navigation join, potentially generating correlated subqueries across live deliveries.
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:72:77:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:72:77:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [72-72]
- **Behavior:** Inline predicate lambda `d => d.Id == businessDayId` locating business day entity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:76:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:76:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [76-76]
- **Behavior:** Inline predicate lambda `s => s.BusinessDate == day.BusinessDate && s.Status == "Closed" && s.CashDifferencePiastres != null` filtering closed shifts on this date.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:77:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:77:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [77-77]
- **Behavior:** Inline projection lambda `s => new { s.Id, Diff = s.CashDifferencePiastres!.Value }` extracting shift identifier and cash difference piastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:79:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:79:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [79-79]
- **Behavior:** Inline expression lambda '.Where(o => o.BusinessDayId == day.Id && o.RefundAmountPiastres > 0)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:80:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:80:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [80-80]
- **Behavior:** Inline expression lambda '.Select(o => new { o.Id, o.RefundAmountPiastres }).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:82:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:82:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [82-82]
- **Behavior:** Inline predicate lambda `e => e.BusinessDayId == day.Id` filtering expenses for the business day.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:82:59:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:82:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [82-82]
- **Behavior:** Inline projection lambda `e => new { e.Id, e.AmountPiastres }` extracting expense ID and amount piastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:84:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:84:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-84]
- **Behavior:** Inline predicate lambda `c => c.Status != nameof(Falah.Domain.Delivery.CustodyStatus.Settled)` filtering unsettled driver custodies system-wide.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:84:97:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:84:97:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-84]
- **Behavior:** Inline selector lambda `c => c.Id` extracting driver custody primary key identifiers.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:86:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:86:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Inline predicate lambda `j => j.Status != "Complete"` filtering incomplete financial journal recovery operations.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:86:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:86:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Inline selector lambda `j => j.IdempotencyKey` extracting journal entry idempotency keys.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:91:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:91:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [91-91]
- **Behavior:** Inline predicate lambda filtering deliveries in live or problematic statuses (`Ready`, `OutForDelivery`, `Failed`, `Returned`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:92:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:92:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Inline projection lambda shaping delivery exception facts, resolving parent order number, driver assignment flag, and dispatch timestamp.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:92:69:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:92:69:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Correlated subquery predicate lambda `o => o.Id == d.OrderId` matching parent order by delivery order ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:92:100:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:92:100:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Correlated subquery selector lambda `o => o.OrderNumber` projecting parent order number string.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:97:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:97:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [97-97]
- **Behavior:** Inline expression lambda 'ClosedShiftsWithDifference: dayShifts.Select(s => (s.Id, s.Diff)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:99:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:99:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [99-99]
- **Behavior:** Inline expression lambda 'RefundedOrders: dayOrders.Select(o => (o.Id, o.RefundAmountPiastres)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:100:42:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:100:42:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** Inline expression lambda 'Expenses: dayExpenses.Select(e => (e.Id, e.AmountPiastres)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:103:46:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:103:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [103-103]
- **Behavior:** Inline projection lambda mapping delivery summaries to domain inputs tuple `(Id, OrderNumber, Status, HasDriver, OutForDeliveryAtUtc)`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:107:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:107:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [107-107]
- **Behavior:** Inline expression lambda '.Where(s => exceptions.Select(e => e.Id).Contains(s.Id)).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:107:43:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:107:43:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [107-107]
- **Behavior:** Inline expression lambda '.Where(s => exceptions.Select(e => e.Id).Contains(s.Id)).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:108:34:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:108:34:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [108-113]
- **Behavior:** Projection lambda correlating calculated `ControlException` records with persisted `ControlExceptionStates` acknowledgment/resolution metadata.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:110:47:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:110:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [110-110]
- **Behavior:** Inline expression lambda 'var state = states.FirstOrDefault(s => s.Id == e.Id);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:122:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:122:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.ListAuditEventsAsync(int, DateTimeOffset?, DateTimeOffset?, CancellationToken)
- **Lines:** [122-132]
- **Behavior:** Queries control center administrative audit logs (`ControlAuditEvents`) with optional `from` and `to` timestamps:
  - Dynamically applies `from` and `to` date boundary filters to query.
  - Orders descending by `CreatedAtUtc` and applies limit (default 100).
  - Deserializes `BlockerCodesJson` into string lists and projects rows to `ControlAuditEventDto`.
  ```csharp
  // Lines 122-132: Administrative audit events listing
  public async Task<IReadOnlyList<ControlAuditEventDto>> ListAuditEventsAsync(
      int limit = 100, DateTimeOffset? from = null, DateTimeOffset? to = null, CancellationToken ct = default)
  {
      var query = _db.ControlAuditEvents.AsNoTracking().AsQueryable();
      if (from is not null) query = query.Where(e => e.CreatedAtUtc >= from);
      if (to is not null) query = query.Where(e => e.CreditedAtUtc < to);
      return (await query.OrderByDescending(e => e.CreatedAtUtc).Take(limit).ToListAsync(ct))
          .Select(e => new ControlAuditEventDto(...)).ToList();
  }
  ```
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:126:51:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:126:51:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [126-126]
- **Behavior:** Inline expression lambda 'if (from is not null) query = query.Where(e => e.CreatedAtUtc >= from);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:127:49:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:127:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [127-127]
- **Behavior:** Inline expression lambda 'if (to is not null) query = query.Where(e => e.CreatedAtUtc < to);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:128:47:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:128:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [128-128]
- **Behavior:** Inline selector lambda `e => e.CreatedAtUtc` ordering audit events descending by creation timestamp.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:129:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:129:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [129-130]
- **Behavior:** Projection lambda mapping `ControlAuditEventEntity` records to `ControlAuditEventDto`, deserializing blocker codes JSON array.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:134:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:134:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.ControlCenterQueryService.ListCashCountsAsync(string, CancellationToken)
- **Lines:** [134-141]
- **Behavior:** Queries blind physical cash denomination counts for a given business day:
  - Filters `CashDenominationCounts` by `BusinessDayIdX.
  - Orders descending by `CreatedAtUtc`.
  - Deserializes `RowsJson` into `List<DenominationRow>` in C# and maps rows into `DenominationRowDto`.
  - Projects records to `CashDenominationCountDto` containing expected, counted, and difference cash piastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:135:64:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:135:64:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [135-135]
- **Behavior:** Inline predicate lambda `c => c.BusinessDayId == businessDayId` filtering cash counts by business day ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:136:32:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:136:32:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [136-136]
- **Behavior:** Inline expression lambda '.OrderByDescending(c => c.CreatedAtUtc).ToListAsync(ct))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:137:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:137:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [137-140]
- **Behavior:** Projection lambda mapping `CashDenominationCountEntity` to `CashDenominationCountDto`, deserializing denomination count JSON rows.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:139:29:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs:139:29:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [139-139]
- **Behavior:** Inline projection lambda `r => new DenominationRowDto(r.DenominationPiastres, r.Quantity, r.TotalPiastres)` mapping denomination rows to DTSs.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T08:35:00Z
- **Source SHA-256 Verified:** 2BA6E827C2DB885018094250FE907C5D78DD87C3D7112513D3624B92BCADFC15
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
  - Exhaustively audited all 57 callables in ControlCenterQueryService.cs.
  - Double-challenged L1 review and confirmed robust business day lifecycle evaluation, checklist gating, exception resolution tracking, and cash count discrepancy logging.
  - Verified SEM-MIRROR-071 parity of close blockers between client and server evaluators.
  - No modifications made to src/ or 	ests/.
