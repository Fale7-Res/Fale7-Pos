# src/Falah.Domain/Inventory/InventoryRecords.cs

**path**: src/Falah.Domain/Inventory/InventoryRecords.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 52d7a39781b9d8a87828ed3d38193ecc8fc87b0a140e666c1df64a8ac66918af

**git_blob_sha256**: 78e0ae8fe989dc1fb2722642532fe3af296ef4f38208dc16432c87a424bb02c3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 34]]

**functionality_and_reasoning**: Full34 lines read. Facts defaults quantity0/out retain costing-only constructors but accidental reuse in balance gives missing quantities silently. Records no positivity or enum invariants; caller responsible. One-active producer/packaging rule invariant not encoded. Estimated cost integer nullable correctly distinguishes unknown.

**significant_methods**: CostSource; InventoryItem; RecipeLineKind; RecipeLine; PrepRecipe; StockMovementType; StockDirection; StockMovementFacts; OrderPackagingRule

**interactions_and_risks**: Full34 lines read. Facts defaults quantity0/out retain costing-only constructors but accidental reuse in balance gives missing quantities silently. Records no positivity or enum invariants; caller responsible. One-active producer/packaging rule invariant not encoded. Estimated cost integer nullable correctly distinguishes unknown.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared CostingRules and deduction consumers; no writes.

**recommendations**: Keep construction sites explicit for balance facts and validate domain records at command boundaries.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
