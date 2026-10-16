# tests/Falah.Server.IntegrationTests/AttendanceVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/AttendanceVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 003ae8b1a74111162a17d6e3f532638c21898a2e4871ace1d57f82da2eb40a66

**git_blob_sha256**: 1b4cdc0cbbc4dafef48b6d0843631814d096bb3c8ba0cab52eeb48f210013e69

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 329]]

**functionality_and_reasoning**: Covers check-in wage dedup, checkout guards, manual pay permissions, correction reason, upward Adjustment credit, replay receipts across newer corrections/restart and two correction serialization. Fact72 claims automatic late penalty but actually asserts immediate no-penalty path87; real late positive path absent. Test206-217 proves Adjustment credit intentionally posted, but no subsequent payroll calculation verifies it contributes to pay FIN-012. Duplicate check-in tests assert wage balance, not timestamp immutability or original event chronology.

**significant_methods**: InitializeAsync; DisposeAsync; CreateDailyEmployeeAsync; biometric/manual/correction/replay/concurrency facts

**interactions_and_risks**: Covers check-in wage dedup, checkout guards, manual pay permissions, correction reason, upward Adjustment credit, replay receipts across newer corrections/restart and two correction serialization. Fact72 claims automatic late penalty but actually asserts immediate no-penalty path87; real late positive path absent. Test206-217 proves Adjustment credit intentionally posted, but no subsequent payroll calculation verifies it contributes to pay FIN-012. Duplicate check-in tests assert wage balance, not timestamp immutability or original event chronology.

**findings**: ["FIN-012"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 329 lines fully read; no tests executed. Concurrency tests use actual factory SQL if run; current user database untouched.

**recommendations**: Add clock-controlled lateness/overnight cases and correction-to-payroll net reconciliation, plus distinct biometric replay ordering.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
