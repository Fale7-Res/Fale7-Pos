# src/Falah.Infrastructure/Persistence/Migrations/20260903143733_AddShiftCloseIdempotency.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260903143733_AddShiftCloseIdempotency.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 706b113b8bdd32af04959e0e8035ca49914e6396e129506dcf1e0ca3ad6d5be5

**git_blob_sha256**: a969ceaf461eb6b1fe372bb70fa75e053684a19e142550bdde45aa65af1f0b9a

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 59]]

**functionality_and_reasoning**: Adds nullable close request and two treasury movement reference strings. Filtered unique close key allows preexisting null rows and prevents duplicate nonnull requests, but movement IDs are unconstrained strings and transaction integrity remains handler responsibility. Down removes replay evidence and movement references.

**significant_methods**: Up; Down

**interactions_and_risks**: Adds nullable close request and two treasury movement reference strings. Filtered unique close key allows preexisting null rows and prevents duplicate nonnull requests, but movement IDs are unconstrained strings and transaction integrity remains handler responsibility. Down removes replay evidence and movement references.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Entire Up/Down read; no migration executed.

**recommendations**: Verify close replay and rollback data-loss plan on disposable upgrade fixtures.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
