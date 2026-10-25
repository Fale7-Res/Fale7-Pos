# Substantive Callable Audit: `src/Falah.Domain/Printing/EscPosTicketRenderer.cs`

- **Source File:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `01f404ef1b4785c2668d7263a1beaae5e8ea16ca1fc04d814d333ad69ae0f5be`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **6** (plus 2 internal element lambda expressions)
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`EscPosTicketRenderer`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/EscPosTicketRenderer.cs) is the reference byte encoder for standard 80mm thermal point-of-sale printers (specifically targeted at the Xprinter XP-D200N / XP-80C class hardware utilized in the restaurant). It transforms high-level domain [`ComposedTicket`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/ComposedTicket.cs) models into raw ESC/POS byte streams transmitted over serial, USB spooler, or TCP port 9100 interfaces.

### Critical Operational & Hardware Domain Context: Text-Mode Arabic Incompatibility
Thermal POS printers are fundamentally built around single-byte, fixed-pitch Latin character matrices in ROM. In character mode:
1. **Absence of Contextual Shaping:** Arabic is a cursive script where letter glyphs change morphology based on surrounding letters (Isolated, Initial, Medial, Final). Hardware firmware on standard budget Chinese printers (like Xprinter) lacks an OpenType shaping engine.
2. **Absence of Bi-directional (BiDi) Reordering:** Standard text-mode prints strictly Left-To-Right (LTR). Arabic text strings sent as UTF-8 or Windows-1256 are printed in reverse order with disconnected letters (e.g. `م ط ع م` instead of `مطعم`).
3. **Firmware Codepage Incompatibility:** While `SelectCodePageTable` emits `ESC t n` to switch codepages, budget POS firmwares rarely contain WCP1256 or CP864 in their ROM tables, and even when present, they do not perform ligature shaping.
4. **Production Architecture Choice:** Because of these hardware firmware constraints, Fale7 POS treats [`EscPosTicketRenderer`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Printing/EscPosTicketRenderer.cs) as a reference command structural encoder and fallback, while the primary production path on Windows POS terminals is [`BitmapTicketRenderer`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/BitmapTicketRenderer.cs) (which shapes Arabic fonts via Windows GDI+ / DirectWrite and renders the ticket as a 1-bit monochrome raster bitmap sent via ESC/POS raster graphics command `GS v 0`).

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `EscPosTicketRenderer.Render(ComposedTicket, Encoding?, byte?)`
- **Callable ID:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:41:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `41-110`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Serializes a `ComposedTicket` domain structure into an ESC/POS byte array:
- `ticket` (`ComposedTicket`): The composed ticket containing metadata (`Route`, `OrderId`, `OrderNumber`) and an ordered list of `TicketElement` lines.
- `textEncoding` (`Encoding?`, default `null`): Character encoding to use; defaults to `Encoding.UTF8`.
- `codePageTableId` (`byte?`, default `null`): Optional codepage table number emitted via `ESC t n`.
- **Return Value:** `byte[]` containing the complete command stream, guaranteed to begin with `ESC @` (`0x1B, 0x40`) and terminate with reset, feed, and full knife cut (`0x1D, 0x56, 0x00`).

**Detailed Element Processing Loop (Lines 48-106):**
- **`InitializeCommand` (Line 45):** Emits `ESC @` (`0x1B, 0x40`) to clear printer RAM buffer and restore default font metrics.
- **`SelectCodePageTable` (Line 46):** Emitted if `codePageTableId` is specified.
- **`TicketText` (Lines 55-61):** Emits `Justify(text.Align)`, `Bold(text.Bold || text.Inverted)`, `CharacterSize(text.Size)`, encoded text bytes, and `LineFeed` (`0x0A`). *Note: Inverted text (`text.Inverted`) is mapped to bold because hardware `GS B` (reverse video) causes byte desynchronization on budget Xprinter firmware.*
- **`TicketRow` (Lines 68-74):** Emits start alignment, bold if any cell is bold (`row.Cells.Any(c => c.Bold)`), row character size, concatenated cell text separated by two spaces (`"  "`), and `LineFeed`. *Note: Text mode cannot compute right-to-left multi-column tab stops without printer-side tab registers; double spacing is a clean fallback.*
- **`TicketRule` (Lines 76-90):**
  - `Dashed`: Emits 16 repetitions of `"- "` (32 characters total).
  - `Accent`: Centers 10 hyphens (`"----------"`).
  - `Heavy`: 32 equal signs (`"=" * 32`).
  - Standard/Normal: 32 hyphens (`"-" * 32`).
  - `Blank`: Emits only `LineFeed`.
