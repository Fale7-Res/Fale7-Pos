# src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `cc1b2d59f858eb45473b2bc5570724f8d92c91364734da85466fe572781344f9`

Worktree SHA-256: `a2b18699b786109662a0fd23e8660bb2a56b015e0c2f5582db14b9dbf4f06aad`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Read-only route/job status overview.

## Source paths and function/resource analysis

- `RouteQueue` at `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs:28`: Formats pending/failed/stuck counts and age, uses local machine time for last success. Describes recorded agent success, not proof paper physically emerged.

- `JobRow` at `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs:68`: Recognizes six routes including DeliveryOrder and DriverDispatch, displays error/attempts/status; LastError may contain sensitive backend details and needs server redaction check.

- `Routes` at `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs:118`: Only four routes are selectable/displayed as queue cards, omitting DeliveryOrder and DriverDispatch despite JobRow understanding them. UI-009 requires server cross-check for actual emitted route usage.

- `LoadAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs:138`: Busy reload flag coalesces changes, loads overview then route jobs; finally triggers pending reload. State properties update after completion. Infinite reload not intrinsically present; repeated external selection can schedule successive loads.

- `LoadJobsAsync` at `src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs:168`: Snapshots selected route and refuses stale response, correctly prevents race. No writes/retry printing function in this status VM.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing six-route overview/filter assertions; no printer or HTTP runtime checks by this reviewer.

## Recommendations (not implemented)

Include every real route and test failed/stuck visibility by actual route enum; keep queued vs printed distinction.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
