# src/Falah.Client.Cashier/App.xaml

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `8e89cf9c15ddab2a036fef14bf98f3f6613c1fc7aaea7da07edd381f19967f5a`

Worktree SHA-256: `4c21917ba2dda4e16a733065c4e15212209cf603d443d564e93753c1f6d44dc0`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.xaml`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: WPF startup and resource/converter registrations.

## Source paths and function/resource analysis

- `StartupUri` at `src/Falah.Client.Cashier/App.xaml:6`: Creates MainWindow.xaml; merged Colors/Typography/Controls resources are WPF-specific, not active WinUI dictionaries.

- `NoteAppendPairConverter` at `src/Falah.Client.Cashier/App.xaml:25`: Converters registered for WPF XAML including note pair, money and online status. Registration alone not evidence binding usage correct; no implicit SQL, DB migration or printing.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

WPF not runtime-tested; separate XML parse by parent inventory.

## Recommendations (not implemented)

Separate reference/production deployment artifact names and smoke checks.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
