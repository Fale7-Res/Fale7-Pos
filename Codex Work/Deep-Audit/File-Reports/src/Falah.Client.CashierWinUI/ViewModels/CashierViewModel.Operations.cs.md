# src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`

SHA-256 (exact Git blob bytes): `b16994d5a208ffb06499671967fc9ff46f908b41863aa8c7160eaf63dffa19fa`

Worktree SHA-256: `e4b4d4257703cbe135a8c821bb2075c7eb6d8b6e31dcb18622b0c8968fd38a06`; normalized LF equivalence: `True`. EOL differences alone are not source tampering.

Type: `.cs`; review: **REVIEWED_SOURCE**; runtime/visual proof: **NOT_TESTED by this reviewer**.

Actual role: Safe drop, drawer transfer/open, live balance, order lookup, receipt reprint and manager approval.

## Source paths and function/resource analysis

- `SubmitSafeDropAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:49`: Validates positive money and reason, obtains server approval when permission absent; CompleteSafeDropAsync retains request/ID only in memory. Source-side no arithmetic ledger mutation.

- `SubmitCashMovementAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:118`: Uses current movement type, positive amount, current actor/shift and approval token. CompleteCashMovementAsync rotates ID only on success and shares UI-001 durability gap.

- `SubmitCashDrawerAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:185`: Records request to open drawer with reason/approval; code explicitly does not claim physical drawer opened. CompleteCashDrawerAsync uses in-memory retention. Real agent outcome must be checked elsewhere.

- `OpenCashCountAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:225`: Read-only treasury balance fetch resets nullable value first; permission guard uses CanViewDrawerBalance. No epoch/cancellation guard prevents late read updating a closed modal, though it does not move money.

- `SearchOrderAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:276`: Requires lookup number and no unresolved reprint; clears result, fetches details, displays server errors. IsSearchingOrder is not included in IsOperationBusy, so concurrent repeated searches can return out of order.

- `ReprintOrderAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:306`: Receipt only with IncludeKitchenRoutes false, correct avoidance of duplicate cooking. Stable request on ambiguous retry but memory-only across termination. Result says queued, not printed. Guards only own IsReprinting and lookup context.

- `ResolveManagerApprovalIfNeededAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:70`: Uses real approval issue endpoint; never trusts a typed approver ID as proof. Comment above still describes obsolete two-step login, while implementation is one call.

- `CompleteSafeDropAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs:52`: Analogous completion helpers preserve ambiguity after later rejection and only close on successful DTO; calls RefreshOperationState before/after API. No disk persistence in any completion helper.

## Responsibilities and limits

Client source does not establish server authorization or database atomicity; cross-file backend review is separate. No application commands, database writes, printer actions, credential entry, source changes or dependency updates were executed by this reviewer. Static absence of a fault is not proof of correctness. Security exploitability is not inferred merely from a client-supplied actor ID.

## Tests and gaps

Missing crash recovery for concrete operations and late order lookup ordering. No physical printer tests executed.

## Recommendations (not implemented)

Unify durable typed operation storage and per-owner recovery. Introduce read epochs/cancellation for lookup, and whole-operation guards where concurrent modal actions can overlap.

## Coverage qualification

Anchored paragraphs describe actual manually traced functions/resources, not semantic declaration enumeration. Generated toolkit methods/properties are compiler-generated and must be accounted for by the parent inventory. Other applicable file dimensions without evidence remain UNKNOWN; printing and DB transaction implementation inside a pure view/resource file are NOT_APPLICABLE. A partial file report must not be counted as exhaustive.
