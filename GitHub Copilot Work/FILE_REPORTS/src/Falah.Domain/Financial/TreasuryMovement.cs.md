# src/Falah.Domain/Financial/TreasuryMovement.cs

**path**: src/Falah.Domain/Financial/TreasuryMovement.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 89ef6f7f2c32e04dd844670ebae972518ab598feb6a8540f26ba1681c9664f87

**git_blob_sha256**: 89ef6f7f2c32e04dd844670ebae972518ab598feb6a8540f26ba1681c9664f87

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 104]]

**functionality_and_reasoning**: Movement factories determine channel accounts and idempotency strings. Positive amount guard delegated caller; Multi rejected by mapping. Day/shift supplied by caller, so original-day refund attribution persists. Electronic collection represents operator confirmation, no processor evidence.

**significant_methods**: PaymentAccountFor; BuildSaleMovement; BuildRefundMovement; BuildCashLifecycleMovement

**interactions_and_risks**: Movement factories determine channel accounts and idempotency strings. Positive amount guard delegated caller; Multi rejected by mapping. Day/shift supplied by caller, so original-day refund attribution persists. Electronic collection represents operator confirmation, no processor evidence.

**findings**: ["FIN-003", "FIN-004"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
