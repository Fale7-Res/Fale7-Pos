# src/Falah.Infrastructure/Shifts/PrepareShiftCloseHandler.cs

**path**: src/Falah.Infrastructure/Shifts/PrepareShiftCloseHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 23708a94b842bf26ec37bcf66b389996174e229caff9651f6c86b4ec6170b16c

**git_blob_sha256**: 23708a94b842bf26ec37bcf66b389996174e229caff9651f6c86b4ec6170b16c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 81]]

**functionality_and_reasoning**: Full81 lines read. Read-only expected calculation after counted amount; no actor/permission in request service so endpoint auth essential. Threshold Settings first then config/default; close evaluator overload no config fallback, causing differing threshold when no Settings row and configured threshold. Config negative floored Domain but control evaluator not floored. Expected can change before final close correctly recomputed there.

**significant_methods**: Constructor; HandleAsync; ReadDiscrepancyThresholdAsync overloads

**interactions_and_risks**: Full81 lines read. Read-only expected calculation after counted amount; no actor/permission in request service so endpoint auth essential. Threshold Settings first then config/default; close evaluator overload no config fallback, causing differing threshold when no Settings row and configured threshold. Config negative floored Domain but control evaluator not floored. Expected can change before final close correctly recomputed there.

**findings**: ["FIN-004"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared FIN-004 expected-cash selection; no endpoint requests.

**recommendations**: Use one threshold provider everywhere and enforce blind-count endpoint permission/privacy; test config/no-row and settings overrides consistent.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
