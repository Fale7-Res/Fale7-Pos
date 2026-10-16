# src/Falah.Domain/Authorization/Actor.cs

**path**: src/Falah.Domain/Authorization/Actor.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: bd3c6edcf6f3dd8ab4f27d0d8890f0c15f278c2aa3420c5570ebf928a4f4203c

**git_blob_sha256**: bd3c6edcf6f3dd8ab4f27d0d8890f0c15f278c2aa3420c5570ebf928a4f4203c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 9]]

**functionality_and_reasoning**: Actor is immutable record but IReadOnlySet may wrap externally mutable collection; policy reads active+permissions each call, no implicit command role grant. Actual session actor constructed from database. Record itself validates no role/permission strings.

**significant_methods**: Actor record constructor/properties

**interactions_and_risks**: Actor is immutable record but IReadOnlySet may wrap externally mutable collection; policy reads active+permissions each call, no implicit command role grant. Actual session actor constructed from database. Record itself validates no role/permission strings.

**findings**: []

**evidence**: Full bounded source read and cross-path reasoning, no runtime/user data mutation.

**recommendations**: Coordinate approval/role/PIN session incidental leads with dedicated Security coordinator; add concurrency and malformed-state regressions only after approved design.

**uncertainties**: No runtime reproduction. Endpoint authorization/rate limits owned root. No Roslyn semantic coverage claim.
