# 07 — Source and Evidence Index

> Documentation cleanup (2026-11-08): imported audits and older reports are preserved in [the verified archive](../docs/archive/documentation-20261108.zip). Original historical paths below identify archive entries; use [the archive guide](../docs/archive/README.md) and its manifest to locate them. Current source, canonical issues and implementation evidence remain outside the archive.


Traceability between source code, canonical issues, historical aliases, earlier audits and test evidence. All paths
are repository-relative. Line numbers refer to the working tree read on 2026-11-04/05: branch
`clients-and-operations-audit`, HEAD `b3071be`, plus uncommitted changes.

**How line numbers were checked:**
- Every one of the 155 cited source files exists.
- All 411 cited line numbers were checked to fall within their file's length (two out-of-range citations were
  corrected before publication).
- Line numbers drift as the tree changes, so the **symbol names** in each issue record are the durable reference.
## A. Source file → canonical issues (generated from the issue records)

| Source file | Issues (cited lines) | Exists now |
|---|---|---|
| `docs/DAILY_OPERATIONS_VERIFICATION.md` | [OPS-0004](ISSUES/OPS-0004.md) | yes |
| `docs/DEPLOYMENT.md` | [DOC-0003](ISSUES/DOC-0003.md) | yes |
| `docs/LAN_DEPLOYMENT.md` | [SEC-0006](ISSUES/SEC-0006.md); [DOC-0003](ISSUES/DOC-0003.md) | yes |
| `docs/METRIC_DEFINITIONS.md` | [FIN-0038](ISSUES/FIN-0038.md); [RPT-0004](ISSUES/RPT-0004.md); [DOC-0003](ISSUES/DOC-0003.md) | yes |
| `docs/REMEDIATION_PLAN.md` | [DOC-0003](ISSUES/DOC-0003.md) | yes |
| `docs/REMEDIATION_PROGRESS.md` | [FIN-0029](ISSUES/FIN-0029.md); [DOC-0001](ISSUES/DOC-0001.md) (156,376); [DOC-0003](ISSUES/DOC-0003.md) | yes |
| `src/Falah.Application/Customers/CustomerRequests.cs` | [CUS-0001](ISSUES/CUS-0001.md) (13-18) | yes |
| `src/Falah.Application/Employees/EmployeeRequests.cs` | [SEC-0001](ISSUES/SEC-0001.md) (23-27) | yes |
| `src/Falah.Application/Inventory/StockMovementRequests.cs` | [INV-0008](ISSUES/INV-0008.md) (23-26) | yes |
| `src/Falah.Application/Printing/PrintingRequests.cs` | [PRT-0014](ISSUES/PRT-0014.md) (17) | yes |
| `src/Falah.Application/Settings/SettingsRequests.cs` | [PAYR-0019](ISSUES/PAYR-0019.md) (13); [SEC-0005](ISSUES/SEC-0005.md) (7-13) | yes |
| `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs` | [API-0004](ISSUES/API-0004.md) (30); [ARCH-0002](ISSUES/ARCH-0002.md) | yes |
| `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs` | [PRT-0014](ISSUES/PRT-0014.md) (337-343); [UI-0010](ISSUES/UI-0010.md) (384-391); [UI-0022](ISSUES/UI-0022.md) (272-277); [UI-0023](ISSUES/UI-0023.md) (275-277) | yes |
| `src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs` | [UI-0008](ISSUES/UI-0008.md) (51-79) | yes |
| `src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs` | [UI-0013](ISSUES/UI-0013.md) (16-20) | yes |
| `src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs` | [UI-0011](ISSUES/UI-0011.md) (29-33) | yes |
| `src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs` | [UI-0024](ISSUES/UI-0024.md) (82) | yes |
| `src/Falah.Client.CashierWinUI/Services/ApiClient.cs` | [MENU-0002](ISSUES/MENU-0002.md) (83); [SEC-0009](ISSUES/SEC-0009.md) (160-166); [API-0003](ISSUES/API-0003.md) (83); [API-0004](ISSUES/API-0004.md) (30); [ARCH-0002](ISSUES/ARCH-0002.md) | yes |
| `src/Falah.Client.CashierWinUI/Services/AppSession.cs` | [MENU-0002](ISSUES/MENU-0002.md) (12); [SEC-0006](ISSUES/SEC-0006.md) (11,); [UI-0026](ISSUES/UI-0026.md) (33-34) | yes |
| `src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs` | [API-0003](ISSUES/API-0003.md) (41-46) | yes |
| `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs` | [SEC-0008](ISSUES/SEC-0008.md) (49-56,73) | yes |
| `src/Falah.Client.CashierWinUI/Services/ThemeResources.cs` | [UI-0005](ISSUES/UI-0005.md) (18-35) | yes |
| `src/Falah.Client.CashierWinUI/Styles/Colors.xaml` | [UI-0020](ISSUES/UI-0020.md) (76,78,162-163) | yes |
| `src/Falah.Client.CashierWinUI/Styles/Typography.xaml` | [UI-0019](ISSUES/UI-0019.md) (38-40,103-106,113-115) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs` | [UI-0012](ISSUES/UI-0012.md) (273) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs` | [FIN-0020](ISSUES/FIN-0020.md) (151) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs` | [UI-0001](ISSUES/UI-0001.md) (436-440); [UI-0006](ISSUES/UI-0006.md) (137-138) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs` | [UI-0007](ISSUES/UI-0007.md) (131-137) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Durability.cs` | [FIN-0025](ISSUES/FIN-0025.md) (59-65); [UI-0002](ISSUES/UI-0002.md) (22-26); [UI-0003](ISSUES/UI-0003.md) (40-95) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs` | [ARCH-0004](ISSUES/ARCH-0004.md) (234) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs` | [FIN-0005](ISSUES/FIN-0005.md) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs` | [PERF-0006](ISSUES/PERF-0006.md) (101-106) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/FinancialControlViewModel.cs` | [RPT-0003](ISSUES/RPT-0003.md) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/InventoryViewModel.cs` | [INV-0007](ISSUES/INV-0007.md) (331-337) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs` | [MENU-0001](ISSUES/MENU-0001.md) (334); [MENU-0002](ISSUES/MENU-0002.md) (17); [UI-0017](ISSUES/UI-0017.md) (257-284) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs` | [UI-0014](ISSUES/UI-0014.md) (230) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs` | [PRT-0015](ISSUES/PRT-0015.md) (116-124) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs` | [UI-0015](ISSUES/UI-0015.md) (384-400); [UI-0016](ISSUES/UI-0016.md) (562); [UI-0018](ISSUES/UI-0018.md) (451) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/ReportsViewModel.cs` | [INV-0011](ISSUES/INV-0011.md) (169) | yes |
| `src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs` | [SEC-0008](ISSUES/SEC-0008.md) (14-15); [SEC-0009](ISSUES/SEC-0009.md) (163-172) | yes |
| `src/Falah.Client.CashierWinUI/Views/AttendanceView.xaml` | [PAYR-0009](ISSUES/PAYR-0009.md) (92-93) | yes |
| `src/Falah.Client.CashierWinUI/Views/CashierView.xaml` | [UI-0004](ISSUES/UI-0004.md) (321-333) | yes |
| `src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs` | [UI-0009](ISSUES/UI-0009.md) (27-28) | yes |
| `src/Falah.Domain/Authorization/ApprovalRegistry.cs` | [SEC-0014](ISSUES/SEC-0014.md) (87); [SEC-0017](ISSUES/SEC-0017.md) (125) | yes |
| `src/Falah.Domain/Authorization/PinHasher.cs` | [SEC-0011](ISSUES/SEC-0011.md) (21); [SEC-0028](ISSUES/SEC-0028.md); [SEC-0031](ISSUES/SEC-0031.md) (21,40) | yes |
| `src/Falah.Domain/Authorization/SessionToken.cs` | [SEC-0007](ISSUES/SEC-0007.md) (35-38) | yes |
| `src/Falah.Domain/Authorization/UserManagementRules.cs` | [SEC-0014](ISSUES/SEC-0014.md) (13,22) | yes |
| `src/Falah.Domain/BusinessDay/BusinessDayRules.cs` | [FIN-0014](ISSUES/FIN-0014.md) (89-90); [FIN-0019](ISSUES/FIN-0019.md) (125-134) | yes |
| `src/Falah.Domain/Common/BusinessClock.cs` | [FIN-0028](ISSUES/FIN-0028.md) (35-41); [RPT-0001](ISSUES/RPT-0001.md) (45-46) | yes |
| `src/Falah.Domain/Common/Money.cs` | [FIN-0026](ISSUES/FIN-0026.md) (47) | yes |
| `src/Falah.Domain/Delivery/DeliveryRules.cs` | [FIN-0003](ISSUES/FIN-0003.md) (11-12) | yes |
| `src/Falah.Domain/Financial/FinancialRules.cs` | [FIN-0004](ISSUES/FIN-0004.md) (13-24) | yes |
| `src/Falah.Domain/Inventory/CostingRules.cs` | [INV-0003](ISSUES/INV-0003.md) (136-141) | yes |
| `src/Falah.Domain/Inventory/PurchasingRules.cs` | [INV-0009](ISSUES/INV-0009.md) (77-81) | yes |
| `src/Falah.Domain/Orders/CheckoutRules.cs` | [FIN-0004](ISSUES/FIN-0004.md) (56-62); [FIN-0015](ISSUES/FIN-0015.md) (35-39); [FIN-0030](ISSUES/FIN-0030.md) (92-97); [MENU-0003](ISSUES/MENU-0003.md) (102-103) | yes |
| `src/Falah.Domain/Orders/Order.cs` | [FIN-0002](ISSUES/FIN-0002.md) (243); [FIN-0005](ISSUES/FIN-0005.md) (243); [FIN-0007](ISSUES/FIN-0007.md) (240-252); [FIN-0025](ISSUES/FIN-0025.md) (245-247) | yes |
| `src/Falah.Domain/Orders/OrderEnums.cs` | [UI-0025](ISSUES/UI-0025.md) (11) | yes |
| `src/Falah.Domain/Orders/OrderPaymentQueries.cs` | [FIN-0002](ISSUES/FIN-0002.md) | yes |
| `src/Falah.Domain/Payroll/PayrollRules.cs` | [PAYR-0001](ISSUES/PAYR-0001.md) (13-21); [PAYR-0002](ISSUES/PAYR-0002.md) (106); [PAYR-0007](ISSUES/PAYR-0007.md) (101-103); [PAYR-0015](ISSUES/PAYR-0015.md) (64-68); [PAYR-0017](ISSUES/PAYR-0017.md); [PAYR-0019](ISSUES/PAYR-0019.md) (44-49,) | yes |
| `src/Falah.Domain/Reports/SalesRules.cs` | [FIN-0009](ISSUES/FIN-0009.md) (85-86) | yes |
| `src/Falah.Infrastructure/Approvals/ApprovalPinAttemptGuard.cs` | [SEC-0019](ISSUES/SEC-0019.md) | yes |
| `src/Falah.Infrastructure/Authorization/LoginAttemptGuard.cs` | [SEC-0021](ISSUES/SEC-0021.md) | yes |
| `src/Falah.Infrastructure/Authorization/SessionService.cs` | [SEC-0013](ISSUES/SEC-0013.md) (56); [UI-0026](ISSUES/UI-0026.md) (51-63) | yes |
| `src/Falah.Infrastructure/Backup/CreateBackupHandler.cs` | [DB-0004](ISSUES/DB-0004.md) (56); [OPS-0002](ISSUES/OPS-0002.md) (49) | yes |
| `src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs` | [DB-0005](ISSUES/DB-0005.md) (85); [DB-0006](ISSUES/DB-0006.md) (163-167) | yes |
| `src/Falah.Infrastructure/Backup/ScheduledBackupService.cs` | [DB-0004](ISSUES/DB-0004.md) (139-151) | yes |
| `src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs` | [FIN-0013](ISSUES/FIN-0013.md) (14-29) | yes |
| `src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs` | [FIN-0022](ISSUES/FIN-0022.md) | yes |
| `src/Falah.Infrastructure/CashDrawer/CashDrawerHandlers.cs` | [HW-0001](ISSUES/HW-0001.md) (134-146) | yes |
| `src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs` | [PERF-0003](ISSUES/PERF-0003.md) (28-31,) | yes |
| `src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs` | [FIN-0021](ISSUES/FIN-0021.md) (91) | yes |
| `src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs` | [FIN-0020](ISSUES/FIN-0020.md) (41-42) | yes |
| `src/Falah.Infrastructure/Customers/UpsertCustomerHandler.cs` | [CUS-0001](ISSUES/CUS-0001.md) (13-15) | yes |
| `src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs` | [FIN-0003](ISSUES/FIN-0003.md) (29-30) | yes |
| `src/Falah.Infrastructure/Delivery/DeliveryServiceHandlers.cs` | [FIN-0016](ISSUES/FIN-0016.md) (51) | yes |
| `src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs` | [FIN-0036](ISSUES/FIN-0036.md) (109,152) | yes |
| `src/Falah.Infrastructure/Employees/AttendanceHandlers.cs` | [PAYR-0009](ISSUES/PAYR-0009.md) (31-33) | yes |
| `src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs` | [PAYR-0002](ISSUES/PAYR-0002.md) (99-116); [PAYR-0005](ISSUES/PAYR-0005.md) (82-94) | yes |
| `src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs` | [PAYR-0004](ISSUES/PAYR-0004.md) (48-63); [PAYR-0008](ISSUES/PAYR-0008.md) (42-47) | yes |
| `src/Falah.Infrastructure/Employees/EmployeeLedgerPeriods.cs` | [PAYR-0008](ISSUES/PAYR-0008.md) (20-21) | yes |
| `src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs` | [PAYR-0013](ISSUES/PAYR-0013.md) (15-30) | yes |
| `src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs` | [PAYR-0012](ISSUES/PAYR-0012.md) (33,82) | yes |
| `src/Falah.Infrastructure/Employees/PayPayrollHandler.cs` | [PAYR-0003](ISSUES/PAYR-0003.md) (64,73); [PAYR-0004](ISSUES/PAYR-0004.md) (142-151) | yes |
| `src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs` | [PAYR-0010](ISSUES/PAYR-0010.md) (50); [PAYR-0014](ISSUES/PAYR-0014.md) (45) | yes |
| `src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs` | [PAYR-0016](ISSUES/PAYR-0016.md) (122-130) | yes |
| `src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs` | [PAYR-0011](ISSUES/PAYR-0011.md) (35-37) | yes |
| `src/Falah.Infrastructure/Employees/SalaryAccrual.cs` | [PAYR-0006](ISSUES/PAYR-0006.md) (41,58) | yes |
| `src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs` | [PAYR-0001](ISSUES/PAYR-0001.md) (36-56) | yes |
| `src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs` | [FIN-0023](ISSUES/FIN-0023.md) (53-55) | yes |
| `src/Falah.Infrastructure/Expenses/ExpenseQueryService.cs` | [SEC-0014](ISSUES/SEC-0014.md) (26) | yes |
| `src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs` | [INV-0001](ISSUES/INV-0001.md) (84-100) | yes |
| `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs` | [INV-0004](ISSUES/INV-0004.md) (63-64); [INV-0012](ISSUES/INV-0012.md); [PERF-0005](ISSUES/PERF-0005.md) (20-26) | yes |
| `src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs` | [INV-0002](ISSUES/INV-0002.md) (83-108) | yes |
| `src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs` | [INV-0007](ISSUES/INV-0007.md) (26-69) | yes |
| `src/Falah.Infrastructure/Menu/ProductHandlers.cs` | [MENU-0001](ISSUES/MENU-0001.md) (36,101); [MENU-0002](ISSUES/MENU-0002.md) (122-124) | yes |
| `src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs` | [FIN-0002](ISSUES/FIN-0002.md) (32-76); [FIN-0037](ISSUES/FIN-0037.md) (63-64) | yes |
| `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs` | [FIN-0001](ISSUES/FIN-0001.md) (148-153); [FIN-0002](ISSUES/FIN-0002.md) (253-266); [FIN-0004](ISSUES/FIN-0004.md) (259-260); [FIN-0010](ISSUES/FIN-0010.md) (47-49); [FIN-0015](ISSUES/FIN-0015.md) (65-67); [FIN-0031](ISSUES/FIN-0031.md) (60-63) | yes |
| `src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs` | [FIN-0001](ISSUES/FIN-0001.md) (75-78); [FIN-0014](ISSUES/FIN-0014.md) (79-81) | yes |
| `src/Falah.Infrastructure/Orders/MenuPricingService.cs` | [MENU-0001](ISSUES/MENU-0001.md) (53); [MENU-0003](ISSUES/MENU-0003.md) (53) | yes |
| `src/Falah.Infrastructure/Orders/OrderMapper.cs` | [UI-0022](ISSUES/UI-0022.md) (37) | yes |
| `src/Falah.Infrastructure/Orders/OrderStockDisposition.cs` | [FIN-0036](ISSUES/FIN-0036.md); [INV-0005](ISSUES/INV-0005.md) | yes |
| `src/Falah.Infrastructure/Orders/RefundOrderHandler.cs` | [FIN-0007](ISSUES/FIN-0007.md) (91-93); [FIN-0008](ISSUES/FIN-0008.md) (99-115); [FIN-0025](ISSUES/FIN-0025.md) (46-49); [FIN-0032](ISSUES/FIN-0032.md) (112); [FIN-0040](ISSUES/FIN-0040.md) (101-106) | yes |
| `src/Falah.Infrastructure/Persistence/DemoDataSeeder.cs` | [SEC-0024](ISSUES/SEC-0024.md) (44-45) | yes |
| `src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs` | [SEC-0017](ISSUES/SEC-0017.md) (17,30) | yes |
| `src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs` | [FIN-0001](ISSUES/FIN-0001.md) (22-27); [ARCH-0006](ISSUES/ARCH-0006.md) (56-57) | yes |
| `src/Falah.Infrastructure/Persistence/Entities/BackupRecordEntity.cs` | [UI-0012](ISSUES/UI-0012.md) (4) | yes |
| `src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs` | [DB-0009](ISSUES/DB-0009.md) (116) | yes |
| `src/Falah.Infrastructure/Persistence/FalahDbContext.cs` | [FIN-0021](ISSUES/FIN-0021.md) (405); [FIN-0022](ISSUES/FIN-0022.md) (137-141); [PAYR-0003](ISSUES/PAYR-0003.md) (460-477); [MENU-0002](ISSUES/MENU-0002.md) (108); [PERF-0001](ISSUES/PERF-0001.md) (132-135); [PERF-0002](ISSUES/PERF-0002.md) (411-415) | yes |
| `src/Falah.Infrastructure/Persistence/Migrations/20260911173753_RenameDineInOrdersToPickup.cs` | [DB-0002](ISSUES/DB-0002.md) (18) | yes |
| `src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs` | [FIN-0031](ISSUES/FIN-0031.md) (36,88) | yes |
| `src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs` | [SEC-0030](ISSUES/SEC-0030.md) (70-75); [PRT-0001](ISSUES/PRT-0001.md) (34-38) | yes |
| `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs` | [PRT-0016](ISSUES/PRT-0016.md) (58-72) | yes |
| `src/Falah.Infrastructure/Printing/PrintJobQueryService.cs` | [SEC-0004](ISSUES/SEC-0004.md) (287-289); [PRT-0001](ISSUES/PRT-0001.md) (42-56); [PRT-0003](ISSUES/PRT-0003.md) (42-56); [PRT-0013](ISSUES/PRT-0013.md) (51); [PRT-0015](ISSUES/PRT-0015.md); [PRT-0017](ISSUES/PRT-0017.md) (209) | yes |
| `src/Falah.Infrastructure/Printing/ReportPrintJobResultHandler.cs` | [PRT-0002](ISSUES/PRT-0002.md) (39-51) | yes |
| `src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs` | [FIN-0012](ISSUES/FIN-0012.md) (61-66) | yes |
| `src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs` | [PERF-0004](ISSUES/PERF-0004.md) (15-20) | yes |
| `src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs` | [FIN-0011](ISSUES/FIN-0011.md) (75-79); [INV-0009](ISSUES/INV-0009.md) (59-63) | yes |
| `src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs` | [INV-0006](ISSUES/INV-0006.md) (337) | yes |
| `src/Falah.Infrastructure/Reports/ReportingQueryService.cs` | [FIN-0006](ISSUES/FIN-0006.md) (617-620); [FIN-0014](ISSUES/FIN-0014.md) (180); [FIN-0024](ISSUES/FIN-0024.md) (196); [FIN-0039](ISSUES/FIN-0039.md) (198); [INV-0011](ISSUES/INV-0011.md) (375-378); [RPT-0001](ISSUES/RPT-0001.md) (245-250) | yes |
| `src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs` | [FIN-0010](ISSUES/FIN-0010.md) (62-63) | yes |
| `src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs` | [FIN-0011](ISSUES/FIN-0011.md) (57-59); [FIN-0034](ISSUES/FIN-0034.md) (54) | yes |
| `src/Falah.Infrastructure/Shifts/PrepareShiftCloseHandler.cs` | [FIN-0020](ISSUES/FIN-0020.md) (47) | yes |
| `src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs` | [FIN-0008](ISSUES/FIN-0008.md) (46-63); [FIN-0050](ISSUES/FIN-0050.md) (59-79) | yes |
| `src/Falah.Infrastructure/Treasury/DepositHandler.cs` | [FIN-0013](ISSUES/FIN-0013.md) (58) | yes |
| `src/Falah.Infrastructure/Treasury/SafeDropHandler.cs` | [FIN-0023](ISSUES/FIN-0023.md) (45) | yes |
| `src/Falah.Infrastructure/Treasury/TransferHandler.cs` | [DB-0010](ISSUES/DB-0010.md) (63) | yes |
| `src/Falah.Infrastructure/Treasury/TreasuryMapper.cs` | [DB-0010](ISSUES/DB-0010.md) (37-46); [PERF-0001](ISSUES/PERF-0001.md) (15-19) | yes |
| `src/Falah.Infrastructure/Users/CreateUserHandler.cs` | [SEC-0014](ISSUES/SEC-0014.md) (38) | yes |
| `src/Falah.Infrastructure/Users/PinChoiceGuard.cs` | [SEC-0011](ISSUES/SEC-0011.md); [SEC-0020](ISSUES/SEC-0020.md) | yes |
| `src/Falah.Infrastructure/Users/PinUniquenessChecker.cs` | [SEC-0010](ISSUES/SEC-0010.md) (23-28) | yes |
| `src/Falah.Infrastructure/Users/SetUserActiveHandler.cs` | [SEC-0012](ISSUES/SEC-0012.md) (27-62) | yes |
| `src/Falah.PrintAgent/AgentOptions.cs` | [PRT-0003](ISSUES/PRT-0003.md) (52) | yes |
| `src/Falah.PrintAgent/appsettings.json` | [SEC-0024](ISSUES/SEC-0024.md); [DOC-0001](ISSUES/DOC-0001.md) | yes |
| `src/Falah.PrintAgent/BitmapTicketRenderer.cs` | [PRT-0009](ISSUES/PRT-0009.md) | yes |
| `src/Falah.PrintAgent/CashDrawerKicker.cs` | [HW-0002](ISSUES/HW-0002.md) | yes |
| `src/Falah.PrintAgent/PrintJobProcessor.cs` | [PRT-0003](ISSUES/PRT-0003.md) (68-78); [PRT-0004](ISSUES/PRT-0004.md) (106-108); [PRT-0010](ISSUES/PRT-0010.md) (106-108) | yes |
| `src/Falah.PrintAgent/PrintServerClient.cs` | [PRT-0006](ISSUES/PRT-0006.md) (74-79) | yes |
| `src/Falah.PrintAgent/Program.cs` | [SEC-0023](ISSUES/SEC-0023.md) (150-152); [PRT-0010](ISSUES/PRT-0010.md) (185-190); [PRT-0011](ISSUES/PRT-0011.md) (21); [PRT-0012](ISSUES/PRT-0012.md) (141-147); [HW-0001](ISSUES/HW-0001.md) (178-179); [HW-0002](ISSUES/HW-0002.md) (161) | yes |
| `src/Falah.PrintAgent/Transports.cs` | [PRT-0004](ISSUES/PRT-0004.md) (26-29) | yes |
| `src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs` | [PRT-0004](ISSUES/PRT-0004.md) (29-40); [PRT-0005](ISSUES/PRT-0005.md) (29-33) | yes |
| `src/Falah.Server/appsettings.json` | [SEC-0006](ISSUES/SEC-0006.md) (10) | yes |
| `src/Falah.Server/AuthenticationMiddleware.cs` | [SEC-0001](ISSUES/SEC-0001.md) (22-26); [SEC-0015](ISSUES/SEC-0015.md) (90-93); [SEC-0016](ISSUES/SEC-0016.md) (110-120) | yes |
| `src/Falah.Server/Program.cs` | [INV-0008](ISSUES/INV-0008.md) (1041-1042); [MENU-0002](ISSUES/MENU-0002.md) (1723-1732); [CUS-0001](ISSUES/CUS-0001.md) (1246-1256); [CUS-0002](ISSUES/CUS-0002.md); [SEC-0001](ISSUES/SEC-0001.md) (778-790); [SEC-0002](ISSUES/SEC-0002.md) (1841,); [SEC-0003](ISSUES/SEC-0003.md) (1958-1962); [SEC-0004](ISSUES/SEC-0004.md) (2363-2364); [SEC-0005](ISSUES/SEC-0005.md) (556,); [SEC-0017](ISSUES/SEC-0017.md) (77); [SEC-0021](ISSUES/SEC-0021.md) (52-60); [SEC-0022](ISSUES/SEC-0022.md) (241-242); [SEC-0023](ISSUES/SEC-0023.md) (2369); [SEC-0025](ISSUES/SEC-0025.md) (202-215); [SEC-0026](ISSUES/SEC-0026.md) (379-383); [SEC-0029](ISSUES/SEC-0029.md) (2487); [PRT-0003](ISSUES/PRT-0003.md) (2373); [HW-0001](ISSUES/HW-0001.md) (1791); [API-0001](ISSUES/API-0001.md); [API-0002](ISSUES/API-0002.md) (2253); [API-0003](ISSUES/API-0003.md) (307); [DB-0001](ISSUES/DB-0001.md) (229); [DB-0007](ISSUES/DB-0007.md) (229); [PERF-0002](ISSUES/PERF-0002.md) (2020-2023); [ARCH-0005](ISSUES/ARCH-0005.md) (307) | yes |
| `tests/Falah.Domain.Tests/BusinessClockTests.cs` | [FIN-0028](ISSUES/FIN-0028.md) | yes |
| `tests/Falah.Server.IntegrationTests/CashDrawerKickTests.cs` | [HW-0002](ISSUES/HW-0002.md) | yes |
| `tests/Falah.Server.IntegrationTests/ConfirmElectronicPaymentTests.cs` | [FIN-0001](ISSUES/FIN-0001.md) | yes |
| `tests/Falah.Server.IntegrationTests/ExpenseCategoryTests.cs` | [FIN-0041](ISSUES/FIN-0041.md) | yes |
| `tests/Falah.Server.IntegrationTests/FalahApiFactory.cs` | [DB-0008](ISSUES/DB-0008.md) (74) | yes |
| `tests/Falah.Server.IntegrationTests/OperatingDayScenarioTests.cs` | [FIN-0035](ISSUES/FIN-0035.md) (115-117); [OPS-0005](ISSUES/OPS-0005.md) | yes |
| `tests/Falah.Server.IntegrationTests/OrderVoidDispositionTests.cs` | [FIN-0036](ISSUES/FIN-0036.md) | yes |
| `tests/Falah.Server.IntegrationTests/PrintAgentSessionTests.cs` | [PRT-0006](ISSUES/PRT-0006.md) (19,34) | yes |
| `tests/Falah.Server.IntegrationTests/PrintJobClaimTests.cs` | [PRT-0003](ISSUES/PRT-0003.md) (53) | yes |
| `tests/Falah.Server.IntegrationTests/SplitOrderVoidTests.cs` | [FIN-0002](ISSUES/FIN-0002.md) (90); [FIN-0006](ISSUES/FIN-0006.md) (196,225) | yes |
| `tools/install-falah-service.ps1` | [PRT-0012](ISSUES/PRT-0012.md) (95-107) | yes |
| `tools/verify-windows-printer-transport.ps1` | [PRT-0007](ISSUES/PRT-0007.md) (61) | yes |

## B. Earlier audits and documents → canonical issues

The complete per-ID table is [02_HISTORICAL_ID_MAPPING.md](02_HISTORICAL_ID_MAPPING.md) (417 IDs, all mapped).

| Source | Location | IDs | Notes |
|---|---|---|---|
| Copilot/Codex register | `reports from another tool/GitHub Copilot Work/Final-Handoff/ALL_VERIFIED_FINDINGS.json` | 100 (+3 sub-report/summary items) | Audited baseline `c35816e`, which is not in this repository; 50 of the 215 hashed files changed since. See [DOC-0004](ISSUES/DOC-0004.md). |
| Prior-tool working files | `reports from another tool/` (Codex Deep-Audit, Copilot handoff notes) | 61 unregistered leads | Mapped by name and spot-checked; unverifiable ones are UNVERIFIED issues |
| Remediation backlog | `docs/REMEDIATION_PLAN.md` (backlog and Decisions tables) | 66 | Statuses re-verified against code |
| First audit | `AUDIT_REPORTS/` | 43 | — |
| Independent audit | `INDEPENDENT_AUDIT/` | 28 (findings and improvement opportunities) | — |
| Resilience audit | `PRODUCTION_RESILIENCE_AUDIT/` | 104 (PRA, UH, SC, CI) | 30 scenario IDs are "no issue (safeguard holds)" |
| This consolidation | `All Issues/` | 12 registry aliases for new findings | — |

## C. Test evidence referenced by issues

| Evidence | Date / state | Used by |
|---|---|---|
| Last full integration run (previous session log): 19 failed / 741 passed / 760 | 2026-11-04 03:51, built before several later edits | TEST-0001; EXISTING_TEST_EVIDENCE on fixed issues whose tests passed |
| Targeted runs recorded in `docs/REMEDIATION_PROGRESS.md` (SEC1 48/48, SEC2 13/13 + 137/137, Domain 411/411, Client 143/143) | Remediation session, before 2026-11-04 | SEC-0019, SEC-0020, SEC-0021, UI-0002 |
| Integration run 628/628 (R1/R2) | 2026-10-24 (plan record) | SEC-0015, SEC-0016 |
| Never-executed classes: `CorsTests`, `CashDrawerKickTests`, `OperatingDayScenarioTests`, `SplitOrderVoidTests` | Written 2026-11-04 | SEC-0025, HW-0002, OPS-0005, FIN-0002 |
| Read-only BCL time-zone check (Windows PowerShell 5.1): Cairo midnight invalid on 2025-04-25, 2026-04-24, 2027-04-30; `ConvertTimeToUtc` throws | 2026-11-04 | FIN-0028 |
| WCAG contrast computations from `src/Falah.Client.CashierWinUI/Styles/Colors.xaml` hex values | 2026-11-04 | UI-0004, UI-0019, UI-0020, UI-0021 |

**No CURRENT_TEST_VERIFIED, HARDWARE_VERIFIED or PRODUCTION_VERIFIED evidence exists for any issue.**

## D. Verification still required
Per issue: see section 14 ("Required regression tests") and section 18 ("Remaining uncertainty") of each issue
file. Consolidated: [06_TEST_AND_ACCEPTANCE_PLAN.md](06_TEST_AND_ACCEPTANCE_PLAN.md) and
[05_UNVERIFIED_RISKS.md](05_UNVERIFIED_RISKS.md).