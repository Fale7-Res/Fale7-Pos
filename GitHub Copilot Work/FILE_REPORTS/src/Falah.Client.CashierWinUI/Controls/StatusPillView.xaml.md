# src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `766db3fa03fe23cf47c6117c4ecc5e629bdd58923e5b0606a33ba9df8d485513`

Worktree SHA-256: `766db3fa03fe23cf47c6117c4ecc5e629bdd58923e5b0606a33ba9df8d485513`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Shared status chip border/text.

## Source paths and function/resource analysis

- `Label` at `src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml:14`: StatusPillText fixes foreground White while dark semantic backgrounds are bright Success #34D399/Warning #FBBF24/Danger #F87171; contrast can be low. Source contrast issue, rendered/screen-reader state not verified. Border receives imperative brush from code-behind.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing per-status/theme contrast checks; no native image supplied.

## Recommendations (not implemented)

Pair status text foreground with background theme tokens and accessible textual state.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
