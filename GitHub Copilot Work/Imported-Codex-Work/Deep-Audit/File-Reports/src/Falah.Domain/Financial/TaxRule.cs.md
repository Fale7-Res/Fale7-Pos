# src/Falah.Domain/Financial/TaxRule.cs

**path**: src/Falah.Domain/Financial/TaxRule.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: baf652da91a01c0587bc5d6038980178509dc41c9c95578e20dc7551f1681766

**git_blob_sha256**: baf652da91a01c0587bc5d6038980178509dc41c9c95578e20dc7551f1681766

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 30]]

**functionality_and_reasoning**: Inclusive/exclusive decimal intermediate tax; disabled/nonpositive rate means none. Math.Round defaults ToEven unlike Money.FromEgp AwayFromZero. Half-piastre rounding policy needs business requirement, not assumed defect. No rate cap in this pure function.

**significant_methods**: TaxEngine.Calculate

**interactions_and_risks**: Inclusive/exclusive decimal intermediate tax; disabled/nonpositive rate means none. Math.Round defaults ToEven unlike Money.FromEgp AwayFromZero. Half-piastre rounding policy needs business requirement, not assumed defect. No rate cap in this pure function.

**findings**: []

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
