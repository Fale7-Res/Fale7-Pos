# src/Falah.Client.CashierWinUI/Styles/Typography.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `250bba459d19aa1f1c2608b710cc20554a2c695fa74de7433270c6333425f79f`

Worktree SHA-256: `825d90f6cd0a7dca69e0f58d5c8a27b1ee0bd3ce2d1eb2dd7c3c95b4d805141c`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Shared RTL typography, semantic text colors, fixed sizes.

## Source paths and function/resource analysis

- `TextBase` at `src/Falah.Client.CashierWinUI/Styles/Typography.xaml:12`: Segoe UI, RTL and themed TextPrimary inherited across named styles; explicit overrides exist for muted/nav/status. Most body text 13 DIP, captions/muted 11 DIP, important for touch-distance readability testing.

- `StatusPillText` at `src/Falah.Client.CashierWinUI/Styles/Typography.xaml:118`: Always White despite bright dark-theme semantic backgrounds, a source contrast risk. Title/price/metric resources use semantic brushes; static FontFamily independent of theme.

- `AppFontFamily` at `src/Falah.Client.CashierWinUI/Styles/Typography.xaml:10`: Comment contains a historical demo PIN; this report deliberately does not repeat it. Historical measurement assertions are unverified documentation, not evidence of current visual parity.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

No text scale/high contrast screenshots reviewed. XML declaration/index not evidence all rendered combinations pass.

## Recommendations (not implemented)

Check all text/background pairs, text scaling and Arabic shaping; remove credentials from publicly shared comments in a later approved fix.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
