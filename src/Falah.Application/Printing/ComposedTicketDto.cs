namespace Falah.Application.Printing;

/// <summary>One cell of a ticket table row. WidthPercent is a share of the printable paper width,
/// not a character count - see Falah.Domain.Printing.TicketCell for why.</summary>
public sealed record TicketCellDto(string Text, int WidthPercent, string Align = "start", bool Bold = false);

/// <summary>
/// One element of a print job's ticket content - what to print, not how to encode it for a specific
/// printer's command set. Falah.PrintAgent turns these into ESC/POS bytes.
///
/// Deliberately FLAT with a Kind discriminator rather than a polymorphic record hierarchy: this
/// record crosses a process boundary as JSON, and System.Text.Json needs explicit converter or
/// JsonDerivedType configuration to round-trip a hierarchy - configuration that lives far from
/// these types and fails at runtime, on a printer, rather than at compile time. A flat record
/// simply works, on both sides, with no serializer setup at all.
///
/// Kind is one of: "text", "row", "rule", "cut". Fields not meaningful for a Kind are left at their
/// defaults - a "cut" carries nothing but its Kind.
/// </summary>
public sealed record TicketElementDto(
    string Kind,
    string? Text = null,
    bool Bold = false,
    string Align = "start",
    string Size = "normal",
    IReadOnlyList<TicketCellDto>? Cells = null,
    string Rule = "light",
    // Appended with safe defaults: an agent built before these existed simply ignores them and
    // prints the element plainly, which is a readable degradation rather than a failure.
    bool Inverted = false,
    bool Boxed = false,
    bool Continuation = false);

public sealed record ComposedTicketDto(string Route, string OrderId, string OrderNumber, IReadOnlyList<TicketElementDto> Lines);
