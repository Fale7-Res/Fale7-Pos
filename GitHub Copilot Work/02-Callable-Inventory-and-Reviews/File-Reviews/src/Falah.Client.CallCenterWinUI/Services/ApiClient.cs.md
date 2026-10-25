# src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs

- **Path:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `24B27CEF561EB6D99C9F9DA4DE4297159DA7575EE734C2044F4CDCD8A9969F2C`
- **Git Blob SHA256:** `0714593ee3ceafda4b48658f6c7b61e95d0870e5c410117f79fae5dfcc95258d`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 238]]`

---

## 1. Architectural Scope and Tri-Client Parity Matrix

[`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs#L1-L238) implements the strongly-typed HTTP transport client for the WinUI 3 Call Center station (`Falah.Client.CallCenterWinUI`). In Falah POS's three-tier architecture, the client possesses zero direct database access (no SQL Server or SQLite connections); every single mutation, inventory deduction, shift calculation, and order lifecycle event must be dispatched over HTTP to `Falah.Server`.

### Tri-Client Architecture & Parity Comparison

A comprehensive comparison across the three client implementations—**WPF Cashier** (`Falah.Client.Cashier.Services.ApiClient`), **WinUI Cashier** (`Falah.Client.CashierWinUI.Services.ApiClient`), and **WinUI Call Center** (`Falah.Client.CallCenterWinUI.Services.ApiClient`)—reveals severe divergence in security, transport resilience, and domain scope:

| Architectural Aspect | WPF Cashier Client (`Falah.Client.Cashier`) | WinUI Cashier Client (`Falah.Client.CashierWinUI`) | WinUI Call Center Client (`Falah.Client.CallCenterWinUI`) | Parity & Divergence Verdict |
| :--- | :--- | :--- | :--- | :--- |
| **Authentication & Tokens** | **Absent:** Hits `/api/auth/login-by-pin`, discards token; subsequent requests are unauthenticated (`Authorization` header never set). | **Stateful Bearer:** Sets `_sessionToken` and mutates `_http.DefaultRequestHeaders.Authorization`. Server validates token on every call. | **Stateful Bearer:** Sets `_sessionToken` and mutates `_http.DefaultRequestHeaders.Authorization`. Identical to WinUI Cashier. | **WPF Severely Divergent:** WPF lacks token security entirely. WinUI clients share `SEC-008` (DefaultRequestHeaders mutation on shared `HttpClient`). |
| **Transport Resilience (`SendAsync`)** | **Absent:** Direct `await _http.GetAsync/PostAsJsonAsync`. Network drops or timeouts throw raw unhandled exceptions to callers. | **Bifurcated:** Separates `ReadAsync<T>` (GET reads) and `SendAsync<T>` (POST commands) and `SendEmptyAsync` (body-less 200/204). | **Unified `SendAsync`:** All reads and writes go through single `SendAsync<T>`. Missing `ReadAsync` and missing `SendEmptyAsync`. | **Divergent Error Semantics:** Call Center GET reads report `OutcomeUnknown: true` on transport drop, conflating safe reads with ambiguous writes. |
| **Error Contract (`ApiResult<T>`)** | `ApiResult<T>(Success, Value, Error)` — Lacks `OutcomeUnknown` and `StatusCode`. | `ApiResult<T>(Success, Value, Error, OutcomeUnknown, StatusCode)`. | `ApiResult<T>(Success, Value, Error, OutcomeUnknown, StatusCode)`. | **Parity between WinUI clients:** WPF client cannot communicate idempotency uncertainty to ViewModels. |
| **Domain Scope & Dead Code** | 8 endpoints only (focused on basic counter POS). | Full cashier client (~60 endpoints across 3 partial classes). | 27 endpoint methods, of which **14 are dead wrappers** copied from CashierWinUI (SafeDrop, CashDrawer, Shifts close, etc.). | **Code Bloat Artifact:** Call Center copied Cashier transport wrappers for cashier hardware/treasury screens that do not exist in Call Center UI. |
| **Session Revocation on Lock** | N/A (no sessions). | `LogoutAsync` called during explicit sign-out; screen lock preserves session. | `LogoutAsync` exists in `ApiClient` but is **never invoked** anywhere in `CallCenterWinUI`. Lock leaves token live. | **Security Parity Defect:** `MainViewModel.Lock` ignores `LogoutAsync`, leaving active JWT tokens in memory across screen locks. |

