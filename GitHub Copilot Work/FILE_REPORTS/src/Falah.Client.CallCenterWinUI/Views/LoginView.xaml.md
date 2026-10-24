# src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `00a631959c580b7b1bd1b9a7e13dc73a3c23946d320cd8d5aef0f6f6e505530e`; worktree SHA256 `00a631959c580b7b1bd1b9a7e13dc73a3c23946d320cd8d5aef0f6f6e505530e`; LF-equivalent `True`.

Actual role: CallCenter PIN touch login presentation.

## Function/resource review

- `FlowDirection="RightToLeft"` at `src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml:4`: Arabic surface RTL but nested numeric keypad deliberately LTR; digits1..9 row order preserves phone layout.

- `PinDisplay` at `src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml:28`: Visible masked VM display, empty placeholder and error display; this markup does not store or reveal raw PIN. No native PasswordBox input here; physical keyboard handling lives elsewhere.

- `PressDigitCommand` at `src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml:57`: Twelve buttons delegate digit, clear and backspace. Backspace glyph lacks explicit AutomationProperties.Name; accessible meaning needs native verification. All actions rely VM busy gates, no keypad IsEnabled busy binding.

- `SubmitLabel` at `src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml:73`: Primary submit caption and command bound; no username because API maps PIN to account. Footer describes personal permissions.

- `Width="440"` at `src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml:9`: Fixed width440 plus40px padding; no ScrollViewer. Small display or large accessibility scale may clip login; native DPI observation NOT_EXECUTED.

- `CardBorderBrush` at `src/Falah.Client.CallCenterWinUI/Views/LoginView.xaml:10`: Cashier uses ThemeResource brushes; CallCenter uses static and literal white/light backgrounds. This loop records observed difference, not a claim of identical theming.

## Tests and limitations

NOT_EXECUTED. Native keyboard/assistive tech, busy double taps, display masking and text scaling tests required. No actual authentication automated.

## Recommendations (not implemented)

Give keypad glyph explicit accessible name; verify high contrast and overflow. Keep auth security review with coordinator.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
