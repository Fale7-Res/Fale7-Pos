# src/Falah.Client.CashierWinUI/ViewModels/CustomersViewModel.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `34f40c8a33b06006f4cdf3eda3acfc6cfb70012496e912dd8eac11cef2b797f5`; worktree SHA256 `34f40c8a33b06006f4cdf3eda3acfc6cfb70012496e912dd8eac11cef2b797f5`; LF-equivalent `True`.

Actual role: Phone-only read-only customer and saved-address lookup.

## Function/resource review

- `SearchAsync` at `src/Falah.Client.CashierWinUI/ViewModels/CustomersViewModel.cs:52`: Synchronous IsSearching guard, trims phone and clears result/addresses.404 means legitimate not found, other failure surfaced; orders saved addresses by last used. Source does not claim browsable directory.

- `AwaitingFirstSearch` at `src/Falah.Client.CashierWinUI/ViewModels/CustomersViewModel.cs:38`: Separates initial prompt from no-match and error. Notifications HasSearched/IsSearching/SearchError only update initial state, finally explicitly notifies NotFound, correct after response.

- `PhoneQuery` at `src/Falah.Client.CashierWinUI/ViewModels/CustomersViewModel.cs:55`: Can change during search; request captures string but response not checked against current query. No overlapping commands, but text can display B while customer A results shown if edited while loading. Less harmful read-only directory variant of UI-007; native field disabled state must be inspected.

## Tests and limitations

No PII accessed. Need delayed lookup/edit query and no-match/error tests; no SQL or write operations here.

## Recommendations (not implemented)

Freeze query or snapshot/display searched phone with result, discard stale response if editable query changed.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
