# src/Falah.Infrastructure/Treasury/BalanceLockContention.cs

**path**: src/Falah.Infrastructure/Treasury/BalanceLockContention.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d982d3fadbec4f7dd42d41465f63c8c9f41fe25cf112374025bc450c3183d690

**git_blob_sha256**: a480d0b55f892a72df9ce61870b446281c962351a1b4b1fed51e077de393807a

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 61]]

**functionality_and_reasoning**: Full61 lines read. Walk inner SQL exception, maps1205/1222, 5 bounded delays sum600ms +lookups; comment about same key contention overstates genuine distinct operations can contend. Db transaction deadlock auto rollback and subsequent explicit Rollback may fail provider state; not runtime verified. No retry of command itself, caller instructed same request retry correctly.

**significant_methods**: IsContention; Sql; AwaitSiblingAsync; RetryMessage

**interactions_and_risks**: Full61 lines read. Walk inner SQL exception, maps1205/1222, 5 bounded delays sum600ms +lookups; comment about same key contention overstates genuine distinct operations can contend. Db transaction deadlock auto rollback and subsequent explicit Rollback may fail provider state; not runtime verified. No retry of command itself, caller instructed same request retry correctly.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared catch blocks and no transaction in OpenShift precheck.

**recommendations**: Test deadlock victim rollback cleanup/provider behavior and same-key eventual visibility with deterministic barriers, preserve distinct-operation conflict vs replay.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
