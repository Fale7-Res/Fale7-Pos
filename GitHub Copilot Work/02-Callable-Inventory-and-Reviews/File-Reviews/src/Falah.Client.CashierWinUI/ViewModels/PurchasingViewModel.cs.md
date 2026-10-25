# File Review: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:24:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:24:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchasingViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [24-24]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchasingViewModel(Falah.Client.CashierWinUI.Services.AppSession) across lines 24-24.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:29:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:29:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnIsBusyChanged(bool)
- **Lines:** [29-33]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnIsBusyChanged(bool) across lines 29-33.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LoadAsync() across lines 48-93.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:69:69:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:69:69:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [69-69]
- **Behavior:** Executes logic for lambda expression across lines 69-69.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:75:53:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:75:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-75]
- **Behavior:** Executes logic for lambda expression across lines 75-75.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LoadPayablesAsync() across lines 120-138.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:130:58:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:130:58:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [130-130]
- **Behavior:** Executes logic for lambda expression across lines 130-130.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:134:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:134:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [134-134]
- **Behavior:** Executes logic for lambda expression across lines 134-134.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:146:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:146:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnDueDateTargetChanged(Falah.Application.Purchasing.PurchaseDto?)
- **Lines:** [146-146]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnDueDateTargetChanged(Falah.Application.Purchasing.PurchaseDto?) across lines 146-146.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:153:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:153:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenDueDate(Falah.Application.Purchasing.PurchaseDto)
- **Lines:** [153-161]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenDueDate(Falah.Application.Purchasing.PurchaseDto) across lines 153-161.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:163:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:163:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseDueDate()
- **Lines:** [163-164]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseDueDate() across lines 163-164.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:166:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:166:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitDueDateAsync()
- **Lines:** [166-187]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitDueDateAsync() across lines 166-187.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:191:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:191:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.TryParseDueDate(string, out DateOnly?)
- **Lines:** [191-199]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.TryParseDueDate(string, out DateOnly?) across lines 191-199.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:214:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:214:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenAddSupplier()
- **Lines:** [214-223]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenAddSupplier() across lines 214-223.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:225:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:225:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseAddSupplier()
- **Lines:** [225-226]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseAddSupplier() across lines 225-226.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:228:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:228:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitSupplierAsync()
- **Lines:** [228-262]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitSupplierAsync() across lines 228-262.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompleteSupplierAsync(Falah.Application.Purchasing.AddSupplierRequest) across lines 264-277.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:267:68:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:267:68:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [267-267]
- **Behavior:** Executes logic for lambda expression across lines 267-267.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnItemChanged(Falah.Application.Inventory.InventoryItemDto?) across lines 300-304.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:305:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:305:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnUnitCostTextChanged(string)
- **Lines:** [305-305]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnUnitCostTextChanged(string) across lines 305-305.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:306:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:306:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnCurrentUnitCostPiastresChanged(long?)
- **Lines:** [306-306]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.OnCurrentUnitCostPiastresChanged(long?) across lines 306-306.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:308:9:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:308:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.RefreshCostShift()
- **Lines:** [308-314]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.RefreshCostShift() across lines 308-314.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:327:13:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:327:13:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostShiftNote.get
- **Lines:** [327-334]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry.CostShiftNote.get across lines 327-334.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:344:60:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:344:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [344-344]
- **Behavior:** Executes logic for lambda expression across lines 344-344.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:348:9:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:348:9:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SuspiciousCostShiftLabel.get
- **Lines:** [348-354]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SuspiciousCostShiftLabel.get across lines 348-354.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
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
- **Behavior:** Executes logic for lambda expression across lines 352-352.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:352:87:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:352:87:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [352-352]
- **Behavior:** Executes logic for lambda expression across lines 352-352.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnReceivePaymentMethodChanged(string) across lines 378-382.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:384:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:384:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenReceive(Falah.Application.Purchasing.SupplierDto)
- **Lines:** [384-402]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenReceive(Falah.Application.Purchasing.SupplierDto) across lines 384-402.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:404:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:404:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseReceive()
- **Lines:** [404-405]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CloseReceive() across lines 404-405.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:407:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:407:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.AddReceiveLine()
- **Lines:** [407-408]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.AddReceiveLine() across lines 407-408.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:410:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:410:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.NewLine()
- **Lines:** [410-410]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.NewLine() across lines 410-410.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:412:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:412:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnReceiveLinesChanged()
- **Lines:** [412-416]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnReceiveLinesChanged() across lines 412-416.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:423:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:423:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LookupCurrentUnitCost(string)
- **Lines:** [423-424]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.LookupCurrentUnitCost(string) across lines 423-424.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:426:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:426:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.RemoveReceiveLine(Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry)
- **Lines:** [426-432]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.RemoveReceiveLine(Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.PurchaseLineEntry) across lines 426-432.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:434:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:434:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SetReceivePaymentMethod(string)
- **Lines:** [434-435]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SetReceivePaymentMethod(string) across lines 434-435.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:437:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:437:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitReceiveAsync()
- **Lines:** [437-506]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitReceiveAsync() across lines 437-506.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompleteReceiveAsync(Falah.Application.Purchasing.ReceivePurchaseRequest) across lines 508-521.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:511:67:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:511:67:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [511-511]
- **Behavior:** Executes logic for lambda expression across lines 511-511.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:531:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:531:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnPayTargetChanged(Falah.Application.Purchasing.PurchaseDto?)
- **Lines:** [531-531]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OnPayTargetChanged(Falah.Application.Purchasing.PurchaseDto?) across lines 531-531.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:536:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:536:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenPay(Falah.Application.Purchasing.PurchaseDto)
- **Lines:** [536-547]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.OpenPay(Falah.Application.Purchasing.PurchaseDto) across lines 536-547.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:549:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:549:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ClosePay()
- **Lines:** [549-550]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ClosePay() across lines 549-550.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:552:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:552:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SetPayMethod(string)
- **Lines:** [552-553]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SetPayMethod(string) across lines 552-553.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:555:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:555:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitPayAsync()
- **Lines:** [555-585]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.SubmitPayAsync() across lines 555-585.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:578:94:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:578:94:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [578-578]
- **Behavior:** Executes logic for lambda expression across lines 578-578.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:587:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:587:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompletePayAsync(Falah.Application.Purchasing.PayPurchaseRequest)
- **Lines:** [587-600]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.CompletePayAsync(Falah.Application.Purchasing.PayPurchaseRequest) across lines 587-600.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:590:63:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:590:63:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [590-590]
- **Behavior:** Executes logic for lambda expression across lines 590-590.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:602:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:602:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.NullIfEmpty(string)
- **Lines:** [602-602]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.NullIfEmpty(string) across lines 602-602.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:604:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/PurchasingViewModel.cs:604:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ResolveApprovalAsync(bool, string, string, string, Action<string>)
- **Lines:** [604-624]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.PurchasingViewModel.ResolveApprovalAsync(bool, string, string, string, Action<string>) across lines 604-624.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
