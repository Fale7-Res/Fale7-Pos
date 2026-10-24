# src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs

**path**: src/Falah.Infrastructure/Purchasing/PurchasingQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 3e4bf1de133d15b4a412d5c8201a5738595bb8a8afe397ceb063eb7288ba259a

**git_blob_sha256**: ca1e51967c73370d615d94e05387deee20ef1fe77ace6b02b63305f8adf62b42

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 109]]

**functionality_and_reasoning**: Full109 lines read. Supplier ledger totals derived correct, invoice aging uses cached Remaining and comment falsely says ledger doesn't attribute invoice though entity PurchaseId does. FIN-005 stale cache =>supplier negative outstanding but aging still positive. Whole history loaded unbounded, supplier query O(suppliers*ledger). Aging current Cairodate only, not historical asof. Due today not overdue.

**significant_methods**: Constructor; ListSuppliersAsync; GetSupplierAsync; GetSupplierLedgerAsync; ListPurchasesAsync; GetPurchaseAsync; GetPayablesAgingAsync; ToDto

**interactions_and_risks**: Full109 lines read. Supplier ledger totals derived correct, invoice aging uses cached Remaining and comment falsely says ledger doesn't attribute invoice though entity PurchaseId does. FIN-005 stale cache =>supplier negative outstanding but aging still positive. Whole history loaded unbounded, supplier query O(suppliers*ledger). Aging current Cairodate only, not historical asof. Due today not overdue.

**findings**: ["FIN-005"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared full pay invoice and entity ledger fields.

**recommendations**: Derive invoice remaining from ledger or transactional synchronized cache then invariant check; paginate and SQL aggregate; test supplier total equals sum invoice aging after concurrent distinct payments.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
