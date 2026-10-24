# src/Falah.Client.CashierWinUI/Services/ApiClient.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `7ddeba1db61b6048594f1321cfe999ae74a55a4ac9a420794f6974a0dd724f5c`; worktree SHA256 `831a6dfd1f18510b240a232b7a4c4fd11251a51746e1e416c7ce691f732ddbf4`; LF-equivalent `True`.

Actual role: Typed HTTP boundary for all WinUI reads, commands and mutable bearer session.

## Function/resource review

- `ReadResultAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:58`: Core helpers/constructors/login/logout exact11 spans reasoned in Knowledge shard. Full file source read; endpoint wrappers below manually traced as families, remaining individual callable receipts pending.

- `GetOrdersAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:231`: Orders/lookup and availability route IDs escaped; actorIDquery is clientassertion, servermustderive/checksession. Product availability/permission writes use SendEmpty, settings/backup restore elsewherepartials requiretypedbody.

- `GetCurrentBusinessDayAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:261`: Currentday404 legitimate no-open state callerhandled; day/history/checklist/close/reopen/exceptions/snapshots separate endpoints. NoAPIhelperautomaticallyopensdayorbacksup.

- `GetAllUsersAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:341`: Usercreate/PIN/active/permissions methods transporttypedrequests; permissioncatalog GET. NoHTTPclient-sideprivilegecheck canreplace serverauth. Auditlimit200 default, escapedcategory/severity.

- `GetEmployeeFinancialReportAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:381`: Employee ledger/cashout/dailywage/overtime/extrashift/pay separatewrites. Employee pay isrestaurantaccount versusdrawer cashout, notinterchangeable. Attendanceoptionaldate/employee, payrollcreate/transition/pay/readroutes distinct.

- `GetRecipeCostDetailAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:460`: Costsummary/coverage/detail aggregate endpoints avoidper-recipeN+1; protectedactor query. SaveRecipefullDTO. LossSummarydated butGetStockMovements hasonlyitem+limit, nodatefilter, corroboratingWasteVMdatedlistmismatchcandidate.

- `SetPurchaseDueDateAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:496`: Supplier/receipt/payment/due-datewrite methods distinct; due-datePUT andagingGET. IDsescaped, fixedroutes; retries/pendingDTO responsibilityVM.

- `GetTreasuryMovementsAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:510`: Treasury actor/account/limit query default100;deposit/withdraw/transferwriteDTO notpersistedbyHTTPhelper. Inventoryoverview/detail/count/wasteandcreate/update wrappers preserveintegercontract, movementsdefault50nodate.

- `GetSalesReportAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:566`: Optionalshift omittedifnull/empty ratherthanpresentempty; dateformathelperyyyy-MM-dd butnoexplicitInvariantCulture despitecommentnevercultureformatted. Actualcalendarsettingscanmatter, notverifiedfault. Reportfamilies useconsistentactor/from/to, serverqueries independentlyreviewed.

- `GetDriverStatisticsAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:656`: DateTimeOffsetroundtripstrings escaped preservingplusoffset. Drivercreate/active/board/ready/assign/reassign/result/settle/account/timeline remaintyped; thisclientcontainsReassignAPI evenifcurrentdispatchviewnoaction.

- `CreatePrintJobsAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:711`: Approvals,expense,safedrop,cashmovement,draweropen/balance,orderprintjobs/documentreprint realendpoints. Theyqueue/logactions, notfullreceiptpreview. NoAPIcallperformedbyreviewer.

## Tests and limitations

SOURCE_REVIEW_ONLY. Exact11 core callable receipts saved; hundreds of wrapper/lambda identities remain NOT_ANALYZED in persistent symbol coverage despite full file-family review. No HTTP/DB/print calls executed.

## Recommendations (not implemented)

Isolate authentication proof from mutable session; validate successful token shape and coordinate exact persistent request identity. Add contract tests per route and cancellation/disposal/session races.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
