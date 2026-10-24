# src/Falah.Domain/ControlCenter/ControlCenterRules.cs

**path**: src/Falah.Domain/ControlCenter/ControlCenterRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 01d20a713e2ef6020319d0f4341bee09d77b57711f4a51a7c9b8e78b5e847652

**git_blob_sha256**: 01d20a713e2ef6020319d0f4341bee09d77b57711f4a51a7c9b8e78b5e847652

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 86]]

**functionality_and_reasoning**: Full86 lines read. Derived exceptions overlay persisted statuses external. Shift severity >=threshold while authorization >threshold makes exact threshold critical but permitted no approval. Math.Abs longMin overflow. Delivery failed/returned uses raw English state in Arabic message, older docs says no backups despite actual service. User resolved state can mask changing issue with same deterministic ID unless versioned.

**significant_methods**: BuildExceptions and deterministic loops; thresholds

**interactions_and_risks**: Full86 lines read. Derived exceptions overlay persisted statuses external. Shift severity >=threshold while authorization >threshold makes exact threshold critical but permitted no approval. Math.Abs longMin overflow. Delivery failed/returned uses raw English state in Arabic message, older docs says no backups despite actual service. User resolved state can mask changing issue with same deterministic ID unless versioned.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared close threshold logic and source delivery status changes.

**recommendations**: Use common threshold semantics and versioned issue condition identity; test equality boundary and reemergence after resolved.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
