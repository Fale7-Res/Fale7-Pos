# src/Falah.Domain/Printing/PrintRules.cs

**path**: src/Falah.Domain/Printing/PrintRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 1d728f9f30593703f473a603eba63945c3859cb8398457beea5b9521fd3d9dc9

**git_blob_sha256**: a12792b890d3264b90a1b3f72c8ad87bdd9c52a039dfc27c64ccc82e743e1db7

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 638]]

**functionality_and_reasoning**: Composes route-specific pure content: snapshot station filtering, cashier number/detail pair separated by cut, single callcenter detail, driver/COD pending instructions, reprint banner after every cut and operational slips. Currency amounts are supplied snapshots, no money posted. Cashier payment state uses explicit IsPaid with fallback while pending takes priority; delivery COD branch wins pending if inconsistent facts supplied, so caller correctness matters. Kitchen numeric enum parsing can accept numeric station string yet item filter requires canonical name, yielding empty station ticket for malformed stored station. Existing handlers constrain stations; no reachable defect promoted. Current brand/settings are supplied, so no independent guarantee historical reprint identical. Reprint empty ticket gets banner and trailing cut starts no banner until actual content.

**significant_methods**: DetermineKitchenRoutes; ComposeKitchenTicket; ComposeReceiptTicket; ComposeCustomerNumberSection; ComposeCashierDetailSection; ComposeBrandHeader/Footer; ComposeCashierTotals; ComposeDetailedOrderSection; ComposeDeliveryOrderTicket; ComposeDriverDispatchTicket; ComposeItemTable; ComposeTotals; ComposeCollectionInstruction; date/type helpers; MarkAsReprint; ReprintBanner; shift/expense slip methods; SlipHeading; DescribeDifference

**interactions_and_risks**: Composes route-specific pure content: snapshot station filtering, cashier number/detail pair separated by cut, single callcenter detail, driver/COD pending instructions, reprint banner after every cut and operational slips. Currency amounts are supplied snapshots, no money posted. Cashier payment state uses explicit IsPaid with fallback while pending takes priority; delivery COD branch wins pending if inconsistent facts supplied, so caller correctness matters. Kitchen numeric enum parsing can accept numeric station string yet item filter requires canonical name, yielding empty station ticket for malformed stored station. Existing handlers constrain stations; no reachable defect promoted. Current brand/settings are supplied, so no independent guarantee historical reprint identical. Reprint empty ticket gets banner and trailing cut starts no banner until actual content.

**findings**: ["FIN-004"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Complete638 lines read in bounded chunks after initial truncated batch was reread; no rendering/printing executed.

**recommendations**: Test valid route guard, partial physical pair uncertainty, mixed pending collection, refund-era historical shift slip conservation and locale/long text visual output. Avoid claiming full source snapshots from pure composer alone.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
