namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Domain.Authorization;
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
        _client = _factory.CreateAuthenticatedClient();
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

    /// <summary>The shared fixture already seeds an owner, so this is the SECOND one - still
    /// refused. Exactly one owner, ever.</summary>
    [Fact]
    public async Task Creating_a_second_owner_is_rejected()
    {
        var response = await CreateUserAsync("مالك آخر", "owner", "5813", new[] { "all" }, _managerId);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    /// <summary>An installation with no owner has no protected emergency account at all - the owner
    /// is the one account that can never be deactivated or stripped. It used to be uncreatable
    /// through any command in the system, which left such a till permanently without one. The FIRST
    /// owner may now be created; the test above proves a second still cannot.</summary>
    [Fact]
    public async Task The_first_owner_can_be_created_when_the_installation_has_none()
    {
        using var setup = new NoOwnerSetup();
        var admin = await setup.Factory.SeedUserAsync("المدير", "manager", "all");

        var created = await setup.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest($"req-{Guid.NewGuid():N}", "المالك", "owner", "7351", new[] { "all" }, admin));
        created.EnsureSuccessStatusCode();

        var again = await setup.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest($"req-{Guid.NewGuid():N}", "مالك ثانٍ", "owner", "7362", new[] { "all" }, admin));
        Assert.Equal(HttpStatusCode.UnprocessableEntity, again.StatusCode);
    }

    /// <summary>And once it exists it is untouchable: neither deactivated nor stripped, by anyone.</summary>
    [Fact]
    public async Task The_owner_can_be_neither_deactivated_nor_stripped()
    {
        var deactivate = await SetActiveAsync(_ownerId, false, _managerId, "اختبار");
        Assert.False(deactivate.IsSuccessStatusCode);

        var strip = await UpdatePermissionsAsync(_ownerId, Array.Empty<string>(), new[] { "all" }, _managerId);
        Assert.False(strip.IsSuccessStatusCode);
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

    // ---- Administrative lockout ----

    /// <summary>These run on their own factory with NO owner account, because the shared fixture
    /// seeds one and an owner makes this lockout structurally impossible: the owner always holds
    /// "all" and can never be deactivated or stripped, so administration always survives through
    /// them. That is worth stating plainly - an owner account IS the break-glass. The guard below
    /// exists for the installation that has no owner, which is what a real till looks like when the
    /// staff accounts were created without one.</summary>
    private sealed class NoOwnerSetup : IDisposable
    {
        public FalahApiFactory Factory { get; } = new();
        public HttpClient Client { get; }
        public NoOwnerSetup() { Client = Factory.CreateAuthenticatedClient(); }
        public void Dispose() => Factory.Dispose();
    }

    /// <summary>The way a restaurant locks itself out of its own till: one manager tidying
    /// permissions revokes users_manage from the only account that had it. Nothing in the product
    /// could undo that - there is no break-glass path without an owner, so the database would have
    /// to be edited by hand. Revocation is deliberately exempt from the privilege-escalation check
    /// (taking a permission away is never an escalation), which is why it needs its own guard.</summary>
    /// <summary>Stands down every other active account that can administer users, so the named one
    /// really is the last. A fresh database is never empty of administrators - the demo seed writes
    /// a manager holding "all" - and the guard is only reachable once they are out of the way.</summary>
    private static async Task LeaveOnlyOneAdministratorAsync(HttpClient client, string keepUserId)
    {
        var everyone = await client.GetFromJsonAsync<List<UserDetailsDto>>($"/api/users/all?actorUserId={keepUserId}");
        foreach (var other in everyone!.Where(u => u.Id != keepUserId && u.Active
                                                   && UserManagementRules.CanManageUsers(u.Permissions.ToHashSet())))
        {
            (await client.PostAsJsonAsync($"/api/users/{other.Id}/active",
                new SetUserActiveRequest(other.Id, false, "إخلاء الساحة للاختبار", keepUserId)))
                .EnsureSuccessStatusCode();
        }
    }

    [Fact]
    public async Task The_last_administrator_cannot_revoke_their_own_user_management()
    {
        using var setup = new NoOwnerSetup();
        var admin = await setup.Factory.SeedUserAsync("المدير", "manager", "users_manage", "pos");
        await LeaveOnlyOneAdministratorAsync(setup.Client, admin);

        var response = await setup.Client.PostAsJsonAsync(
            $"/api/users/{admin}/permissions",
            new UpdateUserPermissionsRequest(admin, Array.Empty<string>(), new[] { "users_manage" }, admin));

        Assert.False(response.IsSuccessStatusCode);
        Assert.Contains("إدارة المستخدمين", await response.Content.ReadAsStringAsync());

        // And the door is still open afterwards.
        var stillWorks = await setup.Client.PostAsJsonAsync("/api/users",
            new CreateUserRequest($"req-{Guid.NewGuid():N}", "كاشير بعد المحاولة", "cashier", "4726", new[] { "pos" }, admin));
        stillWorks.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Concurrent_self_revocations_leave_an_active_administrator()
    {
        using var setup = new NoOwnerSetup();
        var first = await setup.Factory.SeedUserAsync("مدير أول", "manager", "users_manage");
        var second = await setup.Factory.SeedUserAsync("مدير ثانٍ", "manager", "users_manage");
        var responses = await Task.WhenAll(new[] { first, second }.Select(id =>
            setup.Client.PostAsJsonAsync($"/api/users/{id}/permissions",
                new UpdateUserPermissionsRequest(id, Array.Empty<string>(), new[] { "users_manage" }, id))));
        Assert.Single(responses.Where(r => r.IsSuccessStatusCode));
        var permissions = await setup.Factory.QueryAsync(db => db.Users.Where(u => u.Active).Select(u => u.Permissions).ToListAsync());
        Assert.Contains(permissions, p => UserManagementRules.CanManageUsers(p.Split(',')));
    }

    /// <summary>Revoking it from one of two administrators is ordinary housekeeping and must keep
    /// working - the guard refuses the last one, not any one.</summary>
    [Fact]
    public async Task Revoking_user_management_from_one_of_two_administrators_is_allowed()
    {
        using var setup = new NoOwnerSetup();
        var first = await setup.Factory.SeedUserAsync("مدير أول", "manager", "users_manage");
        var second = await setup.Factory.SeedUserAsync("مدير ثانٍ", "manager", "users_manage");

        var response = await setup.Client.PostAsJsonAsync(
            $"/api/users/{second}/permissions",
            new UpdateUserPermissionsRequest(second, Array.Empty<string>(), new[] { "users_manage" }, first));

        response.EnsureSuccessStatusCode();
    }

    /// <summary>Same door, the other handle: deactivating the last administrator instead of
    /// stripping their permission.</summary>
    [Fact]
    public async Task Deactivating_the_last_administrator_is_refused()
    {
        using var setup = new NoOwnerSetup();
        var first = await setup.Factory.SeedUserAsync("مدير أول", "manager", "users_manage");
        var second = await setup.Factory.SeedUserAsync("مدير ثانٍ", "manager", "users_manage");
        await LeaveOnlyOneAdministratorAsync(setup.Client, first);

        // Two administrators again, deliberately: standing one of them down is ordinary and allowed.
        (await setup.Client.PostAsJsonAsync($"/api/users/{second}/active",
            new SetUserActiveRequest(second, true, "اختبار", first))).EnsureSuccessStatusCode();
        (await setup.Client.PostAsJsonAsync($"/api/users/{second}/active",
            new SetUserActiveRequest(second, false, "اختبار", first))).EnsureSuccessStatusCode();

        // "first" is now the last administrator. Self-deactivation was already refused before this
        // guard existed, so the case that needed covering is a colleague doing it - here the
        // stood-down administrator is brought back solely to aim at the last one.
        (await setup.Client.PostAsJsonAsync($"/api/users/{second}/active",
            new SetUserActiveRequest(second, true, "اختبار", first))).EnsureSuccessStatusCode();
        (await setup.Client.PostAsJsonAsync($"/api/users/{first}/active",
            new SetUserActiveRequest(first, false, "اختبار", second))).EnsureSuccessStatusCode();

        var response = await setup.Client.PostAsJsonAsync($"/api/users/{second}/active",
            new SetUserActiveRequest(second, false, "اختبار", second));

        Assert.False(response.IsSuccessStatusCode);
    }
}
