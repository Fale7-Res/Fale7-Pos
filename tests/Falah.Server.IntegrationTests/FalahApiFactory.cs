namespace Falah.Server.IntegrationTests;

using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Boots the REAL Falah.Server (real minimal-API endpoints, real EF Core, a real SQL Server
/// database on the local SQLEXPRESS instance - not an in-memory provider) so these tests exercise
/// the exact path a physical Cashier/Call Center request takes, migrations included. Each factory
/// instance gets its own SQL Server database, with data/log files placed under E:\SqlData\Tests
/// rather than the instance's default path - the dev machine's C: drive has been observed to run
/// out of space entirely - so tests never share state unless a test deliberately shares a factory.
/// </summary>
public sealed class FalahApiFactory : WebApplicationFactory<Program>
{
    private const string MasterConnectionString = @"Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True;";
    private static readonly string DataDir = @"E:\SqlData\Tests";

    public string DatabaseName { get; }
    private readonly bool _ownsDatabase;
    private readonly IReadOnlyDictionary<string, string?>? _configOverrides;

    /// <param name="reuseDatabaseName">Pass another factory's DatabaseName to boot a second,
    /// independent server instance against the SAME database - the cleanest way to prove that
    /// something (e.g. a persisted manager approval) genuinely survives a full Server restart, not
    /// just a method call within the same running process. The reusing instance does not drop the
    /// database on dispose; only the original owner does, once both are done with it.</param>
    /// <param name="configOverrides">Extra configuration keys layered on top of the test database
    /// connection string - e.g. RateLimit:Login:WindowSeconds, so a rate-limit test can shrink the
    /// window to a couple of seconds instead of racing a real 60-second wall-clock window against
    /// the rest of a large parallel suite.</param>
    public FalahApiFactory(string? reuseDatabaseName = null, IReadOnlyDictionary<string, string?>? configOverrides = null)
    {
        _ownsDatabase = reuseDatabaseName is null;
        DatabaseName = reuseDatabaseName ?? $"FalahTest_{Guid.NewGuid():N}";
        _configOverrides = configOverrides;
        if (_ownsDatabase) CreateDatabase();
    }

    private void CreateDatabase()
    {
        Directory.CreateDirectory(DataDir);
        using var connection = new SqlConnection(MasterConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = $"""
            CREATE DATABASE [{DatabaseName}]
            ON PRIMARY (NAME = N'{DatabaseName}', FILENAME = N'{DataDir}\{DatabaseName}.mdf', SIZE = 32MB, FILEGROWTH = 32MB)
            LOG ON (NAME = N'{DatabaseName}_log', FILENAME = N'{DataDir}\{DatabaseName}_log.ldf', SIZE = 16MB, FILEGROWTH = 16MB);
            """;
        command.ExecuteNonQuery();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Falah"] = $@"Server=.\SQLEXPRESS;Database={DatabaseName};Trusted_Connection=True;TrustServerCertificate=True;",
            });
            if (_configOverrides is not null) config.AddInMemoryCollection(_configOverrides);
        });
    }

    /// <summary>Real session tokens for the users this factory seeded, so requests go through the
    /// server's real authentication rather than around it. See ActorTokenHandler.</summary>
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _tokens = new();

    /// <summary>A real session for this user, minting one on first use.
    ///
    /// Lazy on purpose: plenty of tests create their users through the API rather than through
    /// SeedUserAsync - a self-service PIN change, for instance, acts as a user that only exists
    /// because a previous request created it. Falling back to somebody else's session for those
    /// would present a mismatched token and produce a 403 that has nothing to do with what the
    /// test is checking.
    ///
    /// The row written here is exactly what a real login writes.</summary>
    public string? TokenFor(string userId)
    {
        if (_tokens.TryGetValue(userId, out var existing)) return existing;

        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        if (!db.Users.Any(u => u.Id == userId)) return null;

        var token = Falah.Domain.Authorization.SessionToken.Issue();
        db.UserSessions.Add(new Falah.Infrastructure.Persistence.Entities.UserSessionEntity
        {
            Id = $"sess-{Guid.NewGuid():N}",
            TokenHash = Falah.Domain.Authorization.SessionToken.Hash(token),
            UserId = userId,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow,
        });
        db.SaveChanges();
        _tokens[userId] = token;
        return token;
    }

    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _pins = new();

    /// <summary>The PIN of a user this factory seeded. Manager approval identifies its approver by
    /// PIN now, so a test approving as someone has to know that someone's PIN.</summary>
    public string PinFor(string userId) => _pins[userId];

    /// <summary>Any seeded session, for requests that name no acting user of their own - manager
    /// approval is the case that matters: its security is the PIN it carries, but it still has to
    /// come from a station where somebody is logged in.</summary>
    public string? AnySessionToken() => _tokens.Values.FirstOrDefault();

    /// <summary>A client that presents the seeded session of whichever user each request names.
    /// Tests keep the shape they already have; the server still enforces the real rule.</summary>
    protected override void ConfigureClient(HttpClient client) => base.ConfigureClient(client);

    public HttpClient CreateAuthenticatedClient() =>
        CreateDefaultClient(new ActorTokenHandler(TokenFor, AnySessionToken));

    /// <summary>A client for THIS host that presents sessions issued by another factory. Used by the
    /// restart tests: a second host over the same database is a fresh process, and the point of
    /// those tests is that a session issued before the restart still works after it.</summary>
    public HttpClient CreateAuthenticatedClient(FalahApiFactory sessionSource) =>
        CreateDefaultClient(new ActorTokenHandler(sessionSource.TokenFor, sessionSource.AnySessionToken));

    public async Task<string> SeedUserAsync(string name, string role, params string[] permissions)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var id = $"user-{Guid.NewGuid():N}";
        // A random pin, hashed - no test in this suite exercises PIN login itself via this seeded
        // user, so any value is fine here (HashWithoutValidation since this doesn't need to satisfy
        // the real PIN-strength rule either).
        // Deterministic and remembered, because manager approval now verifies the PIN server-side:
        // a test that wants a manager to approve has to be able to present that manager's actual PIN.
        var pinValue = Random.Shared.Next(100_000, 999_999).ToString();
        var pin = Falah.Domain.Authorization.PinHasher.HashWithoutValidation(pinValue);
        db.Users.Add(new UserEntity
        {
            Id = id, Name = name, Role = role, Active = true, Permissions = string.Join(",", permissions),
            PinHash = pin.HashBase64, PinSalt = pin.SaltBase64, PinIterations = pin.Iterations,
        });
        // A real session row, hashed exactly the way a real login stores one - so these tests
        // exercise the production authentication path instead of a test-only shortcut around it.
        var token = Falah.Domain.Authorization.SessionToken.Issue();
        db.UserSessions.Add(new Falah.Infrastructure.Persistence.Entities.UserSessionEntity
        {
            Id = $"sess-{Guid.NewGuid():N}",
            TokenHash = Falah.Domain.Authorization.SessionToken.Hash(token),
            UserId = id,
            CreatedAtUtc = DateTimeOffset.UtcNow,
            LastSeenAtUtc = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();
        _tokens[id] = token;
        _pins[id] = pinValue;
        return id;
    }

    /// <summary>Seeds a minimal, available, grill-flagged-as-requested product at an exact price -
    /// the price the server will look up and freeze, never something the test's HTTP request
    /// carries. Most tests only need one product at a specific total for a quantity-1 line.</summary>
    public async Task<string> SeedProductAsync(long pricePiastres, string name = "منتج اختبار", bool sendToGrill = false, string? recipeId = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var categoryId = $"cat-{Guid.NewGuid():N}";
        db.Categories.Add(new CategoryEntity { Id = categoryId, Name = "قسم اختبار", SortOrder = 0, Active = true });
        var productId = $"prod-{Guid.NewGuid():N}";
        db.Products.Add(new ProductEntity
        {
            Id = productId, CategoryId = categoryId, Name = name, BasePricePiastres = pricePiastres,
            SendToGrill = sendToGrill, IsAvailable = true, SortOrder = 0, RecipeId = recipeId,
        });
        await db.SaveChangesAsync();
        return productId;
    }

    public async Task<string> SeedVariantAsync(string productId, long pricePiastres, string name = "حجم كبير", string? recipeId = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var variantId = $"var-{Guid.NewGuid():N}";
        db.ProductVariants.Add(new ProductVariantEntity { Id = variantId, ProductId = productId, Name = name, PricePiastres = pricePiastres, RecipeId = recipeId });
        await db.SaveChangesAsync();
        return variantId;
    }

    public async Task<string> SeedPriceRuleAsync(string productId, long fixedPricePiastres, bool active = true, TimeOnly? start = null, TimeOnly? end = null)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var ruleId = $"rule-{Guid.NewGuid():N}";
        db.PriceRules.Add(new PriceRuleEntity
        {
            Id = ruleId, Name = "عرض اختبار", FixedPricePiastres = fixedPricePiastres, StartTime = start, EndTime = end, Active = active,
            Products = { new PriceRuleProductEntity { PriceRuleId = ruleId, ProductId = productId } },
        });
        await db.SaveChangesAsync();
        return ruleId;
    }

    /// <summary>Seeds a product whose recipe consumes `quantityMilliUnitsPerUnit` of one fresh
    /// inventory item per unit sold - everything a checkout needs to exercise real stock
    /// deduction end to end.</summary>
    public async Task<(string ProductId, string RecipeId, string InventoryItemId)> SeedProductWithRecipeAsync(long pricePiastres, long quantityMilliUnitsPerUnit)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var categoryId = $"cat-{Guid.NewGuid():N}";
        db.Categories.Add(new CategoryEntity { Id = categoryId, Name = "قسم اختبار", SortOrder = 0, Active = true });
        var inventoryItemId = $"inv-{Guid.NewGuid():N}";
        db.InventoryItems.Add(new InventoryItemEntity
        {
            Id = inventoryItemId, Name = "مكوّن اختبار", CostSource = "Purchase",
            Category = "خامات", Unit = "kg", MinimumMilliUnits = 0, Active = true,
        });
        var productId = $"prod-{Guid.NewGuid():N}";
        var recipeId = $"recipe-{Guid.NewGuid():N}";
        db.Recipes.Add(new RecipeEntity
        {
            Id = recipeId, ProductId = productId, Name = "وصفة اختبار", Active = true,
            Items = { new RecipeItemEntity { RecipeId = recipeId, InventoryItemId = inventoryItemId, QuantityMilliUnits = quantityMilliUnitsPerUnit, Kind = "Direct" } },
        });
        db.Products.Add(new ProductEntity
        {
            Id = productId, CategoryId = categoryId, Name = "منتج بوصفة", BasePricePiastres = pricePiastres,
            SendToGrill = false, IsAvailable = true, SortOrder = 0, RecipeId = recipeId,
        });
        await db.SaveChangesAsync();
        return (productId, recipeId, inventoryItemId);
    }

    /// <summary>Most tests just need "a shift can open with cash" to work, not to exercise Treasury
    /// itself - this seeds an open business day plus enough funds in tre-main directly (bypassing
    /// the API/permission path, same as SeedUserAsync/SeedProductAsync) so OpenShiftHandler's real
    /// shift_opening_float movement (tre-main -> tre-drawer) has something to draw from instead of
    /// every unrelated test needing its own manager user + deposit call.</summary>
    public async Task<string> SeedOpenBusinessDayWithFundedMainAsync(long mainAccountFundingPiastres = 100_000_00)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        var dayId = $"day-{Guid.NewGuid():N}";
        db.BusinessDays.Add(new BusinessDayEntity
        {
            // Cairo business-date, matching Falah.Domain.Common.BusinessClock.Today() exactly - not
            // a plain UTC "today". A real, pre-existing test-flakiness bug found during
            // re-verification: OpenShiftHandler stamps a shift's own BusinessDate via
            // BusinessClock.Today() (Cairo-based), so a UTC-based seed here silently mismatched the
            // shift against this seeded business day for the ~2-3 hours of every UTC day that fall
            // on the other side of the Cairo midnight boundary, making any report/reconciliation
            // query that joins shifts to a business day by BusinessDate intermittently see zero
            // shifts.
            Id = dayId, BusinessDate = Falah.Domain.Common.BusinessClock.Today(), OpenedBy = "seed",
            OpenedAtUtc = DateTimeOffset.UtcNow, Status = "Open", CloseRevision = 0, OpenRequestId = $"seed-{Guid.NewGuid():N}",
        });
        db.TreasuryMovements.Add(new TreasuryMovementEntity
        {
            Id = $"tm-{Guid.NewGuid():N}", IdempotencyKey = $"seed-funding-{Guid.NewGuid():N}", OperationId = "op-seed",
            Type = "capital_injection", DestinationAccountId = "tre-main", AmountPiastres = mainAccountFundingPiastres,
            ReferenceType = "external_funding", ReferenceId = "seed", Reason = "seed funding for tests",
            CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow, Status = "Posted",
        });
        await db.SaveChangesAsync();
        return dayId;
    }

    /// <summary>Funds tre-main directly (bypassing the API/permission path) WITHOUT opening a
    /// business day - for tests that open their OWN business day through the real
    /// POST /api/business-day/open endpoint (e.g. to exercise close/reopen) but still need enough
    /// balance in tre-main for a shift's opening float or a purchase/expense/treasury command to
    /// draw from.</summary>
    public async Task FundMainAccountAsync(long amountPiastres = 100_000_00)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        db.TreasuryMovements.Add(new TreasuryMovementEntity
        {
            Id = $"tm-{Guid.NewGuid():N}", IdempotencyKey = $"seed-funding-{Guid.NewGuid():N}", OperationId = "op-seed",
            Type = "capital_injection", DestinationAccountId = "tre-main", AmountPiastres = amountPiastres,
            ReferenceType = "external_funding", ReferenceId = "seed", Reason = "seed funding for tests",
            CreatedBy = "seed", CreatedAtUtc = DateTimeOffset.UtcNow, Status = "Posted",
        });
        await db.SaveChangesAsync();
    }

    public async Task<T> QueryAsync<T>(Func<FalahDbContext, Task<T>> query)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
        return await query(db);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!_ownsDatabase) return; // a "restart" factory reusing another's database never drops it
        try
        {
            using var connection = new SqlConnection(MasterConnectionString);
            connection.Open();
            using var command = connection.CreateCommand();
            // SET SINGLE_USER WITH ROLLBACK IMMEDIATE force-disconnects any of this test process's
            // own pooled connections to the database before DROP DATABASE, regardless of ADO.NET
            // client-side pooling state.
            command.CommandText = $"ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}];";
            command.ExecuteNonQuery();
        }
        catch { /* best-effort cleanup */ }
    }
}
