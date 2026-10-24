# tests/Falah.Domain.Tests/AuthorizationTests.cs

**path**: tests/Falah.Domain.Tests/AuthorizationTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: c6adc8228a709af6d220b4c1c02cbc35679d02dcd3c76f2709a17501b3bd62ed

**git_blob_sha256**: c6adc8228a709af6d220b4c1c02cbc35679d02dcd3c76f2709a17501b3bd62ed

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 165]]

**functionality_and_reasoning**: Full165 lines read. Active/all/permissionfallback/actionbinding/singleuse, rolelabelnotshiftpermission, supplier OR+fallback and blankapprovalaction. Managerapproval tests manager alwaysall; never manager stripped action then issuingapproval or creator role escalation. No expiry/revocation/targetamount binding (not implemented contract). In-memory single-thread only.

**significant_methods**: MakeActor; all facts

**interactions_and_risks**: Full165 lines read. Active/all/permissionfallback/actionbinding/singleuse, rolelabelnotshiftpermission, supplier OR+fallback and blankapprovalaction. Managerapproval tests manager alwaysall; never manager stripped action then issuingapproval or creator role escalation. No expiry/revocation/targetamount binding (not implemented contract). In-memory single-thread only.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Sent incidental leads to root Security coordinator, no new duplicate scan performed.

**recommendations**: Add roleissuer grant boundaries and persisted consumption race policy tests after securitydesign approved.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
