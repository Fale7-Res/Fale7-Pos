# tests/Falah.Domain.Tests/CheckoutRulesTests.cs

**path**: tests/Falah.Domain.Tests/CheckoutRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 56771c9ce0561a61130f04b64108b6c6078917a8c3751caee816352a6fefbf4a

**git_blob_sha256**: 8df53d0c959e9a8b53bbf1d452644d6efff410c55a9e6c7122adc5ce986bce2c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 181]]

**functionality_and_reasoning**: Tests normal cash/COD/comp/single-electronic/tax/pickup rules and expressly assert mixed split does not defer all. No EF journal-confirm composition; negative fee/discount, multiple electronic methods and overflow not covered. Inspected only, not executed.

**significant_methods**: Item; all facts

**interactions_and_risks**: Tests normal cash/COD/comp/single-electronic/tax/pickup rules and expressly assert mixed split does not defer all. No EF journal-confirm composition; negative fee/discount, multiple electronic methods and overflow not covered. Inspected only, not executed.

**findings**: ["FIN-001"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
