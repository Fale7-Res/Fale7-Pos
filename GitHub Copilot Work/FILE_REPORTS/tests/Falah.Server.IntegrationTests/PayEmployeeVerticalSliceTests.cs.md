# tests/Falah.Server.IntegrationTests/PayEmployeeVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/PayEmployeeVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: cb3763cbdc7747e3b89f2dc6412191d59273294b2fac911990efeb52e52f9d77

**git_blob_sha256**: cb3763cbdc7747e3b89f2dc6412191d59273294b2fac911990efeb52e52f9d77

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 324]]

**functionality_and_reasoning**: Tests partial/full/overpayment, later credits, same-key and distinct-key races, account authority, exact movement fields, audit, Settlement classification versus Advance and range-flow/current-balance reporting. Advances intentionally permit negative employee entitlement. Drawer rejection also lacks funds so status alone not account-rule proof. Main payment versus payroll-run creation reconciliation absent; Adjustment credits absent. Single-by-movement-ID does not prove no logical duplicate rows; earlier reference-based count is stronger.

**significant_methods**: All41 exact callable spans individually reasoned in Knowledge shard

**interactions_and_risks**: Tests partial/full/overpayment, later credits, same-key and distinct-key races, account authority, exact movement fields, audit, Settlement classification versus Advance and range-flow/current-balance reporting. Advances intentionally permit negative employee entitlement. Drawer rejection also lacks funds so status alone not account-rule proof. Main payment versus payroll-run creation reconciliation absent; Adjustment credits absent. Single-by-movement-ID does not prove no logical duplicate rows; earlier reference-based count is stronger.

**findings**: ["FIN-012"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 324 lines fully read;41 symbol receipts saved with verified source hash; no test execution.

**recommendations**: Test payroll after standalone settlement, corrections, precise rejected source error and per-account conservation; enforce deterministic race barriers.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
