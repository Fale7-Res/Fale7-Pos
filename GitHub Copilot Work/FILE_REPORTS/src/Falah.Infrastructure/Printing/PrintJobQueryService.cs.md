# src/Falah.Infrastructure/Printing/PrintJobQueryService.cs

**path**: src/Falah.Infrastructure/Printing/PrintJobQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: be7e9b0dab28dd16af51b952bd4ed000875ca04d6a9b61fa357dc55a0f2e304c

**git_blob_sha256**: be7e9b0dab28dd16af51b952bd4ed000875ca04d6a9b61fa357dc55a0f2e304c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 346]]

**functionality_and_reasoning**: Full346 lines read. Pending GET is unclaimed AsNoTracking, so multi-host duplicate send PRT-001 independently confirmed. Content recomposed from frozen order items but current settings/cashier names and mutable payment state; closed shift totals persisted but explanatory breakdown recomputed after historical refunds, potentially contradicts totals. Driver name uses first later reassignment history to preserve assignment identity, timestamps may tie. COD amount uses Total-AmountPaid not actual settlement; pickup info preserved.

**significant_methods**: Constructor; ListJobsForOrderAsync; ListPendingJobsForRouteAsync; GetOverviewAsync; GetJobDetailsAsync; ComposeTicketContentAsync; ToDto; MarkIfReprint; LoadShiftIdentityAsync; ShiftReference; ComposeShiftOpenSlipAsync; ComposeShiftCloseSlipAsync; LoadCloseWarningsAsync; ComposeExpenseSlipAsync; DescribePaymentAccount; ComposeReceiptAsync; ComposeDeliveryOrderAsync; ComposeDriverDispatchAsync

**interactions_and_risks**: Full346 lines read. Pending GET is unclaimed AsNoTracking, so multi-host duplicate send PRT-001 independently confirmed. Content recomposed from frozen order items but current settings/cashier names and mutable payment state; closed shift totals persisted but explanatory breakdown recomputed after historical refunds, potentially contradicts totals. Driver name uses first later reassignment history to preserve assignment identity, timestamps may tie. COD amount uses Total-AmountPaid not actual settlement; pickup info preserved.

**findings**: ["FIN-004"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Independent PRT-001 challenge traced PrintAgent send/report/local mutex; no physical transport invoked. FIN-004 explains historical breakdown change.

**recommendations**: Coordinate distributed print lease with print reviewer; freeze explanatory shift cash breakdown alongside total and assignment-specific dispatch data.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
