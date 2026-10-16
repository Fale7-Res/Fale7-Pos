# src/Falah.Client.CashierWinUI/Views/CustomersView.xaml

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `4d204d42de6e180722e2639e6bd6424a4ea1d54514ee6519d12289e483591477`; worktree SHA256 `6473eea3e9456dd17a7c42a7045722f5e10f15f2f4c0d02d6049beed952bc007`; LF-equivalent `True`.

Actual role: Phone identity lookup and saved customer address read.

## Function/resource review

- `PhoneQuery` at `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml:29`: Immediate two-way phone search, input remains editable during IsSearching; VM generation check required to prevent stale customer under new query. Search input TelephoneNumber helps keypad, not normalization.

- `Customer.Phone` at `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml:41`: Result card displays actual result phone and name, allowing operator to detect mismatch, unlike only query heading. No write buttons or list-all invented API.

- `Addresses` at `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml:60`: Area150, address star, service160; address concatenates street/building/landmark but omits floor/apartment and notes despite saved delivery address having them. Read view is incomplete for distinguishing units, not database loss.

- `ZeroCountToVisibleConverter` at `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml:85`: Empty address binds Count for updates correctly. HasCustomer gates both card/table; loading/error/notfound/initial instructions separately bound.

- `AwaitingFirstSearch` at `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml:105`: Instruction promises saved addresses and order history, but no order history section or binding exists in this file. Source UI copy promises unavailable content; backend history may exist elsewhere, not proof missing system component.

- `SearchCommand` at `src/Falah.Client.CashierWinUI/Views/CustomersView.xaml:31`: Retry explicit and error wraps. Vertical scroll accommodates long results; eager addresses bounded by one customer but unbounded count contract unknown.

## Tests and limitations

NOT_EXECUTED. Need delayed phone query race, same-building multiple unit records, unavailable customer, empty addresses and native screen reader field names. No callable declarations.

## Recommendations (not implemented)

Show full address identity and align instructional promise with visible functionality; guard async query result in VM.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
