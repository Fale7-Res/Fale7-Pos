# src/Falah.Client.CashierWinUI/Views/CashCountView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `fdff24543c12481850d000b35f23422e46bf56717cd7a49ff23f0e25461b76b0`; worktree SHA256 `806baf939a784987a0ba222702bed49c9a0dd272791e9d52608fe6b8b9a5dfb3`; LF-equivalent `True`.

Actual role: Physical denomination count entry and same-day count history overlay.

## Function/resource review

- `Rows` at `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml:47`: Denomination label130 and total140 around quantity star; root CanAct disables quantity. Forty-eight pixel input and numeric scope aid touch. No automatic cash/ledger mutation in markup.

- `CountedTotalDisplay` at `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml:97`: Explicitly labels local sum a counting aid; ExpectedText remains operator input. Server calculation of difference must not be confused with independently measured expected ledger balance.

- `History` at `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml:108`: Shows counted/expected/difference only, without count time or operator. Multiple same-day entries indistinguishable beyond amounts; result identity/date context supplied by VM/server not shown.

- `IsLoading` at `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml:76`: Loading/error overlays denomination pane only; total/history pane still present and can retain stale data during refresh. Native VM flag sequencing needed before claiming stale actual behavior.

- `SaveLabel` at `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml:147`: Caption supports retry, action error/note pinned with save while body scrolls. Max1020, fixed330 side pane plus left minimum columns requires width validation on smaller terminals.

- `CloseCommand` at `src/Falah.Client.CashierWinUI/Views/CashCountView.xaml:20`: Header uses VM close guard; Grid overlay contains no standalone focus confinement. Draft/recovered context control belongs VM retained request handling.

## Tests and limitations

NOT_EXECUTED. Test physical count arithmetic boundaries, zero quantities, expected source clarity, restart retry, stale reload, history identification and narrow/scaled layout. No callable declarations.

## Recommendations (not implemented)

Expose time/operator for count history and explain expected balance provenance; verify atomic snapshot/retry without writing actual counts during audit.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
