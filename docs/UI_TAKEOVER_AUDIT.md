# WinUI takeover audit — 2026-09-05

Status: source/build audit recorded; live visual inspection blocked by native screenshot capture. No application, backend, or test source changed during this audit. This is not a final UI acceptance report.

## Repository baseline

- Branch: `master`.
- HEAD: `d78b952ec32b417ab112e161c8616c0af896f63e` — Complete backend: full restaurant OS with hardening pass and 27-item build.
- `backend-complete-v1^{}` resolves to HEAD. No commits exist after either this tag or `d78b952`.
- Previous commits: `dd9637d` (SQL migration progress), `dfc9412` (SQL Server Express migration), `4f3e6a5` (pre-migration checkpoint).
- At takeover: 17 modified tracked files, 23 untracked files, all under `src/Falah.Client.CashierWinUI`. No staged changes; no backend or test source changes.
- No reset, revert, stash, cleanup, checkout, or deletion performed. No applicable AGENTS.md was found in the repository or checked ancestor locations.

Modified tracked paths, relative to `src/Falah.Client.CashierWinUI`:

```text
App.xaml
Converters/Converters.cs
MainWindow.xaml.cs
Services/ApiClient.cs
Styles/Colors.xaml
Styles/Controls.xaml
Styles/Typography.xaml
ViewModels/CashierViewModel.Checkout.cs
ViewModels/CashierViewModel.Expense.cs
ViewModels/CashierViewModel.Shift.cs
ViewModels/CashierViewModel.ShiftClose.cs
ViewModels/CashierViewModel.cs
ViewModels/MainViewModel.cs
Views/CartPanelView.xaml
Views/CashierView.xaml
Views/CategoryStripView.xaml
Views/ProductCatalogueView.xaml
```

Untracked paths at takeover, under the same root:

```text
Controls/EmptyStateView.xaml + .xaml.cs
Controls/ErrorStateView.xaml + .xaml.cs
Controls/LoadingStateView.xaml + .xaml.cs
Controls/PermissionDeniedStateView.xaml + .xaml.cs
Controls/StatusPillView.xaml + .xaml.cs
Controls/SummaryCardView.xaml + .xaml.cs
ViewModels/CashierViewModel.Delivery.cs
ViewModels/CashierViewModel.Operations.cs
ViewModels/ShellNavItem.cs
ViewModels/ShellPlaceholderViewModel.cs
ViewModels/ShellViewModel.cs
Views/CashierOperationsModalsView.xaml + .xaml.cs
Views/ShellPlaceholderView.xaml + .xaml.cs
Views/ShellView.xaml + .xaml.cs
```

The tracked diff contains 1,021 insertions and 90 deletions. Untracked file contents are additional to those counts. This audit document was added after reporting the pre-edit findings.

## Project and API structure

The solution builds Domain, Application, Infrastructure, Server, legacy WPF Cashier, CashierWinUI, Domain.Tests, and Server.IntegrationTests. There is exactly one WinUI project; no separate Call Center project exists.

CashierWinUI targets .NET 8 / Windows 10.0.19041, x64, unpackaged with self-contained Windows App SDK 1.5.240311000 and CommunityToolkit.Mvvm 8.3.2. It references Falah.Application, not Infrastructure. ApiClient uses HTTP at `http://localhost:5151` by default; AppSession constructs that default client. No direct SQL client or DbContext use was found in its authored source. LAN host configuration is not exposed in the current UI.

View composition: MainWindow selects LoginView, CashierView, or ShellView. CashierView composes CartPanelView, CategoryStripView, ProductCatalogueView, and CashierOperationsModalsView; checkout, variant, result, expense, and shift-close overlays remain in CashierView. AppHeaderView uses HeaderViewModel. CashierViewModel is split across core, Checkout, Delivery, Expense, Operations, Shift, and ShiftClose files. ShellView builds native navigation items in code and swaps content explicitly.

Real client calls cover health, PIN login, menu, shift open/prepare-close/close, checkout, refund (method only, no WinUI caller found), electronic confirm/cancel, active delivery services, order lookup/details, approvals, drawer expenses, safe drop, cashier cash movement, drawer-open request, treasury balance, and print-job creation. The remaining server modules have endpoints in Falah.Server/Program.cs but no WinUI client bindings.

Both IMPLEMENTATION_MATRIX.md and LEGACY_PARITY_MAP.md were consulted. The parity map is historical: WPF, SQLite, old dark styling, and many NOT_STARTED statements are stale. The matrix's later remediation sections supersede several early rows. Neither overrides the current user's WinUI/light-design/topology requirements.

## UI coverage

