# src/Falah.Infrastructure/Employees/PayrollQueryService.cs

**path**: src/Falah.Infrastructure/Employees/PayrollQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 757dd6e6a2d74462e08602eadadd6b0b989e54f838a67cac9867ead8e1b31af2

**git_blob_sha256**: 757dd6e6a2d74462e08602eadadd6b0b989e54f838a67cac9867ead8e1b31af2

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 31]]

**functionality_and_reasoning**: Full31 lines read. Read-only run/line projections no authorization inside, host gate essential. Missing run lines returns empty unlike GetRun throws. Unbounded run history. Status/payout metadata returned separately, generic Paid bypass visible null TreasuryMovementId but not rejected.

**significant_methods**: Constructor; ListRunsAsync; GetRunAsync; ListLinesAsync

**interactions_and_risks**: Full31 lines read. Read-only run/line projections no authorization inside, host gate essential. Missing run lines returns empty unlike GetRun throws. Unbounded run history. Status/payout metadata returned separately, generic Paid bypass visible null TreasuryMovementId but not rejected.

**findings**: ["FIN-006"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared FIN-006 state update and dedicated payout.

**recommendations**: Expose consistency indicators for Paid-without-movement and null-line references, paginate; enforce terminal consistency in write path.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
