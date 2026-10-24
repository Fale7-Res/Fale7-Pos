# src/Falah.Client.Cashier/Styles/Typography.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `7811af162cc08decd013a7af16e5a2ba435bfd5465ccbf6b2f75418fd103db06`; worktree SHA256 `7811af162cc08decd013a7af16e5a2ba435bfd5465ccbf6b2f75418fd103db06`; LF-equivalent `True`.

Actual role: Legacy WPF keyed Segoe UI Arabic typography.

## Function/resource review

- `TextBase` at `src/Falah.Client.Cashier/Styles/Typography.xaml:12`: Base fixesRTL and dark foreground. Total24Black/title20Black/section14ExtraBold reflect explicit nominal sizes, not independent rendering evidence.

- `ProductNameText` at `src/Falah.Client.Cashier/Styles/Typography.xaml:44`: 15ExtraBold wraps; price16Blackbrand, category12Boldcenter, badge11Bold. Muted11 smaller user text may need scale review.

- `CartItemNameText` at `src/Falah.Client.Cashier/Styles/Typography.xaml:71`: 13Boldwhite specifically dark cart. Different from active and CallCenter16dark styles; do not assume interchangeable shared resource behavior.

## Tests and limitations

NOT_EXECUTED; no callable declarations. Arabic glyph fallback/native DPI, contrast and reading order remain unobserved.

## Recommendations (not implemented)

Validate only if legacy client deployed; preserve active frontend distinction.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
