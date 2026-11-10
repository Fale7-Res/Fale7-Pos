namespace Falah.Application.Printing;

using Falah.Domain.Printing;

/// <summary>
/// The single definition of how a ticket element crosses the wire, in BOTH directions.
///
/// The server composes tickets (Domain -> Dto) and the print agent renders them (Dto -> Domain).
/// Written as one class used by both sides on purpose: the Kind/Align/Size strings are a contract,
/// and two independent copies of a contract is how one side silently starts emitting "centre" while
/// the other still reads "center". Anything unrecognised degrades to the closest safe default
/// rather than throwing - a ticket that prints slightly plainer is recoverable, a station agent
/// that crashes on an unknown string is not.
/// </summary>
public static class TicketElementMapping
{
    public const string KindText = "text";
    public const string KindRow = "row";
    public const string KindRule = "rule";
    public const string KindCut = "cut";
    public const string KindQr = "qr";

    public static TicketElementDto ToDto(TicketElement element) => element switch
    {
        TicketText t => new TicketElementDto(KindText, t.Text, t.Bold, AlignToWire(t.Align), SizeToWire(t.Size),
            Inverted: t.Inverted),
        TicketRow r => new TicketElementDto(KindRow, Size: SizeToWire(r.Size),
            Cells: r.Cells.Select(c => new TicketCellDto(c.Text, c.WidthPercent, AlignToWire(c.Align), c.Bold)).ToList(),
            Inverted: r.Inverted, Boxed: r.Boxed, Continuation: r.Continuation),
        TicketRule rule => new TicketElementDto(KindRule, Rule: RuleToWire(rule.Style)),
        TicketCut => new TicketElementDto(KindCut),
        // The payload rides in Text, so an agent that predates QR support prints the link as a
        // plain line instead of dropping it.
        TicketQr q => new TicketElementDto(KindQr, q.Payload, Align: "center"),
        _ => new TicketElementDto(KindText, element.ToString() ?? ""),
    };

    public static TicketElement ToDomain(TicketElementDto dto) => dto.Kind switch
    {
        KindRow => new TicketRow(
            (dto.Cells ?? Array.Empty<TicketCellDto>())
                .Select(c => new TicketCell(c.Text, c.WidthPercent, AlignFromWire(c.Align), c.Bold)).ToList(),
            SizeFromWire(dto.Size), dto.Inverted, dto.Boxed, dto.Continuation),
        KindRule => new TicketRule(RuleFromWire(dto.Rule)),
        KindCut => new TicketCut(),
        KindQr => new TicketQr(dto.Text ?? ""),
        // Unknown kinds fall through to text, which is why Text carries the content for every kind
        // that has any: an agent built before a future element type still prints something true.
        _ => new TicketText(dto.Text ?? "", dto.Bold, AlignFromWire(dto.Align), SizeFromWire(dto.Size), dto.Inverted),
    };

    private static string AlignToWire(TicketAlign a) => a switch
    {
        TicketAlign.Center => "center", TicketAlign.End => "end", _ => "start",
    };

    private static TicketAlign AlignFromWire(string a) => a switch
    {
        "center" => TicketAlign.Center, "end" => TicketAlign.End, _ => TicketAlign.Start,
    };

    private static string SizeToWire(TicketTextSize s) => s switch
    {
        TicketTextSize.Display => "display", TicketTextSize.Huge => "huge", TicketTextSize.Title => "title",
        TicketTextSize.Large => "large", _ => "normal",
    };

    private static TicketTextSize SizeFromWire(string s) => s switch
    {
        "display" => TicketTextSize.Display, "huge" => TicketTextSize.Huge, "title" => TicketTextSize.Title,
        "large" => TicketTextSize.Large, _ => TicketTextSize.Normal,
    };

    private static string RuleToWire(TicketRuleStyle r) => r switch
    {
        TicketRuleStyle.Heavy => "heavy", TicketRuleStyle.Blank => "blank", TicketRuleStyle.Dashed => "dashed", TicketRuleStyle.Thin => "thin", TicketRuleStyle.Accent => "accent", _ => "light",
    };

    private static TicketRuleStyle RuleFromWire(string r) => r switch
    {
        "heavy" => TicketRuleStyle.Heavy, "blank" => TicketRuleStyle.Blank, "dashed" => TicketRuleStyle.Dashed, "thin" => TicketRuleStyle.Thin, "accent" => TicketRuleStyle.Accent, _ => TicketRuleStyle.Light,
    };
}
