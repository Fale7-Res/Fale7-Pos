# src/Falah.Domain/Orders/OrderPaymentQueries.cs

**path**: src/Falah.Domain/Orders/OrderPaymentQueries.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 805e8d11d87caec242e36504176417b876da153b236910f64502c3488e2b9412

**git_blob_sha256**: 805e8d11d87caec242e36504176417b876da153b236910f64502c3488e2b9412

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 82]]

**functionality_and_reasoning**: Cancelled orders skipped entirely. Mixed immediate cash/card counted before electronic confirmed; pending cancelled mixed cash disappears. Single-method lifetime RefundAmount netted without operational shift/date allocation, unsuitable alone for drawer-period accounting.

**significant_methods**: PaymentMethodTotals; CashSalesFromOrders; CashRefundsFromOrders

**interactions_and_risks**: Cancelled orders skipped entirely. Mixed immediate cash/card counted before electronic confirmed; pending cancelled mixed cash disappears. Single-method lifetime RefundAmount netted without operational shift/date allocation, unsuitable alone for drawer-period accounting.

**findings**: ["FIN-002", "FIN-004"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
