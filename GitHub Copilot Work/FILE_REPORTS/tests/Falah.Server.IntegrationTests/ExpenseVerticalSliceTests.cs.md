# tests/Falah.Server.IntegrationTests/ExpenseVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/ExpenseVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: baa07522fd8dd1dfb0f2e1dd6961482351d90dc36fa51bbe85f522aa510a314c

**git_blob_sha256**: b8f5cea4e051974133ab07b31a12960fc368d9ea526c99d6dda0ac4885334d16

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 247]]

**functionality_and_reasoning**: Tests direct drawer/Main authority versus restricted approval fallback, open shift/day, overbalance and replay, expense visibility and reconciliation. Zero-float/no-day test also lacksdrawer funds so422does not isolate daygate. Own-shift visibility comment claims foreignshift isolation but no foreignshift record seeded. Concurrent samekey ensures oneExpense and onefirstresponse, not fullmoney row count. Cairodate query can crossmidnight.

**significant_methods**: All34actualcallable spans individually reasoned

**interactions_and_risks**: Tests direct drawer/Main authority versus restricted approval fallback, open shift/day, overbalance and replay, expense visibility and reconciliation. Zero-float/no-day test also lacksdrawer funds so422does not isolate daygate. Own-shift visibility comment claims foreignshift isolation but no foreignshift record seeded. Concurrent samekey ensures oneExpense and onefirstresponse, not fullmoney row count. Cairodate query can crossmidnight.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 247lines read;34exactcallable receipts saved; noexecution.

**recommendations**: Add orthogonal precondition fixtures, foreignshift row, exact movement conservation and deniedoperation state snapshots.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
