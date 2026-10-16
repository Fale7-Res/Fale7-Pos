# src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs

**path**: src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: c3665f0dc54809b0f822fc8152dd8dd7d610ab7318a84bdbacd51ab974a09573

**git_blob_sha256**: 7238391cfaa2eff9365366d4217d107559d0f5b307c428b5270e73a72db66729

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 162]]

**functionality_and_reasoning**: Full162 lines read. Stable employee row then drawer lock then ledger lock; withdrawal within employee balance, advance can exceed balance intentionally. Movement+ledger+audit+journal atomic, shift open read no update lock so close race persists. Lock order drawer->ledger after employee differs CorrectAttendance ledger-only and PayEmployee ledger->main; cross-command deadlock possible, contention handler maps only1205/1222. Rejects kind/reason/positive amount.

**significant_methods**: Constructor; HandleAsync

**interactions_and_risks**: Full162 lines read. Stable employee row then drawer lock then ledger lock; withdrawal within employee balance, advance can exceed balance intentionally. Movement+ledger+audit+journal atomic, shift open read no update lock so close race persists. Lock order drawer->ledger after employee differs CorrectAttendance ledger-only and PayEmployee ledger->main; cross-command deadlock possible, contention handler maps only1205/1222. Rejects kind/reason/positive amount.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Read complete file from nontruncated region, compared PayEmployee; no payout executed.

**recommendations**: Standardize lock order and shift gate under lock; test cross-flow payout/correction/close; preserve advance vs earned withdrawal distinction.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
