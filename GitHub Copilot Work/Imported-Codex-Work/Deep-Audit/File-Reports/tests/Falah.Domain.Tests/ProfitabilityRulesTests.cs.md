# tests/Falah.Domain.Tests/ProfitabilityRulesTests.cs

**path**: tests/Falah.Domain.Tests/ProfitabilityRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 63938933d1b29a843037c374978463a4cc9ada64689c115596ecf1670ffb2eb9

**git_blob_sha256**: 5093e9055133bfd324283e1787ee86e64e4185e85d27c5c18f48f01c8e3d17f4

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 171]]

**functionality_and_reasoning**: Normal and negative gross profit, no sales, partial food coverage, operating costs and unknown wage coverage covered. Operating result remains computed when wages unknown; file establishes bounded estimate policy rather than complete-cost accounting.

**significant_methods**: P helper; gross and operating profitability facts

**interactions_and_risks**: Normal and negative gross profit, no sales, partial food coverage, operating costs and unknown wage coverage covered. Operating result remains computed when wages unknown; file establishes bounded estimate policy rather than complete-cost accounting.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Unknown food and zero wage distinctions covered; historical live cost and packaging uncertainty are not.

**recommendations**: Add historical costing provenance and missing packaging/wage UI interpretation cases.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
