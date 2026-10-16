# src/Falah.Client.Cashier/Styles/Controls.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `def517b56505b14025d5631195a2e74a85492e9f999dc7131674971ca2230e75`; worktree SHA256 `def517b56505b14025d5631195a2e74a85492e9f999dc7131674971ca2230e75`; LF-equivalent `True`.

Actual role: Legacy WPF control templates and POS resource styles.

## Function/resource review

- `BaseButtonStyle` at `src/Falah.Client.Cashier/Styles/Controls.xaml:13`: WPF Border uses Tag as CornerRadius, ContentPresenter hardcodescenteralignment despite ProductCard horizontalstretch setter. Mouseover/pressed/disabled opacity triggers .92/.82/.45; no custom focus trigger, default FocusVisualStyle may still render.

- `CtaButtonStyle` at `src/Falah.Client.Cashier/Styles/Controls.xaml:56`: 52fixedheight16Black whiteonactionblue. Primary/success/danger all share template; button keyboard command behavior remains WPF Button implementation.

- `SegmentButtonStyle` at `src/Falah.Client.Cashier/Styles/Controls.xaml:82`: 34height conflicts inherited48minimum; actual arrangedminimum wins dependingconstraints. Keypad64minimum. Explicit Width/Height are not sufficient measured hit targets.

- `CategoryButtonStyle` at `src/Falah.Client.Cashier/Styles/Controls.xaml:113`: 114x80white/dark and selectedactionwhite; product145x180fixed despite older comment208. Fixedheights can clip names/offers attextscale.

- `QuantityButtonStyle` at `src/Falah.Client.Cashier/Styles/Controls.xaml:162`: Width30Height30 but inheritedMinHeight48 means narrow30 and at least48high; no backgroundforeground defined here so relies consumers/defaults. Notechip9px resetsMinHeight0, very small nominal hit target.

- `CartItemCardStyle` at `src/Falah.Client.Cashier/Styles/Controls.xaml:153`: Darkcartcard with.8border and12padding. Light variant/offer pills independent. No runtime theme/highcontrast overrides.

- `PART_ContentHost` at `src/Falah.Client.Cashier/Styles/Controls.xaml:208`: Implicit TextBox template supplies required named ScrollViewer host inside rounded Border. Replaces standard chrome without focused/error/read-only visual states; validationadorner may exist externally. Multilineverticalscroll behavior notexplicit.

## Tests and limitations

NOT_EXECUTED. No callable declarations. Need effective geometry/focus/validation/scaling tests if legacy deployed; activeWinUI separate.

## Recommendations (not implemented)

Respect alignment TemplateBindings and explicit minsize choices in approvedfix; validate focus/error visibility natively.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
