# src/Falah.Infrastructure/Persistence/Entities/ControlCenterEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/ControlCenterEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 343aa348cbbdc5fa31f6aeae70ccce0cba1dace0ec0c020151a937a609c7f8af

**git_blob_sha256**: 343aa348cbbdc5fa31f6aeae70ccce0cba1dace0ec0c020151a937a609c7f8af

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 116]]

**functionality_and_reasoning**: Full116 lines read. Snapshot JSON archives good; revision/Authoritative property alone not constraint. Completion composite key unique but no FK mapping to day/item. Exception state no condition revision. ControlAudit docs stale says general log unwired despite EfAuditLog. Count Id sole key despite comment once per businessDayId/id, same ID crossday replays first. Generic mutable entities not DB immutability.

**significant_methods**: ChecklistItemEntity; ChecklistCompletionEntity; BusinessDayCloseSnapshotEntity; ControlExceptionStateEntity; ControlAuditEventEntity; CashDenominationCountEntity properties

**interactions_and_risks**: Full116 lines read. Snapshot JSON archives good; revision/Authoritative property alone not constraint. Completion composite key unique but no FK mapping to day/item. Exception state no condition revision. ControlAudit docs stale says general log unwired despite EfAuditLog. Count Id sole key despite comment once per businessDayId/id, same ID crossday replays first. Generic mutable entities not DB immutability.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared close/reopen/checklist/status/count handlers fully.

**recommendations**: Document enforced keys accurately; add consistent active/snapshot constraints only after reviewed migration design; bind condition identities.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
