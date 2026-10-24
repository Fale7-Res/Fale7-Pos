# src/Falah.Infrastructure/Persistence/Migrations/20260903143733_AddShiftCloseIdempotency.Designer.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260903143733_AddShiftCloseIdempotency.Designer.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 48c5d95700004c77b95e1206c642d18356a5dfa0161d4cf755901c714c3daed2

**git_blob_sha256**: f272554f7e858e8b77eef5d2db430c53bd15619b5eb8d281dad3eac41a36a197

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**coverage_classification**: CHANGED_BLOCK_REVIEW_PLUS_VERIFIED_EXACT_SOURCE_EQUIVALENCE

**reviewed_line_spans**: [[1, 974]]

**functionality_and_reasoning**: Historical close-idempotency target; Shift gains nullable CloseRequestId nvarchar450 with unique IS NOT NULL filter plus nullable handover/reconciliation movement strings without FK. Existing manualVersion and unique openkey unchanged. Legacy/open rows may share null closekey; nonnull keys globally unique. Complete diff manually inspected, current builder/class/order/relationships/seeds checked;33 unchanged blocks source-equivalent to fully read initial model,2 changed blocks individually reasoned. This doesnot fix historical plaintext PIN, stockdirection or missingfinancial constraints; no currentstate inference.

**significant_methods**: BuildTargetModel plus individually mapped generated entity/index/FK/navigation lambdas; exact stable identities in Knowledge shard.

**interactions_and_risks**: Historical close-idempotency target; Shift gains nullable CloseRequestId nvarchar450 with unique IS NOT NULL filter plus nullable handover/reconciliation movement strings without FK. Existing manualVersion and unique openkey unchanged. Legacy/open rows may share null closekey; nonnull keys globally unique. Complete diff manually inspected, current builder/class/order/relationships/seeds checked;33 unchanged blocks source-equivalent to fully read initial model,2 changed blocks individually reasoned. This doesnot fix historical plaintext PIN, stockdirection or missingfinancial constraints; no currentstate inference.

**findings**: []

**evidence**: Current parent model applicability/registration order/relationships and all changed blocks manually inspected. 33 exact source spans verified by SHA256 AND actual text equality to analyzed predecessor; 2 changed blocks manually reasoned. Reused evidence is NOT fresh independent rereading or runtime validation.

**tests_and_evidence**: Static only; no migration/DB/test execution. Exact equivalence evidence: Sub-Agents/Results/financial-model-equivalence-ledger.json

**recommendations**: Verify actual CloseShift transaction/replay/movement linkage and populated upgrade, unique closekey acrossshifts, legacy nullkeys and reopen semantics; no database execution during audit.

**uncertainties**: Historical target metadata is not applied database state or proof of upgrade safety. Independent depth total UNKNOWN; current handler/callpath coverage separate.
