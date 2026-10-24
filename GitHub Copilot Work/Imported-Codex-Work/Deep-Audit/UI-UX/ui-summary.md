# UI/client audit checkpoint

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`.

Scope: **274 tracked files**. File counts: `{"REVIEWED_SOURCE": 224, "NOT_ANALYZED": 50}`. These source-review labels do not assert exhaustive function review or native runtime verification.

Conservative Roslyn callable review map: `{"MANUALLY_REVIEWED_WITH_REPORTED_REASONING": 507, "NOT_ANALYZED": 1954}`. See `Sub-Agents/Results/ui-function-review-ledger.json` for exact source spans and remaining functions.

## Highest-priority findings

- Primary cashier and treasury financial retry identity survives only process memory (UI-001).

- Persisted electronic confirmation/cancellation cannot reconstruct visible checkout context and blocks ordinary recovery (UI-005).

- Pickup kitchen-before-payment stage is not represented in active client (UI-003; backend corroboration separate).

- CallCenter old customer lookup may overwrite latest phone/order identity (UI-007).

- Customer-copy reprint includes kitchen by DTO default in CallCenter (UI-012, printing peer cross-check).

- Restore completion UI expects Completed while actual handler returns Succeeded (UI-014).

- Expense unselected category and payment choices hardcode white surface with pale dark text (UI-002).

## Expense visual root and limitations

The expense category values/bindings were traced to CashierView.xaml:313–340, StringEqualsToBrushConverter, ThemeResources.Get, Colors.xaml, SecondaryButtonStyle and root RequestedTheme. The category model is not the root of pale text: view-instance background overrides use literal WhiteBrush, while unselected text uses themed TextPrimaryBrush. Dark source token #E6E9EE on white has very low computed contrast. Selected state uses accent/white. Checkout payment buttons repeat this pattern.

No screenshot was supplied to this reviewer, no desktop interaction performed, and no pixel rendering/contrast measurements claimed. ActualTheme/system override and live converter refresh remain runtime verification gaps. Do not globally change WhiteBrush because paper and on-accent uses rely on literal white. Stored expense category keys must remain unchanged.

## Runtime and safety

No application UI, database, business data, printer, source/configuration or package changes were made by this reviewer. No tests/builds/application commands were executed. Test source review distinguishes fake HTTP/stub timers/headless linked VM from actual backend/WinUI runtime. User active drafts preserved.

## Per-file report navigation

Authoritative reports: `Deep-Audit/File-Reports/<tracked path>.md`; ledger: `Sub-Agents/Results/ui-ledger.json`; findings/candidates: `Sub-Agents/Results/ui-findings.json`. Function map reports reviewed and NOT_ANALYZED separately. No clean/production-ready claim.

## Remaining partially reviewed files

ZERO

## Remaining unreviewed files

- `src/Falah.Client.CallCenterWinUI/Converters/Converters.cs`

- `src/Falah.Client.CallCenterWinUI/Styles/Colors.xaml`

- `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml`

- `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml`

- `src/Falah.Client.CallCenterWinUI/Views/CallCenterDeliveryEditorView.xaml`

- `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml`

- `src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml`

- `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml`

- `src/Falah.Client.Cashier/Styles/Colors.xaml`

- `src/Falah.Client.Cashier/Styles/Controls.xaml`

- `src/Falah.Client.Cashier/Styles/Typography.xaml`

- `src/Falah.Client.Cashier/Views/AppHeaderView.xaml`

- `src/Falah.Client.Cashier/Views/CashierView.xaml`

- `src/Falah.Client.Cashier/Views/LoginView.xaml`

- `src/Falah.Client.CashierWinUI/Services/ApiClient.cs`

- `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml`

- `src/Falah.Client.CashierWinUI/Views/AuditView.xaml`

- `src/Falah.Client.CashierWinUI/Views/BackupView.xaml`

- `src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml`

- `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml`

- `src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml`

- `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml`

- `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml`

- `src/Falah.Client.CashierWinUI/Views/DailySummaryView.xaml`

- `src/Falah.Client.CashierWinUI/Views/DeliveryDirectoryView.xaml`

- `src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml`

- `src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml`

- `src/Falah.Client.CashierWinUI/Views/EmployeeEditorView.xaml`

- `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml`

- `src/Falah.Client.CashierWinUI/Views/InventoryModalsView.xaml`

- `src/Falah.Client.CashierWinUI/Views/InventoryView.xaml`

- `src/Falah.Client.CashierWinUI/Views/MenuView.xaml`

- `src/Falah.Client.CashierWinUI/Views/OrdersView.xaml`

- `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml`

- `src/Falah.Client.CashierWinUI/Views/PayrollView.xaml`

- `src/Falah.Client.CashierWinUI/Views/PrintingView.xaml`

- `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml`

- `src/Falah.Client.CashierWinUI/Views/PurchasingModalsView.xaml`

- `src/Falah.Client.CashierWinUI/Views/PurchasingView.xaml`

- `src/Falah.Client.CashierWinUI/Views/RecipesView.xaml`

- `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml`

- `src/Falah.Client.CashierWinUI/Views/SettingsView.xaml`

- `src/Falah.Client.CashierWinUI/Views/ShiftsView.xaml`

- `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml`

- `src/Falah.Client.CashierWinUI/Views/UsersView.xaml`

- `src/Falah.Client.CashierWinUI/Views/WasteView.xaml`

- `tests/Falah.Client.Tests/CashierCorrectnessTests.cs`

- `tests/Falah.Client.Tests/CloseoutRegressionTests.cs`

- `tests/Falah.Client.Tests/FinalVerificationTests.cs`

- `tests/Falah.Client.Tests/NativeFooterLayoutProbe.cs`
