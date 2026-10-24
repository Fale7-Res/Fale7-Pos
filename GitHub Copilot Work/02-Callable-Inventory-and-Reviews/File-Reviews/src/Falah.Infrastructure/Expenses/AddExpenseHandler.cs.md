# src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs

**path**: src/Falah.Infrastructure/Expenses/AddExpenseHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 02f51c8d0598d6772894850b3f02638a5cf57769d75033a62d78a9cf7a69f3a1

**git_blob_sha256**: e2ce5bb7df5972324dd3c67c6098f7dc33554fe3126860500b6f50e0a3781b5d

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 156]]

**functionality_and_reasoning**: Full156 lines reread complete after batch truncation. Account whitelist protected (Wallet excluded), drawer permission fallback vs main dedicated permission, source lock insidetransaction. All expenses require open shift even Main bank expense not drawer. Day ID resolves active but BusinessDate uses currentCairo, if opened custom historical date differs. ShiftOpen precheck races close. Expense/movement/journal/audit/voucher atomic on normal save. No physical printer call.

**significant_methods**: Constructor; HandleAsync; ToResult

**interactions_and_risks**: Full156 lines reread complete after batch truncation. Account whitelist protected (Wallet excluded), drawer permission fallback vs main dedicated permission, source lock insidetransaction. All expenses require open shift even Main bank expense not drawer. Day ID resolves active but BusinessDate uses currentCairo, if opened custom historical date differs. ShiftOpen precheck races close. Expense/movement/journal/audit/voucher atomic on normal save. No physical printer call.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared lifetime refund/day scoping and safe dedicated account locking.

**recommendations**: Align operational date policy, serialize drawer expense with close; optional no-shift main expenses only after business approval. Preserve atomic financial+audit and independent physical print.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
