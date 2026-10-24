# tests/Falah.Domain.Tests/CostingRulesTests.cs

**path**: tests/Falah.Domain.Tests/CostingRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 63f20a348114e234a1191aeb75478e6a9671d3f78d74736f8ce2b4dd58cf5cdb

**git_blob_sha256**: 7b3c5edd19f26daa20ca9fd173a9723ff6b1f9d6f8a25afd8b0c2258ce7cb44f

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 198]]

**functionality_and_reasoning**: Tests latest known purchase, missing costs, yield division, food versus packaging and margin. Self/two-node cycles return bounded results. These fixtures do not establish concurrent recipe-save safety, unique active producer selection or deep graph behavior.

**significant_methods**: EmptyContext; ingredient, preparation, dish and cycle facts

**interactions_and_risks**: Tests latest known purchase, missing costs, yield division, food versus packaging and margin. Self/two-node cycles return bounded results. These fixtures do not establish concurrent recipe-save safety, unique active producer selection or deep graph behavior.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Pure graph calculation fixtures only; no save path exercised.

**recommendations**: Cover empty recipe, ambiguous producer, deep graph and rounded-to-zero yield.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
