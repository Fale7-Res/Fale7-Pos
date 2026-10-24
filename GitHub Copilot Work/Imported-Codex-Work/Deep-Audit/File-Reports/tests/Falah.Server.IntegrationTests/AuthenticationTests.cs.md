# tests/Falah.Server.IntegrationTests/AuthenticationTests.cs

**path**: tests/Falah.Server.IntegrationTests/AuthenticationTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d7be257eefc71ce6e5379302a13b1d79b6add5c60c6fd6f207b108335f1edc58

**git_blob_sha256**: d7be257eefc71ce6e5379302a13b1d79b6add5c60c6fd6f207b108335f1edc58

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 150]]

**functionality_and_reasoning**: Raw client explicitly avoids actor-token helper, testing unauthenticated list/report401, forged session401, cashier claiming manager403 for report and withdrawal, own insufficient permission422, logout invalidation and real PIN approval. Login returns token and health remains public, stronger than old AuthTests. Fixtures seed session directly for most tests, so success does not validate password-to-token-to-protected API roundtrip. No expiration, session restart, user deactivation, query/body conflict, malformed/large body or all-route actor-field coverage here. Comments describing former bugs do not establish current vulnerabilities.

**significant_methods**: InitializeAsync; DisposeAsync; AuthedAs; anonymous user/report/forged token tests; cross actor read/write; own actor permission; logout; approval PIN; cashier approval denied; login health facts

**interactions_and_risks**: Raw client explicitly avoids actor-token helper, testing unauthenticated list/report401, forged session401, cashier claiming manager403 for report and withdrawal, own insufficient permission422, logout invalidation and real PIN approval. Login returns token and health remains public, stronger than old AuthTests. Fixtures seed session directly for most tests, so success does not validate password-to-token-to-protected API roundtrip. No expiration, session restart, user deactivation, query/body conflict, malformed/large body or all-route actor-field coverage here. Comments describing former bugs do not establish current vulnerabilities.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 150 lines fully read; no auth/network/DB actions executed.

**recommendations**: Retain raw-client negative tests, add token lifecycle and cross-field mismatches; parent Security coordinator owns vulnerability conclusions.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
