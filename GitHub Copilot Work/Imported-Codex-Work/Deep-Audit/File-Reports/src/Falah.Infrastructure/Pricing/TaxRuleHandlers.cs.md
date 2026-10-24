# src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs

**path**: src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: f40a0f0879cbc2789f11cd421dd850213e3a81122819011cc73fe8643dccd4b0

**git_blob_sha256**: d1cffe73ec801ac1ee94b6f30d0ed0709fd3fecc87fe2a486b907f7bdf5c91d4

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 118]]

**functionality_and_reasoning**: Full118 lines read. No active-rule uniqueness or deactivating other Enabled rows even sequential, checkout FirstOrDefault picks arbitrary enabled. Mode Enum.TryParse accepts numeric unknown; stored raw numeric treated Exclusive by checkout ternary rather than canonical; no max rate. Nonnegative rate only. Snapshot tax existing orders preserved. Update receipt dedup atomic.

**significant_methods**: CreateTaxRuleHandler constructor/HandleAsync; UpdateTaxRuleHandler constructor/HandleAsync; TaxRuleQueryService constructor/ListAsync

**interactions_and_risks**: Full118 lines read. No active-rule uniqueness or deactivating other Enabled rows even sequential, checkout FirstOrDefault picks arbitrary enabled. Mode Enum.TryParse accepts numeric unknown; stored raw numeric treated Exclusive by checkout ternary rather than canonical; no max rate. Nonnegative rate only. Snapshot tax existing orders preserved. Update receipt dedup atomic.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared checkout enabled rule read and TaxEngine. No taxes changed.

**recommendations**: Define one active rule or ordered composition; canonical mode whitelist, reasonable configured bounds explicit owner decision; add two-enabled deterministic error rather than silent selection.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
