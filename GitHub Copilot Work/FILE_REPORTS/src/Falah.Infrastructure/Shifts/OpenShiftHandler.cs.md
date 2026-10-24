# src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs

**path**: src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 7b2afcf63d01e1bb55c0d88e7d1d64e55b3014539d51dd7f4ed9664e7e8b5ea3

**git_blob_sha256**: 7b2afcf63d01e1bb55c0d88e7d1d64e55b3014539d51dd7f4ed9664e7e8b5ea3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 114]]

**functionality_and_reasoning**: Full114 lines read. Permission and singleOpen filtered index protect duplicate shifts. Main balance loader UPDLOCK called before BeginTransaction, so lock not held through actual float write; competing Main withdrawal can overdraw account. Zero float no business-day gate deliberately permits sales with null day. Creates opening slip atomic; replay checked only precheck and after DB conflict. Name not required.

**significant_methods**: Constructor; HandleAsync; ToResult

**interactions_and_risks**: Full114 lines read. Permission and singleOpen filtered index protect duplicate shifts. Main balance loader UPDLOCK called before BeginTransaction, so lock not held through actual float write; competing Main withdrawal can overdraw account. Zero float no business-day gate deliberately permits sales with null day. Creates opening slip atomic; replay checked only precheck and after DB conflict. Name not required.

**findings**: ["FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Static exact ordering balance53-55 then BeginTransaction71 confirms FIN-011 candidate; no runtime reproduction.

**recommendations**: Acquire balance in same transaction, stable lock order with shift singleton; test shift float vs owner withdrawal distinct concurrent commands, preserve source nonnegative.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
