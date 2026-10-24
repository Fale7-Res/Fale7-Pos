# src/Falah.Domain/Financial/FinancialRules.cs

**path**: src/Falah.Domain/Financial/FinancialRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 0c48f0fef3792f1bd9e85023ad85978ae8583f9f7d2e2bcaa65f3159aef1b926

**git_blob_sha256**: 0c48f0fef3792f1bd9e85023ad85978ae8583f9f7d2e2bcaa65f3159aef1b926

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 58]]

**functionality_and_reasoning**: Split count/positive/unique/exact sum enforced; allowed method set and max-one electronic absent. Protected account balance is pooled and cannot prove collection for specific order. Discrepancy uses strict greater-than threshold. Sequence helper differs from actual SQL allocator recovery behavior.

**significant_methods**: AssertSplitPaymentAllocation; AllocateNextOrderSequence; FormatOrderNumber; TreasuryBalanceFromMovements; CalculateExpectedDrawerCash; DiscrepancyNeedsApproval; AssertProtectedSourceBalance

**interactions_and_risks**: Split count/positive/unique/exact sum enforced; allowed method set and max-one electronic absent. Protected account balance is pooled and cannot prove collection for specific order. Discrepancy uses strict greater-than threshold. Sequence helper differs from actual SQL allocator recovery behavior.

**findings**: ["FIN-003"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
