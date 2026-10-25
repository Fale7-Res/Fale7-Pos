# File Review: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:24:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:24:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.TreasuryViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [24-24]
- **Behavior:** Constructor injecting AppSession dependency to store _session reference.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:26:71:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:26:71:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.Balances.get
- **Lines:** [26-26]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.Balances.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:27:58:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:27:58:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.Movements.get
- **Lines:** [27-27]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.Movements.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:34:56:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:34:56:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.MovementRow.Movement.get
- **Lines:** [34-34]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.MovementRow.Movement.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:34:61:InitAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:34:61:InitAccessorDeclaration
- **Kind:** InitAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.MovementRow.Movement.init
- **Lines:** [34-34]
- **Behavior:** Accessor 'init' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.MovementRow.Movement.init.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:126:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:126:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnAccountFilterChanged(string)
- **Lines:** [126-130]
- **Behavior:** Partial change handler invoked when AccountFilter changes, updating HasAccountFilter notification and triggering ApplyMovementFilter().
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:131:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:131:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnSearchTextChanged(string)
- **Lines:** [131-131]
- **Behavior:** Partial change handler invoked when SearchText changes, triggering ApplyMovementFilter().
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:138:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:138:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.ApplyMovementFilter()
- **Lines:** [138-153]
- **Behavior:** Filters cached _allMovements by AccountFilter (matching source or destination account) and SearchText (matching movement type, reason, reference, or actor). Populates observable Movements collection and updates empty state properties.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:141:40:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:141:40:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [141-147]
- **Behavior:** LINQ predicate filtering movements by matching active account ID and checking if search term appears in TypeLabel, Reason, ReferenceDisplay, or ActorDisplay.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:155:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:155:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.FilterByAccount(string)
- **Lines:** [155-156]
- **Behavior:** RelayCommand toggling account filter; clears filter if selected account is tapped again, or sets AccountFilter to selected accountId.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:166:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:166:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnIsBusyChanged(bool)
- **Lines:** [166-166]
- **Behavior:** Partial change handler delegating to RefreshOperationState() when IsBusy changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:168:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:168:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.RefreshOperationState()
- **Lines:** [168-172]
- **Behavior:** Raises PropertyChanged for CanEditAction and informs AppSession whether a management operation is in flight via _session.SetManagementOperationPending.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:178:52:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:178:52:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.FundingAccounts.get
- **Lines:** [178-178]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.FundingAccounts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:180:48:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:180:48:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.AllAccounts.get
- **Lines:** [180-180]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.AllAccounts.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:182:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:182:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.LoadAsync()
- **Lines:** [182-223]
- **Behavior:** Asynchronously fetches treasury account balances, outside money position (pending electronic transfers, unsettled driver cash custody), and the last 200 ledger movements. Rebuilds Balances, Position, and Movements collections, then notifies summary badges.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:240:60:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:240:60:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.DepositTypes.get
- **Lines:** [240-240]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.DepositTypes.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:247:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:247:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetDepositType(string)
- **Lines:** [247-248]
- **Behavior:** RelayCommand updating DepositType ("owner_deposit", "capital_injection", "other_external_inflow") when CanEditAction is true.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:264:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:264:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnOpenActionChanged(string?)
- **Lines:** [264-271]
- **Behavior:** Partial change handler raising PropertyChanged for dialog state flags (IsActionOpen, IsDeposit, IsWithdraw, IsTransfer, ActionTitle) when OpenAction changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:279:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:279:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OpenTreasuryAction(string)
- **Lines:** [279-293]
- **Behavior:** RelayCommand opening the modal dialog for "deposit", "withdraw", or "transfer". Resets inputs, sets default accounts, generates a unique idempotency key _requestId ("tre-{Guid:N}"), sets OpenAction, and evaluates NeedsApproval.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:295:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:295:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.CloseTreasuryAction()
- **Lines:** [295-296]
- **Behavior:** RelayCommand closing the active treasury action modal by setting OpenAction = null when CanEditAction allows.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:298:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:298:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetSourceAccount(string)
- **Lines:** [298-299]
- **Behavior:** RelayCommand updating SourceAccount when CanEditAction allows.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:301:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:301:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetDestinationAccount(string)
- **Lines:** [301-302]
- **Behavior:** RelayCommand updating DestinationAccount when CanEditAction allows.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:304:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:304:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SubmitTreasuryActionAsync()
- **Lines:** [304-363]
- **Behavior:** RelayCommand validating and dispatching deposit, withdrawal, or transfer. Replays _pendingCommit if present. Validates positive cash amount via CashInput.TryParse, non-empty Reason, and distinct accounts for transfers. Obtains manager approval via ResolveApprovalAsync if needed. Packages request DTO with idempotency key, sets _pendingCommit delegate, and awaits SendPendingAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:345:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:345:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [345-345]
- **Behavior:** Closure delegate () => NormalizeAsync(_session.Api.TreasuryDepositAsync(request)) stored in _pendingCommit for durable execution and replay.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:351:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:351:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [351-351]
- **Behavior:** Closure delegate () => NormalizeAsync(_session.Api.TreasuryWithdrawAsync(request)) stored in _pendingCommit for durable execution and replay.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:357:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:357:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [357-357]
- **Behavior:** Closure delegate () => NormalizeAsync(_session.Api.TreasuryTransferAsync(request)) stored in _pendingCommit for durable execution and replay.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:365:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:365:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.NormalizeAsync<T>(Task<Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>>)
- **Lines:** [365-369]
- **Behavior:** Static helper method unwrapping generic ApiResult<T> into a standardized (Success, OutcomeUnknown, Error) tuple for pending commit execution.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:371:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:371:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SendPendingAsync()
- **Lines:** [371-392]
- **Behavior:** Invokes _pendingCommit delegate. If execution fails with network ambiguity (OutcomeUnknown), retains _pendingCommit to force retry with the same request. On success, clears pending state, displays ActionNote with formatted amount, closes modal, and refreshes ledger via LoadAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:394:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:394:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.ResolveApprovalAsync(string)
- **Lines:** [394-413]
- **Behavior:** Validates manager PIN (length >= 4) and calls backend approval service _session.Api.IssueManagerApprovalAsync when NeedsApproval is true, returning (ApprovalId, ApproverUserId, Ok) tuple.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---


## Candidate Findings & Observations
- **Redundant Ternary Operator in Source Account Defaulting (line 288):**
  In `OpenTreasuryAction`:
  ```csharp
  SourceAccount = action == "withdraw" ? "tre-main" : "tre-main";
  ```
  Both branches assign the identical string literal `"tre-main"`, indicating either an unneeded ternary expression or an intended default account (such as allowing the user's selected account or drawer) that was inadvertently replaced.
