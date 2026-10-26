# File Review: src/Falah.Domain/BusinessDay/BusinessDayRules.cs

## Overview
Substantive security, lifecycle, and authorization audit of src/Falah.Domain/BusinessDay/BusinessDayRules.cs.

## Callables

### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.IsActive(Falah.Domain.BusinessDay.BusinessDayRecord?)
- **Lines:** [24-24]
- **Behavior:** Determines whether a business day record represents an active operational state (Open, Closing, or Reopened). Returns false if record is null.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.AssertTransition(Falah.Domain.BusinessDay.BusinessDayStatus, Falah.Domain.BusinessDay.BusinessDayStatus)
- **Lines:** [34-38]
- **Behavior:** Enforces legal business day lifecycle status transitions (Open -> Closing, Reopened -> Closing, Closing -> Closed, Closed -> Reopened). Throws DomainException if transition is disallowed.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:40:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:40:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.Open(string, DateOnly, string, DateTimeOffset, IEnumerable<Falah.Domain.BusinessDay.BusinessDayRecord>)
- **Lines:** [40-44]
- **Behavior:** Opens a new business day. Asserts that no existing day in existingDays is currently active (throws DomainException if active day found), and instantiates a new BusinessDayRecord with Open status and CloseRevision = 0.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:46:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:46:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.Reopen(Falah.Domain.BusinessDay.BusinessDayRecord, IEnumerable<Falah.Domain.BusinessDay.BusinessDayRecord>, string, string, DateTimeOffset)
- **Lines:** [46-56]
- **Behavior:** Reopens a closed business day. Validates day status is Closed, asserts no other active business day exists, asserts original SnapshotId is present, and asserts non-empty reason. Returns updated record with Reopened status, trimmed reason, and approver name.
- **Validations:** Validation checks detected: True. Throws: DomainException, DomainException, DomainException, DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:50:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:50:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [50-50]
- **Behavior:** Inline expression lambda 'if (allDays.Any(other => other.Id != day.Id && IsActive(other)))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:73:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:73:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.EvaluateCloseBlockers(Falah.Domain.BusinessDay.BusinessDayRules.CloseBlockerInputs)
- **Lines:** [73-105]
- **Behavior:** Evaluates comprehensive closure gating preconditions: checks for open shifts, unfinalized delivery trips, unsettled driver custodies, in-flight financial journal entries, pending electronic payments, unhealthy persistence state, lost writer lease, uncompleted checklist items, and critical reconciliation failures. Returns a list of CloseBlocker items.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:77:50:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:77:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [77-77]
- **Behavior:** Inline expression lambda 'foreach (var shift in input.Shifts.Where(s => !s.Closed))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:80:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:80:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [80-80]
- **Behavior:** Inline expression lambda 'foreach (var trip in input.DeliveryTrips.Where(t => !t.Settled))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:83:61:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:83:61:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [83-83]
- **Behavior:** Inline expression lambda 'foreach (var custody in input.DriverCustodies.Where(c => !c.Settled))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:86:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:86:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Inline expression lambda 'foreach (var journal in input.JournalEntries.Where(j => !j.Complete))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:89:50:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:89:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Inline expression lambda 'foreach (var order in input.Orders.Where(o => o.ElectronicPaymentPending))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:98:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:98:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [98-98]
- **Behavior:** Inline expression lambda 'foreach (var item in input.RequiredChecklist.Where(c => !c.Completed))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:101:64:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:101:64:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [101-101]
- **Behavior:** Inline expression lambda 'foreach (var check in input.ReconciliationChecks.Where(c => c.Critical))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:107:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:107:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.AssertCanClose(IReadOnlyList<Falah.Domain.BusinessDay.CloseBlocker>)
- **Lines:** [107-110]
- **Behavior:** Validates that no blockers exist to close the business day. If blockers is non-empty, aggregates all blocker codes and error messages into a structured Arabic error message and throws DomainException.
- **Validations:** Validation checks detected: True. Throws: DomainException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:125:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:125:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Domain.BusinessDay.BusinessDayRules.CalculateDenominationCount(IReadOnlyList<(long DenominationPiastres, long Quantity)>, long)
- **Lines:** [125-135]
- **Behavior:** Performs cash drawer denomination counting. Normalizes input denomination rows by clamping negative quantities to zero, multiplies quantity by denomination piastres, computes total cash piastres, and returns a DenominationCountResult with variance against expectedPiastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:128:38:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:128:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [128-132]
- **Behavior:** LINQ projection mapping each denomination tuple to a DenominationRow record with clamped quantity Math.Max(0, r.Quantity) and total piastres.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Domain/BusinessDay/BusinessDayRules.cs:133:36:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Domain/BusinessDay/BusinessDayRules.cs:133:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [133-133]
- **Behavior:** Inline expression lambda 'var total = normalized.Sum(r => r.TotalPiastres);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `cdc3306873f2b6f46029186ec995b90415eda247ee3ec5b7408a9285f390ed06`
- **Callable ID Checked:** All 17 callables in `src/Falah.Domain/BusinessDay/BusinessDayRules.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Domain/BusinessDay/BusinessDayRules.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (QA-L2-Challenger-Wave4A)
- **Verification Date:** 2026-10-26T02:50:00Z
- **Source SHA-256 Verified:** `cdc3306873f2b6f46029186ec995b90415eda247ee3ec5b7408a9285f390ed06`
- **Total Callables Audited:** 17
- **Verdicts:** 17 ACCEPTED (0 CHALLENGED)

### Substantive Domain Rules Audit Findings:
1. **Lifecycle State Machine Integrity:**
   - Lines 26-38: `AssertTransition(from, to)` verifies allowed transitions (`Open -> Closing`, `Reopened -> Closing`, `Closing -> Closed`, `Closed -> Reopened`). Disallowed transitions throw `DomainException`.
2. **Single Active Day Invariant:**
   - Lines 40-44: `Open(...)` verifies `!existingDays.Any(IsActive)`, enforcing that only one business day can be active across the entire system.
   - Lines 46-56: `Reopen(...)` verifies `!allDays.Any(other => other.Id != day.Id && IsActive(other))`, ensuring reopening is blocked if another day has since been opened.
3. **Reopening Audit Requirements:**
   - Reopen requires original status `Closed`, valid `SnapshotId`, non-empty `reason`, and captures `approverName` as `ReopenedBy`.
4. **Deterministic Blocker Evaluation:**
   - Lines 73-105: `EvaluateCloseBlockers` maps operational inputs to 9 blocker categories (`OPEN_SHIFT`, `UNSETTLED_DELIVERY_TRIP`, `UNSETTLED_DRIVER_CUSTODY`, `JOURNAL_INCOMPLETE`, `PENDING_ELECTRONIC_PAYMENT`, `PERSISTENCE_UNHEALTHY`, `WRITER_NOT_OWNED`, `CHECKLIST_INCOMPLETE`, `RECONCILIATION_CRITICAL`).
   - Line 107-110: `AssertCanClose` provides strict invariant barrier.
5. **Denomination Cash Breakdown Arithmetic:**
   - Lines 125-135: `CalculateDenominationCount` clamps negative counts to zero (`Math.Max(0, r.Quantity)`), computes line totals and discrepancy using pure integer piastre math.

### Detailed Callable Decisions Summary:
- `BusinessDayRules.cs:24:5:MethodDeclaration` [Line 24] -> ACCEPTED
- `BusinessDayRules.cs:34:5:MethodDeclaration` [Lines 34-38] -> ACCEPTED
- `BusinessDayRules.cs:40:5:MethodDeclaration` [Lines 40-44] -> ACCEPTED
- `BusinessDayRules.cs:46:5:MethodDeclaration` [Lines 46-56] -> ACCEPTED
- `BusinessDayRules.cs:50:25:SimpleLambdaExpression` [Line 50] -> ACCEPTED
- `BusinessDayRules.cs:73:5:MethodDeclaration` [Lines 73-105] -> ACCEPTED
- `BusinessDayRules.cs:77:50:SimpleLambdaExpression` [Line 77] -> ACCEPTED
- `BusinessDayRules.cs:80:56:SimpleLambdaExpression` [Line 80] -> ACCEPTED
- `BusinessDayRules.cs:83:61:SimpleLambdaExpression` [Line 83] -> ACCEPTED
- `BusinessDayRules.cs:86:60:SimpleLambdaExpression` [Line 86] -> ACCEPTED
- `BusinessDayRules.cs:89:50:SimpleLambdaExpression` [Line 89] -> ACCEPTED
- `BusinessDayRules.cs:98:60:SimpleLambdaExpression` [Line 98] -> ACCEPTED
- `BusinessDayRules.cs:101:64:SimpleLambdaExpression` [Line 101] -> ACCEPTED
- `BusinessDayRules.cs:107:5:MethodDeclaration` [Lines 107-110] -> ACCEPTED
- `BusinessDayRules.cs:125:5:MethodDeclaration` [Lines 125-135] -> ACCEPTED
- `BusinessDayRules.cs:128:38:SimpleLambdaExpression` [Lines 128-132] -> ACCEPTED
- `BusinessDayRules.cs:133:36:SimpleLambdaExpression` [Line 133] -> ACCEPTED
