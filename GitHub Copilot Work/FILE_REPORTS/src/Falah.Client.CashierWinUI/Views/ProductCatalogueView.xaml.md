# src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `854ca9c2ea472b17280ca93a8811b20f27c49ce006140357d13a8b08138644ed`; worktree SHA256 `b871ffc43c860f736353206caa0e147a9f2fb5a9d7c691166c8d2fc34cb9264a`; LF-equivalent `True`.

Actual role: Cashier live product grid with theme resources, bestseller marker and offers.

## Function/resource review

- `ProductsGridView` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:12`: VisibleProducts bound, CanEditOperation disables nested input, item click versus selection none. Dynamic sizing is delegated to codebehind; no claim of native fit from comments.

- `IsBestSeller` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:36`: Marker has literal translucent white#F2FFFFFF but TextPrimaryBrush varies by theme. In dark theme light text on near-white can repeat UI002 conditional contrast issue; image underneath can vary. No screenshot proof.

- `FallbackIcon` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:28`: Image overlays fallback in same104px band; HasImage only denotes URL presence, not completed decode. Name wraps3lines and price21px; card sizing must accommodate offers beneath.

- `StationLabel` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:50`: Preparation route label and variant count presented separately; route badge says configured station rather than tested printer availability.

- `OnOfferTapped` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:77`: Nested offer button handles pointer Tapped only, no Click or Command. Parent base-product ItemClick must be suppressed by codebehind. Keyboard/automation activation is not established by markup.

- `Offer.FixedPricePiastres` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:93`: Name star column and auto price avoid direct horizontal overlap. Fixed pale green pill and dark green text stay light palette while surrounding card is theme-aware.

- `RetryLoadMenuCommand` at `src/Falah.Client.CashierWinUI/Views/ProductCatalogueView.xaml:119`: Real retry plus loading/error/empty overlays; only spinner and grid loading inverses defined here. VM must prevent empty state competing with error. No demo records or financial writes in markup.

## Tests and limitations

NOT_EXECUTED. Need native dark bestseller contrast, pointer/keyboard nested offers, thumbnail failure, variable offer heights, DPI/text scaling and coherent error overlay tests. Immutable XAML map has no callable declarations.

## Recommendations (not implemented)

Use semantic badge background/foreground pair and accessible offer activation; native screen evidence pending.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
