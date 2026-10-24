# src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `eda170153810d5ed626be9017d2652b0db224a5977ddaf0b3dc0bd4b3ae273b1`; worktree SHA256 `eda170153810d5ed626be9017d2652b0db224a5977ddaf0b3dc0bd4b3ae273b1`; LF-equivalent `True`.

Actual role: CallCenter native product grid responsive cells and product/offer event routing.

## Function/resource review

- `ProductCatalogueView` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml.cs:8`: Hooks own Grid SizeChanged and own Loaded; local events do not create external retained VM reference. Reapplies after panel realized.

- `ApplyItemWidth` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml.cs:21`: Uses target190DIP, minimum two columns, divides available width; declared min/max constants unused. At narrow/high-scale width can assign cells below declared minimum or arbitrarily large cells, so source comment claiming min/max constraint not implemented. Actual content clipping needs native screenshot; no runtime proof.

- `OnProductClicked` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml.cs:44`: Typed tile/VM dispatches SelectProduct command; VM enforces frozen state (availability differs across station implementations).

- `OnOfferTapped` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml.cs:54`: Sets e.Handled true before dispatch, correctly prevents bubbling product click clobbering offer variant selection. Because offer surface may be Border Tapped (paired XAML), keyboard accessibility must be checked separately.

## Tests and limitations

Missing native available width edge cases, large text and keyboard/offer activation tests. Unit VM tests do not exercise routed tap propagation.

## Recommendations (not implemented)

Enforce actual intended column min/max adaptivity and accessible keyboard offer action; retain Handled correctness.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
