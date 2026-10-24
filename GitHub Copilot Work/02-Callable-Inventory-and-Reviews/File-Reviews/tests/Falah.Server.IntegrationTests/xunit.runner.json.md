# tests/Falah.Server.IntegrationTests/xunit.runner.json

**path**: tests/Falah.Server.IntegrationTests/xunit.runner.json

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 503bd8a8218c2dee7cf9eaf56cc856883b7eb213f19d6d0b94d0f26dec5aa705

**git_blob_sha256**: 503bd8a8218c2dee7cf9eaf56cc856883b7eb213f19d6d0b94d0f26dec5aa705

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 4]]

**functionality_and_reasoning**: Disables collection parallelism to reduce real SQL database pressure, preserving explicit Task.WhenAll races within tests. This affects suite scheduling only and does not make individual tests or production transactions serial.

**significant_methods**: parallelizeTestCollections

**interactions_and_risks**: Disables collection parallelism to reduce real SQL database pressure, preserving explicit Task.WhenAll races within tests. This affects suite scheduling only and does not make individual tests or production transactions serial.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Complete four lines read; schema URL not fetched.

**recommendations**: Retain targeted barriers for races rather than relying on incidental suite overlap.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
