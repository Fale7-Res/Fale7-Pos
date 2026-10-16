# src/Falah.Domain/Inventory/InventoryRules.cs

**path**: src/Falah.Domain/Inventory/InventoryRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 6c445ff5fdfdb08495188e70b13ba03b18fcfb850921dca22838695c7ddebb67

**git_blob_sha256**: 6c445ff5fdfdb08495188e70b13ba03b18fcfb850921dca22838695c7ddebb67

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 109]]

**functionality_and_reasoning**: Full109 lines read. Loss intentionally may exceed recorded stock, count adjustment human delta. Unit lock shape caller dependent. Stock totals signed by direction; type summaries assume type implies direction. Variance sales*current recipe minus recorded usage+waste is neutral signal. Shared day avg rounds integer and null for zero days; overflow possible unchecked intermediate.

**significant_methods**: AssertCountAdjustmentInput; AssertWasteInput; AssertUnitChangeAllowed; StockBalanceMilliUnits; ActualMovementTotals; UnexplainedVariance; SharedConsumptionReport

**interactions_and_risks**: Full109 lines read. Loss intentionally may exceed recorded stock, count adjustment human delta. Unit lock shape caller dependent. Stock totals signed by direction; type summaries assume type implies direction. Variance sales*current recipe minus recorded usage+waste is neutral signal. Shared day avg rounds integer and null for zero days; overflow possible unchecked intermediate.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: No physical count or stock writes; tests pending review.

**recommendations**: Validate host enum/direction consistency, effective date range units and unit-change concurrency; preserve intentional negative-stock discrepancy recording.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
