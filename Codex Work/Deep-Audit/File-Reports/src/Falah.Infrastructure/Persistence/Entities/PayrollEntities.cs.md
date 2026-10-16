# src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 2a827c742cb0c078ae436c936830f9e77d3f1f1c2cc58c9f473988dea2c9ec04

**git_blob_sha256**: 2a827c742cb0c078ae436c936830f9e77d3f1f1c2cc58c9f473988dea2c9ec04

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 40]]

**functionality_and_reasoning**: Run status/payout metadata independently mutable without concurrency Version or structural paid invariant; lines have no FK navigation in entity and context does not configure run/employee FK, so orphan line possible from external DB writes. Financial terminal status consistency must be command-enforced; generic handler omits it. No independent entity validation.

**significant_methods**: PayrollRunEntity properties; PayrollLineEntity properties

**interactions_and_risks**: Run status/payout metadata independently mutable without concurrency Version or structural paid invariant; lines have no FK navigation in entity and context does not configure run/employee FK, so orphan line possible from external DB writes. Financial terminal status consistency must be command-enforced; generic handler omits it. No independent entity validation.

**findings**: ["FIN-006"]

**evidence**: Full bounded source read and explicit cross-path reasoning, not executed.

**recommendations**: Add described distinct-operation/terminal-state/operational-shift regression coverage after approval.

**uncertainties**: No runtime reproduction. No claim of declaration semantic coverage.
