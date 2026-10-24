# src/Falah.Client.Cashier/Services/ApiClient.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0650e78e3cc745166f1f8894143cb6f930ec4c403a1d9002fb97804b9e99dd74`; worktree SHA256 `0650e78e3cc745166f1f8894143cb6f930ec4c403a1d9002fb97804b9e99dd74`; LF-equivalent `True`.

Actual role: Legacy WPF typed HTTP adapter without session-token contract.

## Function/resource review

- `ApiClient` at `src/Falah.Client.Cashier/Services/ApiClient.cs:18`: Fixed default localhost5151with10sectimeout; no injection constructor, environment setting or bearer token storage. Login deserializes summary shape but ignores server Token; later protected requests lack Authorization and likely401under actual middleware (parent server corroboration).

- `PingHealthAsync` at `src/Falah.Client.Cashier/Services/ApiClient.cs:27`: Catchall false, response not disposed. All other wrappers also leave HttpResponseMessage undisposed; process-lifetime HttpClient reused.

- `ReadResultAsync` at `src/Falah.Client.Cashier/Services/ApiClient.cs:42`: 2xx reads JSON and returns true even nullValue; failures best-effort error/status generic. No transport outcomeUnknown classification or exception normalization; caller exceptions can escape async commands.

- `LoginByPinAsync` at `src/Falah.Client.Cashier/Services/ApiClient.cs:71`: POST actual PIN request but no token install. Menu GET/OpenShift/Checkout/Refund/ConfirmElectronic POST, Lookup escaped query/GetOrder encoded ID all real endpoints. No direct DB/printer.

- `CheckoutAsync` at `src/Falah.Client.Cashier/Services/ApiClient.cs:89`: No automatic retry; caller builds mutable same-ID request with no frozen payload. All cancellation forwarded if caller supplies, VMs mostly default.

## Tests and limitations

No legacy live runtime or server request executed. Need authenticated menu checkout contract if reference kept supported; not active WinUI issue.

## Recommendations (not implemented)

Mark reference unsupported until token contract updated; dispose response resources and use authoritative active API result semantics in approved maintenance.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
