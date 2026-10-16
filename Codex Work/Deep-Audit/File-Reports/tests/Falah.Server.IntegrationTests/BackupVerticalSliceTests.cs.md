# tests/Falah.Server.IntegrationTests/BackupVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/BackupVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: dc41e89b45f84cdaf6ee2eff37d3940c3edc914b78875245681a3a1dfd2d04b2

**git_blob_sha256**: 9b0aeb06456a4e6732b344ea6a0d5655ab16822e55303e53044f3e034c4a314c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 150]]

**functionality_and_reasoning**: Uses real SQL backups under fixed E drive directory; successful create/file existence and VERIFYONLY tested. Distinct filename claim60 is weakened by explicit1100ms delay64, excluding exactly same-second FIN-007 case. Restore tests all intentionally stop before RESTORE; no restore exclusion/drain or recovered application readiness coverage FIN-008. Class comment claims active-operation gate fully exercised but file has no pending journal or concurrent write test. Backup files created are not cleaned here; factory disposal drops DB only.

**significant_methods**: InitializeAsync; DisposeAsync; backup permission/create/list/distinct-name/verify facts; restore permission/phrase/reason/audit gate facts

**interactions_and_risks**: Uses real SQL backups under fixed E drive directory; successful create/file existence and VERIFYONLY tested. Distinct filename claim60 is weakened by explicit1100ms delay64, excluding exactly same-second FIN-007 case. Restore tests all intentionally stop before RESTORE; no restore exclusion/drain or recovered application readiness coverage FIN-008. Class comment claims active-operation gate fully exercised but file has no pending journal or concurrent write test. Backup files created are not cleaned here; factory disposal drops DB only.

**findings**: ["FIN-007", "FIN-008"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 150 lines fully read; no test/backup/restore executed.

**recommendations**: Replace uniqueness delay with fixed-clock same-instant concurrent fixture; verify maintenance fencing and actual restore only in explicitly approved disposable environment.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
