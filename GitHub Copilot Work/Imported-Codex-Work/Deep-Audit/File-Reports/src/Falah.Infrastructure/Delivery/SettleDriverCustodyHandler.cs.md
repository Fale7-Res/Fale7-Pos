# src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs

**path**: src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 0fc0eb77d5f271e3f4e3b7add9257e55e5287702737f5fab84718e7283f4d46a

**git_blob_sha256**: 9d73b7f839304f8bd8a88300579e900569067df432ed41f1cb2b9edbed69ae5e

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 198]]

**functionality_and_reasoning**: Full198 lines read. Locks complete unsettled delivery/custody sets per driver, checks no active work then marks resolved all settled and posts actual cash to Main not drawer. Journal only if actual>0, separate settlement row always. Actual compared to recorded Collected not expected obligation. Prepaid no custody accepted. Does not update order IsPaid deliberately reporting resolves settled custody. Approval fallback one token cannot satisfy both base and variance actions if caller holds neither permission; high friction but explicit policy.

**significant_methods**: Constructor; HandleAsync; ToResult

**interactions_and_risks**: Full198 lines read. Locks complete unsettled delivery/custody sets per driver, checks no active work then marks resolved all settled and posts actual cash to Main not drawer. Journal only if actual>0, separate settlement row always. Actual compared to recorded Collected not expected obligation. Prepaid no custody accepted. Does not update order IsPaid deliberately reporting resolves settled custody. Approval fallback one token cannot satisfy both base and variance actions if caller holds neither permission; high friction but explicit policy.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared all delivery write paths and ReportingQueryService COD loader. No database tests.

**recommendations**: Test concurrent distinct settlement, reassign/cancel/confirm interaction and customer collection shortages separately. Maintain non-drawer attribution; no claim missing Order.IsPaid is bug.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
