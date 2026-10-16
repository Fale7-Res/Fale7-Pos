namespace Falah.Server.IntegrationTests;

using Falah.Application.Printing;

/// <summary>Every readable string on a wire ticket line: a text line's own text, or each cell of a
/// row. Item tables and money lines are rows, so reading only Text would miss every product name
/// and every amount on the ticket.</summary>
internal static class TicketDtoText
{
    public static IEnumerable<string?> Of(TicketElementDto line) =>
        line.Cells is { Count: > 0 } cells ? cells.Select(c => (string?)c.Text) : new[] { line.Text };
}
