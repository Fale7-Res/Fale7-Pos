# src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs

**path**: src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: ac7a5a9aefba464dfb2444c86d6a5bb22149455b2f82856f3fea4971931ff941

**git_blob_sha256**: 59acb00e831bcbaaac53ebf3ddeff7ffaef460a4702da93c71a580f173cdcf45

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 97]]

**functionality_and_reasoning**: Full97 lines read. Deterministic request+route jobs with replay from ANY CreateRequestId without order/actor binding. Actual actor exists only, host policy needed. Kernel/kitchen routes from frozen station correct, no hold setting/paymentstate guard on explicit request. Whole batch Save atomic, reprint audit names real actor. SameRequest change routes returns earlier partial batch (e.g. auto receipt-only held prep cannot later append under same ID).

**significant_methods**: Constructor; HandleAsync; ToDto

**interactions_and_risks**: Full97 lines read. Deterministic request+route jobs with replay from ANY CreateRequestId without order/actor binding. Actual actor exists only, host policy needed. Kernel/kitchen routes from frozen station correct, no hold setting/paymentstate guard on explicit request. Whole batch Save atomic, reprint audit names real actor. SameRequest change routes returns earlier partial batch (e.g. auto receipt-only held prep cannot later append under same ID).

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared OrderAutoPrint afterconfirm/manual behavior and PRT001 distributed consume.

**recommendations**: Bind idempotency payload/order/actor, durable outbox/desired document identity; enforce policy on server explicit kitchen request if paymenthold actually required.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
