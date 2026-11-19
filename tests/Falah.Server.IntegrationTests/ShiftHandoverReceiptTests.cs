namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.BusinessDay;
using Falah.Application.Shifts;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// P4: choosing a receiver at the close does not make the money received. The close still moves the counted cash
/// drawer -> main (so its retries and recovery are unchanged), but the handover is pending until the receiver's own
/// session confirms it or the receiver's PIN is typed at the till - never by the cashier who handed it over. A
/// different count is a recorded dispute that moves no money. The drawer opens on 100,000; nothing is sold.
/// </summary>
public class ShiftHandoverReceiptTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _cashierId = default!;
    private string _managerId = default!;
    private string _otherManagerId = default!;
    private string _shiftId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        _otherManagerId = await _factory.SeedUserAsync("مدير آخر", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
        _shiftId = (await (await _client.PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, 100_000))).Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task<CloseShiftResult> CloseToAsync(string? receiverId, string closerId = "")
    {
        var response = await _client.PostAsJsonAsync("/api/shifts/close", new CloseShiftRequest(
            $"close-{Guid.NewGuid():N}", _shiftId, closerId == "" ? _cashierId : closerId, 100_000, null, null, null, 0, receiverId));
        Assert.True(response.IsSuccessStatusCode, await response.Content.ReadAsStringAsync());
        return (await response.Content.ReadFromJsonAsync<CloseShiftResult>())!;
    }

    private Task<HttpResponseMessage> AcknowledgeAsync(string sessionUserId, long received, string? note = null, string? requestId = null,
        string? approvalId = null, string? approverId = null) =>
        _factory.PostAsSessionOfAsync(_client, sessionUserId, $"/api/shifts/{_shiftId}/handover/acknowledge",
            new AcknowledgeShiftHandoverRequest(requestId ?? $"ack-{Guid.NewGuid():N}", _shiftId, sessionUserId, received, note, approvalId, approverId));

    [Fact]
    public async Task Naming_a_receiver_leaves_the_handover_pending_until_the_receiver_confirms_it()
    {
        var close = await CloseToAsync(_managerId);
        Assert.Equal(("pending", 100_000L), (close.HandoverReceiptStatus, close.HandedOverPiastres));

        // The money movement itself is unchanged: the drawer is empty and main holds the handover.
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.Type == "shift_handover" && m.AmountPiastres == 100_000)));
        var pending = (await _client.GetFromJsonAsync<List<ShiftHandoverReceiptDto>>($"/api/shifts/handovers/pending?actorUserId={_managerId}"))!;
        Assert.Equal("pending", Assert.Single(pending).Status);

        // The control center lists it for the day.
        var dayId = await _factory.QueryAsync(db => db.BusinessDays.Where(d => d.Status == "Open").Select(d => d.Id).SingleAsync());
        var list = (await _client.GetFromJsonAsync<List<ControlExceptionDto>>($"/api/business-day/{dayId}/exceptions?actorUserId={_managerId}"))!;
        Assert.Contains(list, e => e.Id == $"handover-pending-{_shiftId}");

        // The cashier who handed it over cannot confirm it; another manager is not the receiver.
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AcknowledgeAsync(_cashierId, 100_000)).StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AcknowledgeAsync(_otherManagerId, 100_000)).StatusCode);

        var requestId = $"ack-{Guid.NewGuid():N}";
        var receipt = (await (await AcknowledgeAsync(_managerId, 100_000, requestId: requestId)).Content.ReadFromJsonAsync<ShiftHandoverReceiptDto>())!;
        Assert.Equal(("acknowledged", 0L, "المدير"), (receipt.Status, receipt.DifferencePiastres!.Value, receipt.AcknowledgedBy));
        Assert.True((await (await AcknowledgeAsync(_managerId, 100_000, requestId: requestId)).Content.ReadFromJsonAsync<ShiftHandoverReceiptDto>())!.WasReplayed);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AcknowledgeAsync(_managerId, 100_000)).StatusCode);
        Assert.Empty((await _client.GetFromJsonAsync<List<ShiftHandoverReceiptDto>>($"/api/shifts/handovers/pending?actorUserId={_managerId}"))!);
    }

    [Fact]
    public async Task The_receiver_can_confirm_at_the_till_with_a_pin_on_the_cashiers_screen()
    {
        await CloseToAsync(_managerId);

        // The other manager's PIN does not stand in for the designated receiver.
        var wrong = (await (await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_otherManagerId), "shifts_manage", "استلام"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await AcknowledgeAsync(_cashierId, 100_000, approvalId: wrong.ApprovalId, approverId: wrong.ApproverUserId)).StatusCode);

        var approval = (await (await _factory.PostAsSessionOfAsync(_client, _cashierId, "/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "shifts_manage", "استلام"))).Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;
        // A forged receiver: the receiver's real approval, but naming someone else as the approver, is refused (and not spent).
        Assert.Equal(HttpStatusCode.UnprocessableEntity,
            (await AcknowledgeAsync(_cashierId, 100_000, approvalId: approval.ApprovalId, approverId: _otherManagerId)).StatusCode);
        var receipt = (await (await AcknowledgeAsync(_cashierId, 100_000, approvalId: approval.ApprovalId, approverId: approval.ApproverUserId))
            .Content.ReadFromJsonAsync<ShiftHandoverReceiptDto>())!;
        Assert.Equal(("acknowledged", "المدير"), (receipt.Status, receipt.AcknowledgedBy));
    }

    [Fact]
    public async Task A_different_count_is_a_recorded_dispute_that_moves_no_money()
    {
        await CloseToAsync(_managerId);
        var movements = await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());

        Assert.Equal(HttpStatusCode.UnprocessableEntity, (await AcknowledgeAsync(_managerId, 95_000)).StatusCode);   // a note is required
        var receipt = (await (await AcknowledgeAsync(_managerId, 95_000, "ناقص ورقة 50")).Content.ReadFromJsonAsync<ShiftHandoverReceiptDto>())!;

        Assert.Equal(("disputed", -5_000L), (receipt.Status, receipt.DifferencePiastres!.Value));
        Assert.Equal(movements, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync()));
        Assert.True(await _factory.QueryAsync(db => db.AuditEvents.AnyAsync(a => a.ShiftId == _shiftId && a.Severity == "critical" && a.Action == "اختلاف في استلام نقدية الإغلاق")));
    }

    [Fact]
    public async Task A_manager_who_closes_and_keeps_the_cash_is_the_receiver()
    {
        var close = await CloseToAsync(_managerId, closerId: _managerId);
        Assert.Equal("acknowledged", close.HandoverReceiptStatus);
    }
}
