# src/Falah.Domain/Backup/BackupRules.cs

**path**: src/Falah.Domain/Backup/BackupRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: daf5a6e64cf4e36df55f29a82f7beb91610a3190901adcbadca3b9f60661242e

**git_blob_sha256**: c2b4b6d3523da6f903f2f8627dbf0e4ad034e0bafeddacdd192598ac6de79131

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 119]]

**functionality_and_reasoning**: Plain identifier rejects SQL metacharacters. Retention preserves newest valid record but assumes each record unique file. Pending count pure guard requires actual exclusion at host. Path syntax validation not service ACL verification. NaN/infinite max age not rejected.

**significant_methods**: AssertDestinationDirectory; AssertRestoreReason; AssertRestoreConfirmationPhrase; AssertNoActiveOperations; AssertSafeDatabaseIdentifier; IsAutomaticBackupDue; PrunableBackupCount; SelectPrunableBackups

**interactions_and_risks**: Plain identifier rejects SQL metacharacters. Retention preserves newest valid record but assumes each record unique file. Pending count pure guard requires actual exclusion at host. Path syntax validation not service ACL verification. NaN/infinite max age not rejected.

**findings**: ["FIN-007", "FIN-008"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
