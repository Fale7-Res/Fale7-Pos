# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `48a28a7c8351ffd0ec599ee599ea5d5de2495a09e8a0f238e515410e305149bd`

Worktree SHA-256: `9826b8fc7ac4c90e688e59bccf35b9b47ec0d9eeca8e79db22aed5c47bf6675d`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Cross-partial busy gates, displayed financial estimates, session initialization.

## Source paths and function/resource analysis

- `IsOperationBusy` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:19`: Includes all financial writes, excludes read-only order search/cash count/customer lookup. UI busy/read overlap is possible but does not itself prove ledger corruption.

- `CanEditOperation` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:22`: Blocks for every unresolved private field and both durable electronic requests. Combining durable requests with absent result context makes electronic recovery dead end UI-005.

- `NotifyTotals` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:74`: Notifies tax/fees/discount/comp/tender and checkout availability; noncash/COD amount follows new total only while editable.

- `InitializeAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:87`: Discovers real active shift; tolerates 404, reports other failure. Settings failure is silently ignored; when shift open loads menu. No database access or auto-created mock data.

- `RefreshOperationState` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:9`: Raises edit/tender/confirm/cancel/checkout signals and header refresh; does not notify IsOperationBusy itself, a potential binding freshness concern where directly bound.

- `ConfirmClearCart` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs:115`: Only clears editable order after confirmation. Cart draft and pending operations are distinct concepts.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing durable electronic startup reconstruction test and IsOperationBusy binding notification assertions.

## Recommendations (not implemented)

Separate retry availability from transient visible order result; notify every bound derived state consistently.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
