# src/Falah.Infrastructure/Treasury/DepositHandler.cs

**path**: src/Falah.Infrastructure/Treasury/DepositHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 2de4b3e873860e96e2f7e8b5608c416c9734a59ea804861ddf08dd1545fddd5d

**git_blob_sha256**: dfba025440ce808d136eeb67062234d592825fcb21de752549cb11fa7c591d63

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 113]]

**functionality_and_reasoning**: Full113 lines read. Allowed positive external capital types/accounts, reason and actor/approval, require day before transaction (day-close race). Journal/movement/audit atomic, no balance lock needed money in. Request key normalizes underscores, allowed types avoid actual collision. Replay ignores payload/destination changes by same request key. Catches any DbUpdateException as conflict hiding infrastructure error.

**significant_methods**: Constructor; HandleAsync; FindExistingAsync; ToResult

**interactions_and_risks**: Full113 lines read. Allowed positive external capital types/accounts, reason and actor/approval, require day before transaction (day-close race). Journal/movement/audit atomic, no balance lock needed money in. Request key normalizes underscores, allowed types avoid actual collision. Replay ignores payload/destination changes by same request key. Catches any DbUpdateException as conflict hiding infrastructure error.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared account validator and journal; no deposit performed.

**recommendations**: Document payload stable replay, day gate lease across writes, structured root exception logging.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