In the table, **P** means `ShellPlaceholderView` with `ShellPlaceholderViewModel`, reached through `ShellViewModel`. P has a static development notice, no module API calls, and no real module loading/error/empty states. Its appearance is only a placeholder, not an implemented workflow. Permission names below describe the current navigation filter, not a complete authorization specification for future commands.

| Area | View / ViewModel | Current real binding and status | Current gate / remaining work |
|---|---|---|---|
| Login / header | LoginView / LoginViewModel; AppHeaderView / HeaderViewModel | PIN API and health polling; login busy/error states | Restaurant identity is hardcoded, not loaded from Settings; dark visual treatment |
| Cashier | CashierView and child views / CashierViewModel partials | Menu, cart, variants, offers, checkout, shifts, comp, split inputs and pending actions wired; partial overall | Shell cashier entry is ungated; backend requires pos. Rebuild composition and fix client defects below |
| Call Center | None | No project, screen, VM, customer lookup/upsert, or current-shift client binding | Build dedicated LAN station; no own shift or drawer |
| Delivery | P; intake fields in CartPanelView / CashierViewModel.Delivery | Services read and structured checkout wired; dispatch absent | delivery_manage; add separate area, saved customers/addresses, compact intake, dispatch/drivers/custody/settlement |
| Manager shell | ShellView / ShellViewModel | Role routing and permission-filtered items exist; every non-cashier item is P | Preserve safe native navigation pattern; preserve draft state during navigation |
| Reports | P | No reports binding | reports_view; real filters, totals, tables and unknown/pending distinctions |
| Inventory | P | No inventory binding | inventory_manage; items, balances, movement history and counts |
| Recipes | P | No recipes binding | Currently inventory_manage; recipe writes actually use menu_manage; costing also needs reports_view |
| Waste | P | No waste binding | inventory_manage; quantities, reasons, actual cost completeness |
| Purchasing | P | No purchasing binding | purchasing_manage; receipt lines, invoice preview and payments |
| Suppliers | P | No suppliers binding | suppliers_manage; roster and ledger; reconcile purchasing permission alternative |
| Treasury | P; operations modals / CashierViewModel.Operations | Safe drop and drawer/main cash movements wired; balance read only | treasury_manage navigation; safe_drop/cash_drawer approval paths in operations; standalone treasury and denomination entry missing |
| Expenses | CashierView expense overlay / CashierViewModel.Expense | Drawer expense POST wired; validation/error and submit guard | expenses_add_drawer or approval; management history and non-drawer expense UI absent |
| Employees | P | No employee binding | employees_manage; roster, ledger, advances and withdrawals |
| Attendance | P | No attendance binding | attendance_manage; entry/correction/history and real-device boundary |
| Payroll / daily wages | P | No payroll binding | Currently employees_ledger; backend also distinguishes employees_manage, payroll_approve and payroll_pay |
| Users / permissions | P | Only login uses users DTOs; management absent | users_manage; server permission catalog and owner restrictions |
| Audit | P | No audit binding | audit_view; read-only filters and detail |
| Settings | P | No settings binding | settings_manage; identity, operational settings and printer configuration |
| Menu / tax / offers administration | P for menu; no dedicated tax/offers screen | Read-side POS consumption only | menu_manage / settings_manage / offers_manage as appropriate |
| Backup / restore | P | No backup binding | backup_manage; history/create/verify plus explicit destructive restore gate |
| Control Center | P | No control-center binding | control_center; business-day/checklist/blockers/exceptions/snapshot flows |
| Printing / hardware | Operations modals / CashierViewModel.Operations | Creates reprint jobs and drawer-open request records only | No station transport, completion reporting, route-status screen, or physical verification |

## Existing work to preserve

Preserve real DTO/API use, server authority, per-command approval semantics, synchronous submission guards, checkout's retained request ID, offer tap event handling, split XAML structure, brush-based selected states, code-built NavigationView items, and checkout scrolling. Shared loading/error/empty/permission-denied/status/summary controls exist, but their existence does not mean module screens use them.

## Source-confirmed defects and takeover risks

