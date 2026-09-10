using System.Net.Http;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.CashDrawer;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Menu;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Application.Users;

namespace Falah.Client.CallCenterWinUI.Services;

/// <summary>
/// The ONLY way this WinUI client ever talks to the restaurant's data - every call goes to
/// Falah.Server over HTTP. There is no SQL Server connection string anywhere in this project, and
/// there must never be one: the server remains the sole financial authority (prices, totals,
/// idempotency, OrderNumber, inventory) even when this client happens to run on the same physical
/// machine as the Host.
/// </summary>
public sealed class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    public ApiClient(string baseAddress = "http://localhost:5151")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>Where product photos are fetched from. Images are the one thing this client loads
    /// by URL rather than through a typed call, because an Image control does its own download -
    /// so it needs the address, not the bytes.</summary>
    public Uri? ProductImageUri(string productId, string? version) =>
        string.IsNullOrEmpty(version) || _http.BaseAddress is null
            ? null
            : new Uri(_http.BaseAddress, $"/api/products/{Uri.EscapeDataString(productId)}/image?v={Uri.EscapeDataString(version)}");

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

    public sealed record ApiResult<T>(bool Success, T? Value, string? Error, bool OutcomeUnknown = false, int StatusCode = 0);

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

    public Task<ApiResult<CurrentShiftDto>> GetCurrentShiftAsync(CancellationToken ct = default) =>
        SendAsync<CurrentShiftDto>(() => _http.GetAsync("/api/shifts/current", ct), ct);
    public Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>> GetTaxRulesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>(() => _http.GetAsync("/api/tax-rules", ct), ct);
    public Task<ApiResult<Falah.Application.Settings.SettingsDto>> GetSettingsAsync(CancellationToken ct = default) =>
        SendAsync<Falah.Application.Settings.SettingsDto>(() => _http.GetAsync("/api/settings", ct), ct);
    public Task<ApiResult<Falah.Application.Customers.CustomerDto>> LookupCustomerAsync(string phone, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.GetAsync($"/api/customers/lookup?phone={Uri.EscapeDataString(phone)}", ct), ct);
    public Task<ApiResult<Falah.Application.Customers.CustomerDto>> SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.PostAsJsonAsync("/api/customers/upsert", request, ct), ct);

    private sealed record ErrorBody(string? error);

    /// <summary>The session token this client presents on every later request. Set once at login
    /// and cleared at logout; without it the server refuses everything, which is the point - the
    /// acting user is now something the server establishes rather than something we assert.</summary>
    private string? _sessionToken;

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

    /// <summary>Ends the session server-side. Locking the screen without this would leave a working
    /// token behind, which is exactly the state a shared till should not be left in.</summary>
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (_sessionToken is null) return;
        try { await _http.PostAsync("/api/auth/logout", content: null, ct); }
        catch { /* the local session ends either way; a stranded token expires on its own. */ }
        _sessionToken = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<ApiResult<MenuResponse>> GetMenuAsync(CancellationToken ct = default)
    {
        return await SendAsync<MenuResponse>(() => _http.GetAsync("/api/menu", ct), ct);
    }

    public async Task<ApiResult<OpenShiftResult>> OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        return await SendAsync<OpenShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/open", request, ct), ct);
    }

    public async Task<ApiResult<PrepareShiftCloseResult>> PrepareShiftCloseAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<PrepareShiftCloseResult>(() => _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct), ct);
    }

    public async Task<ApiResult<CloseShiftResult>> CloseShiftAsync(CloseShiftRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CloseShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/close", request, ct), ct);
    }

    public async Task<ApiResult<CheckoutOrderResult>> CheckoutAsync(CheckoutOrderRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CheckoutOrderResult>(() => _http.PostAsJsonAsync("/api/orders/checkout", request, ct), ct);
    }

    public async Task<ApiResult<RefundOrderResult>> RefundAsync(RefundOrderRequest request, CancellationToken ct = default)
    {
        return await SendAsync<RefundOrderResult>(() => _http.PostAsJsonAsync("/api/orders/refund", request, ct), ct);
    }

    public async Task<ApiResult<ConfirmElectronicPaymentResult>> ConfirmElectronicPaymentAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<ConfirmElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct), ct);
    }

    public async Task<ApiResult<CancelPendingElectronicPaymentResult>> CancelPendingElectronicPaymentAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CancelPendingElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment", request, ct), ct);
    }

    public async Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>> GetActiveDeliveryServicesAsync(CancellationToken ct = default)
    {
        return await SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services", ct), ct);
    }

    public async Task<ApiResult<OrderDetailsDto>> LookupOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct), ct);
    }

    public async Task<ApiResult<OrderDetailsDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct), ct);
    }

    public async Task<ApiResult<IssueManagerApprovalResult>> IssueManagerApprovalAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)
    {
        return await SendAsync<IssueManagerApprovalResult>(() => _http.PostAsJsonAsync("/api/approvals/issue", request, ct), ct);
    }

    public async Task<ApiResult<AddExpenseResult>> AddExpenseAsync(AddExpenseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<AddExpenseResult>(() => _http.PostAsJsonAsync("/api/expenses", request, ct), ct);
    }

    // ---------------- Cashier Operations (Safe Drop / Cash In-Out / Cash Drawer / Cash Count / Reprint) ----------------

    public async Task<ApiResult<SafeDropResult>> SafeDropAsync(SafeDropRequest request, CancellationToken ct = default)
    {
        return await SendAsync<SafeDropResult>(() => _http.PostAsJsonAsync("/api/treasury/safe-drop", request, ct), ct);
    }

    public async Task<ApiResult<CashierCashMovementResult>> CashierCashMovementAsync(CashierCashMovementRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CashierCashMovementResult>(() => _http.PostAsJsonAsync("/api/cash-drawer/cash-movement", request, ct), ct);
    }

    public async Task<ApiResult<CashDrawerOpenDto>> OpenCashDrawerAsync(OpenCashDrawerRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CashDrawerOpenDto>(() => _http.PostAsJsonAsync("/api/cash-drawer/open", request, ct), ct);
    }

    public sealed record TreasuryBalanceDto(string AccountId, long BalancePiastres);

    /// <summary>Read-only, no side effects - "Cash Count" from the Cashier Operations menu is
    /// literally this call against "tre-drawer", not a bespoke calculation kept in sync by hand.</summary>
    public async Task<ApiResult<TreasuryBalanceDto>> GetTreasuryBalanceAsync(string accountId, CancellationToken ct = default)
    {
        return await SendAsync<TreasuryBalanceDto>(() => _http.GetAsync($"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId)}", ct), ct);
    }

    public async Task<ApiResult<CreatePrintJobsResult>> CreatePrintJobsAsync(string orderId, CreatePrintJobsRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CreatePrintJobsResult>(() => _http.PostAsJsonAsync($"/api/orders/{Uri.EscapeDataString(orderId)}/print-jobs", request, ct), ct);
    }
}
