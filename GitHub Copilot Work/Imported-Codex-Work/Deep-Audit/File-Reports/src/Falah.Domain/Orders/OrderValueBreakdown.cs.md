# src/Falah.Domain/Orders/OrderValueBreakdown.cs

**path**: src/Falah.Domain/Orders/OrderValueBreakdown.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 4c7f490f4226a5b1e431a26a065ec2a84b283e7b4ee0f2e1232f1ae3dfb5a6b9

**git_blob_sha256**: 4c7f490f4226a5b1e431a26a065ec2a84b283e7b4ee0f2e1232f1ae3dfb5a6b9

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 22]]

**functionality_and_reasoning**: List minus charged is offer, comp floored, collected only charged items less comp. Does not incorporate manual discount, delivery, tax or actual settlement; consumers must not treat as actual collected amount. Unchecked multiplication remains; no consumer claim inferred.

**significant_methods**: Compute

**interactions_and_risks**: List minus charged is offer, comp floored, collected only charged items less comp. Does not incorporate manual discount, delivery, tax or actual settlement; consumers must not treat as actual collected amount. Unchecked multiplication remains; no consumer claim inferred.

**findings**: []

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
