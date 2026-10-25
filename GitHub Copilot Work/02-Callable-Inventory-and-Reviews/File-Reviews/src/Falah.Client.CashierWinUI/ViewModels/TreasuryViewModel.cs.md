# File Review: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:24:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:24:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.TreasuryViewModel(Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [24-24]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.TreasuryViewModel(Falah.Client.CashierWinUI.Services.AppSession) across lines 24-24.
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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnAccountFilterChanged(string) across lines 126-130.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:131:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:131:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnSearchTextChanged(string)
- **Lines:** [131-131]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnSearchTextChanged(string) across lines 131-131.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:138:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:138:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.ApplyMovementFilter()
- **Lines:** [138-153]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.ApplyMovementFilter() across lines 138-153.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:141:40:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:141:40:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [141-147]
- **Behavior:** Executes logic for lambda expression across lines 141-147.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:155:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:155:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.FilterByAccount(string)
- **Lines:** [155-156]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.FilterByAccount(string) across lines 155-156.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:166:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:166:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnIsBusyChanged(bool)
- **Lines:** [166-166]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnIsBusyChanged(bool) across lines 166-166.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:168:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:168:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.RefreshOperationState()
- **Lines:** [168-172]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.RefreshOperationState() across lines 168-172.
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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.LoadAsync() across lines 182-223.
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
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetDepositType(string) across lines 247-248.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:264:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:264:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnOpenActionChanged(string?)
- **Lines:** [264-271]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OnOpenActionChanged(string?) across lines 264-271.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:279:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:279:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OpenTreasuryAction(string)
- **Lines:** [279-293]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.OpenTreasuryAction(string) across lines 279-293.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:295:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:295:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.CloseTreasuryAction()
- **Lines:** [295-296]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.CloseTreasuryAction() across lines 295-296.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:298:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:298:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetSourceAccount(string)
- **Lines:** [298-299]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetSourceAccount(string) across lines 298-299.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:301:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:301:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetDestinationAccount(string)
- **Lines:** [301-302]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SetDestinationAccount(string) across lines 301-302.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:304:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:304:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SubmitTreasuryActionAsync()
- **Lines:** [304-363]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SubmitTreasuryActionAsync() across lines 304-363.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:345:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:345:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [345-345]
- **Behavior:** Executes logic for lambda expression across lines 345-345.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:351:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:351:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [351-351]
- **Behavior:** Executes logic for lambda expression across lines 351-351.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:357:34:ParenthesizedLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:357:34:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [357-357]
- **Behavior:** Executes logic for lambda expression across lines 357-357.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:365:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:365:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.NormalizeAsync<T>(Task<Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>>)
- **Lines:** [365-369]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.NormalizeAsync<T>(Task<Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<T>>) across lines 365-369.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:371:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:371:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SendPendingAsync()
- **Lines:** [371-392]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.SendPendingAsync() across lines 371-392.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:394:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs:394:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.ResolveApprovalAsync(string)
- **Lines:** [394-413]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.TreasuryViewModel.ResolveApprovalAsync(string) across lines 394-413.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
