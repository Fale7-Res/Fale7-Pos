# src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs

**path**: src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 1ebd4439f52e582f2e890c0db9bbd9f0c7e3e406b8ed56e46f2da900229083b3

**git_blob_sha256**: 1ebd4439f52e582f2e890c0db9bbd9f0c7e3e406b8ed56e46f2da900229083b3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 77]]

**functionality_and_reasoning**: Full77 lines read. Caller controls ExpectedPiastres and arbitrary denomination values; total recomputed with negative quantities clamped, not denomination whitelist. Saved count immutable by ID/replay, concurrentPK catch recovers. No actor/ref binding for reused countID across day, returns earlier record. ShiftId not validated existing/day. Any closed day allowed finalization.

**significant_methods**: Constructor; HandleAsync; ToDto

**interactions_and_risks**: Full77 lines read. Caller controls ExpectedPiastres and arbitrary denomination values; total recomputed with negative quantities clamped, not denomination whitelist. Saved count immutable by ID/replay, concurrentPK catch recovers. No actor/ref binding for reused countID across day, returns earlier record. ShiftId not validated existing/day. Any closed day allowed finalization.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared Domain CalculateDenominationCount and DB model no FK for shift/day counts.

**recommendations**: Validate denomination catalog and recompute authoritative expected if used for business close; bind countID to actor/day/shift, preserve human physical count distinction.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
