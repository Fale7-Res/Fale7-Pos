using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Runtime.Versioning;
using Falah.Domain.Printing;
using QRCoder;

namespace Falah.PrintAgent;

/// <summary>
/// Renders a ticket as a bitmap and sends it as an ESC/POS raster image (GS v 0) instead of
/// character bytes - the only reliable way to print Arabic on the physical Xprinter this app
/// targets. Confirmed against that printer's own command manual: its firmware has NO Arabic code
/// page at all (ESC t n only offers n = 0-10, 16-19 - USA/Europe, Greek, Hebrew, Farsi, East-Europe;
/// five different code pages were tried physically against the real hardware and all five printed
/// garbage). A working code page would not even be enough on its own - single-byte ESC/POS text mode
/// prints one isolated glyph per byte with no contextual shaping, and Arabic letters change shape by
/// position in a word. Rendering through Windows' own GDI+ text layout (which does shape Arabic
/// correctly) and printing the result as pixels sidesteps both problems at once.
///
/// Physically established on the real XP-80C, and load-bearing for anyone changing this file:
///
/// Density commands do NOT work here. ESC 7 (heating parameters) and DC2 # (print density) are both
/// unsupported by this firmware, and sending them desynchronises the byte stream so badly that the
/// GS v 0 that follows is never recognised as a command and the entire raster prints as garbage
/// characters. Darkness must therefore be solved in the pixels, not in printer settings - which is
/// what ThickenStrokes below does.
/// </summary>
[SupportedOSPlatform("windows")]
public static class BitmapTicketRenderer
{
    private const int TopMargin = 6;
    private const int BottomMargin = 10;
    private const int LineGap = 3;
    private const int CellGap = 6;
    private const int RuleThickness = 2;
    private const int RuleMargin = 4;

    // Pills (inverted headings), bands (inverted rows) and boxed grid cells.
    private const int PillPadX = 18;
    private const int PillPadY = 5;
    private const int PillRadius = 12;
    private const int BandPadY = 5;
    private const int BoxPadX = 6;
    private const int BoxPadY = 4;
    private const int DashOn = 6;
    private const int DashOff = 4;

    // The Falah signature mark under the brand name: short, centred, two dots thick.
    private const int AccentWidth = 112;

    // Menu QR. 203 dpi is 8 dots per mm, so a 224-dot symbol is 28 mm - inside the 25-30 mm a phone
    // camera reads comfortably from a hand-held receipt. Modules are whole dots, so the symbol is
    // the nearest whole-module fit to that target, never allowed past 30 mm.
    private const int QrTargetDots = 224;
    private const int QrMaxDots = 240;
    private const int QrMinModuleDots = 3;
    // The quiet zone the QR standard requires on every side, in modules.
    private const int QrQuietModules = 4;

    /// <summary>
    /// Paper fed between the last printed dot and the cut.
    ///
    /// The cutter sits physically below the print head on this hardware, so whatever has not
    /// travelled past the blade yet gets cut through. Two line feeds - the original value - was
    /// measurably not enough: the final line of a ticket came off the printer sliced or missing
    /// altogether. At the printer's default line spacing each feed is roughly 3.75 mm, so five
    /// feeds clear approximately 19 mm, comfortably past the gap with white space to spare.
    ///
    /// Deliberately plain 0x0A bytes rather than a feed-and-cut command such as GS V 66: line feed
    /// and GS V 0 are both proven on this firmware, while unsupported commands on this printer do
    /// not fail cleanly - they desynchronise the byte stream and turn the whole raster into garbage
    /// characters (see this class's doc comment).
    /// </summary>
    private static readonly byte[] FeedBeforeCut = { 0x0A, 0x0A, 0x0A, 0x0A, 0x0A };

    /// <param name="paperWidthDots">The raster width. 576 on this restaurant's printer - measured on
    /// paper, not assumed: an 80mm head at 203 dpi prints 72.1mm, and a width probe at 384, 512 and 576
    /// dots showed both edge marks intact at 576. 384 is the 58mm-printer figure and left a third of
    /// the paper blank.</param>
    /// <param name="marginDots">Kept clear on each side. Content never touches the edge of the raster,
    /// so the head's own edge tolerance can never shave a glyph.</param>
    public static byte[] Render(ComposedTicket ticket, int paperWidthDots = 576, bool thickenStrokes = true, int marginDots = 24)
    {
        using var stream = new MemoryStream();
        stream.Write(new byte[] { 0x1B, 0x40 }); // ESC @ - initialize, once for the whole stream

        // A TicketCut splits the ticket into physically separate pieces of paper. Each segment is
        // laid out and rastered on its own, so a cut is a real boundary rather than a gap.
        foreach (var segment in SplitOnCuts(ticket.Lines))
        {
            var inverted = new List<Rectangle>();
            var codes = new List<QrPlacement>();
            using var bmp = RenderSegment(segment, paperWidthDots, marginDots, inverted, codes);
            WriteRaster(stream, bmp, thickenStrokes, inverted, codes);
            stream.Write(FeedBeforeCut);
            stream.Write(new byte[] { 0x1D, 0x56, 0x00 }); // GS V 0 - full cut
        }

        return stream.ToArray();
    }

