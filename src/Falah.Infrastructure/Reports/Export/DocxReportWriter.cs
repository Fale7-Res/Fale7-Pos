namespace Falah.Infrastructure.Reports.Export;

using System.IO.Compression;
using System.Text;
using Falah.Application.Reports.Export;

/// <summary>
/// An editable Word document (Office Open XML WordprocessingML), written directly: A4, right-to-left section,
/// bidirectional paragraphs and runs, tables laid out right-to-left with their header row repeated on every page,
/// bold total rows, a footer with the page number. Every figure is the text <see cref="ReportText"/> makes from the
/// snapshot, so the document says exactly what the PDF and the workbook say.
/// </summary>
public static class DocxReportWriter
{
    private const string W = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";

    public static byte[] Write(ReportDocument doc)
    {
        var body = new StringBuilder();
        body.Append(P(doc.RestaurantName, size: 22, color: "595959"));
        body.Append(P(doc.Title, size: 36, bold: true));
        body.Append(P($"الفترة: {doc.ScopeLabel}", size: 21));
        body.Append(P($"الأساس: {doc.Basis}", size: 21));
        body.Append(P($"أُنشئ: {ReportText.At(doc.GeneratedAtUtc)} بتوقيت القاهرة · بواسطة: {doc.GeneratedBy}", size: 18, color: "595959"));
        foreach (var w in doc.Warnings) body.Append(P("⚠ " + w, size: 20, bold: true, color: "9C0006", shade: "FDECEA"));

        foreach (var group in doc.Summary)
        {
            body.Append(Heading(group.Title));
            body.Append(Table(
                new[] { new ReportColumn("المؤشر", ReportCellKind.Text, 3), new ReportColumn("القيمة", ReportCellKind.Text, 2), new ReportColumn("الحالة", ReportCellKind.Text, 1) },
                group.Kpis.Select(k => (IReadOnlyList<ReportCell>)new[]
                {
                    ReportCell.Of(k.Label + (k.Note is null ? "" : $"\n{k.Note}")), ReportCell.Of(ReportText.Cell(k.Value)), ReportCell.Of(ReportText.Status(k.Status)),
                }).ToList(), null));
        }
        foreach (var table in doc.Tables)
        {
            body.Append(Heading(table.Title));
            if (table.Basis is { } basis) body.Append(P($"الأساس: {basis}", size: 18, color: "595959"));
            if (table.Rows.Count == 0) body.Append(P("لا توجد بيانات في هذه الفترة.", size: 20, color: "9C0006"));
            else body.Append(Table(table.Columns, table.Rows, table.TotalRow));
            if (table.Note is { } note) body.Append(P(note, size: 18, color: "595959"));
            if (table.Truncated) body.Append(P($"اقتُطع الجدول عند {ReportSnapshotBuilder.MaxRows:#,##0} صف.", size: 18, color: "9C0006"));
        }
        if (doc.Notes.Count > 0)
        {
            body.Append(Heading("ملاحظات"));
            foreach (var n in doc.Notes) body.Append(P("• " + n, size: 20));
        }
        body.Append(P($"بصمة البيانات: {doc.Fingerprint}", size: 14, color: "7F7F7F"));

        var document = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:document xmlns:w=\"{W}\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><w:body>" +
            body + "<w:sectPr><w:footerReference w:type=\"default\" r:id=\"rIdFooter\"/><w:pgSz w:w=\"11906\" w:h=\"16838\"/>" +
            "<w:pgMar w:top=\"1134\" w:right=\"1021\" w:bottom=\"1134\" w:left=\"1021\" w:header=\"567\" w:footer=\"567\" w:gutter=\"0\"/><w:bidi/></w:sectPr></w:body></w:document>";
        var footer = $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:ftr xmlns:w=\"{W}\"><w:p><w:pPr><w:bidi/><w:jc w:val=\"center\"/></w:pPr>" +
            Run($"{doc.Title} · {ReportText.Period(doc.From, doc.To)} · صفحة ", 16, false, "595959") +
            "<w:r><w:rPr><w:rtl/><w:sz w:val=\"16\"/><w:szCs w:val=\"16\"/></w:rPr><w:fldChar w:fldCharType=\"begin\"/></w:r><w:r><w:rPr><w:rtl/><w:sz w:val=\"16\"/></w:rPr><w:instrText xml:space=\"preserve\"> PAGE </w:instrText></w:r>" +
            "<w:r><w:rPr><w:rtl/><w:sz w:val=\"16\"/></w:rPr><w:fldChar w:fldCharType=\"separate\"/></w:r><w:r><w:rPr><w:rtl/><w:sz w:val=\"16\"/></w:rPr><w:t>1</w:t></w:r>" +
            "<w:r><w:rPr><w:rtl/><w:sz w:val=\"16\"/></w:rPr><w:fldChar w:fldCharType=\"end\"/></w:r></w:p></w:ftr>";

        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "[Content_Types].xml",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
                "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
                "<Override PartName=\"/word/document.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.document.main+xml\"/>" +
                "<Override PartName=\"/word/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.styles+xml\"/>" +
                "<Override PartName=\"/word/footer1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.wordprocessingml.footer+xml\"/>" +
                "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/></Types>");
            Add(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"word/document.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/></Relationships>");
            Add(zip, "word/_rels/document.xml.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rIdStyles\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>" +
                "<Relationship Id=\"rIdFooter\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/footer\" Target=\"footer1.xml\"/></Relationships>");
            Add(zip, "word/document.xml", document);
            Add(zip, "word/footer1.xml", footer);
            Add(zip, "word/styles.xml",
                $"<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><w:styles xmlns:w=\"{W}\"><w:docDefaults><w:rPrDefault><w:rPr>" +
                "<w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" w:cs=\"Arial\" w:eastAsia=\"Arial\"/><w:sz w:val=\"20\"/><w:szCs w:val=\"20\"/><w:lang w:val=\"ar-EG\" w:bidi=\"ar-EG\"/>" +
                "</w:rPr></w:rPrDefault><w:pPrDefault><w:pPr><w:bidi/><w:spacing w:after=\"80\" w:line=\"264\" w:lineRule=\"auto\"/></w:pPr></w:pPrDefault></w:docDefaults>" +
                "<w:style w:type=\"paragraph\" w:default=\"1\" w:styleId=\"Normal\"><w:name w:val=\"Normal\"/></w:style>" +
                "<w:style w:type=\"table\" w:default=\"1\" w:styleId=\"TableNormal\"><w:name w:val=\"Normal Table\"/><w:tblPr><w:tblCellMar><w:top w:w=\"40\" w:type=\"dxa\"/>" +
                "<w:left w:w=\"80\" w:type=\"dxa\"/><w:bottom w:w=\"40\" w:type=\"dxa\"/><w:right w:w=\"80\" w:type=\"dxa\"/></w:tblCellMar></w:tblPr></w:style></w:styles>");
            Add(zip, "docProps/core.xml", OpenXmlParts.CoreProperties(doc));
        }
        return buffer.ToArray();
    }

    private static string Heading(string text) => P(text, size: 26, bold: true, spaceBefore: 240, keepNext: true);

    private static string P(string text, int size = 20, bool bold = false, string? color = null, string? shade = null, int spaceBefore = 0, bool keepNext = false, string? jc = null)
    {
        var ppr = new StringBuilder("<w:pPr>");
        if (keepNext) ppr.Append("<w:keepNext/>");
        if (shade is not null) ppr.Append($"<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{shade}\"/>");
        if (spaceBefore > 0) ppr.Append($"<w:spacing w:before=\"{spaceBefore}\"/>");
        ppr.Append("<w:bidi/>");
        if (jc is not null) ppr.Append($"<w:jc w:val=\"{jc}\"/>");
        ppr.Append("</w:pPr>");
        var runs = new StringBuilder();
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
        {
            if (i > 0) runs.Append("<w:r><w:br/></w:r>");
            runs.Append(Run(lines[i], size, bold, color));
        }
        return $"<w:p>{ppr}{runs}</w:p>";
    }

    private static string Run(string text, int size, bool bold, string? color) =>
        "<w:r><w:rPr><w:rFonts w:ascii=\"Arial\" w:hAnsi=\"Arial\" w:cs=\"Arial\"/>" + (bold ? "<w:b/><w:bCs/>" : "") +
        (color is null ? "" : $"<w:color w:val=\"{color}\"/>") + $"<w:sz w:val=\"{size}\"/><w:szCs w:val=\"{size}\"/><w:rtl/></w:rPr>" +
        $"<w:t xml:space=\"preserve\">{XlsxReportWriter.X(text)}</w:t></w:r>";

    private static string Table(IReadOnlyList<ReportColumn> columns, IReadOnlyList<IReadOnlyList<ReportCell>> rows, IReadOnlyList<ReportCell>? total)
    {
        const int usable = 9864;   // A4 width minus margins, twentieths of a point
        var weights = columns.Select(c => c.Kind is ReportCellKind.Text ? Math.Max(1, c.Weight) : 1).ToArray();
        var widths = weights.Select(w => usable * w / weights.Sum()).ToArray();
        var size = columns.Count > 7 ? 15 : 18;
        var sb = new StringBuilder("<w:tbl><w:tblPr><w:bidiVisual/><w:tblW w:w=\"5000\" w:type=\"pct\"/><w:tblBorders>" +
            "<w:top w:val=\"single\" w:sz=\"6\" w:color=\"808080\"/><w:bottom w:val=\"single\" w:sz=\"6\" w:color=\"808080\"/>" +
            "<w:insideH w:val=\"single\" w:sz=\"4\" w:color=\"BFBFBF\"/></w:tblBorders><w:tblLayout w:type=\"fixed\"/></w:tblPr><w:tblGrid>");
        foreach (var w in widths) sb.Append($"<w:gridCol w:w=\"{w}\"/>");
        sb.Append("</w:tblGrid>");
        sb.Append("<w:tr><w:trPr><w:tblHeader/><w:cantSplit/></w:trPr>");
        for (var i = 0; i < columns.Count; i++) sb.Append(Cell(columns[i].Header, widths[i], size, bold: true, shade: "D9D9D9", center: true));
        sb.Append("</w:tr>");
        foreach (var row in rows)
        {
            sb.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
            for (var i = 0; i < columns.Count; i++)
                sb.Append(Cell(i < row.Count ? ReportText.Cell(row[i]) : "", widths[i], size, bold: false, shade: null, center: false));
            sb.Append("</w:tr>");
        }
        if (total is not null)
        {
            sb.Append("<w:tr><w:trPr><w:cantSplit/></w:trPr>");
            for (var i = 0; i < columns.Count; i++)
                sb.Append(Cell(i < total.Count ? ReportText.Cell(total[i]) : "", widths[i], size, bold: true, shade: "F2F2F2", center: false));
            sb.Append("</w:tr>");
        }
        return sb.Append("</w:tbl>").Append(P("", size: 8)).ToString();
    }

    private static string Cell(string text, int width, int size, bool bold, string? shade, bool center) =>
        $"<w:tc><w:tcPr><w:tcW w:w=\"{width}\" w:type=\"dxa\"/>" + (shade is null ? "" : $"<w:shd w:val=\"clear\" w:color=\"auto\" w:fill=\"{shade}\"/>") +
        "</w:tcPr>" + P(text, size, bold, jc: center ? "center" : null) + "</w:tc>";

    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write(content);
    }
}
