# src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs

**path**: src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 0a7282c6766d2a7b446739d56984950d87ab69f52f88dd3be88f27609484a542

**git_blob_sha256**: 2f1bb87e4d9f442dbd11ade3b43b2955967ff5ac8971265bc7125cd1f87b04f2

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 95]]

**functionality_and_reasoning**: Full95 lines read. After financial commit auto print in separate save catches all including cancellation, returnsfalse; crash between commit/queue leaves no durable retry unless checkout repeated or manual. Hold prep defaultfalse, receipt prints pending; when confirmed comment requires manual OrderLookup send, no auto resume. Stable autoRequest ID can already contain receipt-only, replay cannot add kitchen afterconfirm. NumberOfReceiptCopies setting unused in service.

**significant_methods**: Constructor; TryPrintAsync

**interactions_and_risks**: Full95 lines read. After financial commit auto print in separate save catches all including cancellation, returnsfalse; crash between commit/queue leaves no durable retry unless checkout repeated or manual. Hold prep defaultfalse, receipt prints pending; when confirmed comment requires manual OrderLookup send, no auto resume. Stable autoRequest ID can already contain receipt-only, replay cannot add kitchen afterconfirm. NumberOfReceiptCopies setting unused in service.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared CreatePrintJobs and checkout no physicalsend, printing agent owns findings.

**recommendations**: Transactional durable print intent/outbox without printer in money transaction; dedicated confirmation event queues pending kitchen exactly once.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
