# tests/Falah.Domain.Tests/FinancialJournalTests.cs

**path**: tests/Falah.Domain.Tests/FinancialJournalTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 81a724078d5fb70e02097245e8590c74d425d2a0ea11cd875f778117b75baeee

**git_blob_sha256**: 81a724078d5fb70e02097245e8590c74d425d2a0ea11cd875f778117b75baeee

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 95]]

**functionality_and_reasoning**: Full95 lines read. Sequential duplicate start/complete/discard invariants in unsynchronized in-memory dictionary. Title same-task doubleclick is sequential, not parallel or database transaction/restart/failed commit. Interfacecontract tests cannot prove SQL dedup or in-flight visibility for restore. No payload identity checks.

**significant_methods**: All facts

**interactions_and_risks**: Full95 lines read. Sequential duplicate start/complete/discard invariants in unsynchronized in-memory dictionary. Title same-task doubleclick is sequential, not parallel or database transaction/restart/failed commit. Interfacecontract tests cannot prove SQL dedup or in-flight visibility for restore. No payload identity checks.

**findings**: ["FIN-008"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared EF journal and restore gate, no execution.

**recommendations**: Use safe isolatedSQL tests only with approved DB setup for race/durability; puretests limited to reference contract.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
