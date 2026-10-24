# tests/Falah.Domain.Tests/OrderPaymentQueriesTests.cs

**path**: tests/Falah.Domain.Tests/OrderPaymentQueriesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 6d4efbe18d40151ca25104a3b21f2f285a4f4e46085ae31bc89d35a8fdfb9609

**git_blob_sha256**: 6d4efbe18d40151ca25104a3b21f2f285a4f4e46085ae31bc89d35a8fdfb9609

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 64]]

**functionality_and_reasoning**: Tests mixed cash recognized while electronic pending; never cancels mixed order. Aggregate direct confirm omits EF journal and misses FIN-001. Refund case same shift only, no operational-period reconciliation.

**significant_methods**: Item; CashOrder; all facts

**interactions_and_risks**: Tests mixed cash recognized while electronic pending; never cancels mixed order. Aggregate direct confirm omits EF journal and misses FIN-001. Refund case same shift only, no operational-period reconciliation.

**findings**: ["FIN-001", "FIN-002", "FIN-004"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
