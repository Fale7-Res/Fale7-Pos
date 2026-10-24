# tests/Falah.Server.IntegrationTests/AuditWiringCoverageTests.cs

**path**: tests/Falah.Server.IntegrationTests/AuditWiringCoverageTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 953e67177ca67de0b5a1e40924f29a32fc9f418bbebbf5c8b7a4cf99b565d7fe

**git_blob_sha256**: 2f159d1801a07b3d26192c40a6c1eada961ac0b5ff191356a3c19b1a088313d2

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 181]]

**functionality_and_reasoning**: Exercises unified audit events for comp, delivery, inventory, supplier receive/pay and reprint. Mostly action substring plus reference assertions, so actor, money, reason, operation count/rollback and immutable snapshots not established. Delivery full happy path10000 expected/actual, purchase sequential5000 paid2000+3000 and inventory500-200 are source facts but conservation not asserted. Original/reprint POST statuses unchecked, possibly weakening no-original-audit proof.

**significant_methods**: All22 callable spans individually reasoned in Knowledge shard

**interactions_and_risks**: Exercises unified audit events for comp, delivery, inventory, supplier receive/pay and reprint. Mostly action substring plus reference assertions, so actor, money, reason, operation count/rollback and immutable snapshots not established. Delivery full happy path10000 expected/actual, purchase sequential5000 paid2000+3000 and inventory500-200 are source facts but conservation not asserted. Original/reprint POST statuses unchecked, possibly weakening no-original-audit proof.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 181 lines fully read;22 stable callable records saved atomically; no tests executed.

**recommendations**: Correlate exact operation keys/amounts/actors and assert queue request status; add rejected/retried/concurrent transaction audit consistency.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
