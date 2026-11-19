namespace Falah.Infrastructure.Reports.Export;

using Falah.Application.Reports.Export;
using Falah.Domain.Audit;
using Falah.Domain.Common;
using Falah.Infrastructure.Persistence;

/// <summary>
/// One export: build the snapshot once, render it in the requested format, record who exported what. The audit
/// event names the report, format and period - never the figures. Rendering runs on a worker thread so a large
/// period does not hold up the request pipeline; the till's own requests are separate requests and are not
/// blocked by it.
/// </summary>
public sealed class ReportExportService
{
    private readonly ReportSnapshotBuilder _builder;
    private readonly IAuditLog _audit;
    private readonly FalahDbContext _db;

    public ReportExportService(ReportSnapshotBuilder builder, IAuditLog audit, FalahDbContext db)
    {
        _builder = builder;
        _audit = audit;
        _db = db;
    }

    public sealed record ExportFile(byte[] Content, string ContentType, string FileName, string Fingerprint);

    public static string ContentTypeFor(string format) => format switch
    {
        "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "pdf" => "application/pdf",
        "png" => "image/png",
        _ => throw new ArgumentException("صيغة غير مدعومة."),
    };

    /// <summary>Validates the scope the same way for every format: known report, known format, from ≤ to, at most
    /// <see cref="ReportExportCatalog.MaxDays"/> days, not ending after today.</summary>
    public static string? Validate(string? report, string? format, DateOnly from, DateOnly to)
    {
        if (report is null || ReportExportCatalog.Reports.All(r => r.Key != report)) return "نوع التقرير غير معروف.";
        if (format is not null && !ReportExportCatalog.AllFormats.Contains(format)) return "صيغة الملف غير مدعومة.";
        if (from > to) return "تاريخ البداية بعد تاريخ النهاية.";
        if (to.DayNumber - from.DayNumber + 1 > ReportExportCatalog.MaxDays) return $"الفترة أطول من {ReportExportCatalog.MaxDays} يومًا؛ صغّرها.";
        if (to > BusinessClock.Today()) return "الفترة تنتهي بعد اليوم.";
        return null;
    }

    public Task<ReportDocument> SnapshotAsync(string report, DateOnly from, DateOnly to, string generatedBy, CancellationToken ct) =>
        _builder.BuildAsync(report, from, to, generatedBy, ct);

    public async Task<ExportFile> ExportAsync(string report, string format, DateOnly from, DateOnly to,
        (string Id, string Name, string Role) actor, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) throw new PlatformNotSupportedException("تصدير التقارير يعمل على خادم ويندوز.");
        var doc = await _builder.BuildAsync(report, from, to, actor.Name, ct);
        var content = await Task.Run(() => Render(doc, format), ct);

        _audit.Append(new AuditEvent($"audit-{Guid.NewGuid():N}", actor.Id, actor.Name, actor.Role, "تصدير تقرير", "reports",
            $"{doc.Title} ({report}) بصيغة {format} للفترة {ReportText.Period(from, to)} - بصمة {doc.Fingerprint[..16]}",
            "normal", DateTimeOffset.UtcNow, ReferenceId: report));
        await _db.SaveChangesAsync(ct);

        return new ExportFile(content, ContentTypeFor(format), ReportText.SafeFileName(report, from, to, format), doc.Fingerprint);
    }

    public static byte[] Render(ReportDocument doc, string format)
    {
        if (!OperatingSystem.IsWindowsVersionAtLeast(6, 1)) throw new PlatformNotSupportedException();
        switch (format)
        {
            case "xlsx": return XlsxReportWriter.Write(doc);
            case "docx": return DocxReportWriter.Write(doc);
            case "png": return new PngSummaryWriter().Write(doc);
            case "pdf":
                var pages = new ReportPageRenderer().Render(doc);
                try { return PdfReportWriter.Write(doc, pages); }
                finally { foreach (var (image, _) in pages) image.Dispose(); }
            default: throw new ArgumentException("صيغة غير مدعومة.");
        }
    }
}
