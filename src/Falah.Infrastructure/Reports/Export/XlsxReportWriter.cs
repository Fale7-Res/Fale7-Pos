namespace Falah.Infrastructure.Reports.Export;

using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;
using Falah.Application.Reports.Export;

/// <summary>
/// A genuine Excel workbook (Office Open XML SpreadsheetML), written directly - no macros, no formulas, no external
/// links, no third-party library. One sheet for the summary, one per table, one for notes and warnings.
///
/// - Every sheet is right-to-left; headers are frozen and filtered; totals are bold rows below the data.
/// - Money is a NUMBER cell in pounds with the format <c>#,##0.00 "ج.م"</c>, written from integer piastres with
///   decimal arithmetic; unavailable money is the text "غير متاح", never 0. Dates and times are real date cells
///   (Cairo time).
/// - Text is written as an inline string, which Excel never evaluates; text that starts like a formula
///   (= + - @ tab CR) also gets the "quote prefix" style, so even editing the cell cannot turn it into one.
/// - The snapshot fingerprint is in the document properties and on the summary sheet.
/// </summary>
public static class XlsxReportWriter
{
    // Style indexes into cellXfs below.
    private const int SText = 0, SHeader = 1, SMoney = 2, SCount = 3, SDate = 4, SDateTime = 5, STotalText = 6, STotalMoney = 7,
        STotalCount = 8, STitle = 9, SWrap = 10, SWarning = 11, SQuoted = 12, SLabel = 13, SGroup = 14;

    public static byte[] Write(ReportDocument doc)
    {
        using var buffer = new MemoryStream();
        using (var zip = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            var sheets = new List<(string Name, (string Xml, int? Header) Sheet)> { ("الملخص", SummarySheet(doc)) };
            var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "الملخص", "ملاحظات" };
            foreach (var table in doc.Tables) sheets.Add((UniqueSheetName(table.Title, used), TableSheet(doc, table)));
            sheets.Add(("ملاحظات", (NotesSheet(doc), null)));

            Add(zip, "[Content_Types].xml", ContentTypes(sheets.Count));
            Add(zip, "_rels/.rels",
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/package/2006/relationships/metadata/core-properties\" Target=\"docProps/core.xml\"/>" +
                "<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/extended-properties\" Target=\"docProps/app.xml\"/></Relationships>");
            Add(zip, "docProps/core.xml", OpenXmlParts.CoreProperties(doc));
            Add(zip, "docProps/app.xml", "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Properties xmlns=\"http://schemas.openxmlformats.org/officeDocument/2006/extended-properties\"><Application>Falah POS</Application></Properties>");

            var wb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"><bookViews><workbookView/></bookViews><sheets>");
            var rels = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (var i = 0; i < sheets.Count; i++)
            {
                wb.Append($"<sheet name=\"{X(sheets[i].Name)}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
                rels.Append($"<Relationship Id=\"rId{i + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i + 1}.xml\"/>");
                Add(zip, $"xl/worksheets/sheet{i + 1}.xml", sheets[i].Sheet.Xml);
            }
            wb.Append("</sheets>");
            // Print titles: each sheet's header row repeats at the top of every printed page.
            var titles = sheets.Select((s, i) => (s.Name, s.Sheet.Header, Index: i)).Where(s => s.Header is not null).ToList();
            if (titles.Count > 0)
            {
                wb.Append("<definedNames>");
                foreach (var t in titles)
                    wb.Append($"<definedName name=\"_xlnm.Print_Titles\" localSheetId=\"{t.Index}\">'{X(t.Name.Replace("'", "''"))}'!${t.Header}:${t.Header}</definedName>");
                wb.Append("</definedNames>");
            }
            wb.Append("</workbook>");
            rels.Append($"<Relationship Id=\"rId{sheets.Count + 1}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/></Relationships>");
            Add(zip, "xl/workbook.xml", wb.ToString());
            Add(zip, "xl/_rels/workbook.xml.rels", rels.ToString());
            Add(zip, "xl/styles.xml", Styles());
        }
        return buffer.ToArray();
    }

    // ---------------------------------------------------------------- sheets

