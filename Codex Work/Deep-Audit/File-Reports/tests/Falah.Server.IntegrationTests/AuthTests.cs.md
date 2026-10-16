# tests/Falah.Server.IntegrationTests/AuthTests.cs

**path**: tests/Falah.Server.IntegrationTests/AuthTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 9cfe8dd84f80c5d211f1276852bd7b8bec1e642dccaad200c55929a682310798

**git_blob_sha256**: 9ccf2791e310b2d1889f6bb9b3f19a94b9e2d103e3698a3e622e79596fa0be06

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 75]]

**functionality_and_reasoning**: Seeds hashed PIN through HashWithoutValidation to test authentication independently from PIN creation validation, then verifies matching name/permission and Unauthorized failures. Client is preauthenticated; success response assertions do not verify returned SessionToken or subsequent anonymous-client use. Inactive test demonstrates denial but does not test already-issued token revocation. Literal test PIN values omitted from report.

**significant_methods**: InitializeAsync; DisposeAsync; SeedUserWithPinAsync; correct, wrong and inactive PIN facts

**interactions_and_risks**: Seeds hashed PIN through HashWithoutValidation to test authentication independently from PIN creation validation, then verifies matching name/permission and Unauthorized failures. Client is preauthenticated; success response assertions do not verify returned SessionToken or subsequent anonymous-client use. Inactive test demonstrates denial but does not test already-issued token revocation. Literal test PIN values omitted from report.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 75 lines fully read; no auth requests sent.

**recommendations**: Keep unauthenticated login and session round-trip/expiry/deactivation tests; coordinate security conclusions with parent.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
