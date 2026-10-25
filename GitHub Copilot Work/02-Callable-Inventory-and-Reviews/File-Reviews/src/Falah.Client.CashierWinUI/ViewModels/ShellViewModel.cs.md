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
- **Behavior:** Initializes ShellViewModel with injected AppSession and HeaderViewModel. Filters AllCandidates() against user permissions via _session.HasPermission with specific OR logic for payroll (payroll_approve or payroll_pay), shifts (reports_view), and treasury (treasury_manage). Populates NavItems, calls BuildGroups(), and navigates to the first permitted screen via Navigate(NavItems[0]).
- **Validations:** Evaluates permissions for each navigation module. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.BuildGroups()
- **Lines:** [72-95]
- **Behavior:** Builds the top-bar navigation groups (NavGroups) by grouping permitted NavItems according to GroupPlan (10 logical groups matching the operational flow of a restaurant day). Appends any unmapped permitted modules to the "more" group so no permitted screen is orphaned.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

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
- **Behavior:** Enumerates all 24 potential management modules defined in the application specification with their internal key, Arabic display label, and required backend permission identifier.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:144:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:144:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.Navigate(Falah.Client.CashierWinUI.ViewModels.ShellNavItem)
- **Lines:** [144-177]
- **Behavior:** Core navigation command routing to the chosen module. Guarded against navigating away during in-flight operations (_session.HasUnresolvedOperation) and verifies item is in permitted NavItems. Sets SelectedItem = item, switches on item.Key to call the corresponding factory method, and assigns the instantiated ViewModel to CurrentPage.
- **Validations:** Guarded by if (_session.HasUnresolvedOperation || !NavItems.Contains(item)) return;. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:179:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:179:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateOrders()
- **Lines:** [179-184]
- **Behavior:** Factory method creating OrdersViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateCustomers()
- **Lines:** [186-186]
- **Behavior:** Factory method instantiating and returning CustomersViewModel(_session).
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:188:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:188:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateMenu()
- **Lines:** [188-193]
- **Behavior:** Factory method creating MenuViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:195:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:195:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateSettings()
- **Lines:** [195-200]
- **Behavior:** Factory method creating SettingsViewModel(_session, _header), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:202:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:202:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateBackup()
- **Lines:** [202-207]
- **Behavior:** Factory method creating BackupViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:209:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:209:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateControlCenter()
- **Lines:** [209-214]
- **Behavior:** Factory method creating ControlCenterViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:216:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:216:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePrinting()
- **Lines:** [216-221]
- **Behavior:** Factory method creating PrintingViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:223:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:223:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateUsers()
- **Lines:** [223-228]
- **Behavior:** Factory method creating UsersViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:230:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:230:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateAudit()
- **Lines:** [230-235]
- **Behavior:** Factory method creating AuditViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:237:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:237:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateEmployees()
- **Lines:** [237-242]
- **Behavior:** Factory method creating EmployeesViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:244:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:244:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateAttendance()
- **Lines:** [244-249]
- **Behavior:** Factory method creating AttendanceViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:251:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:251:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePayroll()
- **Lines:** [251-256]
- **Behavior:** Factory method creating PayrollViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:258:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:258:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateRecipes()
- **Lines:** [258-263]
- **Behavior:** Factory method creating RecipesViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:265:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:265:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateWaste()
- **Lines:** [265-270]
- **Behavior:** Factory method creating WasteViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:272:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:272:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreatePurchasing()
- **Lines:** [272-277]
- **Behavior:** Factory method creating PurchasingViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:279:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:279:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateTreasury()
- **Lines:** [279-284]
- **Behavior:** Factory method creating TreasuryViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:286:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:286:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateShifts()
- **Lines:** [286-291]
- **Behavior:** Factory method creating ShiftsViewModel(_session, _header), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:293:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:293:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateInventory()
- **Lines:** [293-298]
- **Behavior:** Factory method creating InventoryViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:300:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:300:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateOwnerDashboard()
- **Lines:** [300-308]
- **Behavior:** Factory method creating OwnerDashboardViewModel(_session, NavigateByKey), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance. Injects NavigateByKey callback to enable dashboard attention rows to navigate directly to resolution modules.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:310:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:310:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.NavigateByKey(string)
- **Lines:** [310-313]
- **Behavior:** Navigation callback finding target ShellNavItem in NavItems by module key and invoking Navigate(target).
- **Validations:** Guarded by null-check on matching item in NavItems. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

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
- **Behavior:** Factory method creating ReportsViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:322:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:322:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDailySummary()
- **Lines:** [322-327]
- **Behavior:** Factory method creating DailySummaryViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:329:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:329:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDailyStatement()
- **Lines:** [329-334]
- **Behavior:** Factory method creating DailyStatementViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:336:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:336:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateDelivery()
- **Lines:** [336-341]
- **Behavior:** Factory method creating DeliveryViewModel(_session), triggers fire-and-forget _ = vm.LoadAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:343:5:MethodDeclaration

- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:343:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.ViewModels.ShellViewModel.CreateCashier()
- **Lines:** [343-348]
- **Behavior:** Factory method retrieving cached CashierViewModel from _session.GetCashier(_header), triggers fire-and-forget _ = vm.InitializeAsync(), and returns the instance.
- **Validations:** None. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
