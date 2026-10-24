# src/Falah.Infrastructure/Persistence/Migrations/20260904015716_AddPurchasingAndSuppliers.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260904015716_AddPurchasingAndSuppliers.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 8b2347c5748e57a162b99a175d66e146a9972d1e904b5b7e20a5f4651e1a29d7

**git_blob_sha256**: bdaa9b385da8c5dff55f67097b1ac4f58a81cdd800a62aa98a847c63a8da81c8

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 244]]

**functionality_and_reasoning**: Adds suppliers, purchases, payment rows, supplier ledger and line items with receipt/payment/ledger unique request keys. Paid/remaining totals are cached bigint fields with no Version or check constraint, supporting FIN-005 stale writes despite unique distinct payment keys. Supplier and ingredient FK deletes restricted; line/payment rows cascade with purchase deletion. Ledger PurchaseId is indexed but no FK. Down removes invoice/payment history.

**significant_methods**: Up; Down

**interactions_and_risks**: Adds suppliers, purchases, payment rows, supplier ledger and line items with receipt/payment/ledger unique request keys. Paid/remaining totals are cached bigint fields with no Version or check constraint, supporting FIN-005 stale writes despite unique distinct payment keys. Supplier and ingredient FK deletes restricted; line/payment rows cascade with purchase deletion. Ledger PurchaseId is indexed but no FK. Down removes invoice/payment history.

**findings**: ["FIN-005"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 244 lines fully read; no migration executed.

**recommendations**: Test distinct-payment concurrency, cached versus ledger total reconciliation and supplier orphan prevention.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
