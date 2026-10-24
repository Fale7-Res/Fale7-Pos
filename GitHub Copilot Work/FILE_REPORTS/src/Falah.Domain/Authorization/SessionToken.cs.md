# src/Falah.Domain/Authorization/SessionToken.cs

**path**: src/Falah.Domain/Authorization/SessionToken.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 356e4dcde7e8778421f240589c9f00791d583700266d9b1d548ffc380e6d2668

**git_blob_sha256**: 356e4dcde7e8778421f240589c9f00791d583700266d9b1d548ffc380e6d2668

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 39]]

**functionality_and_reasoning**: Random 32-byte bearer token with SHA256 persisted; 16h idle expiration strict greater-than. Future LastSeen extends session until clock catches; clock dependency static. Absolute lifetime/revocation handled elsewhere, only idle enforced here. No source secret.

**significant_methods**: Issue; Hash; IsExpired

**interactions_and_risks**: Random 32-byte bearer token with SHA256 persisted; 16h idle expiration strict greater-than. Future LastSeen extends session until clock catches; clock dependency static. Absolute lifetime/revocation handled elsewhere, only idle enforced here. No source secret.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
