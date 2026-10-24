# src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c8ce6475aa4042f73a358c9a8f6b90ecea378be0bf9c79e28ab8aabcc0f7e92a`; worktree SHA256 `200dbee7a7909b23d5758585eee485830744037c5e17a9970878c49721bd3ecc`; LF-equivalent `True`.

Actual role: CallCenter explicit keyed Arabic text hierarchy.

## Function/resource review

- `TextBase` at `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml:12`: Segoe UI, static TextPrimary, RTL common base; explicit keys not implicit default for all TextBlocks. Local overrides may differ.

- `TotalAmountText` at `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml:19`: Total24Black/title20Black and section14ExtraBold; body13 and muted11. Source sizes do not prove actual physical readability at DPI.

- `ProductNameText` at `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml:44`: Product15ExtraBold wraps; Price16Black brand; category12Bold center and badge11Bold. Caller catalogue explicitly overrides many sizes, so these are not universal metrics.

- `CartItemNameText` at `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml:71`: Actual16Bold dark primary contradict white-on-dark comment, correct for current white card palette.

- `MetricValueText` at `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml:98`: Management26Black and page22Bold, subtitle/body13; caption11 and tableheader12 muted. Dense text requires scaling checks.

- `StatusPillText` at `src/Falah.Client.CallCenterWinUI/Styles/Typography.xaml:118`: White11SemiBold forces light glyph on any semantic pill; neutral/warning contrast and high contrast mode need native check. Nav13SemiBold white.

## Tests and limitations

NOT_EXECUTED. XAML no callable declarations; screenreader reading order, fallback Arabic glyph shaping, OS text scale and contrast not observed. Source comment reference credentials were not copied into report.

## Recommendations (not implemented)

Use theme/high contrast semantic text styles and test all style consumers; do not claim current native accessibility from nominal font sizes.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
