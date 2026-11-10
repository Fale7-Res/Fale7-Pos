namespace Falah.Domain.Printing;

using System.Text;

/// <summary>
/// Reference ESC/POS byte encoder for an 80mm thermal printer (the Xprinter XP-D200N class of
/// hardware this restaurant actually uses) - turns a ComposedTicket into the byte stream a printer's
/// serial/USB/network port expects, using the standard ESC/POS command set (initialize, bold,
/// center justify, double-size text, line feed, full cut).
///
/// NOT physically verified: which text encoding actually renders Arabic correctly on the real
/// printer - raw UTF-8 in a "Unicode mode" some modern ESC/POS firmwares accept, or a legacy Arabic
/// codepage (CP864/WCP1256) selected via its own ESC/POS command - varies by printer firmware and
/// MUST be confirmed against the physical Xprinter before this is trusted for a live receipt. The
/// textEncoding parameter is deliberately swappable rather than hardcoded for exactly that reason;
/// UTF-8 is only today's best-guess default. The COMMAND STRUCTURE itself (init/bold/center/size/
/// cut sequencing) follows the published ESC/POS standard and is unit-tested for byte-exactness -
/// only the character-encoding step carries real hardware risk.
/// </summary>
public static class EscPosTicketRenderer
{
    private const byte Esc = 0x1B;
    private const byte Gs = 0x1D;
    private static readonly byte[] InitializeCommand = { Esc, 0x40 };
    private static readonly byte[] LineFeed = { 0x0A };
    private static readonly byte[] FullCut = { Gs, 0x56, 0x00 };

    /// <summary>Paper fed between the last printed line and the cut. A thermal printer's cutter sits
    /// physically below its print head, so without enough feed the cut lands in the middle of
    /// content that has not travelled past the blade yet. Five feeds, not two: two was measured on
    /// the real hardware to leave the final line sliced or missing. Kept in step with
    /// BitmapTicketRenderer's own constant, which is the path this restaurant actually prints on.</summary>
    private static readonly byte[] FeedBeforeCut = { 0x0A, 0x0A, 0x0A, 0x0A, 0x0A };

    /// <summary>Selects the printer's active character table (ESC t n) before any text is written -
    /// without it, the printer keeps interpreting bytes under whatever table it powered on with
    /// (typically a Western/USA table), so sending Arabic-encoded bytes alone renders garbage
    /// regardless of which .NET Encoding produced them. The table id is firmware-specific and MUST
    /// be confirmed against the physical printer alongside the text encoding itself - see this
    /// class's own doc comment.</summary>
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

    /// <summary>Reset formatting, feed clear of the cutter, then cut. Reset matters because a
    /// subsequent ticket on the same physical printer must never inherit the previous one's
    /// bold/size/justification state - and with mid-stream cuts there now genuinely is a
    /// subsequent ticket inside the same byte stream.</summary>
    private static void WriteResetFeedAndCut(Stream stream)
    {
        stream.Write(Justify(TicketAlign.Start));
        stream.Write(Bold(false));
        stream.Write(CharacterSize(TicketTextSize.Normal));
        stream.Write(FeedBeforeCut);
        stream.Write(FullCut);
    }

    private static byte[] SelectCodePageTable(byte tableId) => new[] { Esc, (byte)'t', tableId };

    private static byte[] Justify(TicketAlign align) => new[]
    {
        Esc, (byte)0x61, align switch { TicketAlign.Center => (byte)1, TicketAlign.End => (byte)2, _ => (byte)0 },
    };

    private static byte[] Bold(bool on) => new[] { Esc, (byte)0x45, (byte)(on ? 1 : 0) };

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
}
