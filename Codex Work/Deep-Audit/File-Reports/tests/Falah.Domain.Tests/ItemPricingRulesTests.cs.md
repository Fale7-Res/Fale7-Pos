# tests/Falah.Domain.Tests/ItemPricingRulesTests.cs

**path**: tests/Falah.Domain.Tests/ItemPricingRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 865e42433d80bf44b80ed09079f648f6269d7c64d47c8c73d1377629e398521e

**git_blob_sha256**: 865e42433d80bf44b80ed09079f648f6269d7c64d47c8c73d1377629e398521e

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 54]]

**functionality_and_reasoning**: Checks no requested rule fallback, valid fixed snapshot, expired rule fallback and wrong product rejection. Silent fallback means an expired requested price becomes list price; API/UI repricing awareness is outside this test file.

**significant_methods**: Requested-price selection facts

**interactions_and_risks**: Checks no requested rule fallback, valid fixed snapshot, expired rule fallback and wrong product rejection. Silent fallback means an expired requested price becomes list price; API/UI repricing awareness is outside this test file.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Four selection scenarios do not cover competing rules or variant pricing.

**recommendations**: Add higher fixed prices, variant association and stale requested rule behavior assertions.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
