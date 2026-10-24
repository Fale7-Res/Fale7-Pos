# tests/Falah.Domain.Tests/PricingRulesTests.cs

**path**: tests/Falah.Domain.Tests/PricingRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d2ed75da60d780068455b29bfdd287ac65a6e766c41bdc603c88300546028e4b

**git_blob_sha256**: d2ed75da60d780068455b29bfdd287ac65a6e766c41bdc603c88300546028e4b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 119]]

**functionality_and_reasoning**: Exercises inactive/date/day/time pricing, overnight windows, list fallback and complimentary rules. Negative complimentary quantity is named but only zero is exercised. Overnight weekday spill and equal window endpoints remain untested.

**significant_methods**: MakeRule; pricing window, fallback, complimentary and validation facts

**interactions_and_risks**: Exercises inactive/date/day/time pricing, overnight windows, list fallback and complimentary rules. Negative complimentary quantity is named but only zero is exercised. Overnight weekday spill and equal window endpoints remain untested.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Pure domain assertions read statically; no tests executed here.

**recommendations**: Add exact time endpoints, partial window, weekday crossover and negative complimentary cases.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
