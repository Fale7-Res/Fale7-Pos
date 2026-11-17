namespace Falah.Server.IntegrationTests;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Versioning;
using System.Text;
using Falah.Application.Reports.Export;
using Falah.Infrastructure.Reports.Export;
using Xunit;

/// <summary>
/// The document renderers on their own, with a deterministic snapshot: a long table must flow onto further pages
/// with its header repeated and its total drawn once at the end; a wide table must go landscape; every figure the
/// snapshot holds must actually be drawn, in the same text the Word document carries. With FALAH_EXPORT_SAMPLES set
/// to a directory, the files and the PDF's page images are written there for visual inspection.
/// </summary>
[SupportedOSPlatform("windows")]
public class ReportRenderingTests
{
    private static ReportDocument Sample(int rows, int columns = 5)
    {
        var cols = Enumerable.Range(0, columns).Select(i => i == 0
            ? new ReportColumn("البيان", ReportCellKind.Text, 3)
            : new ReportColumn($"عمود {i}", ReportCellKind.Money)).ToArray();
        var data = Enumerable.Range(1, rows).Select(r => (IReadOnlyList<ReportCell>)cols.Select((c, i) =>
            i == 0 ? ReportCell.Of($"بند رقم {r} - شاورما فراخ وصمون") : ReportCell.Money(r * 1_000L + i)).ToList()).ToList();
        var total = cols.Select((c, i) => i == 0 ? ReportCell.Of("الإجمالي") : ReportCell.Money(data.Sum(r => r[i].Number!.Value))).ToList();
        var doc = new ReportDocument("sample", "تقرير تجريبي للتحقق من الطباعة", "مطعم فالح أبو العنبة", new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 14),
            "من 2026-11-01 إلى 2026-11-14 · أيام تقويمية بتوقيت القاهرة", "بوقت البيع", new DateTimeOffset(2026, 11, 14, 10, 0, 0, TimeSpan.Zero), "المالك",
            new[] { new ReportKpiGroup("الملخص", new[]
            {
                new ReportKpi("صافي المبيعات", ReportCell.Money(2_600_050)),
                new ReportKpi("النتيجة التشغيلية التقديرية", ReportCell.Money(null), "unavailable", "تكلفة بعض الأصناف غير معروفة"),
                new ReportKpi("مجمل الربح التقديري", ReportCell.Money(1_234_567), "estimate"),
                new ReportKpi("عدد الطلبات", ReportCell.Count(312)),
            }) },
            new[] { new ReportTable("lines", "تفصيل البنود", "بوقت البيع", cols, data, total, "ملاحظة الجدول") },
            new[] { "المبيعة تُحسب في فترة إنشائها، والمرتجع في فترة دفعه." },
            new[] { "يحتوي التقرير على أرقام تقديرية (موسومة «تقديري») - ليست أرقامًا فعلية." });
        return ReportSnapshotBuilder.WithFingerprint(doc);
    }

    [Fact]
    public void A_long_table_flows_onto_further_pages_with_its_header_repeated_and_its_total_drawn_once()
    {
        var doc = Sample(rows: 160);
        var renderer = new ReportPageRenderer();
        var pages = renderer.Render(doc);
        try
        {
            Assert.True(pages.Count >= 3, $"pages: {pages.Count}");
            Assert.All(pages, p => Assert.Equal((1654, 2339), (p.Image.Width, p.Image.Height)));
            Assert.Contains($"صفحة 1 من {pages.Count}", renderer.DrawnText);
            Assert.Contains($"صفحة {pages.Count} من {pages.Count}", renderer.DrawnText);
            // The header is repeated on every page the table continues onto (the last page may hold only the notes).
            Assert.InRange(renderer.DrawnText.Count(t => t == "عمود 4"), pages.Count - 1, pages.Count);
            var totalText = ReportText.Money(doc.Tables[0].TotalRow![1].Number!.Value);
            Assert.Single(renderer.DrawnText, t => t == totalText);
            for (var r = 1; r <= 160; r++) Assert.Contains($"بند رقم {r} - شاورما فراخ وصمون", renderer.DrawnText);   // no row lost at a break
            Assert.Contains("غير متاح", renderer.DrawnText);
            Assert.Contains("تقديري", renderer.DrawnText);

            var pdf = PdfReportWriter.Write(doc, pages);
            var text = Encoding.ASCII.GetString(pdf);
            Assert.StartsWith("%PDF-1.5", text);
            Assert.Equal(pages.Count, System.Text.RegularExpressions.Regex.Matches(text, "/Type /Page /Parent").Count);
            Assert.Contains($"falah-snapshot:{doc.Fingerprint}", text);
            Assert.Contains("/Direction /R2L", text);
            Assert.EndsWith("%%EOF\n", text);
            WriteSamples("long", doc, pages, pdf);
        }
        finally { foreach (var p in pages) p.Image.Dispose(); }
    }

    [Fact]
    public void A_wide_table_is_laid_out_on_landscape_pages_so_no_column_is_lost()
    {
        var doc = Sample(rows: 12, columns: 10);
        var renderer = new ReportPageRenderer();
        var pages = renderer.Render(doc);
        try
        {
            Assert.Contains(pages, p => p.Landscape && p.Image.Width == 2339);
            for (var c = 1; c < 10; c++) Assert.Contains($"عمود {c}", renderer.DrawnText);
            // Found on a rendered page: the landscape table's header fill and rules stopped at the PORTRAIT width,
            // because the deferred drawing read the last page's width. The header fill must reach the right margin.
            var landscape = pages.First(p => p.Landscape).Image;
            var x = landscape.Width - 130;
            Assert.Contains(Enumerable.Range(0, landscape.Height), y => landscape.GetPixel(x, y) is { R: 217, G: 217, B: 217 });
            var pdf = PdfReportWriter.Write(doc, pages);
            Assert.Contains("/MediaBox [0 0 842.04 595.44]", Encoding.ASCII.GetString(pdf));
            WriteSamples("wide", doc, pages, pdf);
        }
        finally { foreach (var p in pages) p.Image.Dispose(); }
    }

    [Fact]
    public void The_summary_image_is_a_readable_width_carries_the_warnings_and_every_kpi_and_is_never_endless()
    {
        var doc = Sample(rows: 3);
        var writer = new PngSummaryWriter();
        var png = writer.Write(doc);
        using var image = Image.FromStream(new MemoryStream(png));
        Assert.Equal(PngSummaryWriter.Width, image.Width);
        Assert.InRange(image.Height, 600, PngSummaryWriter.MaxHeight);
        Assert.Contains(writer.DrawnText, t => t.Contains("تقديرية"));
        Assert.Contains(ReportText.Money(2_600_050), writer.DrawnText);
        Assert.Contains("غير متاح", writer.DrawnText);
        Assert.Contains(doc.Fingerprint, Encoding.Latin1.GetString(png));

        // A summary with hundreds of KPIs stops at the cap and says how many it left to the full report.
        var huge = doc with { Summary = new[] { new ReportKpiGroup("كثير", Enumerable.Range(1, 300).Select(i => new ReportKpi($"مؤشر {i}", ReportCell.Count(i))).ToList()) } };
        var hugeWriter = new PngSummaryWriter();
        using var capped = Image.FromStream(new MemoryStream(hugeWriter.Write(huge)));
        Assert.True(capped.Height <= PngSummaryWriter.MaxHeight);
        Assert.Contains(hugeWriter.DrawnText, t => t.Contains("مؤشرًا إضافيًا"));
        WriteFile("summary.png", png);
    }

    [Fact]
    public void All_formats_of_one_snapshot_state_the_same_totals()
    {
        var doc = Sample(rows: 20);
        var total = ReportText.Money(doc.Tables[0].TotalRow![1].Number!.Value);
        var word = ReportExportService.Render(doc, "docx");
        var excel = ReportExportService.Render(doc, "xlsx");
        using var wz = new System.IO.Compression.ZipArchive(new MemoryStream(word));
        Assert.Contains(total, new StreamReader(wz.GetEntry("word/document.xml")!.Open()).ReadToEnd());
        using var xz = new System.IO.Compression.ZipArchive(new MemoryStream(excel));
        var pounds = ReportText.PoundsInvariant(doc.Tables[0].TotalRow![1].Number!.Value);
        Assert.Contains(xz.Entries.Where(e => e.FullName.StartsWith("xl/worksheets/")),
            e => new StreamReader(e.Open()).ReadToEnd().Contains($"<v>{pounds}</v>"));
        var renderer = new ReportPageRenderer();
        var pages = renderer.Render(doc);
        foreach (var p in pages) p.Image.Dispose();
        Assert.Contains(total, renderer.DrawnText);
        WriteFile("sample.docx", word);
        WriteFile("sample.xlsx", excel);
    }

    [Fact]
    public void Money_text_uses_integer_arithmetic_and_file_names_cannot_be_steered()
    {
        Assert.Equal("1,234,567.89 ج.م", ReportText.Money(123_456_789));
        Assert.Equal("-0.05 ج.م", ReportText.Money(-5));
        Assert.Equal("0.00 ج.م", ReportText.Money(0));
        Assert.Equal("1234567.89", ReportText.PoundsInvariant(123_456_789));
        Assert.Equal("falah-etcpasswd-20261101-20261114.pdf", ReportText.SafeFileName("../../etc/passwd", new(2026, 11, 1), new(2026, 11, 14), "pdf"));
        Assert.Equal("falah-report-20261101-20261101.exe", ReportText.SafeFileName("..\\..", new(2026, 11, 1), new(2026, 11, 1), ".e/x\\e"));
    }

    private static void WriteSamples(string name, ReportDocument doc, IReadOnlyList<(Bitmap Image, bool Landscape)> pages, byte[] pdf)
    {
        if (Environment.GetEnvironmentVariable("FALAH_EXPORT_SAMPLES") is not { Length: > 0 } dir) return;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, $"{name}.pdf"), pdf);
        for (var i = 0; i < pages.Count; i++) pages[i].Image.Save(Path.Combine(dir, $"{name}-page{i + 1}.png"), ImageFormat.Png);
        _ = doc;
    }

    private static void WriteFile(string name, byte[] bytes)
    {
        if (Environment.GetEnvironmentVariable("FALAH_EXPORT_SAMPLES") is not { Length: > 0 } dir) return;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, name), bytes);
    }
}
