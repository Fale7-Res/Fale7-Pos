# tests/Falah.Domain.Tests/PayablesAgingTests.cs

**path**: tests/Falah.Domain.Tests/PayablesAgingTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d9b5478b09316333de81688d36a1fd5875784c2619ee562cc12a3a8322fb74b2

**git_blob_sha256**: 9873dd5b974918550642f74f66cc2a35a5313776b88c524bf420ae943a428745

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 86]]

**functionality_and_reasoning**: Missing/future/today due dates, inclusive 1/30/31/60/61/90/91 boundaries and receipt age independent of overdue are covered. Future receipt age clamps zero. These classifications cannot detect invoice cache versus ledger disagreement.

**significant_methods**: Today helper; bucket, due-date and age facts

**interactions_and_risks**: Missing/future/today due dates, inclusive 1/30/31/60/61/90/91 boundaries and receipt age independent of overdue are covered. Future receipt age clamps zero. These classifications cannot detect invoice cache versus ledger disagreement.

**findings**: ["FIN-005"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Good pure boundary coverage; no as-of balance or payment concurrency.

**recommendations**: Add cache/ledger reconciliation and historical as-of outstanding tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
