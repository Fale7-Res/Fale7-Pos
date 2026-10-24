# tests/Falah.Domain.Tests/ShiftTests.cs

**path**: tests/Falah.Domain.Tests/ShiftTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: b6330e6b8ef5b67e8cb1f60f47252ade8e359375a9725ba12beed8b45715e2f5

**git_blob_sha256**: b6330e6b8ef5b67e8cb1f60f47252ade8e359375a9725ba12beed8b45715e2f5

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 100]]

**functionality_and_reasoning**: Negative float/actual, balanced close, small/large variance, manager name/reason and already closed guards exercised. Domain manager-name fixtures do not prove API authorization. No concurrent sale/close or refund in later shift scenario.

**significant_methods**: Float validation and blind-close facts

**interactions_and_risks**: Negative float/actual, balanced close, small/large variance, manager name/reason and already closed guards exercised. Domain manager-name fixtures do not prove API authorization. No concurrent sale/close or refund in later shift scenario.

**findings**: ["FIN-004"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Static tests corroborate close arithmetic but do not address FIN-004 attribution.

**recommendations**: Add operational shift refund reconciliation and concurrent close/checkout transaction tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
