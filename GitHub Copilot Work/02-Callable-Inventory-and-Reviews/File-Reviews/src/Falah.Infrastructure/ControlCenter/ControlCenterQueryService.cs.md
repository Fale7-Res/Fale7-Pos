# src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs

**path**: src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 2ba6e827c2db885018094250fe907c5d78dd87c3d7112513d3624b92bcadfc15

**git_blob_sha256**: 2ba6e827c2db885018094250fe907c5d78dd87c3d7112513d3624b92bcadfc15

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 142]]

**functionality_and_reasoning**: Full142 lines read. Live derived issues + persisted sameID status overlay can hide issue changed magnitude after resolution. Latest authoritative snapshot ordering doesn't fix multiple authoritative rows created race. Checklist unknown day returns active list false vs GetDay throws. Global unsettled checks intentional. Audit limit no cap. JSON deserialization unsafe malformed persisted content =>whole query fail. Cash counts live list no snapshot tie.

**significant_methods**: Constructor; ListBusinessDaysAsync; GetBusinessDayAsync; PreviewCloseBlockersAsync; GetLatestCloseSnapshotAsync; ListCloseSnapshotsAsync; GetChecklistAsync; GetExceptionsAsync; ListAuditEventsAsync; ListCashCountsAsync

**interactions_and_risks**: Full142 lines read. Live derived issues + persisted sameID status overlay can hide issue changed magnitude after resolution. Latest authoritative snapshot ordering doesn't fix multiple authoritative rows created race. Checklist unknown day returns active list false vs GetDay throws. Global unsettled checks intentional. Audit limit no cap. JSON deserialization unsafe malformed persisted content =>whole query fail. Cash counts live list no snapshot tie.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared FIN-009 and close handler, no runtime data.

**recommendations**: Version issue condition/acknowledgment, invariant unique authoritative revision, pagination and consistent notfound/filter end boundaries.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
