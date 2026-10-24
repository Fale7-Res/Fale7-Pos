# src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs

**path**: src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: d67147ee1cc8cdb1e560710faf935f637028042a5ab903863498bbbee8a973c7

**git_blob_sha256**: d67147ee1cc8cdb1e560710faf935f637028042a5ab903863498bbbee8a973c7

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 44]]

**functionality_and_reasoning**: Full44 lines read. AmountToCollect always total-refunds independent of AmountPaid and electronic settled state; DeliveryCashResponsibility zeroes noncash already paid. Mixed pending electronic responsibility becomes whole total despite immediate cash leg, enabling overcollection if dispatched. Order.IsPaid never flips for COD by design. Enum parse assumes valid stored statuses.

**significant_methods**: Compose; ToDriverCustody

**interactions_and_risks**: Full44 lines read. AmountToCollect always total-refunds independent of AmountPaid and electronic settled state; DeliveryCashResponsibility zeroes noncash already paid. Mixed pending electronic responsibility becomes whole total despite immediate cash leg, enabling overcollection if dispatched. Order.IsPaid never flips for COD by design. Enum parse assumes valid stored statuses.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared mixed checkout and driver custody calculations.

**recommendations**: Resolve per-leg outstanding collection rather than whole total; test mixed cash/electronic delivery amount and transition when electronic confirms after assignment.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
