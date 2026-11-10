namespace Falah.Server.IntegrationTests;

using System.Runtime.Versioning;
using System.Text;
using Falah.Application.Printing;
using Falah.Domain.Printing;
using Falah.PrintAgent;
using Xunit;
using ZXing;
using ZXing.Common;
using ZXing.QrCode;

/// <summary>
/// The menu QR on the customer receipt, checked on the exact bits the printer would receive: each
/// ticket is rendered by the production BitmapTicketRenderer, its GS v 0 raster is read back out of
/// the byte stream, and the code is decoded with an independent reader (ZXing). A code that merely
/// looks right is not enough - it has to scan.
/// </summary>
[SupportedOSPlatform("windows")]
public class MenuQrRenderingTests
{
    private const string Link = "https://fale7-res.vercel.app/";

    private static ComposedTicket Ticket(params TicketElement[] elements) =>
        new(PrintRoute.Receipt, "ord-1", "#0001", elements);

    private sealed record Raster(int Width, int Height, bool[,] Ink);

    /// <summary>Every GS v 0 raster in a rendered job, as a dark/light matrix.</summary>
    private static List<Raster> Rasters(byte[] bytes)
    {
        var result = new List<Raster>();
        for (var i = 0; i + 8 <= bytes.Length; i++)
        {
            if (bytes[i] != 0x1D || bytes[i + 1] != 0x76 || bytes[i + 2] != 0x30 || bytes[i + 3] != 0x00) continue;
            var widthBytes = bytes[i + 4] + (bytes[i + 5] << 8);
            var height = bytes[i + 6] + (bytes[i + 7] << 8);
            var ink = new bool[widthBytes * 8, height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < widthBytes * 8; x++)
                    ink[x, y] = ((bytes[i + 8 + y * widthBytes + x / 8] >> (7 - x % 8)) & 1) == 1;
            result.Add(new Raster(widthBytes * 8, height, ink));
            i += 8 + widthBytes * height - 1;
        }
        return result;
    }

    private static string? Decode(Raster raster)
    {
        var luminance = new byte[raster.Width * raster.Height];
        for (var y = 0; y < raster.Height; y++)
            for (var x = 0; x < raster.Width; x++)
                luminance[y * raster.Width + x] = raster.Ink[x, y] ? (byte)0 : (byte)255;
        var source = new RGBLuminanceSource(luminance, raster.Width, raster.Height, RGBLuminanceSource.BitmapFormat.Gray8);
        try
        {
            return new QRCodeReader().decode(new BinaryBitmap(new HybridBinarizer(source)),
                new Dictionary<DecodeHintType, object> { [DecodeHintType.TRY_HARDER] = true })?.Text;
        }
        catch (ReaderException)
        {
            return null;
        }
    }

    private static (int Left, int Top, int Right, int Bottom) InkBounds(Raster raster)
    {
        int left = raster.Width, top = raster.Height, right = -1, bottom = -1;
        for (var y = 0; y < raster.Height; y++)
            for (var x = 0; x < raster.Width; x++)
                if (raster.Ink[x, y])
                {
                    left = Math.Min(left, x); right = Math.Max(right, x);
                    top = Math.Min(top, y); bottom = Math.Max(bottom, y);
                }
        return (left, top, right, bottom);
    }

    [Fact]
    public void The_qr_and_the_signature_rule_survive_the_wire_contract()
    {
        var original = new TicketElement[] { new TicketQr(Link), new TicketRule(TicketRuleStyle.Accent) };

        var roundTripped = original.Select(TicketElementMapping.ToDto).Select(TicketElementMapping.ToDomain).ToList();

        Assert.Equal(original, roundTripped);
        // The link rides in Text, so an agent that predates QR support still prints it as a line.
        Assert.Equal(Link, TicketElementMapping.ToDto(original[0]).Text);
    }

