# File Review: src/Falah.Client.CashierWinUI/Services/ApiClient.cs

## File Overview
- **Source Path:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Auditor:** CLIENT_DEEP_SPECIALIST
- **Review Date:** 2026-10-25T07:15:00Z
- **Total Callables:** 268

---

## Callable Reviews

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration
#### ApiClient Constructor (HttpClient injection) — Lines 26-26
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration`
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ApiClient(System.Net.Http.HttpClient)
- **Lines:** [26-26]
- **Behavior:** Initializes the `ApiClient` instance by injecting an existing `HttpClient` instance into private field `_http`. Assumes the caller has configured the base address, default request headers, and timeout policies.
- **Validations:** None detected in body (null injection allowed).
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public ApiClient(HttpClient http) => _http = http;
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration
#### ApiClient Constructor (Base address string) — Lines 28-31
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration`
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ApiClient(string)
- **Lines:** [28-31]
- **Behavior:** Constructs a new `HttpClient` with `BaseAddress = new Uri(baseAddress)` (defaulting to `"http://localhost:5151"`) and `Timeout = TimeSpan.FromSeconds(10)`. Used when the Cashier client runs standalone or connects to a local/network Fale7 host.
- **Validations:** Uri creation on baseAddress string. Throws: `UriFormatException` if baseAddress is invalid.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public ApiClient(string baseAddress = "http://localhost:5151")
{
    _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(10) };
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:36:5:MethodDeclaration
#### ProductImageUri Method — Lines 36-41
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:36:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:36:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ProductImageUri(string, string?)
- **Lines:** [36-41]
- **Behavior:** Constructs a direct photo download URI for XAML Image controls: `new Uri(_http.BaseAddress, $"/api/products/{Uri.EscapeDataString(productId)}/image?v={Uri.EscapeDataString(version)}")`. If version is null/empty or `_http.BaseAddress` is null, returns null. The version parameter acts as a cache-busting query parameter so replaced images produce unique URLs without altering server routing.
- **Validations:** Checks `string.IsNullOrEmpty(version) || _http.BaseAddress is null`. Escapes productId and version. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Uri? ProductImageUri(string productId, string? version) =>
    string.IsNullOrEmpty(version) || _http.BaseAddress is null
        ? null
        : new Uri(_http.BaseAddress, $"/api/products/{Uri.EscapeDataString(productId)}/image?v={Uri.EscapeDataString(version)}");
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:43:5:MethodDeclaration
#### PingHealthAsync Method — Lines 43-54
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:43:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:43:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PingHealthAsync(CancellationToken)
- **Lines:** [43-54]
- **Behavior:** Dispatches HTTP GET `/health`. Returns `true` if `response.IsSuccessStatusCode` is true. Catches all exceptions (network failure, timeout, host down) and safely returns `false`. Used by the shell connection health indicator.
- **Validations:** None. Exception boundary catches all exceptions. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<bool> PingHealthAsync(CancellationToken ct = default)
{
    try
    {
        using var response = await _http.GetAsync("/health", ct);
        return response.IsSuccessStatusCode;
    }
    catch
    {
        return false;
    }
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:58:5:MethodDeclaration
#### ReadResultAsync<T> Method (Response deserialization & error parsing) — Lines 58-84
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:58:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:58:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken)
- **Lines:** [58-84]
- **Behavior:** Core response parsing pipeline. On HTTP success (`IsSuccessStatusCode`), deserializes JSON content to `T` via `ReadFromJsonAsync<T>`. If deserialized object is null, returns failure `ApiResult<T>` with `OutcomeUnknown: true`. On HTTP failure, attempts to deserialize body as `ErrorBody` to extract `{ error: message }`; falls back to status-code localized Arabic messages (401 -> "رمز الدخول غير صحيح.", 404 -> "العنصر المطلوب غير موجود.", 500+ -> "حدث خطأ غير متوقع من السيرفر ({code})."). Sets `OutcomeUnknown = (statusCode >= 500)`.
- **Validations:** Checks `response.IsSuccessStatusCode`. Exception handling around error body JSON parsing. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
{
    if (response.IsSuccessStatusCode)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
        return value is null ? new(false, default, "لم يصل تأكيد صالح للعملية. أعد المحاولة للتحقق.", true)
            : new ApiResult<T>(true, value, null, false, (int)response.StatusCode);
    }

    string? message = null;
    try
    {
        var body = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken: ct);
        message = body?.error;
    }
    catch { }
    message ??= response.StatusCode switch
    {
        System.Net.HttpStatusCode.Unauthorized => "رمز الدخول غير صحيح.",
        System.Net.HttpStatusCode.NotFound => "العنصر المطلوب غير موجود.",
        _ => $"حدث خطأ غير متوقع من السيرفر ({(int)response.StatusCode}).",
    };
    return new ApiResult<T>(false, default, message, (int)response.StatusCode >= 500, (int)response.StatusCode);
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:92:5:MethodDeclaration
#### ReadAsync<T> Method (Query transport pipeline) — Lines 92-103
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:92:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:92:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReadAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [92-103]
- **Behavior:** Transport pipeline for read-only GET queries. Invokes `send()` and processes response with `ReadResultAsync<T>`. Catches transport exceptions (`HttpRequestException`, `OperationCanceledException`, `JsonException`, `IOException`) and returns `ApiResult<T>` with message "تعذر تحميل البيانات من السيرفر. تأكد من الاتصال ثم أعد المحاولة." and `OutcomeUnknown: true`. Does not instruct re-sending duplicate mutation data since queries mutate no server state.
- **Validations:** Catches transport exception filters. Disposes HttpResponseMessage via `using`. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
private static async Task<ApiResult<T>> ReadAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
{
    try
    {
        using var response = await send();
        return await ReadResultAsync<T>(response, ct);
    }
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or System.IO.IOException)
    {
        return new(false, default, "تعذر تحميل البيانات من السيرفر. تأكد من الاتصال ثم أعد المحاولة.", true);
    }
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:112:5:MethodDeclaration
#### SendAsync<T> Method (Command transport pipeline) — Lines 112-123
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:112:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:112:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SendAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [112-123]
- **Behavior:** Transport pipeline for state-mutating commands (POST, PUT, DELETE). Invokes `send()` and processes response with `ReadResultAsync<T>`. Catches transport exceptions (`HttpRequestException`, `OperationCanceledException`, `JsonException`, `IOException`) and returns `ApiResult<T>` with message "تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية." and `OutcomeUnknown: true`. Explicitly advises retrying with the *same data* because operations carry idempotency keys, ensuring safe replay without duplicate state mutation.
- **Validations:** Catches transport exception filters. Disposes HttpResponseMessage via `using`. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
private static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
{
    try
    {
        using var response = await send();
        return await ReadResultAsync<T>(response, ct);
    }
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or System.IO.IOException)
    {
        return new(false, default, "تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية.", true);
    }
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:125:5:MethodDeclaration
#### SendEmptyAsync Method (Void/Bool command transport pipeline) — Lines 125-137
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:125:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:125:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SendEmptyAsync(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [125-137]
- **Behavior:** Transport pipeline for commands expecting boolean confirmation or 204 No Content. Returns `ApiResult<bool>(true, true)` on `response.IsSuccessStatusCode`. On failure, delegates to `ReadResultAsync<bool>`. Catches transport exceptions, returning `ApiResult<bool>(false, false, ..., OutcomeUnknown: true)`.
- **Validations:** Catches transport exception filters. Disposes HttpResponseMessage. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
private static async Task<ApiResult<bool>> SendEmptyAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
{
    try
    {
        using var response = await send();
        return response.IsSuccessStatusCode ? new(true, true, null, StatusCode: (int)response.StatusCode)
            : await ReadResultAsync<bool>(response, ct);
    }
    catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or System.IO.IOException)
    {
        return new(false, false, "تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية.", true);
    }
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:139:5:MethodDeclaration
#### GetCurrentShiftAsync — Lines 139-140
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:139:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:139:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCurrentShiftAsync(CancellationToken)
- **Lines:** [139-140]
- **Behavior:** Executes HTTP GET to `/api/shifts/current`. Request payload: `None`. Response: `Task<ApiResult<CurrentShiftDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<CurrentShiftDto>> GetCurrentShiftAsync(CancellationToken ct = default) =>
        ReadAsync<CurrentShiftDto>(() => _http.GetAsync("/api/shifts/current", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:140:36:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 140-140
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:140:36:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:140:36:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [140-140]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/shifts/current"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:141:5:MethodDeclaration
#### GetTaxRulesAsync — Lines 141-142
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:141:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:141:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTaxRulesAsync(CancellationToken)
- **Lines:** [141-142]
- **Behavior:** Executes HTTP GET to `/api/tax-rules`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>> GetTaxRulesAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>(() => _http.GetAsync("/api/tax-rules", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:142:72:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 142-142
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:142:72:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:142:72:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [142-142]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/tax-rules"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:143:5:MethodDeclaration
#### GetSettingsAsync — Lines 143-144
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:143:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:143:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSettingsAsync(CancellationToken)
- **Lines:** [143-144]
- **Behavior:** Executes HTTP GET to `/api/settings`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Settings.SettingsDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Settings.SettingsDto>> GetSettingsAsync(CancellationToken ct = default) =>
        ReadAsync<Falah.Application.Settings.SettingsDto>(() => _http.GetAsync("/api/settings", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:144:59:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 144-144
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:144:59:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:144:59:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [144-144]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/settings"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:145:5:MethodDeclaration
#### LookupCustomerAsync — Lines 145-146
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:145:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:145:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LookupCustomerAsync(string, CancellationToken)
- **Lines:** [145-146]
- **Behavior:** Executes HTTP GET to `$"/api/customers/lookup?phone={Uri.EscapeDataString(phone`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Customers.CustomerDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Customers.CustomerDto>> LookupCustomerAsync(string phone, CancellationToken ct = default) =>
        ReadAsync<Falah.Application.Customers.CustomerDto>(() => _http.GetAsync($"/api/customers/lookup?phone={Uri.EscapeDataString(phone)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:146:60:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 146-146
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:146:60:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:146:60:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/customers/lookup?phone={Uri.EscapeDataString(phone` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:147:5:MethodDeclaration
#### SaveCustomerAsync — Lines 147-148
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:147:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:147:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken)
- **Lines:** [147-148]
- **Behavior:** Executes HTTP POST to `/api/customers/upsert`. Request payload: `Falah.Application.Customers.UpsertCustomerRequest`. Response: `Task<ApiResult<Falah.Application.Customers.CustomerDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Customers.CustomerDto>> SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.PostAsJsonAsync("/api/customers/upsert", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:148:60:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 148-148
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:148:60:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:148:60:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [148-148]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/customers/upsert"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:157:5:MethodDeclaration
#### LoginByPinAsync Method (Authentication & SEC-008 Finding) — Lines 157-171
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:157:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:157:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken)
- **Lines:** [157-171]
- **Behavior:** Authenticates a cashier via PIN. HTTP POST to `/api/auth/login-by-pin` with `LoginByPinRequest(pin)`. On success, extracts `result.Value.Token` into `_sessionToken` and sets `_http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _sessionToken)`. Returns `ApiResult<UserSummaryDto>` with user id, name, role, and permissions.

> [!CAUTION]
> **SEC-008 Finding — Shared Singleton HttpClient Authorization Header Mutation Race**:
> `_http.DefaultRequestHeaders.Authorization` is directly mutated on the shared singleton `HttpClient` (`_http`). Because `ApiClient` and its underlying `HttpClient` are registered as shared singleton dependencies in the application container, mutating default request headers introduces a severe concurrency race condition. If background synchronization tasks, print queue pollers, or shift status checkers are in flight concurrently, changing or replacing default headers overwrites credentials for all concurrent calls across the process, leading to intermittent 401 Unauthorized errors or potential permission cross-contamination.
- **Validations:** Validates `result.Success` and `result.Value is not null`. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

```csharp
public async Task<ApiResult<UserSummaryDto>> LoginByPinAsync(string pin, CancellationToken ct = default)
{
    var result = await SendAsync<LoginResultDto>(
        () => _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct), ct);
    if (!result.Success || result.Value is null)
        return new ApiResult<UserSummaryDto>(false, null, result.Error, result.OutcomeUnknown, result.StatusCode);

    _sessionToken = result.Value.Token;
    _http.DefaultRequestHeaders.Authorization =
        new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sessionToken);

    return new ApiResult<UserSummaryDto>(true,
        new UserSummaryDto(result.Value.Id, result.Value.Name, result.Value.Role, result.Value.Permissions),
        null, false, result.StatusCode);
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:160:13:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 160-160
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:160:13:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:160:13:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [160-160]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/auth/login-by-pin"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:175:5:MethodDeclaration
#### LogoutAsync Method (Session termination & SEC-008 Finding) — Lines 175-182
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:175:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:175:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LogoutAsync(CancellationToken)
- **Lines:** [175-182]
- **Behavior:** Terminates cashier session. Dispatches HTTP POST to `/api/auth/logout`. In the local client, resets `_sessionToken = null` and nullifies `_http.DefaultRequestHeaders.Authorization = null`.

> [!WARNING]
> **SEC-008 Finding — Shared Singleton HttpClient Authorization Header Mutation Race**:
> Mutating `_http.DefaultRequestHeaders.Authorization = null` on the shared singleton client clears bearer tokens for all requests currently in flight on other threads or background tasks, causing unexpected 401 failures.
- **Validations:** Early return if `_sessionToken is null`. Safely swallows server-side logout exceptions in empty catch block. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

```csharp
public async Task LogoutAsync(CancellationToken ct = default)
{
    if (_sessionToken is null) return;
    try { await _http.PostAsync("/api/auth/logout", content: null, ct); }
    catch { /* the local session ends either way; a stranded token expires on its own. */ }
    _sessionToken = null;
    _http.DefaultRequestHeaders.Authorization = null;
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:184:5:MethodDeclaration
#### GetMenuAsync — Lines 184-187
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:184:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:184:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetMenuAsync(CancellationToken)
- **Lines:** [184-187]
- **Behavior:** Executes HTTP GET to `/api/menu`. Request payload: `None`. Response: `Task<ApiResult<MenuResponse>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<MenuResponse>> GetMenuAsync(CancellationToken ct = default)
    {
        return await ReadAsync<MenuResponse>(() => _http.GetAsync("/api/menu", ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:186:46:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 186-186
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:186:46:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:186:46:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [186-186]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/menu"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:189:5:MethodDeclaration
#### OpenShiftAsync Method (Shift opening) — Lines 189-192
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:189:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:189:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.OpenShiftAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken)
- **Lines:** [189-192]
- **Behavior:** Opens a new work shift: HTTP POST to `/api/shifts/open` with `OpenShiftRequest` (opening float in piastres, cashier user ID). Returns `ApiResult<OpenShiftResult>` via `SendAsync<OpenShiftResult>`.
- **Validations:** Delegates to `SendAsync<OpenShiftResult>`. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

```csharp
public async Task<ApiResult<OpenShiftResult>> OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)
{
    return await SendAsync<OpenShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/open", request, ct), ct);
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:191:49:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 191-191
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:191:49:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:191:49:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [191-191]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/shifts/open"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:194:5:MethodDeclaration
#### PrepareShiftCloseAsync — Lines 194-197
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:194:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:194:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PrepareShiftCloseAsync(Falah.Application.Shifts.PrepareShiftCloseRequest, CancellationToken)
- **Lines:** [194-197]
- **Behavior:** Executes HTTP POST to `/api/shifts/prepare-close`. Request payload: `Falah.Application.Shifts.PrepareShiftCloseRequest`. Response: `Task<ApiResult<PrepareShiftCloseResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<PrepareShiftCloseResult>> PrepareShiftCloseAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<PrepareShiftCloseResult>(() => _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:196:57:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 196-196
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:196:57:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:196:57:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [196-196]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/shifts/prepare-close"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:199:5:MethodDeclaration
#### CloseShiftAsync Method (Shift closing) — Lines 199-202
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:199:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:199:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CloseShiftAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken)
- **Lines:** [199-202]
- **Behavior:** Closes the current active cashier shift: HTTP POST to `/api/shifts/close` with `CloseShiftRequest` (counted cash, closing notes, handover details). Returns `ApiResult<CloseShiftResult>` via `SendAsync<CloseShiftResult>`.
- **Validations:** Delegates to `SendAsync<CloseShiftResult>`. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

```csharp
public async Task<ApiResult<CloseShiftResult>> CloseShiftAsync(CloseShiftRequest request, CancellationToken ct = default)
{
    return await SendAsync<CloseShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/close", request, ct), ct);
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:201:50:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 201-201
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:201:50:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:201:50:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [201-201]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/shifts/close"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:204:5:MethodDeclaration
#### CheckoutAsync Method (Order checkout) — Lines 204-207
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:204:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:204:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken)
- **Lines:** [204-207]
- **Behavior:** Submits order checkout to server: HTTP POST to `/api/orders/checkout` with `CheckoutOrderRequest` payload (order lines, order type, payment method, customer info, idempotency key). Returns `ApiResult<CheckoutOrderResult>`. Wraps call in `SendAsync<CheckoutOrderResult>` to enforce retry-with-same-data semantics under network failure.
- **Validations:** Delegates validation to `SendAsync<CheckoutOrderResult>`. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

```csharp
public async Task<ApiResult<CheckoutOrderResult>> CheckoutAsync(CheckoutOrderRequest request, CancellationToken ct = default)
{
    return await SendAsync<CheckoutOrderResult>(() => _http.PostAsJsonAsync("/api/orders/checkout", request, ct), ct);
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:206:53:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 206-206
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:206:53:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:206:53:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [206-206]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/orders/checkout"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:209:5:MethodDeclaration
#### RefundAsync Method (Order refund) — Lines 209-212
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:209:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:209:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RefundAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken)
- **Lines:** [209-212]
- **Behavior:** Processes order refund: HTTP POST to `/api/orders/refund` with `RefundOrderRequest` payload (order ID, refund reason, items, target treasury account). Returns `ApiResult<RefundOrderResult>` via `SendAsync<RefundOrderResult>`.
- **Validations:** Delegates to `SendAsync<RefundOrderResult>`. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

```csharp
public async Task<ApiResult<RefundOrderResult>> RefundAsync(RefundOrderRequest request, CancellationToken ct = default)
{
    return await SendAsync<RefundOrderResult>(() => _http.PostAsJsonAsync("/api/orders/refund", request, ct), ct);
}
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:211:51:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 211-211
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:211:51:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:211:51:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [211-211]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/orders/refund"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:214:5:MethodDeclaration
#### ConfirmElectronicPaymentAsync — Lines 214-217
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:214:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:214:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ConfirmElectronicPaymentAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken)
- **Lines:** [214-217]
- **Behavior:** Executes HTTP POST to `/api/orders/confirm-electronic-payment`. Request payload: `Falah.Application.Orders.ConfirmElectronicPaymentRequest`. Response: `Task<ApiResult<ConfirmElectronicPaymentResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<ConfirmElectronicPaymentResult>> ConfirmElectronicPaymentAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<ConfirmElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:216:64:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 216-216
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:216:64:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:216:64:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [216-216]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/orders/confirm-electronic-payment"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:219:5:MethodDeclaration
#### CancelPendingElectronicPaymentAsync — Lines 219-222
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:219:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:219:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CancelPendingElectronicPaymentAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken)
- **Lines:** [219-222]
- **Behavior:** Executes HTTP POST to `/api/orders/cancel-pending-electronic-payment`. Request payload: `Falah.Application.Orders.CancelPendingElectronicPaymentRequest`. Response: `Task<ApiResult<CancelPendingElectronicPaymentResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<CancelPendingElectronicPaymentResult>> CancelPendingElectronicPaymentAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CancelPendingElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment", request, ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:221:70:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 221-221
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:221:70:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:221:70:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [221-221]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/orders/cancel-pending-electronic-payment"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:224:5:MethodDeclaration
#### GetActiveDeliveryServicesAsync — Lines 224-227
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:224:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:224:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetActiveDeliveryServicesAsync(CancellationToken)
- **Lines:** [224-227]
- **Behavior:** Executes HTTP GET to `/api/delivery/services`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>> GetActiveDeliveryServicesAsync(CancellationToken ct = default)
    {
        return await ReadAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services", ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:226:67:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 226-226
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:226:67:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:226:67:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [226-226]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/delivery/services"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:231:5:MethodDeclaration
#### GetOrdersAsync — Lines 231-233
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:231:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:231:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetOrdersAsync(string, CancellationToken)
- **Lines:** [231-233]
- **Behavior:** Executes HTTP GET to `$"/api/orders?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<OrderDetailsDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<OrderDetailsDto>>> GetOrdersAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<OrderDetailsDto>>(() => _http.GetAsync(
            $"/api/orders?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:232:51:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 232-233
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:232:51:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:232:51:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [232-233]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/orders?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:237:5:MethodDeclaration
#### GetPriceRulesAsync — Lines 237-238
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:237:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:237:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPriceRulesAsync(CancellationToken)
- **Lines:** [237-238]
- **Behavior:** Executes HTTP GET to `/api/price-rules`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.PriceRuleDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:238:74:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 238-238
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:238:74:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:238:74:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [238-238]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/price-rules"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:240:5:MethodDeclaration
#### SetProductAvailabilityAsync — Lines 240-242
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:240:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:240:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetProductAvailabilityAsync(Falah.Application.Menu.SetProductAvailabilityRequest, CancellationToken)
- **Lines:** [240-242]
- **Behavior:** Executes HTTP POST to `$"/api/products/{Uri.EscapeDataString(request.ProductId`. Request payload: `Falah.Application.Menu.SetProductAvailabilityRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:241:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 241-242
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:241:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:241:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [241-242]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/products/{Uri.EscapeDataString(request.ProductId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:246:5:MethodDeclaration
#### UpdateSettingsAsync — Lines 246-247
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:246:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:246:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateSettingsAsync(Falah.Application.Settings.UpdateSettingsRequest, CancellationToken)
- **Lines:** [246-247]
- **Behavior:** Executes HTTP PUT to `/api/settings`. Request payload: `Falah.Application.Settings.UpdateSettingsRequest`. Response: `Task<ApiResult<Falah.Application.Settings.SettingsDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:247:59:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 247-247
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:247:59:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:247:59:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [247-247]
- **Behavior:** HTTP invocation delegate: dispatches HTTP PUT request to `"/api/settings"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:249:5:MethodDeclaration
#### GetBackupsAsync — Lines 249-250
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:249:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:249:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBackupsAsync(CancellationToken)
- **Lines:** [249-250]
- **Behavior:** Executes HTTP GET to `/api/backups`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Backup.BackupRecordDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Backup.BackupRecordDto>>> GetBackupsAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.Backup.BackupRecordDto>>(() => _http.GetAsync("/api/backups", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:250:76:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 250-250
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:250:76:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:250:76:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [250-250]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/backups"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:252:5:MethodDeclaration
#### CreateBackupAsync — Lines 252-253
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:252:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:252:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateBackupAsync(Falah.Application.Backup.CreateBackupRequest, CancellationToken)
- **Lines:** [252-253]
- **Behavior:** Executes HTTP POST to `/api/backups`. Request payload: `Falah.Application.Backup.CreateBackupRequest`. Response: `Task<ApiResult<Falah.Application.Backup.BackupRecordDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Backup.BackupRecordDto>> CreateBackupAsync(Falah.Application.Backup.CreateBackupRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Backup.BackupRecordDto>(() => _http.PostAsJsonAsync("/api/backups", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:253:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 253-253
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:253:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:253:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [253-253]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/backups"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:255:5:MethodDeclaration
#### VerifyBackupAsync — Lines 255-257
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:255:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:255:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.VerifyBackupAsync(Falah.Application.Backup.VerifyBackupRequest, CancellationToken)
- **Lines:** [255-257]
- **Behavior:** Executes HTTP POST to `$"/api/backups/{Uri.EscapeDataString(request.BackupRecordId`. Request payload: `Falah.Application.Backup.VerifyBackupRequest`. Response: `Task<ApiResult<Falah.Application.Backup.BackupRecordDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Backup.BackupRecordDto>> VerifyBackupAsync(Falah.Application.Backup.VerifyBackupRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Backup.BackupRecordDto>(() => _http.PostAsJsonAsync(
            $"/api/backups/{Uri.EscapeDataString(request.BackupRecordId)}/verify", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:256:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 256-257
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:256:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:256:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [256-257]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/backups/{Uri.EscapeDataString(request.BackupRecordId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:261:5:MethodDeclaration
#### GetCurrentBusinessDayAsync — Lines 261-262
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:261:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:261:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCurrentBusinessDayAsync(CancellationToken)
- **Lines:** [261-262]
- **Behavior:** Executes HTTP GET to `/api/business-day/current`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>> GetCurrentBusinessDayAsync(CancellationToken ct = default) =>
        ReadAsync<Falah.Application.BusinessDay.BusinessDayDto>(() => _http.GetAsync("/api/business-day/current", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:262:65:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 262-262
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:262:65:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:262:65:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [262-262]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/business-day/current"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:264:5:MethodDeclaration
#### GetBusinessDayAsync — Lines 264-265
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:264:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:264:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayAsync(string, CancellationToken)
- **Lines:** [264-265]
- **Behavior:** Executes HTTP GET to `$"/api/business-day/{Uri.EscapeDataString(id`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>> GetBusinessDayAsync(string id, CancellationToken ct = default) =>
        ReadAsync<Falah.Application.BusinessDay.BusinessDayDto>(() => _http.GetAsync($"/api/business-day/{Uri.EscapeDataString(id)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:265:65:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 265-265
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:265:65:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:265:65:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [265-265]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/business-day/{Uri.EscapeDataString(id` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:267:5:MethodDeclaration
#### GetCloseBlockersAsync — Lines 267-269
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:267:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:267:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCloseBlockersAsync(string, CancellationToken)
- **Lines:** [267-269]
- **Behavior:** Executes HTTP GET to `$"/api/business-day/{Uri.EscapeDataString(id`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.CloseBlockerDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:268:81:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 268-269
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:268:81:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:268:81:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [268-269]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/business-day/{Uri.EscapeDataString(id` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:271:5:MethodDeclaration
#### GetChecklistAsync — Lines 271-273
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:271:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:271:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetChecklistAsync(string, CancellationToken)
- **Lines:** [271-273]
- **Behavior:** Executes HTTP GET to `$"/api/business-day/{Uri.EscapeDataString(id`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.ChecklistItemStatusDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:272:88:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 272-273
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:272:88:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:272:88:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [272-273]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/business-day/{Uri.EscapeDataString(id` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:275:5:MethodDeclaration
#### CompleteChecklistItemAsync — Lines 275-277
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:275:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:275:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CompleteChecklistItemAsync(Falah.Application.BusinessDay.CompleteChecklistItemRequest, CancellationToken)
- **Lines:** [275-277]
- **Behavior:** Executes HTTP POST to `$"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId`. Request payload: `Falah.Application.BusinessDay.CompleteChecklistItemRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:276:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 276-277
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:276:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:276:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [276-277]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:279:5:MethodDeclaration
#### GetExceptionsAsync — Lines 279-281
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:279:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:279:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetExceptionsAsync(string, CancellationToken)
- **Lines:** [279-281]
- **Behavior:** Executes HTTP GET to `$"/api/business-day/{Uri.EscapeDataString(id`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.ControlExceptionDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:280:85:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 280-281
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:280:85:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:280:85:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [280-281]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/business-day/{Uri.EscapeDataString(id` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:283:5:MethodDeclaration
#### GetBusinessDaysAsync — Lines 283-284
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:283:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:283:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDaysAsync(CancellationToken)
- **Lines:** [283-284]
- **Behavior:** Executes HTTP GET to `/api/business-days`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.BusinessDayDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.BusinessDayDto>>> GetBusinessDaysAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.BusinessDay.BusinessDayDto>>(() => _http.GetAsync("/api/business-days", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:284:80:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 284-284
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:284:80:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:284:80:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [284-284]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/business-days"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:286:5:MethodDeclaration
#### OpenBusinessDayAsync — Lines 286-287
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:286:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:286:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.OpenBusinessDayAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken)
- **Lines:** [286-287]
- **Behavior:** Executes HTTP POST to `/api/business-day/open`. Request payload: `Falah.Application.BusinessDay.OpenBusinessDayRequest`. Response: `Task<ApiResult<Falah.Application.BusinessDay.OpenBusinessDayResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.BusinessDay.OpenBusinessDayResult>> OpenBusinessDayAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.OpenBusinessDayResult>(() => _http.PostAsJsonAsync("/api/business-day/open", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:287:72:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 287-287
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:287:72:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:287:72:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [287-287]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/business-day/open"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:289:5:MethodDeclaration
#### CloseBusinessDayAsync — Lines 289-291
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:289:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:289:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CloseBusinessDayAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken)
- **Lines:** [289-291]
- **Behavior:** Executes HTTP POST to `$"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId`. Request payload: `Falah.Application.BusinessDay.CloseBusinessDayRequest`. Response: `Task<ApiResult<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>> CloseBusinessDayAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/close", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:290:78:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 290-291
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:290:78:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:290:78:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [290-291]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:293:5:MethodDeclaration
#### ReopenBusinessDayAsync — Lines 293-295
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:293:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:293:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReopenBusinessDayAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest, CancellationToken)
- **Lines:** [293-295]
- **Behavior:** Executes HTTP POST to `$"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId`. Request payload: `Falah.Application.BusinessDay.ReopenBusinessDayRequest`. Response: `Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>> ReopenBusinessDayAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.BusinessDayDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/reopen", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:294:65:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 294-295
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:294:65:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:294:65:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [294-295]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:297:5:MethodDeclaration
#### SetExceptionStatusAsync — Lines 297-299
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:297:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:297:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetExceptionStatusAsync(Falah.Application.BusinessDay.SetExceptionStatusRequest, CancellationToken)
- **Lines:** [297-299]
- **Behavior:** Executes HTTP POST to `$"/api/control-exceptions/{Uri.EscapeDataString(request.ExceptionId`. Request payload: `Falah.Application.BusinessDay.SetExceptionStatusRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:298:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 298-299
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:298:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:298:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [298-299]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/control-exceptions/{Uri.EscapeDataString(request.ExceptionId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:301:5:MethodDeclaration
#### GetBusinessDayReconciliationAsync — Lines 301-303
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:301:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:301:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayReconciliationAsync(string, string, CancellationToken)
- **Lines:** [301-303]
- **Behavior:** Executes HTTP GET to `$"/api/reports/business-day/{Uri.EscapeDataString(businessDayId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.BusinessDayReconciliationDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Reports.BusinessDayReconciliationDto>> GetBusinessDayReconciliationAsync(string businessDayId, string actorUserId, CancellationToken ct = default) =>
        ReadAsync<Falah.Application.Reports.BusinessDayReconciliationDto>(() => _http.GetAsync(
            $"/api/reports/business-day/{Uri.EscapeDataString(businessDayId)}?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:302:75:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 302-303
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:302:75:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:302:75:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [302-303]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/business-day/{Uri.EscapeDataString(businessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:305:5:MethodDeclaration
#### GetDailySummaryAsync — Lines 305-307
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:305:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:305:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDailySummaryAsync(string, string, CancellationToken)
- **Lines:** [305-307]
- **Behavior:** Executes HTTP GET to `$"/api/reports/daily-summary/{Uri.EscapeDataString(businessDayId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.DailySummaryDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:306:62:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 306-307
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:306:62:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:306:62:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [306-307]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/daily-summary/{Uri.EscapeDataString(businessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:311:5:MethodDeclaration
#### GetDailyStatementAsync — Lines 311-315
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:311:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:311:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDailyStatementAsync(string, DateOnly?, CancellationToken)
- **Lines:** [311-315]
- **Behavior:** Executes HTTP GET to `$"/api/reports/daily-statement?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.DailyStatementDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:313:64:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 313-315
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:313:64:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:313:64:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [313-315]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/daily-statement?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:317:5:MethodDeclaration
#### GetLatestCloseSnapshotAsync — Lines 317-319
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:317:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:317:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetLatestCloseSnapshotAsync(string, CancellationToken)
- **Lines:** [317-319]
- **Behavior:** Executes HTTP GET to `$"/api/business-day/{Uri.EscapeDataString(businessDayId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:318:78:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 318-319
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:318:78:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:318:78:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [318-319]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/business-day/{Uri.EscapeDataString(businessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:321:5:MethodDeclaration
#### GetCloseSnapshotsAsync — Lines 321-323
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:321:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:321:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCloseSnapshotsAsync(string, CancellationToken)
- **Lines:** [321-323]
- **Behavior:** Executes HTTP GET to `$"/api/business-day/{Uri.EscapeDataString(businessDayId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:322:93:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 322-323
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:322:93:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:322:93:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [322-323]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/business-day/{Uri.EscapeDataString(businessDayId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:325:5:MethodDeclaration
#### GetPrintingOverviewAsync — Lines 325-326
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:325:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:325:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPrintingOverviewAsync(CancellationToken)
- **Lines:** [325-326]
- **Behavior:** Executes HTTP GET to `/api/print-jobs/overview`. Request payload: `None`. Response: `Task<ApiResult<PrintingOverviewDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:326:40:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 326-326
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:326:40:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:326:40:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [326-326]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/print-jobs/overview"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:328:5:MethodDeclaration
#### GetPendingPrintJobsAsync — Lines 328-330
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:328:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:328:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPendingPrintJobsAsync(string, CancellationToken)
- **Lines:** [328-330]
- **Behavior:** Executes HTTP GET to `$"/api/print-jobs/pending?route={Uri.EscapeDataString(route`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<PrintJobDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:329:47:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 329-330
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:329:47:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:329:47:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [329-330]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/print-jobs/pending?route={Uri.EscapeDataString(route` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:334:5:MethodDeclaration
#### CreateUserAsync — Lines 334-335
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:334:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:334:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateUserAsync(Falah.Application.Users.CreateUserRequest, CancellationToken)
- **Lines:** [334-335]
- **Behavior:** Executes HTTP POST to `/api/users`. Request payload: `Falah.Application.Users.CreateUserRequest`. Response: `Task<ApiResult<CreateUserResult>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<CreateUserResult>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default) =>
        SendAsync<CreateUserResult>(() => _http.PostAsJsonAsync("/api/users", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:335:37:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 335-335
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:335:37:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:335:37:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [335-335]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/users"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:337:5:MethodDeclaration
#### ChangePinAsync — Lines 337-339
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:337:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:337:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ChangePinAsync(Falah.Application.Users.ChangePinRequest, CancellationToken)
- **Lines:** [337-339]
- **Behavior:** Executes HTTP POST to `$"/api/users/{Uri.EscapeDataString(request.UserId`. Request payload: `Falah.Application.Users.ChangePinRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:338:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 338-339
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:338:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:338:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [338-339]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/users/{Uri.EscapeDataString(request.UserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:341:5:MethodDeclaration
#### GetAllUsersAsync — Lines 341-343
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:341:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:341:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllUsersAsync(string, CancellationToken)
- **Lines:** [341-343]
- **Behavior:** Executes HTTP GET to `$"/api/users/all?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<UserDetailsDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<UserDetailsDto>>> GetAllUsersAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<UserDetailsDto>>(() => _http.GetAsync(
            $"/api/users/all?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:342:50:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 342-343
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:342:50:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:342:50:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [342-343]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/users/all?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:345:5:MethodDeclaration
#### GetPermissionCatalogAsync — Lines 345-346
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:345:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:345:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPermissionCatalogAsync(CancellationToken)
- **Lines:** [345-346]
- **Behavior:** Executes HTTP GET to `/api/permissions/catalog`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Domain.Authorization.PermissionDefinition>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:346:83:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 346-346
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:346:83:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:346:83:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [346-346]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/permissions/catalog"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:348:5:MethodDeclaration
#### SetUserActiveAsync — Lines 348-350
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:348:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:348:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetUserActiveAsync(Falah.Application.Users.SetUserActiveRequest, CancellationToken)
- **Lines:** [348-350]
- **Behavior:** Executes HTTP POST to `$"/api/users/{Uri.EscapeDataString(request.UserId`. Request payload: `Falah.Application.Users.SetUserActiveRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<bool>> SetUserActiveAsync(SetUserActiveRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/users/{Uri.EscapeDataString(request.UserId)}/active", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:349:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 349-350
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:349:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:349:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [349-350]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/users/{Uri.EscapeDataString(request.UserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:352:5:MethodDeclaration
#### UpdateUserPermissionsAsync — Lines 352-354
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:352:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:352:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateUserPermissionsAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken)
- **Lines:** [352-354]
- **Behavior:** Executes HTTP POST to `$"/api/users/{Uri.EscapeDataString(request.UserId`. Request payload: `Falah.Application.Users.UpdateUserPermissionsRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<bool>> UpdateUserPermissionsAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/users/{Uri.EscapeDataString(request.UserId)}/permissions", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:353:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 353-354
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:353:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:353:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [353-354]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/users/{Uri.EscapeDataString(request.UserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:356:5:MethodDeclaration
#### GetAuditEventsAsync — Lines 356-359
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:356:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:356:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAuditEventsAsync(string, string?, string?, int, CancellationToken)
- **Lines:** [356-359]
- **Behavior:** Executes HTTP GET to `$"/api/audit/events?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Audit.AuditEventDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:358:73:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 358-359
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:358:73:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:358:73:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [358-359]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/audit/events?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:363:5:MethodDeclaration
#### GetEmployeesAsync — Lines 363-364
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:363:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:363:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeesAsync(CancellationToken)
- **Lines:** [363-364]
- **Behavior:** Executes HTTP GET to `/api/employees`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeDto>>> GetEmployeesAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.Employees.EmployeeDto>>(() => _http.GetAsync("/api/employees", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:364:75:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 364-364
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:364:75:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:364:75:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [364-364]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/employees"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:368:5:MethodDeclaration
#### GetEmployeeBalanceAsync — Lines 368-369
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:368:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:368:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeBalanceAsync(string, CancellationToken)
- **Lines:** [368-369]
- **Behavior:** Executes HTTP GET to `$"/api/employees/{Uri.EscapeDataString(employeeId`. Request payload: `None`. Response: `Task<ApiResult<EmployeeBalanceDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<EmployeeBalanceDto>> GetEmployeeBalanceAsync(string employeeId, CancellationToken ct = default) =>
        ReadAsync<EmployeeBalanceDto>(() => _http.GetAsync($"/api/employees/{Uri.EscapeDataString(employeeId)}/balance", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:369:39:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 369-369
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:369:39:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:369:39:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [369-369]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/employees/{Uri.EscapeDataString(employeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:371:5:MethodDeclaration
#### GetEmployeeLedgerAsync — Lines 371-373
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:371:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:371:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeLedgerAsync(string, CancellationToken)
- **Lines:** [371-373]
- **Behavior:** Executes HTTP GET to `$"/api/employees/{Uri.EscapeDataString(employeeId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeLedgerEntryDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeLedgerEntryDto>>> GetEmployeeLedgerAsync(string employeeId, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.Employees.EmployeeLedgerEntryDto>>(() => _http.GetAsync(
            $"/api/employees/{Uri.EscapeDataString(employeeId)}/ledger", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:372:86:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 372-373
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:372:86:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:372:86:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [372-373]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/employees/{Uri.EscapeDataString(employeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:375:5:MethodDeclaration
#### EmployeeCashOutAsync — Lines 375-377
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:375:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:375:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.EmployeeCashOutAsync(Falah.Application.Employees.RecordEmployeeCashOutRequest, CancellationToken)
- **Lines:** [375-377]
- **Behavior:** Executes HTTP POST to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId`. Request payload: `Falah.Application.Employees.RecordEmployeeCashOutRequest`. Response: `Task<ApiResult<Falah.Application.Employees.EmployeeCashOutResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Employees.EmployeeCashOutResult>> EmployeeCashOutAsync(Falah.Application.Employees.RecordEmployeeCashOutRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.EmployeeCashOutResult>(() => _http.PostAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}/cash-out", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:376:70:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 376-377
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:376:70:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:376:70:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [376-377]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:381:5:MethodDeclaration
#### GetEmployeeFinancialReportAsync — Lines 381-385
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:381:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:381:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetEmployeeFinancialReportAsync(string, DateOnly, DateOnly, CancellationToken)
- **Lines:** [381-385]
- **Behavior:** Executes HTTP GET to `$"/api/employees/financial-report?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeFinancialReportLineDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeFinancialReportLineDto>>> GetEmployeeFinancialReportAsync(
        string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.Employees.EmployeeFinancialReportLineDto>>(() => _http.GetAsync(
            $"/api/employees/financial-report?actorUserId={Uri.EscapeDataString(actorUserId)}" +
            $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:383:94:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 383-385
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:383:94:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:383:94:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [383-385]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/employees/financial-report?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:390:5:MethodDeclaration
#### PayEmployeeAsync — Lines 390-392
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:390:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:390:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PayEmployeeAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken)
- **Lines:** [390-392]
- **Behavior:** Executes HTTP POST to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId`. Request payload: `Falah.Application.Employees.PayEmployeeRequest`. Response: `Task<ApiResult<Falah.Application.Employees.PayEmployeeResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Employees.PayEmployeeResult>> PayEmployeeAsync(Falah.Application.Employees.PayEmployeeRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.PayEmployeeResult>(() => _http.PostAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}/pay", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:391:66:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 391-392
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:391:66:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:391:66:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [391-392]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:394:5:MethodDeclaration
#### RecordDailyWageAsync — Lines 394-396
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:394:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:394:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordDailyWageAsync(Falah.Application.Employees.RecordDailyWageRequest, CancellationToken)
- **Lines:** [394-396]
- **Behavior:** Executes HTTP POST to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId`. Request payload: `Falah.Application.Employees.RecordDailyWageRequest`. Response: `Task<ApiResult<Falah.Application.Employees.EmployeeLedgerEntryDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:395:71:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 395-396
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:395:71:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:395:71:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [395-396]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:398:5:MethodDeclaration
#### RecordOvertimeAsync — Lines 398-400
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:398:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:398:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken)
- **Lines:** [398-400]
- **Behavior:** Executes HTTP POST to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId`. Request payload: `Falah.Application.Employees.RecordOvertimeRequest`. Response: `Task<ApiResult<Falah.Application.Employees.EmployeeLedgerEntryDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:399:71:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 399-400
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:399:71:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:399:71:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [399-400]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:402:5:MethodDeclaration
#### RecordExtraShiftAsync — Lines 402-404
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:402:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:402:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordExtraShiftAsync(Falah.Application.Employees.RecordExtraShiftRequest, CancellationToken)
- **Lines:** [402-404]
- **Behavior:** Executes HTTP POST to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId`. Request payload: `Falah.Application.Employees.RecordExtraShiftRequest`. Response: `Task<ApiResult<Falah.Application.Employees.EmployeeLedgerEntryDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Employees.EmployeeLedgerEntryDto>> RecordExtraShiftAsync(Falah.Application.Employees.RecordExtraShiftRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.EmployeeLedgerEntryDto>(() => _http.PostAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}/extra-shift", request, ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:403:71:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 403-404
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:403:71:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:403:71:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [403-404]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/employees/{Uri.EscapeDataString(request.EmployeeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:406:5:MethodDeclaration
#### GetAttendanceAsync — Lines 406-408
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:406:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:406:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAttendanceAsync(DateOnly?, string?, CancellationToken)
- **Lines:** [406-408]
- **Behavior:** Executes HTTP GET to `$"/api/attendance?employeeId={Uri.EscapeDataString(employeeId ?? `. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Employees.AttendanceRecordDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:407:83:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 407-408
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:407:83:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:407:83:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [407-408]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/attendance?employeeId={Uri.EscapeDataString(employeeId ?? ""` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:410:5:MethodDeclaration
#### CorrectAttendanceAsync — Lines 410-412
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:410:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:410:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CorrectAttendanceAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken)
- **Lines:** [410-412]
- **Behavior:** Executes HTTP POST to `$"/api/attendance/{Uri.EscapeDataString(request.RecordId`. Request payload: `Falah.Application.Employees.CorrectAttendanceRequest`. Response: `Task<ApiResult<Falah.Application.Employees.AttendanceRecordDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:411:68:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 411-412
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:411:68:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:411:68:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [411-412]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/attendance/{Uri.EscapeDataString(request.RecordId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:414:5:MethodDeclaration
#### GetPayrollRunsAsync — Lines 414-415
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:414:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:414:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollRunsAsync(CancellationToken)
- **Lines:** [414-415]
- **Behavior:** Executes HTTP GET to `/api/payroll-runs`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Employees.PayrollRunDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:415:77:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 415-415
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:415:77:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:415:77:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [415-415]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/payroll-runs"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:417:5:MethodDeclaration
#### GetPayrollLinesAsync — Lines 417-419
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:417:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:417:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollLinesAsync(string, CancellationToken)
- **Lines:** [417-419]
- **Behavior:** Executes HTTP GET to `$"/api/payroll-runs/{Uri.EscapeDataString(runId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Employees.PayrollLineDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:418:78:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 418-419
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:418:78:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:418:78:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [418-419]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/payroll-runs/{Uri.EscapeDataString(runId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:421:5:MethodDeclaration
#### CreatePayrollRunAsync — Lines 421-422
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:421:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:421:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreatePayrollRunAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken)
- **Lines:** [421-422]
- **Behavior:** Executes HTTP POST to `/api/payroll-runs`. Request payload: `Falah.Application.Employees.CreatePayrollRequest`. Response: `Task<ApiResult<Falah.Application.Employees.PayrollRunDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:422:62:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 422-422
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:422:62:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:422:62:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [422-422]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/payroll-runs"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:424:5:MethodDeclaration
#### TransitionPayrollAsync — Lines 424-426
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:424:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:424:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TransitionPayrollAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken)
- **Lines:** [424-426]
- **Behavior:** Executes HTTP POST to `$"/api/payroll-runs/{Uri.EscapeDataString(request.PayrollRunId`. Request payload: `Falah.Application.Employees.TransitionPayrollRequest`. Response: `Task<ApiResult<Falah.Application.Employees.PayrollRunDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:425:62:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 425-426
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:425:62:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:425:62:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [425-426]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/payroll-runs/{Uri.EscapeDataString(request.PayrollRunId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:428:5:MethodDeclaration
#### GetPayrollRunAsync — Lines 428-430
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:428:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:428:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayrollRunAsync(string, CancellationToken)
- **Lines:** [428-430]
- **Behavior:** Executes HTTP GET to `$"/api/payroll-runs/{Uri.EscapeDataString(runId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Employees.PayrollRunDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:429:62:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 429-430
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:429:62:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:429:62:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [429-430]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/payroll-runs/{Uri.EscapeDataString(runId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:432:5:MethodDeclaration
#### PayPayrollAsync — Lines 432-434
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:432:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:432:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PayPayrollAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken)
- **Lines:** [432-434]
- **Behavior:** Executes HTTP POST to `$"/api/payroll-runs/{Uri.EscapeDataString(request.PayrollRunId`. Request payload: `Falah.Application.Employees.PayPayrollRequest`. Response: `Task<ApiResult<Falah.Application.Employees.PayPayrollResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:433:65:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 433-434
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:433:65:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:433:65:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [433-434]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/payroll-runs/{Uri.EscapeDataString(request.PayrollRunId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:438:5:MethodDeclaration
#### GetRecipesAsync — Lines 438-439
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:438:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:438:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipesAsync(CancellationToken)
- **Lines:** [438-439]
- **Behavior:** Executes HTTP GET to `/api/recipes`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.RecipeDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:439:73:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 439-439
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:439:73:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:439:73:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [439-439]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/recipes"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:441:5:MethodDeclaration
#### GetRecipeCostAsync — Lines 441-443
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:441:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:441:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostAsync(string, string, CancellationToken)
- **Lines:** [441-443]
- **Behavior:** Executes HTTP GET to `$"/api/recipes/{Uri.EscapeDataString(recipeId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.RecipeCostDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:442:62:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 442-443
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:442:62:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:442:62:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [442-443]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/recipes/{Uri.EscapeDataString(recipeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:445:5:MethodDeclaration
#### GetRecipeProfitabilityAsync — Lines 445-447
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:445:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:445:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeProfitabilityAsync(string, long, string, CancellationToken)
- **Lines:** [445-447]
- **Behavior:** Executes HTTP GET to `$"/api/recipes/{Uri.EscapeDataString(recipeId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.MenuItemProfitabilityDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:446:73:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 446-447
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:446:73:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:446:73:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [446-447]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/recipes/{Uri.EscapeDataString(recipeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:452:5:MethodDeclaration
#### GetRecipeCostSummariesAsync — Lines 452-454
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:452:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:452:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostSummariesAsync(string, CancellationToken)
- **Lines:** [452-454]
- **Behavior:** Executes HTTP GET to `$"/api/recipes/cost-summary?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.RecipeCostSummaryDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:453:84:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 453-454
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:453:84:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:453:84:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [453-454]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/recipes/cost-summary?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:456:5:MethodDeclaration
#### GetRecipeCostCoverageAsync — Lines 456-458
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:456:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:456:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostCoverageAsync(string, CancellationToken)
- **Lines:** [456-458]
- **Behavior:** Executes HTTP GET to `$"/api/recipes/cost-coverage?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.RecipeCostCoverageDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:457:70:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 457-458
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:457:70:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:457:70:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [457-458]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/recipes/cost-coverage?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:460:5:MethodDeclaration
#### GetRecipeCostDetailAsync — Lines 460-462
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:460:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:460:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRecipeCostDetailAsync(string, string, CancellationToken)
- **Lines:** [460-462]
- **Behavior:** Executes HTTP GET to `$"/api/recipes/{Uri.EscapeDataString(recipeId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.RecipeCostDetailDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:461:68:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 461-462
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:461:68:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:461:68:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [461-462]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/recipes/{Uri.EscapeDataString(recipeId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:464:5:MethodDeclaration
#### SaveRecipeAsync — Lines 464-465
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:464:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:464:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SaveRecipeAsync(Falah.Application.Inventory.SaveRecipeRequest, CancellationToken)
- **Lines:** [464-465]
- **Behavior:** Executes HTTP POST to `/api/recipes`. Request payload: `Falah.Application.Inventory.SaveRecipeRequest`. Response: `Task<ApiResult<Falah.Application.Inventory.RecipeDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:465:58:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 465-465
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:465:58:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:465:58:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [465-465]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/recipes"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:467:5:MethodDeclaration
#### GetLossSummaryAsync — Lines 467-469
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:467:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:467:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetLossSummaryAsync(string, DateOnly, CancellationToken)
- **Lines:** [467-469]
- **Behavior:** Executes HTTP GET to `$"/api/inventory/loss-summary?businessDate={businessDate:yyyy-MM-dd}&actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.LossSummaryDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:468:63:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 468-469
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:468:63:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:468:63:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [468-469]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/inventory/loss-summary?businessDate={businessDate:yyyy-MM-dd}&actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:473:5:MethodDeclaration
#### GetSuppliersAsync — Lines 473-474
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:473:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:473:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSuppliersAsync(CancellationToken)
- **Lines:** [473-474]
- **Behavior:** Executes HTTP GET to `/api/suppliers`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Purchasing.SupplierDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:474:76:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 474-474
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:474:76:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:474:76:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [474-474]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/suppliers"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:476:5:MethodDeclaration
#### AddSupplierAsync — Lines 476-477
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:476:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:476:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.AddSupplierAsync(Falah.Application.Purchasing.AddSupplierRequest, CancellationToken)
- **Lines:** [476-477]
- **Behavior:** Executes HTTP POST to `/api/suppliers`. Request payload: `Falah.Application.Purchasing.AddSupplierRequest`. Response: `Task<ApiResult<Falah.Application.Purchasing.AddSupplierResult>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:477:67:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 477-477
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:477:67:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:477:67:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [477-477]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/suppliers"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:479:5:MethodDeclaration
#### GetSupplierLedgerAsync — Lines 479-481
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:479:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:479:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSupplierLedgerAsync(string, CancellationToken)
- **Lines:** [479-481]
- **Behavior:** Executes HTTP GET to `$"/api/suppliers/{Uri.EscapeDataString(supplierId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Purchasing.SupplierLedgerEntryDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:480:87:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 480-481
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:480:87:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:480:87:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [480-481]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/suppliers/{Uri.EscapeDataString(supplierId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:483:5:MethodDeclaration
#### GetPurchasesAsync — Lines 483-484
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:483:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:483:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPurchasesAsync(CancellationToken)
- **Lines:** [483-484]
- **Behavior:** Executes HTTP GET to `/api/purchases`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Purchasing.PurchaseDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:484:76:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 484-484
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:484:76:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:484:76:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [484-484]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/purchases"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:486:5:MethodDeclaration
#### ReceivePurchaseAsync — Lines 486-487
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:486:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:486:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReceivePurchaseAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken)
- **Lines:** [486-487]
- **Behavior:** Executes HTTP POST to `/api/purchases`. Request payload: `Falah.Application.Purchasing.ReceivePurchaseRequest`. Response: `Task<ApiResult<Falah.Application.Purchasing.PurchaseDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:487:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 487-487
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:487:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:487:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [487-487]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/purchases"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:489:5:MethodDeclaration
#### PayPurchaseAsync — Lines 489-491
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:489:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:489:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.PayPurchaseAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken)
- **Lines:** [489-491]
- **Behavior:** Executes HTTP POST to `$"/api/purchases/{Uri.EscapeDataString(request.PurchaseId`. Request payload: `Falah.Application.Purchasing.PayPurchaseRequest`. Response: `Task<ApiResult<Falah.Application.Purchasing.PurchaseDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:490:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 490-491
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:490:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:490:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [490-491]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/purchases/{Uri.EscapeDataString(request.PurchaseId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:493:5:MethodDeclaration
#### GetPayablesAgingAsync — Lines 493-494
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:493:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:493:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetPayablesAgingAsync(CancellationToken)
- **Lines:** [493-494]
- **Behavior:** Executes HTTP GET to `/api/purchases/payables-aging`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Purchasing.PayablesAgingDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:494:66:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 494-494
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:494:66:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:494:66:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [494-494]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/purchases/payables-aging"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:496:5:MethodDeclaration
#### SetPurchaseDueDateAsync — Lines 496-498
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:496:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:496:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetPurchaseDueDateAsync(Falah.Application.Purchasing.SetPurchaseDueDateRequest, CancellationToken)
- **Lines:** [496-498]
- **Behavior:** Executes HTTP PUT to `$"/api/purchases/{Uri.EscapeDataString(request.PurchaseId`. Request payload: `Falah.Application.Purchasing.SetPurchaseDueDateRequest`. Response: `Task<ApiResult<Falah.Application.Purchasing.PurchaseDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:497:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 497-498
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:497:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:497:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [497-498]
- **Behavior:** HTTP invocation delegate: dispatches HTTP PUT request to `$"/api/purchases/{Uri.EscapeDataString(request.PurchaseId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:502:5:MethodDeclaration
#### GetTreasuryPositionAsync — Lines 502-504
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:502:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:502:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryPositionAsync(string, CancellationToken)
- **Lines:** [502-504]
- **Behavior:** Executes HTTP GET to `$"/api/treasury/position?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<TreasuryPositionDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:503:40:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 503-504
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:503:40:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:503:40:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [503-504]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/treasury/position?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:506:5:MethodDeclaration
#### GetTreasuryBalancesAsync — Lines 506-508
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:506:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:506:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryBalancesAsync(string, CancellationToken)
- **Lines:** [506-508]
- **Behavior:** Executes HTTP GET to `$"/api/treasury/balances?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<TreasuryAccountBalanceDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:507:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 507-508
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:507:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:507:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [507-508]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/treasury/balances?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:510:5:MethodDeclaration
#### GetTreasuryMovementsAsync — Lines 510-512
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:510:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:510:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryMovementsAsync(string, string?, int, CancellationToken)
- **Lines:** [510-512]
- **Behavior:** Executes HTTP GET to `$"/api/treasury/movements?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<TreasuryMovementDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:511:55:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 511-512
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:511:55:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:511:55:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [511-512]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/treasury/movements?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:514:5:MethodDeclaration
#### TreasuryDepositAsync — Lines 514-515
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:514:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:514:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TreasuryDepositAsync(Falah.Application.Treasury.DepositRequest, CancellationToken)
- **Lines:** [514-515]
- **Behavior:** Executes HTTP POST to `/api/treasury/deposit`. Request payload: `Falah.Application.Treasury.DepositRequest`. Response: `Task<ApiResult<DepositResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:515:34:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 515-515
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:515:34:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:515:34:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [515-515]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/treasury/deposit"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:517:5:MethodDeclaration
#### TreasuryWithdrawAsync — Lines 517-518
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:517:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:517:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TreasuryWithdrawAsync(Falah.Application.Treasury.WithdrawalRequest, CancellationToken)
- **Lines:** [517-518]
- **Behavior:** Executes HTTP POST to `/api/treasury/withdraw`. Request payload: `Falah.Application.Treasury.WithdrawalRequest`. Response: `Task<ApiResult<WithdrawalResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:518:37:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 518-518
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:518:37:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:518:37:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [518-518]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/treasury/withdraw"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:520:5:MethodDeclaration
#### TreasuryTransferAsync — Lines 520-521
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:520:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:520:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.TreasuryTransferAsync(Falah.Application.Treasury.TransferRequest, CancellationToken)
- **Lines:** [520-521]
- **Behavior:** Executes HTTP POST to `/api/treasury/transfer`. Request payload: `Falah.Application.Treasury.TransferRequest`. Response: `Task<ApiResult<TransferResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:521:35:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 521-521
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:521:35:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:521:35:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [521-521]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/treasury/transfer"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:525:5:MethodDeclaration
#### GetInventoryItemsAsync — Lines 525-526
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:525:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:525:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryItemsAsync(CancellationToken)
- **Lines:** [525-526]
- **Behavior:** Executes HTTP GET to `/api/inventory/items`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.InventoryItemDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:526:80:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 526-526
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:526:80:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:526:80:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [526-526]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/inventory/items"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:528:5:MethodDeclaration
#### CreateInventoryItemAsync — Lines 528-529
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:528:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:528:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateInventoryItemAsync(Falah.Application.Inventory.CreateInventoryItemRequest, CancellationToken)
- **Lines:** [528-529]
- **Behavior:** Executes HTTP POST to `/api/inventory/items`. Request payload: `Falah.Application.Inventory.CreateInventoryItemRequest`. Response: `Task<ApiResult<Falah.Application.Inventory.CreateInventoryItemResult>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:529:74:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 529-529
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:529:74:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:529:74:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [529-529]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/inventory/items"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:531:5:MethodDeclaration
#### UpdateInventoryItemAsync — Lines 531-532
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:531:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:531:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateInventoryItemAsync(Falah.Application.Inventory.UpdateInventoryItemRequest, CancellationToken)
- **Lines:** [531-532]
- **Behavior:** Executes HTTP PUT to `$"/api/inventory/items/{Uri.EscapeDataString(request.Id`. Request payload: `Falah.Application.Inventory.UpdateInventoryItemRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:532:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 532-532
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:532:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:532:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [532-532]
- **Behavior:** HTTP invocation delegate: dispatches HTTP PUT request to `$"/api/inventory/items/{Uri.EscapeDataString(request.Id` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:534:5:MethodDeclaration
#### GetInventoryOverviewAsync — Lines 534-536
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:534:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:534:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryOverviewAsync(string, CancellationToken)
- **Lines:** [534-536]
- **Behavior:** Executes HTTP GET to `$"/api/inventory/overview?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.InventoryOverviewDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:535:69:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 535-536
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:535:69:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:535:69:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [535-536]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/inventory/overview?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:538:5:MethodDeclaration
#### GetInventoryItemDetailAsync — Lines 538-540
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:538:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:538:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetInventoryItemDetailAsync(string, string, CancellationToken)
- **Lines:** [538-540]
- **Behavior:** Executes HTTP GET to `$"/api/inventory/items/{Uri.EscapeDataString(inventoryItemId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Inventory.InventoryItemDetailDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:539:71:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 539-540
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:539:71:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:539:71:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [539-540]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/inventory/items/{Uri.EscapeDataString(inventoryItemId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:542:5:MethodDeclaration
#### RecordCountAdjustmentAsync — Lines 542-543
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:542:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:542:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordCountAdjustmentAsync(Falah.Application.Inventory.RecordCountAdjustmentRequest, CancellationToken)
- **Lines:** [542-543]
- **Behavior:** Executes HTTP POST to `/api/inventory/count-adjustments`. Request payload: `Falah.Application.Inventory.RecordCountAdjustmentRequest`. Response: `Task<ApiResult<Falah.Application.Inventory.RecordCountAdjustmentResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:543:76:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 543-543
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:543:76:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:543:76:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [543-543]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/inventory/count-adjustments"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:545:5:MethodDeclaration
#### RecordWasteAsync — Lines 545-546
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:545:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:545:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordWasteAsync(Falah.Application.Inventory.RecordWasteRequest, CancellationToken)
- **Lines:** [545-546]
- **Behavior:** Executes HTTP POST to `/api/inventory/waste`. Request payload: `Falah.Application.Inventory.RecordWasteRequest`. Response: `Task<ApiResult<Falah.Application.Inventory.RecordWasteResult>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:546:66:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 546-546
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:546:66:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:546:66:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [546-546]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/inventory/waste"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:548:5:MethodDeclaration
#### GetStockMovementsAsync — Lines 548-550
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:548:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:548:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetStockMovementsAsync(string?, int, CancellationToken)
- **Lines:** [548-550]
- **Behavior:** Executes HTTP GET to `$"/api/inventory/movements?inventoryItemId={Uri.EscapeDataString(inventoryItemId ?? `. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.StockMovementDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:549:80:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 549-550
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:549:80:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:549:80:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [549-550]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/inventory/movements?inventoryItemId={Uri.EscapeDataString(inventoryItemId ?? ""` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:557:5:MethodDeclaration
#### Day — Lines 557-557
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:557:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:557:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.Day(DateOnly?)
- **Lines:** [557-557]
- **Behavior:** Executes HTTP GET to ``. Request payload: `None`. Response: `Task<ApiResult<...>>`. Error handling: executes via `standard async HTTP dispatch`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:566:5:MethodDeclaration
#### GetSalesReportAsync — Lines 566-569
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:566:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:566:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSalesReportAsync(string, DateOnly?, DateOnly?, string?, CancellationToken)
- **Lines:** [566-569]
- **Behavior:** Executes HTTP GET to `$"/api/reports/sales?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.SalesReportDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:567:61:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 567-569
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:567:61:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:567:61:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [567-569]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/sales?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:571:5:MethodDeclaration
#### GetExpenseReportAsync — Lines 571-573
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:571:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:571:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetExpenseReportAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [571-573]
- **Behavior:** Executes HTTP GET to `$"/api/reports/expenses?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.ExpenseReportDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:572:63:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 572-573
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:572:63:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:572:63:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [572-573]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/expenses?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:575:5:MethodDeclaration
#### GetTaxReportAsync — Lines 575-577
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:575:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:575:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTaxReportAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [575-577]
- **Behavior:** Executes HTTP GET to `$"/api/reports/tax?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.TaxReportDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:576:59:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 576-577
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:576:59:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:576:59:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [576-577]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/tax?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:579:5:MethodDeclaration
#### GetTopItemsAsync — Lines 579-581
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:579:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:579:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTopItemsAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [579-581]
- **Behavior:** Executes HTTP GET to `$"/api/reports/top-items?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Reports.TopSellingItemDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:580:79:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 580-581
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:580:79:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:580:79:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [580-581]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/top-items?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:583:5:MethodDeclaration
#### GetProductHourlySalesAsync — Lines 583-585
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:583:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:583:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetProductHourlySalesAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [583-585]
- **Behavior:** Executes HTTP GET to `$"/api/reports/product-hourly?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Reports.ProductHourlySalesDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:584:83:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 584-585
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:584:83:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:584:83:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [584-585]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/product-hourly?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:587:5:MethodDeclaration
#### GetCategorySalesAsync — Lines 587-589
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:587:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:587:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCategorySalesAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [587-589]
- **Behavior:** Executes HTTP GET to `$"/api/reports/category-sales?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Reports.CategorySalesDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:588:78:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 588-589
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:588:78:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:588:78:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [588-589]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/category-sales?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:591:5:MethodDeclaration
#### GetDeliveryOutcomesAsync — Lines 591-593
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:591:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:591:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDeliveryOutcomesAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [591-593]
- **Behavior:** Executes HTTP GET to `$"/api/reports/delivery-outcomes?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.DeliveryOutcomesReportDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:592:72:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 592-593
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:592:72:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:592:72:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [592-593]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/delivery-outcomes?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:595:5:MethodDeclaration
#### GetSignificantActionsAsync — Lines 595-597
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:595:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:595:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetSignificantActionsAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [595-597]
- **Behavior:** Executes HTTP GET to `$"/api/reports/significant-actions?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Reports.SignificantActionDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:596:82:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 596-597
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:596:82:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:596:82:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [596-597]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/significant-actions?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:599:5:MethodDeclaration
#### GetShiftsHistoryAsync — Lines 599-601
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:599:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:599:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetShiftsHistoryAsync(string, CancellationToken)
- **Lines:** [599-601]
- **Behavior:** Executes HTTP GET to `$"/api/reports/shifts?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Reports.ShiftSummaryDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Reports.ShiftSummaryDto>>> GetShiftsHistoryAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<Falah.Application.Reports.ShiftSummaryDto>>(() => _http.GetAsync(
            $"/api/reports/shifts?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:600:77:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 600-601
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:600:77:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:600:77:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [600-601]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/shifts?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:603:5:MethodDeclaration
#### GetShiftSummaryAsync — Lines 603-605
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:603:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:603:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetShiftSummaryAsync(string, string, CancellationToken)
- **Lines:** [603-605]
- **Behavior:** Executes HTTP GET to `$"/api/reports/shifts/{Uri.EscapeDataString(shiftId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.ShiftSummaryDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Reports.ShiftSummaryDto>> GetShiftSummaryAsync(string shiftId, string actorUserId, CancellationToken ct = default) =>
        ReadAsync<Falah.Application.Reports.ShiftSummaryDto>(() => _http.GetAsync(
            $"/api/reports/shifts/{Uri.EscapeDataString(shiftId)}?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:604:62:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 604-605
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:604:62:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:604:62:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [604-605]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/shifts/{Uri.EscapeDataString(shiftId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:607:5:MethodDeclaration
#### GetRefundActivityAsync — Lines 607-609
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:607:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:607:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetRefundActivityAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [607-609]
- **Behavior:** Executes HTTP GET to `$"/api/reports/refunds?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<Falah.Application.Reports.RefundActivityDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:608:79:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 608-609
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:608:79:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:608:79:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [608-609]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/refunds?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:611:5:MethodDeclaration
#### GetFoodCostReportAsync — Lines 611-613
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:611:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:611:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetFoodCostReportAsync(string, DateOnly?, DateOnly?, CancellationToken)
- **Lines:** [611-613]
- **Behavior:** Executes HTTP GET to `$"/api/reports/food-cost?actorUserId={Uri.EscapeDataString(actorUserId`. Request payload: `None`. Response: `Task<ApiResult<Falah.Application.Reports.FoodCostReportDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:612:64:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 612-613
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:612:64:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:612:64:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [612-613]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/reports/food-cost?actorUserId={Uri.EscapeDataString(actorUserId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:617:5:MethodDeclaration
#### GetActiveDriversAsync — Lines 617-618
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:617:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:617:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetActiveDriversAsync(CancellationToken)
- **Lines:** [617-618]
- **Behavior:** Executes HTTP GET to `/api/delivery/drivers`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<DriverDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:618:45:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 618-618
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:618:45:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:618:45:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [618-618]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/delivery/drivers"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:620:5:MethodDeclaration
#### GetAllDriversAsync — Lines 620-621
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:620:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:620:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllDriversAsync(CancellationToken)
- **Lines:** [620-621]
- **Behavior:** Executes HTTP GET to `/api/delivery/drivers/all`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<DriverDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:621:45:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 621-621
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:621:45:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:621:45:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [621-621]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/delivery/drivers/all"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:623:5:MethodDeclaration
#### CreateDriverAsync — Lines 623-624
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:623:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:623:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateDriverAsync(Falah.Application.Delivery.CreateDriverRequest, CancellationToken)
- **Lines:** [623-624]
- **Behavior:** Executes HTTP POST to `/api/delivery/drivers`. Request payload: `Falah.Application.Delivery.CreateDriverRequest`. Response: `Task<ApiResult<DriverDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:624:30:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 624-624
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:624:30:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:624:30:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [624-624]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/delivery/drivers"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:626:5:MethodDeclaration
#### SetDriverActiveAsync — Lines 626-627
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:626:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:626:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SetDriverActiveAsync(Falah.Application.Delivery.SetDriverActiveRequest, CancellationToken)
- **Lines:** [626-627]
- **Behavior:** Executes HTTP POST to `$"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId`. Request payload: `Falah.Application.Delivery.SetDriverActiveRequest`. Response: `Task<ApiResult<DriverDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:627:30:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 627-627
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:627:30:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:627:30:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [627-627]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:631:5:MethodDeclaration
#### GetDispatchBoardAsync — Lines 631-632
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:631:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:631:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDispatchBoardAsync(CancellationToken)
- **Lines:** [631-632]
- **Behavior:** Executes HTTP GET to `/api/delivery/board`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<DeliveryDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:632:47:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 632-632
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:632:47:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:632:47:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [632-632]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `"/api/delivery/board"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:634:5:MethodDeclaration
#### MarkDeliveryReadyAsync — Lines 634-635
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:634:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:634:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.MarkDeliveryReadyAsync(Falah.Application.Delivery.MarkDeliveryReadyRequest, CancellationToken)
- **Lines:** [634-635]
- **Behavior:** Executes HTTP POST to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId`. Request payload: `Falah.Application.Delivery.MarkDeliveryReadyRequest`. Response: `Task<ApiResult<bool>>`. Error handling: executes via `SendEmptyAsync pipeline (returns boolean ApiResult; transport exceptions prompt idempotent replay)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:635:24:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 635-635
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:635:24:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:635:24:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [635-635]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:637:5:MethodDeclaration
#### AssignDeliveryAsync — Lines 637-638
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:637:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:637:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.AssignDeliveryAsync(Falah.Application.Delivery.AssignDeliveryRequest, CancellationToken)
- **Lines:** [637-638]
- **Behavior:** Executes HTTP POST to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId`. Request payload: `Falah.Application.Delivery.AssignDeliveryRequest`. Response: `Task<ApiResult<AssignDeliveryResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:638:41:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 638-638
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:638:41:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:638:41:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [638-638]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:640:5:MethodDeclaration
#### ReassignDeliveryAsync — Lines 640-641
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:640:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:640:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReassignDeliveryAsync(Falah.Application.Delivery.ReassignDeliveryRequest, CancellationToken)
- **Lines:** [640-641]
- **Behavior:** Executes HTTP POST to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId`. Request payload: `Falah.Application.Delivery.ReassignDeliveryRequest`. Response: `Task<ApiResult<ReassignDeliveryResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:641:43:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 641-641
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:641:43:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:641:43:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [641-641]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:643:5:MethodDeclaration
#### RecordDeliveryResultAsync — Lines 643-644
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:643:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:643:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RecordDeliveryResultAsync(Falah.Application.Delivery.RecordDeliveryResultRequest, CancellationToken)
- **Lines:** [643-644]
- **Behavior:** Executes HTTP POST to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId`. Request payload: `Falah.Application.Delivery.RecordDeliveryResultRequest`. Response: `Task<ApiResult<RecordDeliveryResultResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:644:47:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 644-644
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:644:47:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:644:47:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [644-644]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/delivery/{Uri.EscapeDataString(request.DeliveryId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:646:5:MethodDeclaration
#### SettleDriverCustodyAsync — Lines 646-647
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:646:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:646:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SettleDriverCustodyAsync(Falah.Application.Delivery.SettleDriverCustodyRequest, CancellationToken)
- **Lines:** [646-647]
- **Behavior:** Executes HTTP POST to `$"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId`. Request payload: `Falah.Application.Delivery.SettleDriverCustodyRequest`. Response: `Task<ApiResult<SettleDriverCustodyResult>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:647:46:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 647-647
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:647:46:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:647:46:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [647-647]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:649:5:MethodDeclaration
#### GetDriverAccountAsync — Lines 649-650
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:649:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:649:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDriverAccountAsync(string, CancellationToken)
- **Lines:** [649-650]
- **Behavior:** Executes HTTP GET to `$"/api/delivery/drivers/{Uri.EscapeDataString(driverId`. Request payload: `None`. Response: `Task<ApiResult<DriverAccountDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:650:37:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 650-650
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:650:37:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:650:37:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [650-650]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/delivery/drivers/{Uri.EscapeDataString(driverId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:656:5:MethodDeclaration
#### GetDriverStatisticsAsync — Lines 656-660
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:656:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:656:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDriverStatisticsAsync(string, DateTimeOffset, DateTimeOffset, CancellationToken)
- **Lines:** [656-660]
- **Behavior:** Executes HTTP GET to `$"/api/delivery/drivers/{Uri.EscapeDataString(driverId`. Request payload: `DateTimeOffset`. Response: `Task<ApiResult<DriverStatisticsDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:658:40:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 658-660
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:658:40:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:658:40:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [658-660]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/delivery/drivers/{Uri.EscapeDataString(driverId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:662:5:MethodDeclaration
#### GetDeliveryTimelineAsync — Lines 662-663
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:662:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:662:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetDeliveryTimelineAsync(string, CancellationToken)
- **Lines:** [662-663]
- **Behavior:** Executes HTTP GET to `$"/api/delivery/{Uri.EscapeDataString(deliveryId`. Request payload: `None`. Response: `Task<ApiResult<IReadOnlyList<DeliveryEventDto>>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:663:52:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 663-663
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:663:52:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:663:52:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [663-663]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/delivery/{Uri.EscapeDataString(deliveryId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:665:5:MethodDeclaration
#### LookupOrderByNumberAsync — Lines 665-668
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:665:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:665:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.LookupOrderByNumberAsync(string, CancellationToken)
- **Lines:** [665-668]
- **Behavior:** Executes HTTP GET to `$"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber`. Request payload: `None`. Response: `Task<ApiResult<OrderDetailsDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<OrderDetailsDto>> LookupOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await ReadAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:667:49:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 667-667
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:667:49:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:667:49:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [667-667]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:670:5:MethodDeclaration
#### GetOrderAsync — Lines 670-673
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:670:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:670:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetOrderAsync(string, CancellationToken)
- **Lines:** [670-673]
- **Behavior:** Executes HTTP GET to `$"/api/orders/{Uri.EscapeDataString(orderId`. Request payload: `None`. Response: `Task<ApiResult<OrderDetailsDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<OrderDetailsDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        return await ReadAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct), ct);
    }
```

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:672:49:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 672-672
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:672:49:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:672:49:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [672-672]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/orders/{Uri.EscapeDataString(orderId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:675:5:MethodDeclaration
#### IssueManagerApprovalAsync — Lines 675-678
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:675:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:675:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.IssueManagerApprovalAsync(Falah.Application.Approvals.IssueManagerApprovalRequest, CancellationToken)
- **Lines:** [675-678]
- **Behavior:** Executes HTTP POST to `/api/approvals/issue`. Request payload: `Falah.Application.Approvals.IssueManagerApprovalRequest`. Response: `Task<ApiResult<IssueManagerApprovalResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:677:60:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 677-677
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:677:60:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:677:60:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [677-677]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/approvals/issue"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:680:5:MethodDeclaration
#### AddExpenseAsync — Lines 680-683
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:680:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:680:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.AddExpenseAsync(Falah.Application.Expenses.AddExpenseRequest, CancellationToken)
- **Lines:** [680-683]
- **Behavior:** Executes HTTP POST to `/api/expenses`. Request payload: `Falah.Application.Expenses.AddExpenseRequest`. Response: `Task<ApiResult<AddExpenseResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:682:50:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 682-682
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:682:50:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:682:50:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [682-682]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/expenses"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:687:5:MethodDeclaration
#### SafeDropAsync — Lines 687-690
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:687:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:687:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SafeDropAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken)
- **Lines:** [687-690]
- **Behavior:** Executes HTTP POST to `/api/treasury/safe-drop`. Request payload: `Falah.Application.Treasury.SafeDropRequest`. Response: `Task<ApiResult<SafeDropResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:689:48:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 689-689
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:689:48:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:689:48:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [689-689]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/treasury/safe-drop"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:692:5:MethodDeclaration
#### CashierCashMovementAsync — Lines 692-695
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:692:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:692:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CashierCashMovementAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken)
- **Lines:** [692-695]
- **Behavior:** Executes HTTP POST to `/api/cash-drawer/cash-movement`. Request payload: `Falah.Application.CashDrawer.CashierCashMovementRequest`. Response: `Task<ApiResult<CashierCashMovementResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:694:59:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 694-694
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:694:59:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:694:59:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [694-694]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/cash-drawer/cash-movement"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:697:5:MethodDeclaration
#### OpenCashDrawerAsync — Lines 697-700
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:697:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:697:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.OpenCashDrawerAsync(Falah.Application.CashDrawer.OpenCashDrawerRequest, CancellationToken)
- **Lines:** [697-700]
- **Behavior:** Executes HTTP POST to `/api/cash-drawer/open`. Request payload: `Falah.Application.CashDrawer.OpenCashDrawerRequest`. Response: `Task<ApiResult<CashDrawerOpenDto>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:699:51:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 699-699
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:699:51:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:699:51:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [699-699]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/cash-drawer/open"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:706:5:MethodDeclaration
#### GetTreasuryBalanceAsync — Lines 706-709
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:706:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:706:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetTreasuryBalanceAsync(string, CancellationToken)
- **Lines:** [706-709]
- **Behavior:** Executes HTTP GET to `$"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId`. Request payload: `None`. Response: `Task<ApiResult<TreasuryBalanceDto>>`. Error handling: executes via `ReadAsync<T> query pipeline (transport exceptions caught; returns connection failure without re-posting data)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:708:52:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 708-708
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:708:52:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:708:52:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [708-708]
- **Behavior:** HTTP invocation delegate: dispatches HTTP GET request to `$"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:711:5:MethodDeclaration
#### CreatePrintJobsAsync — Lines 711-714
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:711:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:711:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreatePrintJobsAsync(string, Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken)
- **Lines:** [711-714]
- **Behavior:** Executes HTTP POST to `$"/api/orders/{Uri.EscapeDataString(orderId`. Request payload: `Falah.Application.Printing.CreatePrintJobsRequest`. Response: `Task<ApiResult<CreatePrintJobsResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:713:55:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 713-713
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:713:55:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:713:55:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [713-713]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `$"/api/orders/{Uri.EscapeDataString(orderId` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:718:5:MethodDeclaration
#### ReprintDocumentAsync — Lines 718-721
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:718:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:718:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.ReprintDocumentAsync(Falah.Application.Printing.ReprintDocumentRequest, CancellationToken)
- **Lines:** [718-721]
- **Behavior:** Executes HTTP POST to `/api/print-jobs/reprint-document`. Request payload: `Falah.Application.Printing.ReprintDocumentRequest`. Response: `Task<ApiResult<CreatePrintJobsResult>>`. Error handling: executes via `SendAsync<T> command pipeline (transport exceptions caught; advises retrying with identical payload for idempotency)`.
- **Validations:** Parameter binding and error boundary handled by pipeline wrapper. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.cs:720:55:ParenthesizedLambdaExpression
#### Lambda Expression (HTTP Dispatch) — Lines 720-720
- **Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:720:55:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CashierWinUI/Services/ApiClient.cs:720:55:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [720-720]
- **Behavior:** HTTP invocation delegate: dispatches HTTP POST request to `"/api/print-jobs/reprint-document"` within the surrounding ApiClient error boundary pipeline.
- **Validations:** Validation checks handled by outer caller. Throws: None detected in lambda body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
