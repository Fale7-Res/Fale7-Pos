# src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `4d17c848e22bc8ee801ce2963109e5280c1faa97b876830dddb85b2310e4eb15`

Worktree SHA-256: `4d17c848e22bc8ee801ce2963109e5280c1faa97b876830dddb85b2310e4eb15`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Status text DP and imperative status-color update.

## Source paths and function/resource analysis

- `OnTextChanged` at `src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs:9`: Copies raw backend state string and computes brush only when Text changes; live theme change alone never recomputes Background UI-004. No localized state label mapping in this control: call sites may preconvert.

- `StatusPillView` at `src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs:6`: Initializes components; default empty Text does not trigger callback, so no default background until actual value update.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing unchanged-status live-theme test; converter excluded from net8 headless build.

## Recommendations (not implemented)

Theme-aware visual state resources or ActualThemeChanged refresh; distinguish raw status vs localized label for color matching.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