    private static (string Xml, int? Header) SummarySheet(ReportDocument doc)
    {
        var rows = new Rows();
        rows.Add(Text(doc.Title, STitle));
        rows.Add(Text("المطعم", SLabel), Text(doc.RestaurantName));
        rows.Add(Text("الفترة", SLabel), Text(doc.ScopeLabel));
        rows.Add(Text("الأساس", SLabel), Text(doc.Basis));
        rows.Add(Text("تاريخ الإنشاء", SLabel), Cell(ReportCell.At(doc.GeneratedAtUtc)));
        rows.Add(Text("أنشأه", SLabel), Text(doc.GeneratedBy));
        rows.Add(Text("بصمة البيانات", SLabel), Text(doc.Fingerprint));
        foreach (var w in doc.Warnings) rows.Add(Text("تنبيه", SWarning), Text(w, SWarning));
        // Metadata and warning values span B:D so long text (the period, the fingerprint) is never cut at a column edge.
        var merges = Enumerable.Range(2, rows.Count - 1).Select(r => $"B{r}:D{r}").ToList();
        rows.Blank();
        var header = rows.Add(Text("المؤشر", SHeader), Text("القيمة", SHeader), Text("الحالة", SHeader), Text("ملاحظة", SHeader));
        foreach (var group in doc.Summary)
        {
            rows.Add(Text(group.Title, SGroup), Text("", SGroup), Text("", SGroup), Text("", SGroup));
            foreach (var k in group.Kpis)
                rows.Add(Text(k.Label), Cell(k.Value), Text(ReportText.Status(k.Status), k.Status == "actual" ? SText : SWarning), Text(k.Note ?? "", SWrap));
        }
        return (Sheet(rows, new[] { 46d, 22, 12, 60 }, freezeBelowRow: header, filter: null, merges), header);
    }

    private static (string Xml, int? Header) TableSheet(ReportDocument doc, ReportTable table)
    {
        var rows = new Rows();
        rows.Add(Text(table.Title, STitle));
        rows.Add(Text($"{doc.Title} · {doc.ScopeLabel}" + (table.Basis is null ? "" : $" · الأساس: {table.Basis}")));
        var header = rows.Add(table.Columns.Select(c => Text(c.Header, SHeader)).ToArray());
        if (table.Rows.Count == 0) rows.Add(Text("لا توجد بيانات في هذه الفترة", SWarning));
        foreach (var r in table.Rows) rows.Add(r.Select(c => Cell(c)).ToArray());
        var lastData = rows.Count;
        if (table.TotalRow is { } total) rows.Add(total.Select(c => Cell(c, totals: true)).ToArray());
        if (table.Note is { } note) { rows.Blank(); rows.Add(Text(note, SWrap)); }
        if (table.Truncated) rows.Add(Text($"اقتُطع الجدول عند {ReportSnapshotBuilder.MaxRows:#,##0} صف.", SWarning));
        var widths = table.Columns.Select(c => c.Kind switch
        {
            ReportCellKind.Money => 18d, ReportCellKind.Count => 12, ReportCellKind.Date => 13, ReportCellKind.DateTime => 18,
            _ => Math.Min(70, 16 * c.Weight),
        }).ToArray();
        var filter = table.Rows.Count > 0 ? $"A{header}:{Col(table.Columns.Count - 1)}{lastData}" : null;
        return (Sheet(rows, widths, freezeBelowRow: header, filter), header);
    }

    private static string NotesSheet(ReportDocument doc)
    {
        var rows = new Rows();
        rows.Add(Text("ملاحظات وتنبيهات", STitle));
        foreach (var w in doc.Warnings) rows.Add(Text("تنبيه", SWarning), Text(w, SWarning));
        foreach (var n in doc.Notes) rows.Add(Text("ملاحظة", SLabel), Text(n, SWrap));
        rows.Add(Text("التقريب", SLabel), Text("المبالغ مخزنة بالقرش كأعداد صحيحة وتُعرض بالجنيه بخانتين عشريتين؛ لا تقريب في الحساب.", SWrap));
        rows.Add(Text("بصمة البيانات", SLabel), Text(doc.Fingerprint));
        return Sheet(rows, new[] { 16d, 110 }, freezeBelowRow: null, filter: null);
    }

