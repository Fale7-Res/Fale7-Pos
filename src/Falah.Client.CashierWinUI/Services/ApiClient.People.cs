using System.Net.Http.Json;
using Falah.Application.Backup;
using Falah.Application.BusinessDay;
using Falah.Application.Delivery;
using Falah.Application.Employees;
using Falah.Application.Reports;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Authoring adapters for the roster and the delivery directory. These routes already
/// existed on the server and were exercised by its own tests; only the client had no way to reach
/// them, so the records they maintain (employees, drivers, delivery services) could be read but
/// never created or corrected from a station.</summary>
public sealed partial class ApiClient
{
    public Task<ApiResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default) =>
        SendAsync<EmployeeDto>(() => _http.PostAsJsonAsync("/api/employees", request, ct), ct);

    public Task<ApiResult<EmployeeDto>> UpdateEmployeeAsync(UpdateEmployeeRequest request, CancellationToken ct = default) =>
        SendAsync<EmployeeDto>(() => _http.PutAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}", request, ct), ct);

    /// <summary>Every service including retired ones - the authoring screen must show what it can
    /// reactivate, unlike the cashier's own list which deliberately offers only active services.</summary>
    public Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>> GetAllDeliveryServicesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services/all", ct), ct);

    public Task<ApiResult<DeliveryServiceDto>> CreateDeliveryServiceAsync(CreateDeliveryServiceRequest request, CancellationToken ct = default) =>
        SendAsync<DeliveryServiceDto>(() => _http.PostAsJsonAsync("/api/delivery/services", request, ct), ct);

    public Task<ApiResult<DeliveryServiceDto>> UpdateDeliveryServiceAsync(UpdateDeliveryServiceRequest request, CancellationToken ct = default) =>
        SendAsync<DeliveryServiceDto>(() => _http.PutAsJsonAsync(
            $"/api/delivery/services/{Uri.EscapeDataString(request.ServiceId)}", request, ct), ct);

    /// <summary>The physical denomination count for a business day. This is a different thing from
    /// the cashier's drawer-balance inquiry: that reads what the system believes is in the drawer,
    /// this records what a human actually counted, note by note, and lets the server compute the
    /// difference. Gated server-side on `cash_count`, which is deliberately not the same permission
    /// as `business_day_close`.</summary>
    public Task<ApiResult<IReadOnlyList<long>>> GetCashCountDenominationsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<long>>(() => _http.GetAsync("/api/business-day/cash-count-denominations", ct), ct);

    public Task<ApiResult<IReadOnlyList<CashDenominationCountDto>>> GetCashCountsAsync(string businessDayId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CashDenominationCountDto>>(() => _http.GetAsync(
            $"/api/business-day/{Uri.EscapeDataString(businessDayId)}/cash-counts", ct), ct);

    public Task<ApiResult<CashDenominationCountDto>> SaveCashCountAsync(SaveCashCountRequest request, CancellationToken ct = default) =>
        SendAsync<CashDenominationCountDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/cash-counts", request, ct), ct);

    /// <summary>A database restore discards every committed transaction made after the backup, so
    /// the server demands a reason, a literal confirmation phrase and zero in-flight financial
    /// operations on top of the usual permission-or-approval gate. The client mirrors those
    /// requirements rather than discovering them through a rejection.</summary>
    public Task<ApiResult<BackupRecordDto>> RestoreBackupAsync(RestoreBackupRequest request, CancellationToken ct = default) =>
        SendAsync<BackupRecordDto>(() => _http.PostAsJsonAsync(
            $"/api/backups/{Uri.EscapeDataString(request.BackupRecordId)}/restore", request, ct), ct);

    /// <summary>The whole owner picture in one read. Deliberately one call, not nine: the point of
    /// the dashboard is that an owner stops assembling the answer from separate screens.</summary>
    public Task<ApiResult<OwnerDashboardDto>> GetOwnerDashboardAsync(
        string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        SendAsync<OwnerDashboardDto>(() => _http.GetAsync(
            $"/api/reports/owner-dashboard?actorUserId={Uri.EscapeDataString(actorUserId)}" +
            $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);
}
