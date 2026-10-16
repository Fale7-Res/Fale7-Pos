# src/Falah.Domain/Inventory/PurchasingRules.cs

**path**: src/Falah.Domain/Inventory/PurchasingRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: e11ccc80b8fb47c7d534bb3595e4553fbfde47eae93eb0eb6ba007f1c7ed2524

**git_blob_sha256**: f926aa4c3317e62750e2fb3d71a013cfce471c3604aa43009cfef69373c5567f

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 99]]

**functionality_and_reasoning**: Decimal rounded quantity/cost with ToEven. Last positive actual receipt chosen, zero-price receipt ignored. Payment bound checks supplied outstanding snapshot only; no concurrency guarantee. Supplier outstanding includes PurchaseId OR ReferenceId. Cost changes advisory not blocking.

**significant_methods**: PurchaseLineTotal; LastPurchaseUnitCost; LastPurchaseAt; DescribeUnitCostChange; ValueStockLoss; AssertPurchasePayment; PurchasePaymentAccountFor; SupplierOutstandingFromLedger

**interactions_and_risks**: Decimal rounded quantity/cost with ToEven. Last positive actual receipt chosen, zero-price receipt ignored. Payment bound checks supplied outstanding snapshot only; no concurrency guarantee. Supplier outstanding includes PurchaseId OR ReferenceId. Cost changes advisory not blocking.

**findings**: ["FIN-005"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
