# tests/Falah.Domain.Tests/OrderReconstituteTests.cs

**path**: tests/Falah.Domain.Tests/OrderReconstituteTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 86818b18e449dc9393f7a219a9fa1b92069a044270a8f3e22c4931be616b93f9

**git_blob_sha256**: 86818b18e449dc9393f7a219a9fa1b92069a044270a8f3e22c4931be616b93f9

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 67]]

**functionality_and_reasoning**: Full67 lines read. Fact claims preserves everypersistedfield but onlyassertsId/Total/Status/IsPaid/RefundAmount, helper omits cancellation/manualdiscount/taxsnapshot optional fields. Refund invariants and prior partial refund preserved. No mapper or database roundtrip, original itemGUID-vsDB identity not tested.

**significant_methods**: Item; FreshOrder; Reconstitute; all facts

**interactions_and_risks**: Full67 lines read. Fact claims preserves everypersistedfield but onlyassertsId/Total/Status/IsPaid/RefundAmount, helper omits cancellation/manualdiscount/taxsnapshot optional fields. Refund invariants and prior partial refund preserved. No mapper or database roundtrip, original itemGUID-vsDB identity not tested.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared OrderMapper field mapping, no runtime.

**recommendations**: Data-driven allfield roundtrip incldiscount/tax/comp/customer/leg/splits/cancel/childidentity and loadedcollection requirements.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
