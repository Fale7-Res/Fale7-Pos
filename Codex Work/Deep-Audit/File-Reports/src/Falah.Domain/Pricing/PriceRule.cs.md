# src/Falah.Domain/Pricing/PriceRule.cs

**path**: src/Falah.Domain/Pricing/PriceRule.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 96af5115cb582e129e04140cd81ca97555e80a548078202f69693e5348cac826

**git_blob_sha256**: 96af5115cb582e129e04140cd81ca97555e80a548078202f69693e5348cac826

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 15]]

**functionality_and_reasoning**: Full15 lines read. Fixed-price per product day/time rule optional partial window. Record carries Active and list references no validation. Overlap legitimate cashier selection. Price rule affects every variant equally (per product, not variant), business policy should be explicit.

**significant_methods**: PriceRule record

**interactions_and_risks**: Full15 lines read. Fixed-price per product day/time rule optional partial window. Record carries Active and list references no validation. Overlap legitimate cashier selection. Price rule affects every variant equally (per product, not variant), business policy should be explicit.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared ItemPricingRules and PricingRules.

**recommendations**: Document variant pricing semantics and freeze snapshots in order as current code does.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
