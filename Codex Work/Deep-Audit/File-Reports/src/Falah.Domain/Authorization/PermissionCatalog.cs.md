# src/Falah.Domain/Authorization/PermissionCatalog.cs

**path**: src/Falah.Domain/Authorization/PermissionCatalog.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 53b0897f59c3278e3fcc261d7da88592ecfc14977bfa8dba9679569805571a69

**git_blob_sha256**: 53b0897f59c3278e3fcc261d7da88592ecfc14977bfa8dba9679569805571a69

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 58]]

**functionality_and_reasoning**: Explicit list includes all broad money/admin domains and all grant. Static IReadOnlyList wraps mutable List, IReadOnlySet wraps HashSet; trusted in-process consumers could mutate after init. Catalog completeness claim requires global call-site check by root; not inferred from this file. Descriptive stale comment says employees/settings not implemented despite entries present.

**significant_methods**: PermissionDefinition; All initializer; Keys initializer

**interactions_and_risks**: Explicit list includes all broad money/admin domains and all grant. Static IReadOnlyList wraps mutable List, IReadOnlySet wraps HashSet; trusted in-process consumers could mutate after init. Catalog completeness claim requires global call-site check by root; not inferred from this file. Descriptive stale comment says employees/settings not implemented despite entries present.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
