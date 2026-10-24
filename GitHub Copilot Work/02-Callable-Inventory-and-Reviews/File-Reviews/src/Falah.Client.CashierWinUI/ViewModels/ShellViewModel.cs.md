# src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `332d91a8710324eadba28d1b9811383f83dc3e68d6ab763a78e477eeb805978f`; worktree SHA256 `d3fec2d8a36d5e7fc1036fd80f3c2ece05069a20f5ec6ea522add8c227dce0a9`; LF-equivalent `True`.

Actual role: Permission-filtered management catalog/grouping and actual page factories.

## Function/resource review

- `ShellViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:16`: Filters real page candidates by permission, payroll OR approve/pay, shifts OR reports, treasury OR manage. Constructs no unauthorized module before filter. Constructor selects first allowed entry; no page if empty.

- `BuildGroups` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:49`: Plan groups related allowed items; skips empty groups, tracks placed keys, unplaced items appended more. Avoids accidental unreachable future page because grouping forgotten.

- `AllCandidates` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:40`: Actual module map includes cashier/orders/delivery/customers/reports/daily statement/summary/shifts/inventory/recipes/waste/purchasing/suppliers/employees/attendance/payroll/menu/users/audit/controlcenter/backup/settings/printing. Gate can be broader or narrower than endpoint functionality; e.g offers-only user cannot see menu entry gated menu_manage, evaluate intended role.

- `Navigate` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:52`: Refuses global unresolved operation and item not already allowed NavItems. Constructs new page VM and real load task per navigation; does not reuse management VM, so unknown not globally registered at construction can be abandoned. Ordinary unsaved forms not registered, navigation discards draft without prompt.

- `CreateOrders` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:173`: New Orders VM, fire-and-forget Load; corresponding simple factories forMenu,Settings,Backup,ControlCenter,Printing,Users,Audit,Employees,Attendance,Payroll,Shifts,Inventory,Reports,DailySummary,DailyStatement,Delivery all same instantiate+load pattern. Only cashier reused through session draft.

- `CreateOwnerDashboard` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:151`: New owner dashboard receives NavigateByKey callback; target nav lookup applies same permissions. CreateCustomers has no eager query until phone entered; Purchasing/suppliers route same VM, role-limited functions in page.

- `CreateCashier` at `src/Falah.Client.CashierWinUI/ViewModels/ShellViewModel.cs:152`: Reuses per-user cached VM, fire-and-forget initialize. User lock can replace shell while read loads pending; no page disposal/cancellation scope here.

## Tests and limitations

Manual flow review, not all factory/accessor callable declarations marked audited by conservative map. Missing per-permission capability matrix and navigation-discard/read cancellation tests.

## Recommendations (not implemented)

Tie page/task lifetime to navigation, register recovered pending before allowing departure, expose dirty-form confirmation only for actual unsaved work; server remains auth authority.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
