# src/Falah.Client.CashierWinUI/Views/WasteView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `d604a1c80f258d4f4dfbf22ad718153c1cbf95eb5101f8c186f1fa8f91632d1f`; worktree SHA256 `c63ecabc62fcae31e0a6d100538c5f41aa4352f7712a9de287ed8f82f1170d3a`; LF-equivalent `True`.

Actual role: Read-only dated waste/loss summary and movement list.

## Function/resource review

- `BusinessDateLabel` at `src/Falah.Client.CashierWinUI/Views/WasteView.xaml:31`: Prev/nextbuttons have Arabicaccessible names andalways-enabled markup; VMmustqueue/coalesce dates. Summarytitle explicitlyonebusinessdate.

- `Summary.TotalLossValuePiastres` at `src/Falah.Client.CashierWinUI/Views/WasteView.xaml:51`: Summary metric alwaysvisible regardlessIsLoading/error; stale/nullsummariesmayread olddate with newheading. ByTypehorizontalmin140percategory plus170total needs substantialwidth; no horizontalwrapping.

- `Movements` at `src/Falah.Client.CashierWinUI/Views/WasteView.xaml:95`: Rows showtype/quantity/value/reason but noitemname, timestamp orbusinessdate. Therefore VMglobalrecentmovementdate mismatch cannot be detectedfromtable itself; priorWasteVMsource candidate requires endpoint/datechallenge.

- `NullablePiastresToDisplayConverter` at `src/Falah.Client.CashierWinUI/Views/WasteView.xaml:112`: Individualnullcost explicitlyunvaluednot0. SummarytotalusesnonnullablePiastresconverter andcannotaloneconvey unknownlosscoverage; backendDTOsemanticsmustbechecked.

- `HasNoLoss` at `src/Falah.Client.CashierWinUI/Views/WasteView.xaml:136`: Empty statesays no lossinthisday, whiletableunconditionallyboundMovements. Differentdate-filtersemanticscanyieldcontradictoryemptybanner/list; nativecasesnotobserved.

- `LoadCommand` at `src/Falah.Client.CashierWinUI/Views/WasteView.xaml:132`: Spinner/error/retryoverlay tableonly, nottopsummaries; fixed140/120/130columns plusreasonstar potentialnarrowviewportpressure. EagerItemsControlnotvirtualized.

## Tests and limitations

NOT_EXECUTED. Needrapiddatechanges, serverrecentlistversusdatedsummary, unknowncostcoverage andlongreason/narrowlayout. No callabledeclarations.

## Recommendations (not implemented)

Exposemovementitem/timeanddate-consistentloadingdata; neverclaimdatedsummaryandglobalrecentrowsreconcilewithoutqueryevidence.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
