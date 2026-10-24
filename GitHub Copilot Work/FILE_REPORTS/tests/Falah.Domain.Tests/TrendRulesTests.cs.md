# tests/Falah.Domain.Tests/TrendRulesTests.cs

**path**: tests/Falah.Domain.Tests/TrendRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 49c6d56382ad7920ff68e0ca121adf7b6b974a45480c85e41f1631d21ca79a54

**git_blob_sha256**: 49c6d56382ad7920ff68e0ca121adf7b6b974a45480c85e41f1631d21ca79a54

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 93]]

**functionality_and_reasoning**: Seven-day same-weekday comparison, rising/falling values, zero denominator and unchanged values are covered. Negative previous values, extreme decimal values, reversed dates and longer periods are not.

**significant_methods**: Comparable period and percent-change facts

**interactions_and_risks**: Seven-day same-weekday comparison, rising/falling values, zero denominator and unchanged values are covered. Negative previous values, extreme decimal values, reversed dates and longer periods are not.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Pure date/arithmetic tests only.

**recommendations**: Specify reversed-range and negative-base behavior.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
