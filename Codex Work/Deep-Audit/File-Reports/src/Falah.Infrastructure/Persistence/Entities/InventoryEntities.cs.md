# src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 934d1252ffc1fc0e0d899bdc7e91f633f1f44ab8ccc8fecc4808753692d2be34

**git_blob_sha256**: c88b68327a1dedffb8e51d694d288b8f08cd755ec2c4f0c829f29052d94ccf99

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 111]]

**functionality_and_reasoning**: Full111 lines read. Raw Kind/CostSource/Direction strings accept bad metadata until parsed. Stock UnitCost comment loss-only contradicts purchase receipt actually sets it; loss snapshot cost correct. No per-movement unit snapshot makes lock-before-first-stock essential. Prep records costing recipes not batch-production transactions; no inventory conversion movements. Multiple active output/type rules no uniqueness.

**significant_methods**: RecipeEntity; RecipeItemEntity; InventoryItemEntity; StockMovementEntity; PrepRecipeEntity; PrepRecipeLineEntity; OrderPackagingRuleEntity properties

**interactions_and_risks**: Full111 lines read. Raw Kind/CostSource/Direction strings accept bad metadata until parsed. Stock UnitCost comment loss-only contradicts purchase receipt actually sets it; loss snapshot cost correct. No per-movement unit snapshot makes lock-before-first-stock essential. Prep records costing recipes not batch-production transactions; no inventory conversion movements. Multiple active output/type rules no uniqueness.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared full costing and inventory save/deduction paths.

**recommendations**: Enforce enum/quantity and single-active source constraints; document costing-only prep capability rather than actual production stock conversion.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
