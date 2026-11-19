using System.Drawing;
using Falah.Domain.Printing;
using Falah.PrintAgent;
using Xunit;

namespace Falah.Server.IntegrationTests;

/// <summary>Paper redesign 2026-11-17: the Dubai typeface (with a safe fallback), the embedded logo,
/// and label/value rows whose label never wraps. Renders through the production BitmapTicketRenderer
/// and reads the result back out of the ESC/POS bytes. No database.</summary>
public class TicketTypographyTests
{
    [Fact]
    public void Dubai_is_used_when_installed() =>
        Assert.Equal(("Dubai Medium", "Dubai", false),
            BitmapTicketRenderer.ResolveFaces(new[] { "Dubai", "Dubai Medium", "Segoe UI", "Segoe UI Semibold" }));

    [Fact]
    public void Missing_dubai_falls_back_to_the_proven_segoe_faces() =>
        Assert.Equal(("Segoe UI Semibold", "Segoe UI", true),
            BitmapTicketRenderer.ResolveFaces(new[] { "Segoe UI", "Segoe UI Semibold" }));

    [Fact]
    public void The_logo_ships_inside_the_agent() => Assert.True(BitmapTicketRenderer.LogoAvailable);

    [Fact]
    public void Logo_is_256_dots_centred_and_not_thickened()
    {
        var bytes = BitmapTicketRenderer.Render(Ticket(new TicketLogo(), new TicketText("فالح")), 576, thickenStrokes: true, marginDots: 24);
        var raster = Pieces(bytes).Single();
        var logo = EmbeddedLogo();
        Assert.Equal(256, logo.GetLength(0));
        Assert.Equal(256, logo.GetLength(1));

        var logoTop = FirstInkRow(logo);
        var rasterTop = FirstInkRow(raster);
        var dy = rasterTop - logoTop;
        const int dx = (576 - 256) / 2;
        for (var y = 0; y < 256; y++)
            for (var x = 0; x < 256; x++)
                Assert.True(logo[x, y] == raster[dx + x, dy + y], $"logo dot ({x},{y}) differs on paper");
    }

    [Fact]
    public void A_fitted_label_never_wraps()
    {
        var oneLine = Pieces(BitmapTicketRenderer.Render(Ticket(new TicketText("سطر", Size: TicketTextSize.Large)))).Single();
        var row = Pieces(BitmapTicketRenderer.Render(Ticket(new TicketRow(new[]
        {
            new TicketCell("إيداعات / إضافات من الخزنة", 52, TicketAlign.Start),
            new TicketCell("12,500 ج", 48, TicketAlign.End),
        }, TicketTextSize.Large, FitStart: true)))).Single();
        Assert.True(row.GetLength(1) <= oneLine.GetLength(1) + 2,
            $"a fitted label/value row is {row.GetLength(1)} dots tall; one line is {oneLine.GetLength(1)}");
    }

    /// <summary>Spec anti-goal: a kitchen ticket must not get noticeably longer. Before the redesign
    /// this sample measured 54.8 mm (Charcoal) and 49.4 mm (Grill). The plan's +10% budget proved
    /// unreachable: the owner-chosen Dubai 23 px alone adds 21% (measured 66.1 / 59.8 mm with the old
    /// layout), and the type/time line was merged to claw some back. These limits pin the approved
    /// design (69.6 / 63.3 mm, plus 0.5 mm) so it cannot creep longer; see the ledger ruling.</summary>
    [Theory]
    [InlineData("Charcoal", 561)]
    [InlineData("Grill", 510)]
    public void Kitchen_tickets_do_not_grow_past_the_approved_design(string station, int maxDots)
    {
        var at = new DateTimeOffset(2026, 11, 17, 17, 42, 0, TimeSpan.Zero);
        var items = new[]
        {
            new TicketItem("شاورما فراخ", "جامبو", 2, Falah.Domain.Common.Money.FromEgp(85), Falah.Domain.Common.Money.FromEgp(170), "بدون بصل، زيادة طحينة", null, null, "Grill"),
            new TicketItem("كفتة على الفحم", "نص كيلو", 1, Falah.Domain.Common.Money.FromEgp(245), Falah.Domain.Common.Money.FromEgp(245), null, null, null, "Charcoal"),
            new TicketItem("كبدة إسكندراني", "ربع", 2, Falah.Domain.Common.Money.FromEgp(60), Falah.Domain.Common.Money.FromEgp(120), "حار", "عرض الغدا", Falah.Domain.Common.Money.FromEgp(70), "Charcoal"),
            new TicketItem("بطاطس محمرة", "كبير", 3, Falah.Domain.Common.Money.FromEgp(30), Falah.Domain.Common.Money.FromEgp(90), null, null, null, null),
            new TicketItem("بيبسي", null, 2, Falah.Domain.Common.Money.FromEgp(20), Falah.Domain.Common.Money.FromEgp(40), null, null, null, null),
        };
        var route = Enum.Parse<PrintRoute>(station);
        var ticket = PrintRules.ComposeKitchenTicket(route, "فالح أبو العنبه", "o-1", "#0286", Falah.Domain.Orders.OrderType.Takeaway, items, at);
        var height = Pieces(BitmapTicketRenderer.Render(ticket)).Single().GetLength(1);
        Assert.True(height <= maxDots, $"{station} ticket is {height} dots ({height / 8.0:0.0} mm); limit {maxDots}");
    }

