namespace Falah.Server.IntegrationTests;
using System.Data.Common;
using System.Net.Http.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Migrations;

public class MigrationSafetyTests
{
    [Fact]
    public async Task Production_with_startup_migrations_disabled_leaves_an_empty_test_database_untouched()
    {
        using var factory = new FalahApiFactory(configOverrides: new Dictionary<string, string?> { ["Database:MigrateOnStartup"] = "false" }, environment: "Production");
        Assert.StartsWith("FalahTest_", factory.DatabaseName);
        var error = Assert.ThrowsAny<Exception>(() => factory.CreateClient());
        Assert.Contains("Database:MigrateOnStartup", error.ToString());
        await using var connection = new SqlConnection($@"Server=.\SQLEXPRESS;Database={factory.DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;");
        await connection.OpenAsync();
        await using var command = connection.CreateCommand(); command.CommandText = "SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped = 0";
        Assert.Equal(0, Convert.ToInt32(await command.ExecuteScalarAsync()));
    }
    [Fact]
    public void Pickup_normalization_refuses_a_lossy_downgrade()
    {
        var migration = new RenameDineInOrdersToPickup();
        Assert.Throws<NotSupportedException>(() => migration.DownOperations);
    }
    private sealed class NoDatabaseConnections : DbConnectionInterceptor
    {
        public override InterceptionResult ConnectionOpening(DbConnection connection, ConnectionEventData eventData, InterceptionResult result) =>
            throw new InvalidOperationException("Script generation must not connect to a database.");
    }
    [Fact]
    public void An_idempotent_migration_script_can_be_reviewed_without_opening_a_database()
    {
        using var db = new FalahDbContext(new DbContextOptionsBuilder<FalahDbContext>()
            .UseSqlServer(@"Server=.\SQLEXPRESS;Database=FalahTest_ScriptGenerationOnly;Trusted_Connection=True;TrustServerCertificate=True;")
            .AddInterceptors(new NoDatabaseConnections()).Options);
        var script = db.GetService<IMigrator>().GenerateScript(options: MigrationsSqlGenerationOptions.Idempotent);
        Assert.Contains("IF NOT EXISTS", script);
        Assert.Contains("20261107010000_AddVariantRetirement", script);
        Assert.DoesNotContain("SET OrderType = 'dine_in' WHERE OrderType = 'pickup'", script);
    }

    [Fact]
    public async Task Pickup_normalization_preserves_existing_pickups_and_all_financial_values()
    {
        using var factory = new FalahApiFactory();
        var actor = await factory.SeedUserAsync("مالك اختبار", "owner", "all");
        await factory.SeedOpenBusinessDayWithFundedMainAsync();
        using var client = factory.CreateAuthenticatedClient();
        var opened = await client.PostAsJsonAsync("/api/shifts/open", new Falah.Application.Shifts.OpenShiftRequest("migration-shift", "اختبار", actor, 0));
        opened.EnsureSuccessStatusCode();
        var shift = (await opened.Content.ReadFromJsonAsync<Falah.Application.Shifts.OpenShiftResult>())!.ShiftId;
        await factory.QueryAsync(async db => {
            var kinds = new[] { "dine_in", "dinein", "pickup" };
            for (var i = 0; i < kinds.Length; i++) db.Orders.Add(new Falah.Infrastructure.Persistence.Entities.OrderEntity {
                Id = "migration-order-" + i, CheckoutRequestId = "migration-request-" + i, OrderNumber = "#migration-" + i,
                SequenceNumber = 100 + i, CashierId = actor, ShiftId = shift, OrderType = kinds[i], PaymentMethod = "cash", Status = "completed",
                SubtotalPiastres = 100 + i, TotalPiastres = 100 + i, AmountPaidPiastres = 100 + i, IsPaid = true, CreatedAtUtc = DateTimeOffset.UtcNow });
            await db.SaveChangesAsync();
            var before = await db.Orders.AsNoTracking().OrderBy(o => o.Id).Select(o => o.TotalPiastres).ToListAsync();
            var migration = new RenameDineInOrdersToPickup();
            foreach (var sql in migration.UpOperations.OfType<Microsoft.EntityFrameworkCore.Migrations.Operations.SqlOperation>())
                await db.Database.ExecuteSqlRawAsync(sql.Sql);
            Assert.Equal(3, await db.Orders.CountAsync(o => o.OrderType == "pickup"));
            Assert.Equal(before, await db.Orders.AsNoTracking().OrderBy(o => o.Id).Select(o => o.TotalPiastres).ToListAsync());
            return true;
        });
    }

}
