# src/Falah.Infrastructure/Inventory/InventoryQueryService.cs

**path**: src/Falah.Infrastructure/Inventory/InventoryQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 3ff16ec5eb50957c11ad1ba39ed5e45cd8f29b972674d49b4e3448e0e3aee6de

**git_blob_sha256**: 8c469abe1b385805570575d5d0cd96058622a4b2b91cc37ca4d45ef5b6ea1247

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 508]]

**functionality_and_reasoning**: Full508 lines read in2 bounded chunks. Many calls reload all inventory/preps/purchasehistory and perline linear lookups; current-cost queries grow lifetime. Loss summary null LossValue=>0 without unknown coverage, contradict unknownnotzero pledge. Variance current recipe/deduction vs cancelled-order filter intentionally uses current recipe but cancelled usage remains actual, neutral signal. Shareddays rounds elapsed duration not inclusive businessdates. Recipe summary uses recipe.ProductId baseprice even if no product backlink, while detail resolves backlink: same recipe margin may differ. Coverage productsWithoutRecipe ignores variant-only recipe and inactivecategory; uncosted ingredient counts inactive. Detail movementlist ignores requested from/to while totals scoped. Enum.Parse bad authoring metadata crashes whole costly reads.

**significant_methods**: Constructor; ListInventoryItemsAsync; GetStockBalanceAsync; ListStockMovementsAsync; GetLossSummaryAsync; GetIngredientCostAsync; GetRecipeCostAsync; GetPrepRecipeCostAsync; GetMenuItemProfitabilityAsync; ResolveSellingPriceAsync; GetVarianceAsync; GetSharedConsumptionReportAsync; ListRecipesAsync; GetRecipeAsync; ListPrepRecipesAsync; ListOrderPackagingRulesAsync; LoadFactsAsync; GetRecipeCostDetailAsync; ListRecipeCostSummariesAsync; GetRecipeCostCoverageAsync; BuildCostLine; Profitability; LoadItemUnitsAsync; ResolvePriceWithBasisAsync; GetInventoryOverviewAsync; GetInventoryItemDetailAsync; ListStockMovementRowsAsync; LoadCostingContextAsync; ToDomainLine; ToRecipeDto; ToPrepRecipeDto

**interactions_and_risks**: Full508 lines read in2 bounded chunks. Many calls reload all inventory/preps/purchasehistory and perline linear lookups; current-cost queries grow lifetime. Loss summary null LossValue=>0 without unknown coverage, contradict unknownnotzero pledge. Variance current recipe/deduction vs cancelled-order filter intentionally uses current recipe but cancelled usage remains actual, neutral signal. Shareddays rounds elapsed duration not inclusive businessdates. Recipe summary uses recipe.ProductId baseprice even if no product backlink, while detail resolves backlink: same recipe margin may differ. Coverage productsWithoutRecipe ignores variant-only recipe and inactivecategory; uncosted ingredient counts inactive. Detail movementlist ignores requested from/to while totals scoped. Enum.Parse bad authoring metadata crashes whole costly reads.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared all costing domain/save/deduction/stock movement sources, no runtime query against user DB.

**recommendations**: SQLaggregate stock totals/latest unitcost, cache consistent versioned costingcontext, propagate unknown loss-value coverage, align recipeprice provenance and range movement list; bounded pagination and canonical validated enums.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
