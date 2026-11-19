using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Printing;
using Falah.Application.Users;

namespace Falah.PrintAgent;

/// <summary>The four server calls a print agent makes. Split behind an interface so the job loop
/// can be tested without a server, and so the agent never grows a second way to reach the
/// database - it is an HTTP client like every other station process.</summary>
public interface IPrintServerClient
{
    Task AuthenticateAsync(string pin, CancellationToken ct);
    Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct);
    Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct);
    Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct);
    Task<bool> StartSendingAsync(PrintJobDto job, CancellationToken ct) => Task.FromResult(true);

    /// <summary>HW1: the open-drawer commands to send now. A client without drawer support has none.</summary>
    Task<IReadOnlyList<Falah.Application.CashDrawer.CashDrawerOpenDto>> ClaimCashDrawerKicksAsync(CancellationToken ct) =>
        Task.FromResult<IReadOnlyList<Falah.Application.CashDrawer.CashDrawerOpenDto>>(Array.Empty<Falah.Application.CashDrawer.CashDrawerOpenDto>());

    Task ReportCashDrawerResultAsync(Falah.Application.CashDrawer.ReportCashDrawerResultRequest request, CancellationToken ct) =>
        Task.CompletedTask;
}

/// <summary>Every request needs a Bearer session token now (see AuthenticationMiddleware on the
/// server) - a real gap this agent had until it was actually run against a live server for the
/// first time: it never logged in at all. AuthenticateAsync logs in once and the token is reused
/// for the life of the process; the server's own sliding session expiry means a poll loop hitting
/// it every few seconds never lets that session go idle.</summary>
public sealed class PrintServerClient : IPrintServerClient, IDisposable
{
    private readonly HttpClient _http;

    /// <summary>Who this agent is when it claims jobs - unique per machine and route, so two agents
    /// serving one route from two machines never both take the same job. Null means the agent does
    /// not claim and reads the pending queue as older agents did.</summary>
    private readonly string _agentId;
    private readonly Dictionary<string, string> _claims = new();

    public PrintServerClient(string baseAddress, TimeSpan timeout, string? agentId = null, TimeProvider? time = null, bool allowInsecureRemoteHttp = false)
    {
        _http = new HttpClient { BaseAddress = Falah.Application.Security.ServerEndpointPolicy.RequireSecureAddress(baseAddress, allowInsecureRemoteHttp), Timeout = timeout };
        _agentId = agentId ?? $"agent-{Guid.NewGuid():N}";
        _time = time ?? TimeProvider.System;
    }

    /// <summary>Over an HttpClient that already has its base address - the server's own test host.</summary>
    public PrintServerClient(HttpClient http, string? agentId = null, TimeProvider? time = null)
    {
        _http = http;
        _agentId = agentId ?? $"agent-{Guid.NewGuid():N}";
        _time = time ?? TimeProvider.System;
    }

    private readonly TimeProvider _time;

    /// <summary>The PIN the agent logged in with, kept so an ended session can be renewed without a
    /// restart. Held in memory only, exactly where the configuration already put it.</summary>
    private string? _pin;

    /// <summary>Who the agent is logged in as - the user it reports print attempts as (the server
    /// accepts no other).</summary>
    public string? UserId { get; private set; }
    public string? UserName { get; private set; }

    /// <summary>How long to leave login alone after the server refused the agent's PIN.</summary>
    public static readonly TimeSpan RefusedLoginPause = TimeSpan.FromMinutes(5);

    // The server counts wrong PINs per machine; an agent polling every few seconds with a PIN that no
    // longer works would spend that allowance in seconds and stop the till on the same machine from
    // logging in. After a refusal it waits before asking again (longer if the server says so).
    private DateTimeOffset _noLoginBefore = DateTimeOffset.MinValue;

