# src/Falah.Infrastructure/Menu/MenuQueryService.cs

**path**: src/Falah.Infrastructure/Menu/MenuQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 90e1091a22eb19c817a9c4192644246980105aa1839fbd386827f01d48655608

**git_blob_sha256**: 90e1091a22eb19c817a9c4192644246980105aa1839fbd386827f01d48655608

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 136]]

**functionality_and_reasoning**: Full136 lines read. Active categories+available product menu, bestsellers computes sellableProductIds including products in disabled categories, so returned shortcut IDs may not exist category DTO. IDsort then full rows multiple snapshots graceful missing records; variants cheapest and separate no blob content. Claimed historical measured memory tuning not independently verified. Bestseller 14day start no upperbound future rows included, noncancel SQL casing collation assumption.

**significant_methods**: Constructor; GetMenuAsync; LoadProductsInMenuOrderAsync; LoadBestSellerIdsAsync; ListAllCategoriesAsync; ListAllProductsAsync; GetProductAsync; ToProductDetailsDto

**interactions_and_risks**: Full136 lines read. Active categories+available product menu, bestsellers computes sellableProductIds including products in disabled categories, so returned shortcut IDs may not exist category DTO. IDsort then full rows multiple snapshots graceful missing records; variants cheapest and separate no blob content. Claimed historical measured memory tuning not independently verified. Bestseller 14day start no upperbound future rows included, noncancel SQL casing collation assumption.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared MenuPricing server permissive inactivecategory and snapshot variant removal.

**recommendations**: Return bestseller IDs restricted actual rendered category products, SQL group recent quantities, explicit imageversion caching; tests disabled category shortcut and catalog concurrent edit.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
