# src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 8aa26b3ab1511f2975c893ee4e571305d0544a2a228f404d08313da740276978

**git_blob_sha256**: 8bee9148544c6025b177b748cef36ac65761b11b9d979df1c686d3bfceb289a3

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 111]]

**functionality_and_reasoning**: Invoice mutable running totals with no concurrency token; context also none. Separate payment keys allow genuine different requests, so ledger can overpay while stale cache reports remaining. Entity docs claim always-in-sync cache unsupported under concurrent settlement.

**significant_methods**: SupplierEntity; PurchaseEntity; PurchaseLineEntity; PurchasePaymentEntity; SupplierLedgerEntryEntity properties

**interactions_and_risks**: Invoice mutable running totals with no concurrency token; context also none. Separate payment keys allow genuine different requests, so ledger can overpay while stale cache reports remaining. Entity docs claim always-in-sync cache unsupported under concurrent settlement.

**findings**: ["FIN-005"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
