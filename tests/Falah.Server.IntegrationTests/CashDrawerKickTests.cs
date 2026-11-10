namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.CashDrawer;
using Falah.Application.Shifts;
using Falah.PrintAgent;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// HW1. "Open drawer" was recorded and audited but nothing ever opened a drawer. The print station
/// wired to the till's drawer now collects the request and sends its printer the drawer pulse - once,
/// to one station, and never late: a request not collected within the limit expires instead of
/// springing the drawer open minutes later. What the printer cannot confirm (that the drawer actually
/// opened) is never claimed: "Succeeded" means the pulse was accepted.
/// </summary>
public class CashDrawerKickTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _agentId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "cash_drawer");
        _agentId = await _factory.SeedUserAsync("محطة الكاشير", "print_agent", "print_agent");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 20000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<string> AskToOpenAsync() =>
        (await (await _client.PostAsJsonAsync("/api/cash-drawer/open",
            new OpenCashDrawerRequest($"req-{Guid.NewGuid():N}", _shiftId, "فكة للعميل", _cashierId, null, null)))
            .Content.ReadFromJsonAsync<CashDrawerOpenDto>())!.Id;

    private async Task<PrintServerClient> StationAsync(string agentId)
    {
        var station = new PrintServerClient(_factory.CreateClient(), agentId);
        await station.AuthenticateAsync(_factory.PinFor(_agentId), CancellationToken.None);
        return station;
    }

    private Task<string> StatusAsync(string id) =>
        _factory.QueryAsync(db => db.CashDrawerOpens.AsNoTracking().Where(c => c.Id == id).Select(c => c.Status).FirstAsync());

    private sealed class RecordingTransport : ITicketTransport
    {
        public List<byte[]> Sent { get; } = new();
        public Exception? Fail { get; init; }
        public string Description => "recording";
        public Task SendAsync(byte[] bytes, CancellationToken ct)
        {
            if (Fail is not null) throw Fail;
            Sent.Add(bytes);
            return Task.CompletedTask;
        }
    }

    [Fact]
    public async Task A_request_to_open_the_drawer_reaches_one_station_once()
    {
        var id = await AskToOpenAsync();
        using var counter = await StationAsync("counter-pc");
        using var kitchen = await StationAsync("kitchen-pc");

        var both = await Task.WhenAll(
            counter.ClaimCashDrawerKicksAsync(CancellationToken.None),
            kitchen.ClaimCashDrawerKicksAsync(CancellationToken.None));

        Assert.Equal(1, both[0].Count + both[1].Count);
        Assert.Equal("Dispatched", await StatusAsync(id));
        Assert.Empty(await counter.ClaimCashDrawerKicksAsync(CancellationToken.None));
    }

    [Fact]
    public async Task A_request_nobody_collected_in_time_expires_and_is_never_sent()
    {
        var id = await AskToOpenAsync();
        await _factory.QueryAsync(db => db.Database.ExecuteSqlRawAsync(
            "UPDATE CashDrawerOpens SET RequestedAtUtc = DATEADD(minute, -10, SYSDATETIMEOFFSET()) WHERE Id = {0}", id));
        using var station = await StationAsync("counter-pc");

        Assert.Empty(await station.ClaimCashDrawerKicksAsync(CancellationToken.None));
        Assert.Equal("Expired", await StatusAsync(id));

        // A late report cannot make it look sent.
        await station.ReportCashDrawerResultAsync(new ReportCashDrawerResultRequest(id, true, null, _agentId), CancellationToken.None);
        Assert.Equal("Expired", await StatusAsync(id));
    }

    [Fact]
    public async Task A_till_cannot_collect_drawer_requests()
    {
        await AskToOpenAsync();
        var refused = await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/cash-drawer/claim", new ClaimCashDrawerKicksRequest("till"));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, refused.StatusCode);
    }

    [Fact]
    public async Task The_station_sends_the_configured_pulse_and_reports_it_sent()
    {
        var id = await AskToOpenAsync();
        using var station = await StationAsync("counter-pc");
        var printer = new RecordingTransport();
        var pulse = new AgentOptions { CashDrawerKickBytes = "1B 70 01 19 FA" }.ResolveCashDrawerKick();

        var sent = await new CashDrawerKicker(station, printer, pulse, station.UserId!, _ => { }).DrainAsync(CancellationToken.None);

        Assert.Equal(1, sent);
        Assert.Equal(new byte[] { 0x1B, 0x70, 0x01, 0x19, 0xFA }, Assert.Single(printer.Sent));
        Assert.Equal("Succeeded", await StatusAsync(id));
    }

    [Fact]
    public async Task A_pulse_the_printer_refuses_is_reported_failed_with_the_reason()
    {
        var id = await AskToOpenAsync();
        using var station = await StationAsync("counter-pc");
        var printer = new RecordingTransport { Fail = new IOException("printer offline") };

        await new CashDrawerKicker(station, printer, new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA }, station.UserId!, _ => { })
            .DrainAsync(CancellationToken.None);

        var command = await _factory.QueryAsync(db => db.CashDrawerOpens.AsNoTracking().FirstAsync(c => c.Id == id));
        Assert.Equal("Failed", command.Status);
        Assert.Contains("printer offline", command.LastError);
    }

    [Theory]
    [InlineData("1B 70 00 19 FA", new byte[] { 0x1B, 0x70, 0x00, 0x19, 0xFA })]
    [InlineData("0x10,0x14,0x01,0x00,0x05", new byte[] { 0x10, 0x14, 0x01, 0x00, 0x05 })]
    public void The_pulse_is_configured_as_hex_bytes(string configured, byte[] expected) =>
        Assert.Equal(expected, new AgentOptions { CashDrawerKickBytes = configured }.ResolveCashDrawerKick());

    [Fact]
    public void A_station_with_the_drawer_switched_on_and_unreadable_bytes_does_not_start()
    {
        var problems = new AgentOptions
        {
            Route = "Receipt", Pin = "123456", Transport = "file", CashDrawerKickEnabled = true, CashDrawerKickBytes = "ESC p 0",
        }.Validate();
        Assert.Contains(problems, p => p.Contains("CashDrawerKickBytes"));
    }
}
