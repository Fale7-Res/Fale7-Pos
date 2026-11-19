using System.Text.Json;
using Falah.Application.Printing;
using Falah.Domain.Printing;
using Xunit;

namespace Falah.Server.IntegrationTests;

/// <summary>Paper redesign 2026-11-17: the logo element and fitted label/value rows cross the wire
/// between server and print agent, and an agent built before them degrades instead of failing.
/// No database.</summary>
public class TicketElementMappingTests
{
    [Fact]
    public void Logo_round_trips() =>
        Assert.IsType<TicketLogo>(TicketElementMapping.ToDomain(TicketElementMapping.ToDto(new TicketLogo())));

    [Fact]
    public void FitStart_round_trips()
    {
        var row = new TicketRow(new[] { new TicketCell("أ", 60), new TicketCell("ب", 40) }, FitStart: true);
        var back = Assert.IsType<TicketRow>(TicketElementMapping.ToDomain(TicketElementMapping.ToDto(row)));
        Assert.True(back.FitStart);
    }

    [Fact]
    public void A_kind_the_agent_does_not_know_prints_as_harmless_empty_text()
    {
        // What an agent built before "logo" existed does with it: the unknown-kind fallback.
        var text = Assert.IsType<TicketText>(TicketElementMapping.ToDomain(new TicketElementDto("logo-from-the-future")));
        Assert.Equal("", text.Text);
    }

    [Fact]
    public void A_row_sent_by_an_older_server_is_not_fitted()
    {
        var json = """{"kind":"row","cells":[{"text":"أ","widthPercent":60,"align":"start","bold":false}]}""";
        var dto = JsonSerializer.Deserialize<TicketElementDto>(json, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        Assert.False(Assert.IsType<TicketRow>(TicketElementMapping.ToDomain(dto)).FitStart);
    }
}
