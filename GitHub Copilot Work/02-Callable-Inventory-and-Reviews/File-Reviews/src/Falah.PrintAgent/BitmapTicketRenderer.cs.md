# Substantive Callable Audit: `src/Falah.PrintAgent/BitmapTicketRenderer.cs`

- **Source File:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `5b5699ec13aeab84cbcb93079b027f63019eeeb60430d37ea02c940c4a17656a`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **21**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`BitmapTicketRenderer`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/BitmapTicketRenderer.cs) is the mission-critical graphical rasterization engine for Fale7 POS thermal printing. It converts high-level domain [`ComposedTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/ComposedTicket.cs) models into raw ESC/POS monochrome raster graphics byte streams (`GS v 0`) targeted at 80mm thermal receipt printers (specifically the Xprinter XP-D200N / XP-80C class hardware).

### The Root Imperative: Resolution of Hardware Arabic Incompatibility (`ESC-001`)
1. **The Firmware Limitation:** The physical thermal printers utilized in the restaurant have ROM firmwares strictly configured for single-byte Western, Cyrillic, and East Asian character matrices. As verified against the XP-80C command manual, the printer firmware contains **zero Arabic character tables** in its ROM (`ESC t n` only accepts $n \in \{0\dots10, 16\dots19\}$; sending any of these results in unintelligible garbage characters).
2. **The Linguistic Cursive Barrier:** Even if a firmware provided a static 8-bit Arabic codepage (such as CP1256 or CP864), single-byte ESC/POS text mode prints isolated, fixed-pitch glyphs from left to right. Arabic is a cursive, right-to-left script where characters change morphology depending on adjacent characters (Isolated, Initial, Medial, Final). Hardware text mode cannot perform OpenType ligature shaping or BiDi reordering.
3. **The Raster Solution:** [`BitmapTicketRenderer`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/BitmapTicketRenderer.cs) bypasses printer firmware limitations entirely by rendering the ticket canvas in Windows memory via GDI+ (`System.Drawing`), utilizing Windows' native OpenType Arabic shaping and right-to-left BiDi layout engine (`StringFormatFlags.DirectionRightToLeft`). The rendered GDI+ canvas is converted into a 1-bit-per-pixel monochrome bit matrix and emitted using standard ESC/POS raster command `GS v 0`.
4. **Physical Density & Thermal Dynamics:** Budget thermal print heads lack dynamic heating regulation. ESC/POS density commands (`ESC 7`, `DC2 #`) are unsupported by the XP-80C firmware and corrupt the byte stream. Thin 1-pixel stems printed on white paper fade due to insufficient thermal dissipation. `BitmapTicketRenderer` solves this directly in software via 4-connected pixel dilation (`Thicken`), increasing stem darkness without filling the counter loops of Arabic characters.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `BitmapTicketRenderer.Render(ComposedTicket, int, bool, int)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:83:5:MethodDeclaration`
- **Exact Line Range:** Lines `83–101`
- **Signature:** `public static byte[] Render(ComposedTicket ticket, int paperWidthDots = 576, bool thickenStrokes = true, int marginDots = 24)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Coordinates the overall raster rendering pipeline for a complete ticket:
- `ticket` (`ComposedTicket`): The high-level ticket model containing route, order identity, and element lines.
- `paperWidthDots` (`int`, default `576`): The raster width in dots. 576 dots corresponds to an 80mm thermal head printing at 203 DPI (8 dots/mm across 72mm printable width).
- `thickenStrokes` (`bool`, default `true`): Toggles 4-connected morphological dilation to prevent thermal stroke fading.
- `marginDots` (`int`, default `24`): Lateral margin reserved on both edges to ensure text never clips on physical head edge tolerances.
- **Return Value:** `byte[]` containing the complete ESC/POS byte stream, beginning with `ESC @` (`0x1B, 0x40`) and containing one or more raster segments, clearance feeds (`FeedBeforeCut`), and full knife cuts (`GS V 0`).

**B. Callers, Dependencies & Execution Path:**  
Called by [`PrintJobProcessor.ProcessAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L102) when running on Windows with `_renderAsImage = true`.

