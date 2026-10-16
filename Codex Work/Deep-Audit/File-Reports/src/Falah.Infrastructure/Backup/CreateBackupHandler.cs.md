# src/Falah.Infrastructure/Backup/CreateBackupHandler.cs

**path**: src/Falah.Infrastructure/Backup/CreateBackupHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: df823c1d419205a70fe7bd80900c16137dd07e5a7c77a82f768b988a94c17ea1

**git_blob_sha256**: d35b9622b2d01aa0d96c9fa2bb6c97e42d89a6d3cdbb01acb3f5ebbc6fc2de69

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 104]]

**functionality_and_reasoning**: Native SQL COPY_ONLY/CHECKSUM/FORMAT plus Requested/outcome records. Filename only second resolution; concurrent/manual automatic same-second calls share path and WITH FORMAT can overwrite. SQL remote filesystem may differ File.Exists/size in app. Cancellation can leave Requested or suppress outcome persistence. No backup run.

**significant_methods**: Constructor; HandleAsync; RunAsync

**interactions_and_risks**: Native SQL COPY_ONLY/CHECKSUM/FORMAT plus Requested/outcome records. Filename only second resolution; concurrent/manual automatic same-second calls share path and WITH FORMAT can overwrite. SQL remote filesystem may differ File.Exists/size in app. Cancellation can leave Requested or suppress outcome persistence. No backup run.

**findings**: ["FIN-007"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
