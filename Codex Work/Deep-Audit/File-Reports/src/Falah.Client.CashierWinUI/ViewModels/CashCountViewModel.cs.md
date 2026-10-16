# src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `ceebbaa7c1614cd7716f828cb44d543cc442604217f0e109d666a1c36a925625`; worktree SHA256 `bcf699964dcc694f570818fe41cc876c3b31749725df7e893cb23ae99989b8ee`; LF-equivalent `True`.

Actual role: Business-day denomination count form/history with durable save.

## Function/resource review

- `DenominationRow` at `src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs:30`: Quantity uses long.TryParse without Arabic/Persian normalization; invalid/negative input silently treated0. At least one valid row allows bad other row to submit0instead of flagging; source data-entry risk distinct from backend totals authority. Product multiplication unchecked long can overflow in live display/Rows.Sum.

- `OpenAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs:84`: Requires cash_count and no global unresolved op, sets businessDay and opens then loads. Recovered _write exists but no constructor form reopening/target; on load IsLoading triggers session pending, can block later open paths. Save supports recovered DTO independent day but native retry reachability must be verified.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs:91`: Fetches server denominations and actual history; detaches prior row Changed events before replacement, attaches new once, correct event hygiene. IsReady only true after both reads; no hardcoded denomination list.

- `SaveAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs:141`: Fresh requires CanAct/date/user, any positive quantity and valid expected amount; captures all row quantities and ShiftId with stable pendingId, uses durable helper. Does not reject every malformed row individually. Success display uses server authoritative counted/difference and reloads.

- `NotifyState` at `src/Falah.Client.CashierWinUI/ViewModels/CashCountViewModel.cs:74`: Session gate busy or retained pending, triggered by busy/loading changes not constructor. Close blocks pending. Difference negation long.Min overflow theoretical if server domain accepts extreme values, not normal restaurant scenario.

## Tests and limitations

Need invalid/Arabic count row error tests, process recovery target/retry UI, numeric limits and zero physical count policy. No counts written.

## Recommendations (not implemented)

Normalize/validate every row explicitly and use checked bounded arithmetic; bind recovered business-day target and visible retry.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
