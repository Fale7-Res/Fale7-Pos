# src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `8995e7432c49f944b5908a010c1777932be6656408a19b1611b359b31df62fee`; worktree SHA256 `3cb8beacf1ca5d83da4dddd9b9cd1afdeccd9f783ef11392f474ef0ae330d4f7`; LF-equivalent `True`.

Actual role: Treasury account positions, searchable ledger and money movement overlay.

## Function/resource review

- `Balances` at `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml:7`: 176x78 wrapping balance buttons filter by account; account label mapped Arabic. Outside-money pane min300 separately explains pending transfers and driver custody rather than including them in held balance.

- `FilterByAccountCommand` at `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml:56`: All-accounts reset plus240px text filter; actual server query scope and100row limit in API mean search may cover only loaded movements. No paging/date controls visible; empty copy says every movement, potentially overstates completeness.

- `Movements` at `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml:149`: Type/source/destination/amount/date/details including actor+reference supports traceability. Fixed650px columns plus detailstar, eager ItemsControl/vertical scroll. Amounts formatted from server integer values, no ledger edits.

- `IsActionOpen` at `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml:232`: 460max680 full scroll, deposit/withdraw funding accounts distinct from transferAllAccounts. Amount/reason/reference and managerPIN disabledCanEditAction. External funding restrictions stated but actual server validation independent.

- `SubmitTreasuryActionCommand` at `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml:290`: Static execute caption with no separate process-recovered pending panel. UI001 prior memory-only treasury request concern not cured by disabled fields. No monetary write executed.

- `ActionNote` at `src/Falah.Client.CashierWinUI/Views/TreasuryView.xaml:226`: White unwrapped toast on TextPrimary theme brush: dark lowcontrast and longnote overflow risk. Refresh icon no explicit accessible name; no standalone focus trap.

## Tests and limitations

NOT_EXECUTED. Need exact retry/global navigation/restart, account filtering with capped history, source/destination constraints, native small viewport and dark feedback. No callable XAML declarations.

## Recommendations (not implemented)

Expose ledger coverage/paging and pending action context. Keep outside money separate and use semantic toast pair.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
