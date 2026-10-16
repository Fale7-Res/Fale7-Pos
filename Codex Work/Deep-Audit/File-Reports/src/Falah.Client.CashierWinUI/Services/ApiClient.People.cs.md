# src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `e7fa6464747cd2191c684e64d7c4f7c27af214702d91824dd4f2489f55e75822`; worktree SHA256 `001422f6a5bcc160cbd831b67346ae58ae93f3e54f3874a0a451ec59d43f4630`; LF-equivalent `True`.

Actual role: Employee/delivery-directory/cash-count/restore/dashboard API adapters.

## Function/resource review

- `CreateEmployeeAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:16`: POST employee typed DTO, UpdateEmployee PUT encoded ID; successful DTO deserialized. Manager/employee permissions are server responsibility.

- `GetAllDeliveryServicesAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:25`: GET includes inactive entries needed for reactivation. Create service POST, update PUT ID encoded. GET incorrectly uses SendAsync ambiguous-operation message.

- `GetCashCountDenominationsAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:40`: GET fixed denomination vocabulary; GetCashCounts GET encoded business day; SaveCashCount POST to business day counts typed DTO. Concrete count uses durable helper, validation in view model/server separate.

- `RestoreBackupAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:55`: POST encoded record ID restore with typed reason/phrase/actor approval request. Dangerous real DB operation reachable through existing server; no invocation in audit. DTO status issue UI-014 is consumer behavior, not this transport wrapper.

- `GetOwnerDashboardAsync` at `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:61`: GET one owner snapshot with encoded actor and ISO dates. Uses SendAsync for a read; no server-side date mutation implied. Cancellation forwarded all methods.

## Tests and limitations

Read-only network failure diagnostic tests absent in reviewed tests. EmptyResponse contract not relevant where typed DTO mandated. No restore or DB writes tested.

## Recommendations (not implemented)

Distinguish read failure text from unresolved writes; avoid client interpreting HTTP success as completed restore.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