    /// <summary>One list of elements per physical ticket. A trailing cut does not produce an empty
    /// final ticket, and a ticket with no cuts at all yields exactly one segment - so the common
    /// case is unchanged by this existing at all.</summary>
    private static List<List<TicketElement>> SplitOnCuts(IReadOnlyList<TicketElement> elements)
    {
        var segments = new List<List<TicketElement>>();
        var current = new List<TicketElement>();
        foreach (var element in elements)
        {
            if (element is TicketCut)
            {
                if (current.Count > 0) segments.Add(current);
                current = new List<TicketElement>();
                continue;
            }
            current.Add(element);
        }
        if (current.Count > 0) segments.Add(current);
        if (segments.Count == 0) segments.Add(new List<TicketElement>());
        return segments;
    }

    private static float PixelSize(TicketTextSize size) => size switch
    {
        TicketTextSize.Display => 88f,
        TicketTextSize.Huge => 64f,
        TicketTextSize.Title => 40f,
        TicketTextSize.Large => 26f,
        _ => 18f,
    };

    /// <summary>Resolved once. "Segoe UI Semibold" is a separate FAMILY on Windows, not a style of
    /// Segoe UI, and asking GDI+ for a family that is not installed does not throw - it silently
    /// substitutes some default face, which on an Arabic ticket means losing the contextual shaping
    /// that is the entire reason this renderer draws text as pixels. So it is looked up, and falls
    /// back to plain Segoe UI (rendered Bold) if it is ever absent.</summary>
    private static readonly string? SemiBoldFamily = ResolveSemiBoldFamily();

    private static string? ResolveSemiBoldFamily()
    {
        try
        {
            using var installed = new InstalledFontCollection();
            return installed.Families.Any(f => f.Name == "Segoe UI Semibold") ? "Segoe UI Semibold" : null;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// The font for one piece of ticket text.
    ///
    /// Text that is not bold is rendered in Semibold rather than Regular. On the real printer
    /// regular-weight text was faint and patchy even with stroke thickening, while bold text beside
    /// it printed solid - so "not bold" here means Semibold, never Regular, at every size.
    /// </summary>
    private static Font CreateFont(TicketTextSize size, bool bold)
    {
        var px = PixelSize(size);
        if (bold) return new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel);
        return SemiBoldFamily is { } semiBold
            ? new Font(semiBold, px, FontStyle.Regular, GraphicsUnit.Pixel)
            : new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel);
    }

    private static StringAlignment ToAlignment(TicketAlign align) => align switch
    {
        TicketAlign.Center => StringAlignment.Center,
        TicketAlign.End => StringAlignment.Far,
        _ => StringAlignment.Near,
    };

    private sealed record Measured(TicketElement Element, float Height, Font? Font, List<Font>? CellFonts, float ContentWidth,
        QrSymbol? Qr = null);

    /// <summary>A QR's module matrix (dark = true, WITHOUT quiet zone) and the whole-dot size of one
    /// module.</summary>
    private sealed record QrSymbol(bool[,] Modules, int ModuleDots)
    {
        public int Size => Modules.GetLength(0);
        /// <summary>Side of the whole square including the quiet zone, in dots.</summary>
        public int TotalDots => (Size + 2 * QrQuietModules) * ModuleDots;
    }

    /// <summary>Where a QR landed on the segment: the square including its quiet zone.</summary>
    private sealed record QrPlacement(QrSymbol Symbol, int Left, int Top);

