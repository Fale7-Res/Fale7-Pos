# src/Falah.Client.CallCenterWinUI/Services/AppSession.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `ade21bda88fdb6bcc6995c8dd8c3d2e5d29d6d099499f51eee20f9c9750e2694`

Worktree SHA-256: `ade21bda88fdb6bcc6995c8dd8c3d2e5d29d6d099499f51eee20f9c9750e2694`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: CallCenter session and per-user retained order desks.

## Source paths and function/resource analysis

- `AppSession` at `src/Falah.Client.CallCenterWinUI/Services/AppSession.cs:6`: Uses FALAH_SERVER_URL fallback localhost:5151; no local DB connection or drawer owner. Permission values from real login DTO.

- `GetOrderDesk` at `src/Falah.Client.CallCenterWinUI/Services/AppSession.cs:27`: Caches drafts per logged-in ID and captures owner at construction; preserves during lock in-process.

- `HasUnresolvedOperation` at `src/Falah.Client.CallCenterWinUI/Services/AppSession.cs:38`: Checks draft edit gates, preventing normal lock/close with unknown order. Power failure still depends on journal integrity.

- `RefreshCurrentShiftAsync` at `src/Falah.Client.CallCenterWinUI/Services/AppSession.cs:46`: Reads server active shift, clears state on any failure. Return false conflates no open shift with server connectivity/authorization failure, leads misleading no-shift UI.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

CallCenterDurabilityTests relevant; explicit no-shift vs network failure test missing in reviewed set.

## Recommendations (not implemented)

Preserve API status/error for operator explanation while withholding checkout; safe durable owner scope already implemented.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
