# src/Falah.Client.CashierWinUI/Views/ReportsView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c651254a3cf46fde7d9b8337061f87603c0d54e06dc2e7d20c3392bd94e0e99a`; worktree SHA256 `2d8f72800401c26c5b183a8bc308881f5e757abb550a5eaa9504266b719cd852`; LF-equivalent `True`.

Actual role: Period report presentation for sales, collection, products/hourly, delivery, tax, cost, refunds and actions.

## Function/resource review

- `IsSpecificDay` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:34`: Period combobox named for accessibility; specific-day stepper only visible chosen-day, previous/next names explicit. Datepicker bridge existsVM but no datepicker in this markup, so arbitrarypastday requires repeated stepping.

- `HasData` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:59`: Contenthiddenwhile loading/error byVMHasData, preventing visible partial sequential report. HeaderRangeLabel updates while reload; displaydatawaits coherent finalsuccess. No reportprint/exportbutton.

- `Sales.CollectedSalesPiastres` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:78`: Net and actuallycollected headline distinct; pendingelectronic and drivercash banners expresslyexcludeduntilconfirmation/settlement. Fourchannel totals do not themselves fullyexplaincollecteddriver settlementcontribution here, unlikeowner.

- `Sales.OffersValuePiastres` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:158`: Ordertypes andoffers/comps/manualdiscount separate fields, avoidsmerging differentdiscountsources. Large monetarytitles fixedfourcolumns mayneedwrap.

- `TopItems` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:181`: Top-selling table and emptyperiodtext, no topNcount disclosed. CategorySalesloadedbyVM but noCategorySalesbinding exists here, so serverreadcost has novisualoutput onthisview.

- `ProductHours` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:208`: Horizontal-only internal scroll handles24hourgrid,200name+70total+70peak+40perhour. Peak markedcoloronly, though PeakLabel textualcolumn gives sameinformation. VMcaps15rowsbutviewdoesnotstatecap, so headingmayappearcomplete.

- `Delivery.FailedCount` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:292`: Label combines failed/returned but bindingonlyFailedCount; whetherDTOalreadycombines mustverifyquerycontract, notassumemissingreturned. UnsettledcustodyExpected maybecurrentposition versusperiodoutcomes;needsdatebasis.

- `FoodCostBasisNote` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:341`: Explicitcurrentrecipe/currentingredientpricebasis and knownlinecoverage; incompletemissingcostwarnsvalueunderstatestruecost. Thisisstrongerhistoricaldisclosurethanowner. Taxbase/gross/refundimpact/netallseparate.

- `SignificantActions` at `src/Falah.Client.CashierWinUI/Views/ReportsView.xaml:396`: Refundlist includesapprover/reason, noteworthyactionswholeDTOcategoryconverter+actor/nullableamount/reason/approver. No timestampcolumns and eagerrowrealization. No actualrefund/physicalprintperformed.

## Tests and limitations

NOT_EXECUTED. Need missingcategoryoutput,15hourlycapdisclosure, deliveryfailedreturnedsemantics, arbitrarydateaccess, datasetcompleteness/largeamounts and nativehorizontalkeyboardscroll. No callabledeclarations.

## Recommendations (not implemented)

Disclose cappedsections and datebasis, expose arbitrarydayselection and onlyfetchrenderedreportsections. Preserveunknowncost/currentbasis warnings.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
