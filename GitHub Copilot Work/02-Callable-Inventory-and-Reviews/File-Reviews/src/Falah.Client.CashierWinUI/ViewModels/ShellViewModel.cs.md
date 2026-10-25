# File Review: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:24:58:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:24:58:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavItems.get
- **Lines:** [24-24]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavItems.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:28:60:GetAccessorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:28:60:GetAccessorDeclaration
- **Kind:** GetAccessorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavGroups.get
- **Lines:** [28-28]
- **Behavior:** Accessor 'get' for property in Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavGroups.get.
- **Validations:** None (auto-property).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:35:5:ConstructorDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:35:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.ShellViewModel(Falah.Client.CashierWinUI.Services.AppSession, Falah.Client.CashierWinUI.ViewModels.HeaderViewModel)
- **Lines:** [35-53]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.ShellViewModel(Falah.Client.CashierWinUI.Services.AppSession, Falah.Client.CashierWinUI.ViewModels.HeaderViewModel) across lines 35-53.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.BuildGroups()
- **Lines:** [72-95]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.BuildGroups() across lines 72-95.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:79:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:79:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [79-79]
- **Behavior:** Inline expression lambda '.Select(m => NavItems.FirstOrDefault(i => i.Key == m))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:79:54:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:79:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [79-79]
- **Behavior:** Inline expression lambda '.Select(m => NavItems.FirstOrDefault(i => i.Key == m))'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:80:24:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:80:24:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [80-80]
- **Behavior:** Inline expression lambda '.Where(i => i is not null)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:81:25:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:81:25:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [81-81]
- **Behavior:** Inline expression lambda '.Select(i => i!)'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:90:39:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:90:39:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** Inline expression lambda 'var unplaced = NavItems.Where(i => !placed.Contains(i.Key)).ToList();'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:92:45:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:92:45:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [92-92]
- **Behavior:** Inline expression lambda 'var more = NavGroups.FirstOrDefault(g => g.Key == "more");'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:102:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:102:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.AllCandidates()
- **Lines:** [102-142]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.AllCandidates() across lines 102-142.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:144:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:144:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.Navigate(Falah.Client.CashierWinUI.ViewModels.ShellNavItem)
- **Lines:** [144-177]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.Navigate(Falah.Client.CashierWinUI.ViewModels.ShellNavItem) across lines 144-177.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:179:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:179:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateOrders()
- **Lines:** [179-184]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateOrders() across lines 179-184.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateCustomers()
- **Lines:** [186-186]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateCustomers() across lines 186-186.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:188:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:188:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateMenu()
- **Lines:** [188-193]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateMenu() across lines 188-193.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:195:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:195:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateSettings()
- **Lines:** [195-200]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateSettings() across lines 195-200.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:202:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:202:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateBackup()
- **Lines:** [202-207]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateBackup() across lines 202-207.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:209:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:209:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateControlCenter()
- **Lines:** [209-214]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateControlCenter() across lines 209-214.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:216:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:216:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePrinting()
- **Lines:** [216-221]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePrinting() across lines 216-221.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:223:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:223:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateUsers()
- **Lines:** [223-228]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateUsers() across lines 223-228.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:230:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:230:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateAudit()
- **Lines:** [230-235]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateAudit() across lines 230-235.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:237:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:237:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateEmployees()
- **Lines:** [237-242]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateEmployees() across lines 237-242.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:244:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:244:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateAttendance()
- **Lines:** [244-249]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateAttendance() across lines 244-249.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:251:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:251:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePayroll()
- **Lines:** [251-256]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePayroll() across lines 251-256.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:258:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:258:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateRecipes()
- **Lines:** [258-263]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateRecipes() across lines 258-263.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:265:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:265:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateWaste()
- **Lines:** [265-270]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateWaste() across lines 265-270.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:272:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:272:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePurchasing()
- **Lines:** [272-277]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePurchasing() across lines 272-277.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:279:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:279:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateTreasury()
- **Lines:** [279-284]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateTreasury() across lines 279-284.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:286:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:286:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateShifts()
- **Lines:** [286-291]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateShifts() across lines 286-291.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:293:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:293:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateInventory()
- **Lines:** [293-298]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateInventory() across lines 293-298.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:300:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:300:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateOwnerDashboard()
- **Lines:** [300-308]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateOwnerDashboard() across lines 300-308.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:310:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:310:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavigateByKey(string)
- **Lines:** [310-313]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavigateByKey(string) across lines 310-313.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:312:37:SimpleLambdaExpression

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:312:37:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [312-312]
- **Behavior:** Inline expression lambda 'if (NavItems.FirstOrDefault(i => i.Key == key) is { } target) Navigate(target);'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:315:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:315:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateReports()
- **Lines:** [315-320]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateReports() across lines 315-320.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:322:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:322:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDailySummary()
- **Lines:** [322-327]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDailySummary() across lines 322-327.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:329:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:329:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDailyStatement()
- **Lines:** [329-334]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDailyStatement() across lines 329-334.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:336:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:336:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDelivery()
- **Lines:** [336-341]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDelivery() across lines 336-341.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:343:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:343:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateCashier()
- **Lines:** [343-348]
- **Behavior:** Executes logic for Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateCashier() across lines 343-348.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
