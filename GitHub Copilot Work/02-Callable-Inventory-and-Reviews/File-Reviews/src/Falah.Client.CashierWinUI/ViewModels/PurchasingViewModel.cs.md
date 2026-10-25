# File Review: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:24:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:24:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchasingViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [24-24]
- **Behavior:** Initializes PurchasingViewModel with the injected AppSession dependency. Instantiates three RetainedRequest instances (_supplierRequest, _receiveRequest, _payRequest) for crash-resilient write idempotency, and initializes observable collections for Suppliers, Purchases, InventoryItems, and Payables.
- **Validations:** None on constructor parameters. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnIsBusyChanged(bool)
- **Lines:** [29-33]
- **Behavior:** CommunityToolkit.Mvvm partial method triggered when IsBusy changes. Notifies PropertyChanged for CanEditAction and calls _session.SetManagementOperationPending(this, !CanEditAction) to lock navigation in the main shell while purchasing requests are in flight.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:35:58:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:35:58:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.Suppliers.get
- **Lines:** [35-35]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.Suppliers.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:36:58:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:36:58:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.Purchases.get
- **Lines:** [36-36]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.Purchases.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:37:68:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:37:68:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.InventoryItems.get
- **Lines:** [37-37]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.InventoryItems.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:48:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:48:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LoadAsync()
- **Lines:** [48-93]
- **Behavior:** Asynchronous command orchestrating full purchasing view state hydration. Consecutively calls GetSuppliersAsync(), GetPurchasesAsync(), GetInventoryItemsAsync(), GetInventoryOverviewAsync(_session.CurrentUser!.Id), and LoadPayablesAsync(). Populates Suppliers, Purchases (sorted newest first by CreatedAtUtc), active InventoryItems, and cached _currentUnitCosts. Manages IsLoading flag, notifies HasNoSuppliers in finally block, and surfaces API failures via LoadError.
- **Validations:** Guarded by if (IsLoading) return;. Assumes CurrentUser is non-null for overview cost retrieval. Throws: NullReferenceException if _session.CurrentUser is null.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:69:69:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:69:69:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [69-69]
- **Behavior:** LINQ ordering selector p => p.CreatedAtUtc used in LoadAsync to sort purchase invoices chronologically descending before populating the Purchases collection.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:75:53:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:75:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-75]
- **Behavior:** LINQ filtering predicate i => i.Active used in LoadAsync to exclude inactive inventory items from the receipt item picker.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:97:56:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:97:56:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.Payables.get
- **Lines:** [97-97]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.Payables.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:102:60:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:102:60:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.WorstOverdue.get
- **Lines:** [102-102]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.WorstOverdue.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:104:69:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:104:69:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PayableBuckets.get
- **Lines:** [104-104]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PayableBuckets.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:120:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:120:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LoadPayablesAsync()
- **Lines:** [120-138]
- **Behavior:** Asynchronously queries payables aging details from _session.Api.GetPayablesAgingAsync(). On success, assigns Aging, populates Payables, PayableBuckets (where InvoiceCount > 0), and WorstOverdue (top 5 overdue invoices where DaysOverdue > 0). Uses non-blocking failure model: gracefully ignores API failure to avoid breaking the core purchasing view. Fires change notifications for HasPayables, HasOverdue, OverdueSummary, and UnscheduledSummary.
- **Validations:** Validates aging.Success && aging.Value is not null. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:130:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:130:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [130-130]
- **Behavior:** LINQ filtering predicate b => b.InvoiceCount > 0 used in LoadPayablesAsync to exclude empty aging buckets from the UI display.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:134:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:134:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [134-134]
- **Behavior:** LINQ filtering predicate i => i.DaysOverdue is > 0 selecting invoices that have passed their payment due date for the WorstOverdue priority card.
- **Validations:** Pattern matches nullable integer > 0. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:146:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:146:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnDueDateTargetChanged(Falah.Application.Purchasing.PurchaseDto?)
- **Lines:** [146-146]
- **Behavior:** Partial change notification handler for DueDateTarget; notifies PropertyChanged for IsDueDateOpen to open or close the due date dialog.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:153:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:153:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenDueDate(Falah.Application.Purchasing.PurchaseDto)
- **Lines:** [153-161]
- **Behavior:** RelayCommand opening the due date edit dialog for the target purchase invoice. Validates CanEditAction and CanManage (purchasing_manage permission), clears previous errors, pre-fills DueDateText, and sets DueDateTarget.
- **Validations:** Guarded by if (!CanEditAction || !CanManage) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:163:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:163:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseDueDate()
- **Lines:** [163-164]
- **Behavior:** RelayCommand closing the due date edit dialog by setting DueDateTarget = null if CanEditAction is true.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:166:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:166:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitDueDateAsync()
- **Lines:** [166-187]
- **Behavior:** Asynchronously submits updated or cleared due date for the selected invoice. Validates !IsBusy, non-null target, and non-null current user. Validates date format via TryParseDueDate. Dispatches SetPurchaseDueDateRequest with unique due-{guid} request key to _session.Api.SetPurchaseDueDateAsync. On success, sets ActionNote, resets dialog state, and triggers LoadAsync(). Wrapped in try/finally setting IsBusy.
- **Validations:** Validates date format (yyyy-MM-dd or blank), target presence, current user session. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:191:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:191:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.TryParseDueDate(string, out DateOnly?)
- **Lines:** [191-199]
- **Behavior:** Validates and parses due date text. Returns true with date = null if text is null, empty, or whitespace (valid for invoices with no agreed payment term). Otherwise parses exact format yyyy-MM-dd using invariant culture.
- **Validations:** Validates exact yyyy-MM-dd format via DateOnly.TryParseExact. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:214:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:214:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenAddSupplier()
- **Lines:** [214-223]
- **Behavior:** RelayCommand opening the Add Supplier modal. Verifies CanEditAction, resets input fields (SupplierName, SupplierPhone, SupplierCategory, SupplierNotes, SupplierManagerPin), clears errors, generates sup-{guid} request key, sets IsSupplierOpen = true, and notifies SupplierNeedsApproval.
- **Validations:** Guarded by if (!CanEditAction) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:225:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:225:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseAddSupplier()
- **Lines:** [225-226]
- **Behavior:** RelayCommand closing the Add Supplier modal by setting IsSupplierOpen = false when CanEditAction is true.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:228:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:228:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitSupplierAsync()
- **Lines:** [228-262]
- **Behavior:** Asynchronously submits new supplier creation. Checks IsBusy and pending writes on other purchasing retained requests. Replays existing pending supplier write if present in _supplierRequest. Validates non-empty SupplierName. If user lacks supplier management permissions, requests manager authorization via ResolveApprovalAsync. Then calls CompleteSupplierAsync with AddSupplierRequest. Wrapped in try/finally setting IsBusy.
- **Validations:** Checks string.IsNullOrWhiteSpace(SupplierName), requests manager PIN authorization when SupplierNeedsApproval. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:253:17:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:253:17:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [253-253]
- **Behavior:** Inline expression lambda 'e => SupplierError = e);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:264:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:264:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompleteSupplierAsync(Falah.Application.Purchasing.AddSupplierRequest)
- **Lines:** [264-277]
- **Behavior:** Dispatches AddSupplierRequest through _supplierRequest.SendAsync executing _session.Api.AddSupplierAsync(r). On failure, sets SupplierError from FailureMessage. On success, sets ActionNote, closes modal, clears request ID, and reloads view state via LoadAsync().
- **Validations:** Validates result.Success and non-null result.Value. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:267:68:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:267:68:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [267-267]
- **Behavior:** API invocation delegate r => _session.Api.AddSupplierAsync(r) passed to _supplierRequest.SendAsync to execute supplier creation.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:293:50:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:293:50:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostLookup.get
- **Lines:** [293-293]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostLookup.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:293:55:SetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:293:55:SetAccessorDeclaration
- **Kind:** SetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostLookup.set
- **Lines:** [293-293]
- **Behavior:** Accessor 'set' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostLookup.set.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:298:34:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:298:34:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.Changed.get
- **Lines:** [298-298]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.Changed.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:298:39:SetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:298:39:SetAccessorDeclaration
- **Kind:** SetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.Changed.set
- **Lines:** [298-298]
- **Behavior:** Accessor 'set' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.Changed.set.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:300:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:300:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnItemChanged(Falah.Application.Inventory.InventoryItemDto?)
- **Lines:** [300-304]
- **Behavior:** CommunityToolkit partial method on PurchaseLineEntry triggered when Item changes. Looks up baseline unit cost via CostLookup delegate using item ID, updates CurrentUnitCostPiastres, and invokes RefreshCostShift().
- **Validations:** Null-check on item value. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:305:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:305:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnUnitCostTextChanged(string)
- **Lines:** [305-305]
- **Behavior:** CommunityToolkit partial method on PurchaseLineEntry triggered when UnitCostText changes; calls RefreshCostShift() to recalculate tenfold cost basis shift.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:306:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:306:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnCurrentUnitCostPiastresChanged(long?)
- **Lines:** [306-306]
- **Behavior:** CommunityToolkit partial method on PurchaseLineEntry triggered when CurrentUnitCostPiastres changes; calls RefreshCostShift() to update cost deviation note.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:308:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:308:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.RefreshCostShift()
- **Lines:** [308-314]
- **Behavior:** Refreshes PropertyChanged notifications for CostShiftNote, HasCostShiftNote, and IsCostShiftSuspicious on the line, and triggers the parent Changed callback to update dialog-level warning banners.
- **Validations:** Safe invocation of optional Changed delegate. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:327:13:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:327:13:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostShiftNote.get
- **Lines:** [327-334]
- **Behavior:** Computes descriptive Arabic note comparing keyed line price against previous unit cost basis. Warns if keyed price is tenfold higher or lower, indicating likely misplaced decimal point. Returns empty string if unpriced or identical.
- **Validations:** Checks previous > 0 and validates non-null typed cost. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:344:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:344:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [344-344]
- **Behavior:** LINQ predicate l => l.IsCostShiftSuspicious used in HasSuspiciousCostShift to test if any line in ReceiveLines has a tenfold cost deviation.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:348:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:348:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SuspiciousCostShiftLabel.get
- **Lines:** [348-354]
- **Behavior:** Generates Arabic warning banner text naming all items whose entered price shifts cost basis by 10x or more, warning that recipe costs will change immediately upon receipt.
- **Validations:** Filters lines where IsCostShiftSuspicious == true. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:350:44:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:350:44:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [350-350]
- **Behavior:** Inline expression lambda 'var lines = ReceiveLines.Where(l => l.IsCostShiftSuspicious).ToList();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:352:56:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:352:56:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [352-352]
- **Behavior:** Projection lambda l => l.Item?.Name ?? "" extracting item name from suspicious line entries for display in SuspiciousCostShiftLabel.
- **Validations:** Null-safe navigation on Item. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:352:87:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:352:87:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [352-352]
- **Behavior:** Filter predicate lambda n => n.Length > 0 removing empty item names before joining names with Arabic comma.
- **Validations:** String length check. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:366:67:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:366:67:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ReceiveLines.get
- **Lines:** [366-366]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ReceiveLines.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:378:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:378:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnReceivePaymentMethodChanged(string)
- **Lines:** [378-382]
- **Behavior:** Partial change notification handler for ReceivePaymentMethod. Notifies PropertyChanged for IsCreditPurchase and, when method is credit (آجل), zeroes ReceivePaidText to enforce zero drawer outflow.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:384:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:384:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenReceive(Falah.Application.Purchasing.SupplierDto)
- **Lines:** [384-402]
- **Behavior:** RelayCommand opening purchase receipt modal for the specified supplier. Verifies CanEditAction, initializes receipt fields (InvoiceNumber, ReceivePaidText = "0", ReceiveDueDateText, ReceiveNotes, ReceiveManagerPin), resets CostShiftAcknowledged = false, clears and adds initial blank line via NewLine(), generates request key pur-{guid}, and marks IsReceiveOpen = true.
- **Validations:** Guarded by if (!CanEditAction) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:404:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:404:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseReceive()
- **Lines:** [404-405]
- **Behavior:** RelayCommand closing purchase receive dialog by setting IsReceiveOpen = false if CanEditAction is true.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:407:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:407:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.AddReceiveLine()
- **Lines:** [407-408]
- **Behavior:** RelayCommand appending a new blank line created via NewLine() to ReceiveLines when CanEditAction allows.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:410:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:410:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.NewLine()
- **Lines:** [410-410]
- **Behavior:** Factory helper instantiating a PurchaseLineEntry wired with LookupCurrentUnitCost as its CostLookup delegate and OnReceiveLinesChanged as its Changed callback.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:412:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:412:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnReceiveLinesChanged()
- **Lines:** [412-416]
- **Behavior:** Callback invoked whenever any child purchase line item or cost changes; updates PropertyChanged notifications for HasSuspiciousCostShift and SuspiciousCostShiftLabel.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:423:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:423:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LookupCurrentUnitCost(string)
- **Lines:** [423-424]
- **Behavior:** Looks up inventory item current unit cost in piastres from the cached _currentUnitCosts map populated during LoadAsync. Returns cost if found, else null.
- **Validations:** Dictionary TryGetValue check. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:426:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:426:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.RemoveReceiveLine(Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry)
- **Lines:** [426-432]
- **Behavior:** RelayCommand removing a line from ReceiveLines. Ensures the list is never empty by adding a new line if count becomes zero. NOTE: Does not call OnReceiveLinesChanged(), creating a UI defect where removing a suspicious line leaves the warning banner visible.
- **Validations:** Guarded by if (!CanEditAction) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:434:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:434:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SetReceivePaymentMethod(string)
- **Lines:** [434-435]
- **Behavior:** RelayCommand setting ReceivePaymentMethod to the specified method string (e.g. "cash", "credit") when CanEditAction allows.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:437:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:437:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitReceiveAsync()
- **Lines:** [437-506]
- **Behavior:** Asynchronously validates and processes purchase invoice reception. Replays pending retained request if present. Validates that each line has an item, positive milli-unit quantity, and positive unit cost. Verifies at least one valid line exists. Enforces explicit operator acknowledgement of tenfold cost shifts (HasSuspiciousCostShift && !CostShiftAcknowledged). Parses paid cash amount and due date. Solicits manager PIN authorization via ResolveApprovalAsync if needed, then invokes CompleteReceiveAsync. Wrapped in try/finally setting IsBusy.
- **Validations:** Validates line item selection, quantity > 0, unit cost > 0, at least 1 line, cost shift acknowledgement, valid cash paid parsing, valid due date format, manager PIN if unprivileged. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:497:17:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:497:17:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [497-497]
- **Behavior:** Inline expression lambda 'e => ReceiveError = e);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:508:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:508:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompleteReceiveAsync(Falah.Application.Purchasing.ReceivePurchaseRequest)
- **Lines:** [508-521]
- **Behavior:** Dispatches ReceivePurchaseRequest through _receiveRequest.SendAsync executing _session.Api.ReceivePurchaseAsync(r). On failure, sets ReceiveError from FailureMessage. On success, displays invoice total piastres in ActionNote, closes modal, clears request ID, and reloads view state via LoadAsync().
- **Validations:** Validates result.Success and non-null result.Value. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:511:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:511:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [511-511]
- **Behavior:** API invocation delegate r => _session.Api.ReceivePurchaseAsync(r) passed to _receiveRequest.SendAsync to execute purchase receipt.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:531:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:531:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnPayTargetChanged(Falah.Application.Purchasing.PurchaseDto?)
- **Lines:** [531-531]
- **Behavior:** Partial change notification handler for PayTarget; notifies PropertyChanged for IsPayOpen to toggle visibility of the invoice payment settlement dialog.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:536:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:536:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenPay(Falah.Application.Purchasing.PurchaseDto)
- **Lines:** [536-547]
- **Behavior:** RelayCommand opening the invoice payment dialog for a purchase invoice. Verifies CanEditAction, clears errors, pre-fills PayAmountText with RemainingAmountPiastres formatted in pounds, defaults PayMethod to "cash", generates pay-{guid} request key, sets PayTarget = purchase, and notifies PayNeedsApproval.
- **Validations:** Guarded by if (!CanEditAction) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:549:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:549:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ClosePay()
- **Lines:** [549-550]
- **Behavior:** RelayCommand closing the invoice payment dialog by setting PayTarget = null if CanEditAction is true.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:552:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:552:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SetPayMethod(string)
- **Lines:** [552-553]
- **Behavior:** RelayCommand setting PayMethod to the selected payment method when CanEditAction allows.
- **Validations:** Guarded by if (CanEditAction). Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:555:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:555:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitPayAsync()
- **Lines:** [555-585]
- **Behavior:** Asynchronously validates and processes payment against an outstanding supplier invoice. Replays pending retained request if present in _payRequest. Validates positive cash amount parsed via CashInput.TryParse. Solicits manager PIN authorization via ResolveApprovalAsync if needed, then invokes CompletePayAsync with PayPurchaseRequest. Wrapped in try/finally setting IsBusy.
- **Validations:** Validates PayTarget non-null, amount > 0, manager authorization if unprivileged. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:578:94:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:578:94:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [578-578]
- **Behavior:** Error handler callback lambda e => PayError = e passed to ResolveApprovalAsync to display authorization error message on the payment modal.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:587:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:587:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompletePayAsync(Falah.Application.Purchasing.PayPurchaseRequest)
- **Lines:** [587-600]
- **Behavior:** Dispatches PayPurchaseRequest through _payRequest.SendAsync executing _session.Api.PayPurchaseAsync(r). On failure, sets PayError from FailureMessage. On success, displays amount paid in ActionNote, closes payment modal, clears request ID, and reloads view state via LoadAsync().
- **Validations:** Validates result.Success and non-null result.Value. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:590:63:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:590:63:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [590-590]
- **Behavior:** API invocation delegate r => _session.Api.PayPurchaseAsync(r) passed to _payRequest.SendAsync to execute invoice payment.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:602:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:602:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.NullIfEmpty(string)
- **Lines:** [602-602]
- **Behavior:** Utility helper returning null if input string is null, empty, or whitespace; otherwise returns trimmed string. Used to normalize optional form fields (phone, category, notes, invoice number) before API transmission.
- **Validations:** Evaluates string.IsNullOrWhiteSpace. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:604:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:604:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ResolveApprovalAsync(bool, string, string, string, Action<string>)
- **Lines:** [604-624]
- **Behavior:** Authorizes sensitive purchasing actions by validating manager PIN against backend approval service via _session.Api.IssueManagerApprovalAsync. Returns immediate success tuple if needsApproval is false. When required, validates PIN length >= 4, calls API with permission and reason, surfaces error callback on failure, and returns (ApprovalId, ApproverUserId, Ok).
- **Validations:** Checks PIN length >= 4, checks approval API result.Success and non-null Value. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
