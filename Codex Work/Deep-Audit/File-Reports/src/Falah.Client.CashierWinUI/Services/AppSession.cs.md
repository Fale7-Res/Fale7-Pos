# src/Falah.Client.CashierWinUI/Services/AppSession.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `91ef44c7f825401a9afd3fab7e7a89869b1c1741877b47bda1da72179cdd1f2b`

Worktree SHA-256: `820f17c87a3c0f935632f7ecbaa53574045a8b48c883df392a688674ccb35086`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Logged-in user, shared shift and per-user in-memory cashier drafts.

## Source paths and function/resource analysis

- `AppSession` at `src/Falah.Client.CashierWinUI/Services/AppSession.cs:8`: Uses FALAH_SERVER_URL with localhost:5151 fallback, no secret connection string; HttpClient behind ApiClient is process lifetime.

- `GetCashier` at `src/Falah.Client.CashierWinUI/Services/AppSession.cs:22`: Caches one view model per user ID, preserving drafts through locking within same process. All cached VMs read mutable shared session.CurrentUser dynamically, so async callbacks spanning login boundaries require guards.

- `HasUnresolvedOperation` at `src/Falah.Client.CashierWinUI/Services/AppSession.cs:12`: Combines management owner set and draft gates; blocks close/lock for unresolved financial states but not ordinary unsaved drafts. No persisted primary financial DTO retrieval.

- `SetManagementOperationPending` at `src/Falah.Client.CashierWinUI/Services/AppSession.cs:16`: HashSet owner registration plus event; UI-thread assumption, no synchronization. Correct removal requires every management code path finally refresh.

- `HasPermission` at `src/Falah.Client.CashierWinUI/Services/AppSession.cs:33`: Exact permission or all from login summary; client UI filtering is not a security authority. Server must bind actor authorization independently.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

WindowClosingSafetyTests directly toggles busy booleans/management pending; does not reconstruct all concrete recovered screens.

## Recommendations (not implemented)

Use explicit operation owner and immutable logged-in identity across awaits; bind recovered financial state to correct operator.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
