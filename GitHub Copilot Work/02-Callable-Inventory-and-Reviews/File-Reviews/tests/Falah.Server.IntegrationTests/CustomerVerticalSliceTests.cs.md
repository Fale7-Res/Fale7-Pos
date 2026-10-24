# tests/Falah.Server.IntegrationTests/CustomerVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/CustomerVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 0f899776bbe0896d46369919825417596d4d4739cc34f20a549a940c63108b50

**git_blob_sha256**: a9cdc09c311eb34751f796504476e91b667b29ea1f463f6c8d9f7e0858cc8182

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 182]]

**functionality_and_reasoning**: Tests normalized phone identity, name refresh, geographic address append versus service/landmark refresh, required fields, read-only unknown lookup, ID retrieval and six concurrent same-address upserts. First response often unchecked; some names broader than assertions (full record compares ID/phone; never modifies only unknown empty lookup). Concurrent count uses area not CustomerId, adequate current empty fixture but fragile expansion. No unicode/country normalization, deterministic overlap or concurrent different-address merge tests.

**significant_methods**: InitializeAsync; DisposeAsync; MakeRequest; fourteen customer upsert/lookup/concurrency facts and eleven lambdas individually identified in Knowledge shard

**interactions_and_risks**: Tests normalized phone identity, name refresh, geographic address append versus service/landmark refresh, required fields, read-only unknown lookup, ID retrieval and six concurrent same-address upserts. First response often unchecked; some names broader than assertions (full record compares ID/phone; never modifies only unknown empty lookup). Concurrent count uses area not CustomerId, adequate current empty fixture but fragile expansion. No unicode/country normalization, deterministic overlap or concurrent different-address merge tests.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 182 lines fully read. All28 callable stable IDs reasoned individually in Knowledge/Reviews/financial_integrity exact file shard. No tests executed.

**recommendations**: Add persisted rejection rollback, stable address ID, all-field readback and barrier-controlled normalization races.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
