# src/Falah.Client.CashierWinUI/MainWindow.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `16c8f0f4a007bd2973e7a1a5ddbde64f0c75e188cc5360dcc6700757533d1c7f`

Worktree SHA-256: `42992f5b0c16a801fa6671035e2600a5ec3abf06c48c7b836b30f68cf03c4558`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Native window composition, lifecycle, root theme and login/view swaps.

## Source paths and function/resource analysis

- `MainWindow` at `src/Falah.Client.CashierWinUI/MainWindow.xaml.cs:10`: Attaches close cancel to root TryClose, Closed unsubscribes root/appearance events and stops timers. Header data contexts assigned before maximizing.

- `OnThemeChanged` at `src/Falah.Client.CashierWinUI/MainWindow.xaml.cs:24`: Dispatches root RequestedTheme update, but does not invalidate converter-backed brush bindings or StatusPill imperative backgrounds UI-004.

- `MaximizeOnTargetDisplay` at `src/Falah.Client.CashierWinUI/MainWindow.xaml.cs:43`: Uses native presenter maximize so physical-pixel/DIP resize mismatch avoided. No guarantee narrow high-scale content fits all fixed modal widths.

- `OnViewModelPropertyChanged` at `src/Falah.Client.CashierWinUI/MainWindow.xaml.cs:23`: Routes root IsLoggedIn/IsOnline/CurrentView updates, unsubscribed on close.

- `ApplyCurrentView` at `src/Falah.Client.CashierWinUI/MainWindow.xaml.cs:40`: Creates new view per VM type; DataContext inherited through children. Unknown view nulls content visibly rather than fallback error. No direct DB/printing.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Headless tests do not compile native view. Real lifecycle/theme/modal/keyboard observations needed.

## Recommendations (not implemented)

Refresh all imperative theme visuals and ensure proper unloaded subscriptions in nested ShellView; maintain final authoritative checks server-side.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
