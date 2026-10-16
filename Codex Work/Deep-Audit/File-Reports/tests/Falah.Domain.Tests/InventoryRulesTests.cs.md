# tests/Falah.Domain.Tests/InventoryRulesTests.cs

**path**: tests/Falah.Domain.Tests/InventoryRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 5a9a44e238b4e38ff34b16ae6aab22f26dbbb26bc183e26c63698090593b6cba

**git_blob_sha256**: 5a9a44e238b4e38ff34b16ae6aab22f26dbbb26bc183e26c63698090593b6cba

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 124]]

**functionality_and_reasoning**: Positive adjustments and reasons are validated; signed In/Out stock balance and separate purchase/usage/loss/count buckets establish intended direction semantics. Owner dashboard unsigned summation differs from these expectations.

**significant_methods**: Adjustment, waste, unit validation and stock balance facts

**interactions_and_risks**: Positive adjustments and reasons are validated; signed In/Out stock balance and separate purchase/usage/loss/count buckets establish intended direction semantics. Owner dashboard unsigned summation differs from these expectations.

**findings**: ["FIN-013"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Signed balance fixtures support FIN-013 candidate; no SQL concurrency tested.

**recommendations**: Add first-stock races, unit changes, invalid direction and date-range boundary coverage.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
