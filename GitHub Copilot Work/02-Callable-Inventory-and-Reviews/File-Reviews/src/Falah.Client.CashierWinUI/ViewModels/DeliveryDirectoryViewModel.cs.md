# File Review: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:28:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:28:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.DeliveryDirectoryViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [28-28]
- **Behavior:** Constructor injecting AppSession dependency to store _session reference and initialize directory management.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:32:54:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:32:54:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.Drivers.get
- **Lines:** [32-32]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.Drivers.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:33:64:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:33:64:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.Services.get
- **Lines:** [33-33]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.Services.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:63:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:63:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.OnIsBusyChanged(bool)
- **Lines:** [63-63]
- **Behavior:** Partial change handler delegating to NotifyState() when IsBusy changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:64:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:64:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.OnIsLoadingChanged(bool)
- **Lines:** [64-64]
- **Behavior:** Partial change handler delegating to NotifyState() when IsLoading changes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:66:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:66:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.NotifyState()
- **Lines:** [66-71]
- **Behavior:** Helper method raising PropertyChanged for CanAct, SaveLabel, IsEditingService, and ServiceFormTitle, and synchronizing management operation lock state with AppSession via _session.SetManagementOperationPending.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:73:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:73:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.OpenAsync()
- **Lines:** [73-83]
- **Behavior:** Asynchronously opens the delivery directory dialog when CanOpen is true and no background management operation is unresolved. Resets input fields, clears errors, notifies NeedsApproval, and loads driver and service collections via LoadAsync().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:85:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:85:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.Close()
- **Lines:** [85-90]
- **Behavior:** RelayCommand dismissing the delivery directory dialog by setting IsOpen = false when not busy and no write is pending.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:92:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:92:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.SetTab(string)
- **Lines:** [92-100]
- **Behavior:** RelayCommand switching between "drivers" and "services" tabs when not busy and no write is pending. Resets feedback messages and notifies IsDriversTab and IsServicesTab.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:102:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:102:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.LoadAsync()
- **Lines:** [102-123]
- **Behavior:** Asynchronously queries all drivers from _session.Api.GetAllDriversAsync() and all delivery services from _session.Api.GetAllDeliveryServicesAsync(). Populates Drivers and Services collections, sets IsReady = true, and manages IsLoading and LoadError states.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:125:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:125:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.ResetServiceForm()
- **Lines:** [125-132]
- **Behavior:** Resets service editing form fields (_editingService = null, ServiceName = "", ServiceFeeText = "", ServiceActive = true) and updates state notifications.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:134:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:134:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.NewService()
- **Lines:** [134-140]
- **Behavior:** RelayCommand clearing form fields via ResetServiceForm() to create a new delivery service when CanAct is true and no write is pending.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:142:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:142:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.SelectService(Falah.Application.Delivery.DeliveryServiceDto)
- **Lines:** [142-152]
- **Behavior:** RelayCommand selecting an existing delivery service into the editor. Sets _editingService, pre-populates name, fee, and active state, and invokes NotifyState().
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:154:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:154:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.AddDriverAsync()
- **Lines:** [154-162]
- **Behavior:** RelayCommand creating a new driver. Replays pending write from _write if present or calls BuildDriverRequest(), then dispatches through SubmitAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:164:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:164:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.BuildDriverRequest()
- **Lines:** [164-169]
- **Behavior:** Helper method constructing a CreateDriverRequest with a fresh idempotency key drv-{Guid:N}, trimmed DriverName, and active user ID. Validates non-empty name.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:171:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:171:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.ToggleDriverAsync(Falah.Application.Delivery.DriverDto)
- **Lines:** [171-178]
- **Behavior:** RelayCommand toggling driver active status. Replays pending write or constructs SetDriverActiveRequest with driver ID and inverted active flag (!driver.Active), dispatching through SubmitAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:180:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:180:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.SaveServiceAsync()
- **Lines:** [180-209]
- **Behavior:** RelayCommand validating and saving new or modified delivery service. Replays pending write if present. Enforces non-empty ServiceName and validates optional non-negative fee parsed via CashInput.TryParse. Obtains manager approval via ResolveApprovalAsync if needed. Constructs CreateDeliveryServiceRequest (with dsv-{Guid:N} key) or UpdateDeliveryServiceRequest, and triggers CompleteAsync.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:211:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:211:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.SubmitAsync(object, string)
- **Lines:** [211-216]
- **Behavior:** Helper managing IsBusy state lifecycle and dispatching directory commands to CompleteAsync.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:218:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:218:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.CompleteAsync(object, string)
- **Lines:** [218-229]
- **Behavior:** Submits directory command through retained request helper _write.SendAsync. On failure, records ActionError. On success, displays ActionNote, resets input fields and service editor form, reloads data via LoadAsync(), and triggers the Changed event to alert observers.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:231:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:231:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.SendWriteAsync(object)
- **Lines:** [231-244]
- **Behavior:** Command routing delegate pattern-matching directory requests (CreateDriverRequest, SetDriverActiveRequest, CreateDeliveryServiceRequest, UpdateDeliveryServiceRequest) to corresponding _session.Api client methods.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:233:9:LocalFunctionStatement

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:233:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Confirm<T>(Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>)
- **Lines:** [233-234]
- **Behavior:** Static local mapping function transforming ApiResult<T> to ApiResult<bool> while preserving success, error, and OutcomeUnknown status.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:246:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs:246:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.DeliveryDirectoryViewModel.ResolveApprovalAsync(string)
- **Lines:** [246-262]
- **Behavior:** Evaluates manager authorization if NeedsApproval is true. Validates manager PIN (length >= 4) and calls backend _session.Api.IssueManagerApprovalAsync with permission "delivery_manage" and specified reason, returning (ApprovalId, ApproverUserId, Ok) tuple.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `32572081689e600ee9740614b02ea4b1d75573739357c1c6a39ac0dc83049c31`
- **Callable ID Checked:** All 22 callables in `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `32572081689e600ee9740614b02ea4b1d75573739357c1c6a39ac0dc83049c31`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `32572081689e600ee9740614b02ea4b1d75573739357c1c6a39ac0dc83049c31` against client source `src/Falah.Client.CashierWinUI/ViewModels/DeliveryDirectoryViewModel.cs`. All 22 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 22 callables.


## Candidate Findings & Observations
- **CanOpen Precondition Blocks Intended Manager PIN Approval Fallback (lines 53, 57, 75, 248):**
  In `DeliveryDirectoryViewModel`:
  ```csharp
  public bool CanOpen => _session.HasPermission("delivery_manage");
  ...
  public bool NeedsApproval => !_session.HasPermission("delivery_manage");
  ...
  public async Task OpenAsync()
  {
      if (!CanOpen || _session.HasUnresolvedOperation) return;
      ...
  }
  ```
  The architectural design (detailed in the class doc comment, lines 19-21) intended for users without `delivery_manage` to be able to open the directory and submit actions using a manager's PIN via `NeedsApproval` and `ResolveApprovalAsync`. However, `OpenAsync` unconditionally guards entry with `if (!CanOpen ...) return;`. As a consequence, unprivileged users can never open the directory dialog in the first place, rendering `NeedsApproval`, `ManagerPin`, and the approval workflow dead code.
