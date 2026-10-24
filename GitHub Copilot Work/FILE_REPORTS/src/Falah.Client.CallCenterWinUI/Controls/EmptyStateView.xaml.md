# src/Falah.Client.CallCenterWinUI/Controls/EmptyStateView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `b87a1b8acc5fe1b51ef7da3d91f743b62f2d7a641354705d63ecc4a69a0bee1e`; worktree SHA256 `b87a1b8acc5fe1b51ef7da3d91f743b62f2d7a641354705d63ecc4a69a0bee1e`; LF-equivalent `True`.

Actual role: CallCenter empty-list message layout.

## Function/resource review

- `TitleBlock` at `src/Falah.Client.CallCenterWinUI/Controls/EmptyStateView.xaml:15`: Centered shared SectionTitleText title and MutedText wrapping message in MaxWidth360/40DIP padding stack; decorative icon. No list visibility condition owned here: caller must bind count/loading/error correctly.

- `Foreground` at `src/Falah.Client.CallCenterWinUI/Controls/EmptyStateView.xaml:13`: StaticResource TextMutedBrush for icon; fonts/styles resolve station dictionaries. Title itself has no wrap, so long caller-supplied title may overflow narrow/high-scale viewport; native measurement not observed.

## Tests and limitations

NOT_APPLICABLE financial functions/transactions/direct printing. Native empty vs loading/error mutual exclusion and Arabic text-scale tests required.

## Recommendations (not implemented)

Confirm parent visibility binds collection Count and excludes loading/error; add long title wrap if actual measurement shows clipping.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
