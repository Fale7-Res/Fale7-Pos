# tests/Falah.Server.IntegrationTests/AuditLogVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/AuditLogVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d09936e48067c576386a254fce9a7a298f7171a27892d29aab82b9d7e2d5bfe8

**git_blob_sha256**: 14c08d440ec95a6dc0f5c9b5515063caf991b768e1a2120c3eb242b542ea4662

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 222]]

**functionality_and_reasoning**: Covers refund/expense severity,refund replay audit dedup,close/deposit/withdraw/transfer/safe-drop wiring and audit_view denial/allow. Most checks text/action presence, no exact operation identity or financial snapshot. Deposit test broad event may match initial funding; category All can pass empty; transaction atomicity claimed by class comment never fault-injected. Details substring money brittle.

**significant_methods**: All29actualcallable spans individually mapped

**interactions_and_risks**: Covers refund/expense severity,refund replay audit dedup,close/deposit/withdraw/transfer/safe-drop wiring and audit_view denial/allow. Most checks text/action presence, no exact operation identity or financial snapshot. Deposit test broad event may match initial funding; category All can pass empty; transaction atomicity claimed by class comment never fault-injected. Details substring money brittle.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 222lines fully read;29stablecallable receipts saved; no tests run.

**recommendations**: Add nonempty correlated events,typed money,combinedfilter/paging and induced business rollback no-orphan audit tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
