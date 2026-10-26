# File Review: BusinessDayCloseEvaluator.cs

## Overview
Substantive security and authorization audit of BusinessDayCloseEvaluator.cs.

## Callables

### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:26:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:26:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.BusinessDayCloseEvaluator.EvaluateAsync(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken)
- **Lines:** [26-55]
- **Behavior:** Gathers all financial and operational prerequisites required by `BusinessDayRules.EvaluateCloseBlockers` straight from SQL Server persistence tables (`Shifts`, `DriverCustodies`, `FinancialJournalEntries`, `Orders`, `ChecklistItems`, `ChecklistCompletions`, and treasury reconciliation checks). Constructs `CloseBlockerInputs` tuple mapping shift statuses, empty delivery trips (as delivery slice dispatches per order without trips), driver custody settlement states, journal completion states, pending electronic payment orders, checklist items, and critical reconciliation checks. Finally invokes pure domain rule `BusinessDayRules.EvaluateCloseBlockers(inputs)` to return an immutable list of blocking conditions (`CloseBlocker`).
  ```csharp
  // Lines 26-55: Assembles close blocker inputs and evaluates gating rules
  public static async Task<IReadOnlyList<CloseBlocker>> EvaluateAsync(FalahDbContext db, BusinessDayEntity day, CancellationToken ct)
  {
      var shifts = await db.Shifts.AsNoTracking().Select(s => new { s.Id, s.Status }).ToListAsync(ct);
      var driverCustodies = await db.DriverCustodies.AsNoTracking().Select(c => new { c.Id, c.Status }).ToListAsync(ct);
      var journalEntries = await db.FinancialJournalEntries.AsNoTracking().Select(j => new { j.IdempotencyKey, j.Status }).ToListAsync(ct);
      var pendingElectronicOrderIds = await db.Orders.AsNoTracking()
          .Where(o => o.ElectronicPaymentStatus == "Pending").Select(o => o.Id).ToListAsync(ct);
      ...
      return BusinessDayRules.EvaluateCloseBlockers(inputs);
  }
  ```
  - **Candidate Finding / Operational Risk*:
    - **Global Unbounded Scans**: Queries `db.Shifts`, `db.DriverCustodies`, and `db.FinancialJournalEntries` without filtering by `BusinessDate` or `BusinessDayId`. As table history accumulates over time, this performs full table scans loading every historical shift and custody into memory. Furthermore, any historical shift or custody left unsettled indefinitely blocks subsequent day closures with `OPEN_SHIFT` or `UNSETTLED_DRIVER_CUSTODY`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:28:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:28:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [28-28]
