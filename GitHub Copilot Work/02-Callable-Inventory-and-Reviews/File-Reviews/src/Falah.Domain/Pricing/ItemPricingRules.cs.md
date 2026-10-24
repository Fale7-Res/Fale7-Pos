# src/Falah.Domain/Pricing/ItemPricingRules.cs

**path**: src/Falah.Domain/Pricing/ItemPricingRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 47248fb19dfcb29d219a349e99e1e60995bccd12a84afbd4139b081467161303

**git_blob_sha256**: 47248fb19dfcb29d219a349e99e1e60995bccd12a84afbd4139b081467161303

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 28]]

**functionality_and_reasoning**: Full28 lines read. Requested live rule replaces list price with fixed price even if higher; invalid/stale offer silently returns list price intentionally, which may require cashier display before charge. No independently validates list/fixed positivity; trusts catalog handler.

**significant_methods**: ResolvedItemPrice.RuleApplied; Resolve

**interactions_and_risks**: Full28 lines read. Requested live rule replaces list price with fixed price even if higher; invalid/stale offer silently returns list price intentionally, which may require cashier display before charge. No independently validates list/fixed positivity; trusts catalog handler.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared MenuPricingService and PricingRules; checkout mixed/tax tests previously inspected.

**recommendations**: Test stale rule repricing user acknowledgement and negative catalog variant guard in authoring handler.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
