# src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `e9771424cbe111d081d1b32b1e665700546d31fdd97456617b1d956b99f69b51`; worktree SHA256 `a4719ca4c0b5f6ddd089aac9cf790f31fdd850c06506ab521a0d53461ccc2cbe`; LF-equivalent `True`.

Actual role: Owner selected-day statement composed of11 collapsible real-data sections.

## Function/resource review

- `SelectedDateDisplay` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:34`: Yesterday/today/previous/next/refresh commands with next boundCanGoNext. Glyph-only day/refresh buttons lack explicit accessible names; large horizontal toolbar can crowd title.

- `Statement.Sales.NetSalesPiastres` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:58`: Gross/refund/net, ordercount, channels, ordertypes and average distinct. Pending electronic, drivercash, manualdiscount, comps and nettax explicitly separate. Five unwrapped horizontal footnotes may overflow narrower than1100max sheet.

- `SoldProducts` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:133`: Full sold-product categoryfilter/table includes quantity/ordercount/total/peak, no truncationnote/topN promise. Grouping/data completeness belongs server/VM; product/category labels unwrapped in star columns, risk large names clipping.

- `ProductHourly` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:158`: Per-product total/peak/hour text separate, compact scrolling outer sheet; eager ItemsControls may instantiate many product rows when expanded. No chart mock data.

- `ExpenseLines` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:178`: Real expense category/description/amount/entryactor/approvalactor and directCreatedAtUtc binding rather than localtime converter used in treasury. Timestamp presentation inconsistent. Expense reprint real command with automationID/name; no preview or physical print executed.

- `Statement.ShiftTotals.ShortagePiastres` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:217`: Shortage and overage shown separately, preserving offsetting differences; per-shift rows remain distinct. Several monetary columns lack column header labels in detail rows, harder to interpret screen-reader/order context.

- `Statement.TreasuryBalancesNow` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:247`: Main label explicitlynow while other accounts adjacent samecurrentDTO; historical statement contains live balances, not frozen close snapshot. Treasury movementtime uses localconverter and showsactor/reason.

- `Employees` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:278`: Attendance times/late, wage/overtime accrued, total paid today and runningbalance displayed. Earnings-versus-payment distinct; seven unwrapped equal columns need layout check and row-level headers/accessibility.

- `Statement.Inventory.SaleConsumptionMilliUnits` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:307`: Consumption/loss/count quantities displayed raw integer with unit/1000label, unlike MilliUnitsToDisplay elsewhere. Aggregating heterogeneous inventory units can mislead without backend semantics; not treated as monetary loss. Known cost coverage not stated for loss sum.

- `Statement.Delivery.UnsettledCustodyExpectedPiastres` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:334`: Delivery counts, fees/time/settlementdifferences plus currentcustody explicitlynow, mixing daily events with live positions deliberately labeled. Physicaldelivery stateproof belongs server.

- `AuditTrail` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:363`: Refund authorizedactor+reason and combined audit/controltrail source, no timestamp column shown for auditrows. Header claims all events today; actual caps/query completeness need serverchallenge.

- `Statement.Backup.LastVerifiedBackupAtUtc` at `src/Falah.Client.CashierWinUI/Views/DailyStatementView.xaml:391`: System printing/backup cards are statuscounts/timestamps only; no realpaperhealth proof. Many rawnullable fields renderblank without unknown label. Source commenttheme-safe not accepted as visualtest.

## Tests and limitations

NOT_EXECUTED. No reprints or live data read. Need current-versus-selected-day disclosure, complete-list caps, rawmilliquantities, unknowncostcoverage, longlabels/footnotes and accessible columns. No callable declarations.

## Recommendations (not implemented)

Keep11section boundaries but add snapshot-time and units/context, coherent timestamp formatting and responsive wrapping. Rendered usability/contrast remains unverified.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
