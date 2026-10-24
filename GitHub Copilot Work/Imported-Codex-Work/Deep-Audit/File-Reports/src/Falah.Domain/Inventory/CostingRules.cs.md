# src/Falah.Domain/Inventory/CostingRules.cs

**path**: src/Falah.Domain/Inventory/CostingRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: c1624846b505dd4e13798efa8cbf39129c3b711500c5da064e30c3dace0278ca

**git_blob_sha256**: fe2a9a59ade5ff4c0635e912063c7b865c1630f92f4dd4f6ecde5dfd2c8746b4

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 205]]

**functionality_and_reasoning**: Full205 lines read. Unknown cost preserved null and completeness controls profitability. Resolving stack guards prep cycles; BFS flags route to self. Prep uses first active recipe for output with no uniqueness, ambiguous concurrent alternatives. Yield quantity uses double rounding; large ratios can round a real small usage to0. Empty recipe incomplete. Real positive purchase overrides estimate; Prep ignores purchase/estimate. Cost source depends current recipes not sale snapshot.

**significant_methods**: IngredientUnitCost overloads; IsCircularPrep; ComputePrepRecipeCost overloads; BaseQuantityMilliUnits; RecipeCostResult.IsComplete; ComputeRecipeCost; OrderPackagingCost; MenuItemProfitability

**interactions_and_risks**: Full205 lines read. Unknown cost preserved null and completeness controls profitability. Resolving stack guards prep cycles; BFS flags route to self. Prep uses first active recipe for output with no uniqueness, ambiguous concurrent alternatives. Yield quantity uses double rounding; large ratios can round a real small usage to0. Empty recipe incomplete. Real positive purchase overrides estimate; Prep ignores purchase/estimate. Cost source depends current recipes not sale snapshot.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Existing domain tests not yet read for all costing cases; actual quantity formula matches stock deduction helper.

**recommendations**: Specify yield precision and units, enforce one active prep producer or deterministic selection; retain unknown semantics and add boundary/rounding/cycle graph tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
