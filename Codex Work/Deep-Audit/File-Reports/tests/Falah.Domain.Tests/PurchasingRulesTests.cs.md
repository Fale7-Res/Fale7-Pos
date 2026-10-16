# tests/Falah.Domain.Tests/PurchasingRulesTests.cs

**path**: tests/Falah.Domain.Tests/PurchasingRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: cf9c5244c2bda72d0d9021368974d7adb85b1a0d0dce2e5f69d0564c77760496

**git_blob_sha256**: cf9c5244c2bda72d0d9021368974d7adb85b1a0d0dce2e5f69d0564c77760496

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 89]]

**functionality_and_reasoning**: Rounding, latest positive receipt, missing price, payment bounds, supported payment accounts and supplier attribution covered. The 499.5 example rounds to500 under multiple midpoint policies and does not prove policy distinction. No quantity/cost positivity or distinct-request stale invoice concurrency scenario.

**significant_methods**: Line totals, latest cost, payment guards and supplier ledger facts

**interactions_and_risks**: Rounding, latest positive receipt, missing price, payment bounds, supported payment accounts and supplier attribution covered. The 499.5 example rounds to500 under multiple midpoint policies and does not prove policy distinction. No quantity/cost positivity or distinct-request stale invoice concurrency scenario.

**findings**: ["FIN-005"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Pure arithmetic and payment guards cannot establish transactional safety FIN-005.

**recommendations**: Add midpoint discriminator, negative cost/quantity, duplicate ingredient and concurrent distinct-request scenarios.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
