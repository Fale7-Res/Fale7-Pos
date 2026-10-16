# src/Falah.Infrastructure/Persistence/Entities/MenuEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/MenuEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 93fb6d04445a3c79c5bebc713cb79811bc36fa918b8eda32e3e320651ad007a5

**git_blob_sha256**: dafe7329539f81ecfcb08368ba4f141ba946b88eeadcdabed7e17c44568441f4

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 67]]

**functionality_and_reasoning**: Full67 lines read. Live menu has parallel SendToGrill state vs PreparationStation routing intentionally independent; category Active separate product available. Product images own table avoids menu blob loads, version supports cache. Variant no Active, so removal sold variant blocked FK; no price sign constraints. Recipe references plain FK-free and can stale.

**significant_methods**: CategoryEntity; ProductEntity; ProductImageEntity; ProductVariantEntity properties

**interactions_and_risks**: Full67 lines read. Live menu has parallel SendToGrill state vs PreparationStation routing intentionally independent; category Active separate product available. Product images own table avoids menu blob loads, version supports cache. Variant no Active, so removal sold variant blocked FK; no price sign constraints. Recipe references plain FK-free and can stale.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared product authoring and authoritative pricing, image handlers.

**recommendations**: Add controlled variant retirement policy, validate prices and recipe ownership; avoid pretending current category is historical snapshot.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
