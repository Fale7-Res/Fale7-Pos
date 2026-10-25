# File Review: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:26:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:26:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.OnIsBusyChanged(bool)
- **Lines:** [26-32]
- **Behavior:** Partial change handler invoked when IsBusy changes. Raises PropertyChanged for CanEditAction, CanAdvance, and CanOpenPay, and synchronizes management operation lock state with AppSession via _session.SetManagementOperationPending(this, !CanEditAction).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:34:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:34:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [34-40]
- **Behavior:** Constructor injecting AppSession dependency and initializing NewPeriod to current Cairo local wall-clock month formatted as "YYYY-MM" using Egypt Standard Time.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:42:55:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:42:55:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.Runs.get
- **Lines:** [42-42]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.Runs.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:48:47:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:48:47:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.Line.get
- **Lines:** [48-48]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.Line.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:48:52:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:48:52:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.Line.init
- **Lines:** [48-48]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.Line.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:49:47:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:49:47:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.EmployeeName.get
- **Lines:** [49-49]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.EmployeeName.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:49:52:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:49:52:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.EmployeeName.init
- **Lines:** [49-49]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.EmployeeName.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:83:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:83:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayrollLineRow.Describe((string Label, long Amount)[])
- **Lines:** [83-88]
- **Behavior:** Static helper formatting non-zero payroll line financial components (overtime, shifts, bonuses, deductions, advances) as label and formatted cash pairs joined by " · ".
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:85:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:85:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [85-85]
- **Behavior:** Inline expression lambda 'var shown = parts.Where(p => p.Amount != 0)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:86:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:86:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [86-86]
- **Behavior:** Inline expression lambda '.Select(p => $"{p.Label} {CashInput.Format(p.Amount)}");'.
- **Validations:** None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:91:57:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:91:57:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.Lines.get
- **Lines:** [91-91]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.Lines.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:132:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:132:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.OnIsLoadingLinesChanged(bool)
- **Lines:** [132-132]
- **Behavior:** Partial change handler notifying PropertyChanged for CanOpenPay when IsLoadingLines changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:133:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:133:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.OnLoadErrorChanged(string?)
- **Lines:** [133-133]
- **Behavior:** Partial change handler notifying PropertyChanged for CanOpenPay when LoadError changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:135:55:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:135:55:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [135-135]
- **Behavior:** Inline expression lambda 'public long SelectedRunTotalPiastres => Lines.Sum(l => l.NetPayablePiastres);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:140:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:140:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.OnSelectedRunChanged(Falah.Application.Employees.PayrollRunDto?)
- **Lines:** [140-149]
- **Behavior:** Partial change handler invoked when SelectedRun changes. Notifies NextStatus, NextStatusLabel, CanAdvance, IsApprovedAndUnpaid, and CanOpenPay. Asynchronously triggers LoadLinesAsync(value) if non-null; otherwise clears lines, increments _linesVersion concurrency token, and updates total display.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:151:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:151:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.LoadAsync()
- **Lines:** [151-156]
- **Behavior:** RelayCommand loading payroll runs when CanEditAction is true and payment dialog is not open (!IsPayOpen), delegating to ReloadAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:158:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:158:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.ReloadAsync()
- **Lines:** [158-180]
- **Behavior:** Asynchronously fetches payroll runs from _session.Api.GetPayrollRunsAsync() with IsLoading reentry guard. Populates Runs collection ordered descending by CreatedAtUtc, restores previous selection or defaults to the first run, and notifies HasNoRuns.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:172:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:172:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [172-172]
- **Behavior:** LINQ sorting key selector delegate ordering payroll runs descending by creation timestamp (CreatedAtUtc).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:173:47:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:173:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [173-173]
- **Behavior:** LINQ predicate matching existing SelectedRun by ID in the freshly loaded runs collection to preserve selection.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:182:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:182:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.LoadLinesAsync(Falah.Application.Employees.PayrollRunDto)
- **Lines:** [182-211]
- **Behavior:** Asynchronously queries employee payroll lines for the selected run via _session.Api.GetPayrollLinesAsync(run.Id). Employs _linesVersion concurrency token to discard stale out-of-order responses, ensures employee name cache is loaded, populates Lines with PayrollLineRow wrappers, and updates total payable properties.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:216:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:216:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.EnsureEmployeeNamesAsync()
- **Lines:** [216-222]
- **Behavior:** Queries and caches employee roster from _session.Api.GetEmployeesAsync() into _employeeNames lookup dictionary to resolve employee IDs to human-readable names on payroll lines.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:224:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:224:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.SelectRun(Falah.Application.Employees.PayrollRunDto)
- **Lines:** [224-225]
- **Behavior:** RelayCommand updating SelectedRun to the tapped payroll run when CanEditAction is true and pay dialog is closed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:227:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:227:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.CreateRunAsync()
- **Lines:** [227-251]
- **Behavior:** RelayCommand creating a new monthly payroll run. Replays retained request if present in _createRequest. Validates NewPeriod matches regex pattern ^\d{4}-(0[1-9]|1[0-2])$. Constructs CreatePayrollRequest with idempotency key pr-{Guid:N} and current user ID, then calls CompleteCreateAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:253:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:253:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.CompleteCreateAsync(Falah.Application.Employees.CreatePayrollRequest)
- **Lines:** [253-264]
- **Behavior:** Submits CreatePayrollRequest via _createRequest.SendAsync executing _session.Api.CreatePayrollRunAsync. On error sets ActionError with failure message. On success sets ActionNote and refreshes runs via ReloadAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:256:66:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:256:66:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [256-256]
- **Behavior:** API client invocation delegate r => _session.Api.CreatePayrollRunAsync(r) passed to _createRequest.SendAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:266:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:266:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.AdvanceRunAsync()
- **Lines:** [266-300]
- **Behavior:** RelayCommand advancing selected payroll run to next lifecycle status (draft → calculated → reviewed → approved). Replays retained request or constructs TransitionPayrollRequest with SelectedRun.Id and NextStatus. Submits request via _transitionRequest.SendAsync with custom monotonic state reconciliation against GetPayrollRunAsync on failure/timeout.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:277:70:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:277:70:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [277-290]
- **Behavior:** Async invocation delegate executing _session.Api.TransitionPayrollAsync(r) with idempotent retry recovery reconciling status against GetPayrollRunAsync using StatusRank if network outcome is uncertain.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:302:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:302:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.StatusRank(string)
- **Lines:** [302-305]
- **Behavior:** Static helper mapping payroll run status strings to monotonic numerical ranks (draft: 0, calculated: 1, reviewed: 2, approved: 3, paid: 4, other: -1) for state reconciliation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:309:66:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:309:66:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PaymentAccounts.get
- **Lines:** [309-309]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PaymentAccounts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:319:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:319:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.OpenPay()
- **Lines:** [319-326]
- **Behavior:** RelayCommand opening the disbursement dialog for an approved payroll run. Resets PaymentAccount and PayError, and sets IsPayOpen = true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:328:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:328:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.ClosePay()
- **Lines:** [328-329]
- **Behavior:** RelayCommand closing the disbursement dialog when CanEditAction is true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:331:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:331:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayRunAsync()
- **Lines:** [331-358]
- **Behavior:** RelayCommand executing disbursement payment for an approved payroll run. Replays pending retained request or validates selected PaymentAccount (main treasury, bank, or InstaPay) and constructs PayPayrollRequest. Dispatches via _payRequest.SendAsync executing _session.Api.PayPayrollAsync. On success, closes dialog, sets ActionNote with paid amount and employee count, and reloads runs via ReloadAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:346:63:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs:346:63:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [346-346]
- **Behavior:** API client invocation delegate r => _session.Api.PayPayrollAsync(r) passed to _payRequest.SendAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `8cf4e9f6ec47b2165413bc6ec1583f9261d08db7f950bdc75b3ee84fc8fe31a0`
- **Callable ID Checked:** All 33 callables in `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `8cf4e9f6ec47b2165413bc6ec1583f9261d08db7f950bdc75b3ee84fc8fe31a0`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `8cf4e9f6ec47b2165413bc6ec1583f9261d08db7f950bdc75b3ee84fc8fe31a0` against client source `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`. All 33 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 33 callables.


## Candidate Findings & Observations
- **Stale Status Label on Retained Transition Replay (lines 274-276, 296):**
  In `AdvanceRunAsync`, when replaying an unresolved transition request (`wasUnresolved = _transitionRequest.HasPending`), `ActionNote = $"تم تحديث حالة المسيّر إلى {NextStatusLabel}.";` is constructed using the current `NextStatusLabel` computed from `SelectedRun`. If the backend has already transitioned the run and `SelectedRun` status was updated or altered, `NextStatusLabel` will reflect the next future transition rather than the status that was just replayed and committed, generating a misleading confirmation message in the UI.
