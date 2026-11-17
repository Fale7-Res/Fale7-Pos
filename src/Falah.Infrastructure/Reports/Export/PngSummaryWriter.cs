namespace Falah.Infrastructure.Reports.Export;

using System.Buffers.Binary;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.Runtime.Versioning;
using System.Text;
using Falah.Application.Reports.Export;

/// <summary>
/// The executive summary as one image for sharing (WhatsApp): 1080 px wide, the report's KPI summary only - the
/// detailed tables stay in the PDF and the workbook. Laid out from the content so nothing is cropped: the image is as
/// tall as its lines, capped at <see cref="MaxHeight"/>; KPIs that would not fit are counted in a closing line
/// instead of being squeezed. Warnings (estimates, unavailable figures) are a banner at the top. The snapshot
/// fingerprint is written into the PNG as a text chunk, and drawn in the footer.
/// </summary>
[SupportedOSPlatform("windows")]
public sealed class PngSummaryWriter
{
    public const int Width = 1080;
    public const int MaxHeight = 4200;
    private const int Pad = 48;

    public List<string> DrawnText { get; } = new();

    public byte[] Write(ReportDocument doc)
    {
        using var installed = new InstalledFontCollection();
        var family = installed.Families.Any(f => f.Name == "Segoe UI") ? "Segoe UI" : "Arial";
        using var title = new Font(family, 46, FontStyle.Bold, GraphicsUnit.Pixel);
        using var sub = new Font(family, 26, FontStyle.Regular, GraphicsUnit.Pixel);
        using var group = new Font(family, 32, FontStyle.Bold, GraphicsUnit.Pixel);
        using var label = new Font(family, 28, FontStyle.Regular, GraphicsUnit.Pixel);
        using var value = new Font(family, 30, FontStyle.Bold, GraphicsUnit.Pixel);
        using var tag = new Font(family, 22, FontStyle.Bold, GraphicsUnit.Pixel);
        using var warn = new Font(family, 25, FontStyle.Bold, GraphicsUnit.Pixel);
        using var foot = new Font(family, 20, FontStyle.Regular, GraphicsUnit.Pixel);
        using var rtl = new StringFormat(StringFormatFlags.DirectionRightToLeft);
        using var rtlFar = new StringFormat(StringFormatFlags.DirectionRightToLeft) { Alignment = StringAlignment.Far };

        var ops = new List<Action<Graphics>>();
        using var measureBitmap = new Bitmap(1, 1);
        using var mg = Graphics.FromImage(measureBitmap);
        mg.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        float H(string s, Font f, float w) => mg.MeasureString(s.Length == 0 ? " " : s, f, (int)w, rtl).Height;
        void Draw(string s, Font f, Brush b, RectangleF r, StringFormat fmt) { DrawnText.Add(s); ops.Add(g => g.DrawString(s, f, b, r, fmt)); }
        var inner = Width - 2 * Pad;

        // Header band.
        var headerLines = new[] { (doc.RestaurantName, sub), (doc.Title, title), ($"الفترة: {ReportText.Period(doc.From, doc.To)}", sub),
            ($"أُنشئ: {ReportText.At(doc.GeneratedAtUtc)} بتوقيت القاهرة", sub) };
        var y = (float)Pad;
        foreach (var (text, font) in headerLines)
        {
            var h = H(text, font, inner);
            Draw(text, font, Brushes.White, new RectangleF(Pad, y, inner, h), rtl);
            y += h + 4;
        }
        var bandHeight = y + Pad / 2f;
        ops.Insert(0, g => { using var b = new SolidBrush(Color.FromArgb(38, 38, 38)); g.FillRectangle(b, 0, 0, Width, bandHeight); });
        y = bandHeight + 28;

        foreach (var w in doc.Warnings)
        {
            var h = H("⚠ " + w, warn, inner - 32) + 24;
            var top = y;
            ops.Add(g =>
            {
                using var fill = new SolidBrush(Color.FromArgb(255, 243, 205));
                using var pen = new Pen(Color.FromArgb(120, 90, 0), 3);
                g.FillRectangle(fill, Pad, top, inner, h);
                g.DrawRectangle(pen, Pad, top, inner, h);
            });
            Draw("⚠ " + w, warn, Brushes.Black, new RectangleF(Pad + 16, top + 12, inner - 32, h - 24), rtl);
            y += h + 14;
        }

        var footerReserve = 150f;
        var omitted = 0;
        foreach (var grp in doc.Summary)
        {
            var gh = H(grp.Title, group, inner);
            if (y + gh + 80 > MaxHeight - footerReserve) { omitted += grp.Kpis.Count; continue; }
            y += 18;
            Draw(grp.Title, group, Brushes.Black, new RectangleF(Pad, y, inner, gh), rtl);
            y += gh + 6;
            var lineY = y;
            ops.Add(g => { using var pen = new Pen(Color.FromArgb(38, 38, 38), 3); g.DrawLine(pen, Pad, lineY, Width - Pad, lineY); });
            y += 10;
            foreach (var k in grp.Kpis)
            {
                var valueText = ReportText.Cell(k.Value);
                var labelText = k.Status == "actual" ? k.Label : $"{k.Label} ({ReportText.Status(k.Status)})";
                var lh = Math.Max(H(labelText, label, inner * 0.6f), H(valueText, value, inner * 0.4f));
                var nh = k.Note is null ? 0 : H(k.Note, tag, inner * 0.6f);
                var rowH = lh + nh + 18;
                if (y + rowH > MaxHeight - footerReserve) { omitted++; continue; }
                Draw(labelText, label, k.Status == "actual" ? Brushes.Black : Brushes.DarkRed, new RectangleF(Pad + inner * 0.4f, y + 8, inner * 0.6f, lh), rtl);
                Draw(valueText, value, Brushes.Black, new RectangleF(Pad, y + 8, inner * 0.4f, lh), rtlFar);
                if (k.Note is not null) Draw(k.Note, tag, Brushes.DimGray, new RectangleF(Pad + inner * 0.4f, y + 8 + lh, inner * 0.6f, nh), rtl);
                var sep = y + rowH;
                ops.Add(g => { using var pen = new Pen(Color.FromArgb(215, 215, 215), 1); g.DrawLine(pen, Pad, sep, Width - Pad, sep); });
                y += rowH;
            }
        }
        y += 20;
        var closing = omitted > 0
            ? $"{omitted} مؤشرًا إضافيًا لم تتسع لها الصورة - التفاصيل الكاملة في ملف PDF أو Excel."
            : "التفاصيل الكاملة والجداول في ملف PDF أو Excel.";
        var ch = H(closing, foot, inner);
        Draw(closing, foot, Brushes.DimGray, new RectangleF(Pad, y, inner, ch), rtl);
        y += ch + 6;
        var fingerprint = $"بصمة البيانات: {doc.Fingerprint[..Math.Min(16, doc.Fingerprint.Length)]}";
        Draw(fingerprint, foot, Brushes.Gray, new RectangleF(Pad, y, inner, 30), rtl);
        var height = (int)Math.Min(MaxHeight, y + 30 + Pad);

        using var bitmap = new Bitmap(Width, height);
        using (var g = Graphics.FromImage(bitmap))
        {
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.White);
            foreach (var op in ops) op(g);
        }
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);
        return WithTextChunk(png.ToArray(), "falah-snapshot", doc.Fingerprint);
    }

    /// <summary>Inserts a PNG tEXt chunk right after IHDR (signature 8 bytes + IHDR 25 bytes).</summary>
    public static byte[] WithTextChunk(byte[] png, string key, string text)
    {
        var data = Encoding.Latin1.GetBytes(key + "\0" + text);
        var type = Encoding.ASCII.GetBytes("tEXt");
        var chunk = new byte[12 + data.Length];
        BinaryPrimitives.WriteInt32BigEndian(chunk, data.Length);
        type.CopyTo(chunk, 4);
        data.CopyTo(chunk, 8);
        BinaryPrimitives.WriteUInt32BigEndian(chunk.AsSpan(8 + data.Length), Crc32(chunk.AsSpan(4, 4 + data.Length)));
        const int afterIhdr = 8 + 25;
        return png[..afterIhdr].Concat(chunk).Concat(png[afterIhdr..]).ToArray();
    }

    private static uint Crc32(ReadOnlySpan<byte> bytes)
    {
        var crc = 0xFFFFFFFFu;
        foreach (var b in bytes)
        {
            crc ^= b;
            for (var k = 0; k < 8; k++) crc = (crc & 1) != 0 ? 0xEDB88320u ^ (crc >> 1) : crc >> 1;
        }
        return ~crc;
    }
}
