# src/Falah.Client.CashierWinUI/App.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `f593c13c1a64b9df886526c990093dd49c327498c7aec1e059ffe9e490c6a2c1`

Worktree SHA-256: `0765214707a8c3d4e0f07b0feff9cb4f5c7bb278e666486088a51be222014dde`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: WinUI application launch and journal initialization.

## Source paths and function/resource analysis

- `App` at `src/Falah.Client.CashierWinUI/App.xaml.cs:5`: Initializes XAML resources then journal directory before view model field initializers; no onError handler means subsequent persistence degradation silent. Directory creation can throw on launch.

- `OnLaunched` at `src/Falah.Client.CashierWinUI/App.xaml.cs:29`: Normal path creates/activates MainWindow and stores global handle. FOOTER_LAYOUT_PROBE/SCREEN_LAYOUT_PROBE compile-time paths replace actual app with verification fixture, not production hidden timer/backdoor.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Compile-time fixture paths are not normal runtime coverage. No global unhandled-exception recovery observed in this file.

## Recommendations (not implemented)

Add diagnostic startup path for journal unavailability; retain clear distinction native fixtures vs live runtime verification.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
