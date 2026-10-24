# tests/Falah.Server.IntegrationTests/ActorTokenHandler.cs

**path**: tests/Falah.Server.IntegrationTests/ActorTokenHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 1cff4e756e63668ef2f0d1e8d3861e683726dd9e125a499df1b58ef316005ce6

**git_blob_sha256**: 660900ba724e1512b09a960470e93182046331e88c125e4d0e366d684d7a9a58

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 92]]

**functionality_and_reasoning**: Test transport selects actual seeded session matching actor fields in query then JSON body; otherwise falls back to any session. Buffered body preserves content, malformed JSON still reaches server. Automatically matching actor hides accidental identity mismatch in ordinary fixtures; explicit raw clients are required to test unauthenticated and cross-user attacks. Query wins over body by design, so middleware parity matters.

**significant_methods**: constructor; SendAsync; FromQuery; FromBodyAsync

**interactions_and_risks**: Test transport selects actual seeded session matching actor fields in query then JSON body; otherwise falls back to any session. Buffered body preserves content, malformed JSON still reaches server. Automatically matching actor hides accidental identity mismatch in ordinary fixtures; explicit raw clients are required to test unauthenticated and cross-user attacks. Query wins over body by design, so middleware parity matters.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 92 lines read. Comment promises real sessions rather than bypass; does not itself validate authorization middleware correctness.

**recommendations**: Keep negative tests with manually mismatched tokens and unknown actors; ensure new actor field names update helper and middleware together.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
