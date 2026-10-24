# src/Falah.Domain/Authorization/UserManagementRules.cs

**path**: src/Falah.Domain/Authorization/UserManagementRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 3a72ee5f6b79d83b6364bf22d8150b39b2a13211fa2db392f13b90c5b10c32fe

**git_blob_sha256**: 4657cce3a6d2ed18f56a128e82ee3aca9b919b8ba61cce0741e8a554ee147a50

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 71]]

**functionality_and_reasoning**: Owner protection exact lowercase string; grant rules constrain permissions not role. Admin survival count pure requires transaction protection. Known permissions case sensitive. Role issuance via CreateUser can broaden approval authority despite limited permission grant; Security coordinator informed.

**significant_methods**: AssertTargetIsNotOwner; AssertNoPrivilegeEscalation; CanManageUsers; AssertUserAdministrationSurvives; AssertKnownPermissions

**interactions_and_risks**: Owner protection exact lowercase string; grant rules constrain permissions not role. Admin survival count pure requires transaction protection. Known permissions case sensitive. Role issuance via CreateUser can broaden approval authority despite limited permission grant; Security coordinator informed.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