    /// <summary>
    /// The QR module matrix for a payload, sized to whole dots, or null if it cannot be encoded
    /// (empty, or too long to fit the paper at a readable module size).
    ///
    /// Error correction Q (about 25% of the symbol recoverable) rather than the usual M: this
    /// printer has shown horizontal banding, and a menu link is short enough that the extra
    /// redundancy costs almost nothing in module size.
    /// </summary>
    private static QrSymbol? EncodeQr(string payload, int contentWidth)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        bool[,] modules;
        try
        {
            using var data = QRCodeGenerator.GenerateQrCode(payload.Trim(), QRCodeGenerator.ECCLevel.Q);
            var size = 17 + 4 * data.Version;
            // QRCoder's matrix already carries its own quiet zone; strip it so the zone below is
            // the only one and its width is exactly what this renderer says it is.
            var offset = (data.ModuleMatrix.Count - size) / 2;
            modules = new bool[size, size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                    modules[x, y] = data.ModuleMatrix[y + offset][x + offset];
        }
        catch (Exception)
        {
            return null;
        }

        var n = modules.GetLength(0);
        var moduleDots = Math.Max(QrMinModuleDots, (int)Math.Round(QrTargetDots / (double)n));
        while (moduleDots > QrMinModuleDots && (moduleDots * n > QrMaxDots || (n + 2 * QrQuietModules) * moduleDots > contentWidth))
            moduleDots--;
        if ((n + 2 * QrQuietModules) * moduleDots > contentWidth) return null;
        return new QrSymbol(modules, moduleDots);
    }

    private static string EmptyToSpace(string text) => string.IsNullOrEmpty(text) ? " " : text;

    /// <summary>Horizontal room a cell's text loses to its surroundings: the inner padding on both
    /// sides of a boxed cell, or the plain gap between unboxed columns.</summary>
    private static int CellInset(TicketRow row) => row.Boxed ? 2 * BoxPadX : CellGap;

    private static int RowPadY(TicketRow row) => row.Boxed ? BoxPadY : row.Inverted ? BandPadY : 0;