    // ---------------------------------------------------------------- cells

    private sealed class Rows
    {
        public readonly List<string> Xml = new();
        public int Count => Xml.Count;
        public int Add(params string[] cells)
        {
            var r = Xml.Count + 1;
            var sb = new StringBuilder($"<row r=\"{r}\">");
            // The reference placeholder is a control character StripInvalidXml guarantees no content can contain.
            for (var i = 0; i < cells.Length; i++) sb.Append(cells[i].Replace(RefMark, $"{Col(i)}{r}"));
            Xml.Add(sb.Append("</row>").ToString());
            return r;
        }
        public void Blank() => Xml.Add($"<row r=\"{Xml.Count + 1}\"/>");
    }

    private static string Text(string text, int style = SText)
    {
        // Excel lays out bidi itself and mishandles explicit embedding characters (seen: the period text clipped);
        // dates in the workbook are real date cells, so the embeddings the printed formats need are dropped here.
        text = text.Replace("‪", "").Replace("‬", "");
        var quoted = text.Length > 0 && text[0] is '=' or '+' or '-' or '@' or '\t' or '\r';
        return $"<c r=\"{RefMark}\" t=\"inlineStr\" s=\"{(quoted && style == SText ? SQuoted : style)}\"><is><t xml:space=\"preserve\">{X(text)}</t></is></c>";
    }

    private const string RefMark = "\u0001";

    private static string Number(string invariant, int style) => $"<c r=\"{RefMark}\" s=\"{style}\"><v>{invariant}</v></c>";

    private static string Cell(ReportCell cell, bool totals = false) => cell.Kind switch
    {
        ReportCellKind.Money when cell.Number is { } p => Number(ReportText.PoundsInvariant(p), totals ? STotalMoney : SMoney),
        ReportCellKind.Count when cell.Number is { } n => Number(n.ToString(CultureInfo.InvariantCulture), totals ? STotalCount : SCount),
        ReportCellKind.Money or ReportCellKind.Count => Text(ReportText.Unavailable, SWarning),
        ReportCellKind.Date when cell.Date is { } d => Number(Serial(d.ToDateTime(TimeOnly.MinValue)), SDate),
        ReportCellKind.DateTime when cell.Instant is { } i => Number(Serial(ReportText.Cairo(i)), SDateTime),
        ReportCellKind.Date or ReportCellKind.DateTime => Text(""),
        _ => Text(cell.Text ?? "", totals ? STotalText : SText),
    };

    /// <summary>Excel's date serial (days since 1899-12-30), in invariant form.</summary>
    private static string Serial(DateTime local) =>
        ((decimal)(local - new DateTime(1899, 12, 30)).Ticks / TimeSpan.TicksPerDay).ToString("0.##########", CultureInfo.InvariantCulture);

