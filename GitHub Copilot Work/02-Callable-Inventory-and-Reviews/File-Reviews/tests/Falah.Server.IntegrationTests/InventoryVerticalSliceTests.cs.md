# tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 28e718db495a265ada6dff446c2bc26527ff838f2d4cd6e72681cc8d6325f9c2

**git_blob_sha256**: c7b1c820977f71d5a1ea880c20898cd9913b4315317b2fb04344266d1229e67d

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 827]]

**functionality_and_reasoning**: All 827 lines read in four bounded segments and 79 exact callable records saved. Good tests cover explicit negative-stock waste policy, latestdated cost arithmetic, one-key concurrent waste, unit immutability after movements, direct/two-node cycles and shared-leaf noncycle, legacy circular prep readback classified Circular, unknown costs null and incomplete margins withheld. Direct stock/prep seed lambdas bypass production purchasing/recipes intentionally. Update-no-history title has empty history and no historical comparison; waste-freeze asserts current response only without later reprice. Shared consumption expects consumed10000/standard4000 but only nonnull average/variance, so wrong numbers pass; comment incorrectly calls3333 above4000. Small waste only-warning title checks presence without absencecritical. Costcoverage seeds only complete class. Packaging only checks item/type, not1000quantity. Replay PK count cannot exclude logical duplicate item IDs; concurrency no barrier. No business bug inferred from intended negative inventory.

**significant_methods**: Inventory CRUD/units/balance/count/waste; purchased/estimated/prep costing; recipe graph guards and legacy-cycle read defence; overview/detail/profitability/history; report permission/audit/shared usage facts

**interactions_and_risks**: All 827 lines read in four bounded segments and 79 exact callable records saved. Good tests cover explicit negative-stock waste policy, latestdated cost arithmetic, one-key concurrent waste, unit immutability after movements, direct/two-node cycles and shared-leaf noncycle, legacy circular prep readback classified Circular, unknown costs null and incomplete margins withheld. Direct stock/prep seed lambdas bypass production purchasing/recipes intentionally. Update-no-history title has empty history and no historical comparison; waste-freeze asserts current response only without later reprice. Shared consumption expects consumed10000/standard4000 but only nonnull average/variance, so wrong numbers pass; comment incorrectly calls3333 above4000. Small waste only-warning title checks presence without absencecritical. Costcoverage seeds only complete class. Packaging only checks item/type, not1000quantity. Replay PK count cannot exclude logical duplicate item IDs; concurrency no barrier. No business bug inferred from intended negative inventory.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 79 individual symbol receipts, source only; no fixture/database/test execution. StockOut arithmetic in detailed overview distinct from OwnerDashboard FIN013.

**recommendations**: Strengthen priced/history freeze and exact average/variance assertions; signed movements/range/tie/rounding/overflow tests, concurrent graph saves, no partial writes on rejection, exact audit action and all costcoverageclasses, packaging quantities with real checkout deduction.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