- **Behavior:** Inline projection lambda `s => new { s.Id, s.Status }` selecting shift identifier and status from `ShiftEntity` for shift closure verification.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:29:78:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:29:78:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [29-29]
- **Behavior:** Inline projection lambda `c => new { c.Id, c.Status }` selecting driver custody identifier and settlement status from `DriverCustodyEntity` to evaluate unsettled driver custodies.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:30:85:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:30:85:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [30-30]
- **Behavior:** Inline projection lambda `j => new { j.IdempotencyKey, j.Status }` extracting financial journal entry idempotency key and status to verify background recovery completion.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:32:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:32:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Inline predicate lambda `o => o.ElectronicPaymentStatus == "Pending"` filtering order entities with unconfirmed electronic payments (e.g. InstaPay or digital wallets).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:32:72:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:32:72:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-32]
- **Behavior:** Inline selector lambda `o => o.Id` extracting order primary key identifiers for orders with pending electronic payments.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:35:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:35:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [35-35]
- **Behavior:** Inline expression lambda '.Where(i => i.Phase == "Closing" && i.Active && i.Required).ToListAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:36:78:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:36:78:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [36-36]
- **Behavior:** Inline predicate lambda `c => c.BusinessDayId == day.Id` filtering checklist completion records matching the target business day identifier.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:38:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:38:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [38-38]
- **Behavior:** Inline projection lambda `i => (i.Id, i.Label, completions.Any(c => c.ItemId == i.Id && c.Completed))` mapping closing checklist items to a tuple indicating whether each mandatory item has been completed for this business day.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:38:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:38:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [38-38]
- **Behavior:** Inline predicate lambda `c => c.ItemId == i.Id && c.Completed` checking whether a completion record exists and is marked as completed for a specific checklist item.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:44:35:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:44:35:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** Inline expression lambda 'Shifts: shifts.Select(s => (s.Id, s.Status == "Closed")).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:46:53:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:46:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [46-46]
- **Behavior:** Inline projection lambda `c => (c.Id, c.Status == nameof(CustodyStatus.Settled))` transforming driver custody summaries into tuples indicating whether custody is fully settled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:47:51:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:47:51:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Inline projection lambda `j => (j.IdempotencyKey, j.Status == "Complete")` evaluating whether financial journal recovery entries are complete.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:48:54:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:48:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [48-48]
- **Behavior:** Inline expression lambda 'Orders: pendingElectronicOrderIds.Select(id => (id, true)).ToList(),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:62:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:62:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Infrastructure.ControlCenter.BusinessDayCloseEvaluator.ComputeReconciliationChecksAsync(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken)
- **Lines:** [62-86]
- **Behavior:** Executes defensive reconciliation sanity checks that guard against financial inconsistencies prior to day closure:
  1. Duplicate Idempotency Keys: Checks `db.TreasuryMovements` for duplicated idempotency keys via `GroupBy-(m => m.IdempotencyKey).Where(g => g.Count() > 1).AnyAsync(ct)`. Adds blocker "DUPLICATE_IDEMPOTENCY_KEY".
  2. Protected Account Solvency: Loads all posted treasury movements (`m.Status == nameof(MovementStatus.Posted)`) into memory and iterates through `TreasuryAccounts.Protected` accounts, verifying that cumulative debits and credits do not yield a negative balance (`balance < 0`). Adds blocker `${("NEGATIVE_PROTECTED_BALANCE:")}{accountId}`.
  3. Drawer Cash Discrepancy Gate: Reads the authorized cash discrepancy threshold from `PrepareShiftCloseHandler.ReadDiscrepancyThresholdAsync(db, ct)`. Queries shifts for `day.BusinessDate` with recorded differences lacking authorization (`s.DifferenceAuthorizedBy == null`) that exceed the threshold in either direction (`> threshold || < -threshold`). Adds blocker "DRAWER_RECONCILIATION".
  ```csharp
  // Lines 62-86: Financial reconciliation and protected balance evaluation
  private static async Task<List<(string Code, bool Critical)>> ComputeReconciliationChecksAsync(FalahDbContext db, BusinessDayEntity day, CancellationToken ct)
  {
      var checks = new List<(string, bool)>();
      var duplicateKeyExists = await db.TreasuryMovements.AsNoTracking()
          .GroupBy(m => m.IdempotencyKey).Where(g => g.Count() > 1).AnyAsync(ct);
      checks.Add(("DUPLICATE_IDEMPOTENCY_KEY", duplicateKeyExists));
      ...
      return checks;
  }
  ```
  - **Candidate Finding / Financial & Performance Risk*:
    - **In-Memory Ledger Aggregation**: Fetches all posted `TreasuryMovements` across the entire database history into memory and evaluates account balances using LINQ to Objects rather than performing SQL server-side aggregation (`SUM` grouped by account). On a production database with large transaction volumes, this causes significant memory pressure and query latency.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:67:22:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:67:22:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [67-67]
- **Behavior:** Inline expression lambda '.GroupBy(m => m.IdempotencyKey).Where(g => g.Count() > 1).AnyAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:67:51:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:67:51:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [67-67]
- **Behavior:** Inline expression lambda '.GroupBy(m => m.IdempotencyKey).Where(g => g.Count() > 1).AnyAsync(ct);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:71:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:71:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [71-71]
- **Behavior:** Inline expression lambda '.Where(m => m.Status == nameof(MovementStatus.Posted))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:72:21:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:72:21:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [72-72]
- **Behavior:** Inline projection lambda `m => new { m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres }` extracting account IDs and transaction amounts from posted movements for balance calculation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:75:47:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:75:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-75]
- **Behavior:** Inline selector lambda `m => (m.DestinationAccountId == accountId ? m.AmountPiastres : 0) - (m.SourceAccountId == accountId ? m.AmountPiastres : 0)` computing net piastre delta for a protected account across posted treasury movements.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:81:20:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:81:20:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [81-81]
- **Behavior:** Inline predicate lambda `s => s.BusinessDate == day.BusinessDate && s.CashDifferencePiastres != null && s.DifferenceAuthorizedBy == null` filtering shifts on the closing business date that have cash differences lacking manager authorization.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:82:23:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs:82:23:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [82-82]
- **Behavior:** Inline predicate lambda `s => s.CashDifferencePiastres! > threshold || s.CashDifferencePiastres! < -threshold` testing if an unauthorized shift cash difference exceeds the configured tolerance threshold.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (QA-L2-Challenger-Wave4A)
- **Verification Date:** 2026-10-26T02:50:00Z
- **Source SHA-256 Verified:** `cc3a318c0332b40555ee9f2e545b42876b0e74a90e71c15732c5153eb576a832`
- **Total Callables Audited:** 22
- **Verdicts:** 21 ACCEPTED, 1 CHALLENGED

