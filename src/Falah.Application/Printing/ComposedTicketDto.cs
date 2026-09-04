namespace Falah.Application.Printing;

/// <summary>Structured ticket content for a print job - what to print, not how to encode it for a
/// specific printer's command set. A station's print agent (future WinUI/hardware work, not built
/// in this pass) turns this into ESC/POS bytes; see Falah.Infrastructure.Printing.EscPosTicketRenderer
/// for a reference encoder already built and unit-tested against the standard ESC/POS command set -
/// its exact codepage/Arabic behavior on the real Xprinter still needs physical verification.</summary>
public sealed record TicketLineDto(string Text, bool Bold, bool Centered, bool LargeFont);

public sealed record ComposedTicketDto(string Route, string OrderId, string OrderNumber, IReadOnlyList<TicketLineDto> Lines);