    /// <summary>The whole cashier receipt, rendered at the production settings, decodes back to the
    /// configured link - with thickened bold text directly above and below the code.</summary>
    [Fact]
    public void The_rendered_receipt_qr_decodes_to_the_menu_link()
    {
        var bytes = BitmapTicketRenderer.Render(Ticket(
            new TicketText("امسح الكود لعرض المنيو", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Large),
            new TicketQr(Link),
            new TicketText("فالح أبو العنبه", Bold: true, Align: TicketAlign.Center, Size: TicketTextSize.Title)),
            paperWidthDots: 576, thickenStrokes: true, marginDots: 24);

        var raster = Assert.Single(Rasters(bytes));
        Assert.Equal(576, raster.Width);
        Assert.Equal(Link, Decode(raster));
    }

    /// <summary>The QR is stamped after stroke thickening, so the 1px dilation that darkens text
    /// never touches it: the code's bits are identical with thickening on and off.</summary>
    [Fact]
    public void Stroke_thickening_never_touches_the_qr_modules()
    {
        var ticket = Ticket(new TicketQr(Link));

        var thickened = BitmapTicketRenderer.Render(ticket, 576, thickenStrokes: true, marginDots: 24);
        var plain = BitmapTicketRenderer.Render(ticket, 576, thickenStrokes: false, marginDots: 24);

        Assert.Equal(plain, thickened);
    }

    /// <summary>Printed at 25-30 mm (203 dpi = 8 dots per mm), centred on the paper, with every
    /// module a whole number of dots - so the head prints square modules, not rounded fractions.</summary>
    [Fact]
    public void The_qr_is_25_to_30_mm_centred_and_drawn_in_whole_dot_modules()
    {
        var raster = Assert.Single(Rasters(BitmapTicketRenderer.Render(Ticket(new TicketQr(Link)), 576, true, 24)));
        var (left, top, right, bottom) = InkBounds(raster);
        var side = right - left + 1;

        Assert.Equal(side, bottom - top + 1);
        Assert.InRange(side, 200, 240);
        Assert.InRange((left + right) / 2.0 - (raster.Width - 1) / 2.0, -1, 1);

        // The top-left finder's top edge is one solid run of exactly 7 modules.
        var finder = 0;
        while (raster.Ink[left + finder, top]) finder++;
        Assert.Equal(0, finder % 7);
        var module = finder / 7;
        Assert.Equal(0, side % module);

        // Every dark or light run across the symbol is a whole number of modules.
        for (var y = top; y <= bottom; y++)
            for (var x = left; x <= right;)
            {
                var colour = raster.Ink[x, y];
                var run = 0;
                while (x <= right && raster.Ink[x, y] == colour) { run++; x++; }
                Assert.True(run % module == 0, $"row {y}: a run of {run} dots is not a whole number of {module}-dot modules");
            }

        // The quiet zone: four modules of white above the symbol, inside the raster.
        Assert.True(top >= 4 * module, $"quiet zone above the code is {top} dots, needs {4 * module}");
    }

    /// <summary>A link too long to fit the paper at a readable size is printed as text rather than
    /// as an unscannable code - and never throws, because a crashing station agent prints nothing.</summary>
    [Fact]
    public void A_payload_too_long_for_a_readable_code_falls_back_to_text()
    {
        var huge = "https://menu.example/" + new string('a', 2500);

        var raster = Assert.Single(Rasters(BitmapTicketRenderer.Render(Ticket(new TicketQr(huge)), 576, true, 24)));

        Assert.Null(Decode(raster));
        Assert.True(raster.Height > 0);
    }

    /// <summary>Character mode has no proven QR command on this printer (and unknown commands
    /// corrupt the stream), so it prints the link itself.</summary>
    [Fact]
    public void The_character_mode_renderer_prints_the_link_as_text()
    {
        var bytes = EscPosTicketRenderer.Render(Ticket(new TicketQr(Link)));

        Assert.Contains(Link, Encoding.UTF8.GetString(bytes));
        // No GS ( k - the firmware QR command family.
        for (var i = 0; i + 2 < bytes.Length; i++)
            Assert.False(bytes[i] == 0x1D && bytes[i + 1] == 0x28 && bytes[i + 2] == 0x6B, "GS ( k must not be emitted");
    }
}
