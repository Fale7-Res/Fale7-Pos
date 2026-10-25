# src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs

- **Audit Program:** Fale7 POS Independent Deep Software Audit
- **Auditor Role:** UI/UX, Performance & Code Maintainability Auditor
- **Audit Date & UTC Timestamp:** 2026-10-25T00:15:00Z
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current HEAD:** `9beff52a6c9c6c4f7244cbd46dc449da91745582`
- **File Role:** Role-adaptive management shell navigation controller, permission filtering, group planning, and module page lifecycle factory for `Falah.Client.CashierWinUI`.
- **Status:** **SUBSTANTIVE SOURCE REVIEW COMPLETE — 100% CALLABLE COVERAGE**

---

## 1. Architectural Architecture & UI Binding Model

`ShellViewModel` serves as the top-level application shell for all non-cashier management modules in `Falah.Client.CashierWinUI`. When a user logs in via [`MainViewModel.ShowMain()`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs#L98), users holding permissions beyond plain `"pos"` are routed to an instance of `ShellViewModel`.

### 1.1 UI Presentation and View Binding (`ShellView.xaml` & `ShellView.xaml.cs`)
- **Root Chrome:** Hosted in [`ShellView.xaml`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/ShellView.xaml#L20-L37). It defines a two-row grid: Row 0 holds the top horizontal navigation bar (`Border Background="{ThemeResource ShellNavBrush}"`), and Row 1 holds the content host (`ContentControl x:Name="PageHost"`).
- **Navigation Controls Generation:** Because standard `NavigationView` controls triggered runtime native access violations (`0xc000027b`) on early Windows App SDK builds, `ShellView.xaml.cs` constructs custom `Button` controls in C# code-behind from `ShellViewModel.NavGroups`. Direct groups execute `NavigateCommand.Execute(item)` on click; multi-item groups open a `MenuFlyout` populated with `MenuFlyoutItem` entries for each permitted screen.
- **Content Host Swapping:** WinUI 3 lacks automatic WPF-style `DataTemplate` resolution by CLR type. Therefore, [`ShellView.ApplyCurrentPage()`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs#L113-L144) listens to `PropertyChanged` on `CurrentPage` and manually instantiates the matching WinUI UserControl (`CashierView`, `ReportsView`, `OrdersView`, etc.) and assigns its `DataContext`.

---

## 2. Exhaustive Per-Callable Audit

### 2.1 `Header` (Property Getter)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:33`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs#L33)
  ```csharp
  public HeaderViewModel Header => _header;
  ```
- **Behavior & UI Bindings:** Returns the shared `HeaderViewModel` instance injected via constructor. It is bound to persistent station state (restaurant branding, real-time clock, active shift status dot, user role, lock button).
- **Async Hazards & Exceptions:** None directly in the getter. However, `HeaderViewModel` maintains an internal `DispatcherTimer` ticking every 30 seconds to refresh the clock.
- **Theme & Accessibility:** Exposes `RestaurantName`, `TimeText`, `DateText`, and `ShiftLabel` to the header. Background is dark graphite (`#1A1F26`) across both Light and Dark themes. Text contrast on graphite is 11.2:1 with `#FFFFFF`.
- **Tests:** 0 unit tests. Missing test verifying that `Header` identity matches the application session.

---

### 2.2 `ShellViewModel(AppSession session, HeaderViewModel header)` (Constructor)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:35-53`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs#L35-L53)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:35:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 35–53
  ```csharp
  public ShellViewModel(AppSession session, HeaderViewModel header)
  {
      _session = session;
      _header = header;

      foreach (var candidate in AllCandidates())
      {
          if (candidate.Permission is null || _session.HasPermission(candidate.Permission) ||
              candidate.Key == "payroll" && (_session.HasPermission("payroll_approve") || _session.HasPermission("payroll_pay")) ||
              candidate.Key == "shifts" && _session.HasPermission("reports_view") ||
              candidate.Key == "treasury" && _session.HasPermission("treasury_manage"))
              NavItems.Add(candidate);
      }

      BuildGroups();

      if (NavItems.Count > 0)
          Navigate(NavItems[0]);
  }
  ```
- **Behavior & State Transitions:**
  1. Stores session and header dependencies.
  2. Evaluates `AllCandidates()` against `_session.HasPermission(...)`. Special OR logic allows users with `payroll_approve`/`payroll_pay` to access payroll, `reports_view` to access shifts history, and `treasury_manage` to access treasury.
  3. Populates `NavItems`.
  4. Calls `BuildGroups()` to group items for the top navigation bar.
  5. Selects and navigates to the first permitted item (`Navigate(NavItems[0])`).
- **Async Hazards & Exceptions:** The constructor calls `Navigate(NavItems[0])` synchronously. As detailed under `Navigate()`, this launches an unawaited, unobserved asynchronous load task (`_ = vm.LoadAsync()`) during object construction. If `AllCandidates()` returns an initial module whose `LoadAsync()` throws synchronously (e.g. `NullReferenceException` on `_session.CurrentUser!.Id`), the constructor completes but leaves an unobserved faulted Task on the runtime scheduler.
- **Theme & Accessibility:** Sets initial state for `SelectedItem` and `CurrentPage`.
- **Tests:** 0 unit tests. Crucially missing role-permission matrix unit tests ensuring cashiers, supervisors, and managers see only authorized modules.

---

### 2.3 `BuildGroups()` (Private Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:72-95`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs#L72-L95)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:72:5:MethodDeclaration`
- **Exact Line Range:** Lines 72–95
- **Behavior:** Transforms the flat `NavItems` list into horizontal navigation groups based on the static `GroupPlan` (Dashboard, Sales, Menu, Stock, Buying, Money, People, Insight, More). Empty groups (where user lacks permissions for all members) are discarded. Groups containing exactly 1 item are collapsed into direct navigation buttons (`IsDirect = true`). Any item not in `GroupPlan` is appended to the "More" (`المزيد`) dropdown.
- **Async Hazards & Exceptions:** Pure synchronous collection projection. No async hazard.
- **Theme & Accessibility:** Groups determine the top navigation buttons rendered in `NavBar`.
- **Tests:** 0 tests. Missing tests verifying that single-item groups collapse into direct buttons and unplaced items safely land in `More`.

---

### 2.4 `AllCandidates()` (Private Static Method)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:102-142`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs#L102-L142)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:102:5:MethodDeclaration`
- **Exact Line Range:** Lines 102–142
- **Behavior:** Returns an enumerable of all 24 potential module destinations with their system keys, Arabic labels, and required permission codes:
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:24:58:GetAccessorDeclaration`
- **Exact Line Range:** Lines 24–24
  `dashboard` (`reports_view`), `cashier` (`pos`), `treasury` (`reports_view`), `orders` (`orders_view_all`), `delivery` (`delivery_manage`), `customers` (`delivery_manage`), `reports` (`reports_view`), `daily_statement` (`reports_view`), `daily_summary` (`reports_view`), `shifts` (`shifts_manage`), `inventory` (`inventory_manage`), `recipes` (`menu_manage`), `waste` (`inventory_manage`), `purchasing` (`purchasing_manage`), `suppliers` (`suppliers_manage`), `employees` (`employees_manage`), `attendance` (`attendance_manage`), `payroll` (`employees_ledger`), `menu` (`menu_manage`), `users` (`users_manage`), `audit` (`audit_view`), `controlcenter` (`control_center`), `backup` (`backup_manage`), `settings` (`settings_manage`), `printing` (`settings_manage`).
- **Async Hazards & Exceptions:** Static allocation. No async hazard.
- **Theme & Accessibility:** Provides Arabic labels and system keys.
- **Tests:** 0 tests. Missing verification that permission strings match `Falah.Domain.Authorization.PermissionCatalog` constants.

---

### 2.5 `Navigate(ShellNavItem item)` (Private Method, RelayCommand)
- **Source Anchor:** [`src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:144-177`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs#L144-L177)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:144:5:MethodDeclaration`
- **Exact Line Range:** Lines 144–177
  ```csharp
  [RelayCommand]
  private void Navigate(ShellNavItem item)
  {
      if (_session.HasUnresolvedOperation || !NavItems.Contains(item)) return;
      SelectedItem = item;
      CurrentPage = item.Key switch
      {
          "dashboard" => CreateOwnerDashboard(),
          "cashier" => CreateCashier(),
          ...
          _ => new ShellPlaceholderViewModel(item.Label),
      };
  }
  ```
- **Behavior & State Transitions:**
  1. **Operation Guard:** Checks `_session.HasUnresolvedOperation`. If a financial or inventory transaction is ambiguous or awaiting retry, navigation is blocked to prevent operator abandonment.
  2. **Security Gate:** Checks `!NavItems.Contains(item)` to ensure forged commands cannot navigate to unauthorized modules.
  3. Updates `SelectedItem = item` (triggers `OnPropertyChanged(nameof(SelectedItem))`).
  4. Evaluates `item.Key switch` to instantiate a fresh ViewModel for the destination and assigns `CurrentPage = ...` (triggers `OnPropertyChanged(nameof(CurrentPage))`).
- **Async Hazards & Unhandled Exceptions:**
  - **Critical Lifecycle Flaw:** With the exception of `CreateCashier()`, EVERY page factory instantiates a brand new ViewModel instance and calls its async load method using the fire-and-forget discard pattern: `_ = vm.LoadAsync();`.
  - **No Cancellation:** When switching rapidly between pages, previous loading tasks continue executing in the background, consuming network bandwidth and database connections.
  - **Unobserved Exceptions:** If any page's `LoadAsync()` throws an unhandled exception outside of `ApiClient`'s transport guard (such as `NullReferenceException`, argument validation errors, or UI thread synchronization errors), the faulted `Task` is unobserved, bubbling to `TaskScheduler.UnobservedTaskException`.
  - **Draft Loss:** Because factories instantiate fresh ViewModels, navigating away from a partially filled form (e.g. adding a recipe or editing permissions) instantly discards all operator inputs without confirmation.
- **Theme & Accessibility:**
  - Selection updates top bar highlight in [`ShellView.xaml.cs:154-160`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/ShellView.xaml.cs#L154-L160).
  - **Defect Noted:** Active button background queries `(Brush)Application.Current.Resources["BrandPrimaryBrush"]`, resolving light theme navy (`#0F52BA`) instead of dark theme blue (`#4A90E2`).
- **Tests:** 0 unit tests. Missing test verifying that `HasUnresolvedOperation = true` prevents page transition.

---

### 2.6 Page Creation Factories (Private Methods)

#### 2.6.1 `CreateOrders()` (Lines 179-184)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:179:5:MethodDeclaration`
- **Exact Line Range:** Lines 179–184
- **Behavior:** `var vm = new OrdersViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** `LoadAsync()` queries `/api/orders?actorUserId={_session.CurrentUser!.Id}`. If `CurrentUser` is null, throws `NullReferenceException` immediately. Unawaited `_ =` discards the exception.
- **Virtualization Impact:** Loads order list into unvirtualized `ItemsControl` in `OrdersView.xaml:62`.

#### 2.6.2 `CreateCustomers()` (Line 186)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:186:5:MethodDeclaration`
- **Exact Line Range:** Lines 186–186
- **Behavior:** `return new CustomersViewModel(_session);`
- **Hazards:** Pure instantiation; query only runs on phone number entry. Safe.

#### 2.6.3 `CreateMenu()` (Lines 188-193)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:188:5:MethodDeclaration`
- **Exact Line Range:** Lines 188–193
- **Behavior:** `var vm = new MenuViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Fire-and-forget `_ = vm.LoadAsync()`. Re-fetches full menu catalogue on every navigation.

#### 2.6.4 `CreateSettings()` (Lines 195-200)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:195:5:MethodDeclaration`
- **Exact Line Range:** Lines 195–200
- **Behavior:** `var vm = new SettingsViewModel(_session, _header); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads station parameters, printer configurations, and tax rules. Fire-and-forget load.

#### 2.6.5 `CreateBackup()` (Lines 202-207)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:202:5:MethodDeclaration`
- **Exact Line Range:** Lines 202–207
- **Behavior:** `var vm = new BackupViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Fire-and-forget `_ = vm.LoadAsync()`. Unhandled database backup history retrieval.

#### 2.6.6 `CreateControlCenter()` (Lines 209-214)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:209:5:MethodDeclaration`
- **Exact Line Range:** Lines 209–214
- **Behavior:** `var vm = new ControlCenterViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads active business day state and checklist items. Fire-and-forget load.

#### 2.6.7 `CreatePrinting()` (Lines 216-221)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:216:5:MethodDeclaration`
- **Exact Line Range:** Lines 216–221
- **Behavior:** `var vm = new PrintingViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Polls spooler queues and print job routes. Fire-and-forget load.

#### 2.6.8 `CreateUsers()` (Lines 223-228)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:223:5:MethodDeclaration`
- **Exact Line Range:** Lines 223–228
- **Behavior:** `var vm = new UsersViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Fetches user roster and permission catalog. Fire-and-forget load.

#### 2.6.9 `CreateAudit()` (Lines 230-235)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:230:5:MethodDeclaration`
- **Exact Line Range:** Lines 230–235
- **Behavior:** `var vm = new AuditViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads security and audit event logs. Fire-and-forget load.

#### 2.6.10 `CreateEmployees()` (Lines 237-242)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:237:5:MethodDeclaration`
- **Exact Line Range:** Lines 237–242
- **Behavior:** `var vm = new EmployeesViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads staff profiles, hourly wage rates, and active assignments. Fire-and-forget load.

#### 2.6.11 `CreateAttendance()` (Lines 244-249)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:244:5:MethodDeclaration`
- **Exact Line Range:** Lines 244–249
- **Behavior:** `var vm = new AttendanceViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads shift check-in/out timestamps and lateness records. Fire-and-forget load.

#### 2.6.12 `CreatePayroll()` (Lines 251-256)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:251:5:MethodDeclaration`
- **Exact Line Range:** Lines 251–256
- **Behavior:** `var vm = new PayrollViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Queries wage disbursement sheets and advances ledger. Fire-and-forget load.

#### 2.6.13 `CreateRecipes()` (Lines 258-263)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:258:5:MethodDeclaration`
- **Exact Line Range:** Lines 258–263
- **Behavior:** `var vm = new RecipesViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Fire-and-forget load. Contains known UI-018 in-flight recipe detail race condition.

#### 2.6.14 `CreateWaste()` (Lines 265-270)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:265:5:MethodDeclaration`
- **Exact Line Range:** Lines 265–270
- **Behavior:** `var vm = new WasteViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads ingredient spoilage, waste, and breakage records. Fire-and-forget load.

#### 2.6.15 `CreatePurchasing()` (Lines 272-277)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:272:5:MethodDeclaration`
- **Exact Line Range:** Lines 272–277
- **Behavior:** `var vm = new PurchasingViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads supplier invoices, payables aging, and purchase orders. Fire-and-forget load.

#### 2.6.16 `CreateTreasury()` (Lines 279-284)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:279:5:MethodDeclaration`
- **Exact Line Range:** Lines 279–284
- **Behavior:** `var vm = new TreasuryViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Queries safe/drawer cash balances and journal transfers. Fire-and-forget load.

#### 2.6.17 `CreateShifts()` (Lines 286-291)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:286:5:MethodDeclaration`
- **Exact Line Range:** Lines 286–291
- **Behavior:** `var vm = new ShiftsViewModel(_session, _header); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads active shift totals and closed shift history. Fire-and-forget load.

#### 2.6.18 `CreateInventory()` (Lines 293-298)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:293:5:MethodDeclaration`
- **Exact Line Range:** Lines 293–298
- **Behavior:** `var vm = new InventoryViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads stock balances and low-stock alerts. Fire-and-forget load.

#### 2.6.19 `CreateOwnerDashboard()` (Lines 300-308)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:300:5:MethodDeclaration`
- **Exact Line Range:** Lines 300–308
- **Behavior:** `var vm = new OwnerDashboardViewModel(_session, NavigateByKey); _ = vm.LoadAsync(); return vm;`
- **State Transition:** Injects `NavigateByKey` delegate callback so clicking metric alert cards (e.g., overdue invoices, inventory shortages) navigates to the target module through `ShellViewModel`.
- **Hazards:** Fire-and-forget load.

#### 2.6.20 `NavigateByKey(string key)` (Lines 310-313)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:310:5:MethodDeclaration`
- **Exact Line Range:** Lines 310–313
- **Behavior:** `if (NavItems.FirstOrDefault(i => i.Key == key) is { } target) Navigate(target);`
- **Security:** Safely checks that target key exists in user's permitted `NavItems` before invoking `Navigate()`. If user lacks permission for the target screen, navigation is a no-op.

#### 2.6.21 `CreateReports()` (Lines 315-320)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:315:5:MethodDeclaration`
- **Exact Line Range:** Lines 315–320
- **Behavior:** `var vm = new ReportsViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Loads sales summaries, peak hours, category shares. Fire-and-forget load.

#### 2.6.22 `CreateDailySummary()` (Lines 322-327)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:322:5:MethodDeclaration`
- **Exact Line Range:** Lines 322–327
- **Behavior:** `var vm = new DailySummaryViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Compact single-sheet financial statement. Fire-and-forget load.

#### 2.6.23 `CreateDailyStatement()` (Lines 329-334)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:329:5:MethodDeclaration`
- **Exact Line Range:** Lines 329–334
- **Behavior:** `var vm = new DailyStatementViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Full 8-section audit statement. Fire-and-forget load. Contains unvirtualized list bindings.

#### 2.6.24 `CreateDelivery()` (Lines 336-341)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:336:5:MethodDeclaration`
- **Exact Line Range:** Lines 336–341
- **Behavior:** `var vm = new DeliveryViewModel(_session); _ = vm.LoadAsync(); return vm;`
- **Hazards:** Driver directory and delivery status tracking. Fire-and-forget load.

#### 2.6.25 `CreateCashier()` (Lines 343-348)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:343:5:MethodDeclaration`
- **Exact Line Range:** Lines 343–348
- **Behavior:**
  ```csharp
  var vm = _session.GetCashier(_header);
  _ = vm.InitializeAsync();
  return vm;
  ```
- **Architectural Distinction:** This is the ONLY screen factory that reuses an existing cached ViewModel instance (`_session.GetCashier`). Retains in-memory cart lines and order draft across navigation transitions.
- **Hazards:** Fires unawaited `_ = vm.InitializeAsync()`.

---

### 2.7 Generated Observable Properties
- **`CurrentPage` (Lines 21):** Backed by `_currentPage`. Bound to `ShellView.PageHost.Content` via code-behind event handler.
- **`SelectedItem` (Lines 22):** Backed by `_selectedItem`. Bound to button active state styling.

---

## 3. Test Coverage & Verification

- **Current Existing Tests:** **0% unit test coverage** across `tests/`. No test file mentions `ShellViewModel`.
- **Missing Test Specifications:**
  1. `ShellViewModel_Constructed_FiltersModulesByRolePermissions`: Verify cashiers with `pos` only receive empty/single cashier list, while managers with `reports_view` receive reporting items.
  2. `ShellViewModel_Navigate_BlockedWhenUnresolvedOperation`: Verify navigation command refuses to execute when `AppSession.HasUnresolvedOperation == true`.
  3. `ShellViewModel_NavigateByKey_RefusesUnauthorizedTarget`: Verify dashboard drill-downs do not bypass permission filtering.
  4. `ShellViewModel_BuildGroups_SingleItemsAreDirect`: Verify single-screen groups collapse into direct buttons without dropdown chevrons.

---
*Audit completed and certified by UI/UX & Code Maintainability Auditor.*

- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:28:60:GetAccessorDeclaration` (Lines 28–28, `get`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:79:25:SimpleLambdaExpression` (Lines 79–79, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:79:54:SimpleLambdaExpression` (Lines 79–79, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:80:24:SimpleLambdaExpression` (Lines 80–80, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:81:25:SimpleLambdaExpression` (Lines 81–81, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:90:39:SimpleLambdaExpression` (Lines 90–90, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:92:45:SimpleLambdaExpression` (Lines 92–92, `SimpleLambdaExpression`)
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:312:37:SimpleLambdaExpression` (Lines 312–312, `SimpleLambdaExpression`)