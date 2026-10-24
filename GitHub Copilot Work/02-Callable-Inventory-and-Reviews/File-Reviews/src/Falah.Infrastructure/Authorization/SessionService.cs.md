# src/Falah.Infrastructure/Authorization/SessionService.cs

**path**: src/Falah.Infrastructure/Authorization/SessionService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81

**git_blob_sha256**: eb65442b2f36e359dee026d2988ef67def1b7484ca39fca09a8dbeff271cab81

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 78]]

**functionality_and_reasoning**: Issue persists token hash; Resolve checks revocation+idle+active user and periodically updates LastSeen after minute, so apparently read-only authenticated API can write session metadata. Shared scoped DbContext SaveChanges flushes all tracked state if callers already staged mutation; order of auth filter matters. PIN change does not revoke sessions via this service; no pruning.

**significant_methods**: Constructor; IssueAsync; ResolveAsync; RevokeAsync

**interactions_and_risks**: Issue persists token hash; Resolve checks revocation+idle+active user and periodically updates LastSeen after minute, so apparently read-only authenticated API can write session metadata. Shared scoped DbContext SaveChanges flushes all tracked state if callers already staged mutation; order of auth filter matters. PIN change does not revoke sessions via this service; no pruning.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
