# src/Falah.Infrastructure/Users/ChangePinHandler.cs

**path**: src/Falah.Infrastructure/Users/ChangePinHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 92efa770a8ddbc53fe25ffb287a3d6344c052ad2cf863e6c879e7370eaa63b99

**git_blob_sha256**: 92efa770a8ddbc53fe25ffb287a3d6344c052ad2cf863e6c879e7370eaa63b99

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 66]]

**functionality_and_reasoning**: Self-service validates old PIN; other actor requires users_manage and owner protected. New PIN scans active users before hash, no serializable lock so concurrent changes same PIN race. Existing sessions not revoked, meaning credential reset does not invalidate stolen token. Atomic audit+hash save; no current hash concurrency token, so overlapping changes last-wins.

**significant_methods**: Constructor; HandleAsync

**interactions_and_risks**: Self-service validates old PIN; other actor requires users_manage and owner protected. New PIN scans active users before hash, no serializable lock so concurrent changes same PIN race. Existing sessions not revoked, meaning credential reset does not invalidate stolen token. Atomic audit+hash save; no current hash concurrency token, so overlapping changes last-wins.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
