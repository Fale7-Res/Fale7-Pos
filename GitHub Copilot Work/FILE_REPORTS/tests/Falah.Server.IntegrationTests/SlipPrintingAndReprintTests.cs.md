# tests/Falah.Server.IntegrationTests/SlipPrintingAndReprintTests.cs

**path**: tests/Falah.Server.IntegrationTests/SlipPrintingAndReprintTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 487f40bf8b069060e7e5dce6bf4dde033f0cfb0af52593ae6bdc8697ed39d911

**git_blob_sha256**: 487f40bf8b069060e7e5dce6bf4dde033f0cfb0af52593ae6bdc8697ed39d911

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 253]]

**functionality_and_reasoning**: Checks shift open/close slips, employee advance reconciliation, expense voucher recomposition, operational and order reprint marking/replay/audit with selected financial footprint. Pure software queue/ticket reads do not physically print. Footprint only counts selected tables and treasury sum, so same-count/value-preserving mutation or omitted ledgers not detected. Closing amounts both equalwhole EGP; test cannot expose swapped expected/actual or cents. No recipe means stock immutability weaker. Kitchen exclusion uses no kitchen-bound product.

**significant_methods**: All40 actual callable identities individually reasoned

**interactions_and_risks**: Checks shift open/close slips, employee advance reconciliation, expense voucher recomposition, operational and order reprint marking/replay/audit with selected financial footprint. Pure software queue/ticket reads do not physically print. Footprint only counts selected tables and treasury sum, so same-count/value-preserving mutation or omitted ledgers not detected. Closing amounts both equalwhole EGP; test cannot expose swapped expected/actual or cents. No recipe means stock immutability weaker. Kitchen exclusion uses no kitchen-bound product.

**findings**: ["FIN-004"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 253 lines fully read;40 stable callable receipts saved with verified source identity; no execution.

**recommendations**: Use full state/row snapshots, recipe-backed sale, kitchen-eligible item, cents/discrepancy and changed historical refund/config fixtures; verify banner per segment not count only.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