1. **Discount approval mismatch:** Checkout.cs obtains discount approval IDs but cannot send them in CheckoutOrderRequest. The server explicitly requires the cashier's own orders_discount permission. The UI promises an unsupported approval route. Correct the client to the existing contract.
2. **Retry safety:** Expense, safe drop, cash movement, drawer-open, shift-open and shift-close mint new IDs inside submission. A lost response followed by retry may become a distinct operation. Synchronous double-click guards do not solve uncertain-response retries.
3. **Transport failures:** most financial/menu methods use try/finally without catching HTTP/time-out failures, while ApiClient does not wrap transport errors. The expected error UI is therefore not reached for those failures.
4. **Cash entry:** the exact-amount button passes GrandTotalDisplay including the currency suffix; QuickCash copies it directly into input that SubmitPayment parses as a decimal. ChangeDuePiastres parses the input as an integer and subtracts piastres without converting EGP to piastres. Both require client fixes.
5. **Draft/session loss:** shell navigation constructs a fresh CashierViewModel; locking replaces the current view without retaining its cart. AppSession has no draft store or current-shift recovery; restart cannot discover an already-open shift through the current client.
6. **Delivery:** service Name is sent as DeliveryArea; no independent geographic-area field. Service loading marks itself loaded before success and offers no retry/error UI. Delivery fields occupy a non-scrollable cart header. No saved-customer/address calls exist. Delivery selection changes do not notify the dependent total properties.
7. **Cash Count:** reads and exposes tre-drawer balance, rather than accepting/saving denomination counts. This also needs review against the required blind-count and field-visibility behavior.
8. **Orders:** lookup/reprint exists; the refund API method has no WinUI caller. Pending actions operate on LastCheckoutResult, not a complete order-history workflow. The result overlay always says payment succeeded even for a pending electronic payment.
9. **Layout:** category cells are fixed at 84 DIP despite taller card content; product width logic assumes at least four columns; cart names sit in horizontal stacks that can exceed their available width. Several modal bodies are unscrollable and several controls explicitly reduce minimum touch height. Actual clipping remains unverified.
10. **Visual direction:** current header/cart/login are dark; category cards and gradient/emoji product bands do not deliver the requested light, photography-led composition. No product-photo binding exists in the current product view. Photography sourcing/mapping remains to be established.
11. **Permissions:** recipe and payroll navigation gates do not fully match their future workflows; permission-denied shared control is not a substitute for implementing those gates. Current cashier shell item has no pos filter.
12. **Hardware language:** reprint success text assumes a station will print, although no station agent exists. Hardware request acceptance must remain distinct from physical success.

These are source findings, not claims of completed UI regression tests. No backend bug was proven or changed during this audit.

## Validation and blocker

- `dotnet build Falah.sln --no-restore -v minimal`: succeeded, 0 warnings, 0 errors, before any edit.
- Domain tests: 255 passed, 0 failed.
- Initial sandboxed integration run: 7 passed, 427 failed during SQL setup, with "requires encryption but this machine does not support it." An unrestricted isolated probe passed; do not count these setup failures as 427 backend regressions.
- Full unrestricted integration suite: **432 passed, 2 failed, 0 skipped, 434 total**, duration **16 minutes 37 seconds**. Failures: `EmployeeVerticalSliceTests.Six_concurrent_advance_requests_with_the_same_request_id_post_exactly_once` and `UserManagementVerticalSliceTests.Repeated_failed_logins_from_the_same_client_eventually_hit_the_rate_limit`. Both subsequently passed in isolated reruns. The unrestricted menu-management probe also passed. These reruns do not erase the full-run failures; their causes are not established. Combined with the domain run, the baseline is **687 passing tests out of 689**, not a reproduced 689/689 green checkpoint.
- Console logs are retained in `%TEMP%/falah-takeover-integration.log`, `%TEMP%/falah-takeover-advance.log`, and `%TEMP%/falah-takeover-ratelimit.log`. A read-only SQL status sample during the long run showed test-database activity and no blocker for the sampled request; no SQL configuration was changed.
- Test infrastructure uses uniquely named FalahTest databases, not the restaurant database. Restore tests cover gates, not an actual destructive restore.
- Running window `فلاح - نقطة البيع` was found. Native screenshot capture failed twice, including fresh window selection, with `SetIsBorderRequired failed: No such interface supported (0x80004002)`.
- Consequently no actual rendered-screen, long-Arabic-name, large-cart/catalog, dense-table, DPI, physical-printer, drawer, or two-machine LAN acceptance is claimed.

## Execution order after takeover verification

1. Restore native visual inspection; rebuild Cashier header, category navigation, product cards/grid, light cart, totals, checkout and operations placement. Repair the demonstrated client defects while preserving backend contracts.
2. Separate Call Center WinUI station using shared design/API components, current-shift discovery and customer/address intake.
3. Delivery dispatch, drivers and settlement.
4. Management/reports, treasury and expense administration.
5. Inventory, recipes, waste and costing.
6. Purchasing and suppliers.
7. Employees, attendance, daily wages and payroll.
8. Users/permissions and audit.
9. Settings, menu, tax and offers administration.
10. Backup/verify/restore UI; actual restore validation only on an isolated target.
11. Control Center and business-day closing.
12. Printing/hardware status and station integration for Receipt, CallCenter, Charcoal and Grill. No charcoal/grill screen.
13. Visual consistency and real Arabic/catalog/cart/table/DPI, physical hardware and two-machine LAN verification. Offline Internet operation must not imply the cashier can work without its authoritative LAN server.
