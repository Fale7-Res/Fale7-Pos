namespace Falah.Application.Printing;

/// <summary>Structured ticket content for a print job - what to print, not how to encode it for a
/// specific printer's command set. The Falah.PrintAgent station service turns this into ESC/POS bytes; see Falah.Domain.Printing.EscPosTicketRenderer
/// for a reference encoder already built and unit-tested against the standard ESC/POS command set -
/// its exact codepage/Arabic behavior on the real Xprinter still needs physical verification.</summary>
public sealed record TicketLineDto(string Text, bool Bold, bool Centered, bool LargeFont);

public sealed record ComposedTicketDto(string Route, string OrderId, string OrderNumber, IReadOnlyList<TicketLineDto> Lines);
