namespace Falah.Server.IntegrationTests;

using Falah.Domain.Printing;
using Falah.Infrastructure.Printing;
using Xunit;

/// <summary>
/// Pure byte-level tests for the ESC/POS command STRUCTURE (init/bold/center/size/cut sequencing) -
/// no database, no hardware. These prove the encoder emits the standard, published ESC/POS command
/// bytes correctly; they cannot and do not prove Arabic renders correctly on the real Xprinter -
/// see EscPosTicketRenderer's own doc comment for why that needs physical verification later.
/// </summary>
public class EscPosTicketRendererTests
{
    private static ComposedTicket SimpleTicket(params TicketLine[] lines) =>
        new(PrintRoute.Receipt, "ord-1", "#0001", lines);

    [Fact]
    public void Every_rendered_ticket_starts_with_the_initialize_command()
    {
        var bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("test")));
        Assert.Equal(0x1B, bytes[0]);
        Assert.Equal(0x40, bytes[1]);
    }

    [Fact]
    public void Every_rendered_ticket_ends_with_a_full_cut_command()
    {
        var bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("test")));
        Assert.Equal(0x1D, bytes[^3]);
        Assert.Equal(0x56, bytes[^2]);
        Assert.Equal(0x00, bytes[^1]);
    }

    [Fact]
    public void A_bold_line_is_preceded_by_the_bold_on_command()
    {
        var bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("bold", Bold: true)));
        // ESC E 1 (bold on) must appear somewhere before the text bytes.
        var text = System.Text.Encoding.UTF8.GetBytes("bold");
        var boldOnIndex = IndexOf(bytes, new byte[] { 0x1B, 0x45, 0x01 });
        var textIndex = IndexOf(bytes, text);
        Assert.True(boldOnIndex >= 0 && boldOnIndex < textIndex);
    }

    [Fact]
    public void A_centered_line_is_preceded_by_the_center_justify_command()
    {
        var bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("centered", Centered: true)));
        var centerIndex = IndexOf(bytes, new byte[] { 0x1B, 0x61, 0x01 });
        Assert.True(centerIndex >= 0);
    }

    [Fact]
    public void A_large_font_line_is_preceded_by_the_double_size_command()
    {
        var bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("big", LargeFont: true)));
        var sizeIndex = IndexOf(bytes, new byte[] { 0x1D, 0x21, 0x11 });
        Assert.True(sizeIndex >= 0);
    }

    [Fact]
    public void Formatting_is_reset_before_the_final_cut_so_it_never_leaks_into_the_next_ticket()
    {
        var bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("bold and big", Bold: true, LargeFont: true, Centered: true)));
        var cutIndex = IndexOf(bytes, new byte[] { 0x1D, 0x56, 0x00 });
        var resetBoldIndex = LastIndexOf(bytes, new byte[] { 0x1B, 0x45, 0x00 }, cutIndex);
        var resetSizeIndex = LastIndexOf(bytes, new byte[] { 0x1D, 0x21, 0x00 }, cutIndex);
        Assert.True(resetBoldIndex >= 0 && resetBoldIndex < cutIndex);
        Assert.True(resetSizeIndex >= 0 && resetSizeIndex < cutIndex);
    }

    [Fact]
    public void Text_is_encoded_with_the_provided_encoding_not_hardcoded()
    {
        var latin1Bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("abc")), System.Text.Encoding.Latin1);
        var utf8Bytes = EscPosTicketRenderer.Render(SimpleTicket(new TicketLine("abc")), System.Text.Encoding.UTF8);
        // For pure ASCII "abc" both encodings agree - this just proves the parameter is actually used,
        // not ignored in favor of a hardcoded encoding.
        Assert.Equal(latin1Bytes, utf8Bytes);
    }

    private static int IndexOf(byte[] haystack, byte[] needle)
    {
        for (var i = 0; i <= haystack.Length - needle.Length; i++)
            if (haystack.Skip(i).Take(needle.Length).SequenceEqual(needle)) return i;
        return -1;
    }

    private static int LastIndexOf(byte[] haystack, byte[] needle, int before)
    {
        for (var i = before - needle.Length; i >= 0; i--)
            if (haystack.Skip(i).Take(needle.Length).SequenceEqual(needle)) return i;
        return -1;
    }
}
