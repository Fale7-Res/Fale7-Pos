# src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `e70dba7108106dc5dfd4929ae0c837e4d0443b3c4d2ddb3c88590446cfdf4f65`

Worktree SHA-256: `e70dba7108106dc5dfd4929ae0c837e4d0443b3c4d2ddb3c88590446cfdf4f65`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Header lock event bridge.

## Source paths and function/resource analysis

- `AppHeaderView` at `src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml.cs:6`: InitializeComponent only; header has HeaderViewModel DataContext, so cannot directly find root lock command.

- `OnLockClicked` at `src/Falah.Client.CallCenterWinUI/Views/AppHeaderView.xaml.cs:18`: Raises LockRequested to MainWindow where MainViewModel.LockCommand enforces unresolved-operation gate. Event notification itself does not authorize state change; current wire exists in MainWindow. No direct data or printing side effects.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Native event wiring and root lock gate tests separate; no credential automation executed.

## Recommendations (not implemented)

Maintain root authorization/unknown-operation gate and accessible lock name in paired XAML.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
