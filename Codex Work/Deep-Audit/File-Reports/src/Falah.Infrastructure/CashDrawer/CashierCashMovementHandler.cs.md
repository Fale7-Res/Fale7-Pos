# src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs

**path**: src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: ce1184171f64ce4ce1f8399d26b6228592ce0c6767aded0f4ff4140d35dbd6fd

**git_blob_sha256**: 3693c0da66e3187b850bbe00c9197aebf9705745b9beb2c48419f98a48067462

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 139]]

**functionality_and_reasoning**: Full139 lines read. cash_in Main->Drawer and cash_out Drawer->Main, stable journal, positive reason, correct guarded account inside transaction, aligns ShiftReconciliation type+ShiftId. Shift Open checked beforetransaction so concurrent close can miss late cash movement. Any authenticated actor with cash_drawer can affect shared shift, not owner shift association check.

**significant_methods**: Constructor; HandleAsync; FindExistingAsync; ToResult

**interactions_and_risks**: Full139 lines read. cash_in Main->Drawer and cash_out Drawer->Main, stable journal, positive reason, correct guarded account inside transaction, aligns ShiftReconciliation type+ShiftId. Shift Open checked beforetransaction so concurrent close can miss late cash movement. Any authenticated actor with cash_drawer can affect shared shift, not owner shift association check.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared generic TreasuryTransfer missing operational shift and reconciliation; no cash writes.

**recommendations**: Serialize against shift close on stable shift row, consistent lock order, test distinct cashmovement-vs-close rather than same-ID retry.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