    /// <summary>
    /// Lays one physical ticket out on a canvas the full width of the paper, with everything drawn
    /// inside the content box between the two safe margins. Nothing is ever drawn in the margins,
    /// so the printer's own edge tolerance can never shave a glyph.
    /// </summary>
    /// <param name="inverted">Receives every white-on-black region drawn, so the raster pass can
    /// treat those areas differently when thickening - see Thicken.</param>
    private static Bitmap RenderSegment(IReadOnlyList<TicketElement> elements, int paperWidthDots, int marginDots,
        List<Rectangle> inverted, List<QrPlacement> codes)
    {
        var left = marginDots;
        var contentWidth = Math.Max(1, paperWidthDots - 2 * marginDots);

        using var measureBmp = new Bitmap(1, 1);
        using var measureG = Graphics.FromImage(measureBmp);
        // Not AntiAlias: a thermal head only has black/white dots, and anti-aliased edge pixels
        // (mid-gray) mostly fall on the "don't print" side of any reasonable threshold, thinning
        // small/regular-weight text out until it barely registers. Single-bit hinting keeps every
        // stroke a full, solid dot instead.
        measureG.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        var measured = new List<Measured>();
        float totalHeight = TopMargin;
        foreach (var original in elements)
        {
            var element = original;
            QrSymbol? qr = null;
            if (element is TicketQr code)
            {
                qr = EncodeQr(code.Payload, contentWidth);
                // A link that cannot be drawn as a code is still printed - as the link itself.
                if (qr is null) element = new TicketText(code.Payload, Align: TicketAlign.Center);
            }

            switch (element)
            {
                case TicketQr:
                    measured.Add(new Measured(element, qr!.TotalDots, null, null, 0, qr));
                    totalHeight += qr.TotalDots + LineGap;
                    break;

                case TicketText text:
                {
                    // White text on black is always bold: heat from the surrounding black dots
                    // bleeds into thin white strokes and closes them up.
                    var font = CreateFont(text.Size, text.Bold || text.Inverted);
                    var available = text.Inverted ? contentWidth - 2 * PillPadX : contentWidth;
                    var size = measureG.MeasureString(EmptyToSpace(text.Text), font, available);
                    var height = size.Height + (text.Inverted ? 2 * PillPadY : 0);
                    measured.Add(new Measured(element, height, font, null, size.Width));
                    totalHeight += height + LineGap;
                    break;
                }
                case TicketRow row:
                {
                    var fonts = new List<Font>();
                    var textHeight = 0f;
                    foreach (var cell in row.Cells)
                    {
                        var font = CreateFont(row.Size, cell.Bold || row.Inverted);
                        fonts.Add(font);
                        var width = Math.Max(1, CellWidth(cell, contentWidth) - CellInset(row));
                        var size = measureG.MeasureString(EmptyToSpace(cell.Text), font, width);
                        // The tallest cell sets the row height, so a long wrapped item name cannot
                        // overlap the row beneath it.
                        textHeight = Math.Max(textHeight, size.Height);
                    }
                    var height = textHeight + 2 * RowPadY(row);
                    measured.Add(new Measured(element, height, null, fonts, 0));
                    totalHeight += height + LineGap;
                    break;
                }
                case TicketRule rule:
                {
                    var height = rule.Style == TicketRuleStyle.Blank ? RuleMargin * 2 : RuleThickness + RuleMargin * 2;
                    measured.Add(new Measured(element, height, null, null, 0));
                    totalHeight += height + LineGap;
                    break;
                }
            }
        }
        totalHeight += BottomMargin;

        var bmp = new Bitmap(paperWidthDots, Math.Max(1, (int)Math.Ceiling(totalHeight)));
        using (var g = Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            g.Clear(Color.White);

            float y = TopMargin;
            for (var index = 0; index < measured.Count; index++)
            {
                var item = measured[index];
                switch (item.Element)
                {
                    case TicketText { Inverted: true } text:
                        DrawPill(g, text, item, y, left, contentWidth, inverted);
                        break;

                    case TicketText text:
                    {
                        // RTL is the paragraph direction these tickets use throughout; centered lines
                        // still need it so an Arabic line centers around its own reading order.
                        using var format = new StringFormat(StringFormatFlags.DirectionRightToLeft)
                        {
                            Alignment = ToAlignment(text.Align),
                        };
                        g.DrawString(text.Text, item.Font!, Brushes.Black,
                            new RectangleF(left, y, contentWidth, item.Height), format);
                        break;
                    }

                    case TicketRow row:
                    {
                        var next = index + 1 < measured.Count ? measured[index + 1].Element as TicketRow : null;
                        DrawRow(g, row, item, next, y, left, contentWidth, inverted);
                        break;
                    }

                    case TicketRule { Style: TicketRuleStyle.Dashed }:
                        for (var dx = 0; dx < contentWidth; dx += DashOn + DashOff)
                            g.FillRectangle(Brushes.Black, left + dx, y + RuleMargin, Math.Min(DashOn, contentWidth - dx), 1);
                        break;

                    case TicketRule { Style: TicketRuleStyle.Thin }:
                        g.FillRectangle(Brushes.Black, left, y + RuleMargin, contentWidth, 1);
                        break;

                    case TicketRule { Style: TicketRuleStyle.Accent }:
                        g.FillRectangle(Brushes.Black, left + (contentWidth - AccentWidth) / 2, y + RuleMargin, AccentWidth, RuleThickness);
                        break;

                    // Not drawn here: GDI+ would be free to antialias or shift a module edge. The
                    // square is only reserved; WriteRaster stamps the exact modules into the bits.
                    case TicketQr:
                    {
                        var symbol = item.Qr!;
                        codes.Add(new QrPlacement(symbol, left + (contentWidth - symbol.TotalDots) / 2, (int)Math.Round(y)));
                        break;
                    }

                    case TicketRule rule when rule.Style != TicketRuleStyle.Blank:
                    {
                        var thickness = rule.Style == TicketRuleStyle.Heavy ? RuleThickness * 2 : RuleThickness;
                        g.FillRectangle(Brushes.Black, left, y + RuleMargin, contentWidth, thickness);
                        break;
                    }
                }
                y += item.Height + LineGap;
            }
        }

        foreach (var item in measured)
        {
            item.Font?.Dispose();
            if (item.CellFonts is { } fonts) foreach (var f in fonts) f.Dispose();
        }
        return bmp;
    }

