# src/Falah.Client.Cashier/App.xaml.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `6e2a3aa6ed04e4eef58c31936860cb6f9625a554233758ef375b5a642c3658eb`

Worktree SHA-256: `d43589d878cc50c15bc3ef7229382ca635b298e3a98503d85363763c8879ff18`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: WPF Application class declaration.

## Source paths and function/resource analysis

- `App` at `src/Falah.Client.Cashier/App.xaml.cs:8`: Empty System.Windows.Application partial; startup URI/resources live in App.xaml. No code hooks, timers or hidden startup process in this file. Fully qualified base prevents namespace collision.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

NOT_APPLICABLE application business logic; App.xaml/paired window separate.

## Recommendations (not implemented)

No immediate change required; preserve correct fully qualified base type.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
