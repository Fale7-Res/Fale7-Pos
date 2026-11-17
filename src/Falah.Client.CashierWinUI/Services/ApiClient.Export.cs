using System.Net.Http.Headers;
using Falah.Application.Reports.Export;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>Report export: which reports this user may export (the server decides, from the same permissions as
/// the screens), and the generated file itself. A read, not a command - nothing changes on the server except the
/// audit record of the export.</summary>
public sealed partial class ApiClient
{
    public sealed record ExportedReport(byte[] Content, string FileName, string ContentType, string? Fingerprint);

    public Task<ApiResult<List<ReportExportCatalogEntry>>> GetReportExportCatalogAsync(string actorUserId, CancellationToken ct = default) =>
        ReadAsync<List<ReportExportCatalogEntry>>(() => _http.GetAsync(
            $"/api/reports/export/catalog?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public async Task<ApiResult<ExportedReport>> ExportReportAsync(string actorUserId, string report, string format, DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get,
                $"/api/reports/export?actorUserId={Uri.EscapeDataString(actorUserId)}&report={Uri.EscapeDataString(report)}&format={Uri.EscapeDataString(format)}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}");
            request.Headers.Authorization = _http.DefaultRequestHeaders.Authorization;
            using var response = await _exportHttp.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            if (!response.IsSuccessStatusCode) return await ReadResultAsync<ExportedReport>(response, ct);
            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            if (bytes.Length == 0) return new(false, default, "وصل ملف فارغ من السيرفر. أعد المحاولة.", true);
            var name = response.Content.Headers.ContentDisposition?.FileNameStar ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? $"falah-{report}.{format}";
            var fingerprint = response.Headers.TryGetValues("X-Falah-Snapshot", out var values) ? values.FirstOrDefault() : null;
            return new(true, new ExportedReport(bytes, name, response.Content.Headers.ContentType?.MediaType ?? "", fingerprint), null);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or IOException)
        {
            return new(false, default, ex is OperationCanceledException
                ? "استغرق إنشاء التقرير وقتًا طويلًا. صغّر الفترة أو أعد المحاولة."
                : "تعذر إنشاء التقرير من السيرفر. تأكد من الاتصال ثم أعد المحاولة.", true);
        }
    }
}
