# src/Falah.Infrastructure/Menu/ProductHandlers.cs

**path**: src/Falah.Infrastructure/Menu/ProductHandlers.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 98b29b5ace01d781c176b352b054b561973281e4e923c23cb0794412c6107247

**git_blob_sha256**: 72586c396821ff033030936f49f62c5dee957038d129fac80a11040cb88706a3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 206]]

**functionality_and_reasoning**: Full206 lines read. Base positive, variant prices/names not validated; negative variant creates nonpositive sales prices downstream. Update deletes omitted variants but sold OrderItem FK Restrict blocks legitimate variant deletion with generic DB exception; historical identity preserved but UI needs retirement path. Unknown input variant ID mints fresh GUID silently; duplicate known IDs repeated last-wins. RecipeId not FK and not validated. Receipt atomic for updates.

**significant_methods**: CreateProductHandler constructor/HandleAsync; UpdateProductHandler constructor/HandleAsync; SetProductAvailabilityHandler constructor/HandleAsync; MenuValidation.AssertValidStation

**interactions_and_risks**: Full206 lines read. Base positive, variant prices/names not validated; negative variant creates nonpositive sales prices downstream. Update deletes omitted variants but sold OrderItem FK Restrict blocks legitimate variant deletion with generic DB exception; historical identity preserved but UI needs retirement path. Unknown input variant ID mints fresh GUID silently; duplicate known IDs repeated last-wins. RecipeId not FK and not validated. Receipt atomic for updates.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared OrderItem variant FK context and authoritative MenuPricingService.

**recommendations**: Validate each variant price/name/identity, soft-retire sold variants, preserve old order FK; test negative variant, cross-product variant ID and omit sold variant.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
