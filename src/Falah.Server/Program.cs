using Falah.Domain.Authorization;
using Falah.Infrastructure.Approvals;
using Falah.Infrastructure.Menu;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Shifts;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Connection string is resolved lazily from the final DI container, not captured into a local at
// startup - WebApplicationFactory-based tests inject their own per-test override via
// IConfiguration, and that override must win even though it is added after this point in
// Program's own top-level code runs.
builder.Services.AddDbContext<FalahDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Falah")
        ?? "Data Source=falah.db;Cache=Shared";
    options.UseSqlite(connectionString);
});

// EF Core-backed and persisted: issuing (a manager approves) and consuming (a cashier's later
// checkout/refund) are genuinely two separate HTTP requests, so the approval must survive a
// Server restart in between - an in-memory registry could not guarantee that.
builder.Services.AddScoped<IApprovalRegistry, EfApprovalRegistry>();

builder.Services.AddScoped<MenuPricingService>();
builder.Services.AddScoped<InventoryDeductionService>();
builder.Services.AddScoped<CheckoutOrderHandler>();
builder.Services.AddScoped<OpenShiftHandler>();
builder.Services.AddScoped<RefundOrderHandler>();
builder.Services.AddScoped<ConfirmElectronicPaymentHandler>();
builder.Services.AddScoped<IssueManagerApprovalHandler>();
builder.Services.AddScoped<MenuQueryService>();

// The Cashier app is the only client so far, but CORS costs nothing to enable now and saves a
// confusing debugging session the day a browser-based client shows up.
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
    db.Database.Migrate();
    // WAL lets Call Center's reads proceed without waiting on a Cashier write in progress, and
    // vice versa - only actual writers ever block each other.
    db.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
    await DemoDataSeeder.SeedIfEmptyAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/shifts/open", async (Falah.Application.Shifts.OpenShiftRequest request, OpenShiftHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(request, ct);
    return Results.Ok(result);
});

app.MapPost("/api/orders/checkout", async (Falah.Application.Orders.CheckoutOrderRequest request, CheckoutOrderHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Orders.ShiftNotOpenException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Falah.Application.Orders.CheckoutConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Falah.Domain.Common.DomainException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message });
    }
});

app.MapPost("/api/orders/refund", async (Falah.Application.Orders.RefundOrderRequest request, RefundOrderHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Orders.RefundConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Falah.Domain.Common.DomainException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapPost("/api/orders/confirm-electronic-payment", async (Falah.Application.Orders.ConfirmElectronicPaymentRequest request, ConfirmElectronicPaymentHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Orders.ConfirmConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (Falah.Domain.Common.DomainException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapPost("/api/approvals/issue", async (Falah.Application.Approvals.IssueManagerApprovalRequest request, IssueManagerApprovalHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

// Pure read: fetching an order to (re)print its receipt must never be able to touch financial
// state. There is no write path anywhere in this handler.
app.MapGet("/api/orders/{id}", async (string id, FalahDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    return order is null ? Results.NotFound() : Results.Ok(OrderMapper.ToDto(order));
});

// A cashier reads the order number off what they already see on screen/receipt - GUIDs are not
// something a human types back in.
app.MapGet("/api/orders/lookup", async (string orderNumber, FalahDbContext db, CancellationToken ct) =>
{
    var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    return order is null ? Results.NotFound() : Results.Ok(OrderMapper.ToDto(order));
});

app.MapGet("/api/menu", async (MenuQueryService menu, CancellationToken ct) => Results.Ok(await menu.GetMenuAsync(ct)));

app.MapGet("/api/users", async (FalahDbContext db, CancellationToken ct) =>
{
    var users = await db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
    return Results.Ok(users.Select(ToUserSummary));
});

// PIN travels in the request body, never a query string - the lock screen's entire login contract
// is "this PIN alone" (no username), matching the reference app's unlockScreen exactly.
app.MapPost("/api/auth/login-by-pin", async (Falah.Application.Users.LoginByPinRequest request, FalahDbContext db, CancellationToken ct) =>
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Pin == request.Pin && u.Active, ct);
    return user is null ? Results.Unauthorized() : Results.Ok(ToUserSummary(user));
});

// A liveness/connectivity probe the client polls to show Online/Offline - deliberately checks the
// database too, since a server process that is "up" but cannot reach its own database is not
// actually usable.
app.MapGet("/health", async (FalahDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503));

app.Run();

static Falah.Application.Users.UserSummaryDto ToUserSummary(UserEntity user) => new(
    user.Id, user.Name, user.Role,
    user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());

public partial class Program { }
