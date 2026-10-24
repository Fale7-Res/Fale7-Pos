# src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0761c09ac64ba8268499d368e4a2ee82607f9565198f281ecb3581b59f84ac2d`; worktree SHA256 `3e0e132d15c7a74ba685af1057eb1f33b63d874bfe691ba1fef1bf0464e489c0`; LF-equivalent `True`.

Actual role: CallCenter WinUI button templates, input and management resources.

## Function/resource review

- `BaseButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:19`: Border template binds content/template/alignment/padding; Normal/PointerOver/Pressed/Disabled opacity1/.92/.8/.45. No explicit focus visual state in template; platform system focus visuals may still apply, native verification required.

- `CtaButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:74`: Primary/CTA/success/danger white foreground, fixedCTA52 plus inheritedMinHeight48; long/scaled labels may clip. Secondary literal lightgray. Segment gray foreground on pale background defaults may be weak unless instance overrides (actual payment/order segments do).

- `PaymentMethodButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:116`: White/darkprimary border2,72height; selected colors applied per-instance. Keypad min60x52 stretched; close min44x44. Actual hit target governed by constraints; nominal styles alone not usability proof.

- `CategoryButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:167`: Fixed114x80 category and150x190 product, selected style blue/action white; actual catalogue uses bespoke grid cards rather than ProductCardStyle. Cart Quantity44height inherits48minimum so effective height may be48, unlike comment square44.

- `NoteChipButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:225`: Note min44 and pale background; variant/offer border styles use fixed light colors. Implicit MenuFlyout48, TextBox/Password/Combo48 withRTL and14px; URL/phone callers must override flow where needed.

- `SearchBoxStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:308`: Keyed TextBox style independently restatesfont/RTL/padding and42minimum; implicit style not automatically inherited. NumberBox compact spin uses48minimum, quantity/money precision still VM responsibility.

- `IconButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:332`: 40x40 withMinHeight0; Ghost36 and toolbar38 smaller than44touch target, may be desktop choice. Accessible icon names must be instance-specific, not supplied here.

- `PageCardStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:378`: White static cards, summarymin180, tablemin48, pill10x4padding and alertborder. No theme dictionaries/highcontrast substitutions in file; use actual resource consumers.

- `ShellNavViewStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:436`: Only open248/compact56 sizes; no PaneBackground setter. Historicalcompilercrash comment not independently reproduced and is not active defect evidence.

- `DialogDestructiveButtonStyle` at `src/Falah.Client.CallCenterWinUI/Styles/Controls.xaml:444`: Dangerwhite button label/body remain callerresponsibility; no permission/confirmation/business write logic implemented by style.

## Tests and limitations

NOT_EXECUTED. No callable declarations. Native focused/disabled/highcontrast state, longlabels/RTL and effective min-height conflicts require measured geometry. Historical render/compiler claims not accepted as our observation.

## Recommendations (not implemented)

Preserve focus/accessible names, semantic brush pairs and consistent touch targets if deployment hardware requires; no source changes performed.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
