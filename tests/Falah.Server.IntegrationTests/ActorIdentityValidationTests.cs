namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Falah.Application.Approvals;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Application.Users;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>
/// The session is the identity, whatever the request body says - and however it spells the field.
///
/// Two gaps let a cashier's session act under a manager's name. Closing a shift named its closer in
/// ClosedByUserId, a field the middleware never compared with the session, so a cashier could close
/// a shift that the record, the audit trail and the handover slip all attribute to the manager. And the comparison
/// matched field names exactly, while the endpoint binds them case-insensitively: "CreatedByUserId"
/// was never looked at, and still bound.
///
/// Every request here goes out on a raw client carrying one explicit session - never through
/// ActorTokenHandler - so nothing on the test side can choose the "right" token and hide a mismatch.
/// </summary>
public class ActorIdentityValidationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);

    /// <summary>50000 opened and nothing sold: this count is exactly what the drawer should hold.</summary>
    private const long ExactCount = 50000;

    /// <summary>A 10000-piastre shortage - twice the 5000-piastre default threshold, so the close
    /// needs shifts_manage or a manager's approval.</summary>
    private const long ShortCount = 40000;

    private FalahApiFactory _factory = default!;
    private string _cashierId = default!;
    private string _managerId = default!;

    public async Task InitializeAsync()
    {
        // The automatic backup is off: these hosts live for seconds, and a test database has no
        // business appearing in the restaurant's real backup folder.
        _factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["Backup:Automatic:Enabled"] = "false",
        });

        // Before anything is written: this class must be talking to its own throwaway database.
        var database = await _factory.QueryAsync(db => Task.FromResult(db.Database.GetDbConnection().Database));
        Assert.StartsWith("FalahTest_", database);

        _cashierId = await _factory.SeedUserAsync("كاشير", "cashier", "pos", "shifts_open_own", "shifts_blind_close");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        await _factory.SeedOpenBusinessDayWithFundedMainAsync();
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    // ---------- Impersonation through ClosedByUserId ----------

    [Fact]
    public async Task A_cashier_cannot_close_a_shift_as_the_manager()
    {
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/close", Close(shiftId, _managerId, ShortCount));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillOpenAndUntouchedAsync(shiftId);
    }

    [Fact]
    public async Task A_cashier_cannot_close_a_shift_as_the_manager_even_when_the_count_is_exact()
    {
        // Identity, not the size of the discrepancy: the shift must not be recorded as the
        // manager's close however small the difference.
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/close", Close(shiftId, _managerId, ExactCount));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillOpenAndUntouchedAsync(shiftId);
    }

    // ---------- The same field, spelled another way ----------

    [Theory]
    [InlineData("ClosedByUserId")]
    [InlineData("CLOSEDBYUSERID")]
    [InlineData("closedbyuserid")]
    public async Task The_closer_cannot_be_respelled_past_the_check(string sentAs)
    {
        var shiftId = await OpenShiftAsync();

        // An exact count, so nothing downstream refuses the close: only the identity check can.
        var response = await As(_cashierId).PostAsync("/api/shifts/close",
            WithRenamedField(Close(shiftId, _managerId, ExactCount), "closedByUserId", sentAs));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillOpenAndUntouchedAsync(shiftId);
    }

    [Theory]
    [InlineData("CreatedByUserId")]
    [InlineData("CREATEDBYUSERID")]
    public async Task A_field_already_checked_cannot_be_respelled_past_the_check(string sentAs)
    {
        var movementsBefore = await TreasuryMovementCountAsync();

        var response = await As(_cashierId).PostAsync("/api/treasury/withdraw",
            WithRenamedField(Withdrawal(_managerId), "createdByUserId", sentAs));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(movementsBefore, await TreasuryMovementCountAsync());
    }

    [Fact]
    public async Task A_cashier_cannot_grant_themselves_a_permission_by_respelling_the_actor()
    {
        var response = await As(_cashierId).PostAsync($"/api/users/{_cashierId}/permissions",
            WithRenamedField(new UpdateUserPermissionsRequest(_cashierId, new[] { "users_manage" }, Array.Empty<string>(), _managerId),
                "actorUserId", "ActorUserId"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        var permissions = await _factory.QueryAsync(db =>
            db.Users.AsNoTracking().Where(u => u.Id == _cashierId).Select(u => u.Permissions).FirstAsync());
        Assert.DoesNotContain("users_manage", permissions.Split(','));
    }

    // ---------- A second copy of the field ----------

    [Theory]
    [InlineData("ClosedByUserId", false)]
    [InlineData("ClosedByUserId", true)]
    [InlineData("closedByUserId", false)]
    public async Task A_second_copy_of_the_closer_cannot_override_the_honest_one(string sentAs, bool first)
    {
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsync("/api/shifts/close",
            WithExtraField(Close(shiftId, _cashierId, ExactCount), sentAs, _managerId, first));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        await AssertStillOpenAndUntouchedAsync(shiftId);
    }

    [Theory]
    [InlineData("CreatedByUserId", false)]
    [InlineData("CreatedByUserId", true)]
    [InlineData("createdByUserId", false)]
    public async Task A_second_copy_of_a_checked_field_cannot_override_the_honest_one(string sentAs, bool first)
    {
        var movementsBefore = await TreasuryMovementCountAsync();

        var response = await As(_cashierId).PostAsync("/api/treasury/withdraw",
            WithExtraField(Withdrawal(_cashierId), sentAs, _managerId, first));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(movementsBefore, await TreasuryMovementCountAsync());
    }

    // ---------- Honest work still goes through ----------

    [Fact]
    public async Task A_cashier_closes_their_own_shift_and_is_recorded_as_the_closer()
    {
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/close", Close(shiftId, _cashierId, ExactCount));

        response.EnsureSuccessStatusCode();
        var shift = await ShiftAsync(shiftId);
        Assert.Equal("Closed", shift.Status);
        Assert.Equal(_cashierId, shift.ClosedByUserId);
    }

    [Fact]
    public async Task Naming_yourself_in_another_casing_is_not_mistaken_for_impersonation()
    {
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsync("/api/shifts/close",
            WithRenamedField(Close(shiftId, _cashierId, ExactCount), "closedByUserId", "ClosedByUserId"));

        response.EnsureSuccessStatusCode();
        Assert.Equal(_cashierId, (await ShiftAsync(shiftId)).ClosedByUserId);
    }

    [Fact]
    public async Task A_cashier_over_the_threshold_without_approval_is_refused_for_the_real_reason()
    {
        // Authenticated as themselves, so this reaches the approval gate and fails THERE - a 422,
        // not the 403 an impersonation gets.
        var shiftId = await OpenShiftAsync();

        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/close", Close(shiftId, _cashierId, ShortCount));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await AssertStillOpenAndUntouchedAsync(shiftId);
    }

    [Fact]
    public async Task A_cashier_over_the_threshold_closes_with_the_managers_pin_approval()
    {
        // The manager types their PIN on the cashier's screen: the session stays the cashier's, and
        // ManagerApproverUserId names a second person - it must not be treated as the actor.
        var shiftId = await OpenShiftAsync();
        var cashier = As(_cashierId);
        var issued = await cashier.PostAsJsonAsync("/api/approvals/issue",
            new IssueManagerApprovalRequest(_factory.PinFor(_managerId), "shifts_manage", "فرق كبير"));
        issued.EnsureSuccessStatusCode();
        var approval = (await issued.Content.ReadFromJsonAsync<IssueManagerApprovalResult>())!;

        var response = await cashier.PostAsJsonAsync("/api/shifts/close",
            Close(shiftId, _cashierId, ShortCount, approval.ApprovalId, _managerId));

        response.EnsureSuccessStatusCode();
        var shift = await ShiftAsync(shiftId);
        Assert.Equal("Closed", shift.Status);
        Assert.Equal(_cashierId, shift.ClosedByUserId);
        Assert.Equal("المدير", shift.DifferenceAuthorizedBy);
    }

    [Fact]
    public async Task A_manager_closes_their_own_shift_and_is_recorded_as_the_closer()
    {
        var shiftId = await OpenShiftAsync();

        var response = await As(_managerId).PostAsJsonAsync("/api/shifts/close", Close(shiftId, _managerId, ExactCount));

        response.EnsureSuccessStatusCode();
        var shift = await ShiftAsync(shiftId);
        Assert.Equal("Closed", shift.Status);
        Assert.Equal(_managerId, shift.ClosedByUserId);
    }

    [Fact]
    public async Task Even_a_manager_needs_an_approval_on_record_for_an_over_threshold_close()
    {
        // Shift.Close refuses an over-threshold close with no approver named, whoever closes - so
        // holding shifts_manage is not enough on its own. This fix must leave that untouched.
        var shiftId = await OpenShiftAsync();

        var response = await As(_managerId).PostAsJsonAsync("/api/shifts/close", Close(shiftId, _managerId, ShortCount));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        await AssertStillOpenAndUntouchedAsync(shiftId);
    }

    // ---------- Keeping the list complete ----------

    [Fact]
    public void Every_request_field_that_names_the_acting_user_is_checked_against_the_session()
    {
        // A new "...ByUserId" field is exactly how ClosedByUserId came to be missed.
        var checkedFields = AuthenticationMiddleware.ActorFields;

        // A second person, proven by their own PIN through an approval - or the user an action is
        // about rather than the one performing it.
        var notTheActor = new[] { "ManagerApproverUserId", "CompApproverUserId", "UserId" };

        var unverified = typeof(CloseShiftRequest).Assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Request", StringComparison.Ordinal))
            .SelectMany(t => t.GetProperties().Select(p => (Type: t, Property: p)))
            .Where(x => x.Property.PropertyType == typeof(string))
            .Where(x => x.Property.Name.EndsWith("UserId", StringComparison.Ordinal)
                        || x.Property.Name.EndsWith("ById", StringComparison.Ordinal)
                        || x.Property.Name == "CashierId")
            .Where(x => !notTheActor.Contains(x.Property.Name))
            .Where(x => !checkedFields.Contains(x.Property.Name, StringComparer.OrdinalIgnoreCase))
            .Select(x => $"{x.Type.Name}.{x.Property.Name}")
            .Distinct()
            .ToList();

        Assert.Empty(unverified);
    }

    // ---------- Helpers ----------

    private HttpClient As(string userId)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _factory.TokenFor(userId));
        return client;
    }

    private async Task<string> OpenShiftAsync()
    {
        var response = await As(_cashierId).PostAsJsonAsync("/api/shifts/open",
            new OpenShiftRequest($"open-{Guid.NewGuid():N}", "صباحية", _cashierId, ExactCount));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<OpenShiftResult>())!.ShiftId;
    }

    private static CloseShiftRequest Close(string shiftId, string closedBy, long counted,
        string? approvalId = null, string? approverId = null) =>
        new($"creq-{Guid.NewGuid():N}", shiftId, closedBy, counted, "فرق في العد", approvalId, approverId);

    private static WithdrawalRequest Withdrawal(string createdBy) =>
        new($"wd-{Guid.NewGuid():N}", 100, "tre-main", "سحب", null, createdBy, null, null);

    private Task<ShiftEntity> ShiftAsync(string id) =>
        _factory.QueryAsync(db => db.Shifts.AsNoTracking().FirstAsync(s => s.Id == id));

    private Task<int> TreasuryMovementCountAsync() =>
        _factory.QueryAsync(db => db.TreasuryMovements.CountAsync());

    private async Task AssertStillOpenAndUntouchedAsync(string shiftId)
    {
        Assert.Equal("Open", (await ShiftAsync(shiftId)).Status);
        // The opening float alone: a refused close posts neither a reconciliation nor a handover.
        Assert.Equal(1, await _factory.QueryAsync(db => db.TreasuryMovements.CountAsync(m => m.ShiftId == shiftId)));
    }

    /// <summary>The body a client would send, with one field renamed - the only way to produce a
    /// spelling no serializer in this codebase ever would.</summary>
    private static StringContent WithRenamedField<T>(T request, string field, string sentAs)
    {
        var source = JsonSerializer.SerializeToNode(request, Web)!.AsObject();
        var renamed = new JsonObject();
        foreach (var pair in source)
            renamed[pair.Key == field ? sentAs : pair.Key] = pair.Value?.DeepClone();
        return Json(renamed.ToJsonString());
    }

    /// <summary>The body with a second copy of a field spliced in, before or after the honest one.
    /// Spliced as text because JsonObject itself refuses duplicate keys.</summary>
    private static StringContent WithExtraField<T>(T request, string sentAs, string value, bool first)
    {
        var json = JsonSerializer.Serialize(request, Web);
        var extra = $"\"{sentAs}\":{JsonSerializer.Serialize(value)}";
        json = first ? "{" + extra + "," + json[1..] : json[..^1] + "," + extra + "}";
        return Json(json);
    }

    private static StringContent Json(string json) => new(json, Encoding.UTF8, "application/json");
}
