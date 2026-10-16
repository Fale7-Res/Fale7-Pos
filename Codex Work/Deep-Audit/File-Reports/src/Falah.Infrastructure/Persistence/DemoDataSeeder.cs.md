# src/Falah.Infrastructure/Persistence/DemoDataSeeder.cs

**path**: src/Falah.Infrastructure/Persistence/DemoDataSeeder.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 60e04791dc8f76993d9dd871bd997b702d18aedfd6570af01e73634e86dea1c2

**git_blob_sha256**: 60e04791dc8f76993d9dd871bd997b702d18aedfd6570af01e73634e86dea1c2

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 215]]

**functionality_and_reasoning**: Seed gate checks Products.Any only, not each catalog or user. Main method stages deterministic demo cashier/manager hashed PIN accounts, ten categories, one inventory/recipe with no opening stock, menu breadth/variants and two fixed price offers then single SaveChanges213. Existing product causes all user/schema backfill to skip; empty product table alongside retained seeded IDs can produce key conflicts instead of safe repair. Concurrent empty startups can both stage same deterministic keys and one fails, absent external startup serialization. Simple converts EGPlong to piastres; WithVariants relies nonempty tuples (all actual calls nonempty). No treasury funding/day/shift/settings creation, so successful seed does not make ready-to-trade state. Credential values deliberately omitted.

**significant_methods**: SeedIfEmptyAsync14-214; local Simple115-120; local WithVariants122-132

**interactions_and_risks**: Seed gate checks Products.Any only, not each catalog or user. Main method stages deterministic demo cashier/manager hashed PIN accounts, ten categories, one inventory/recipe with no opening stock, menu breadth/variants and two fixed price offers then single SaveChanges213. Existing product causes all user/schema backfill to skip; empty product table alongside retained seeded IDs can produce key conflicts instead of safe repair. Concurrent empty startups can both stage same deterministic keys and one fails, absent external startup serialization. Simple converts EGPlong to piastres; WithVariants relies nonempty tuples (all actual calls nonempty). No treasury funding/day/shift/settings creation, so successful seed does not make ready-to-trade state. Credential values deliberately omitted.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 215 lines fully read with PIN values redacted in tool display; no seeder executed. All actual helper calls and tuple prices reasoned; demo sources are not production cost evidence.

**recommendations**: Separate fresh-install bootstrap from partial repair with explicit operator approval; test populated product upgrade and empty menu with existing users; provision production accounts separately.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