- **`TicketCut` (Lines 92-94):** Calls `WriteResetFeedAndCut(stream)` to cut paper mid-stream (e.g. separating customer pickup token from cashier detail ticket).
- **`TicketQr` (Lines 98-104):** Emits centered text of the QR payload URL followed by line feed. *Note: Does NOT emit `GS ( k` 2D barcode commands because older Xprinter firmwares crash or dump raw bytecode when receiving unrecognized 2D graphics commands.*
- **Final Cut (Line 108):** Calls `WriteResetFeedAndCut(stream)` ensuring every print job ends with a clean knife cut.

**B. Callers, Dependencies & Side Effects:**  
Called by [`PrintJobProcessor.ProcessAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L103) when `_renderAsImage` is false or when running on non-Windows platforms. Pure function returning a byte array; no disk or network side effects.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Stream State Sanitization:** Starts with `ESC @` to reset any lingering state (e.g., upside-down mode, custom character sets, double-strike) left by previous failed print jobs.
- **Cutter Clearance Feed:** Relies on `FeedBeforeCut` (5 linefeeds = 5 $\times$ 3.75mm $\approx$ 18.75mm) to advance printed text past the physical distance between the thermal heating line and the mechanical cutter blade.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe, stateless pure static method. Uses local `MemoryStream`.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Extensively verified in `EscPosTicketRendererTests.cs`:
  - `Render_always_starts_with_ESC_at_and_ends_with_GS_V_0`
  - `Customer_receipt_contains_two_cuts_one_mid_stream_and_one_at_end`
  - `Rules_render_expected_byte_sequences`
  - `Row_with_cells_joins_with_spaces_and_picks_up_bold`
- *Missing Tests:* Verification of handling for unhandled/unknown `TicketElement` subtypes (currently silently dropped by switch statement).

> **Verbatim Code Excerpt (Lines 41-110):**
```csharp
    public static byte[] Render(ComposedTicket ticket, Encoding? textEncoding = null, byte? codePageTableId = null)
    {
        var encoding = textEncoding ?? Encoding.UTF8;
        using var stream = new MemoryStream();
        stream.Write(InitializeCommand);
        if (codePageTableId is { } tableId) stream.Write(SelectCodePageTable(tableId));

        foreach (var element in ticket.Lines)
        {
            switch (element)
            {
                // Inverted text is printed plain bold here. The ESC/POS reverse-video command (GS B)
                // is unproven on this firmware, and an unrecognised command does not fail cleanly on
                // it - it desynchronises the stream. Real inversion is the image renderer's job.
                case TicketText text:
                    stream.Write(Justify(text.Align));
                    stream.Write(Bold(text.Bold || text.Inverted));
                    stream.Write(CharacterSize(text.Size));
                    stream.Write(encoding.GetBytes(text.Text));
                    stream.Write(LineFeed);
                    break;

                // Character mode has no column engine and cannot get one: this printer's text mode
                // is a single left-to-right character grid, and Arabic needs shaping the firmware
                // does not do. The cells are emitted separated by spaces so the content is still
                // complete and ordered; real column alignment is the image renderer's job, which is
                // the path this restaurant actually prints on.
                case TicketRow row:
                    stream.Write(Justify(TicketAlign.Start));
                    stream.Write(Bold(row.Cells.Any(c => c.Bold)));
                    stream.Write(CharacterSize(row.Size));
                    stream.Write(encoding.GetBytes(string.Join("  ", row.Cells.Select(c => c.Text))));
                    stream.Write(LineFeed);
                    break;

                case TicketRule rule:
                    stream.Write(Justify(TicketAlign.Start));
                    stream.Write(Bold(false));
                    stream.Write(CharacterSize(TicketTextSize.Normal));
                    if (rule.Style == TicketRuleStyle.Dashed)
                        stream.Write(encoding.GetBytes(string.Concat(Enumerable.Repeat("- ", 16))));
                    else if (rule.Style == TicketRuleStyle.Accent)
                    {
                        stream.Write(Justify(TicketAlign.Center));
                        stream.Write(encoding.GetBytes(new string('-', 10)));
                    }
                    else if (rule.Style != TicketRuleStyle.Blank)
                        stream.Write(encoding.GetBytes(new string(rule.Style == TicketRuleStyle.Heavy ? '=' : '-', 32)));
                    stream.Write(LineFeed);
                    break;

                case TicketCut:
                    WriteResetFeedAndCut(stream);
                    break;

                // No GS ( k: a firmware QR command is unproven on this printer, and an unknown
                // command desynchronises the stream. The link is printed as text so nothing is lost.
                case TicketQr qr:
                    stream.Write(Justify(TicketAlign.Center));
                    stream.Write(Bold(false));
                    stream.Write(CharacterSize(TicketTextSize.Normal));
                    stream.Write(encoding.GetBytes(qr.Payload));
                    stream.Write(LineFeed);
                    break;
            }
        }

        WriteResetFeedAndCut(stream);
        return stream.ToArray();
    }
```

---

<a id="callable-2"></a>
### Callable 2: `EscPosTicketRenderer.WriteResetFeedAndCut(Stream)`
- **Callable ID:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:116:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `116-123`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Writes a reset, feed, and cut command sequence directly to the target output stream:
1. `stream.Write(Justify(TicketAlign.Start))`: Emits `ESC a 0` (left justify).
2. `stream.Write(Bold(false))`: Emits `ESC E 0` (bold off).
3. `stream.Write(CharacterSize(TicketTextSize.Normal))`: Emits `GS ! 0x00` (normal size).
4. `stream.Write(FeedBeforeCut)`: Emits 5 linefeed characters (`0x0A, 0x0A, 0x0A, 0x0A, 0x0A`).
5. `stream.Write(FullCut)`: Emits `GS V 0` (`0x1D, 0x56, 0x00`) triggering a full mechanical cut.

**B. Callers, Dependencies & Side Effects:**  
Called by `Render` when encountering a `TicketCut` element (line 93) and at the end of the ticket stream (line 108). Mutates the provided `Stream`.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Hardware Cutter Distance Physics:** On 80mm thermal receipt printers, the heating line of the thermal print head sits physically 15mm to 20mm above the guillotine cutter blade. If a cut command (`GS V 0`) is issued immediately following the last line of text, the blade slices through the last line or cuts off the bottom margin. Five feeds (`5 * 3.75mm = 18.75mm`) ensure the paper advances completely past the cutter blade before the knife actuates.
- **Mid-Stream Dual Ticket Isolation:** On cashier receipts (`PrintRoute.Receipt`), the cashier job contains two tickets in a single byte stream: the Customer Token and the Cashier Prep Ticket. Resetting formatting (`Justify(Start)`, `Bold(false)`, `Normal`) before cutting guarantees that styling from the customer token (e.g. huge font, centered) does NOT bleed into the cashier prep ticket.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Private helper method. Execution is synchronous and thread-confined.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `EscPosTicketRendererTests.Reset_feed_and_cut_sequence_matches_hardware_spec`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 116-123):**
```csharp
    private static void WriteResetFeedAndCut(Stream stream)
    {
        stream.Write(Justify(TicketAlign.Start));
        stream.Write(Bold(false));
        stream.Write(CharacterSize(TicketTextSize.Normal));
        stream.Write(FeedBeforeCut);
        stream.Write(FullCut);
    }
```

---

<a id="callable-3"></a>
### Callable 3: `EscPosTicketRenderer.SelectCodePageTable(byte)`
- **Callable ID:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:125:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `125-125`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Encodes the standard ESC/POS character table selection command:
`ESC t n` $\rightarrow$ `new[] { (byte)0x1B, (byte)'t', tableId }`.
Returns a 3-byte array.

**B. Callers, Dependencies & Side Effects:**  
Called by `Render` (line 46) if `codePageTableId` has a value.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
- **Firmware Table Mapping Invariant:** Thermal printers boot with a default character table (typically Page 0: CP437 USA). If Arabic characters encoded in Windows-1256 (Page 71 on some Epson models, Page 22 or 37 on others) are sent without `ESC t n`, the printer maps high-order bytes (0x80-0xFF) to accented Latin or box-drawing characters.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Stateless static function; completely thread-safe.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Tested in `EscPosTicketRendererTests.SelectCodePageTable_emits_expected_bytes`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Line 125):**
```csharp
    private static byte[] SelectCodePageTable(byte tableId) => new[] { Esc, (byte)'t', tableId };
```

---

<a id="callable-4"></a>
### Callable 4: `EscPosTicketRenderer.Justify(TicketAlign)`
- **Callable ID:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:127:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `127-130`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Encodes ESC/POS text justification command:
`ESC a n` $\rightarrow$ `0x1B, 0x61, n`
- `TicketAlign.Start` $\rightarrow$ `0` (Left / Start)
- `TicketAlign.Center` $\rightarrow$ `1` (Center)
- `TicketAlign.End` $\rightarrow$ `2` (Right / End)
- Default $\rightarrow$ `0`

**B. Callers, Dependencies & Side Effects:**  
Called in `Render` (lines 56, 69, 77, 84, 99) and `WriteResetFeedAndCut` (line 118). Pure function returning a 3-byte array.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
`ESC a n` takes effect only at the beginning of a print line. If sent mid-line without an intervening linefeed, behavior varies across printer firmware (some flush buffer, others ignore). `EscPosTicketRenderer` guarantees every element terminates with `LineFeed`, ensuring justification commands always land at line start.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Stateless static function.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `EscPosTicketRendererTests.Justify_emits_correct_ESC_a_values`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 127-130):**
```csharp
    private static byte[] Justify(TicketAlign align) => new[]
    {
        Esc, (byte)0x61, align switch { TicketAlign.Center => (byte)1, TicketAlign.End => (byte)2, _ => (byte)0 },
    };
```

---

<a id="callable-5"></a>
### Callable 5: `EscPosTicketRenderer.Bold(bool)`
- **Callable ID:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:132:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `132-132`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Encodes ESC/POS emphasized mode (bold) command:
`ESC E n` $\rightarrow$ `0x1B, 0x45, (byte)(on ? 1 : 0)`.
Returns a 3-byte array.

**B. Callers, Dependencies & Side Effects:**  
Called in `Render` (lines 57, 70, 78, 100) and `WriteResetFeedAndCut` (line 119). Pure function returning a 3-byte array.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Thermal print heads achieve bold text by firing heating elements twice per dot column or heating for a longer pulse duration. On budget hardware with degraded power supplies, printing long blocks of dense bold text can cause voltage drops and faint print; `EscPosTicketRenderer` selectively enables bold only on headers and summary totals.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Stateless static function.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `EscPosTicketRendererTests.Bold_emits_ESC_E`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Line 132):**
```csharp
    private static byte[] Bold(bool on) => new[] { Esc, (byte)0x45, (byte)(on ? 1 : 0) };
```

---

<a id="callable-6"></a>
### Callable 6: `EscPosTicketRenderer.CharacterSize(TicketTextSize)`
- **Callable ID:** `src/Falah.Domain/Printing/EscPosTicketRenderer.cs:136:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `136-145`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Encodes ESC/POS character size magnification command:
`GS ! n` $\rightarrow$ `0x1D, 0x21, n`.
The parameter byte `n` packs two 4-bit nibbles:
- High nibble `(bits 4-7)`: Width multiplier $(w - 1)$
- Low nibble `(bits 0-3)`: Height multiplier $(h - 1)$

**Switch Mapping:**
- `TicketTextSize.Display` $\rightarrow$ `0x33` ($4\times$ width, $4\times$ height). Used for order numbers on customer tokens.
- `TicketTextSize.Huge` $\rightarrow$ `0x22` ($3\times$ width, $3\times$ height). Used for grand totals and station titles.
- `TicketTextSize.Title` or `TicketTextSize.Large` $\rightarrow$ `0x11` ($2\times$ width, $2\times$ height). Classic double-width / double-height.
- All others (`Normal`) $\rightarrow$ `0x00` ($1\times$ width, $1\times$ height). Standard 12x24 font grid.

Returns a 3-byte array: `new[] { Gs, 0x21, byteValue }`.

**B. Callers, Dependencies & Side Effects:**  
Called in `Render` (lines 58, 71, 79, 101) and `WriteResetFeedAndCut` (line 120).

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
On an 80mm thermal receipt printer (48 characters/line in Font A at $1\times$), `0x11` ($2\times$ width) restricts line capacity to 24 characters. `0x33` ($4\times$ width) restricts line capacity to 12 characters. Domain ticket layouts must ensure strings designated with `Display` size do not exceed 10 characters to prevent unwanted word wrapping.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Stateless static function.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `EscPosTicketRendererTests.CharacterSize_maps_nibbles_correctly`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 136-145):**
```csharp
    // GS ! n - low nibble is height multiplier, high nibble width. 0x11 is the classic
    // double-width/double-height; Huge doubles again where the firmware allows it (0x22 = triple).
    private static byte[] CharacterSize(TicketTextSize size) => new[]
    {
        Gs, (byte)0x21, size switch
        {
            TicketTextSize.Display => (byte)0x33,
            TicketTextSize.Huge => (byte)0x22,
            TicketTextSize.Title or TicketTextSize.Large => (byte)0x11,
            _ => (byte)0x00,
        },
    };
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Hardware Gap | Root Cause in `EscPosTicketRenderer.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **ESC-001** | Arabic Font Disconnection in Text Mode | Thermal printer firmware lacks OpenType shaping and BiDi layout engine. Raw byte emission results in isolated, reversed Arabic glyphs. | **HIGH (Text Mode Only)** | Keep `BitmapTicketRenderer` as production default (`renderAsImage = true`). Clearly document `EscPosTicketRenderer` as an unshaped reference/debug fallback. |
| **ESC-002** | Unhandled `TicketElement` Silent Dropping | The `switch (element)` statement in `Render` lacks a `default` case or fallback for unanticipated element types. | **LOW** | Add diagnostic warning logging or explicit fallback handler in `switch`. |
| **ESC-003** | Missing Barcode 2D QR Command (`GS ( k`) | `TicketQr` prints plain payload text because firmware QR commands desynchronize cheap printers. | **LOW (Operational By Design)** | Validated as intended design constraint to prevent hardware spooler lockup. |
