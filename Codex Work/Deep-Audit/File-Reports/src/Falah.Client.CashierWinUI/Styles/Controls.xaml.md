# src/Falah.Client.CashierWinUI/Styles/Controls.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `3713a8f80a1f81cd29634fca46f2013ee65bbb0ad5305fcd528168e77d01f60d`; worktree SHA256 `c198e41d37d31ac9e6e2ffcd3e44c5c70d4f9f854e6d00b999c88ba7e812209b`; LF-equivalent `True`.

Actual role: Complete reusable button/control/layout design system with visual states.

## Function/resource review

- `BaseButtonStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:19`: Full source and XML style entries reviewed. Base Template BorderContentPresenter maps Background/Border/radius/padding; CommonStates Normal/hover.92/press.8/disabled.45opacity, no explicit FocusStates. Native UseSystemFocusVisuals behavior must be observed, not assumed absent.

- `PrimaryButtonStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:67`: Accent/White styles Primary/Cta/Success/Danger/selected variants; Darkaccent brightness can reduce white contrast. Secondary/Payment base semantic surface+TextPrimary correct until overridden by viewWhitebinding UI-002.

- `SuccessOutlineButtonStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:100`: Outline success/info/warn/danger each light/tinted semantic backgrounds and matching colored border/text; source theme table supports selected text concerns. Segment fully fills cells, selected Danger/White alternate.

- `ProductCardStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:236`: Fixed150x190old style; actual catalogue GridView uses custom card content and width calculation, so do not infer active product dimensions solely from unused style comment. Quantity44DIP, note min44, keypad60; modal close44; Icon40target smaller.

- `SearchBoxStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:358`: RTL pillTextBox42height, ordinary TextBox/Password/Combo/Calendar48height; NumberBox compact spin48. Keycap/summary/table/alert/card/dialog styles are presentation only no backend/printing actions.

- `ShellNavViewStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:486`: Legacy NavigationView dictionary style still present though current shell uses C#Buttons; historical XamlCompiler comments about1.5may not apply current1.7. No actual compiler run/repro asserted.

- `DialogDestructiveButtonStyle` at `src/Falah.Client.CashierWinUI/Styles/Controls.xaml:494`: Explicit danger fill helps distinguish destructive operation; does not enforce confirmation/authorization. ContentDialog preferred semantics only used if call sites adopt, custom Gridoverlays elsewhere no focus trap implemented by this file.

## Tests and limitations

XML parser found50style entries, machine-readable list saved. No tests/native screenshots, highcontrast, text scaling or keyboard-focus proof. Function count NOT_APPLICABLE.

## Recommendations (not implemented)

Test actual used style/callsite full states and focus; semantic selected/unselected foreground/background; do not globally alterWhiteutility.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
