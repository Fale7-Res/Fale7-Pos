namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;

/// <summary>Independent audit 2026-11-23, F-05. A closer holding shifts_manage was never asked to prove the
/// approval they presented - AuthorizeCommand returns early for a permission holder - yet the shift recorded
/// the approver the request named. A made-up approval id put the owner's name on a 500 ج shortage.</summary>
public class ApprovalAttributionTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private string _ownerId = default!;
    private string _supervisorId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        Assert.StartsWith("FalahTest_", _factory.DatabaseName);
        _ownerId = await _factory.SeedUserAsync("المالك التجريبي", "owner", "all");
        _supervisorId = await _factory.SeedUserAsync("مشرف", "supervisor", "pos", "shifts_open_own", "shifts_blind_close", "shifts_manage");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<string> OpenShiftAsync()
    {
        var response = await As(_supervisorId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "وردية", _supervisorId, 50_000));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private Task<string?> AuthorizedByAsync(string shiftId) => _factory.QueryAsync(db => db.Shifts.AsNoTracking()
        .Where(s => s.Id == shiftId).Select(s => s.DifferenceAuthorizedBy).FirstOrDefaultAsync());

    [Fact]
    public async Task A_made_up_approval_from_a_permission_holder_is_refused_and_names_nobody()
    {
        var shiftId = await OpenShiftAsync();

        var close = await As(_supervisorId).PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", shiftId, _supervisorId, 0, "عجز", "approval-never-issued", _ownerId, RetainedFloatPiastres: 0));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, close.StatusCode);
        Assert.Null(await AuthorizedByAsync(shiftId));
    }

    [Fact]
    public async Task A_real_manager_approval_is_still_recorded_as_the_authoriser()
    {
        var shiftId = await OpenShiftAsync();
        var issued = await As(_supervisorId).PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_ownerId), "shifts_manage", "عجز"));
        issued.EnsureSuccessStatusCode();
        var approval = (await issued.Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;

        var close = await As(_supervisorId).PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", shiftId, _supervisorId, 0, "عجز", approval.ApprovalId, approval.ApproverUserId, RetainedFloatPiastres: 0));

        close.EnsureSuccessStatusCode();
        Assert.Equal("المالك التجريبي", await AuthorizedByAsync(shiftId));
    }
}
