namespace Falah.Server.IntegrationTests;
using System.Data.Common;
using System.Net;
using System.Net.Http.Json;
using Falah.Application.Users;
using Falah.Infrastructure.Persistence;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Configuration;
public class LoginInfrastructureFailureTests
{
    private sealed class UserReadFault : DbCommandInterceptor
    {
        public bool Enabled;
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken ct = default)
        {
            if (Enabled && command.CommandText.Contains("FROM [Users]")) throw new InvalidOperationException("Injected user read failure");
            return base.ReaderExecutingAsync(command, eventData, result, ct);
        }
    }
    [Fact]
    public async Task Six_infrastructure_failures_do_not_lock_out_a_correct_pin()
    {
        var fault = new UserReadFault();
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?> {
            ["RateLimit:Login:PermitLimit"] = "1000", ["Security:Login:PerSourceFailures"] = "5" });
        using var host = factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
            {
            services.RemoveAll<DbContextOptions<FalahDbContext>>();
            services.RemoveAll<FalahDbContext>();
            services.AddDbContext<FalahDbContext>((sp, options) => options
                .UseSqlServer(sp.GetRequiredService<IConfiguration>().GetConnectionString("Falah"))
                .AddInterceptors(fault));
        }));
        using var client = host.CreateClient();
        client.DefaultRequestHeaders.Add(FalahApiFactory.RemoteAddressHeader, "10.1.1.99");
        // Start and seed through the original factory; both hosts share this disposable test DB.
        var user = await factory.SeedUserAsync("كاشير", "cashier", "pos");
        _ = host.Services;
        fault.Enabled = true;
        for (var i = 0; i < 6; i++)
        {
            var failed = await client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(factory.PinFor(user)));
            Assert.Equal(HttpStatusCode.InternalServerError, failed.StatusCode);
        }
        fault.Enabled = false;
        var success = await client.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(factory.PinFor(user)));
        Assert.Equal(HttpStatusCode.OK, success.StatusCode);
    }
}
