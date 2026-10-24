# src/Falah.PrintAgent/BitmapTicketRenderer.cs

Arabic GDI+ raster and QR rendering.

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Status **STATIC_FILE_REVIEWED**.

Git blob SHA-256 `2a392f1ff18c29ccaa257f7847ed0feefaa9324f130676385a136e6a1c7136dc`. Working SHA-256 `5b5699ec13aeab84cbcb93079b027f63019eeeb60430d37ea02c940c4a17656a`. Exact bytes equal: False; LF-normalized equal: True.

Reviewed lines: 1-607.

Symbols indexed (index is not a claim every method was reviewed): BitmapTicketRenderer@30, Measured@177, QrSymbol@182, QrPlacement@190.

Callable signature aid inside reviewed ranges (regex aid only; source ranges above are authoritative):

- Line 83: `public static byte[] Render(ComposedTicket ticket, int paperWidthDots = 576, bool thickenStrokes = true, int marginDots = 24)`
- Line 106: `private static List<List<TicketElement>> SplitOnCuts(IReadOnlyList<TicketElement> elements)`
- Line 125: `private static float PixelSize(TicketTextSize size) => size switch`
- Line 141: `private static string? ResolveSemiBoldFamily()`
- Line 161: `private static Font CreateFont(TicketTextSize size, bool bold)`
- Line 170: `private static StringAlignment ToAlignment(TicketAlign align) => align switch`
- Line 177: `private sealed record Measured(TicketElement Element, float Height, Font? Font, List<Font>? CellFonts, float ContentWidth,`
- Line 182: `private sealed record QrSymbol(bool[,] Modules, int ModuleDots)`
- Line 190: `private sealed record QrPlacement(QrSymbol Symbol, int Left, int Top);`
- Line 200: `private static QrSymbol? EncodeQr(string payload, int contentWidth)`
- Line 229: `private static string EmptyToSpace(string text) => string.IsNullOrEmpty(text) ? " " : text;`
- Line 233: `private static int CellInset(TicketRow row) => row.Boxed ? 2 * BoxPadX : CellGap;`
- Line 235: `private static int RowPadY(TicketRow row) => row.Boxed ? BoxPadY : row.Inverted ? BandPadY : 0;`
- Line 244: `private static Bitmap RenderSegment(IReadOnlyList<TicketElement> elements, int paperWidthDots, int marginDots,`
- Line 400: `private static void DrawPill(Graphics g, TicketText text, Measured item, float y, int left, int contentWidth, List<Rectangle> inverted)`
- Line 424: `private static void DrawRow(Graphics g, TicketRow row, Measured item, TicketRow? next, float y,`
- Line 481: `private static GraphicsPath RoundedRectangle(RectangleF r, float radius)`
- Line 495: `private static int CellWidth(TicketCell cell, int contentWidth) =>`
- Line 500: `private static void WriteRaster(Stream stream, Bitmap bmp, bool thickenStrokes, IReadOnlyList<Rectangle> inverted,`
- Line 537: `private static void StampQr(bool[,] ink, int width, int height, QrPlacement code)`
- Line 552: `private static bool[,] ProtectedMask(IReadOnlyList<Rectangle> regions, int width, int height)`
- Line 584: `private static bool[,] Thicken(bool[,] source, int width, int height, bool[,] isProtected)`

Render splits on TicketCut, renders each segment, emits GS v 0 raster plus five feeds and full cut. RTL Segoe fonts, measured wrapping, boxed rows and inverse-region thickening are explicit. QR modules are stamped after thickening with a four-module quiet zone. The preview must decode these bytes rather than redraw ticket data. Width/margins and total height are not bounded: large input costs several width*height bool arrays plus slow GetPixel loops; GS raster height is serialized in 16 bits. Narrow negative text widths can fail. Fonts are disposed on normal completion, not all exception paths. Recommend bounded layout and long-ticket chunking, exception-safe font ownership, tests for long Arabic notes and mixed numerals. Existing physical claims in comments were not independently reproduced.

Verification: static inspection only; no live DB/API mutation, original tool execution, printer submission, runtime/UI interaction or source modification. Only report-generation scripts under artifacts were executed. Contract-only review does not certify downstream handlers.

Remaining: Runtime behavior, device behavior and adversarial concurrency remain unverified; see recommendations above and specialist reports.
