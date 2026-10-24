# src/Falah.Client.CashierWinUI/Controls/EmptyStateView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `3119d3b33047924b5300e27e4ddbbfdd196ab1ba3ffa3ce8c9e90e819f950892`; worktree SHA256 `7f7738d5b59f0ea3d8a8753bbba875f7a0366a342b31bcc4be05e94101a3d32a`; LF-equivalent `True`.

Actual role: cashier empty-list message layout.

## Function/resource review

- `TitleBlock` at `src/Falah.Client.CashierWinUI/Controls/EmptyStateView.xaml:15`: Centered shared SectionTitleText title and MutedText wrapping message in MaxWidth360/40DIP padding stack; decorative icon. No list visibility condition owned here: caller must bind count/loading/error correctly.

- `Foreground` at `src/Falah.Client.CashierWinUI/Controls/EmptyStateView.xaml:13`: ThemeResource TextMutedBrush for icon; fonts/styles resolve station dictionaries. Title itself has no wrap, so long caller-supplied title may overflow narrow/high-scale viewport; native measurement not observed.

## Tests and limitations

NOT_APPLICABLE financial functions/transactions/direct printing. Native empty vs loading/error mutual exclusion and Arabic text-scale tests required.

## Recommendations (not implemented)

Confirm parent visibility binds collection Count and excludes loading/error; add long title wrap if actual measurement shows clipping.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