**C. Spooler Interaction, Raster Mechanics & Hardware Invariants:**  
- **Multi-Segment Cut Handling:** Employs `SplitOnCuts` to break the element list by `TicketCut`. For cashier receipts (`PrintRoute.Receipt`), this renders the customer token and cashier prep ticket as two distinct raster bitmaps, separated by an ESC/POS cut sequence (`0x1D, 0x56, 0x00`), within a single contiguous byte stream.
- **Mechanical Blade Clearance:** Emits `FeedBeforeCut` (5 linefeeds = ~18.75mm) before each cut to ensure the bottom line of the bitmap has physically advanced past the guillotine blade before the knife fires.

**D. Concurrency & Memory Boundaries:**  
Thread-safe static method. All bitmaps and memory streams are allocated per call and disposed deterministically.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `BitmapTicketRendererTests.cs`: `Render_produces_valid_ESC_POS_raster_stream` and `Render_handles_dual_ticket_cuts`.
- *Missing Tests:* Memory allocation verification under high ticket volume (e.g., rendering 500 orders consecutively).

> **Verbatim Code Excerpt (Lines 83-101):**
```csharp
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
```

---

<a id="callable-2"></a>
### Callable 2: `BitmapTicketRenderer.SplitOnCuts(IReadOnlyList<TicketElement>)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:106:5:MethodDeclaration`
- **Exact Line Range:** Lines `106–123`
- **Signature:** `private static List<List<TicketElement>> SplitOnCuts(IReadOnlyList<TicketElement> elements)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Splits an ordered list of `TicketElement` items into sub-lists partitioned by `TicketCut` elements.
- Ignores trailing cuts without creating empty trailing segments.
- Guaranteed to return at least one segment (even if empty) to prevent empty stream generation.
- Returns `List<List<TicketElement>>`.

**B. Callers, Dependencies & Execution Path:**  
Called by `Render` (line 90). Pure partitioning function.

> **Verbatim Code Excerpt (Lines 106-123):**
```csharp
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
```

---

<a id="callable-3"></a>
### Callable 3: `BitmapTicketRenderer.PixelSize(TicketTextSize)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:125:5:MethodDeclaration`
- **Exact Line Range:** Lines `125–132`
- **Signature:** `private static float PixelSize(TicketTextSize size)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Maps the abstract domain `TicketTextSize` enum to exact pixel font heights:
- `Display` $\rightarrow$ `88f` (Order numbers on customer pickup tokens).
- `Huge` $\rightarrow$ `64f` (Grand totals and order numbers on prep receipts).
- `Title` $\rightarrow$ `40f` (Brand titles and reprint banners).
- `Large` $\rightarrow$ `26f` (Headers and money lines).
- Default (`Normal`) $\rightarrow$ `18f` (Body text, table items, notes).

> **Verbatim Code Excerpt (Lines 125-132):**
```csharp
    private static float PixelSize(TicketTextSize size) => size switch
    {
        TicketTextSize.Display => 88f,
        TicketTextSize.Huge => 64f,
        TicketTextSize.Title => 40f,
        TicketTextSize.Large => 26f,
        _ => 18f,
    };
```

---

<a id="callable-4"></a>
### Callable 4: `BitmapTicketRenderer.ResolveSemiBoldFamily()`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:141:5:MethodDeclaration`
- **Exact Line Range:** Lines `141–152`
- **Signature:** `private static string? ResolveSemiBoldFamily()`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Inspects `InstalledFontCollection` to determine if `"Segoe UI Semibold"` is installed as an independent font family on the Windows host. Returns `"Segoe UI Semibold"` if present; otherwise `null`.

**B. Callers, Dependencies & Execution Path:**  
Initializes static field `SemiBoldFamily` (line 139).

**C. GDI+ Typography & Arabic Shaping Invariants:**  
On Windows, asking GDI+ for an uninstalled font family silently substitutes a default font, which breaks Arabic contextual shaping. Checking font family availability avoids font fallback corruption.

> **Verbatim Code Excerpt (Lines 141-152):**
```csharp
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
```

---

