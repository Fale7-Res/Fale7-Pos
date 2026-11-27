using System.Net.Http;
using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Orders;

namespace Falah.Client.CallCenterWinUI.Services;

/// <summary>Delivery dispatch and order history, ported from the cashier's ApiClient with the screens that use
/// them (owner request 2026-11-24). Same endpoints, same request ids; the server decides every permission.</summary>
public sealed partial class ApiClient
{
    /// <summary>A read: nothing on the server can have changed, so a failure says to try again, not to verify.</summary>
    private static async Task<ApiResult<T>> ReadAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or System.IO.IOException)
        {
            Falah.Client.Diagnostics.ClientLog.AnswerFailed(ex);
            return new(false, default, "تعذر تحميل البيانات من السيرفر. تأكد من الاتصال ثم أعد المحاولة.", true);
        }
    }

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
            Falah.Client.Diagnostics.ClientLog.AnswerFailed(ex);
            return new(false, false, "تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية.", true);
        }
    }

    /// <summary>Without orders_view_all the server returns only the open shift's orders.</summary>
    public Task<ApiResult<IReadOnlyList<OrderDetailsDto>>> GetOrdersAsync(string actorUserId, DateOnly? from = null, DateOnly? to = null,
        int? limit = null, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<OrderDetailsDto>>(() => _http.GetAsync(
            $"/api/orders?actorUserId={Uri.EscapeDataString(actorUserId)}"
            + (from is { } f ? $"&from={f:yyyy-MM-dd}" : "") + (to is { } t ? $"&to={t:yyyy-MM-dd}" : "")
            + (limit is { } l ? $"&limit={l}" : ""), ct), ct);

    public Task<ApiResult<DriverDailyStatementDto>> GetDriverDailyStatementAsync(
        string driverId, DateOnly businessDate, string? paymentFilter, CancellationToken ct = default) =>
        ReadAsync<DriverDailyStatementDto>(() => _http.GetAsync(
            $"/api/delivery/drivers/{Uri.EscapeDataString(driverId)}/daily-statement?businessDate={businessDate:yyyy-MM-dd}"
            + (string.IsNullOrWhiteSpace(paymentFilter) || paymentFilter == "all" ? "" : $"&paymentFilter={Uri.EscapeDataString(paymentFilter)}"), ct), ct);

    public sealed record PrintQueuedResult(string PrintJobId, bool WasReplayed);

    public Task<ApiResult<PrintQueuedResult>> PrintDriverDailyStatementAsync(
        string driverId, PrintDriverDailyStatementRequest request, CancellationToken ct = default) =>
        SendAsync<PrintQueuedResult>(() => _http.PostAsJsonAsync($"/api/delivery/drivers/{Uri.EscapeDataString(driverId)}/daily-statement/print", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<DriverDto>>> GetActiveDriversAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<DriverDto>>(() => _http.GetAsync("/api/delivery/drivers", ct), ct);

    public Task<ApiResult<IReadOnlyList<DriverDto>>> GetAllDriversAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<DriverDto>>(() => _http.GetAsync("/api/delivery/drivers/all", ct), ct);

    /// <summary>Workflow redesign (owner, 2026-11-24): the live delivery and pickup queue, stages computed by the server.</summary>
    public Task<ApiResult<List<QueueOrderDto>>> GetOrderQueueAsync(CancellationToken ct = default) =>
        SendAsync<List<QueueOrderDto>>(() => _http.GetAsync("/api/orders/queue", ct), ct);
    /// <summary>Owner, 2026-11-25 («ترحيل»): finished orders leave the queue's screen; nothing is deleted.</summary>
    public Task<ApiResult<ClearOrderQueueResult>> ClearOrderQueueAsync(ClearOrderQueueRequest request, CancellationToken ct = default) =>
        SendAsync<ClearOrderQueueResult>(() => _http.PostAsJsonAsync("/api/orders/queue/clear", request, ct), ct);

    public Task<ApiResult<bool>> SendKitchenInvoiceAsync(SendKitchenInvoiceRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync($"/api/orders/{Uri.EscapeDataString(request.OrderId)}/kitchen-invoice", request, ct), ct);

    /// <summary>An unpaid pickup cancelled from the queue (a manager's PIN once its kitchen paper printed).</summary>
    public Task<ApiResult<bool>> CancelUncollectedPickupAsync(CancelUncollectedPickupRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync("/api/orders/cancel-uncollected-pickup", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<DeliveryDto>>> GetDispatchBoardAsync(CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<DeliveryDto>>(() => _http.GetAsync("/api/delivery/board", ct), ct);

    public Task<ApiResult<CancelUndispatchedDeliveryResult>> CancelUndispatchedDeliveryAsync(CancelUndispatchedDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync<CancelUndispatchedDeliveryResult>(() => _http.PostAsJsonAsync("/api/orders/cancel-undispatched-delivery", request, ct), ct);

    public Task<ApiResult<bool>> MarkDeliveryReadyAsync(MarkDeliveryReadyRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/ready", request, ct), ct);

    public Task<ApiResult<AssignDeliveryResult>> AssignDeliveryAsync(AssignDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync<AssignDeliveryResult>(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/assign", request, ct), ct);

    /// <summary>Owner's trial 2026-11-25: an order out with one driver goes to another (same custody, new driver).</summary>
    public Task<ApiResult<ReassignDeliveryResult>> ReassignDeliveryAsync(ReassignDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync<ReassignDeliveryResult>(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/reassign", request, ct), ct);

    public Task<ApiResult<RecordDeliveryResultResult>> RecordDeliveryResultAsync(RecordDeliveryResultRequest request, CancellationToken ct = default) =>
        SendAsync<RecordDeliveryResultResult>(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/result", request, ct), ct);

    public Task<ApiResult<SettleDriverCustodyResult>> SettleDriverCustodyAsync(SettleDriverCustodyRequest request, CancellationToken ct = default) =>
        SendAsync<SettleDriverCustodyResult>(() => _http.PostAsJsonAsync($"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId)}/settle", request, ct), ct);

    public Task<ApiResult<DriverAccountDto>> GetDriverAccountAsync(string driverId, CancellationToken ct = default) =>
        ReadAsync<DriverAccountDto>(() => _http.GetAsync($"/api/delivery/drivers/{Uri.EscapeDataString(driverId)}/account", ct), ct);

    /// <summary>The instants are escaped: a bare "+" of the "+00:00" offset decodes as a space in a query string.</summary>
    public Task<ApiResult<DriverStatisticsDto>> GetDriverStatisticsAsync(
        string driverId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) =>
        ReadAsync<DriverStatisticsDto>(() => _http.GetAsync(
            $"/api/delivery/drivers/{Uri.EscapeDataString(driverId)}/statistics" +
            $"?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}", ct), ct);
}
