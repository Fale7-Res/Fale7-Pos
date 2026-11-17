using System.Net.Http.Json;
using Falah.Application.Delivery;
using Falah.Application.Purchasing;
using Falah.Application.Reports;
using Falah.Application.Shifts;
using Falah.Application.Treasury;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Financial control calls: the owner's financial report, collection shortfalls and their classification,
/// handover receipts, treasury entry corrections and supplier credits. Reads use ReadAsync; every write is a command
/// with a request id that the server replays (SendAsync, so an unknown outcome is reported as such).</summary>
public sealed partial class ApiClient
{
    public Task<ApiResult<OwnerFinancialReportDto>> GetOwnerFinancialReportAsync(string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        ReadAsync<OwnerFinancialReportDto>(() => _http.GetAsync(
            $"/api/reports/owner-financial?actorUserId={Uri.EscapeDataString(actorUserId)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);

    public Task<ApiResult<CollectionShortfallListDto>> GetCollectionShortfallsAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<CollectionShortfallListDto>(() => _http.GetAsync(
            $"/api/delivery/collection-shortfalls?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<CollectionShortfallDto>> ClassifyCollectionShortfallAsync(ClassifyCollectionShortfallRequest request, CancellationToken ct = default) =>
        SendAsync<CollectionShortfallDto>(() => _http.PostAsJsonAsync(
            $"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/collection-shortfall/classify", request, ct), ct);

    public Task<ApiResult<List<ShiftHandoverReceiptDto>>> GetUnconfirmedHandoversAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<List<ShiftHandoverReceiptDto>>(() => _http.GetAsync(
            $"/api/shifts/handovers/pending?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<ShiftHandoverReceiptDto>> AcknowledgeShiftHandoverAsync(AcknowledgeShiftHandoverRequest request, CancellationToken ct = default) =>
        SendAsync<ShiftHandoverReceiptDto>(() => _http.PostAsJsonAsync(
            $"/api/shifts/{Uri.EscapeDataString(request.ShiftId)}/handover/acknowledge", request, ct), ct);

    public Task<ApiResult<CorrectTreasuryMovementResult>> CorrectTreasuryMovementAsync(CorrectTreasuryMovementRequest request, CancellationToken ct = default) =>
        SendAsync<CorrectTreasuryMovementResult>(() => _http.PostAsJsonAsync(
            $"/api/treasury/movements/{Uri.EscapeDataString(request.MovementId)}/correct", request, ct), ct);

    public Task<ApiResult<SupplierCreditResult>> RecordSupplierCreditAsync(RecordSupplierCreditRequest request, CancellationToken ct = default) =>
        SendAsync<SupplierCreditResult>(() => _http.PostAsJsonAsync(
            $"/api/purchases/{Uri.EscapeDataString(request.PurchaseId)}/credits", request, ct), ct);
}
