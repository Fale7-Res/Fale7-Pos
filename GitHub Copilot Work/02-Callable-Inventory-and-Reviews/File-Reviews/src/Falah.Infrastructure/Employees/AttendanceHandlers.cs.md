# src/Falah.Infrastructure/Employees/AttendanceHandlers.cs

**path**: src/Falah.Infrastructure/Employees/AttendanceHandlers.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 258fd52af239fd06e3084242f71fd0cbfbbd0baf1dce78dd1dbe8498531bffe6

**git_blob_sha256**: 445bd6901c7f5e3f170135127a0e68fc9149e2aa175f1c4998256a0ba3760d07

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 154]]

**functionality_and_reasoning**: Full154 lines read. No explicit permission by design device/simulator assumption root Security owns endpoint. Active employee not checked; duplicate event does not no-op attendance row and overwrites CheckIn/late while deterministic wage/penalty unchanged. Time HH:mm UTC but Date Cairo, checkout reconstructs same UTC date, crossing UTC midnight/Cairo calendar boundaries produces0/rejected attendance. Checkout finds only today so overnight across Cairo midnight impossible. Repeated late clock-out changes hours but overtime entry dedup freezes first amount. First record unique-index concurrency throws no replay catch. Wage key shift means recheck in second shift same day credits again by design.

**significant_methods**: BiometricCheckInHandler constructor/HandleAsync; BiometricCheckOutHandler constructor/HandleAsync

**interactions_and_risks**: Full154 lines read. No explicit permission by design device/simulator assumption root Security owns endpoint. Active employee not checked; duplicate event does not no-op attendance row and overwrites CheckIn/late while deterministic wage/penalty unchanged. Time HH:mm UTC but Date Cairo, checkout reconstructs same UTC date, crossing UTC midnight/Cairo calendar boundaries produces0/rejected attendance. Checkout finds only today so overnight across Cairo midnight impossible. Repeated late clock-out changes hours but overtime entry dedup freezes first amount. First record unique-index concurrency throws no replay catch. Wage key shift means recheck in second shift same day credits again by design.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared PayrollRules and EmployeeLedgerPoster, no biometric event injected.

**recommendations**: Persist original full instants and business-date shift association, idempotent event inbox; corrections must reconcile entitlement deltas, active employee check, overnight/DST/month-boundary regressions.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