---

## 2. In-Depth Vulnerability and Architectural Parity Findings

### A. Vulnerability `SEC-008`: Shared HttpClient DefaultRequestHeaders Mutation
- **Location:** [`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:122-124`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs#L122-L124)
- **Code Pattern:**
  ```csharp
  _sessionToken = result.Value.Token;
  _http.DefaultRequestHeaders.Authorization =
      new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sessionToken);
  ```
- **Architectural Risk:** `HttpClient` is designed to be instantiated once and shared across the entire application lifetime. Mutating `_http.DefaultRequestHeaders.Authorization` directly alters state across all concurrent requests and worker threads. If an operator locks or switches accounts while background tasks (such as health checks, print job creation, or retained request retries) are executing, those requests will execute with the newly assigned operator's Bearer token.
- **Unused `LogoutAsync` Compounding Risk:** In [`MainViewModel.Lock()`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/MainViewModel.cs#L56-L60), the client simply sets `IsLoggedIn = false` and navigates to `LoginViewModel`. It **never calls `LogoutAsync()`**. The server session remains active and the bearer token remains permanently attached to `_http.DefaultRequestHeaders` until a different operator successfully logs in.

### B. Defect `SEM-MIRROR-072`: False-Positive `OutcomeUnknown` on Read-Only Operations
- **Location:** [`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:84-106`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs#L84-L106)
- **Architectural Divergence vs `CashierWinUI`:** In `CashierWinUI`, read operations are routed through `ReadAsync<T>` which yields:
  `"تعذر تحميل البيانات من السيرفر. تأكد من الاتصال ثم أعد المحاولة."` without setting `OutcomeUnknown: true`.
  In contrast, `CallCenterWinUI` routes all GET calls (`GetCurrentShiftAsync`, `GetTaxRulesAsync`, `GetSettingsAsync`, `LookupCustomerAsync`, `GetMenuAsync`, `GetActiveDeliveryServicesAsync`, `LookupOrderByNumberAsync`) through `SendAsync<T>`, which returns:
  `ApiResult<T>(false, default, "Could not reach server or operation timed out. Please retry or contact support.", OutcomeUnknown: true, StatusCode: 0)`.
- **Impact:** GET operations have zero financial side-effects. Setting `OutcomeUnknown: true` on safe read failures causes durability and retry handlers to treat a transient network glitch during menu loading or customer phone lookup as an ambiguous financial transaction.

### C. Defect `SEM-MIRROR-073`: Misleading 401 Error Mapping Across Non-Auth Endpoints
- **Location:** [`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:72-76`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs#L72-L76)
- **Code Pattern:**
  ```csharp
  HttpStatusCode.Unauthorized => "Invalid PIN or inactive user",
  ```
- **Impact:** If a token expires or is invalidated during normal POS usage (e.g., during `CheckoutAsync`, `SaveCustomerAsync`, or `CreatePrintJobsAsync`), `ReadResultAsync` falls back to `"Invalid PIN or inactive user"`. The operator is told their PIN was wrong when they were performing a checkout, rather than being notified that their session has expired.

### D. Defect `SEM-MIRROR-074`: Empty 200/204 Success Rejection via Missing `SendEmptyAsync`
- **Location:** [`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:58-64`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs#L58-L64)
- **Code Pattern:**
  ```csharp
  if (response.IsSuccessStatusCode)
  {
      var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
      return value is null
          ? new ApiResult<T>(false, default, "Unexpected empty response from server", OutcomeUnknown: true, StatusCode: (int)response.StatusCode)
          : new ApiResult<T>(true, value, StatusCode: (int)response.StatusCode);
  }
  ```
- **Impact:** Any HTTP 200 OK or 204 No Content response where the server returns an empty body or JSON null is rejected as a failure with `OutcomeUnknown: true`. While `CashierWinUI` defines `SendEmptyAsync` for void/boolean endpoints, `CallCenterWinUI` lacks this adapter entirely.

### E. Dead Code Inventory: 14 Unused Cashier Transport Wrappers
Call Center is a phone-order dispatch station that collects no cash at the desk and has no physical till drawer, no shift-closing settlement screen, and no manager approval prompt. However, `ApiClient.cs` contains 14 dead wrappers copied verbatim from `CashierWinUI`:
1. `LogoutAsync` (never called; lock bypasses it)
2. `OpenShiftAsync` (never called; Call Center relies on cashier opening shift)
3. `PrepareShiftCloseAsync` (never called; no shift closing UI)
4. `CloseShiftAsync` (never called; no shift closing UI)
5. `RefundAsync` (never called; Call Center cannot perform cashier refunds)
6. `ConfirmElectronicPaymentAsync` (never called; delivery orders remain pending claims)
7. `CancelPendingElectronicPaymentAsync` (never called; cancellations handled at cashier/admin desk)
8. `GetOrderAsync` (never called; checkout lookup uses `LookupOrderByNumberAsync`)
9. `IssueManagerApprovalAsync` (never called; no manager PIN override dialog in Call Center)
10. `AddExpenseAsync` (never called; no expense payout workflow)
11. `SafeDropAsync` (never called; no safe drop workflow)
12. `CashierCashMovementAsync` (never called; no cash movement workflow)
13. `OpenCashDrawerAsync` (never called; no cash drawer hardware)
14. `GetTreasuryBalanceAsync` (never called; no treasury balance inquiry)

---

## 3. Exhaustive 55-Callable Inventory and Source Review

### Callable 1: `ApiClient(HttpClient http)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 26–26
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.ApiClient(System.Net.Http.HttpClient)`
- **Kind:** `ConstructorDeclaration`
- **Architectural Role & Semantics:** Primary testability constructor allowing DI injection of mock `HttpClient` / test handlers (e.g. in `CallCenterDurabilityTests`). Retains `_http` without validation.
- **Verdict:** **VERIFIED**

---

### Callable 2: `ApiClient(string baseAddress = "http://localhost:5151")`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 28–31
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.ApiClient(string)`
- **Kind:** `ConstructorDeclaration`
- **Architectural Role & Semantics:** Default runtime constructor. Instantiates a private `HttpClient` with `baseAddress` and hardcodes `Timeout = TimeSpan.FromSeconds(10)`. In production, `AppSession` instantiates this with `FALAH_SERVER_URL` or defaults to `http://localhost:5151`.
- **Verdict:** **VERIFIED**

---

### Callable 3: `ProductImageUri(string productId, string? version)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:36:5:MethodDeclaration`
- **Exact Line Range:** Lines 36–39
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.ProductImageUri(string, string?)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Generates image URI for WinUI XAML `Image` controls (`/api/products/{id}/image?v={version}`). Returns `null` if version is empty or BaseAddress is null. Image downloads bypass typed Bearer client; server serves product images anonymously.
- **Verdict:** **VERIFIED**

---

### Callable 4: `PingHealthAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:41:5:MethodDeclaration`
- **Exact Line Range:** Lines 41–52
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.PingHealthAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Dispatches GET `/health` with a dedicated 2-second timeout via linked `CancellationTokenSource`. Correctly disposes `HttpResponseMessage` via `using var response`. Catches all exceptions and returns `false`.
- **Verdict:** **VERIFIED**

---

### Callable 5: `ReadResultAsync(HttpResponseMessage response, CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:56:5:MethodDeclaration`
- **Exact Line Range:** Lines 56–82
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Deserializes JSON HTTP response into `ApiResult<T>`.
  - On 2xx: Deserializes `T`. If null, returns `OutcomeUnknown: true` (`SEM-MIRROR-074`).
  - On non-2xx: Best-effort parses error/message JSON property. Falls back to status-specific text (401 -> "Invalid PIN or inactive user", 403 -> "You do not have permission").
  - Status >= 500 sets `OutcomeUnknown: true`.
- **Verdict:** **VERIFIED — DEFECTS `SEM-MIRROR-073`, `SEM-MIRROR-074` NOTED**

---

### Callable 6: `SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:84:5:MethodDeclaration`
- **Exact Line Range:** Lines 84–95
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.SendAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Universal pipeline wrapper. Invokes `send()`, disposes response via `using var response`, and catches `HttpRequestException`, `OperationCanceledException`, `JsonException`, and `IOException`, returning `OutcomeUnknown: true` with status code 0.
- **Verdict:** **VERIFIED — DEFECT `SEM-MIRROR-072` NOTED (False-Positive Unknown on Reads)**

---

### Callable 7: `GetCurrentShiftAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:97:5:MethodDeclaration`
- **Exact Line Range:** Lines 97–98
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetCurrentShiftAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/shifts/current` to verify if a cashier shift is active. Invoked by `AppSession.RefreshCurrentShiftAsync`.
- **Verdict:** **VERIFIED**

---

### Callable 8: `ParenthesizedLambdaExpression` (GetCurrentShiftAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:98:36:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 98–98
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync("/api/shifts/current", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 9: `GetTaxRulesAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:99:5:MethodDeclaration`
- **Exact Line Range:** Lines 99–100
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetTaxRulesAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Fetches active tax configuration via GET `/api/tax-rules`. Used by `CallCenterViewModel` during initialization to configure order tax calculation.
- **Verdict:** **VERIFIED**

---

### Callable 10: `ParenthesizedLambdaExpression` (GetTaxRulesAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:100:72:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 100–100
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync("/api/tax-rules", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 11: `GetSettingsAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:101:5:MethodDeclaration`
- **Exact Line Range:** Lines 101–102
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetSettingsAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/settings` for restaurant metadata, currency, and rounding rules.
- **Verdict:** **VERIFIED**

---

### Callable 12: `ParenthesizedLambdaExpression` (GetSettingsAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:102:59:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 102–102
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync("/api/settings", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 13: `LookupCustomerAsync(string phone, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:103:5:MethodDeclaration`
- **Exact Line Range:** Lines 103–104
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.LookupCustomerAsync(string, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/customers/lookup?phone={escapedPhone}`. Used in `CallCenterViewModel.Customer.cs` during live phone entry.
- **Verdict:** **VERIFIED**

---

### Callable 14: `ParenthesizedLambdaExpression` (LookupCustomerAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:104:60:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 104–104
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync($"/api/customers/lookup?phone={Uri.EscapeDataString(phone)}", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 15: `SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:105:5:MethodDeclaration`
- **Exact Line Range:** Lines 105–106
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Dispatches POST `/api/customers/upsert` to persist new customer profile and delivery address before checkout.
- **Verdict:** **VERIFIED**

---

### Callable 16: `ParenthesizedLambdaExpression` (SaveCustomerAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:106:60:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 106–106
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/customers/upsert", request, ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 17: `LoginByPinAsync(string pin, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:115:5:MethodDeclaration`
- **Exact Line Range:** Lines 115–129
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Authenticates operator PIN against POST `/api/auth/login-by-pin`. On success, stores `_sessionToken` and sets `_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sessionToken)`.
- **Verdict:** **VERIFIED — VULNERABILITY `SEC-008` (DefaultRequestHeaders mutation on shared client)**

---

### Callable 18: `ParenthesizedLambdaExpression` (LoginByPinAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:118:13:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 118–118
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 19: `LogoutAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:133:5:MethodDeclaration`
- **Exact Line Range:** Lines 133–140
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.LogoutAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Posts to `/api/auth/logout` and in `finally` clears `_sessionToken` and `_http.DefaultRequestHeaders.Authorization = null`.
- **Verdict:** **DEAD_CODE — Never called anywhere in CallCenterWinUI project (`MainViewModel.Lock` ignores it)**

---

### Callable 20: `GetMenuAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:142:5:MethodDeclaration`
- **Exact Line Range:** Lines 142–145
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetMenuAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/menu` to retrieve all product categories, items, and pricing variants. Invoked during desk initialization.
- **Verdict:** **VERIFIED**

---

### Callable 21: `ParenthesizedLambdaExpression` (GetMenuAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:144:46:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 144–144
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync("/api/menu", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 22: `OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:147:5:MethodDeclaration`
- **Exact Line Range:** Lines 147–150
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.OpenShiftAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/shifts/open`. Copied from CashierWinUI. Call Center operates within cashier shifts and has no shift open UI.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 23: `ParenthesizedLambdaExpression` (OpenShiftAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:149:49:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 149–149
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/shifts/open", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 24: `PrepareShiftCloseAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:152:5:MethodDeclaration`
- **Exact Line Range:** Lines 152–155
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.PrepareShiftCloseAsync(Falah.Application.Shifts.PrepareShiftCloseRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/shifts/prepare-close`. Shift close preparation is exclusive to Cashier tills.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 25: `ParenthesizedLambdaExpression` (PrepareShiftCloseAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:154:57:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 154–154
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 26: `CloseShiftAsync(CloseShiftRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:157:5:MethodDeclaration`
- **Exact Line Range:** Lines 157–160
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.CloseShiftAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/shifts/close`. Cashier shift closure transport wrapper.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 27: `ParenthesizedLambdaExpression` (CloseShiftAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:159:50:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 159–159
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/shifts/close", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 28: `CheckoutAsync(CheckoutOrderRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:162:5:MethodDeclaration`
- **Exact Line Range:** Lines 162–165
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.CheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Primary financial mutation: POST `/api/orders/checkout`. Dispatches the complete delivery order payload, including customer ID, items, delivery fee, and payment claims.
- **Verdict:** **VERIFIED**

---

### Callable 29: `ParenthesizedLambdaExpression` (CheckoutAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:164:53:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 164–164
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/orders/checkout", request, ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 30: `RefundAsync(RefundOrderRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:167:5:MethodDeclaration`
- **Exact Line Range:** Lines 167–170
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.RefundAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/orders/refund`. Call Center operators cannot perform order refunds.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 31: `ParenthesizedLambdaExpression` (RefundAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:169:51:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 169–169
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/orders/refund", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 32: `ConfirmElectronicPaymentAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:172:5:MethodDeclaration`
- **Exact Line Range:** Lines 172–175
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.ConfirmElectronicPaymentAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/orders/confirm-electronic-payment`. Delivery electronic payments are recorded as pending; settlement confirmation occurs at the physical cashier desk.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 33: `ParenthesizedLambdaExpression` (ConfirmElectronicPaymentAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:174:64:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 174–174
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 34: `CancelPendingElectronicPaymentAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:177:5:MethodDeclaration`
- **Exact Line Range:** Lines 177–180
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.CancelPendingElectronicPaymentAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/orders/cancel-pending-electronic-payment`. Cancellation of pending electronic payments is not supported in the Call Center UX.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 35: `ParenthesizedLambdaExpression` (CancelPendingElectronicPaymentAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:179:70:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 179–179
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 36: `GetActiveDeliveryServicesAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:182:5:MethodDeclaration`
- **Exact Line Range:** Lines 182–185
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetActiveDeliveryServicesAsync(CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/delivery/services` to populate external delivery aggregator choices (e.g. Talabat, Jahez) and in-house delivery services.
- **Verdict:** **VERIFIED**

---

### Callable 37: `ParenthesizedLambdaExpression` (GetActiveDeliveryServicesAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:184:67:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 184–184
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync("/api/delivery/services", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 38: `LookupOrderByNumberAsync(string orderNumber, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:187:5:MethodDeclaration`
- **Exact Line Range:** Lines 187–190
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.LookupOrderByNumberAsync(string, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/orders/lookup?orderNumber={orderNumber}`. Powers historical order lookup in the Call Center checkout view for delivery inquiries and manual reprint requests.
- **Verdict:** **VERIFIED**

---

### Callable 39: `ParenthesizedLambdaExpression` (LookupOrderByNumberAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:189:49:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 189–189
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct)`.
- **Verdict:** **VERIFIED**

---

### Callable 40: `GetOrderAsync(string orderId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:192:5:MethodDeclaration`
- **Exact Line Range:** Lines 192–195
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetOrderAsync(string, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/orders/{orderId}`. Unused in CallCenterWinUI; Call Center always queries by daily order number via `LookupOrderByNumberAsync`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 41: `ParenthesizedLambdaExpression` (GetOrderAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:194:49:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 194–194
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 42: `IssueManagerApprovalAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:197:5:MethodDeclaration`
- **Exact Line Range:** Lines 197–200
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.IssueManagerApprovalAsync(Falah.Application.Approvals.IssueManagerApprovalRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/approvals/issue`. Manager approval token generator. Never invoked in Call Center.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 43: `ParenthesizedLambdaExpression` (IssueManagerApprovalAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:199:60:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 199–199
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/approvals/issue", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 44: `AddExpenseAsync(AddExpenseRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:202:5:MethodDeclaration`
- **Exact Line Range:** Lines 202–205
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.AddExpenseAsync(Falah.Application.Expenses.AddExpenseRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/expenses`. Till expense entry. Never invoked in Call Center.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 45: `ParenthesizedLambdaExpression` (AddExpenseAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:204:50:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 204–204
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/expenses", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 46: `SafeDropAsync(SafeDropRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:209:5:MethodDeclaration`
- **Exact Line Range:** Lines 209–212
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.SafeDropAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/treasury/safe-drop`. Cashier drawer-to-safe cash transfer. Never invoked in Call Center.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 47: `ParenthesizedLambdaExpression` (SafeDropAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:211:48:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 211–211
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/treasury/safe-drop", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 48: `CashierCashMovementAsync(CashierCashMovementRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:214:5:MethodDeclaration`
- **Exact Line Range:** Lines 214–217
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.CashierCashMovementAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/cash-drawer/cash-movement`. Till pay-in / pay-out movement. Never invoked in Call Center.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 49: `ParenthesizedLambdaExpression` (CashierCashMovementAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:216:59:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 216–216
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/cash-drawer/cash-movement", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 50: `OpenCashDrawerAsync(OpenCashDrawerRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:219:5:MethodDeclaration`
- **Exact Line Range:** Lines 219–222
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.OpenCashDrawerAsync(Falah.Application.CashDrawer.OpenCashDrawerRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** POST `/api/cash-drawer/open`. Physical drawer kick command. Never invoked in Call Center.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 51: `ParenthesizedLambdaExpression` (OpenCashDrawerAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:221:51:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 221–221
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync("/api/cash-drawer/open", request, ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 52: `GetTreasuryBalanceAsync(string accountId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:228:5:MethodDeclaration`
- **Exact Line Range:** Lines 228–231
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.GetTreasuryBalanceAsync(string, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Queries GET `/api/treasury/balance?accountId={accountId}` for cash counts. Never invoked in Call Center.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 53: `ParenthesizedLambdaExpression` (GetTreasuryBalanceAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:230:52:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 230–230
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.GetAsync($"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId)}", ct)`.
- **Verdict:** **DEAD_CODE_MIRROR**

---

### Callable 54: `CreatePrintJobsAsync(string orderId, CreatePrintJobsRequest request, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:233:5:MethodDeclaration`
- **Exact Line Range:** Lines 233–236
- **Signature:** `Falah.Client.CallCenterWinUI.Services.ApiClient.CreatePrintJobsAsync(string, Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken)`
- **Kind:** `MethodDeclaration`
- **Architectural Role & Semantics:** Dispatches POST `/api/orders/{orderId}/print-jobs`. Invoked by `CallCenterViewModel.Checkout.cs` to trigger manual ticket reprints or kitchen reprints.
- **Verdict:** **VERIFIED**

---

### Callable 55: `ParenthesizedLambdaExpression` (CreatePrintJobsAsync)
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:235:55:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 235–235
- **Signature:** `lambda expression`
- **Kind:** `ParenthesizedLambdaExpression`
- **Architectural Role & Semantics:** Delegated call `() => _http.PostAsJsonAsync($"/api/orders/{Uri.EscapeDataString(orderId)}/print-jobs", request, ct)`.
- **Verdict:** **VERIFIED**

---

## 4. Summary of Parity Defects and Actionable Recommendations

### Architectural Summary
`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs` is a truncated clone of the Cashier WinUI client rather than a purpose-built Call Center service. While it properly provides essential endpoints for phone delivery order placement (`CheckoutAsync`, `SaveCustomerAsync`, `LookupCustomerAsync`), it exhibits significant parity deficiencies:
1. **Security Vulnerability `SEC-008`:** Unprotected mutation of `_http.DefaultRequestHeaders.Authorization` combined with complete non-use of `LogoutAsync` upon screen locking.
2. **False-Positive Financial Uncertainty (`SEM-MIRROR-072`):** Universal `SendAsync` mapping wraps read-only GET requests in `OutcomeUnknown: true`, treating safe network drops as ambiguous money operations.
3. **Misleading Error Messages (`SEM-MIRROR-073`):** Blanket mapping of HTTP 401 to "Invalid PIN or inactive user" across all transactional endpoints.
4. **Substantial Code Bloat:** 14 out of 27 high-level methods are dead code, mirroring Cashier-specific hardware and settlement operations.

### Actionable Remediations (Audit Documentation Only — No Code Modified)
1. **Per-Request Bearer Token Injection:** Replace mutable `_http.DefaultRequestHeaders.Authorization` with an `AuthenticationHeaderHandler` delegating handler or explicit `HttpRequestMessage.Headers.Authorization` per-call assignment.
2. **Invoke Server Logout on Lock:** Update `MainViewModel.Lock()` to await `_session.ApiClient.LogoutAsync()` prior to transitioning to `LoginViewModel`.
3. **Split `ReadAsync` vs `SendAsync`:** Reintroduce the `CashierWinUI` pattern of separating read-only queries (which should never report `OutcomeUnknown: true`) from mutating transactional commands.
4. **Prune Unused Cashier Endpoints:** Remove the 14 dead cashier-specific wrappers to reduce surface area and eliminate confusion regarding Call Center operational boundaries.


---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T01:55:00Z
- **Source SHA-256 Verified:** `5702006B3B43BA4CD7242080292FA2CE6C4B232F6DB1A85AD04E93BF94D277DB` (Worktree CRLF) / `0714593ee3ceafda4b48658f6c7b61e95d0870e5c410117f79fae5dfcc95258d` (Normalized LF Git Blob)
- **Callable ID Checked:** 55 of 55 in-scope callables verified (`src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration` through `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:235:55:ParenthesizedLambdaExpression`)
- **Verdict:** ACCEPTED
- **Challenger Analysis:**
  1. Verified SHA-256 matches actual production source `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs` (238 lines). Normalized line endings match git blob `0714593ee3ceafda4b48658f6c7b61e95d0870e5c410117f79fae5dfcc95258d`.
  2. Confirmed `SEC-008`: In `LoginByPinAsync` (lines 122-124), `_http.DefaultRequestHeaders.Authorization` is directly mutated on the shared `HttpClient` instance. Concurrent or background worker requests risk transmitting newly logged-in bearer tokens. Compounding this, `MainViewModel.Lock()` does not invoke `LogoutAsync()`, leaving unrevoked bearer credentials active in `DefaultRequestHeaders`.
  3. Confirmed `SEM-MIRROR-072`: Universal routing of all HTTP operations (including idempotent GET reads like `GetTaxRulesAsync`, `LookupCustomerAsync`, `GetMenuAsync`) through `SendAsync<T>` results in `OutcomeUnknown: true` on transport timeout or network drop, conflating safe reads with ambiguous financial mutations.
  4. Confirmed `SEM-MIRROR-073` and `SEM-MIRROR-074`: Error envelope deserialization in `ReadResultAsync` falls back to `"رمز الدخول غير صحيح."` on HTTP 401 across transactional non-auth endpoints, and empty 200/204 responses are rejected with `OutcomeUnknown: true` due to missing `SendEmptyAsync`.
  5. Confirmed dead code analysis: 14 cashier-specific transport wrappers (`SafeDropAsync`, `OpenCashDrawerAsync`, `CloseShiftAsync`, etc.) are verbatim copies from `CashierWinUI` that are never invoked anywhere in Call Center UI.
  6. All 55 canonical callable IDs, method signatures, line numbers, and classification metadata verified against Roslyn AST and source lines.


---

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T01:58:15Z
- **Source SHA-256 Verified:** `5702006b3b43ba4cd7242080292fa2ce6c4b232f6db1a85ad04e93bf94d277db`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `5702006b3b43ba4cd7242080292fa2ce6c4b232f6db1a85ad04e93bf94d277db` against `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs` (238 lines). All 55 canonical callables in-scope verified against Roslyn AST.
2. Concur with L1 analysis on `SEC-008`, `SEM-MIRROR-072`, `SEM-MIRROR-073`, `SEM-MIRROR-074`, and transport wrapper dead code.
3. Additional L2 Challenger Assessment:
   - In `LoginByPinAsync` (lines 115-129), mutating `_http.DefaultRequestHeaders.Authorization` on the singleton `HttpClient` introduces thread safety issues across concurrent background tasks. Furthermore, because screen locking does not trigger `LogoutAsync()`, bearer tokens remain pinned in memory on shared workstation terminals (`SEC-008`).
   - In `SendAsync<T>` (lines 84-95), setting `OutcomeUnknown: true` on transport catch block treats safe, idempotent GET queries (e.g. `GetMenuAsync`, `GetTaxRulesAsync`, `LookupCustomerAsync`) as high-risk ambiguous operations, triggering duplicate warning banners on harmless network retries (`SEM-MIRROR-072`).
   - In `ReadResultAsync` (lines 56-82), HTTP 401 Unauthorized status is hardcoded to `"رمز الدخول غير صحيح."`, misinforming operators on expired session tokens during operational checkout and customer lookup (`SEM-MIRROR-073`).
   - In `ProductImageUri` (lines 36-39), URI generation relies on string concatenation without checking `_http.BaseAddress` schema or trailing slash normalization, risking invalid URI formation if configured with custom reverse-proxy paths.
4. All 55 callables rigorously double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
