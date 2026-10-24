# src/Falah.Infrastructure/Persistence/Entities/PrintEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/PrintEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: b7bc9f20a28e2486a23f47d8094c1d44dc666998bb0f664cd9ce7aef810e4f4c

**git_blob_sha256**: 34ba05220793488413ae21f1e06f102d4da83a6a7ce24f0f0539df1bdbab580d

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 56]]

**functionality_and_reasoning**: Full56 lines read. No lease/claim owner/state/token confirms PRT-001 architecture. JobDocumentKind generic OrderId intentionally no FK permits slips, needs composer existence. Attempt idempotency index only, no submitted physical attempt timestamp; records server report time. IsReprint comment purely informational stale because compositor labels paper.

**significant_methods**: PrintJobEntity; PrintJobAttemptEntity properties

**interactions_and_risks**: Full56 lines read. No lease/claim owner/state/token confirms PRT-001 architecture. JobDocumentKind generic OrderId intentionally no FK permits slips, needs composer existence. Attempt idempotency index only, no submitted physical attempt timestamp; records server report time. IsReprint comment purely informational stale because compositor labels paper.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Independent PRT-001 challenge full agent send path, no printer invocation.

**recommendations**: Distributed claim+attempt fencing, retain uncertainty/physicalsend provenance; avoid job status implying paper confirmed.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