### Substantive Architectural & Operational Audit Findings:
1. **Critical Operational Risk Verified — `BUSINESSDAY-GLOBAL-SCAN-BLOCKER` (`BusinessDayCloseEvaluator.cs:26:5:MethodDeclaration`):**
   - Lines 28-30 execute unbounded `ToListAsync` table scans across `Shifts`, `DriverCustodies`, and `FinancialJournalEntries` without filtering by `BusinessDate` or `BusinessDayId`.
   - While intended to prevent closing if any legacy operation is unsettled, it creates an operational vulnerability: an unresolved shift from months ago permanently wedges business day closing with no bypass mechanism.
   - At production scale, loading all historical shifts and custodies into memory on every evaluation causes latency degradation and memory pressure.
   - **Verdict:** `CHALLENGED` (Overriding unconditioned L1 acceptance).
2. **Defensive Reconciliation Checks (`ComputeReconciliationChecksAsync`, lines 62-86):**
   - Line 67: Detects duplicate idempotency keys on `TreasuryMovements` via `GroupBy(m => m.IdempotencyKey).Where(g => g.Count() > 1)`.
   - Lines 73-77: Verifies non-negative balances on protected treasury accounts (`TreasuryAccounts.Protected`) across posted movements.
   - Lines 79-83: Scopes unauthorized drawer discrepancy checks strictly to `day.BusinessDate` against the configured tolerance threshold.
3. **SEM-MIRROR-071 Client/Server Parity:**
   - Accurately mirrors all frontend close blockers: `OPEN_SHIFT`, `UNSETTLED_DRIVER_CUSTODY`, `JOURNAL_INCOMPLETE`, `PENDING_ELECTRONIC_PAYMENT`, `CHECKLIST_INCOMPLETE`, and `RECONCILIATION_CRITICAL`.

### Detailed Callable Decisions Summary:
- `BusinessDayCloseEvaluator.cs:26:5:MethodDeclaration` [Lines 26-55] -> CHALLENGED (Defect: `BUSINESSDAY-GLOBAL-SCAN-BLOCKER`)
- `BusinessDayCloseEvaluator.cs:28:60:SimpleLambdaExpression` [Line 28] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:29:78:SimpleLambdaExpression` [Line 29] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:30:85:SimpleLambdaExpression` [Line 30] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:32:20:SimpleLambdaExpression` [Line 32] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:32:72:SimpleLambdaExpression` [Line 32] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:35:20:SimpleLambdaExpression` [Line 35] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:36:78:SimpleLambdaExpression` [Line 36] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:38:21:SimpleLambdaExpression` [Line 38] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:38:58:SimpleLambdaExpression` [Line 38] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:44:35:SimpleLambdaExpression` [Line 44] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:46:53:SimpleLambdaExpression` [Line 46] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:47:51:SimpleLambdaExpression` [Line 47] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:48:54:SimpleLambdaExpression` [Line 48] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:62:5:MethodDeclaration` [Lines 62-86] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:67:22:SimpleLambdaExpression` [Line 67] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:67:51:SimpleLambdaExpression` [Line 67] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:71:20:SimpleLambdaExpression` [Line 71] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:72:21:SimpleLambdaExpression` [Line 72] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:75:47:SimpleLambdaExpression` [Line 75] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:81:20:SimpleLambdaExpression` [Line 81] -> ACCEPTED
- `BusinessDayCloseEvaluator.cs:82:23:SimpleLambdaExpression` [Line 82] -> ACCEPTED
