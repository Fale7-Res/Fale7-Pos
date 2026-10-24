# src/Falah.Client.Cashier/Models/ProductTile.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `8f4a9081cffc8aac8fdb7a0b762d8e20b6e4648ae052cf83a9338c9d87284e58`; worktree SHA256 `8f4a9081cffc8aac8fdb7a0b762d8e20b6e4648ae052cf83a9338c9d87284e58`; LF-equivalent `True`.

Actual role: WPF immutable product/offer pairing records.

## Function/resource review

- `OfferChoice` at `src/Falah.Client.Cashier/Models/ProductTile.cs:8`: Pairs actual product DTO+offer DTO by immutable record, view can send correct product offer without ancestor lookup. ProductTile only product/offers; no images/station display helpers unlike active WinUI.

- `ProductTile` at `src/Falah.Client.Cashier/Models/ProductTile.cs:10`: No business calculation, network, authorization, IDisposable or concurrency state; IReadOnlyList does not guarantee underlying list immutable but normal construction passes fresh menu offer list.

## Tests and limitations

NOT_APPLICABLE direct DB/printing, no WPF tests linked current suite.

## Recommendations (not implemented)

Keep reference scope documented; do not assume active image features in WPF.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
