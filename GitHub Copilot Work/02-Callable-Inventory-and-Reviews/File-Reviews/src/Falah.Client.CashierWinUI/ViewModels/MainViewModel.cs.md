# src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `fc52f6a6c70d81be46c2e9c771131ecbc5d41a8afc7b0be69654000ed29020b1`

Worktree SHA-256: `baee478e8a47af1722c65a058d327a2c26a50278d08b1c445008e249b7baa2b7`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Root login/shell navigation, health poll, close safety, recovered notice.

## Source paths and function/resource analysis

- `MainViewModel` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:12`: Creates shared session/header, login, snapshots pending journal list, starts five-second DispatcherTimer and immediate health call. No overlap guard: 10-second HTTP timeout permits several health calls at once.

- `DescribeRecoveredOperations` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:31`: Read-only notice uses owner keys/type metadata; corrupt journal List can hide valid notice, and generic instruction to open screen is not proof retry is reachable (UI-005).

- `CheckHealthNowAsync` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:34`: Updates only if not stopped; overlapping health completions can reorder online status. No cancellation token to stop in-flight HTTP reads.

- `TryClose` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:65`: Refuses unresolved operations, protects normal close only; power failure/process kill bypasses UI guard, requiring actual durability UI-001.

- `Stop` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:71`: Stops timers/header and ignores late health result; does not dispose ApiClient HttpClient or cancel pending loads.

- `Lock` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:82`: Only invokes ShowLogin if no unresolved op. Session CurrentUser remains until next success; view switch is lock UX, server request auth is elsewhere.

- `ShowMain` at `src/Falah.Client.CashierWinUI/ViewModels/MainViewModel.cs:88`: Filters shell by management permissions; creates reused cashier or role shell. CreateCashier starts InitializeAsync fire-and-forget; errors not mapped outside that method.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

WindowClosingSafetyTests covers explicit close gate only with fake timer; no power-cut/restart proof. DispatcherTimer stub cannot test real scheduling.

## Recommendations (not implemented)

Single-flight cancellable health poll; concrete recovered-operation retry navigation; durable requests before money send.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
