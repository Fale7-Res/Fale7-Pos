# src/Falah.Client.CashierWinUI/Services/CashInput.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `541677785c462ffd6ac45bae742ffca263f0779ec0deb0dc42d3364bead7748e`

Worktree SHA-256: `541677785c462ffd6ac45bae742ffca263f0779ec0deb0dc42d3364bead7748e`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Strict localized EGP to integer piastre parsing.

## Source paths and function/resource analysis

- `TryParse` at `src/Falah.Client.CashierWinUI/Services/CashInput.cs:7`: Normalizes Arabic/Persian digits and Arabic decimal separator, invariant decimal without signs/exponents/group separators. Rejects negative, >long/100 and >2 effective fractional decimals; initializes out to zero before parse. Trailing zeros allowed when decimal value rounds identically, sensible for money.

- `Format` at `src/Falah.Client.CashierWinUI/Services/CashInput.cs:19`: Invariant two decimals from long/100m, avoids binary float loss. Formatting negatives possible for balances even though input parser rejects negative entry.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Boundary tests should include long.MaxValue, huge normalized strings, Arabic/Persian digits, decimal rounding rejection, empty and grouping separators. Existing exact assertions not yet inventoried.

## Recommendations (not implemented)

Retain integer financial units and explicit parse contract; no immediate correctness defect established.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
