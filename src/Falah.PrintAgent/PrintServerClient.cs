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
}

/// <summary>Every request needs a Bearer session token now (see AuthenticationMiddleware on the
/// server) - a real gap this agent had until it was actually run against a live server for the
/// first time: it never logged in at all. AuthenticateAsync logs in once and the token is reused
/// for the life of the process; the server's own sliding session expiry means a poll loop hitting
/// it every few seconds never lets that session go idle.</summary>
public sealed class PrintServerClient : IPrintServerClient, IDisposable
{
    private readonly HttpClient _http;

    public PrintServerClient(string baseAddress, TimeSpan timeout)
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = timeout };
    }

    public async Task AuthenticateAsync(string pin, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken: ct)
            ?? throw new InvalidOperationException("Login response was empty.");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
    }

    public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)
    {
        var jobs = await _http.GetFromJsonAsync<List<PrintJobDto>>(
            $"/api/print-jobs/pending?route={Uri.EscapeDataString(route)}", ct);
        return jobs ?? new List<PrintJobDto>();
    }

    public async Task<ComposedTicketDto> GetTicketAsync(string jobId, CancellationToken ct)
    {
        var ticket = await _http.GetFromJsonAsync<ComposedTicketDto>(
            $"/api/print-jobs/{Uri.EscapeDataString(jobId)}/ticket", ct);
        return ticket ?? throw new InvalidOperationException($"Ticket content for job {jobId} was empty.");
    }

    public async Task ReportResultAsync(ReportPrintJobResultRequest request, CancellationToken ct)
    {
        var response = await _http.PostAsJsonAsync(
            $"/api/print-jobs/{Uri.EscapeDataString(request.PrintJobId)}/report-result", request, ct);
        response.EnsureSuccessStatusCode();
    }

    public void Dispose() => _http.Dispose();
}
