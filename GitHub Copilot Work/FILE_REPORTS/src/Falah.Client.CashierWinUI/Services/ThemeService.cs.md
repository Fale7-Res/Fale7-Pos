# src/Falah.Client.CashierWinUI/Services/ThemeService.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `283b346b238c9c8fff8b920cfab3c2507a2590ff0a8a1372d737a8b1d6ed31c8`

Worktree SHA-256: `283b346b238c9c8fff8b920cfab3c2507a2590ff0a8a1372d737a8b1d6ed31c8`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Persisted process-wide Light/Dark/System appearance preference.

## Source paths and function/resource analysis

- `ThemeService` at `src/Falah.Client.CashierWinUI/Services/ThemeService.cs:24`: Loads LocalApplicationData/Falah/preferences/cashier-appearance.json. No database or printer side effects.

- `Set` at `src/Falah.Client.CashierWinUI/Services/ThemeService.cs:46`: Serializes preference changes under lock; applies memory state even if disk save fails. Changed runs outside lock, subscriber thread is caller thread, MainWindow dispatches safely.

- `ToElementTheme` at `src/Falah.Client.CashierWinUI/Services/ThemeService.cs:18`: Maps unknown enum to Default; supports system preference via root inheritance.

- `Load` at `src/Falah.Client.CashierWinUI/Services/ThemeService.cs:43`: Falls back on corrupt file, but Enum.TryParse accepts undefined numeric values. SettingsViewModel.InitializeAppearance First() then can throw for undefined persisted enum; plausible local corruption bug.

- `Save` at `src/Falah.Client.CashierWinUI/Services/ThemeService.cs:52`: Temp/move protects JSON whole-write, swallows all exceptions with no logging; preference persistence degrades silently. No interprocess coordination.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Excluded from net8.0 headless tests by project file. Live theme/resource verification needed; no screen images supplied to this reviewer.

## Recommendations (not implemented)

Validate Enum.IsDefined and expose persistence failures only if material; verify converted brushes after live theme switches.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
