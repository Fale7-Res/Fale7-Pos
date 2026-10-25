# File Review: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:33:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:33:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenSafeDrop()
- **Lines:** [33-43]
- **Behavior:** RelayCommand opening the safe drop modal when CanEditOperation is true. Resets input fields, clears errors, sets IsSafeDropOpen = true, and raises PropertyChanged for SafeDropNeedsApproval.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:45:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:45:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelSafeDrop()
- **Lines:** [45-46]
- **Behavior:** RelayCommand dismissing the safe drop modal when not busy and no unconfirmed request is held in _pendingSafeDrop.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:48:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:48:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitSafeDropAsync()
- **Lines:** [48-85]
- **Behavior:** RelayCommand validating and processing safe drop from cash drawer to treasury safe. Guards against double-clicks via IsSubmittingSafeDrop. Replays _pendingSafeDrop if present. Parses positive amount via CashInput.TryParse, enforces non-empty reason, solicits manager approval via ResolveManagerApprovalIfNeededAsync if SafeDropNeedsApproval is true, constructs SafeDropRequest with current cashier ID and shift ID, and awaits CompleteSafeDropAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:71:89:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:71:89:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [71-71]
- **Behavior:** Error callback delegate e => SafeDropError = e passed to ResolveManagerApprovalIfNeededAsync to display authorization failure messages on the modal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:100:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:100:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenCashMovement(string)
- **Lines:** [100-112]
- **Behavior:** RelayCommand opening cash movement modal for drawer cash in / cash out. Resets fields, sets CashMovementType to parameter value, sets IsCashMovementOpen = true, and notifies CashMovementNeedsApproval and CashMovementTitle.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:114:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:114:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelCashMovement()
- **Lines:** [114-115]
- **Behavior:** RelayCommand dismissing cash movement dialog when not busy and no unresolved request is held in _pendingCashMovement.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:117:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:117:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitCashMovementAsync()
- **Lines:** [117-154]
- **Behavior:** RelayCommand executing drawer cash in or cash out. Guards against duplicate execution with IsSubmittingCashMovement. Replays _pendingCashMovement if present. Validates positive cash amount, requires non-empty reason, requests manager approval if CashMovementNeedsApproval is true, builds CashierCashMovementRequest, and invokes CompleteCashMovementAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:140:103:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:140:103:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [140-140]
- **Behavior:** Error callback delegate e => CashMovementError = e passed to ResolveManagerApprovalIfNeededAsync to display authorization error messages.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:166:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:166:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenCashDrawerDialog()
- **Lines:** [166-175]
- **Behavior:** RelayCommand opening manual drawer kick modal. Pre-populates reason as "فتح يدوي", clears PIN and error, sets IsCashDrawerOpen = true, and notifies CashDrawerNeedsApproval.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:177:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:177:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelCashDrawer()
- **Lines:** [177-178]
- **Behavior:** RelayCommand dismissing drawer command dialog when not busy and no unresolved request is held in _pendingCashDrawer.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:184:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:184:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitCashDrawerAsync()
- **Lines:** [184-215]
- **Behavior:** RelayCommand submitting manual cash drawer opening request to backend. Guards against duplicate execution. Replays _pendingCashDrawer if present. Enforces non-empty reason, resolves manager approval via ResolveManagerApprovalIfNeededAsync if user lacks cash_drawer permission, constructs OpenCashDrawerRequest, and invokes CompleteCashDrawerAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:202:97:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:202:97:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [202-202]
- **Behavior:** Error callback delegate e => CashDrawerError = e passed to ResolveManagerApprovalIfNeededAsync to display authorization error messages.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:224:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:224:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenCashCountAsync()
- **Lines:** [224-246]
- **Behavior:** RelayCommand opening cash count view and querying real-time balance for drawer treasury account ("tre-drawer") from _session.Api.GetTreasuryBalanceAsync("tre-drawer"). Guarded by CanViewDrawerBalance. Updates CashCountBalancePiastres or sets CashCountError.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:248:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:248:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CloseCashCount()
- **Lines:** [248-249]
- **Behavior:** RelayCommand closing cash count view by setting IsCashCountOpen = false.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:261:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:261:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenOrderLookup()
- **Lines:** [261-270]
- **Behavior:** RelayCommand opening order lookup dialog when CanEditOperation allows. Resets search query, lookup results, and error state, then sets IsOrderLookupOpen = true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:272:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:272:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CloseOrderLookup()
- **Lines:** [272-273]
- **Behavior:** RelayCommand closing order lookup dialog when no reprint is pending (_pendingReprint is null) and operation is not busy.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:275:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:275:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SearchOrderAsync()
- **Lines:** [275-303]
- **Behavior:** RelayCommand querying order details by order number via _session.Api.LookupOrderByNumberAsync. Validates non-empty query string, sets OrderLookupResult on success, or populates OrderLookupError on failure.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:305:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:305:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReprintOrderAsync()
- **Lines:** [305-336]
- **Behavior:** RelayCommand reprinting customer receipt for looked-up order. Guards against double clicks with IsReprinting. Assembles CreatePrintJobsRequest with fresh idempotency key rp-{Guid:N}, CustomerRoute="Receipt", IsReprint=true, and IncludeKitchenRoutes=false. Retains request in _pendingReprint during network uncertainty, and updates ReprintStatusMessage with operation outcome.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:345:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:345:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ResolveManagerApprovalIfNeededAsync(bool, string, string, string, Action<string>)
- **Lines:** [345-367]
- **Behavior:** Shared manager authorization resolver. If needsApproval is true, validates manager PIN length >= 4, calls backend _session.Api.IssueManagerApprovalAsync with required permission and reason, invokes onError callback on failure, and returns (ApprovalId, ApproverUserId, Ok) tuple.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:370:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:370:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteSafeDropAsync(Falah.Application.Treasury.SafeDropRequest)
- **Lines:** [370-386]
- **Behavior:** Submits SafeDropRequest to backend via _session.Api.SafeDropAsync. Manages _pendingSafeDrop to support durable retry across network dropouts. On success, rotates _SafeDropRequestId and closes dialog.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:390:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:390:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteCashMovementAsync(Falah.Application.CashDrawer.CashierCashMovementRequest)
- **Lines:** [390-406]
- **Behavior:** Submits CashierCashMovementRequest to backend via _session.Api.CashierCashMovementAsync. Retains _pendingCashMovement across uncertain network outcomes. On success, rotates _CashMovementRequestId and closes modal.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:410:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:410:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteCashDrawerAsync(Falah.Application.CashDrawer.OpenCashDrawerRequest)
- **Lines:** [410-426]
- **Behavior:** Submits OpenCashDrawerRequest to backend via _session.Api.OpenCashDrawerAsync. Retains _pendingCashDrawer across uncertain network responses. On success, rotates _CashDrawerRequestId and closes modal.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `e4b4d4257703cbe135a8c821bb2075c7eb6d8b6e31dcb18622b0c8968fd38a06`
- **Callable ID Checked:** All 22 callables in `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `e4b4d4257703cbe135a8c821bb2075c7eb6d8b6e31dcb18622b0c8968fd38a06`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `e4b4d4257703cbe135a8c821bb2075c7eb6d8b6e31dcb18622b0c8968fd38a06` against client source `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`. All 22 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 22 callables.


## Candidate Findings & Observations
- **Redundant Unreachable Pending Checks in Operation Submission Methods (lines 52, 68, 121, 137, 188, 199):**
  In `SubmitSafeDropAsync`, `SubmitCashMovementAsync`, and `SubmitCashDrawerAsync`, the method begins with an early check:
  ```csharp
  if (_pendingSafeDrop is not null) { IsSubmittingSafeDrop = true; try { await CompleteSafeDropAsync(_pendingSafeDrop); } finally { IsSubmittingSafeDrop = false; } return; }
  ```
  Following validation, inside the subsequent `try` block (lines 68, 137, 199), the exact same check is repeated:
  ```csharp
  if (_pendingSafeDrop is not null) { await CompleteSafeDropAsync(_pendingSafeDrop); return; }
  ```
  Because the early guard returns unconditionally when `_pendingSafeDrop` is non-null and neither validation nor the try block modifies `_pendingSafeDrop`, the second check is dead code.
