# tests/Falah.Domain.Tests/BackupRulesTests.cs

**path**: tests/Falah.Domain.Tests/BackupRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 7960285fd6e0f23a2bd584dd2c62fd4648661e5ee30e26e11a0e32e9d433763e

**git_blob_sha256**: 9c6d69ab8a0058a795d5472d896063971f11d2e8ace4728f25f0d708a44d22ce

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 160]]

**functionality_and_reasoning**: Tests phrase/reason/pending count guards, automatic backup timing threshold and retention protecting newest verified copy. Pure rules do not create SQL maintenance exclusion or distinct physical backup filenames. Negative/nonfinite intervals absent.

**significant_methods**: Directory, restore guards, interval and retention facts

**interactions_and_risks**: Tests phrase/reason/pending count guards, automatic backup timing threshold and retention protecting newest verified copy. Pure rules do not create SQL maintenance exclusion or distinct physical backup filenames. Negative/nonfinite intervals absent.

**findings**: ["FIN-007", "FIN-008"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: No backup/restore execution; FIN-007/008 remain static candidates.

**recommendations**: Add physical path collision tests and exclusive maintenance protocol tests safely isolated.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
