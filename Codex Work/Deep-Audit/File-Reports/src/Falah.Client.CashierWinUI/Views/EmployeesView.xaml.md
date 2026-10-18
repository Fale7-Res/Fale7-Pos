# src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `17bf5f2d9ca55948e1ef9b4d8c80f1910d23b0eea65316dcee5144065e7b04b2`; worktree SHA256 `049e44a2eab52ee2cb47c91f052debbd00ff3e5c8b704c1a8d242f75926ee8ee`; LF-equivalent `True`.

Actual role: Employee balances, accrual/cash actions, ledger, financial report and standalone payment.

## Function/resource review

- `Employees` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:1`: 150job/130balance/272actions plus name star; name horizontal inactivebadge risks unbounded wrap. Three rows of compact36px financial actions include wage/overtime/extrashift/advance/withdrawal/pay; VM must authorize each, no per-row visibility gates.

- `OpenLedgerCommand` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:92`: Ledger720max700 fullspan overlay shows description/type, debit/credit/running balance, distinct loading/error/retry/empty. No timestamps or actor shown per ledger entry, limiting traceability despite full running money trail.

- `IsCashOutOpen` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:211`: Shared accrual/cash modal440max640 centeredscroll; advance/withdrawal open-shift alert, wage editable amount, overtime1..5 preview and extrashiftreadonlyvalue. Commands differentiated despite shared display; descriptions are not proof exact server payroll calculation.

- `CanEditAction` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:265`: Amount/reason/managerPIN locked, overtimebuttons rely commandguard. Static savecaption not explicit retained retry. Close guards/globalpending/context restoration belongVM; same-process tests do not certify restart.

- `IsReportOpen` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:292`: 1000wide max720 financial report, custom DatePickers,6fixed moneycolumns plus name. Explicitly separates advances from payments and current balance from period subtotal. Preserves semantic distinction, but window narrower than1048DIP has fixedwidth overflow risk.

- `IsPayOpen` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:380`: Standalone pay explicitly restaurantaccount, partial/full current entitlement, no payrollrun requirement. Nothingdue hides editable action and explains state; CanEditAction locks account/amount/reason/PIN. No employee payment executed.

- `EmployeeEditorView` at `src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml:445`: SeparateEditorDataContext declaredlast, overlays any existing actionmodal; VMmutualexclusion/focusrequired. No background session or customer data inspected. White-onTextPrimarytoast darkcontrast issue repeats.

## Tests and limitations

NOT_EXECUTED. Need wage accrual versus cash debit correctness, period/current balance distinction, restartpending actions, active/inactive switch, longnames,1000wide report and darktoast. No callable declarations.

## Recommendations (not implemented)

Preserve distinct financial routes, add ledger timestamp/actor evidence and adaptive widths. Explicitly reconcile durable request context instead of relying only on disabled fields.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
