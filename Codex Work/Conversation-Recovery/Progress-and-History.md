# Verified recovery history

Visible conversation and saved reports describe architecture discovery, Release solution build, isolated LocalDB initialization, real backend and client startup, authenticated API checks and file-only printing. These are historical runtime observations, not fresh audit executions. The existing backend is Falah.Server; no replacement backend is needed. CashierWinUI is the active cashier; CallCenterWinUI is optional. Domain/Application/Infrastructure implement business contracts and persistence. PrintAgent polls server jobs and transports/render tickets.

Earlier runtime work wrote one assistant test sale (EGP10), opened a test business day/shift and requested a reprint. The owner later used the isolated environment. Audit recovery performs no new financial operation. Do not assume later test records belong to the assistant.

Recovered 250 per-file reports: financial 71 complete static files; UI 106 source-reviewed plus 2 partial; printing 70 static plus 1 partial. This is 247 reported full-file source reviews and 3 partial, not a completed audit. Another 440 files remain without completed review, including 18 parent-owned files. Review receipts and current source hashes agree. Independent review and function-level reconciliation remain incomplete.

The Domain test attempt aborted before execution because testhost failed to connect within 90 seconds: executed 0, passed 0, failed test cases 0. This is a failed test infrastructure step, never a passing test run.

Financial and UI original agents returned saved checkpoints. Printing recovery hit an agent thread limit; no duplicate agent was created. All recovered files were copied, never moved. Thirteen artifact-root directories of unverified prior origin were preserved unchanged.
