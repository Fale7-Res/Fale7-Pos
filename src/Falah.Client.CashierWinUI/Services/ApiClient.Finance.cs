using System.Net.Http.Json;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Application.Users;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>The owner's financial control reads (cash flow per account, voids, unconfirmed electronic
/// payments, a shift's cash counts) and the user administrator's login-protection reads. The routes
/// already existed and were tested on the server; no screen could reach them.</summary>
public sealed partial class ApiClient
{
    public Task<ApiResult<CashFlowReportDto>> GetCashFlowAsync(string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        ReadAsync<CashFlowReportDto>(() => _http.GetAsync(
            $"/api/reports/cash-flow?actorUserId={Uri.EscapeDataString(actorUserId)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);

    public Task<ApiResult<VoidsReportDto>> GetVoidsAsync(string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        ReadAsync<VoidsReportDto>(() => _http.GetAsync(
            $"/api/reports/voids?actorUserId={Uri.EscapeDataString(actorUserId)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);

    /// <summary>Current state, not a period: every claim still waiting, however old.</summary>
    public Task<ApiResult<PendingElectronicReportDto>> GetPendingElectronicAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<PendingElectronicReportDto>(() => _http.GetAsync(
            $"/api/reports/pending-electronic?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<RefundActivityDto>>> GetRefundsAsync(string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<RefundActivityDto>>(() => _http.GetAsync(
            $"/api/reports/refunds?actorUserId={Uri.EscapeDataString(actorUserId)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);

    /// <summary>Every count typed for a shift's close, in order (shifts_manage / reports_view).</summary>
    public Task<ApiResult<IReadOnlyList<ShiftCashDeclarationDto>>> GetShiftCashDeclarationsAsync(string shiftId, CancellationToken ct = default) =>
        ReadAsync<IReadOnlyList<ShiftCashDeclarationDto>>(() => _http.GetAsync(
            $"/api/shifts/{Uri.EscapeDataString(shiftId)}/cash-declarations", ct), ct);

    public Task<ApiResult<SecurityPostureDto>> GetSecurityPostureAsync(CancellationToken ct = default) =>
        ReadAsync<SecurityPostureDto>(() => _http.GetAsync("/api/security/posture", ct), ct);

    public Task<ApiResult<bool>> ClearLoginSourceAsync(string source, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync("/api/security/login-sources/clear", new ClearLoginSourceRequest(source), ct), ct);
}
