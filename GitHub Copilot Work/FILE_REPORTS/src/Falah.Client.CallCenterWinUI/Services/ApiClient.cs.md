# src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `0714593ee3ceafda4b48658f6c7b61e95d0870e5c410117f79fae5dfcc95258d`; worktree SHA256 `5702006b3b43ba4cd7242080292fa2ce6c4b232f6db1a85ad04e93bf94d277db`; LF-equivalent `True`.

Actual role: CallCenter typed HTTP transport and bearer-token lifecycle.

## Function/resource review

- `ApiClient` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:23`: Injected HttpClient supported for fixtures. Default has10-second timeout and localhost:5151 URI; AppSession overrides from FALAH_SERVER_URL. HttpClient retained process-wide without IDisposable; acceptable reuse but owner lifetime unclear.

- `ProductImageUri` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:36`: Encodes product and image version; ignores unknown image version by returning null. Bitmap downloads happen outside typed authenticated HTTP (same URL may be intentionally anonymous server; verify authorization server-side).

- `PingHealthAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:41`: Response disposed; catch all including cancel returns false, health callers cannot distinguish explicit stop/offline. HTTP health says reachable, not full workflow healthy.

- `ReadResultAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:56`: Success requires nonnull deserialized JSON; empty/malformed success interpreted unknown by SendAsync. Failure reads error JSON best-effort, status>=500 flagged unknown. Auth failures fallback invalid PIN text even outside login.

- `SendAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:84`: Catches transport/cancellation/JSON/IO failures as unknown, retains status0; sends GET reads through same diagnostic as writes. No automatic retry; callers own idempotency. bool endpoints would reject empty success because no SendEmpty adapter exists here, but currently typed calls predominate.

- `LoginByPinAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:115`: Authenticates existing endpoint, installs bearer DefaultRequestHeaders and maps actual returned identity/permissions. No hardcoded account in client; no PIN/token values exposed by this review.

- `LogoutAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:133`: Calls server endpoint then clears bearer even on failure; returned HttpResponseMessage not disposed and status ignored. MainViewModel.Lock does not call this function, so implemented revoke is unused in ordinary lock path (parent security lead notified).

- `GetMenuAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:142`: Read menu via GET. GetCurrentShift/GetSettings/GetTaxRules/GetActiveDeliveryServices/GetTreasuryBalance GET actual server state; read failure text misclassifies uncertainty as money operation.

- `CheckoutAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:162`: POST /api/orders/checkout; Refund/ConfirmElectronic/CancelPending POST named typed endpoints, no client SQL or direct ledger changes. All pass cancellation through.

- `OpenShiftAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:147`: POST open; PrepareShiftClose/CloseShift POST real endpoints even though CallCenter UX does not offer these actions. Exposed class method is not public server authorization bypass.

- `LookupOrderByNumberAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:187`: GET escaped number; GetOrder encoded path. IssueManagerApproval POST PIN request, AddExpense POST typed expense (methods copied for shared vocabulary, not all active CallCenter UI).

- `CreatePrintJobsAsync` at `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:233`: POST order print-jobs with encoded path and full request; does not override IncludeKitchenRoutes so CallCenter caller omission propagates UI-012. SafeDrop/CashierCashMovement/OpenCashDrawer also typed POST; no native printing here.

## Tests and limitations

CallCenterDurabilityTests fake HTTP handler can validate exact request IDs/body. Need response disposal, login/logout lifecycle, offline read diagnostic, non-JSON success, permission status and cancellation cases. No external endpoint requests executed.

## Recommendations (not implemented)

Invoke actual logout/revoke on lock if required contract; unify safe read/empty-success semantics and cancellation without hiding unknown writes; restrict UI without treating client methods as authority.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
