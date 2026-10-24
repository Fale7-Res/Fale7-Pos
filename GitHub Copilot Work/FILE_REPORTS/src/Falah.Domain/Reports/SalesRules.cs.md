# src/Falah.Domain/Reports/SalesRules.cs

**path**: src/Falah.Domain/Reports/SalesRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: eb5ff2f1a5e957ff106386e34849d11a8b6a2e7f01fb864c3988a00f77adc4e9

**git_blob_sha256**: 7a52c3e29e9185471415dadfd81a28ee329811c653974a31a314f9baed147737

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 111]]

**functionality_and_reasoning**: Full111 lines read. PendingElectronicSales subtracts entire orderTotal for pending split despite immediate cash/card leg; collected0 for200 split100+100 although treasury cash100. Distinct FIN-010. COD whole net deducted until settled custody, but any settled custody including failed zero-collected removes outside set so failed COD may appear collected unless outcome filter elsewhere. Payment channels exclude COD forever due IsPaidfalse; headline cash differs channel sum. RefundTaxImpact proportional total includes delivery/comp so estimate only, original periods mutate with later refunds.

**significant_methods**: ComputeSalesReport; local OrderTypeNet; SumMoney; ComputeTaxReport

**interactions_and_risks**: Full111 lines read. PendingElectronicSales subtracts entire orderTotal for pending split despite immediate cash/card leg; collected0 for200 split100+100 although treasury cash100. Distinct FIN-010. COD whole net deducted until settled custody, but any settled custody including failed zero-collected removes outside set so failed COD may appear collected unless outcome filter elsewhere. Payment channels exclude COD forever due IsPaidfalse; headline cash differs channel sum. RefundTaxImpact proportional total includes delivery/comp so estimate only, original periods mutate with later refunds.

**findings**: ["FIN-010"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Static numeric mixed case and compared payment totals. No runtime financial writes.

**recommendations**: Subtract actual pending leg, compute settled cash from provenance/outcome and reconcile channel sums with headline; test failed/returned COD after zero settlement, late refunds and mixed pending.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
