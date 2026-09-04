namespace Falah.Infrastructure.Printing;

using System.Text;
using Falah.Domain.Printing;

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

    public static byte[] Render(ComposedTicket ticket, Encoding? textEncoding = null)
    {
        var encoding = textEncoding ?? Encoding.UTF8;
        using var stream = new MemoryStream();
        stream.Write(InitializeCommand);

        foreach (var line in ticket.Lines)
        {
            stream.Write(Justify(line.Centered));
            stream.Write(Bold(line.Bold));
            stream.Write(CharacterSize(line.LargeFont));
            stream.Write(encoding.GetBytes(line.Text));
            stream.Write(LineFeed);
        }

        // Reset formatting before cutting - a subsequent ticket on the same physical printer must
        // never inherit the previous ticket's bold/size/justification state.
        stream.Write(Justify(centered: false));
        stream.Write(Bold(false));
        stream.Write(CharacterSize(large: false));
        stream.Write(LineFeed);
        stream.Write(LineFeed);
        stream.Write(FullCut);

        return stream.ToArray();
    }

    private static byte[] Justify(bool centered) => new[] { Esc, (byte)0x61, (byte)(centered ? 1 : 0) };
    private static byte[] Bold(bool on) => new[] { Esc, (byte)0x45, (byte)(on ? 1 : 0) };
    private static byte[] CharacterSize(bool large) => new[] { Gs, (byte)0x21, (byte)(large ? 0x11 : 0x00) };
}