    private static string Sheet(Rows rows, double[] widths, int? freezeBelowRow, string? filter, IReadOnlyList<string>? merges = null)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
        // Printed one page wide (as many pages tall as needed): no column is ever split onto a page of its own.
        sb.Append("<sheetPr><pageSetUpPr fitToPage=\"1\"/></sheetPr>");
        sb.Append("<sheetViews><sheetView rightToLeft=\"1\" workbookViewId=\"0\">");
        if (freezeBelowRow is { } f)
            sb.Append($"<pane ySplit=\"{f}\" topLeftCell=\"A{f + 1}\" activePane=\"bottomLeft\" state=\"frozen\"/><selection pane=\"bottomLeft\" activeCell=\"A{f + 1}\" sqref=\"A{f + 1}\"/>");
        sb.Append("</sheetView></sheetViews><sheetFormatPr defaultRowHeight=\"16\"/><cols>");
        for (var i = 0; i < widths.Length; i++)
            sb.Append($"<col min=\"{i + 1}\" max=\"{i + 1}\" width=\"{widths[i].ToString(CultureInfo.InvariantCulture)}\" customWidth=\"1\"/>");
        sb.Append("</cols><sheetData>");
        foreach (var r in rows.Xml) sb.Append(r);
        sb.Append("</sheetData>");
        if (filter is not null) sb.Append($"<autoFilter ref=\"{filter}\"/>");
        if (merges is { Count: > 0 })
            sb.Append($"<mergeCells count=\"{merges.Count}\">{string.Concat(merges.Select(m => $"<mergeCell ref=\"{m}\"/>"))}</mergeCells>");
        sb.Append("<pageMargins left=\"0.5\" right=\"0.5\" top=\"0.6\" bottom=\"0.6\" header=\"0.3\" footer=\"0.3\"/>");
        sb.Append("<pageSetup paperSize=\"9\" orientation=\"landscape\" fitToWidth=\"1\" fitToHeight=\"0\"/>");
        sb.Append("<headerFooter><oddFooter>&amp;C&amp;P / &amp;N</oddFooter></headerFooter></worksheet>");
        return sb.ToString();
    }

    private static string Styles() =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
        "<numFmts count=\"4\"><numFmt numFmtId=\"164\" formatCode=\"#,##0.00 &quot;ج.م&quot;;-#,##0.00 &quot;ج.م&quot;\"/><numFmt numFmtId=\"165\" formatCode=\"#,##0\"/>" +
        "<numFmt numFmtId=\"166\" formatCode=\"yyyy-mm-dd\"/><numFmt numFmtId=\"167\" formatCode=\"yyyy-mm-dd hh:mm\"/></numFmts>" +
        "<fonts count=\"5\"><font><sz val=\"11\"/><name val=\"Arial\"/></font><font><b/><sz val=\"11\"/><name val=\"Arial\"/></font>" +
        "<font><b/><sz val=\"14\"/><name val=\"Arial\"/></font><font><i/><sz val=\"11\"/><color rgb=\"FF9C0006\"/><name val=\"Arial\"/></font><font><sz val=\"11\"/><color rgb=\"FF444444\"/><name val=\"Arial\"/></font></fonts>" +
        "<fills count=\"4\"><fill><patternFill patternType=\"none\"/></fill><fill><patternFill patternType=\"gray125\"/></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFD9D9D9\"/><bgColor indexed=\"64\"/></patternFill></fill>" +
        "<fill><patternFill patternType=\"solid\"><fgColor rgb=\"FFF2F2F2\"/><bgColor indexed=\"64\"/></patternFill></fill></fills>" +
        "<borders count=\"3\"><border><left/><right/><top/><bottom/><diagonal/></border>" +
        "<border><left/><right/><top/><bottom style=\"thin\"><color rgb=\"FF808080\"/></bottom><diagonal/></border>" +
        "<border><left/><right/><top style=\"thin\"><color rgb=\"FF000000\"/></top><bottom style=\"double\"><color rgb=\"FF000000\"/></bottom><diagonal/></border></borders>" +
        "<cellStyleXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellStyleXfs>" +
        "<cellXfs count=\"15\">" +
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"right\" readingOrder=\"2\"/></xf>" +                    // 0 text
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"2\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"center\" vertical=\"center\" wrapText=\"1\" readingOrder=\"2\"/></xf>" + // 1 header
        "<xf numFmtId=\"164\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +                                                // 2 money
        "<xf numFmtId=\"165\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +                                                // 3 count
        "<xf numFmtId=\"166\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +                                                // 4 date
        "<xf numFmtId=\"167\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyNumberFormat=\"1\"/>" +                                                // 5 datetime
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"3\" borderId=\"2\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" readingOrder=\"2\"/></xf>" + // 6 total text
        "<xf numFmtId=\"164\" fontId=\"1\" fillId=\"3\" borderId=\"2\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" + // 7 total money
        "<xf numFmtId=\"165\" fontId=\"1\" fillId=\"3\" borderId=\"2\" xfId=\"0\" applyNumberFormat=\"1\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\"/>" + // 8 total count
        "<xf numFmtId=\"0\" fontId=\"2\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment readingOrder=\"2\"/></xf>" + // 9 title
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyAlignment=\"1\"><alignment horizontal=\"right\" wrapText=\"1\" vertical=\"top\" readingOrder=\"2\"/></xf>" + // 10 wrap
        "<xf numFmtId=\"0\" fontId=\"3\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" wrapText=\"1\" readingOrder=\"2\"/></xf>" + // 11 warning
        "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\" xfId=\"0\" quotePrefix=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" readingOrder=\"2\"/></xf>" + // 12 quoted text
        "<xf numFmtId=\"0\" fontId=\"4\" fillId=\"0\" borderId=\"0\" xfId=\"0\" applyFont=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" readingOrder=\"2\"/></xf>" + // 13 label
        "<xf numFmtId=\"0\" fontId=\"1\" fillId=\"3\" borderId=\"1\" xfId=\"0\" applyFont=\"1\" applyFill=\"1\" applyBorder=\"1\" applyAlignment=\"1\"><alignment horizontal=\"right\" readingOrder=\"2\"/></xf>" + // 14 KPI group title
        "</cellXfs><cellStyles count=\"1\"><cellStyle name=\"Normal\" xfId=\"0\" builtinId=\"0\"/></cellStyles></styleSheet>";

    private static string ContentTypes(int sheetCount)
    {
        var sb = new StringBuilder("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">" +
            "<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/><Default Extension=\"xml\" ContentType=\"application/xml\"/>" +
            "<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>" +
            "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" +
            "<Override PartName=\"/docProps/core.xml\" ContentType=\"application/vnd.openxmlformats-package.core-properties+xml\"/>" +
            "<Override PartName=\"/docProps/app.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.extended-properties+xml\"/>");
        for (var i = 1; i <= sheetCount; i++)
            sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
        return sb.Append("</Types>").ToString();
    }

    /// <summary>Excel sheet names: at most 31 characters, none of []:*?/\ and unique.</summary>
    private static string UniqueSheetName(string title, HashSet<string> used)
    {
        var clean = new string(title.Where(c => c is not ('[' or ']' or ':' or '*' or '?' or '/' or '\\')).ToArray()).Trim();
        if (clean.Length > 28) clean = clean[..28].Trim();
        if (clean.Length == 0) clean = "جدول";
        var name = clean;
        for (var n = 2; !used.Add(name); n++) name = $"{clean} {n}";
        return name;
    }

    private static string Col(int index)
    {
        var s = "";
        for (index++; index > 0; index = (index - 1) / 26) s = (char)('A' + (index - 1) % 26) + s;
        return s;
    }

    internal static string X(string s) => SecurityElement.Escape(StripInvalidXml(s)) ?? "";

    /// <summary>Characters XML 1.0 cannot carry are dropped (a stray control character must not corrupt the file).</summary>
    internal static string StripInvalidXml(string s) =>
        s.Any(c => c < 0x20 && c is not ('\t' or '\n' or '\r')) ? new string(s.Where(c => c >= 0x20 || c is '\t' or '\n' or '\r').ToArray()) : s;

    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var w = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        w.Write(content);
    }
}

/// <summary>Parts shared by the Excel and Word packages.</summary>
internal static class OpenXmlParts
{
    public static string CoreProperties(ReportDocument doc) =>
        "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?><cp:coreProperties xmlns:cp=\"http://schemas.openxmlformats.org/package/2006/metadata/core-properties\" " +
        "xmlns:dc=\"http://purl.org/dc/elements/1.1/\" xmlns:dcterms=\"http://purl.org/dc/terms/\" xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\">" +
        $"<dc:title>{XlsxReportWriter.X(doc.Title)}</dc:title><dc:subject>{XlsxReportWriter.X(doc.ScopeLabel)}</dc:subject><dc:creator>{XlsxReportWriter.X(doc.GeneratedBy)}</dc:creator>" +
        $"<cp:keywords>falah-snapshot:{doc.Fingerprint}</cp:keywords><dc:description>{XlsxReportWriter.X(doc.Basis)}</dc:description>" +
        $"<dcterms:created xsi:type=\"dcterms:W3CDTF\">{doc.GeneratedAtUtc.UtcDateTime:yyyy-MM-ddTHH:mm:ssZ}</dcterms:created></cp:coreProperties>";
}
