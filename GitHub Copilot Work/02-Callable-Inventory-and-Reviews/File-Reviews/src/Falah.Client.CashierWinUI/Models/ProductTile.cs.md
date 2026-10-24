# src/Falah.Client.CashierWinUI/Models/ProductTile.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `40b45d923f33b6a9a3edf1038491ab5e407f99f2f3ed3e2c6e018dbca6375917`

Worktree SHA-256: `30e90ebda4f5752fd379f168be17c6541753fa7988ef6b14c316ce22cc9658ac`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Immutable menu tile/offer pairing and observable category selection.

## Source paths and function/resource analysis

- `ProductTile` at `src/Falah.Client.CashierWinUI/Models/ProductTile.cs:11`: Owns image URI rather than bitmap; source category icon fallback, minimum variant starting price, actual preparation station label. Does not claim image download success when HasImage true; failed image may leave blank without fallback event handling elsewhere.

- `OfferChoice` at `src/Falah.Client.CashierWinUI/Models/ProductTile.cs:9`: Pairs offer to actual product DTO to avoid fragile ancestor lookup; no authorization or persistent state.

- `CategoryTile` at `src/Falah.Client.CashierWinUI/Models/ProductTile.cs:42`: Exposes category plus generated observable IsSelected; VM maintains reference-based selected state.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing inaccessible-image fallback and multiple-variant starting-price tests.

## Recommendations (not implemented)

Verify failed image fallback in native catalogue; no immediate data integrity defect proven.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
