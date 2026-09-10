namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.BusinessDay;
using Falah.Application.Users;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// New in this pass - the reference app has NO user-creation command at all (only `updateUser`,
/// which maps an existing id and cannot create one), no PIN hashing anywhere (`user.pin === pin`,
/// plain string equality), and a real, documented owner-immutability gap (`role === 'owner'` is only
/// checked inside one UI click handler, never at the command level - see UserManagementRules' own
/// doc comment). This slice: hashes PINs (PBKDF2-SHA256, per-user salt), replaces plaintext
/// comparison everywhere, and builds real create/deactivate/reactivate/change-pin/permission
/// management commands with owner-immutability and privilege-escalation guards enforced server-side,
/// plus full auditability via ControlAuditEventEntity.
/// </summary>
public class UserManagementVerticalSliceTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;
    private string _ownerId = default!;
    private string _managerId = default!;
    private string _limitedAdminId = default!;

    public async Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateClient();
        _ownerId = await _factory.SeedUserAsync("المالك", "owner", "all");
        _managerId = await _factory.SeedUserAsync("المدير", "manager", "all");
        // Holds users_manage but only a couple of OTHER permissions - used to prove privilege
        // escalation is blocked (cannot grant what it does not itself hold).
        _limitedAdminId = await _factory.SeedUserAsync("مسؤول محدود", "supervisor", "users_manage", "menu_manage");
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private Task<HttpResponseMessage> CreateUserAsync(string name, string role, string pin, IReadOnlyList<string> permissions, string actorId, string? requestId = null) =>
        _client.PostAsJsonAsync("/api/users", new CreateUserRequest(requestId ?? $"req-{Guid.NewGuid():N}", name, role, pin, permissions, actorId));

    private Task<HttpResponseMessage> LoginAsync(string pin) =>
        _client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin));

    private Task<HttpResponseMessage> SetActiveAsync(string userId, bool active, string actorId, string? reason = null) =>
        _client.PostAsJsonAsync($"/api/users/{userId}/active", new SetUserActiveRequest(userId, active, reason, actorId));

    private Task<HttpResponseMessage> ChangePinAsync(string userId, string? currentPin, string newPin, string actorId) =>
        _client.PostAsJsonAsync($"/api/users/{userId}/change-pin", new ChangePinRequest(userId, currentPin, newPin, actorId));

    private Task<HttpResponseMessage> UpdatePermissionsAsync(string userId, IReadOnlyList<string> grant, IReadOnlyList<string> revoke, string actorId) =>
        _client.PostAsJsonAsync($"/api/users/{userId}/permissions", new UpdateUserPermissionsRequest(userId, grant, revoke, actorId));

    // ---- Create ----

    [Fact]
    public async Task Creating_a_user_with_valid_data_succeeds_and_the_new_pin_logs_in()
    {
        var response = await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<CreateUserResult>();
        Assert.False(result!.WasReplayed);

        var login = await LoginAsync("4715");
        login.EnsureSuccessStatusCode();
        var user = await login.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.Equal("كاشير جديد", user!.Name);
    }

    [Fact]
    public async Task The_created_users_pin_is_never_stored_in_plain_text()
    {
        var response = await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId);
        var result = await response.Content.ReadFromJsonAsync<CreateUserResult>();

        var entity = await _factory.QueryAsync(db => db.Users.AsNoTracking().FirstAsync(u => u.Id == result!.UserId));
        Assert.NotEqual("4715", entity.PinHash);
        Assert.NotEmpty(entity.PinSalt);
        Assert.True(entity.PinIterations > 0);
    }

    [Fact]
    public async Task Creating_a_user_with_role_owner_is_rejected()
    {
        var response = await CreateUserAsync("مالك آخر", "owner", "5813", new[] { "all" }, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_user_without_users_manage_is_rejected()
    {
        var noPermissionUser = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var response = await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, noPermissionUser);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_user_cannot_grant_a_permission_the_actor_does_not_hold()
    {
        // _limitedAdminId holds users_manage + menu_manage only - cannot grant treasury_manage.
        var response = await CreateUserAsync("مدير خزينة", "supervisor", "6284", new[] { "treasury_manage" }, _limitedAdminId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_user_can_grant_a_permission_the_actor_holds_themselves()
    {
        var response = await CreateUserAsync("مسؤول قوائم", "supervisor", "6284", new[] { "menu_manage" }, _limitedAdminId);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Creating_a_user_with_an_unknown_permission_string_is_rejected()
    {
        var response = await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "not_a_real_permission" }, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_user_with_a_pin_already_in_use_by_another_active_user_is_rejected()
    {
        (await CreateUserAsync("كاشير أول", "cashier", "4715", new[] { "pos" }, _managerId)).EnsureSuccessStatusCode();
        var response = await CreateUserAsync("كاشير ثاني", "cashier", "4715", new[] { "pos" }, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Creating_a_user_with_a_weak_repeated_digit_pin_is_rejected()
    {
        var response = await CreateUserAsync("كاشير جديد", "cashier", "7777", new[] { "pos" }, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Retrying_the_same_create_user_request_id_replays_not_duplicates()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var first = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId, requestId)).Content.ReadFromJsonAsync<CreateUserResult>();
        var second = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId, requestId)).Content.ReadFromJsonAsync<CreateUserResult>();
        Assert.False(first!.WasReplayed);
        Assert.True(second!.WasReplayed);
        Assert.Equal(first.UserId, second.UserId);
    }

    [Fact]
    public async Task Concurrent_create_user_requests_with_the_same_request_id_create_exactly_one_user()
    {
        var requestId = $"req-{Guid.NewGuid():N}";
        var tasks = Enumerable.Range(0, 6).Select(_ => CreateUserAsync("كاشير متزامن", "cashier", "4715", new[] { "pos" }, _managerId, requestId));
        var responses = await Task.WhenAll(tasks);

        Assert.All(responses, r => Assert.True(r.IsSuccessStatusCode, $"unexpected status {r.StatusCode}"));
        var results = await Task.WhenAll(responses.Select(r => r.Content.ReadFromJsonAsync<CreateUserResult>()));
        Assert.Equal(1, results.Count(r => !r!.WasReplayed));
        var userId = results.First()!.UserId;
        var count = await _factory.QueryAsync(db => db.Users.CountAsync(u => u.Id == userId));
        Assert.Equal(1, count);
    }

    // ---- Deactivate / reactivate ----

    [Fact]
    public async Task Deactivating_a_user_prevents_further_login()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await SetActiveAsync(created!.UserId, false, _managerId, "استقالة")).EnsureSuccessStatusCode();

        var login = await LoginAsync("4715");
        Assert.Equal(HttpStatusCode.Unauthorized, login.StatusCode);
    }

    [Fact]
    public async Task Reactivating_a_user_restores_login()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await SetActiveAsync(created!.UserId, false, _managerId)).EnsureSuccessStatusCode();
        (await SetActiveAsync(created.UserId, true, _managerId)).EnsureSuccessStatusCode();

        var login = await LoginAsync("4715");
        login.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Deactivating_the_owner_is_rejected_even_by_a_users_manage_holder()
    {
        var response = await SetActiveAsync(_ownerId, false, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_actor_cannot_deactivate_their_own_account()
    {
        var response = await SetActiveAsync(_managerId, false, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Deactivating_an_already_inactive_user_is_a_harmless_no_op()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await SetActiveAsync(created!.UserId, false, _managerId)).EnsureSuccessStatusCode();
        (await SetActiveAsync(created.UserId, false, _managerId)).EnsureSuccessStatusCode();
    }

    // ---- Change PIN ----

    [Fact]
    public async Task Self_service_pin_change_with_the_correct_current_pin_succeeds()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await ChangePinAsync(created!.UserId, "4715", "8264", created.UserId)).EnsureSuccessStatusCode();

        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync("4715")).StatusCode);
        (await LoginAsync("8264")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Self_service_pin_change_with_the_wrong_current_pin_is_rejected()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        var response = await ChangePinAsync(created!.UserId, "0000", "8264", created.UserId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_admin_can_reset_someone_elses_pin_without_knowing_the_current_one()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await ChangePinAsync(created!.UserId, currentPin: null, "8264", _managerId)).EnsureSuccessStatusCode();
        (await LoginAsync("8264")).EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task An_admin_cannot_reset_the_owners_pin()
    {
        var response = await ChangePinAsync(_ownerId, currentPin: null, "8264", _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task The_owner_can_change_their_own_pin()
    {
        // The owner was seeded directly in the DB, so its real pin is unknown to this test - reset
        // it once as an admin acting AS the owner (ActorUserId == UserId still requires CurrentPin,
        // so seed a known one first via the admin path is not applicable here; instead verify the
        // owner path is not blocked by AssertTargetIsNotOwner at all when acting on themselves).
        var ownerPin = await _factory.QueryAsync(async db =>
        {
            var owner = await db.Users.FirstAsync(u => u.Id == _ownerId);
            var hashed = Falah.Domain.Authorization.PinHasher.Hash("6031");
            owner.PinHash = hashed.HashBase64; owner.PinSalt = hashed.SaltBase64; owner.PinIterations = hashed.Iterations;
            await db.SaveChangesAsync();
            return "6031";
        });
        var response = await ChangePinAsync(_ownerId, ownerPin, "7402", _ownerId);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Changing_a_pin_to_one_already_in_use_is_rejected()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        var response = await ChangePinAsync(created!.UserId, "4715", "9876", _managerId); // 9876 unused - collides with nothing; use a real collision instead
        response.EnsureSuccessStatusCode();

        var second = await (await CreateUserAsync("كاشير آخر", "cashier", "5142", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        var collision = await ChangePinAsync(second!.UserId, "5142", "9876", second.UserId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, collision.StatusCode);
    }

    // ---- Permissions ----

    [Fact]
    public async Task Granting_a_permission_the_actor_holds_succeeds_and_is_usable()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await UpdatePermissionsAsync(created!.UserId, new[] { "orders_refund" }, Array.Empty<string>(), _managerId)).EnsureSuccessStatusCode();

        var details = await (await _client.GetAsync($"/api/users/{created.UserId}?actorUserId={_managerId}")).Content.ReadFromJsonAsync<UserDetailsDto>();
        Assert.Contains("orders_refund", details!.Permissions);
    }

    [Fact]
    public async Task Granting_a_permission_the_actor_does_not_hold_is_rejected()
    {
        var created = await (await CreateUserAsync("مسؤول قوائم", "supervisor", "4715", new[] { "menu_manage" }, _limitedAdminId)).Content.ReadFromJsonAsync<CreateUserResult>();
        var response = await UpdatePermissionsAsync(created!.UserId, new[] { "treasury_manage" }, Array.Empty<string>(), _limitedAdminId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Revoking_a_permission_never_needs_the_actor_to_hold_it_themselves()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos", "orders_refund" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        // _limitedAdminId does not hold orders_refund itself, but revoking is never an escalation.
        (await UpdatePermissionsAsync(created!.UserId, Array.Empty<string>(), new[] { "orders_refund" }, _limitedAdminId)).EnsureSuccessStatusCode();

        var details = await (await _client.GetAsync($"/api/users/{created.UserId}?actorUserId={_managerId}")).Content.ReadFromJsonAsync<UserDetailsDto>();
        Assert.DoesNotContain("orders_refund", details!.Permissions);
    }

    [Fact]
    public async Task Changing_the_owners_permissions_is_rejected()
    {
        var response = await UpdatePermissionsAsync(_ownerId, Array.Empty<string>(), new[] { "all" }, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task An_actor_without_all_can_never_grant_all_itself()
    {
        var created = await (await CreateUserAsync("مسؤول قوائم", "supervisor", "4715", new[] { "menu_manage" }, _limitedAdminId)).Content.ReadFromJsonAsync<CreateUserResult>();
        var response = await UpdatePermissionsAsync(created!.UserId, new[] { "all" }, Array.Empty<string>(), _limitedAdminId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    // ---- Reads ----

    [Fact]
    public async Task Listing_all_users_requires_users_manage()
    {
        var noPermissionUser = await _factory.SeedUserAsync("كاشير", "cashier", "pos");
        var response = await _client.GetAsync($"/api/users/all?actorUserId={noPermissionUser}");
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task Listing_all_users_includes_inactive_accounts_unlike_the_login_picker()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await SetActiveAsync(created!.UserId, false, _managerId)).EnsureSuccessStatusCode();

        var all = await (await _client.GetAsync($"/api/users/all?actorUserId={_managerId}")).Content.ReadFromJsonAsync<List<UserDetailsDto>>();
        Assert.Contains(all!, u => u.Id == created.UserId && !u.Active);

        var pickerList = await (await _client.GetAsync("/api/users")).Content.ReadFromJsonAsync<List<UserSummaryDto>>();
        Assert.DoesNotContain(pickerList!, u => u.Id == created.UserId);
    }

    [Fact]
    public async Task The_permission_catalog_is_openly_readable_and_complete()
    {
        var catalog = await (await _client.GetAsync("/api/permissions/catalog")).Content.ReadFromJsonAsync<List<Falah.Domain.Authorization.PermissionDefinition>>();
        Assert.Contains(catalog!, p => p.Key == "users_manage");
        Assert.Contains(catalog!, p => p.Key == "all");
        Assert.True(catalog!.Count >= 20);
    }

    // ---- Auditability ----

    [Fact]
    public async Task Every_sensitive_user_action_is_audited()
    {
        var created = await (await CreateUserAsync("كاشير جديد", "cashier", "4715", new[] { "pos" }, _managerId)).Content.ReadFromJsonAsync<CreateUserResult>();
        (await SetActiveAsync(created!.UserId, false, _managerId, "استقالة")).EnsureSuccessStatusCode();
        (await SetActiveAsync(created.UserId, true, _managerId)).EnsureSuccessStatusCode();
        (await ChangePinAsync(created.UserId, currentPin: null, "8264", _managerId)).EnsureSuccessStatusCode();
        (await UpdatePermissionsAsync(created.UserId, new[] { "orders_refund" }, Array.Empty<string>(), _managerId)).EnsureSuccessStatusCode();

        var events = await (await _client.GetAsync("/api/control-center/audit-events")).Content.ReadFromJsonAsync<List<ControlAuditEventDto>>();
        Assert.Contains(events!, e => e.Action == "user_created" && e.EntityId == created.UserId);
        Assert.Contains(events!, e => e.Action == "user_deactivated" && e.EntityId == created.UserId);
        Assert.Contains(events!, e => e.Action == "user_reactivated" && e.EntityId == created.UserId);
        Assert.Contains(events!, e => e.Action == "pin_changed" && e.EntityId == created.UserId);
        Assert.Contains(events!, e => e.Action == "permissions_changed" && e.EntityId == created.UserId);
    }

    [Fact]
    public async Task A_failed_login_attempt_is_audited_without_revealing_which_pin_was_tried()
    {
        await LoginAsync("0192");
        var events = await (await _client.GetAsync("/api/control-center/audit-events")).Content.ReadFromJsonAsync<List<ControlAuditEventDto>>();
        var failure = events!.First(e => e.Action == "pin_login_failed");
        Assert.DoesNotContain("0192", failure.Reason ?? "");
        Assert.DoesNotContain("0192", failure.EntityId);
    }

    // ---- Rate limiting ----

    /// <summary>A dedicated factory isolates this limiter from other login tests. The permit count
    /// deliberately differs from the production default: this also proves the final host's
    /// configuration reaches the limiter, instead of accidentally testing captured defaults.
    /// The generous window prevents slow SQL/CI hosts from racing the window boundary.</summary>
    [Fact]
    public async Task Repeated_failed_logins_from_the_same_client_eventually_hit_the_rate_limit()
    {
        const int permitLimit = 2;
        using var rateLimitFactory = new FalahApiFactory(configOverrides: new Dictionary<string, string?>
        {
            ["RateLimit:Login:WindowSeconds"] = "300",
            ["RateLimit:Login:PermitLimit"] = permitLimit.ToString(),
        });
        using var rateLimitClient = rateLimitFactory.CreateClient();

        for (var i = 0; i < permitLimit; i++)
        {
            var response = await rateLimitClient.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest($"{1000 + i}"));
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }

        var limited = await rateLimitClient.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest("9999"));
        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
    }
}
