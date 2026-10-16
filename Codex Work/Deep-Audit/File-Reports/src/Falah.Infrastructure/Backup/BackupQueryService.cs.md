# src/Falah.Infrastructure/Backup/BackupQueryService.cs

**path**: src/Falah.Infrastructure/Backup/BackupQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 8ffbc88eb64e21d8a95e1da6ba6da8ac4527acf174ef4c2f48b2dd3f97426370

**git_blob_sha256**: 8ffbc88eb64e21d8a95e1da6ba6da8ac4527acf174ef4c2f48b2dd3f97426370

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 23]]

**functionality_and_reasoning**: Full23 lines read. Read-only record DTO exposes absolute paths, SQL error text, timestamps and verification state, endpoint must gate. Unbounded backup history because prune preserves records; grows indefinitely. Missing record InvalidOperation maps root host. Metadata FileSizeNull means pruned/missing not distinguished DTO status.

**significant_methods**: Constructor; ListAsync; GetAsync; ToDto

**interactions_and_risks**: Full23 lines read. Read-only record DTO exposes absolute paths, SQL error text, timestamps and verification state, endpoint must gate. Unbounded backup history because prune preserves records; grows indefinitely. Missing record InvalidOperation maps root host. Metadata FileSizeNull means pruned/missing not distinguished DTO status.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared backup prune/history semantics and root no runtime logs read.

**recommendations**: Paginate, separate file-present/verified state and archive retention; privileged detail view keep path/errors.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
