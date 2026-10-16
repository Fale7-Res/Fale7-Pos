# src/Falah.Infrastructure/Inventory/SaveRecipeHandler.cs

**path**: src/Falah.Infrastructure/Inventory/SaveRecipeHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 75de7f4c93ff90697af92ac8686586dd0ea88b5d95def6bc8551a3a15a1298af

**git_blob_sha256**: b86797c7ddaf58100d4acd1f9872655204613e9791d356f2ce0083683918cf7b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 104]]

**functionality_and_reasoning**: Full104 lines read. Replaces whole recipe lines transactionally, requires nonempty active recipe and positive quantities. Does not validate Kind enum or Yield ratio; invalid Kind later Enum.Parse at checkout throws, missing ratio silently skips stock. Can change ProductId of existing recipe without clearing old product RecipeId, so old dish starts consuming new dish recipe. Multiple active per product possible, last product backlink wins. Same create request concurrent can throw PK instead of replay.

**significant_methods**: Constructor; HandleAsync; ToDto

**interactions_and_risks**: Full104 lines read. Replaces whole recipe lines transactionally, requires nonempty active recipe and positive quantities. Does not validate Kind enum or Yield ratio; invalid Kind later Enum.Parse at checkout throws, missing ratio silently skips stock. Can change ProductId of existing recipe without clearing old product RecipeId, so old dish starts consuming new dish recipe. Multiple active per product possible, last product backlink wins. Same create request concurrent can throw PK instead of replay.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared MenuPricingService and InventoryDeductionService; no DB writes.

**recommendations**: Validate all line kinds/yields and existing ingredient/product; clear old backlink when reassociating; define unique active recipe policy; add same-request race and reassignment tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