<a id="callable-5"></a>
### Callable 5: Lambda `f => f.Name == "Segoe UI Semibold"`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:146:43:SimpleLambdaExpression`
- **Exact Line Range:** Lines `146–146`
- **Signature:** `f => f.Name == "Segoe UI Semibold"`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Predicate for LINQ `.Any()` checking if any installed font family matches `"Segoe UI Semibold"`.

> **Verbatim Code Excerpt (Line 146):**
```csharp
    return installed.Families.Any(f => f.Name == "Segoe UI Semibold") ? "Segoe UI Semibold" : null;
```

---

<a id="callable-6"></a>
### Callable 6: `BitmapTicketRenderer.CreateFont(TicketTextSize, bool)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:161:5:MethodDeclaration`
- **Exact Line Range:** Lines `161–168`
- **Signature:** `private static Font CreateFont(TicketTextSize size, bool bold)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Creates a `Font` instance in pixel units (`GraphicsUnit.Pixel`).
- If `bold` is true: returns `new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel)`.
- If `bold` is false: returns `Segoe UI Semibold` if available, or falls back to `Segoe UI Bold`.
- **Operational Invariant:** Standard "Regular" weight Segoe UI is deliberately omitted because thin regular stems print faint and patchy on thermal paper. "Not bold" maps to Semibold or Bold.

> **Verbatim Code Excerpt (Lines 161-168):**
```csharp
    private static Font CreateFont(TicketTextSize size, bool bold)
    {
        var px = PixelSize(size);
        if (bold) return new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel);
        return SemiBoldFamily is { } semiBold
            ? new Font(semiBold, px, FontStyle.Regular, GraphicsUnit.Pixel)
            : new Font("Segoe UI", px, FontStyle.Bold, GraphicsUnit.Pixel);
    }
```

---

<a id="callable-7"></a>
### Callable 7: `BitmapTicketRenderer.ToAlignment(TicketAlign)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:170:5:MethodDeclaration`
- **Exact Line Range:** Lines `170–175`
- **Signature:** `private static StringAlignment ToAlignment(TicketAlign align)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Maps domain `TicketAlign` to GDI+ `StringAlignment`:
- `Center` $\rightarrow$ `StringAlignment.Center`
- `End` $\rightarrow$ `StringAlignment.Far`
- Default / `Start` $\rightarrow$ `StringAlignment.Near`

> **Verbatim Code Excerpt (Lines 170-175):**
```csharp
    private static StringAlignment ToAlignment(TicketAlign align) => align switch
    {
        TicketAlign.Center => StringAlignment.Center,
        TicketAlign.End => StringAlignment.Far,
        _ => StringAlignment.Near,
    };
```

---

