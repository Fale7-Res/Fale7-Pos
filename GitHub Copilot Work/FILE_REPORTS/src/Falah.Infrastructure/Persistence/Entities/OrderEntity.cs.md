# src/Falah.Infrastructure/Persistence/Entities/OrderEntity.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/OrderEntity.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: ba6c9d14be3afbfc4edbc48185619506d82c6db595324a2db198b9c4e3f093ab

**git_blob_sha256**: cc4a7cd33c034e286755a1e79a3caf279b640e1aa6d2e5f6d2ccfd9bd0435652

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 131]]

**functionality_and_reasoning**: Scalar order snapshot and child storage. Manual Version requires callers increment. Domain item string ID absent in persistence. Split JSON plus single electronic leg can represent inconsistent multi-electronic state. OperationalShiftId nonnullable although refund policy permits null noncash historical. No independent entity invariants.

**significant_methods**: OrderEntity; OrderItemEntity; OrderRefundEntity property declarations

**interactions_and_risks**: Scalar order snapshot and child storage. Manual Version requires callers increment. Domain item string ID absent in persistence. Split JSON plus single electronic leg can represent inconsistent multi-electronic state. OperationalShiftId nonnullable although refund policy permits null noncash historical. No independent entity invariants.

**findings**: ["FIN-004"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
