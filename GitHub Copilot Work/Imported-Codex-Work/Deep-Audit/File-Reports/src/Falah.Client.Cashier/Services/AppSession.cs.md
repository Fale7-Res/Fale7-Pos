# src/Falah.Client.Cashier/Services/AppSession.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `78f3827a4f5eed2126bef903204824b8bc3973aca3da12b079a30695c65e1d37`

Worktree SHA-256: `78f3827a4f5eed2126bef903204824b8bc3973aca3da12b079a30695c65e1d37`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: WPF reference shared session.

## Source paths and function/resource analysis

- `Api` at `src/Falah.Client.Cashier/Services/AppSession.cs:10`: Constructs default ApiClient directly; unlike WinUI does not read FALAH_SERVER_URL at this session constructor. No per-user drafts or unresolved-operation owner gate.

- `HasPermission` at `src/Falah.Client.Cashier/Services/AppSession.cs:15`: Exact permission or all based on mutable login summary; server remains final authority. Shift/user properties in memory only.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

No active-client tests cover WPF source in current csproj.

## Recommendations (not implemented)

Prevent accidental reference deployment; parameterize backend and operation state if reference remains supported.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
