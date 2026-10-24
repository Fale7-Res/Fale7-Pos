# tests/Falah.Server.IntegrationTests/PurchasingVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/PurchasingVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 746e08316d0a3933f38e1de46a2fa31e2cf9eb829c03a05ad9dbb50dd8b973dd

**git_blob_sha256**: d791cb571e6bd5fceecc25764a39ba83f2474476f831d3f130c8612846298113

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 345]]

**functionality_and_reasoning**: Covers supplier gates, receipt stock/treasury/ledger atomicity, credit/partial paid, invalid overpayment, same-key receive/payment concurrency and retry. All simultaneous later payments share one RequestId, so different financial operations against one outstanding invoice never tested. Same account funded fixture cannot substitute stale outstanding test. No test execution; CREATE/DROP fixture prohibited.

**significant_methods**: InitializeAsync; DisposeAsync; CreateSupplierAsync; CreateInventoryItemAsync; ReceiveAsync; PayAsync; all facts

**interactions_and_risks**: Covers supplier gates, receipt stock/treasury/ledger atomicity, credit/partial paid, invalid overpayment, same-key receive/payment concurrency and retry. All simultaneous later payments share one RequestId, so different financial operations against one outstanding invoice never tested. Same account funded fixture cannot substitute stale outstanding test. No test execution; CREATE/DROP fixture prohibited.

**findings**: ["FIN-005"]

**evidence**: Full bounded source read and explicit cross-path reasoning, not executed.

**recommendations**: Add described distinct-operation/terminal-state/operational-shift regression coverage after approval.

**uncertainties**: No runtime reproduction. No claim of declaration semantic coverage.
