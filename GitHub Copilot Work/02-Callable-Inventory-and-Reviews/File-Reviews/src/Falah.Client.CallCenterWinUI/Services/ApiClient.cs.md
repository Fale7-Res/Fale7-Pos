# File Review: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs.

## Callables

### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration
#### ApiClient Constructor (HttpClient Injection) — Lines 26-26
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:26:5:ConstructorDeclaration`
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.ApiClient(System.Net.Http.HttpClient)
- **Lines:** [26-26]
- **Behavior:** Initializes the Call Center ApiClient using an externally provided HttpClient instance, stored in private field _http. Presumes base address, default headers, and handlers are configured by caller or dependency injection.
- **Validations:** None in body (null injection allowed). Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public ApiClient(HttpClient http) => _http = http;
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration
#### ApiClient Constructor (Base Address String) — Lines 28-31
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:28:5:ConstructorDeclaration`
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.ApiClient(string)
- **Lines:** [28-31]
- **Behavior:** Instantiates a new HttpClient targeting baseAddress (defaulting to "http://localhost:5151") with a 10-second request timeout. Enables Call Center client standalone execution against local or networked Fale7 server instances.
- **Validations:** Uri constructor parses baseAddress string. Throws: UriFormatException if baseAddress is malformed.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public ApiClient(string baseAddress = "http://localhost:5151")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(10) };
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:36:5:MethodDeclaration
#### ProductImageUri Method — Lines 36-39
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:36:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:36:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.ProductImageUri(string, string?)
- **Lines:** [36-39]
- **Behavior:** Constructs a direct HTTP GET URI for product photos loaded by XAML Image controls: new Uri(_http.BaseAddress, $"/api/products/{Uri.EscapeDataString(productId)}/image?v={Uri.EscapeDataString(version)}"). Returns null if version is null/empty or _http.BaseAddress is null. Uses version as a cache-busting query parameter.
- **Validations:** Checks string.IsNullOrEmpty(version) and _http.BaseAddress is null. Escapes productId and version. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Uri? ProductImageUri(string productId, string? version) =>
        string.IsNullOrEmpty(version) || _http.BaseAddress is null
            ? null
            : new Uri(_http.BaseAddress, $"/api/products/{Uri.EscapeDataString(productId)}/image?v={Uri.EscapeDataString(version)}");
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:41:5:MethodDeclaration
#### PingHealthAsync Method — Lines 41-52
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:41:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:41:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.PingHealthAsync(CancellationToken)
- **Lines:** [41-52]
- **Behavior:** Pings the server health endpoint via HTTP GET /health using GetAsync. Returns true if response.IsSuccessStatusCode; catches all exceptions (transport failure, timeout, server offline) and safely returns false. Used by station connection status monitor.
- **Validations:** Exception boundary catches all exceptions. Throws: None.
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
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:56:5:MethodDeclaration
#### ReadResultAsync<T> Method — Lines 56-82
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:56:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:56:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken)
- **Lines:** [56-82]
- **Behavior:** Core response parsing and error extraction pipeline. For successful HTTP status codes, deserializes response JSON via ReadFromJsonAsync<T>; if deserialized object is null, returns failure ApiResult with OutcomeUnknown=true. On non-success codes, attempts deserializing ErrorBody to extract error message; falls back to localized Arabic error messages for 401 Unauthorized ("رمز الدخول غير صحيح."), 404 NotFound ("العنصر المطلوب غير موجود."), or generic server errors. Flags OutcomeUnknown=true for status codes >= 500.
- **Validations:** Checks response.IsSuccessStatusCode. Safe JSON parsing of ErrorBody. Throws: None.
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
        catch
        {
            // Body wasn't the expected {error} shape - fall through to the generic message below.
        }
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
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:84:5:MethodDeclaration
#### SendAsync<T> Method — Lines 84-95
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:84:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:84:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.SendAsync<T>(Func<Task<System.Net.Http.HttpResponseMessage>>, CancellationToken)
- **Lines:** [84-95]
- **Behavior:** Generic HTTP execution pipeline. Invokes asynchronous send delegate, manages HttpResponseMessage lifetime via using block, and delegates parsing to ReadResultAsync<T>. Catches HttpRequestException, OperationCanceledException, JsonException, and IOException, translating network/transport failures to failure ApiResult with OutcomeUnknown=true ("تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية.").
- **Validations:** Exception filter catches transport and deserialization failures. Throws: None.
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
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:97:5:MethodDeclaration
#### GetCurrentShiftAsync Method — Lines 97-98
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:97:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:97:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetCurrentShiftAsync(CancellationToken)
- **Lines:** [97-98]
- **Behavior:** Queries currently active shift details via HTTP GET /api/shifts/current. Dispatches via SendAsync<CurrentShiftDto>. Crucial for Call Center order intake: discovers the active system-wide shift to which phone/delivery orders will be attached.
- **Validations:** None in client wrapper. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<CurrentShiftDto>> GetCurrentShiftAsync(CancellationToken ct = default) =>
        SendAsync<CurrentShiftDto>(() => _http.GetAsync("/api/shifts/current", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:98:36:ParenthesizedLambdaExpression
#### Lambda Expression (GetCurrentShift HTTP Dispatch) — Lines 98-98
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:98:36:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:98:36:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [98-98]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/shifts/current using _http.GetAsync with cancellation token ct.
- **Validations:** None. Exceptions handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
SendAsync<CurrentShiftDto>(() => _http.GetAsync("/api/shifts/current", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:99:5:MethodDeclaration
#### GetTaxRulesAsync Method — Lines 99-100
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:99:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:99:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetTaxRulesAsync(CancellationToken)
- **Lines:** [99-100]
- **Behavior:** Retrieves active tax calculation rules via HTTP GET /api/tax-rules. Dispatches via SendAsync<IReadOnlyList<TaxRuleDto>>. Used by CallCenterViewModel to configure TaxEngine for live grand total display.
- **Validations:** None in client wrapper. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>> GetTaxRulesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>(() => _http.GetAsync("/api/tax-rules", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:100:72:ParenthesizedLambdaExpression
#### Lambda Expression (GetTaxRules HTTP Dispatch) — Lines 100-100
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:100:72:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:100:72:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/tax-rules using _http.GetAsync with cancellation token ct.
- **Validations:** None. Exceptions handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
SendAsync<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>(() => _http.GetAsync("/api/tax-rules", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:101:5:MethodDeclaration
#### GetSettingsAsync Method — Lines 101-102
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:101:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:101:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetSettingsAsync(CancellationToken)
- **Lines:** [101-102]
- **Behavior:** Fetches global restaurant settings (restaurant name, print configurations) via HTTP GET /api/settings. Dispatches through SendAsync<SettingsDto>. Used by HeaderViewModel to display restaurant branding on the desk.
- **Validations:** None in client wrapper. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Settings.SettingsDto>> GetSettingsAsync(CancellationToken ct = default) =>
        SendAsync<Falah.Application.Settings.SettingsDto>(() => _http.GetAsync("/api/settings", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:102:59:ParenthesizedLambdaExpression
#### Lambda Expression (GetSettings HTTP Dispatch) — Lines 102-102
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:102:59:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:102:59:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [102-102]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/settings using _http.GetAsync with cancellation token ct.
- **Validations:** None. Exceptions handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
SendAsync<Falah.Application.Settings.SettingsDto>(() => _http.GetAsync("/api/settings", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:103:5:MethodDeclaration
#### LookupCustomerAsync Method — Lines 103-104
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:103:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:103:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.LookupCustomerAsync(string, CancellationToken)
- **Lines:** [103-104]
- **Behavior:** Queries customer profile and saved delivery addresses by telephone number via HTTP GET /api/customers/lookup?phone={escapedPhone}. Escapes phone parameter via Uri.EscapeDataString. Dispatches through SendAsync<CustomerDto>. Triggered when operator types >= 11 digits.
- **Validations:** URL escaping of phone query parameter. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public Task<ApiResult<Falah.Application.Customers.CustomerDto>> LookupCustomerAsync(string phone, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.GetAsync($"/api/customers/lookup?phone={Uri.EscapeDataString(phone)}", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:104:60:ParenthesizedLambdaExpression
#### Lambda Expression (LookupCustomer HTTP Dispatch) — Lines 104-104
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:104:60:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:104:60:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [104-104]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/customers/lookup with escaped phone query parameter and cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.GetAsync($"/api/customers/lookup?phone={Uri.EscapeDataString(phone)}", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:105:5:MethodDeclaration
#### SaveCustomerAsync Method — Lines 105-106
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:105:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:105:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest, CancellationToken)
- **Lines:** [105-106]
- **Behavior:** Creates or updates customer and address records via HTTP POST /api/customers/upsert. Serializes UpsertCustomerRequest to JSON. Dispatches through SendAsync<CustomerDto>. Fired in a background fire-and-forget task after successful checkout.
- **Validations:** Server-side validation for phone and customer name. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

```csharp
public Task<ApiResult<Falah.Application.Customers.CustomerDto>> SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.PostAsJsonAsync("/api/customers/upsert", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:106:60:ParenthesizedLambdaExpression
#### Lambda Expression (SaveCustomer HTTP Dispatch) — Lines 106-106
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:106:60:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:106:60:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [106-106]
- **Behavior:** Delegate passed to SendAsync. Posts UpsertCustomerRequest as JSON to /api/customers/upsert with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

```csharp
SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.PostAsJsonAsync("/api/customers/upsert", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:115:5:MethodDeclaration
#### LoginByPinAsync Method — Lines 115-129
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:115:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:115:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken)
- **Lines:** [115-129]
- **Behavior:** Authenticates an operator by PIN via HTTP POST /api/auth/login-by-pin. Dispatches LoginByPinRequest through SendAsync<LoginResultDto>. Upon success, stores Token in _sessionToken and mutates _http.DefaultRequestHeaders.Authorization with Bearer token. Returns ApiResult<UserSummaryDto> containing user ID, name, role, and permissions. Finding: Mutating DefaultRequestHeaders on a shared HttpClient instance is not thread-safe if requests are concurrently in-flight.
- **Validations:** Checks result.Success and result.Value is not null. Throws: None.
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
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:118:13:ParenthesizedLambdaExpression
#### Lambda Expression (LoginByPin HTTP Dispatch) — Lines 118-118
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:118:13:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:118:13:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [118-118]
- **Behavior:** Delegate passed to SendAsync. Posts LoginByPinRequest to /api/auth/login-by-pin with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

```csharp
() => _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:133:5:MethodDeclaration
#### LogoutAsync Method — Lines 133-140
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:133:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:133:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.LogoutAsync(CancellationToken)
- **Lines:** [133-140]
- **Behavior:** Terminates the server session via HTTP POST /api/auth/logout and clears client-side credentials. If _sessionToken is not null, dispatches logout request, sets _sessionToken = null, and clears _http.DefaultRequestHeaders.Authorization. Swallows exceptions so local session ends even if server is unreachable. Finding: Header mutation is not thread-safe; swallows all exceptions silently without logging.
- **Validations:** Checks _sessionToken is not null. Try-catch swallows transport errors. Throws: None.
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
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:142:5:MethodDeclaration
#### GetMenuAsync Method — Lines 142-145
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:142:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:142:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetMenuAsync(CancellationToken)
- **Lines:** [142-145]
- **Behavior:** Loads the complete catalog snapshot for the order intake screen via HTTP GET /api/menu. Dispatches through SendAsync<MenuResponse>. Deserializes categories, products, variants, applicable offers, and ranked best-seller product IDs.
- **Validations:** None in client wrapper. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<MenuResponse>> GetMenuAsync(CancellationToken ct = default)
    {
        return await SendAsync<MenuResponse>(() => _http.GetAsync("/api/menu", ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:144:46:ParenthesizedLambdaExpression
#### Lambda Expression (GetMenu HTTP Dispatch) — Lines 144-144
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:144:46:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:144:46:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [144-144]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/menu using _http.GetAsync with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
return await SendAsync<MenuResponse>(() => _http.GetAsync("/api/menu", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:147:5:MethodDeclaration
#### OpenShiftAsync Method — Lines 147-150
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:147:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:147:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.OpenShiftAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken)
- **Lines:** [147-150]
- **Behavior:** Opens a cashier shift via HTTP POST /api/shifts/open. Serializes OpenShiftRequest (UserId, InitialCashPiastres) to JSON. Dispatches through SendAsync<OpenShiftResult>. Finding: Redundant surface area in CallCenter ApiClient — Call Center station has no physical cash drawer and does not operate or open shifts.
- **Validations:** Server enforces shift opening rules and drawer limits. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<OpenShiftResult>> OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        return await SendAsync<OpenShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/open", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:149:49:ParenthesizedLambdaExpression
#### Lambda Expression (OpenShift HTTP Dispatch) — Lines 149-149
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:149:49:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:149:49:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [149-149]
- **Behavior:** Delegate passed to SendAsync. Posts OpenShiftRequest to /api/shifts/open with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<OpenShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/open", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:152:5:MethodDeclaration
#### PrepareShiftCloseAsync Method — Lines 152-155
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:152:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:152:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.PrepareShiftCloseAsync(Falah.Application.Shifts.PrepareShiftCloseRequest, CancellationToken)
- **Lines:** [152-155]
- **Behavior:** Prepares shift closure via HTTP POST /api/shifts/prepare-close. Serializes PrepareShiftCloseRequest. Dispatches through SendAsync<PrepareShiftCloseResult>. Calculates expected cash, card, and delivery totals.
- **Validations:** Server enforces shift active state. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: false

```csharp
public async Task<ApiResult<PrepareShiftCloseResult>> PrepareShiftCloseAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<PrepareShiftCloseResult>(() => _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:154:57:ParenthesizedLambdaExpression
#### Lambda Expression (PrepareShiftClose HTTP Dispatch) — Lines 154-154
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:154:57:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:154:57:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [154-154]
- **Behavior:** Delegate passed to SendAsync. Posts PrepareShiftCloseRequest to /api/shifts/prepare-close with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: false

```csharp
return await SendAsync<PrepareShiftCloseResult>(() => _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:157:5:MethodDeclaration
#### CloseShiftAsync Method — Lines 157-160
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:157:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:157:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.CloseShiftAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken)
- **Lines:** [157-160]
- **Behavior:** Finalizes shift closure via HTTP POST /api/shifts/close. Serializes CloseShiftRequest (ShiftId, ActualCashPiastres, Notes). Dispatches through SendAsync<CloseShiftResult>. Reconciles cash drawer and records variance.
- **Validations:** Server validates shift state and authorization. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<CloseShiftResult>> CloseShiftAsync(CloseShiftRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CloseShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/close", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:159:50:ParenthesizedLambdaExpression
#### Lambda Expression (CloseShift HTTP Dispatch) — Lines 159-159
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:159:50:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:159:50:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [159-159]
- **Behavior:** Delegate passed to SendAsync. Posts CloseShiftRequest to /api/shifts/close with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<CloseShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/close", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:162:5:MethodDeclaration
#### CheckoutAsync Method — Lines 162-165
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:162:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:162:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.CheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken)
- **Lines:** [162-165]
- **Behavior:** Core order checkout submission via HTTP POST /api/orders/checkout. Serializes CheckoutOrderRequest (including CheckoutRequestId idempotency key, OrderType="delivery", CashierId, ShiftId, Items, DeliveryFeePiastres, structured delivery address, and CustomerPrintRoute="CallCenter"). Dispatches through SendAsync<CheckoutOrderResult>. Critical financial boundary.
- **Validations:** Server verifies shift is open, prices, totals, idempotency key, and inventory availability. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

```csharp
public async Task<ApiResult<CheckoutOrderResult>> CheckoutAsync(CheckoutOrderRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CheckoutOrderResult>(() => _http.PostAsJsonAsync("/api/orders/checkout", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:164:53:ParenthesizedLambdaExpression
#### Lambda Expression (Checkout HTTP Dispatch) — Lines 164-164
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:164:53:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:164:53:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [164-164]
- **Behavior:** Delegate passed to SendAsync. Posts CheckoutOrderRequest to /api/orders/checkout with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

```csharp
return await SendAsync<CheckoutOrderResult>(() => _http.PostAsJsonAsync("/api/orders/checkout", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:167:5:MethodDeclaration
#### RefundAsync Method — Lines 167-170
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:167:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:167:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.RefundAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken)
- **Lines:** [167-170]
- **Behavior:** Processes an order refund via HTTP POST /api/orders/refund. Serializes RefundOrderRequest (OrderId, Reason, AmountPiastres, ManagerApprovalId) to JSON. Dispatches through SendAsync<RefundOrderResult>. Reverses ledger entries and updates order state.
- **Validations:** Server requires manager approval and validates refundable amount. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<RefundOrderResult>> RefundAsync(RefundOrderRequest request, CancellationToken ct = default)
    {
        return await SendAsync<RefundOrderResult>(() => _http.PostAsJsonAsync("/api/orders/refund", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:169:51:ParenthesizedLambdaExpression
#### Lambda Expression (Refund HTTP Dispatch) — Lines 169-169
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:169:51:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:169:51:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [169-169]
- **Behavior:** Delegate passed to SendAsync. Posts RefundOrderRequest to /api/orders/refund with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<RefundOrderResult>(() => _http.PostAsJsonAsync("/api/orders/refund", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:172:5:MethodDeclaration
#### ConfirmElectronicPaymentAsync Method — Lines 172-175
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:172:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:172:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.ConfirmElectronicPaymentAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken)
- **Lines:** [172-175]
- **Behavior:** Confirms a pending electronic transfer (InstaPay, Mobile Wallet) via HTTP POST /api/orders/confirm-electronic-payment. Serializes ConfirmElectronicPaymentRequest (OrderId, ReferenceNumber, ApproverUserId) to JSON. Dispatches through SendAsync<ConfirmElectronicPaymentResult>. Moves order from pending payment to settled.
- **Validations:** Server validates payment status and manager authorization. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<ConfirmElectronicPaymentResult>> ConfirmElectronicPaymentAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<ConfirmElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:174:64:ParenthesizedLambdaExpression
#### Lambda Expression (ConfirmElectronicPayment HTTP Dispatch) — Lines 174-174
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:174:64:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:174:64:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [174-174]
- **Behavior:** Delegate passed to SendAsync. Posts ConfirmElectronicPaymentRequest to /api/orders/confirm-electronic-payment with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<ConfirmElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:177:5:MethodDeclaration
#### CancelPendingElectronicPaymentAsync Method — Lines 177-180
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:177:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:177:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.CancelPendingElectronicPaymentAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken)
- **Lines:** [177-180]
- **Behavior:** Cancels an unverified electronic payment via HTTP POST /api/orders/cancel-pending-electronic-payment. Serializes CancelPendingElectronicPaymentRequest to JSON. Dispatches through SendAsync<CancelPendingElectronicPaymentResult>.
- **Validations:** Server verifies order is in pending electronic payment state. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<CancelPendingElectronicPaymentResult>> CancelPendingElectronicPaymentAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CancelPendingElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:179:70:ParenthesizedLambdaExpression
#### Lambda Expression (CancelPendingElectronicPayment HTTP Dispatch) — Lines 179-179
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:179:70:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:179:70:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [179-179]
- **Behavior:** Delegate passed to SendAsync. Posts CancelPendingElectronicPaymentRequest to /api/orders/cancel-pending-electronic-payment with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<CancelPendingElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:182:5:MethodDeclaration
#### GetActiveDeliveryServicesAsync Method — Lines 182-185
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:182:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:182:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetActiveDeliveryServicesAsync(CancellationToken)
- **Lines:** [182-185]
- **Behavior:** Queries currently active delivery services and fee structures via HTTP GET /api/delivery/services. Dispatches via SendAsync<IReadOnlyList<DeliveryServiceDto>>. Populates the delivery service selection dropdown on the order form.
- **Validations:** None in client wrapper. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>> GetActiveDeliveryServicesAsync(CancellationToken ct = default)
    {
        return await SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services", ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:184:67:ParenthesizedLambdaExpression
#### Lambda Expression (GetActiveDeliveryServices HTTP Dispatch) — Lines 184-184
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:184:67:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:184:67:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [184-184]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/delivery/services using _http.GetAsync with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
return await SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:187:5:MethodDeclaration
#### LookupOrderByNumberAsync Method — Lines 187-190
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:187:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:187:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.LookupOrderByNumberAsync(string, CancellationToken)
- **Lines:** [187-190]
- **Behavior:** Looks up an existing order by human-readable daily order number via HTTP GET /api/orders/lookup?orderNumber={escapedOrderNumber}. Escapes orderNumber via Uri.EscapeDataString. Dispatches through SendAsync<OrderDetailsDto>. Used for customer follow-up inquiries.
- **Validations:** URL escaping of orderNumber parameter. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<OrderDetailsDto>> LookupOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:189:49:ParenthesizedLambdaExpression
#### Lambda Expression (LookupOrderByNumber HTTP Dispatch) — Lines 189-189
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:189:49:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:189:49:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [189-189]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP GET to /api/orders/lookup with escaped orderNumber query parameter.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:192:5:MethodDeclaration
#### GetOrderAsync Method — Lines 192-195
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:192:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:192:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetOrderAsync(string, CancellationToken)
- **Lines:** [192-195]
- **Behavior:** Fetches complete order details by GUID OrderId via HTTP GET /api/orders/{escapedOrderId}. Escapes orderId via Uri.EscapeDataString. Dispatches through SendAsync<OrderDetailsDto>.
- **Validations:** URL escaping of orderId parameter. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<OrderDetailsDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:194:49:ParenthesizedLambdaExpression
#### Lambda Expression (GetOrder HTTP Dispatch) — Lines 194-194
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:194:49:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:194:49:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [194-194]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP GET to /api/orders/{escapedOrderId} with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:197:5:MethodDeclaration
#### IssueManagerApprovalAsync Method — Lines 197-200
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:197:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:197:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.IssueManagerApprovalAsync(Falah.Application.Approvals.IssueManagerApprovalRequest, CancellationToken)
- **Lines:** [197-200]
- **Behavior:** Issues manager authorization for sensitive operations (discounts, comps, refunds) via HTTP POST /api/approvals/issue. Serializes IssueManagerApprovalRequest (Action, ManagerPin, Reason) to JSON. Dispatches through SendAsync<IssueManagerApprovalResult>.
- **Validations:** Server verifies manager PIN and authorized permissions. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<IssueManagerApprovalResult>> IssueManagerApprovalAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)
    {
        return await SendAsync<IssueManagerApprovalResult>(() => _http.PostAsJsonAsync("/api/approvals/issue", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:199:60:ParenthesizedLambdaExpression
#### Lambda Expression (IssueManagerApproval HTTP Dispatch) — Lines 199-199
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:199:60:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:199:60:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [199-199]
- **Behavior:** Delegate passed to SendAsync. Posts IssueManagerApprovalRequest to /api/approvals/issue with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<IssueManagerApprovalResult>(() => _http.PostAsJsonAsync("/api/approvals/issue", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:202:5:MethodDeclaration
#### AddExpenseAsync Method — Lines 202-205
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:202:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:202:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.AddExpenseAsync(Falah.Application.Expenses.AddExpenseRequest, CancellationToken)
- **Lines:** [202-205]
- **Behavior:** Registers a petty cash expense paid out of drawer/safe via HTTP POST /api/expenses. Serializes AddExpenseRequest (AmountPiastres, CategoryId, Description, ApproverUserId) to JSON. Dispatches through SendAsync<AddExpenseResult>. Finding: Redundant cash drawer operation in Call Center client.
- **Validations:** Server validates expense amount and manager authorization. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<AddExpenseResult>> AddExpenseAsync(AddExpenseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<AddExpenseResult>(() => _http.PostAsJsonAsync("/api/expenses", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:204:50:ParenthesizedLambdaExpression
#### Lambda Expression (AddExpense HTTP Dispatch) — Lines 204-204
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:204:50:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:204:50:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [204-204]
- **Behavior:** Delegate passed to SendAsync. Posts AddExpenseRequest to /api/expenses with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<AddExpenseResult>(() => _http.PostAsJsonAsync("/api/expenses", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:209:5:MethodDeclaration
#### SafeDropAsync Method — Lines 209-212
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:209:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:209:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.SafeDropAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken)
- **Lines:** [209-212]
- **Behavior:** Performs safe drop cash transfer via HTTP POST /api/treasury/safe-drop. Serializes SafeDropRequest (AmountPiastres, SourceDrawerId, TargetSafeId) to JSON. Dispatches through SendAsync<SafeDropResult>. Reduces drawer cash balance and debits treasury safe ledger. Finding: Redundant drawer operation in Call Center client.
- **Validations:** Server validates drawer cash balance sufficiency and user permission. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<SafeDropResult>> SafeDropAsync(SafeDropRequest request, CancellationToken ct = default)
    {
        return await SendAsync<SafeDropResult>(() => _http.PostAsJsonAsync("/api/treasury/safe-drop", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:211:48:ParenthesizedLambdaExpression
#### Lambda Expression (SafeDrop HTTP Dispatch) — Lines 211-211
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:211:48:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:211:48:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [211-211]
- **Behavior:** Delegate passed to SendAsync. Posts SafeDropRequest to /api/treasury/safe-drop with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<SafeDropResult>(() => _http.PostAsJsonAsync("/api/treasury/safe-drop", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:214:5:MethodDeclaration
#### CashierCashMovementAsync Method — Lines 214-217
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:214:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:214:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.CashierCashMovementAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken)
- **Lines:** [214-217]
- **Behavior:** Logs manual cash movement (cash in/out) via HTTP POST /api/cash-drawer/cash-movement. Serializes CashierCashMovementRequest (AmountPiastres, Type: In/Out, Reason) to JSON. Dispatches through SendAsync<CashierCashMovementResult>. Finding: Redundant drawer operation in Call Center client.
- **Validations:** Server enforces drawer movement rules and records audit ledger entry. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<CashierCashMovementResult>> CashierCashMovementAsync(CashierCashMovementRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CashierCashMovementResult>(() => _http.PostAsJsonAsync("/api/cash-drawer/cash-movement", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:216:59:ParenthesizedLambdaExpression
#### Lambda Expression (CashierCashMovement HTTP Dispatch) — Lines 216-216
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:216:59:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:216:59:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [216-216]
- **Behavior:** Delegate passed to SendAsync. Posts CashierCashMovementRequest to /api/cash-drawer/cash-movement with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<CashierCashMovementResult>(() => _http.PostAsJsonAsync("/api/cash-drawer/cash-movement", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:219:5:MethodDeclaration
#### OpenCashDrawerAsync Method — Lines 219-222
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:219:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:219:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.OpenCashDrawerAsync(Falah.Application.CashDrawer.OpenCashDrawerRequest, CancellationToken)
- **Lines:** [219-222]
- **Behavior:** Sends hardware drawer kick command via HTTP POST /api/cash-drawer/open. Serializes OpenCashDrawerRequest (DrawerId, Reason, ManagerApprovalId) to JSON. Dispatches through SendAsync<CashDrawerOpenDto>. Finding: Redundant drawer kick command in Call Center client without attached cash hardware.
- **Validations:** Server validates authorization and records audit log. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

```csharp
public async Task<ApiResult<CashDrawerOpenDto>> OpenCashDrawerAsync(OpenCashDrawerRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CashDrawerOpenDto>(() => _http.PostAsJsonAsync("/api/cash-drawer/open", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:221:51:ParenthesizedLambdaExpression
#### Lambda Expression (OpenCashDrawer HTTP Dispatch) — Lines 221-221
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:221:51:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:221:51:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [221-221]
- **Behavior:** Delegate passed to SendAsync. Posts OpenCashDrawerRequest to /api/cash-drawer/open with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

```csharp
return await SendAsync<CashDrawerOpenDto>(() => _http.PostAsJsonAsync("/api/cash-drawer/open", request, ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:228:5:MethodDeclaration
#### GetTreasuryBalanceAsync Method — Lines 228-231
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:228:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:228:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.GetTreasuryBalanceAsync(string, CancellationToken)
- **Lines:** [228-231]
- **Behavior:** Reads system ledger balance for a treasury account (e.g. "tre-drawer") via HTTP GET /api/treasury/balance?accountId={escapedAccountId}. Escapes accountId parameter via Uri.EscapeDataString. Dispatches through SendAsync<TreasuryBalanceDto>. Purely read-only query of account ledger balance.
- **Validations:** URL escaping of accountId parameter. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
public async Task<ApiResult<TreasuryBalanceDto>> GetTreasuryBalanceAsync(string accountId, CancellationToken ct = default)
    {
        return await SendAsync<TreasuryBalanceDto>(() => _http.GetAsync($"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId)}", ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:230:52:ParenthesizedLambdaExpression
#### Lambda Expression (GetTreasuryBalance HTTP Dispatch) — Lines 230-230
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:230:52:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:230:52:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [230-230]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP GET to /api/treasury/balance with escaped accountId query parameter.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

```csharp
return await SendAsync<TreasuryBalanceDto>(() => _http.GetAsync($"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId)}", ct), ct);
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:233:5:MethodDeclaration
#### CreatePrintJobsAsync Method — Lines 233-236
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:233:5:MethodDeclaration`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:233:5:MethodDeclaration`
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CallCenterWinUI.Services.ApiClient.CreatePrintJobsAsync(string, Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken)
- **Lines:** [233-236]
- **Behavior:** Queues reprint ticket print jobs via HTTP POST /api/orders/{escapedOrderId}/print-jobs. Escapes orderId via Uri.EscapeDataString. Serializes CreatePrintJobsRequest (PrintJobId, OrderId, Route="CallCenter", IsReprint=true, ActorUserId) to JSON. Dispatches through SendAsync<CreatePrintJobsResult>. Note: Server creates initial checkout print jobs automatically; this client method is invoked only for manual reprints from the order lookup panel.
- **Validations:** URL escaping of orderId in endpoint route. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

```csharp
public async Task<ApiResult<CreatePrintJobsResult>> CreatePrintJobsAsync(string orderId, CreatePrintJobsRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CreatePrintJobsResult>(() => _http.PostAsJsonAsync($"/api/orders/{Uri.EscapeDataString(orderId)}/print-jobs", request, ct), ct);
    }
```

---
### Callable: src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:235:55:ParenthesizedLambdaExpression
#### Lambda Expression (CreatePrintJobs HTTP Dispatch) — Lines 235-235
- **Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:235:55:ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Client.CallCenterWinUI/Services/ApiClient.cs:235:55:ParenthesizedLambdaExpression`
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [235-235]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP POST with serialized print request to /api/orders/{escapedOrderId}/print-jobs with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

```csharp
return await SendAsync<CreatePrintJobsResult>(() => _http.PostAsJsonAsync($"/api/orders/{Uri.EscapeDataString(orderId)}/print-jobs", request, ct), ct);
```

---
