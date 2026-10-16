# src/Falah.Domain/Employees/EmployeeRules.cs

**path**: src/Falah.Domain/Employees/EmployeeRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 8b25256a36f25b58aa3f61ddbaeee9f7bb32fed5e8ff522d71486b92b308d3b5

**git_blob_sha256**: 8b25256a36f25b58aa3f61ddbaeee9f7bb32fed5e8ff522d71486b92b308d3b5

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 50]]

**functionality_and_reasoning**: Full50 lines read. Required employee fields only nonblank, wage parameters not validated here. Ledger sum credit-debit no sign constraints. Withdrawal/payment bound checks do not enforce amountpositive themselves, handlers compose AssertLedgerAmount correctly. Negative existing balance blocks positive payout. Unchecked arithmetic potential extreme.

**significant_methods**: AssertEmployeeInput; CalculateBalance; AssertLedgerAmount; AssertWithdrawalWithinBalance; AssertPaymentWithinBalance

**interactions_and_risks**: Full50 lines read. Required employee fields only nonblank, wage parameters not validated here. Ledger sum credit-debit no sign constraints. Withdrawal/payment bound checks do not enforce amountpositive themselves, handlers compose AssertLedgerAmount correctly. Negative existing balance blocks positive payout. Unchecked arithmetic potential extreme.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared PayEmployee and manual ledger callers partly.

**recommendations**: Keep composed invariants together to avoid future caller skipping positivity; test negative/nonfinite quantities at boundary.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
