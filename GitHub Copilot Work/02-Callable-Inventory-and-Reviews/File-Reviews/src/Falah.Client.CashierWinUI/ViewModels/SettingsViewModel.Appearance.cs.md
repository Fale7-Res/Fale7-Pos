# src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.Appearance.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `41aa18460e93e1f8f147e979eda55d969a3142a81f6d8155d1d2b48e73c590c0`

Worktree SHA-256: `41aa18460e93e1f8f147e979eda55d969a3142a81f6d8155d1d2b48e73c590c0`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Immediate local theme preference binding.

## Source paths and function/resource analysis

- `InitializeAppearance` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.Appearance.cs:28`: Matches persisted theme against three ThemeOptions using First. Undefined parsed numeric AppTheme would throw; see ThemeService candidate.

- `OnSelectedThemeChanged` at `src/Falah.Client.CashierWinUI/ViewModels/SettingsViewModel.Appearance.cs:33`: Applies ThemeService.Set immediately independent of server-save workflow. It is still reachable through settings management screen; no current direct kiosk appearance selector identified.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Excluded from headless test build; native appearance fixtures needed.

## Recommendations (not implemented)

Validate persisted theme robustly; verify dark/light unselected buttons and already-realized converter brushes.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
