# src/Falah.Infrastructure/Persistence/Migrations/20260904001314_AddOrderDiscountSnapshot.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260904001314_AddOrderDiscountSnapshot.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: e131b7e88d5e6fb7c308e8a02b8fedb25e82401e504d19c00c4fdc4a226f5650

**git_blob_sha256**: 9c6d3e2e51a31ba6e26da086ff2e89d11cdd9015903f5d702be5b1cea1b62ec3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 29]]

**functionality_and_reasoning**: Adds nonnullable DiscountPiastres default zero. Existing order totals/items remain unchanged, but no legacy discount inference occurs, so historical explicit discount reporting depends on predecessor schema data. Down drops snapshot without reconstructing amount.

**significant_methods**: Up; Down

**interactions_and_risks**: Adds nonnullable DiscountPiastres default zero. Existing order totals/items remain unchanged, but no legacy discount inference occurs, so historical explicit discount reporting depends on predecessor schema data. Down drops snapshot without reconstructing amount.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Entire small migration inspected; no evidence that legacy discounts existed, so no confirmed loss claim.

**recommendations**: Test upgrade fixtures with historical orders and document zero-default historical interpretation.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
