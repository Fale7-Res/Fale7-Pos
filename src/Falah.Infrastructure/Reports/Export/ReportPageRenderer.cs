namespace Falah.Infrastructure.Reports.Export;

using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Versioning;
using Falah.Application.Reports.Export;

/// <summary>
/// Lays a <see cref="ReportDocument"/> out on A4 pages and draws them, with Windows' own text layout (GDI+), which
/// shapes and orders Arabic correctly. The result is the page images the PDF embeds.
///
/// - 200 DPI, 14 mm margins; a running header (title, restaurant, period) and footer (page X of Y, fingerprint).
/// - The first page carries the document title block, the warnings, and the KPI summary.
/// - Tables run right to left; their header row is repeated after every page break; a table wider than seven
///   columns is laid out on landscape pages so no column is squeezed out; totals are bold with a rule above.
/// - Long cell text wraps; nothing is cut off - a row grows to fit its tallest cell.
/// - Grayscale-friendly: black text, gray rules and fills, no colour carries meaning on its own.
///
/// Every string drawn is recorded in <see cref="DrawnText"/>, so a test can prove the pages show the snapshot's figures.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class ReportPageRenderer
{
    public const float Dpi = 200f;
    private static readonly Size Portrait = new(1654, 2339);
    private static readonly Size Landscape = new(2339, 1654);
    private const int Margin = 110;
    private const int HeaderHeight = 120;
    private const int FooterHeight = 70;
    private const int WideTableColumns = 7;

    public List<string> DrawnText { get; } = new();

    private sealed class Page
    {
        public bool IsLandscape;
        public readonly List<Action<Graphics>> Ops = new();
        public Size Size => IsLandscape ? Landscape : Portrait;
    }

    private readonly List<Page> _pages = new();
    private Page _page = default!;
    private float _y;
    private readonly string _family;

    public ReportPageRenderer()
    {
        using var installed = new InstalledFontCollection();
        _family = installed.Families.Any(f => f.Name == "Segoe UI") ? "Segoe UI" : "Arial";
    }

    public IReadOnlyList<(Bitmap Image, bool Landscape)> Render(ReportDocument doc)
    {
        _pages.Clear();
        NewPage(landscape: false);
        TitleBlock(doc);
        foreach (var w in doc.Warnings) WarningBox(w);
        foreach (var group in doc.Summary)
        {
            Heading(group.Title, null);
            DrawTable(
                new[] { new ReportColumn("المؤشر", ReportCellKind.Text, 4), new ReportColumn("القيمة", ReportCellKind.Text, 2), new ReportColumn("الحالة", ReportCellKind.Text, 1) },
                group.Kpis.Select(k => (IReadOnlyList<ReportCell>)new[]
                {
                    ReportCell.Of(k.Note is null ? k.Label : $"{k.Label}\n{k.Note}"), ReportCell.Of(ReportText.Cell(k.Value)), ReportCell.Of(ReportText.Status(k.Status)),
                }).ToList(), null, numericColumns: new[] { 1 });
        }
        foreach (var table in doc.Tables)
        {
            var wide = table.Columns.Count > WideTableColumns;
            if (wide != _page.IsLandscape) NewPage(wide);
            Heading(table.Title, table.Basis is null ? null : $"الأساس: {table.Basis}");
            if (table.Rows.Count == 0) Paragraph("لا توجد بيانات في هذه الفترة.", italic: true);
            else DrawTable(table.Columns, table.Rows, table.TotalRow,
                table.Columns.Select((c, i) => (c, i)).Where(x => x.c.Kind != ReportCellKind.Text).Select(x => x.i).ToArray());
            if (table.Note is { } note) Paragraph(note, italic: true);
            if (table.Truncated) Paragraph($"اقتُطع الجدول عند {ReportSnapshotBuilder.MaxRows:#,##0} صف.", italic: true);
        }
        if (doc.Notes.Count > 0)
        {
            if (_page.IsLandscape) NewPage(false);
            Heading("ملاحظات", null);
            foreach (var n in doc.Notes) Paragraph("• " + n);
        }

        var result = new List<(Bitmap, bool)>();
        for (var i = 0; i < _pages.Count; i++)
        {
            var page = _pages[i];
            var bitmap = new Bitmap(page.Size.Width, page.Size.Height);
            bitmap.SetResolution(Dpi, Dpi);
            using var g = Graphics.FromImage(bitmap);
            Prepare(g);
            g.Clear(Color.White);
            PageChrome(g, doc, page, i + 1, _pages.Count);
            foreach (var op in page.Ops) op(g);
            result.Add((bitmap, page.IsLandscape));
        }
        foreach (var font in _fonts) font.Dispose();
        _fonts.Clear();
        return result;
    }

    // ---------------------------------------------------------------- layout primitives

    private float Width => _page.Size.Width - 2 * Margin;
    private float Bottom => _page.Size.Height - Margin - FooterHeight;

    private void NewPage(bool landscape)
    {
        _page = new Page { IsLandscape = landscape };
        _pages.Add(_page);
        _y = Margin + HeaderHeight;
    }

    private void EnsureSpace(float height)
    {
        if (_y + height > Bottom) NewPage(_page.IsLandscape);
    }

    private readonly List<Font> _fonts = new();

    /// <summary>Fonts used by the deferred drawing operations live until the pages are drawn, then are disposed together.</summary>
    private Font F(float px, bool bold = false, bool italic = false)
    {
        var font = new Font(_family, px, (bold ? FontStyle.Bold : FontStyle.Regular) | (italic ? FontStyle.Italic : FontStyle.Regular), GraphicsUnit.Pixel);
        _fonts.Add(font);
        return font;
    }

    private static StringFormat Rtl(StringAlignment align = StringAlignment.Near) =>
        new(StringFormatFlags.DirectionRightToLeft) { Alignment = align, LineAlignment = StringAlignment.Near, Trimming = StringTrimming.None };

    private static void Prepare(Graphics g)
    {
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.PageUnit = GraphicsUnit.Pixel;
    }

    private static readonly Graphics Measurer = CreateMeasurer();
    private static readonly object MeasureLock = new();

    private static Graphics CreateMeasurer()
    {
        var g = Graphics.FromImage(new Bitmap(1, 1));
        Prepare(g);
        return g;
    }

    private float Measure(string text, Font font, float width)
    {
        using var format = Rtl();
        lock (MeasureLock) return Measurer.MeasureString(text.Length == 0 ? " " : text, font, (int)Math.Max(1, width), format).Height;
    }

    private void Text(string text, Font font, RectangleF rect, StringAlignment align, Brush? brush = null)
    {
        DrawnText.Add(text);
        _page.Ops.Add(g =>
        {
            using var format = Rtl(align);
            g.DrawString(text, font, brush ?? Brushes.Black, rect, format);
        });
    }

    // ---------------------------------------------------------------- blocks

    private void TitleBlock(ReportDocument doc)
    {
        var title = F(46, bold: true);
        var small = F(24);
        var h = Measure(doc.Title, title, Width);
        Text(doc.Title, title, new RectangleF(Margin, _y, Width, h), StringAlignment.Near);
        _y += h + 6;
        foreach (var line in new[] { doc.RestaurantName, $"الفترة: {doc.ScopeLabel}", $"الأساس: {doc.Basis}",
                     $"أُنشئ: {ReportText.At(doc.GeneratedAtUtc)} بتوقيت القاهرة · بواسطة: {doc.GeneratedBy}" })
        {
            var lh = Measure(line, small, Width);
            Text(line, small, new RectangleF(Margin, _y, Width, lh), StringAlignment.Near, Brushes.DimGray);
            _y += lh + 2;
        }
        var y = _y + 10;
        var lineWidth = Width;   // captured now: the operation runs after every page is laid out, when _page is the last one
        _page.Ops.Add(g => { using var pen = new Pen(Color.Black, 3); g.DrawLine(pen, Margin, y, Margin + lineWidth, y); });
        _y += 30;
    }

    private void WarningBox(string text)
    {
        var font = F(24, bold: true);
        var h = Measure("⚠ " + text, font, Width - 40) + 24;
        EnsureSpace(h + 12);
        var rect = new RectangleF(Margin, _y, Width, h);
        _page.Ops.Add(g =>
        {
            using var fill = new SolidBrush(Color.FromArgb(235, 235, 235));
            using var pen = new Pen(Color.Black, 3);
            g.FillRectangle(fill, rect);
            g.DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        });
        Text("⚠ " + text, font, new RectangleF(rect.X + 20, rect.Y + 12, rect.Width - 40, h - 24), StringAlignment.Near);
        _y += h + 14;
    }

    private void Heading(string title, string? subtitle)
    {
        var font = F(32, bold: true);
        var sub = F(21, italic: true);
        var h = Measure(title, font, Width) + (subtitle is null ? 0 : Measure(subtitle, sub, Width)) + 30;
        EnsureSpace(h + 120);   // never leave a heading alone at the foot of a page
        _y += 14;
        var th = Measure(title, font, Width);
        Text(title, font, new RectangleF(Margin, _y, Width, th), StringAlignment.Near);
        _y += th;
        if (subtitle is not null)
        {
            var sh = Measure(subtitle, sub, Width);
            Text(subtitle, sub, new RectangleF(Margin, _y, Width, sh), StringAlignment.Near, Brushes.DimGray);
            _y += sh;
        }
        _y += 10;
    }

    private void Paragraph(string text, bool italic = false)
    {
        var font = F(22, italic: italic);
        var h = Measure(text, font, Width);
        EnsureSpace(h + 8);
        Text(text, font, new RectangleF(Margin, _y, Width, h), StringAlignment.Near, italic ? Brushes.DimGray : Brushes.Black);
        _y += h + 8;
    }

    private void DrawTable(IReadOnlyList<ReportColumn> columns, IReadOnlyList<IReadOnlyList<ReportCell>> rows, IReadOnlyList<ReportCell>? total, int[] numericColumns)
    {
        var fontSize = _page.IsLandscape || columns.Count > 6 ? 20f : 22f;
        var body = F(fontSize);
        var bold = F(fontSize, bold: true);
        const float pad = 10;
        // Captured now: the drawing operations run after the whole document is laid out, when _page (and so Width) is
        // the LAST page - a landscape table was being drawn with the portrait width of a later page.
        var tableWidth = Width;
        var weights = columns.Select(c => (float)Math.Max(1, c.Weight)).ToArray();
        // Columns run right to left: column 0 is at the right edge.
        var widths = weights.Select(w => tableWidth * w / weights.Sum()).ToArray();
        float X(int i) => Margin + tableWidth - widths.Take(i + 1).Sum();

        float RowHeight(IReadOnlyList<string> texts, Font font) =>
            texts.Select((t, i) => Measure(t, font, widths[i] - 2 * pad)).DefaultIfEmpty(0).Max() + 2 * pad;

        var headerTexts = columns.Select(c => c.Header).ToList();
        var headerHeight = RowHeight(headerTexts, bold);

        void Header()
        {
            var y = _y;
            _page.Ops.Add(g =>
            {
                using var fill = new SolidBrush(Color.FromArgb(217, 217, 217));
                g.FillRectangle(fill, Margin, y, tableWidth, headerHeight);
            });
            for (var i = 0; i < columns.Count; i++)
                Text(headerTexts[i], bold, new RectangleF(X(i) + pad, y + pad, widths[i] - 2 * pad, headerHeight - 2 * pad), StringAlignment.Center);
            _y += headerHeight;
        }

        void Row(IReadOnlyList<ReportCell> cells, bool isTotal, int index)
        {
            var texts = columns.Select((_, i) => i < cells.Count ? ReportText.Cell(cells[i]) : "").ToList();
            var font = isTotal ? bold : body;
            var h = RowHeight(texts, font);
            if (_y + h > Bottom) { NewPage(_page.IsLandscape); Header(); }
            var y = _y;
            _page.Ops.Add(g =>
            {
                if (isTotal || index % 2 == 1)
                {
                    using var fill = new SolidBrush(isTotal ? Color.FromArgb(230, 230, 230) : Color.FromArgb(246, 246, 246));
                    g.FillRectangle(fill, Margin, y, tableWidth, h);
                }
                using var pen = new Pen(isTotal ? Color.Black : Color.FromArgb(200, 200, 200), isTotal ? 3 : 1);
                if (isTotal) g.DrawLine(pen, Margin, y, Margin + tableWidth, y);
                g.DrawLine(pen, Margin, y + h, Margin + tableWidth, y + h);
            });
            for (var i = 0; i < columns.Count; i++)
                Text(texts[i], font, new RectangleF(X(i) + pad, y + pad, widths[i] - 2 * pad, h - 2 * pad),
                    numericColumns.Contains(i) ? StringAlignment.Far : StringAlignment.Near);
            _y += h;
        }

        EnsureSpace(headerHeight + 60);
        Header();
        for (var r = 0; r < rows.Count; r++) Row(rows[r], false, r);
        if (total is not null) Row(total, true, 0);
        _y += 24;
    }

    private void PageChrome(Graphics g, ReportDocument doc, Page page, int number, int count)
    {
        var size = page.Size;
        var width = size.Width - 2 * Margin;
        using var small = F(20);
        using var bold = F(22, bold: true);
        using var right = Rtl(StringAlignment.Near);
        using var left = Rtl(StringAlignment.Far);
        using var center = Rtl(StringAlignment.Center);
        using var pen = new Pen(Color.FromArgb(150, 150, 150), 2);

        g.DrawString($"{doc.RestaurantName} · {doc.Title}", bold, Brushes.Black, new RectangleF(Margin, Margin, width, 40), right);
        g.DrawString(ReportText.Period(doc.From, doc.To), small, Brushes.DimGray, new RectangleF(Margin, Margin, width, 40), left);
        g.DrawLine(pen, Margin, Margin + 50, Margin + width, Margin + 50);

        var footerY = size.Height - Margin - FooterHeight + 20;
        g.DrawLine(pen, Margin, footerY, Margin + width, footerY);
        var pageText = $"صفحة {number} من {count}";
        DrawnText.Add(pageText);
        g.DrawString(pageText, small, Brushes.Black, new RectangleF(Margin, footerY + 10, width, 36), center);
        g.DrawString("تقرير داخلي - سري", small, Brushes.DimGray, new RectangleF(Margin, footerY + 10, width, 36), right);
        g.DrawString($"بصمة: {doc.Fingerprint[..Math.Min(16, doc.Fingerprint.Length)]}", small, Brushes.DimGray, new RectangleF(Margin, footerY + 10, width, 36), left);
    }
}
