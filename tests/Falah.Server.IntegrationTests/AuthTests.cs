namespace Falah.Server.IntegrationTests;

using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Xunit;

public class AuthTests : IAsyncLifetime
{
    private FalahApiFactory _factory = default!;
    private HttpClient _client = default!;

    public Task InitializeAsync()
    {
        _factory = new FalahApiFactory();
        _client = _factory.CreateAuthenticatedClient();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() { _factory.Dispose(); return Task.CompletedTask; }

    private async Task SeedUserWithPinAsync(string id, string name, string role, string pin, params string[] permissions)
    {
        await _factory.QueryAsync(async db =>
        {
            var hashed = Falah.Domain.Authorization.PinHasher.HashWithoutValidation(pin);
            db.Users.Add(new UserEntity
            {
                Id = id, Name = name, Role = role, Active = true, Permissions = string.Join(",", permissions),
                PinHash = hashed.HashBase64, PinSalt = hashed.SaltBase64, PinIterations = hashed.Iterations,
            });
            await db.SaveChangesAsync();
            return true;
        });
    }

    [Fact]
    public async Task Logging_in_with_the_correct_pin_identifies_the_matching_user()
    {
        await SeedUserWithPinAsync("user-1", "أحمد", "cashier", "5555", "pos");
        var response = await _client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest("5555"));
        response.EnsureSuccessStatusCode();
        var user = await response.Content.ReadFromJsonAsync<UserSummaryDto>();
        Assert.Equal("أحمد", user!.Name);
        Assert.Contains("pos", user.Permissions);
    }

    [Fact]
    public async Task Logging_in_with_a_wrong_pin_is_rejected_without_revealing_anything()
    {
        await SeedUserWithPinAsync("user-1", "أحمد", "cashier", "5555", "pos");
        var response = await _client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest("0000"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task An_inactive_users_pin_no_longer_logs_in()
    {
        await _factory.QueryAsync(async db =>
        {
            var hashed = Falah.Domain.Authorization.PinHasher.HashWithoutValidation("7777");
            db.Users.Add(new UserEntity
            {
                Id = "user-2", Name = "موظف سابق", Role = "cashier", Active = false, Permissions = "pos",
                PinHash = hashed.HashBase64, PinSalt = hashed.SaltBase64, PinIterations = hashed.Iterations,
            });
            await db.SaveChangesAsync();
            return true;
        });
        var response = await _client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest("7777"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
