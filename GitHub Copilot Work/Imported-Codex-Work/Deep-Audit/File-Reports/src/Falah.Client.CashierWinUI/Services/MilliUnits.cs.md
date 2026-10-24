# src/Falah.Client.CashierWinUI/Services/MilliUnits.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `7bacd9ec6afd8fbf7b2e2dcfcbf176a79e9bf1f12a9e83aaa6893787a8c36efe`

Worktree SHA-256: `7bacd9ec6afd8fbf7b2e2dcfcbf176a79e9bf1f12a9e83aaa6893787a8c36efe`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Localized inventory quantity parser/formatter.

## Source paths and function/resource analysis

- `TryParse` at `src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:15`: Same normalization as CashInput at 1000 units, rejects sign/exponent/grouping and effective >3 decimals. Safe pre-multiplication long bound.

- `Format` at `src/Falah.Client.CashierWinUI/Services/MilliUnits.cs:29`: Uses decimal division and trims zeros to 0.###, correct for milli-unit precision; unknown unit semantic remains caller responsibility.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing independently verified inventory boundary/Arabic digit coverage in this checkpoint.

## Recommendations (not implemented)

Share tested normalization intentionally if later refactoring authorized; no change during audit.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
