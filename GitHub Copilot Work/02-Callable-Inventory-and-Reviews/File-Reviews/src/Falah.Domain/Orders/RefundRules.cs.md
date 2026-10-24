# src/Falah.Domain/Orders/RefundRules.cs

**path**: src/Falah.Domain/Orders/RefundRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 10bf926d5ad29fcc2d25b98ce32e523ae76769ae077bbd0d659b59443501feb9

**git_blob_sha256**: 10bf926d5ad29fcc2d25b98ce32e523ae76769ae077bbd0d659b59443501feb9

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 43]]

**functionality_and_reasoning**: Over-request clamps to remaining intentionally. Closed cash original requires active shift; closed noncash can return null OperationalShiftId despite nonnullable persistence field. No paid-state input used. Old/current shift policy must be honored by reconciliation.

**significant_methods**: RefundableRemaining; ClampRefundAmount; AssertOrderCanBeCancelled; ResolveOperationalShift

**interactions_and_risks**: Over-request clamps to remaining intentionally. Closed cash original requires active shift; closed noncash can return null OperationalShiftId despite nonnullable persistence field. No paid-state input used. Old/current shift policy must be honored by reconciliation.

**findings**: ["FIN-003", "FIN-004"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
