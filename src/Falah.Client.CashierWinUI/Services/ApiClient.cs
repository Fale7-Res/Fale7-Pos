using System.Net.Http;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Expenses;
using Falah.Application.Menu;
using Falah.Application.Orders;
using Falah.Application.Shifts;
using Falah.Application.Users;

namespace Falah.Client.CashierWinUI.Services;

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

    public ApiClient(string baseAddress = "http://localhost:5151")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<bool> PingHealthAsync(CancellationToken ct = default)
    {
        try
        {
            var response = await _http.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public sealed record ApiResult<T>(bool Success, T? Value, string? Error);

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return new ApiResult<T>(true, value, null);
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
        return new ApiResult<T>(false, default, message);
    }

    private sealed record ErrorBody(string? error);

    public async Task<ApiResult<UserSummaryDto>> LoginByPinAsync(string pin, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct);
        return await ReadResultAsync<UserSummaryDto>(response, ct);
    }

    public async Task<ApiResult<MenuResponse>> GetMenuAsync(CancellationToken ct = default)
    {
        var response = await _http.GetAsync("/api/menu", ct);
        return await ReadResultAsync<MenuResponse>(response, ct);
    }

    public async Task<ApiResult<OpenShiftResult>> OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/shifts/open", request, ct);
        return await ReadResultAsync<OpenShiftResult>(response, ct);
    }

    public async Task<ApiResult<PrepareShiftCloseResult>> PrepareShiftCloseAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct);
        return await ReadResultAsync<PrepareShiftCloseResult>(response, ct);
    }

    public async Task<ApiResult<CloseShiftResult>> CloseShiftAsync(CloseShiftRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/shifts/close", request, ct);
        return await ReadResultAsync<CloseShiftResult>(response, ct);
    }

    public async Task<ApiResult<CheckoutOrderResult>> CheckoutAsync(CheckoutOrderRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/orders/checkout", request, ct);
        return await ReadResultAsync<CheckoutOrderResult>(response, ct);
    }

    public async Task<ApiResult<RefundOrderResult>> RefundAsync(RefundOrderRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/orders/refund", request, ct);
        return await ReadResultAsync<RefundOrderResult>(response, ct);
    }

    public async Task<ApiResult<ConfirmElectronicPaymentResult>> ConfirmElectronicPaymentAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct);
        return await ReadResultAsync<ConfirmElectronicPaymentResult>(response, ct);
    }

    public async Task<ApiResult<OrderDetailsDto>> LookupOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct);
        return await ReadResultAsync<OrderDetailsDto>(response, ct);
    }

    public async Task<ApiResult<OrderDetailsDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        var response = await _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct);
        return await ReadResultAsync<OrderDetailsDto>(response, ct);
    }

    public async Task<ApiResult<IssueManagerApprovalResult>> IssueManagerApprovalAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/approvals/issue", request, ct);
        return await ReadResultAsync<IssueManagerApprovalResult>(response, ct);
    }

    public async Task<ApiResult<AddExpenseResult>> AddExpenseAsync(AddExpenseRequest request, CancellationToken ct = default)
    {
        var response = await _http.PostAsJsonAsync("/api/expenses", request, ct);
        return await ReadResultAsync<AddExpenseResult>(response, ct);
    }
}
