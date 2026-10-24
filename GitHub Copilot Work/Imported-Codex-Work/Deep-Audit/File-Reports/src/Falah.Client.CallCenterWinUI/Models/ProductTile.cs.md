# src/Falah.Client.CallCenterWinUI/Models/ProductTile.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `a96b5ebff8d106481219546f910cb9b88e49e4b99d6437e47b34f4c3af929484`; worktree SHA256 `d670f96b32b6239616a9f6ba3085b3e8563873cb89929fe67356a8990e6d821c`; LF-equivalent `True`.

Actual role: CallCenter product/offer/category display wrappers.

## Function/resource review

- `ProductTile` at `src/Falah.Client.CallCenterWinUI/Models/ProductTile.cs:11`: ImageURI optional, HasImage only URI presence not download success; category glyph fallback and min-variant starting price, preparation station Arabic label from real DTO. No best-seller per-tile badge flag unlike cashier.

- `OfferChoice` at `src/Falah.Client.CallCenterWinUI/Models/ProductTile.cs:9`: Immutable correct product+offer pairing; CategoryTile constructor owns category and generated IsSelected mutable flag, VM synchronizes reference selected. No physical printing merely station label.

## Tests and limitations

Missing image-error fallback and station/parity test; no real image download invoked.

## Recommendations (not implemented)

Test failed URI download independently from absent image and preserve correct offer product reference.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