<a id="callable-8"></a>
### Callable 8: `BitmapTicketRenderer.EncodeQr(string, int)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:200:5:MethodDeclaration`
- **Exact Line Range:** Lines `200–227`
- **Signature:** `private static QrSymbol? EncodeQr(string payload, int contentWidth)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Generates a QR code symbol matrix using `QRCoder.QRCodeGenerator`:
- Uses Error Correction Level `Q` (~25% data recovery) to withstand horizontal thermal head print dropouts.
- Strips default QRCoder quiet zone to enforce custom `QrQuietModules = 4`.
- Calculates module size in whole dots (clamped to `QrMinModuleDots = 3`, `QrTargetDots = 224`, `QrMaxDots = 240`).
- Returns `QrSymbol(modules, moduleDots)` or `null` if the payload cannot fit within `contentWidth`.

> **Verbatim Code Excerpt (Lines 200-227):**
```csharp
    private static QrSymbol? EncodeQr(string payload, int contentWidth)
    {
        if (string.IsNullOrWhiteSpace(payload)) return null;
        bool[,] modules;
        try
        {
            using var data = QRCodeGenerator.GenerateQrCode(payload.Trim(), QRCodeGenerator.ECCLevel.Q);
            var size = 17 + 4 * data.Version;
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
```

---

<a id="callable-9"></a>
### Callable 9: `BitmapTicketRenderer.EmptyToSpace(string)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:229:5:MethodDeclaration`
- **Exact Line Range:** Lines `229–229`
- **Signature:** `private static string EmptyToSpace(string text)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Returns `" "` if `text` is null or empty, preventing GDI+ `MeasureString` from measuring zero height for empty cells.

> **Verbatim Code Excerpt (Line 229):**
```csharp
    private static string EmptyToSpace(string text) => string.IsNullOrEmpty(text) ? " " : text;
```

---

<a id="callable-10"></a>
### Callable 10: `BitmapTicketRenderer.CellInset(TicketRow)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:233:5:MethodDeclaration`
- **Exact Line Range:** Lines `233–233`
- **Signature:** `private static int CellInset(TicketRow row)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Returns internal horizontal padding deducted from cell width: `row.Boxed ? 2 * BoxPadX : CellGap` (12 dots if boxed, 6 dots if unboxed).

> **Verbatim Code Excerpt (Line 233):**
```csharp
    private static int CellInset(TicketRow row) => row.Boxed ? 2 * BoxPadX : CellGap;
```

---

<a id="callable-11"></a>
### Callable 11: `BitmapTicketRenderer.RowPadY(TicketRow)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:235:5:MethodDeclaration`
- **Exact Line Range:** Lines `235–235`
- **Signature:** `private static int RowPadY(TicketRow row)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Calculates vertical cell padding: `row.Boxed ? BoxPadY : row.Inverted ? BandPadY : 0` (4 dots if boxed, 5 dots if inverted, 0 if plain).

> **Verbatim Code Excerpt (Line 235):**
```csharp
    private static int RowPadY(TicketRow row) => row.Boxed ? BoxPadY : row.Inverted ? BandPadY : 0;
```

---

<a id="callable-12"></a>
### Callable 12: `BitmapTicketRenderer.RenderSegment(IReadOnlyList<TicketElement>, int, int, List<Rectangle>, List<QrPlacement>)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:244:5:MethodDeclaration`
- **Exact Line Range:** Lines `244–395`
- **Signature:** `private static Bitmap RenderSegment(IReadOnlyList<TicketElement> elements, int paperWidthDots, int marginDots, List<Rectangle> inverted, List<QrPlacement> codes)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Performs the two-pass layout and rendering of a ticket segment:
- **Pass 1 (Measurement):** Uses a dummy $1 \times 1$ bitmap with `TextRenderingHint.SingleBitPerPixelGridFit` to calculate exact line wraps and element heights for `TicketText`, `TicketRow`, `TicketRule`, and `TicketQr`. Tracks maximum cell height in rows to ensure multi-line item names do not overlap adjacent lines.
- **Pass 2 (Drawing):** Allocates a `Bitmap(paperWidthDots, totalHeight)` and paints background white. Renders text elements using `StringFormatFlags.DirectionRightToLeft`. Reserves QR regions without drawing pixels (delegating to `WriteRaster` for bit-exact stamping).
- **Return Value:** Rendered GDI+ `Bitmap`.

**B. Typography & BiDi Invariants:**  
Uses `TextRenderingHint.SingleBitPerPixelGridFit` rather than AntiAlias. Anti-aliasing generates gray intermediate pixels that fade or vanish under thermal heating thresholds. Single-bit hinting forces sharp black-or-white boundaries.

> **Verbatim Code Excerpt (Lines 244-260, 319-325):**
```csharp
    private static Bitmap RenderSegment(IReadOnlyList<TicketElement> elements, int paperWidthDots, int marginDots,
        List<Rectangle> inverted, List<QrPlacement> codes)
    {
        var left = marginDots;
        var contentWidth = Math.Max(1, paperWidthDots - 2 * marginDots);

        using var measureBmp = new Bitmap(1, 1);
        using var measureG = Graphics.FromImage(measureBmp);
        measureG.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

        var measured = new List<Measured>();
        float totalHeight = TopMargin;
        // ... (measurement pass) ...
        var bmp = new Bitmap(paperWidthDots, Math.Max(1, (int)Math.Ceiling(totalHeight)));
        using (var g = Graphics.FromImage(bmp))
        {
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            g.Clear(Color.White);
            // ... (drawing pass) ...
        }
        return bmp;
    }
```

---

<a id="callable-13"></a>
### Callable 13: `BitmapTicketRenderer.DrawPill(Graphics, TicketText, Measured, float, int, int, List<Rectangle>)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:400:5:MethodDeclaration`
- **Exact Line Range:** Lines `400–412`
- **Signature:** `private static void DrawPill(Graphics g, TicketText text, Measured item, float y, int left, int contentWidth, List<Rectangle> inverted)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Renders inverted white-on-black pills (rounded badges for station titles or reprint headers). Fills a `RoundedRectangle` with black brush, draws text in white, and records the bounding box in `inverted` for protected dilation.

> **Verbatim Code Excerpt (Lines 400-412):**
```csharp
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
```

---

<a id="callable-14"></a>
### Callable 14: `BitmapTicketRenderer.DrawRow(Graphics, TicketRow, Measured, TicketRow?, float, int, int, List<Rectangle>)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:424:5:MethodDeclaration`
- **Exact Line Range:** Lines `424–479`
- **Signature:** `private static void DrawRow(Graphics g, TicketRow row, Measured item, TicketRow? next, float y, int left, int contentWidth, List<Rectangle> inverted)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Renders multi-column table rows. Right-to-left layout: the first cell in `row.Cells` is placed at the right-most edge of the canvas. Draws shared border edges and vertical dividers if `row.Boxed`.

> **Verbatim Code Excerpt (Lines 444-450):**
```csharp
        float x = left + contentWidth;
        for (var i = 0; i < row.Cells.Count; i++)
        {
            var cell = row.Cells[i];
            var width = CellWidth(cell, contentWidth);
            x -= width;
            // ...
```

---

<a id="callable-15"></a>
### Callable 15: `BitmapTicketRenderer.RoundedRectangle(RectangleF, float)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:481:5:MethodDeclaration`
- **Exact Line Range:** Lines `481–491`
- **Signature:** `private static GraphicsPath RoundedRectangle(RectangleF r, float radius)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Builds a `GraphicsPath` with 4 corner arcs defining a rounded rectangle. Clamps corner diameter to bounding dimensions.

> **Verbatim Code Excerpt (Lines 481-491):**
```csharp
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
```

---

<a id="callable-16"></a>
### Callable 16: `BitmapTicketRenderer.CellWidth(TicketCell, int)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:495:5:MethodDeclaration`
- **Exact Line Range:** Lines `495–496`
- **Signature:** `private static int CellWidth(TicketCell cell, int contentWidth)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Calculates dot width for a cell based on percentage of content width: `Math.Max(1, (int)Math.Round(contentWidth * (cell.WidthPercent / 100.0)))`.

> **Verbatim Code Excerpt (Lines 495-496):**
```csharp
    private static int CellWidth(TicketCell cell, int contentWidth) =>
        Math.Max(1, (int)Math.Round(contentWidth * (cell.WidthPercent / 100.0)));
```

---

<a id="callable-17"></a>
### Callable 17: `BitmapTicketRenderer.WriteRaster(Stream, Bitmap, bool, IReadOnlyList<Rectangle>, IReadOnlyList<QrPlacement>)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:500:5:MethodDeclaration`
- **Exact Line Range:** Lines `500–535`
- **Signature:** `private static void WriteRaster(Stream stream, Bitmap bmp, bool thickenStrokes, IReadOnlyList<Rectangle> inverted, IReadOnlyList<QrPlacement> codes)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Converts the GDI+ bitmap to ESC/POS raster byte stream:
1. Samples RGB pixels into a 2D boolean ink array `ink[x, y]`.
2. Applies `Thicken` if `thickenStrokes` is enabled.
3. Stamps QR codes directly into the bit array (`StampQr`) AFTER thickening to prevent module distortion.
4. Packs bits into MSB-first byte array (padded to whole byte width: `widthBytes = (width + 7) / 8`).
5. Emits ESC/POS raster command: `GS v 0 m=0` (`0x1D, 0x76, 0x30, 0x00`), followed by 16-bit width bytes, 16-bit height dots, and raster byte data.

> **Verbatim Code Excerpt (Lines 531-534):**
```csharp
        stream.Write(new byte[] { 0x1D, 0x76, 0x30, 0x00 }); // GS v 0 m=0
        stream.Write(new byte[] { (byte)(widthBytes & 0xFF), (byte)(widthBytes >> 8 & 0xFF) });
        stream.Write(new byte[] { (byte)(height & 0xFF), (byte)(height >> 8 & 0xFF) });
        stream.Write(imageBytes);
```

---

<a id="callable-18"></a>
### Callable 18: `BitmapTicketRenderer.StampQr(bool[,], int, int, QrPlacement)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:537:5:MethodDeclaration`
- **Exact Line Range:** Lines `537–550`
- **Signature:** `private static void StampQr(bool[,] ink, int width, int height, QrPlacement code)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Stamps exact QR modules and white quiet zone into the ink array, overwriting any neighbouring text dilation that may have encroached into the quiet zone.

> **Verbatim Code Excerpt (Lines 537-550):**
```csharp
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
```

---

<a id="callable-19"></a>
### Callable 19: `BitmapTicketRenderer.ProtectedMask(IReadOnlyList<Rectangle>, int, int)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:552:5:MethodDeclaration`
- **Exact Line Range:** Lines `552–560`
- **Signature:** `private static bool[,] ProtectedMask(IReadOnlyList<Rectangle> regions, int width, int height)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Builds a boolean mask marking inverted white-on-black regions (pills, banded rows), ensuring stroke thickening applies reverse dilation (growing white pixels instead of black).

> **Verbatim Code Excerpt (Lines 552-560):**
```csharp
    private static bool[,] ProtectedMask(IReadOnlyList<Rectangle> regions, int width, int height)
    {
        var mask = new bool[width, height];
        foreach (var r in regions)
            for (var y = Math.Max(0, r.Top); y < Math.Min(height, r.Bottom); y++)
                for (var x = Math.Max(0, r.Left); x < Math.Min(width, r.Right); x++)
                    mask[x, y] = true;
        return mask;
    }
```

---

<a id="callable-20"></a>
### Callable 20: `BitmapTicketRenderer.Thicken(bool[,], int, int, bool[,])`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:584:5:MethodDeclaration`
- **Exact Line Range:** Lines `584–606`
- **Signature:** `private static bool[,] Thicken(bool[,] source, int width, int height, bool[,] isProtected)`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Applies 4-connected morphological dilation to black strokes:
- Standard regions: an uninked pixel is set to black if any orthogonal neighbor (up, down, left, right) is black.
- Protected inverted regions: a black background pixel is turned white if any orthogonal neighbor inside the region is white.
- **Topological Invariant:** Diagonal pixels are intentionally ignored. 8-connected dilation fills small loops and counters in Arabic characters (e.g. `م`, `ف`, `ط`), turning them into solid ink blobs.

> **Verbatim Code Excerpt (Lines 584-606):**
```csharp
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
```

---

<a id="callable-21"></a>
### Callable 21: Local Function `WhiteInside(int, int)`
- **Canonical Callable ID:** `src/Falah.PrintAgent/BitmapTicketRenderer.cs:586:9:LocalFunctionStatement`
- **Exact Line Range:** Line `586`
- **Signature:** `bool WhiteInside(int nx, int ny) => isProtected[nx, ny] && !source[nx, ny];`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Helper evaluated inside `Thicken` to verify whether a neighbor pixel is both inside an inverted region and currently white.

> **Verbatim Code Excerpt (Line 586):**
```csharp
        bool WhiteInside(int nx, int ny) => isProtected[nx, ny] && !source[nx, ny];
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Hardware Gap | Root Cause in `BitmapTicketRenderer.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **ESC-001** | Character-Mode Arabic Impossibility | Firmware lacks Arabic shaping ROM. Hardware density commands corrupt stream. `BitmapTicketRenderer` is the sole viable path for Arabic thermal tickets on XP-80C. | **CRITICAL (Text Mode Only)** | Maintain `BitmapTicketRenderer` as production default on Windows terminals. Keep `EscPosTicketRenderer` strictly as reference fallback. |
| **PRT-007** | High-DPI Memory Consumption | `Bitmap` allocations scale with ticket line count. For extreme banquet orders (>100 items), raster bitmap height can exceed 6,000 pixels (~4 MB RAM per render). | **LOW** | Stream raster in chunks of 512 lines if tickets exceed standard lengths. |
