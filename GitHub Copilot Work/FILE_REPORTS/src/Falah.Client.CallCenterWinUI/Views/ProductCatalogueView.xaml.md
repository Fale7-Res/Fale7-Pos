# src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `5635357871f72ff3cd3c068a2c0825caa08702b88f107a81837693e3c96d0fe1`; worktree SHA256 `ba6bc3675c0037ac87cea8e12fbcdde86d2d52eae64dd522f8769a11a5b46f4a`; LF-equivalent `True`.

Actual role: CallCenter RTL live menu grid with product/photo/offer presentation.

## Function/resource review

- `ProductsGridView` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml:12`: Real VisibleProducts collection, item click enabled and selection none; CanEditOperation gates entire grid including nested offers. Grid hidden during menu load; error and empty overlays independent VM flags.

- `FallbackIcon` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml:28`: Photo/fallback share104px band; photo visibility based HasImage, not BitmapImage download success. Failed image may cover fallback depending image transparency, native behavior unobserved.

- `StartingPriceDisplay` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml:52`: Uses product-tile starting price, name wraps3lines, variant badge only for >1 variants. Station label reports actual VM routing; not evidence printer is reachable.

- `OnOfferTapped` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml:69`: Offer button uses Tapped event rather than ICommand, expected code-behind handles event to stop parent base-product click. Native keyboard Invoke/Space may raise Click without Tapped: keyboard accessibility remains unverified and no Command/Click exists here.

- `Offer.FixedPricePiastres` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml:85`: Offer name star column wraps while fixed-price auto column retains amount; hardcoded green pill/light white card rather than theme-aware colors.

- `RetryLoadMenuCommand` at `src/Falah.Client.CallCenterWinUI/Views/ProductCatalogueView.xaml:111`: Error shows wrapped text and explicit real-menu retry; no fabricated fallback rows. HasNoResults text appears independently and must be mutually coherent with VM load/error states.

## Tests and limitations

NOT_EXECUTED. Need native pointer versus keyboard offer invocation, long names/offer height, no image/download error, scaling and read-error overlay tests. No callable declarations exist in immutable XAML symbol map.

## Recommendations (not implemented)

Provide accessible offer command activation across keyboard/pointer paths and image-failed fallback; confirm generated resources and contrast at native render.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
