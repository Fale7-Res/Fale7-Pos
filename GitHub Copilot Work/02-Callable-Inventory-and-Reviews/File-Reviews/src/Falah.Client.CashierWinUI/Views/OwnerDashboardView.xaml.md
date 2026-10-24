# src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c7626ca4d4c66233519de67702c4835e8e59dee9754e6c347cf728cf068edeba`; worktree SHA256 `b1e10c421627332741e9a70078b354f25e02df46ce74180bd1902007641415c9`; LF-equivalent `True`.

Actual role: Owner operational dashboard with attention, KPIs, trend, cash/cost/obligations/people/health.

## Function/resource review

- `VisibleAttention` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:62`: First attention card displays severityword and color, root navigationcommand, disabled ifnoNavKey. Toggle exposes collapsedrest; priorVMdoesnotseveritysort latecriticalrows despitecomment, so nativeviewconfirmsonlyVisibleAttention shown.

- `Metrics` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:108`: 252x132 wrapping KPIcards contain22pxamount, comparison,22pxsparkline and2linecaption. Fixedheight with wrappingamount/caption may clip attextscale. Net/collectedsparklinepoints provenance documentedVM; no fabricatedchartdata.

- `SeriesPoints` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:162`: 120pxtrend with clear zero-filled note. Doesnotannotate axes/timestamps in thismarkup, so trendshape cannot independently identify exactpeak time withoutotherreports.

- `FractionToWidthConverter` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:187`: Split barinner width uses converterfixed150pxscale inside startrack next72label+46percent in one ofthreepagecolumns. Actualtrackcanbeless150, allowingbaroverflow; nativeboundsnotmeasured. Emptytotalem dash controlledVM.

- `DriverSettlementNote` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:199`: Explains collectedincludesdriver settlements, while Split actuallyVMpickup/takeaway/delivery shares. Currentcash explicitlynow versusselectedperiod. Pendingelectronic adjacent currenttreasury butsourceperiodsemantics must be clear.

- `MoneyOrUnknownConverter` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:257`: Profitability preserves unknown foodcost/grossprofit/result with warning and excludedrent/depreciationnote. Thisscreen lacks Reports currentrecipe/currentpricebasisnote, so historicalperiodcostbasis less explicit.

- `Data.Leakage` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:295`: Offers/comps/manualdiscount/refunds/waste separate, no unintendednettinginview. Obligationsshowcurrentowedcounts alongsideperiodpurchases; useDatecontext labels carefully.

- `PrintRoutes` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:386`: PrinthealthusesallDTO routes withlastlogicalsuccess timestamp, not measuredphysicalpaper output. BackupdisplayformattedVM. Lowstock countheader canexceedsixshownrows becauseVMcaps6, no explicitmore/link here.

- `HasData` at `src/Falah.Client.CashierWinUI/Views/OwnerDashboardView.xaml:44`: Rootcontenthiddenwhileloading/error, avoiding visiblymixed old/newKPI snapshot. Refresh/period have accessible names, goodcontrastwiring sourceonly; no nativeobservations.

## Tests and limitations

NOT_EXECUTED. Need attentioncriticalsort/collapse, narrowpercenttrackbounds, KPItextscale, historicalcostbasis, current-versus-periodpositions and>6lowstock disclosure. No callable declarations.

## Recommendations (not implemented)

Use relative barwidth based actualtrack, explicitlyrankattention and disclose cappedlowstock/currentcostbasis. Preserve unknown values and no physicalhealthclaim.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
