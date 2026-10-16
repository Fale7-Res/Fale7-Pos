# src/Falah.Infrastructure/Persistence/Entities/PricingEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/PricingEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: cef7ef0c0d3daa6a59754bac354c738c0cf108739bc4c4ac05bd3ce0cae579bf

**git_blob_sha256**: cef7ef0c0d3daa6a59754bac354c738c0cf108739bc4c4ac05bd3ce0cae579bf

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 40]]

**functionality_and_reasoning**: Full40 lines read. Join tables good referential product/rule sets, no Active tax singleton unique index, multiple Enabled possible concurrent. Days unique composite but no weekday bounds DB. Rule fixed price and tax basis raw values validated authoring only. TimeOnly overnight semantics domain-owned.

**significant_methods**: PriceRuleEntity; PriceRuleProductEntity; PriceRuleDayEntity; TaxRuleEntity properties

**interactions_and_risks**: Full40 lines read. Join tables good referential product/rule sets, no Active tax singleton unique index, multiple Enabled possible concurrent. Days unique composite but no weekday bounds DB. Rule fixed price and tax basis raw values validated authoring only. TimeOnly overnight semantics domain-owned.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared PricingRules and context; tax/save handlers pending review.

**recommendations**: Transactional one-active-tax invariant, whitelist modes/days/rates; regression concurrent activation and stale rule IDs.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
