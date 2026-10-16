# src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs

**path**: src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 70287ac1d6a6d089586a8843b0472abd2dba4d0f1182e19563a9fd309fa11795

**git_blob_sha256**: 9c84406fc7cf89f60cc65b9b06c6324880ae5a29a296384c3052bf27771cc61b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 242]]

**functionality_and_reasoning**: Full242 lines reread bounded after earlier batch truncation. Validates supplier exists and nonempty lines, but no per-line positive quantity/cost; two negative factors produce positive invoice and negative stock, zero quantity/cost accepted. Duplicate ingredient lines share usage key purchase-stock-purchase-item so unique constraint fails ordinary invoice repeated item. Account balance lock outside transaction same FIN-011 race. Previous unit costs N queries and stale versus simultaneous receipt, advisory audit inaccuracies. Atomically supplier invoice/initial payment/stock/journal/audit when normal.

**significant_methods**: Constructor; HandleAsync; FindPurchaseAsync; ToDto

**interactions_and_risks**: Full242 lines reread bounded after earlier batch truncation. Validates supplier exists and nonempty lines, but no per-line positive quantity/cost; two negative factors produce positive invoice and negative stock, zero quantity/cost accepted. Duplicate ingredient lines share usage key purchase-stock-purchase-item so unique constraint fails ordinary invoice repeated item. Account balance lock outside transaction same FIN-011 race. Previous unit costs N queries and stale versus simultaneous receipt, advisory audit inaccuracies. Atomically supplier invoice/initial payment/stock/journal/audit when normal.

**findings**: ["FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared PurchasingRules and tests normal receives; account precheck before BeginTransaction verified; no writes.

**recommendations**: Validate signed quantities/cost before totals, allow/group duplicate ingredient lines or line-index unique keys, move account guard inside transaction and enforce active supplier/item policy.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
