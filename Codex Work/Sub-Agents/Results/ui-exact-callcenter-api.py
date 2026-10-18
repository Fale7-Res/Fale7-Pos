from ui_source_receipts import *
facts={
(26,5):('Retains injected HttpClient without setting base URL/timeout or owning disposal. Fixture client may differ from production ten-second constructor; tests cannot assume identical token lifetime/network handling.','injected HttpClient','Missing BaseAddress/authorization inherited client fixture.'),
(28,5):('Creates HttpClient with supplied URI and10second timeout, default HTTP localhost5151. No TLS enforcement, URI trim or certificate override. LAN credential confidentiality belongs deployment/security coordinator.','new Uri; new HttpClient','Empty/malformed environment URI and HTTPS deployment requirements.'),
(36,5):('Returns null without version/base address, otherwise escaped product ID/version absolute-root image URL. Native Image own fetch does not automatically share this HttpClient bearer; endpoint accessibility/caching must match server policy.','Uri.EscapeDataString; new Uri','Authenticated image fetch behavior native/server challenge.'),
(41,5):('Health GET disposes response and returns HTTP status success, catches every exception including cancellation as offline. Does not validate database readiness or auth permission.','HttpClient.GetAsync /health','Stop/late response and DB unavailable while health200.'),
(56,5):('Success JSON deserializes typed value; null body becomes unknown with status0, malformed JSON caught by wrapper. Non-success tries error JSON then generic401/404/status; all5xx unknown,4xx definite. Error-body catch swallows cancellation and malformed response as generic failure.','ReadFromJsonAsync; ErrorBody','204 typed endpoint and5xx/cancellation classification.'),
(84,5):('Single SendAsync wrapper serves GET and POST, disposing response and mapping transport/cancel/JSON/IO errors to unknown-operation message. Unlike Cashier ReadAsync, read-only failures misleadingly suggest verifying potentially moved money. No auto retry or broad catch of InvalidOperationException.','ReadResultAsync; HttpRequestException/OperationCanceledException/JsonException/IOException filter','GET offline copy versus POST unknown safety; missing base address can escape.'),
(115,5):('Posts PIN to login, then on successful DTO replaces shared bearer and token before returning UserSummary. Failed login leaves previous bearer untouched. No CurrentUser assignment here; LoginVM performs separately, so deniedpos route can retain token.','SendAsync<LoginResultDto>; Authorization header','Two-user stale response and failure retaining old token; no live auth.'),
(118,13):('Captures PIN into LoginByPinRequest JSON and cancellation token, using current shared client (possibly old Authorization header). No token logged or persisted in this code.','HttpClient.PostAsJsonAsync /api/auth/login-by-pin','Dedicated login endpoint must ignore mismatched old bearer appropriately.'),
(133,5):('If private token absent returns without clearing externally injected header. Otherwise tries server logout, swallows all failure then clears local token/header. HttpResponseMessage from PostAsync not disposed; root Lock never calls this path.','HttpClient.PostAsync /api/auth/logout; Authorization=null','Token revocation/root lock discrepancy security parent-owned; response disposal low impact.')
}
pairs=[
(97,98,36,'GET /api/shifts/current','Reads active shift;404 legitimate none handled AppSession. Uses command wrapper even for read.'),
(99,100,72,'GET /api/tax-rules','Reads tax catalog for preview; no local selection of active rule at transport.'),
(101,102,59,'GET /api/settings','Reads restaurant/default settings; no server configuration update.'),
(103,104,60,'GET /api/customers/lookup?phone=escaped','Escapes phone query, but no response generation check; UI007 belongs CallCenter caller.'),
(105,106,60,'POST /api/customers/upsert','Writes captured customer/address DTO, with unknown result but no transport-generated idempotency key.'),
(142,144,46,'GET /api/menu','Reads menu and offers; stale snapshot repricing authoritative at checkout.'),
(147,149,49,'POST /api/shifts/open','API surface exists despite CallCenter station never owning shift; unused surface not evidence actual station opens shift.'),
(152,154,57,'POST /api/shifts/prepare-close','Prepare-close API exists but current station flow does not call it; do not confuse dependency surface with active feature.'),
(157,159,50,'POST /api/shifts/close','Forwards closing DTO unchanged; active CallCenter deployment relies external cashier shift instead.'),
(162,164,53,'POST /api/orders/checkout','Forwards immutable order snapshot and request ID; no local SQL, automatic retry or durable intent in transport.'),
(167,169,51,'POST /api/orders/refund','Refund transport surface exists; active frontend may not expose workflow, backend still authorizes every actor.'),
(172,174,64,'POST /api/orders/confirm-electronic-payment','Sends electronic confirmation DTO with actor unchanged; this does not make CallCenter receipt state paid locally.'),
(177,179,70,'POST /api/orders/cancel-pending-electronic-payment','Sends captured cancellation reason/order unchanged; unknown outcome must not be treated rollback.'),
(182,184,67,'GET /api/delivery/services','Reads active delivery options/fees; no guessed fee calculation.'),
(187,189,49,'GET /api/orders/lookup?orderNumber=escaped','Escapes public order number, reads authorized order snapshot; caller selection generation independent.'),
(192,194,49,'GET /api/orders/{escaped orderId}','Escapes internal order ID path; no payment or print side effect from this read.'),
(197,199,60,'POST /api/approvals/issue','Sends PIN to dedicated approval endpoint without replacing current bearer, unlike LoginByPin. No real PIN used during audit.'),
(202,204,50,'POST /api/expenses','Expense API inherited broad surface but unused by station intake; server permission/shift/account authoritative.'),
(209,211,48,'POST /api/treasury/safe-drop','Safe-drop transport forwards existing DTO/key; active station without drawer should not infer this screen exposed.'),
(214,216,59,'POST /api/cash-drawer/cash-movement','Posts drawer movement DTO; no physical or ledger mutation client-side.'),
(219,221,51,'POST /api/cash-drawer/open','Requests hardware command via server, not physical success. No printer/drawer call executed.'),
(228,230,52,'GET /api/treasury/balance?accountId=escaped','Escapes account query and reads balance, no count adjustment. Permission enforced server-side.'),
(233,235,55,'POST /api/orders/{escaped orderId}/print-jobs','Posts explicit print DTO without overriding IncludeKitchenRoutes; CallCenter UI012 omission inherits defaulttrue. Transport does not protect customer-only reprint automatically.')]
for line,ll,col,route,reason in pairs:
 facts[(line,5)]=(reason+' '+('Read still uses SendAsync unknown-operation messaging, unlike Cashier ReadAsync.' if route.startswith('GET') else 'Typed SendAsync preserves OutcomeUnknown for caller recovery.'),route+' -> SendAsync','Synthetic exact verb/path/body/token/cancellation contract; no server request.')
 facts[(ll,col)]=('Exact send lambda for '+route+'. '+reason+' Captures existing cancellation token and supplied DTO/query without generating replacement request identity.',route+' -> HttpClient.GetAsync/PostAsJsonAsync as exact body','Transport fixture only; no live credentials/business/physical output.')
persist_exact(C+'Services/ApiClient.cs',facts,'Read complete237-line CallCenter API independently of Cashier; all55 exact constructors/methods/send lambdas reconciled. Distinct shared GET/POST error wrapper recorded, no copied coverage inference.')
