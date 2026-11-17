namespace Falah.Infrastructure.Reports.Export;

using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Text;
using Falah.Application.Reports.Export;

/// <summary>
/// A4 PDF built from the pages <see cref="ReportPageRenderer"/> draws. Each page is one lossless grayscale image
/// (Flate) placed on an A4 page of exactly its orientation, at 200 DPI - so Arabic prints exactly as Windows shaped
/// it, with no font substitution in the viewer. The trade-off, stated rather than hidden: the text is not
/// selectable or searchable; the Excel and Word exports carry the same figures as editable text.
///
/// Document info carries the title, period and the snapshot fingerprint; the viewer is told the reading order is
/// right to left.
/// </summary>
[SupportedOSPlatform("windows")]
public static class PdfReportWriter
{
    public static byte[] Write(ReportDocument doc, IReadOnlyList<(Bitmap Image, bool Landscape)> pages)
    {
        var objects = new List<byte[]>();
        int Reserve() { objects.Add(Array.Empty<byte>()); return objects.Count; }
        void Set(int id, string text) => objects[id - 1] = Encoding.ASCII.GetBytes(text);
        void SetStream(int id, string dict, byte[] data)
        {
            var head = Encoding.ASCII.GetBytes($"{dict.TrimEnd('>').TrimEnd('>')} /Length {data.Length} >>\nstream\n");
            var tail = Encoding.ASCII.GetBytes("\nendstream");
            objects[id - 1] = head.Concat(data).Concat(tail).ToArray();
        }

        var catalog = Reserve();
        var pagesId = Reserve();
        var info = Reserve();
        var kids = new List<int>();
        foreach (var (image, landscape) in pages)
        {
            var pageId = Reserve();
            var contentId = Reserve();
            var imageId = Reserve();
            kids.Add(pageId);
            var widthPt = (image.Width * 72f / ReportPageRenderer.Dpi).ToString("0.##", CultureInfo.InvariantCulture);
            var heightPt = (image.Height * 72f / ReportPageRenderer.Dpi).ToString("0.##", CultureInfo.InvariantCulture);
            Set(pageId, $"<< /Type /Page /Parent {pagesId} 0 R /MediaBox [0 0 {widthPt} {heightPt}] /Resources << /XObject << /Im0 {imageId} 0 R >> >> /Contents {contentId} 0 R >>");
            SetStream(contentId, "<< >>", Encoding.ASCII.GetBytes($"q {widthPt} 0 0 {heightPt} 0 0 cm /Im0 Do Q"));
            SetStream(imageId, $"<< /Type /XObject /Subtype /Image /Width {image.Width} /Height {image.Height} /ColorSpace /DeviceGray /BitsPerComponent 8 /Filter /FlateDecode >>",
                GrayFlate(image));
        }
        Set(pagesId, $"<< /Type /Pages /Kids [{string.Join(" ", kids.Select(k => $"{k} 0 R"))}] /Count {kids.Count} >>");
        Set(catalog, $"<< /Type /Catalog /Pages {pagesId} 0 R /ViewerPreferences << /Direction /R2L /DisplayDocTitle true >> >>");
        Set(info, $"<< /Title {Utf16(doc.Title)} /Subject {Utf16(doc.ScopeLabel)} /Author {Utf16(doc.GeneratedBy)} /Creator (Falah POS) /Producer (Falah POS report export) " +
            $"/Keywords (falah-snapshot:{doc.Fingerprint}) /CreationDate (D:{doc.GeneratedAtUtc.UtcDateTime:yyyyMMddHHmmss}Z) >>");

        using var output = new MemoryStream();
        void W(string s) { var b = Encoding.ASCII.GetBytes(s); output.Write(b); }
        W("%PDF-1.5\n");
        output.Write(new byte[] { (byte)'%', 0xE2, 0xE3, 0xCF, 0xD3, (byte)'\n' });
        var offsets = new long[objects.Count];
        for (var i = 0; i < objects.Count; i++)
        {
            offsets[i] = output.Position;
            W($"{i + 1} 0 obj\n");
            output.Write(objects[i]);
            W("\nendobj\n");
        }
        var xref = output.Position;
        W($"xref\n0 {objects.Count + 1}\n0000000000 65535 f \n");
        foreach (var o in offsets) W($"{o:0000000000} 00000 n \n");
        W($"trailer\n<< /Size {objects.Count + 1} /Root {catalog} 0 R /Info {info} 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        return output.ToArray();
    }

    /// <summary>The page as 8-bit gray, zlib-compressed (PDF FlateDecode).</summary>
    private static byte[] GrayFlate(Bitmap image)
    {
        var rect = new Rectangle(0, 0, image.Width, image.Height);
        var data = image.LockBits(rect, ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
        try
        {
            var stride = data.Stride;
            var row = new byte[stride];
            var gray = new byte[image.Width * image.Height];
            for (var y = 0; y < image.Height; y++)
            {
                Marshal.Copy(data.Scan0 + y * stride, row, 0, stride);
                for (var x = 0; x < image.Width; x++)
                {
                    var b = row[x * 3]; var g = row[x * 3 + 1]; var r = row[x * 3 + 2];
                    gray[y * image.Width + x] = (byte)((r * 299 + g * 587 + b * 114) / 1000);
                }
            }
            using var compressed = new MemoryStream();
            using (var z = new ZLibStream(compressed, CompressionLevel.Optimal, leaveOpen: true)) z.Write(gray);
            return compressed.ToArray();
        }
        finally { image.UnlockBits(data); }
    }

    /// <summary>A PDF text string as UTF-16BE with a byte-order mark, in hex - the form that carries Arabic.</summary>
    private static string Utf16(string s) => "<FEFF" + Convert.ToHexString(Encoding.BigEndianUnicode.GetBytes(s)) + ">";
}
