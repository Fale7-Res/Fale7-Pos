# src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs

**path**: src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: cd1a406323ee3e43ac267321d1b9d8b42cc739fb7e18ad87fdc5ca9c2162f099

**git_blob_sha256**: cd1a406323ee3e43ac267321d1b9d8b42cc739fb7e18ad87fdc5ca9c2162f099

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 82]]

**functionality_and_reasoning**: Expected drawer from original-shift orders net lifetime refunds and selective movements. refund type excluded; historical cash refund operationally current shift omitted from current drawer, old shift query changes after close. Order children loaded for whole shift.

**significant_methods**: ComputeExpectedCashAsync; ComputeBreakdownAsync

**interactions_and_risks**: Expected drawer from original-shift orders net lifetime refunds and selective movements. refund type excluded; historical cash refund operationally current shift omitted from current drawer, old shift query changes after close. Order children loaded for whole shift.

**findings**: ["FIN-002", "FIN-004"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
