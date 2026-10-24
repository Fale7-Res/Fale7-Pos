# src/Falah.Client.Cashier/MainWindow.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `e04fad78c7de6f4c759cc4ecba0b70be8c02f3f6bcff81e97011a24a0ecebbf9`

Worktree SHA-256: `477928047643d3dfb96685c107e198710dbb642951111f5d0f98844e1ef200a3`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: WPF technical-reference window initialization.

## Source paths and function/resource analysis

- `MainWindow` at `src/Falah.Client.Cashier/MainWindow.xaml.cs:16`: InitializeComponent then new MainViewModel DataContext; no Closing gate, timer stop, or explicit event unsubscribe here. Actual WPF root model lifecycle requires separate review before leak/safety conclusions.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

NOT_APPLICABLE business-unit test; actual WPF lifecycle not executed.

## Recommendations (not implemented)

Mark reference UI clearly in deployment docs; do not infer native WinUI behavior from WPF.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