    public async Task AuthenticateAsync(string pin, CancellationToken ct)
    {
        var now = _time.GetUtcNow();
        if (now < _noLoginBefore)
            throw new HttpRequestException($"login refused earlier; not retrying until {_noLoginBefore.ToLocalTime():HH:mm:ss}.",
                inner: null, System.Net.HttpStatusCode.Unauthorized);

        using var response = await _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct);
        if (!response.IsSuccessStatusCode)
        {
            var pause = response.Headers.RetryAfter?.Delta is { } serverSays && serverSays > RefusedLoginPause ? serverSays : RefusedLoginPause;
            _noLoginBefore = now + pause;
            throw new HttpRequestException($"login refused ({(int)response.StatusCode}); next attempt after {_noLoginBefore.ToLocalTime():HH:mm:ss}.",
                inner: null, response.StatusCode);
        }
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Login response was empty.");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
        _pin = pin;
        UserId = result.Id;
        UserName = result.Name;
    }

    /// <summary>The jobs to print now. An agent with an identity CLAIMS them (the server hands each
    /// job to one agent at a time, for a lease), so agents for one route on several machines never
    /// print the same ticket; without one it reads the pending queue as agents before claiming did.</summary>
    public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)
    {
        using var response = await SendAsync(() => _http.PostAsJsonAsync("/api/print-jobs/claim", new ClaimPrintJobsRequest(route, _agentId), ct), ct);
        response.EnsureSuccessStatusCode();
        var jobs = await response.Content.ReadFromJsonAsync<List<PrintJobDto>>(cancellationToken: ct) ?? new List<PrintJobDto>();
        foreach (var job in jobs) if (job.ClaimId is { } claim) _claims[job.Id] = claim;
        return jobs;
    }

    public async Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct)
    {
        using var response = await SendAsync(() => _http.GetAsync($"/api/print-jobs/{Uri.EscapeDataString(jobId)}/ticket", ct), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<ComposedTicketDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException($"Ticket content for job {jobId} was empty.");
    }

    public async Task<bool> StartSendingAsync(PrintJobDto job, CancellationToken ct)
    {
        var claim = job.ClaimId ?? _claims.GetValueOrDefault(job.Id);
        if (claim is null) return false;
        using var response = await SendAsync(() => _http.PostAsJsonAsync($"/api/print-jobs/{Uri.EscapeDataString(job.Id)}/start",
            new StartPrintJobRequest(job.Id, claim), ct), ct);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<StartPrintJobResult>(cancellationToken: ct))?.Started == true;
    }

    public async Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)
    {
        request = request with { ClaimId = request.ClaimId ?? _claims.GetValueOrDefault(request.PrintJobId) };
        using var response = await SendAsync(() => _http.PostAsJsonAsync(
            $"/api/print-jobs/{Uri.EscapeDataString(request.PrintJobId)}/report-result", request, ct), ct);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IReadOnlyList<Falah.Application.CashDrawer.CashDrawerOpenDto>> ClaimCashDrawerKicksAsync(CancellationToken ct)
    {
        using var response = await SendAsync(() => _http.PostAsJsonAsync("/api/cash-drawer/claim",
            new Falah.Application.CashDrawer.ClaimCashDrawerKicksRequest(_agentId ?? Environment.MachineName), ct), ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<Falah.Application.CashDrawer.CashDrawerOpenDto>>(cancellationToken: ct)
            ?? new List<Falah.Application.CashDrawer.CashDrawerOpenDto>();
    }

    public async Task ReportCashDrawerResultAsync(Falah.Application.CashDrawer.ReportCashDrawerResultRequest request, CancellationToken ct)
    {
        using var response = await SendAsync(() => _http.PostAsJsonAsync("/api/cash-drawer/report-result", request, ct), ct);
        response.EnsureSuccessStatusCode();
    }

    /// <summary>
    /// Sends once; if the server no longer recognises the session (401), logs in again with the same
    /// PIN and sends once more. Sessions end - an idle expiry while the server or the machine was
    /// off, a logout, a deactivate/reactivate - and an agent that kept presenting the dead token
    /// polled into 401s until someone restarted it. Exactly one retry: a PIN that no longer logs in
    /// fails the login itself, loudly, rather than looping. Every call here is safe to send twice: the
    /// two reads change nothing, and a result report carries its own idempotent attempt id.
    /// </summary>
    private async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        var response = await send();
        if (response.StatusCode != System.Net.HttpStatusCode.Unauthorized || _pin is null) return response;

        response.Dispose();
        await AuthenticateAsync(_pin, ct);
        return await send();
    }

    public void Dispose() => _http.Dispose();
}
