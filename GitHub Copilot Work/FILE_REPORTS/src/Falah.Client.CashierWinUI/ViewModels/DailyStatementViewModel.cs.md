# src/Falah.Client.CashierWinUI/ViewModels/DailyStatementViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `2c2fc4dfd881adbd4721f61726157e1441cc1bcbbffd45377d657647220e48fc`; worktree SHA256 `2c2fc4dfd881adbd4721f61726157e1441cc1bcbbffd45377d657647220e48fc`; LF-equivalent `True`.

Actual role: Owner day-specific consolidated statement and expense reprint.

## Function/resource review

- `DailyStatementViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/DailyStatementViewModel.cs:22`: Captures Cairo today once at construction; long-open page crossing midnight Today/Yesterday buttons still olddate. No timer rollover, legitimate static snapshot but label can mislead.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DailyStatementViewModel.cs:49`: Selected-date snapshot checked after read, queues reload if changed; correct stale read prevention. On failure Statement=null but old ExpenseLines/Shifts/etc collections not cleared, paired XAML must gate on Statement/error to avoid stale section display.

- `ApplyProductFilter` at `src/Falah.Client.CashierWinUI/ViewModels/DailyStatementViewModel.cs:63`: Persists CategoryFilter across dates; new date category absent leaves no rows until choose all, could look no sales. ProductHourly groups by ProductName not stable ProductId, merges distinct products sharing name; actual DTO onlyname maybe schema limitation.

- `ReprintExpenseAsync` at `src/Falah.Client.CashierWinUI/ViewModels/DailyStatementViewModel.cs:118`: Real marked expense voucher document print job with new Guid each intended press; same-process busy guard, no durable ambiguous print replay. Does not mutate expense/treasury; says queued paper not confirmed. No printing executed.

- `ProductHourRow` at `src/Falah.Client.CashierWinUI/ViewModels/DailyStatementViewModel.cs:79`: Plain immutable row formats hour and quantities; cost/report values from actual server statement, no mock data.

## Tests and limitations

Need midnight rollover, date change category filter, error clears stale sections and ambiguous reprint identity tests; no actual receipt rendered.

## Recommendations (not implemented)

Reset invalid filter on new day, bind all sections to valid matching statement, show businessdate truth and durable reprint semantics.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