    /// <summary>A heading printed white inside a rounded black pill, centred in the content box and
    /// sized to its text. Always centred: a pill labels what follows it rather than being a line of
    /// content.</summary>
    private static void DrawPill(Graphics g, TicketText text, Measured item, float y, int left, int contentWidth, List<Rectangle> inverted)
    {
        var pillWidth = Math.Min(contentWidth, item.ContentWidth + 2 * PillPadX);
        var pill = new RectangleF(left + (contentWidth - pillWidth) / 2f, y, pillWidth, item.Height);
        using (var path = RoundedRectangle(pill, PillRadius)) g.FillPath(Brushes.Black, path);
        using var format = new StringFormat(StringFormatFlags.DirectionRightToLeft)
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center,
        };
        g.DrawString(text.Text, item.Font!, Brushes.White, pill, format);
        inverted.Add(Rectangle.Round(pill));
    }

    /// <summary>
    /// One table or total row. Plain rows draw text only. Inverted rows get a black band across the
    /// content box with white text; boxed rows join a ruled grid.
    ///
    /// Grid lines are shared rather than doubled: a boxed row draws its top edge and its sides, and
    /// only the LAST boxed row of a run draws the bottom edge. When the grid continues, the sides and
    /// band extend through the line gap so the next row meets them with no seam. A Continuation row
    /// (an item's note) suppresses the edge between it and the row above, which is what keeps a note
    /// visibly inside its own item.
    /// </summary>
    private static void DrawRow(Graphics g, TicketRow row, Measured item, TicketRow? next, float y,
        int left, int contentWidth, List<Rectangle> inverted)
    {
        var gridContinues = row.Boxed && next is { Boxed: true };
        var spanBelow = gridContinues ? LineGap : 0;

        if (row.Inverted)
        {
            var band = new RectangleF(left, y, contentWidth, item.Height + spanBelow);
            g.FillRectangle(Brushes.Black, band);
            inverted.Add(Rectangle.Round(band));
        }

        var padY = RowPadY(row);
        var ink = row.Inverted ? Brushes.White : Brushes.Black;
        var framed = row.Boxed || row.Inverted;
        var dividers = new List<float>();

        // Cells are laid out right to left: the first cell is the right-most one, because that is
        // where an Arabic table starts.
        float x = left + contentWidth;
        for (var i = 0; i < row.Cells.Count; i++)
        {
            var cell = row.Cells[i];
            var width = CellWidth(cell, contentWidth);
            x -= width;
            var rect = row.Boxed
                ? new RectangleF(x + BoxPadX, y + padY, width - 2 * BoxPadX, item.Height - 2 * padY)
                : new RectangleF(x, y + padY, width - CellGap, item.Height - 2 * padY);
            using var format = new StringFormat(StringFormatFlags.DirectionRightToLeft)
            {
                Alignment = ToAlignment(cell.Align),
                // Only framed rows centre vertically; plain rows keep top alignment.
                LineAlignment = framed ? StringAlignment.Center : StringAlignment.Near,
            };
            g.DrawString(cell.Text, item.CellFonts![i], ink, rect, format);
            if (i < row.Cells.Count - 1) dividers.Add(x);
        }

        if (!row.Boxed) return;

        var right = left + contentWidth - 1;
        var bottom = y + item.Height + spanBelow;
        using var divider = new Pen(row.Inverted ? Color.White : Color.Black, 1);
        using var edge = new Pen(Color.Black, 1);

        if (!row.Continuation) g.DrawLine(edge, left, y, right, y);
        g.DrawLine(edge, left, y, left, bottom);
        g.DrawLine(edge, right, y, right, bottom);
        if (!gridContinues) g.DrawLine(edge, left, bottom, right, bottom);

        // An item followed by its own note stops its dividers at the item, so they do not dangle
        // into the note row, which has none of its own.
        var dividerBottom = next is { Boxed: true, Continuation: false } ? bottom : y + item.Height;
        foreach (var d in dividers) g.DrawLine(divider, d, y, d, dividerBottom);
    }

    private static GraphicsPath RoundedRectangle(RectangleF r, float radius)
    {
        var d = Math.Min(radius * 2, Math.Min(r.Width, r.Height));
        var path = new GraphicsPath();
        path.AddArc(r.X, r.Y, d, d, 180, 90);
        path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
        path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
        path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    /// <summary>A cell's share of the CONTENT box - never of the full paper - so a row's cells add
    /// up to exactly the width between the two safe margins.</summary>
    private static int CellWidth(TicketCell cell, int contentWidth) =>
        Math.Max(1, (int)Math.Round(contentWidth * (cell.WidthPercent / 100.0)));

    /// <summary>Packs the bitmap into ESC/POS's GS v 0 raster format (1 bit per pixel, MSB first,
    /// rows padded to a whole byte).</summary>
    private static void WriteRaster(Stream stream, Bitmap bmp, bool thickenStrokes, IReadOnlyList<Rectangle> inverted,
        IReadOnlyList<QrPlacement> codes)
    {
        var width = bmp.Width;
        var height = bmp.Height;

        var ink = new bool[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var pixel = bmp.GetPixel(x, y);
                ink[x, y] = (pixel.R + pixel.G + pixel.B) / 3 < 128;
            }

        if (thickenStrokes) ink = Thicken(ink, width, height, ProtectedMask(inverted, width, height));

        // QR codes go in AFTER thickening, directly as bits. Dilation would grow every dark module
        // by a dot on each side and close up the light ones - fine for a letter, fatal for a code
        // whose meaning is the exact ratio of dark to light. Stamping the whole square, quiet zone
        // included, also wipes any thickened text that crept into the zone from a neighbouring line.
        foreach (var code in codes) StampQr(ink, width, height, code);

        var widthBytes = (width + 7) / 8;
        var imageBytes = new byte[widthBytes * height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (!ink[x, y]) continue;
                imageBytes[y * widthBytes + x / 8] |= (byte)(1 << (7 - x % 8));
            }

        stream.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00 }); // GS v 0 m=0
        stream.Write(new byte[] { (byte)(widthBytes & 0xFF), (byte)(widthBytes >> 8 & 0xFF) });
        stream.Write(new byte[] { (byte)(height & 0xFF), (byte)(height >> 8 & 0xFF) });
        stream.Write(imageBytes);
    }

    private static void StampQr(bool[,] ink, int width, int height, QrPlacement code)
    {
        var symbol = code.Symbol;
        var quiet = QrQuietModules * symbol.ModuleDots;
        for (var dy = 0; dy < symbol.TotalDots; dy++)
            for (var dx = 0; dx < symbol.TotalDots; dx++)
            {
                int x = code.Left + dx, y = code.Top + dy;
                if (x < 0 || x >= width || y < 0 || y >= height) continue;
                int mx = (dx - quiet) / symbol.ModuleDots, my = (dy - quiet) / symbol.ModuleDots;
                var inSymbol = dx >= quiet && dy >= quiet && mx < symbol.Size && my < symbol.Size;
                ink[x, y] = inSymbol && symbol.Modules[mx, my];
            }
    }

    private static bool[,] ProtectedMask(IReadOnlyList<Rectangle> regions, int width, int height)
    {
        var mask = new bool[width, height];
        foreach (var r in regions)
            for (var y = Math.Max(0, r.Top); y < Math.Min(height, r.Bottom); y++)
                for (var x = Math.Max(0, r.Left); x < Math.Min(width, r.Right); x++)
                    mask[x, y] = true;
        return mask;
    }

    /// <summary>
    /// Grows every inked dot by one, so a stroke that was one dot wide becomes three.
    ///
    /// This exists because of a physical finding, not a theory: on the real XP-80C the bold, large
    /// title of a ticket prints solid black while regular-weight 18px text prints faint and broken.
    /// A thermal head drives an isolated dot with less effective energy than a dot with lit
    /// neighbours, so thin single-dot stems never fully develop. The printer's own density commands
    /// cannot be used to compensate (see this class's doc comment - they corrupt the raster), so the
    /// compensation happens here, in the pixels.
    ///
    /// 4-connected rather than 8-connected on purpose: it thickens horizontal and vertical stems
    /// without also filling diagonals, which is what would turn the counters of small Arabic letters
    /// into blobs.
    ///
    /// Inside a white-on-black region (a pill, a banded row) the foreground is WHITE, so the same
    /// rule is applied the other way round: white grows by one dot instead of black. Only neighbours
    /// inside the same region count, so the band's own outer edge does not move.
    ///
    /// Dimensions are deliberately untouched - the result is written into a second array of exactly
    /// the same width and height, and every neighbour read is bounds-checked. The raster header,
    /// line positions and both margins therefore cannot move, and ink cannot escape the canvas.
    /// </summary>
    private static bool[,] Thicken(bool[,] source, int width, int height, bool[,] isProtected)
    {
        bool WhiteInside(int nx, int ny) => isProtected[nx, ny] && !source[nx, ny];

        var grown = new bool[width, height];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                if (isProtected[x, y])
                {
                    var white = !source[x, y]
                        || (x > 0 && WhiteInside(x - 1, y)) || (x < width - 1 && WhiteInside(x + 1, y))
                        || (y > 0 && WhiteInside(x, y - 1)) || (y < height - 1 && WhiteInside(x, y + 1));
                    grown[x, y] = !white;
                    continue;
                }
                if (source[x, y]
                    || (x > 0 && source[x - 1, y]) || (x < width - 1 && source[x + 1, y])
                    || (y > 0 && source[x, y - 1]) || (y < height - 1 && source[x, y + 1]))
                    grown[x, y] = true;
            }
        return grown;
    }
}
