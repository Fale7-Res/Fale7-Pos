# src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs

**path**: src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 40347a2c68fc1f09c20e588573331a893f40e8165b2bddd0b4e29dee20d638ab

**git_blob_sha256**: 40347a2c68fc1f09c20e588573331a893f40e8165b2bddd0b4e29dee20d638ab

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 172]]

**functionality_and_reasoning**: Full172 lines read. Idempotent stock+audit saved atomically, waste last actual purchase cost frozen. Count direction Enum.TryParse permits undefined numeric enum string such as99; stored99 later Enum.Parse gives unnamed value and balance treats anything not In as Out. Waste loss-type whitelist prevents same. No unit-change shared lock. Large waste critical as intended.

**significant_methods**: RecordCountAdjustmentHandler constructor/HandleAsync; RecordWasteHandler constructor/HandleAsync

**interactions_and_risks**: Full172 lines read. Idempotent stock+audit saved atomically, waste last actual purchase cost frozen. Count direction Enum.TryParse permits undefined numeric enum string such as99; stored99 later Enum.Parse gives unnamed value and balance treats anything not In as Out. Waste loss-type whitelist prevents same. No unit-change shared lock. Large waste critical as intended.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared InventoryRules and unit metadata writer. No actual stock writes.

**recommendations**: Require Enum.IsDefined for directions and canonical whitelist; serialize first stock movement vs unit mutation; test replay and invalid enums without fixture side effects.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
