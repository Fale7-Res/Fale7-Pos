# tests/Falah.Server.IntegrationTests/OwnerDashboardVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/OwnerDashboardVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: ac90773e9c322596df77b62f2f257be17f16c11b63d7afb8aa9faf76732ed0e6

**git_blob_sha256**: dafabe15f75a8e81a9095b9825896d0ce5c5e0c818aa7fd9736209f8671602e7

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 207]]

**functionality_and_reasoning**: Verifies headline equality to sales, unknown-cost nulls, week comparator, zero-base ratio, chart bucket sums, current treasury ignoring historic period, open blockers and no-backup health. All sales use cash; no mixed pending FIN-010 case. No inventory stock fixture exercises FIN-013 unsigned dashboard balance. Comparing dashboard to same report can share report bug and is not independent accounting oracle.

**significant_methods**: InitializeAsync; DisposeAsync; SellAsync; DashboardAsync; Today; dashboard facts

**interactions_and_risks**: Verifies headline equality to sales, unknown-cost nulls, week comparator, zero-base ratio, chart bucket sums, current treasury ignoring historic period, open blockers and no-backup health. All sales use cash; no mixed pending FIN-010 case. No inventory stock fixture exercises FIN-013 unsigned dashboard balance. Comparing dashboard to same report can share report bug and is not independent accounting oracle.

**findings**: ["FIN-010", "FIN-013"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 207 lines read; no runtime writes or test invocation.

**recommendations**: Add positive-magnitude Out stock and low-stock parity with independent signed expectation; add pending split cash collection and report conservation.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