    /// <summary>Final review, Important 3: a single word wider than its cell is broken inside the cell,
    /// as GDI+ did before the tight layout. Without that the word was cut off at the cell edge (GDI
    /// trims a no-wrap line), so part of it never reached the paper; nor may it run into the next column.</summary>
    [Fact]
    public void A_word_wider_than_its_cell_wraps_inside_its_cell_and_is_printed_whole()
    {
        TicketRow Row(string text) => new(new[]
        {
            new TicketCell(text, 50, TicketAlign.Start),
            new TicketCell(" ", 50, TicketAlign.End),
        });
        var oneLine = Pieces(BitmapTicketRenderer.Render(Ticket(Row("WWW")))).Single();
        var ink = Pieces(BitmapTicketRenderer.Render(Ticket(Row("WWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWWW")))).Single();

        // Forty W's are about three times the cell's width: they must take several lines, not one.
        Assert.True(ink.GetLength(1) >= oneLine.GetLength(1) + 2 * 30,
            $"the long word took {ink.GetLength(1)} dots; one line takes {oneLine.GetLength(1)} - it was cut off, not wrapped");
        // And nothing spills into the empty left-hand cell.
        const int leftHalfEnd = 24 + 528 / 2 - 8;
        for (var y = 0; y < ink.GetLength(1); y++)
            for (var x = 0; x < leftHalfEnd; x++)
                Assert.False(ink[x, y], $"ink at ({x},{y}) - the long word overran into the next column");
    }

    internal static ComposedTicket Ticket(params TicketElement[] lines) => new(PrintRoute.Receipt, "t", "#1", lines);

    /// <summary>Each GS v 0 raster in the stream as [x, y] ink.</summary>
    internal static List<bool[,]> Pieces(byte[] bytes)
    {
        var pieces = new List<bool[,]>();
        for (var p = 0; p + 8 < bytes.Length;)
        {
            if (bytes[p] != 0x1D || bytes[p + 1] != 0x76 || bytes[p + 2] != 0x30) { p++; continue; }
            int widthBytes = bytes[p + 4] | bytes[p + 5] << 8, height = bytes[p + 6] | bytes[p + 7] << 8;
            var ink = new bool[widthBytes * 8, height];
            for (var y = 0; y < height; y++)
                for (var x = 0; x < widthBytes * 8; x++)
                    ink[x, y] = (bytes[p + 8 + y * widthBytes + x / 8] & (1 << (7 - x % 8))) != 0;
            pieces.Add(ink);
            p += 8 + widthBytes * height;
        }
        return pieces;
    }

    private static bool[,] EmbeddedLogo()
    {
        using var stream = typeof(BitmapTicketRenderer).Assembly.GetManifestResourceStream("Falah.PrintAgent.Assets.receipt-logo.png")
            ?? throw new InvalidOperationException("receipt-logo.png is not embedded in Falah.PrintAgent");
        using var bmp = new Bitmap(stream);
        var ink = new bool[bmp.Width, bmp.Height];
        for (var y = 0; y < bmp.Height; y++)
            for (var x = 0; x < bmp.Width; x++)
                ink[x, y] = bmp.GetPixel(x, y).GetBrightness() < 0.5f;
        return ink;
    }

    private static int FirstInkRow(bool[,] ink)
    {
        for (var y = 0; y < ink.GetLength(1); y++)
            for (var x = 0; x < ink.GetLength(0); x++)
                if (ink[x, y]) return y;
        return -1;
    }
}
