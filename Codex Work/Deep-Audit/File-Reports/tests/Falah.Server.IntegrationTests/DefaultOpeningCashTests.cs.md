# tests/Falah.Server.IntegrationTests/DefaultOpeningCashTests.cs

**path**: tests/Falah.Server.IntegrationTests/DefaultOpeningCashTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: c83d925533b572ac108851718afc95147390cc66aa32633375cc1c8bee57267f

**git_blob_sha256**: c83d925533b572ac108851718afc95147390cc66aa32633375cc1c8bee57267f

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 161]]

**functionality_and_reasoning**: Protects distinction between suggested float and confirmed stored float. Later settings changes do not rewrite shift amount/reconciliation or opening slip; negative rejected and zero audited. Facts named dialog reads only read settings API, not actual WinUI wiring. Funded fixture hides constrained-funds races FIN-011; no competing account spend. Next shift test reads new setting without actually opening second shift.

**significant_methods**: InitializeAsync; DisposeAsync; ReadSettingsAsync; SetDefaultOpeningCashAsync; OpenShiftAsync; CloseShiftAsync; default/custom/snapshot/next shift/slip/negative/zero audit facts

**interactions_and_risks**: Protects distinction between suggested float and confirmed stored float. Later settings changes do not rewrite shift amount/reconciliation or opening slip; negative rejected and zero audited. Facts named dialog reads only read settings API, not actual WinUI wiring. Funded fixture hides constrained-funds races FIN-011; no competing account spend. Next shift test reads new setting without actually opening second shift.

**findings**: ["FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 161 lines fully read; no SQL mutations/tests run.

**recommendations**: Keep snapshot tests; test real dialog separately and distinct Main spend versus open with bounded liquidity.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
