using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Falah.Client.CashierWinUI.Services;
using Falah.Client.CashierWinUI.ViewModels;

namespace Falah.Client.Tests;

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
    public async Task Lost_self_pin_confirmation_is_reconciled_to_the_same_user_without_rewriting_the_pin()
    {
        var writes = 0; var proofs = 0;
        var session = Session(async r =>
        {
            if (r.RequestUri!.AbsolutePath == "/api/auth/login-by-pin")
            {
                proofs++; var proof = await r.Content!.ReadFromJsonAsync<LoginByPinRequest>();
                Assert.Equal("2468", proof!.Pin);
                return Json(new UserSummaryDto(Owner.Id, Owner.Name, Owner.Role, Owner.Permissions));
            }
            writes++; throw new HttpRequestException("committed, confirmation lost");
        });
        var vm = new UsersViewModel(session); await vm.LoadAsync(); vm.OpenPinChangeCommand.Execute(null);
        Assert.True(vm.IsSelfPinChange);
        vm.CurrentPin = "1357"; vm.NewPin = vm.ConfirmPin = "2468";
        await vm.ChangePinCommand.ExecuteAsync(null);
        Assert.True(session.HasUnresolvedOperation);
        vm.CloseUserDialogCommand.Execute(null); Assert.True(vm.IsPinChangeOpen);
        vm.CurrentPin = "changed"; vm.NewPin = "bad";
        await vm.RetryUserActionCommand.ExecuteAsync(null);
        Assert.Equal(1, writes); Assert.Equal(1, proofs);
        Assert.False(session.HasUnresolvedOperation); Assert.False(vm.IsPinChangeOpen);
        Assert.Empty(vm.CurrentPin); Assert.Empty(vm.NewPin); Assert.Empty(vm.ConfirmPin);
        Assert.Equal(Owner.Id, session.CurrentUser!.Id);
    }

    [Fact]
    public async Task A_pin_proof_for_a_different_user_does_not_confirm_the_pending_change()
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
}
