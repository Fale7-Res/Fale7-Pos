using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

[Collection(RetainedRequestStorageCollection.Name)]
public class UserWorkflowTests
{
    private sealed class Handler(Func<HttpRequestMessage, Task<HttpResponseMessage>> send) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct) => send(request);
    }
    private static HttpResponseMessage Json(object value, HttpStatusCode status = HttpStatusCode.OK) => new(status) { Content = JsonContent.Create(value) };
    private static readonly UserDetailsDto Owner = new("actor", "المالك", "owner", true, new[] { "all" });
    private static readonly UserDetailsDto Target = new("target", "مستخدم", "cashier", true, new[] { "pos" });
    private static AppSession Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> write) =>
        new(new ApiClient(new HttpClient(new Handler(r => r.Method == HttpMethod.Get
            ? Task.FromResult(r.RequestUri!.AbsolutePath == "/api/users/all"
                ? Json(new[] { Owner, Target }) : Json(new[] { new { key = "pos", category = "orders" }, new { key = "all", category = "system" } }))
            : write(r))) { BaseAddress = new Uri("http://fixture.invalid") }))
        { CurrentUser = new(Owner.Id, Owner.Name, Owner.Role, Owner.Permissions) };

    [Fact]
    public async Task User_creation_retains_the_original_identity_pin_and_permissions_after_lost_confirmation()
    {
        var bodies = new List<string>();
        var session = Session(async r =>
        {
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("lost");
            if (bodies.Count == 2) return Json(new { error = "unavailable" }, HttpStatusCode.UnprocessableEntity);
            return Json(new CreateUserResult("created", true));
        });
        var vm = new UsersViewModel(session); await vm.LoadAsync();
        vm.OpenCreateUserCommand.Execute(null);
        vm.NewUserName = "كاشير"; vm.NewUserRole = "cashier"; vm.NewUserPin = vm.ConfirmUserPin = "2468";
        vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = true;
        await vm.CreateUserCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        vm.CloseUserDialogCommand.Execute(null);
        vm.SelectUserCommand.Execute(Target);
        Assert.True(vm.IsCreateUserOpen); Assert.Equal(Owner.Id, vm.SelectedUser!.Id);
        vm.NewUserName = "changed"; vm.NewUserRole = "owner"; vm.NewUserPin = "bad";
        vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = false;
        await vm.CreateUserCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        await vm.CreateUserCommand.ExecuteAsync(null);
        Assert.Equal(3, bodies.Count); Assert.All(bodies, body => Assert.Equal(bodies[0], body));
        Assert.False(session.HasUnresolvedOperation); Assert.False(vm.IsCreateUserOpen);
        Assert.Empty(vm.NewUserPin); Assert.Empty(vm.ConfirmUserPin);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Existing_user_writes_retry_the_original_decision_and_target(bool permissions)
    {
        var bodies = new List<string>();
        var session = Session(async r =>
        {
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("lost");
            return new(HttpStatusCode.OK);
        });
        var vm = new UsersViewModel(session); await vm.LoadAsync(); vm.SelectUserCommand.Execute(Target);
        vm.Permissions.Single(p => p.Key == "pos").IsGranted = false;
        var command = permissions ? vm.SavePermissionsCommand : vm.ToggleActiveCommand;
        await command.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        vm.SelectUserCommand.Execute(Owner); vm.OpenCreateUserCommand.Execute(null);
        Assert.Equal(Target.Id, vm.SelectedUser!.Id); Assert.False(vm.IsCreateUserOpen);
        vm.Permissions.Single(p => p.Key == "pos").IsGranted = true;
        await vm.RetryUserActionCommand.ExecuteAsync(null);
        Assert.Equal(2, bodies.Count); Assert.Equal(bodies[0], bodies[1]);
        Assert.False(session.HasUnresolvedOperation);
    }

    [Fact]
    public async Task Lost_pin_confirmation_retries_the_same_command_without_logging_in()
    {
        var bodies = new List<string>();
        var logins = 0;
        var session = Session(async r =>
        {
            if (r.RequestUri!.AbsolutePath == "/api/auth/login-by-pin")
            {
                logins++;
                return Json(new UserSummaryDto(Target.Id, Target.Name, Target.Role, Target.Permissions));
            }
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("lost");
            return new HttpResponseMessage(HttpStatusCode.OK);
        });
        var vm = new UsersViewModel(session); await vm.LoadAsync(); vm.OpenPinChangeCommand.Execute(null);
        vm.CurrentPin = "1357"; vm.NewPin = vm.ConfirmPin = "2468";
        await vm.ChangePinCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        await vm.RetryUserActionCommand.ExecuteAsync(null);
        Assert.Equal(0, logins);
        Assert.Equal(2, bodies.Count);
        Assert.Equal(bodies[0], bodies[1]);
        Assert.Contains("requestId", bodies[0]);
        Assert.False(session.HasUnresolvedOperation);
        Assert.Equal(Owner.Id, session.CurrentUser!.Id);
    }

    [Fact]
    public async Task A_refused_retry_of_an_unconfirmed_pin_change_stays_pending()
    {
        var bodies = new List<string>();
        var session = Session(async r =>
        {
            if (r.RequestUri!.AbsolutePath == "/api/auth/login-by-pin") return Json(new UserSummaryDto(Target.Id, Target.Name, Target.Role, Target.Permissions));
            bodies.Add(await r.Content!.ReadAsStringAsync());
            if (bodies.Count == 1) throw new HttpRequestException("lost");
            return Json(new { error = "current PIN invalid" }, HttpStatusCode.UnprocessableEntity);
        });
        var vm = new UsersViewModel(session); await vm.LoadAsync(); vm.OpenPinChangeCommand.Execute(null);
        vm.CurrentPin = "1357"; vm.NewPin = vm.ConfirmPin = "2468";
        await vm.ChangePinCommand.ExecuteAsync(null); await vm.RetryUserActionCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation); Assert.True(vm.IsPinChangeOpen);
        Assert.Equal(2, bodies.Count); Assert.Equal(bodies[0], bodies[1]);
        Assert.Equal(Owner.Id, session.CurrentUser!.Id);
    }

    [Fact]
    public async Task Administrative_pin_reset_cannot_target_the_owner_and_creation_cannot_grant_unheld_permissions()
    {
        var writes = 0;
        var session = Session(_ => { writes++; return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)); });
        session.CurrentUser = new("manager", "مدير", "manager", new[] { "users_manage", "pos" });
        var vm = new UsersViewModel(session); await vm.LoadAsync();
        vm.OpenPinChangeCommand.Execute(null); Assert.False(vm.IsPinChangeOpen);
        vm.OpenCreateUserCommand.Execute(null);
        vm.NewUserName = "اختبار"; vm.NewUserRole = "cashier"; vm.NewUserPin = vm.ConfirmUserPin = "2468";
        vm.NewUserPermissions.Single(p => p.Key == "all").IsGranted = true;
        await vm.CreateUserCommand.ExecuteAsync(null);
        Assert.Equal(0, writes); Assert.NotNull(vm.ActionError);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Unconfirmed_user_credentials_are_retained_only_in_memory(bool create)
    {
        var directory = Path.Combine(Path.GetTempPath(), "falah-pin-store-" + Guid.NewGuid().ToString("N"));
        var previous = RetainedRequestStorage.Current;
        try
        {
            RetainedRequestStorage.Current = new FileRetainedRequestStore(directory);
            var session = Session(_ => throw new HttpRequestException("lost"));
            var vm = new UsersViewModel(session); await vm.LoadAsync();
            if (create)
            {
                vm.OpenCreateUserCommand.Execute(null);
                vm.NewUserName = "كاشير"; vm.NewUserRole = "cashier";
                vm.NewUserPin = vm.ConfirmUserPin = "2468";
                vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = true;
                await vm.CreateUserCommand.ExecuteAsync(null);
            }
            else
            {
                vm.OpenPinChangeCommand.Execute(null);
                vm.CurrentPin = "1357"; vm.NewPin = vm.ConfirmPin = "2468";
                await vm.ChangePinCommand.ExecuteAsync(null);
            }
            Assert.True(vm.HasPendingAction);
            Assert.Empty(Directory.EnumerateFiles(directory));
        }
        finally { RetainedRequestStorage.Current = previous; if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    }

    [Fact]
    public void Old_pin_request_files_are_removed_without_deleting_financial_requests()
    {
        var directory = Path.Combine(Path.GetTempPath(), "falah-pin-store-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            foreach (var key in new[] { "users.create", "users.change-pin" })
            {
                File.WriteAllText(Path.Combine(directory, key + ".json"), "legacy credential");
                File.WriteAllText(Path.Combine(directory, key + ".json.tmp"), "legacy credential");
            }
            var financial = Path.Combine(directory, "checkout.json");
            File.WriteAllText(financial, "pending financial operation");
            _ = new FileRetainedRequestStore(directory);
            Assert.Equal(new[] { financial }, Directory.GetFiles(directory));
        }
        finally { Directory.Delete(directory, true); }
    }

}
