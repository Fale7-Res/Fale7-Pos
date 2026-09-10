using Falah.Domain.Authorization;
using Falah.Infrastructure.Approvals;
using Falah.Infrastructure.Audit;
using Falah.Infrastructure.Backup;
using Falah.Infrastructure.CashDrawer;
using Falah.Infrastructure.BusinessDay;
using Falah.Infrastructure.ControlCenter;
using Falah.Infrastructure.Customers;
using Falah.Infrastructure.Delivery;
using Falah.Infrastructure.Employees;
using Falah.Infrastructure.Expenses;
using Falah.Infrastructure.Inventory;
using Falah.Infrastructure.Menu;
using Falah.Infrastructure.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Falah.Infrastructure.Pricing;
using Falah.Infrastructure.Printing;
using Falah.Infrastructure.Purchasing;
using Falah.Infrastructure.Reports;
using Falah.Infrastructure.Settings;
using Falah.Infrastructure.Shifts;
using Falah.Infrastructure.Treasury;
using Falah.Infrastructure.Users;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// A generic technical safeguard against PIN brute-forcing on the one unauthenticated endpoint in
// this system (login-by-pin has no session/token to gate it beforehand) - NOT a restaurant policy
// (no lockout duration, no per-account counter, nothing that requires knowing how this specific
// business wants failed logins handled). A fixed window keyed by client IP; a real attacker
// spraying PINs from one machine hits this fast, while a single cashier mistyping their own PIN a
// few times in a row never will.
// Resolve overrides from the final host configuration, like the database connection below.
// Capturing these values during top-level startup ignored WebApplicationFactory's configuration
// and silently used the 60-second window in tests configured for 300 seconds.
// The production defaults remain one minute / ten requests.
builder.Services.AddRateLimiter(_ => { });
builder.Services.AddOptions<RateLimiterOptions>().Configure<IConfiguration>((options, configuration) =>
{
    var loginRateLimitWindow = TimeSpan.FromSeconds(configuration.GetValue("RateLimit:Login:WindowSeconds", 60));
    var loginRateLimitPermits = configuration.GetValue("RateLimit:Login:PermitLimit", 10);
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("login", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { Window = loginRateLimitWindow, PermitLimit = loginRateLimitPermits, QueueLimit = 0 }));
});

// Connection string is resolved lazily from the final DI container, not captured into a local at
// startup - WebApplicationFactory-based tests inject their own per-test override via
// IConfiguration, and that override must win even though it is added after this point in
// Program's own top-level code runs.
builder.Services.AddDbContext<FalahDbContext>((sp, options) =>
{
    var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("Falah")
        ?? @"Server=.\SQLEXPRESS;Database=Falah;Trusted_Connection=True;TrustServerCertificate=True;";
    options.UseSqlServer(connectionString);
});

// EF Core-backed and persisted: issuing (a manager approves) and consuming (a cashier's later
// checkout/refund) are genuinely two separate HTTP requests, so the approval must survive a
// Server restart in between - an in-memory registry could not guarantee that.
builder.Services.AddScoped<IApprovalRegistry, EfApprovalRegistry>();

builder.Services.AddScoped<MenuPricingService>();
builder.Services.AddScoped<InventoryDeductionService>();
builder.Services.AddScoped<CheckoutOrderHandler>();
builder.Services.AddScoped<OpenShiftHandler>();
builder.Services.AddScoped<PrepareShiftCloseHandler>();
builder.Services.AddScoped<CloseShiftHandler>();
builder.Services.AddScoped<RefundOrderHandler>();
builder.Services.AddScoped<ConfirmElectronicPaymentHandler>();
builder.Services.AddScoped<CancelPendingElectronicPaymentHandler>();
builder.Services.AddScoped<IssueManagerApprovalHandler>();
builder.Services.AddScoped<MenuQueryService>();
builder.Services.AddScoped<OpenBusinessDayHandler>();
builder.Services.AddScoped<DepositHandler>();
builder.Services.AddScoped<WithdrawalHandler>();
builder.Services.AddScoped<TransferHandler>();
builder.Services.AddScoped<SafeDropHandler>();
builder.Services.AddScoped<AddExpenseHandler>();
builder.Services.AddScoped<CreateEmployeeHandler>();
builder.Services.AddScoped<UpdateEmployeeHandler>();
builder.Services.AddScoped<EmployeeQueryService>();
builder.Services.AddScoped<RecordManualLedgerEntryHandler>();
builder.Services.AddScoped<RecordEmployeeCashOutHandler>();
builder.Services.AddScoped<RecordDailyWageHandler>();
builder.Services.AddScoped<RecordOvertimeHandler>();
builder.Services.AddScoped<RecordExtraShiftHandler>();
builder.Services.AddScoped<BiometricCheckInHandler>();
builder.Services.AddScoped<BiometricCheckOutHandler>();
builder.Services.AddScoped<CorrectAttendanceHandler>();
builder.Services.AddScoped<CreatePayrollHandler>();
builder.Services.AddScoped<TransitionPayrollHandler>();
builder.Services.AddScoped<PayPayrollHandler>();
builder.Services.AddScoped<PayrollQueryService>();
builder.Services.AddScoped<CreateBackupHandler>();
builder.Services.AddScoped<VerifyBackupHandler>();
builder.Services.AddScoped<RestoreBackupHandler>();
builder.Services.AddScoped<BackupQueryService>();
builder.Services.AddScoped<ExpenseQueryService>();
builder.Services.AddScoped<CreateDriverHandler>();
builder.Services.AddScoped<SetDriverActiveHandler>();
builder.Services.AddScoped<CreateDeliveryServiceHandler>();
builder.Services.AddScoped<UpdateDeliveryServiceHandler>();
builder.Services.AddScoped<MarkDeliveryReadyHandler>();
builder.Services.AddScoped<AssignDeliveryHandler>();
builder.Services.AddScoped<ReassignDeliveryHandler>();
builder.Services.AddScoped<RecordDeliveryResultHandler>();
builder.Services.AddScoped<SettleDriverCustodyHandler>();
builder.Services.AddScoped<DeliveryQueryService>();
builder.Services.AddScoped<CreateInventoryItemHandler>();
builder.Services.AddScoped<UpdateInventoryItemHandler>();
builder.Services.AddScoped<RecordCountAdjustmentHandler>();
builder.Services.AddScoped<RecordWasteHandler>();
builder.Services.AddScoped<SetEstimatedCostHandler>();
builder.Services.AddScoped<SaveRecipeHandler>();
builder.Services.AddScoped<SavePrepRecipeHandler>();
builder.Services.AddScoped<SaveOrderPackagingRuleHandler>();
builder.Services.AddScoped<InventoryQueryService>();
builder.Services.AddScoped<UpsertCustomerHandler>();
builder.Services.AddScoped<CustomerQueryService>();
builder.Services.AddScoped<ReportingQueryService>();
builder.Services.AddScoped<OwnerDashboardQueryService>();
builder.Services.AddScoped<CreatePrintJobsHandler>();
builder.Services.AddScoped<OrderAutoPrintService>();
builder.Services.AddScoped<Falah.Infrastructure.Authorization.SessionService>();
builder.Services.AddScoped<ReportPrintJobResultHandler>();
builder.Services.AddScoped<SetProductStationHandler>();
builder.Services.AddScoped<PrintJobQueryService>();
builder.Services.AddScoped<AddSupplierHandler>();
builder.Services.AddScoped<ReceivePurchaseHandler>();
builder.Services.AddScoped<PayPurchaseHandler>();
builder.Services.AddScoped<SetPurchaseDueDateHandler>();
builder.Services.AddScoped<PurchasingQueryService>();
builder.Services.AddScoped<CloseBusinessDayHandler>();
builder.Services.AddScoped<ReopenBusinessDayHandler>();
builder.Services.AddScoped<CompleteChecklistItemHandler>();
builder.Services.AddScoped<SaveCashCountHandler>();
builder.Services.AddScoped<SettingsQueryService>();
builder.Services.AddScoped<UpdateSettingsHandler>();
builder.Services.AddScoped<SetExceptionStatusHandler>();
builder.Services.AddScoped<ControlCenterQueryService>();
builder.Services.AddScoped<CreateUserHandler>();
builder.Services.AddScoped<SetUserActiveHandler>();
builder.Services.AddScoped<ChangePinHandler>();
builder.Services.AddScoped<UpdateUserPermissionsHandler>();
builder.Services.AddScoped<UserManagementQueryService>();
builder.Services.AddScoped<Falah.Domain.Audit.IAuditLog, EfAuditLog>();
builder.Services.AddScoped<AuditQueryService>();
builder.Services.AddScoped<CreateCategoryHandler>();
builder.Services.AddScoped<UpdateCategoryHandler>();
builder.Services.AddScoped<SetCategoryActiveHandler>();
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<SetProductImageHandler>();
builder.Services.AddScoped<RemoveProductImageHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<SetProductAvailabilityHandler>();
builder.Services.AddScoped<CreateTaxRuleHandler>();
builder.Services.AddScoped<UpdateTaxRuleHandler>();
builder.Services.AddScoped<TaxRuleQueryService>();
builder.Services.AddScoped<SavePriceRuleHandler>();
builder.Services.AddScoped<PriceRuleQueryService>();
builder.Services.AddScoped<OpenCashDrawerHandler>();
builder.Services.AddScoped<ReportCashDrawerResultHandler>();
builder.Services.AddScoped<CashierCashMovementHandler>();

// The Cashier app is the only client so far, but CORS costs nothing to enable now and saves a
// confusing debugging session the day a browser-based client shows up.
builder.Services.AddCors(options => options.AddDefaultPolicy(p => p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();
app.UseCors();
app.UseRateLimiter();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
    db.Database.Migrate();
    // READ_COMMITTED_SNAPSHOT is the SQL Server equivalent of what WAL gave the SQLite version of
    // this system: Call Center's reads proceed from a consistent snapshot without waiting on a
    // Cashier write in progress, and vice versa - only actual writers ever block each other. Runs
    // once at startup before any real client connects, so ROLLBACK IMMEDIATE never has a live
    // transaction to actually roll back.
    db.Database.ExecuteSqlRaw(
        "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE database_id = DB_ID() AND is_read_committed_snapshot_on = 1) " +
        "ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;");
    await DemoDataSeeder.SeedIfEmptyAsync(db);
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Last line of defence. Every handler below already translates the failures it EXPECTS into a 422
// or a 404 with an Arabic message; this catches the ones nobody expected.
//
// It exists because an unhandled exception used to reach the caller as a bare 500 with an empty
// body in Production - a till showing "the operation failed" with nothing to act on and nothing
// written down. A missing required field on /api/approvals/issue reached the database and failed
// on a NOT NULL constraint exactly that way, on the one path that authorises comps and refunds.
//
// The response is deliberately generic: the operator gets a sentence they can act on and a
// reference to quote, while the detail goes to the log rather than over the wire.
app.UseMiddleware<Falah.Server.AuthenticationMiddleware>();

app.UseExceptionHandler(branch => branch.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var reference = context.TraceIdentifier;
    app.Logger.LogError(feature?.Error,
        "Unhandled exception on {Method} {Path} (reference {Reference})",
        context.Request.Method, context.Request.Path, reference);

    context.Response.StatusCode = StatusCodes.Status500InternalServerError;
    context.Response.ContentType = "application/json; charset=utf-8";
    await context.Response.WriteAsJsonAsync(new
    {
        error = "حدث خطأ غير متوقع ولم تكتمل العملية. أعد المحاولة، وإن تكرر أبلغ الدعم بالرقم المرجعي.",
        reference,
    });
}));

app.MapPost("/api/shifts/open", async (Falah.Application.Shifts.OpenShiftRequest request, OpenShiftHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Shifts.ShiftAlreadyOpenException ex)
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

// A station with no shift-open UI of its own (Call Center) reads this to discover which shift id
// to attach its checkout requests to - single system-wide active shift, so there is at most one.
app.MapGet("/api/shifts/current", async (FalahDbContext db, CancellationToken ct) =>
{
    var shift = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == "Open", ct);
    return shift is null
        ? Results.NotFound(new { error = "لا توجد وردية مفتوحة حاليًا." })
        : Results.Ok(new Falah.Application.Shifts.CurrentShiftDto(shift.Id, shift.ShiftName, shift.OpenedByUserId, shift.OpeningCashPiastres, shift.OpenedAtUtc));
});

app.MapPost("/api/shifts/prepare-close", async (Falah.Application.Shifts.PrepareShiftCloseRequest request, PrepareShiftCloseHandler handler, CancellationToken ct) =>
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

app.MapPost("/api/shifts/close", async (Falah.Application.Shifts.CloseShiftRequest request, CloseShiftHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Shifts.ShiftCloseConflictException ex)
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

app.MapPost("/api/business-day/open", async (Falah.Application.BusinessDay.OpenBusinessDayRequest request, OpenBusinessDayHandler handler, CancellationToken ct) =>
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
});

// The business day currently in play (Open/Closing/Reopened), or 404 when none is open. A Control
// Center screen cannot ask for blockers, checklist or exceptions without first knowing WHICH day it
// is looking at, and every other route here is keyed by id. Read-only; resolves the day the same way
// BusinessDayGate.TryGetActiveAsync already does for checkout, so the screen and the command path
// can never disagree about which day is active.
app.MapGet("/api/business-day/current", async (FalahDbContext db, ControlCenterQueryService query, CancellationToken ct) =>
{
    var id = await Falah.Infrastructure.BusinessDay.BusinessDayGate.TryGetActiveAsync(db, ct);
    return id is null
        ? Results.NotFound(new { error = "لا يوجد يوم عمل مفتوح حاليًا." })
        : Results.Ok(await query.GetBusinessDayAsync(id, ct));
});

app.MapGet("/api/business-days", async (ControlCenterQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListBusinessDaysAsync(ct)));

app.MapGet("/api/business-day/{id}", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetBusinessDayAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/business-day/{id}/close-blockers", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.PreviewCloseBlockersAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/business-day/{id}/close", async (string id, Falah.Application.BusinessDay.CloseBusinessDayRequest body, CloseBusinessDayHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { BusinessDayId = id };
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.BusinessDay.BusinessDayCloseBlockedException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message, blockers = ex.Blockers });
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

app.MapGet("/api/business-day/{id}/close-snapshot", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
{
    var snapshot = await query.GetLatestCloseSnapshotAsync(id, ct);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/api/business-day/{id}/close-snapshots", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListCloseSnapshotsAsync(id, ct)));

app.MapPost("/api/business-day/{id}/reopen", async (string id, Falah.Application.BusinessDay.ReopenBusinessDayRequest body, ReopenBusinessDayHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { BusinessDayId = id };
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

app.MapGet("/api/business-day/{id}/checklist", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
    Results.Ok(await query.GetChecklistAsync(id, ct)));

app.MapPost("/api/business-day/{id}/checklist/{itemId}", async (string id, string itemId, Falah.Application.BusinessDay.CompleteChecklistItemRequest body, CompleteChecklistItemHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { BusinessDayId = id, ItemId = itemId };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
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

app.MapGet("/api/settings", async (SettingsQueryService query, CancellationToken ct) => Results.Ok(await query.GetAsync(ct)));

app.MapPut("/api/settings", async (Falah.Application.Settings.UpdateSettingsRequest request, UpdateSettingsHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/business-day/cash-count-denominations", () =>
    Results.Ok(Falah.Domain.BusinessDay.BusinessDayRules.EgyptianDenominationsPiastres));

app.MapGet("/api/business-day/{id}/cash-counts", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListCashCountsAsync(id, ct)));

app.MapPost("/api/business-day/{id}/cash-counts", async (string id, Falah.Application.BusinessDay.SaveCashCountRequest body, SaveCashCountHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { BusinessDayId = id };
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/business-day/{id}/exceptions", async (string id, ControlCenterQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetExceptionsAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/control-exceptions/{exceptionId}/status", async (string exceptionId, Falah.Application.BusinessDay.SetExceptionStatusRequest body, SetExceptionStatusHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { ExceptionId = exceptionId };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
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

app.MapGet("/api/control-center/audit-events", async (int? limit, ControlCenterQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListAuditEventsAsync(limit ?? 100, ct)));

// The general-purpose audit trail (orders/cash/shifts/menu/inventory/delivery) - distinct from
// /api/control-center/audit-events above, which is the narrower business-day/user-management trail.
// Gated on audit_view OR control_center (either is evidence of a legitimate review role), matching
// the reference app's own Control Center review screen permission check.
app.MapGet("/api/audit/events", async (string actorUserId, string? category, string? severity, string? userId,
    DateTimeOffset? from, DateTimeOffset? to, int? limit, FalahDbContext db, AuditQueryService query, CancellationToken ct) =>
{
    var actor = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct);
    if (actor is null) return Results.NotFound(new { error = "المستخدم غير موجود." });
    var permissions = actor.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (!permissions.Contains("all") && !permissions.Contains("audit_view") && !permissions.Contains("control_center"))
        return Results.UnprocessableEntity(new { error = "ليس لديك صلاحية عرض سجل المراجعة." });
    return Results.Ok(await query.ListAsync(category, severity, userId, from, to, limit ?? 200, ct));
});

app.MapPost("/api/treasury/deposit", async (Falah.Application.Treasury.DepositRequest request, DepositHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex)
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

app.MapPost("/api/treasury/withdraw", async (Falah.Application.Treasury.WithdrawalRequest request, WithdrawalHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex)
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

app.MapPost("/api/treasury/transfer", async (Falah.Application.Treasury.TransferRequest request, TransferHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex)
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

app.MapPost("/api/treasury/safe-drop", async (Falah.Application.Treasury.SafeDropRequest request, SafeDropHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex)
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

app.MapPost("/api/expenses", async (Falah.Application.Expenses.AddExpenseRequest request, AddExpenseHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Expenses.ExpenseConflictException ex)
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

// Server-enforced visibility, not a UI-side filter - see ExpenseQueryService's own doc comment.
app.MapGet("/api/expenses", async (string actorUserId, string? shiftId, string? businessDate, ExpenseQueryService query, CancellationToken ct) =>
{
    try
    {
        var result = await query.GetVisibleExpensesAsync(actorUserId, shiftId, businessDate, ct);
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

// ---- Employees (roster + financial ledger) ----

app.MapPost("/api/employees", async (Falah.Application.Employees.CreateEmployeeRequest request, CreateEmployeeHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPut("/api/employees/{employeeId}", async (string employeeId, Falah.Application.Employees.UpdateEmployeeRequest body, UpdateEmployeeHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { EmployeeId = employeeId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/employees", async (EmployeeQueryService query, CancellationToken ct) => Results.Ok(await query.ListAsync(ct)));

app.MapGet("/api/employees/{employeeId}", async (string employeeId, EmployeeQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetAsync(employeeId, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/employees/{employeeId}/ledger", async (string employeeId, EmployeeQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListLedgerEntriesAsync(employeeId, ct)));

app.MapGet("/api/employees/{employeeId}/balance", async (string employeeId, EmployeeQueryService query, CancellationToken ct) =>
    Results.Ok(new { employeeId, balancePiastres = await query.GetBalanceAsync(employeeId, ct) }));

app.MapPost("/api/employees/{employeeId}/ledger-entries", async (string employeeId, Falah.Application.Employees.RecordManualLedgerEntryRequest body, RecordManualLedgerEntryHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { EmployeeId = employeeId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/employees/{employeeId}/cash-out", async (string employeeId, Falah.Application.Employees.RecordEmployeeCashOutRequest body, RecordEmployeeCashOutHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { EmployeeId = employeeId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/employees/{employeeId}/daily-wage", async (string employeeId, Falah.Application.Employees.RecordDailyWageRequest body, RecordDailyWageHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { EmployeeId = employeeId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/employees/{employeeId}/overtime", async (string employeeId, Falah.Application.Employees.RecordOvertimeRequest body, RecordOvertimeHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { EmployeeId = employeeId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/employees/{employeeId}/extra-shift", async (string employeeId, Falah.Application.Employees.RecordExtraShiftRequest body, RecordExtraShiftHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { EmployeeId = employeeId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// ---- Attendance ----

app.MapPost("/api/attendance/check-in", async (Falah.Application.Employees.BiometricCheckInRequest request, BiometricCheckInHandler handler, CancellationToken ct) =>
{
    var result = await handler.HandleAsync(request, ct);
    return result is null ? Results.NotFound(new { error = "الموظف غير موجود." }) : Results.Ok(result);
});

app.MapPost("/api/attendance/check-out", async (Falah.Application.Employees.BiometricCheckOutRequest request, BiometricCheckOutHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/attendance/{recordId}/correct", async (string recordId, Falah.Application.Employees.CorrectAttendanceRequest body, CorrectAttendanceHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { RecordId = recordId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/attendance", async (string? employeeId, DateOnly? date, EmployeeQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListAttendanceAsync(employeeId, date, ct)));

// ---- Payroll ----

app.MapPost("/api/payroll-runs", async (Falah.Application.Employees.CreatePayrollRequest request, CreatePayrollHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/payroll-runs", async (PayrollQueryService query, CancellationToken ct) => Results.Ok(await query.ListRunsAsync(ct)));

app.MapGet("/api/payroll-runs/{runId}", async (string runId, PayrollQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetRunAsync(runId, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/payroll-runs/{runId}/lines", async (string runId, PayrollQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListLinesAsync(runId, ct)));

app.MapPost("/api/payroll-runs/{runId}/transition", async (string runId, Falah.Application.Employees.TransitionPayrollRequest body, TransitionPayrollHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { PayrollRunId = runId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/payroll-runs/{runId}/pay", async (string runId, Falah.Application.Employees.PayPayrollRequest body, PayPayrollHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { PayrollRunId = runId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// ---- Backup / Restore ----

app.MapPost("/api/backups", async (Falah.Application.Backup.CreateBackupRequest request, CreateBackupHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/backups", async (BackupQueryService query, CancellationToken ct) => Results.Ok(await query.ListAsync(ct)));

app.MapGet("/api/backups/{id}", async (string id, BackupQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/backups/{id}/verify", async (string id, Falah.Application.Backup.VerifyBackupRequest body, VerifyBackupHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { BackupRecordId = id };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/backups/{id}/restore", async (string id, Falah.Application.Backup.RestoreBackupRequest body, RestoreBackupHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { BackupRecordId = id };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/inventory/items", async (Falah.Application.Inventory.CreateInventoryItemRequest request, CreateInventoryItemHandler handler, CancellationToken ct) =>
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

app.MapPut("/api/inventory/items/{id}", async (string id, Falah.Application.Inventory.UpdateInventoryItemRequest body, UpdateInventoryItemHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { Id = id };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
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

app.MapGet("/api/inventory/items", async (InventoryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListInventoryItemsAsync(ct)));

app.MapGet("/api/inventory/items/{id}/balance", async (string id, InventoryQueryService query, CancellationToken ct) =>
    Results.Ok(new { inventoryItemId = id, balanceMilliUnits = await query.GetStockBalanceAsync(id, ct) }));

// Not a financial command: no money, no stock, no supplier. See the handler for why that matters.
app.MapPut("/api/inventory/items/{id}/estimated-cost", async (string id, Falah.Application.Inventory.SetEstimatedCostRequest body, SetEstimatedCostHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(body with { InventoryItemId = id }, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/inventory/items/{id}/cost", async (string id, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await query.GetIngredientCostAsync(id, ct)));

app.MapGet("/api/inventory/movements", async (string? inventoryItemId, int? limit, InventoryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListStockMovementsAsync(inventoryItemId, limit ?? 50, ct)));

// The stock screen could say what was on the shelf and nothing else - not what it was worth, not
// when it last moved, not why. These two answer the screen instead of making it ask per item.
app.MapGet("/api/inventory/overview", async (string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await query.GetInventoryOverviewAsync(ct));
});

app.MapGet("/api/inventory/items/{id}/detail", async (string id, DateTimeOffset? from, DateTimeOffset? to, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await query.GetInventoryItemDetailAsync(id, from, to, ct: ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/inventory/loss-summary", async (DateOnly businessDate, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await query.GetLossSummaryAsync(businessDate, ct)));

app.MapGet("/api/inventory/items/{id}/variance", async (string id, DateTimeOffset? from, DateTimeOffset? to, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await query.GetVarianceAsync(id, from, to, ct)));

app.MapGet("/api/inventory/items/{id}/shared-consumption", async (string id, DateTimeOffset from, DateTimeOffset to, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var gate = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (gate is not null) return gate;
    try { return Results.Ok(await query.GetSharedConsumptionReportAsync(id, from, to, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/inventory/count-adjustments", async (Falah.Application.Inventory.RecordCountAdjustmentRequest request, RecordCountAdjustmentHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Inventory.InventoryConflictException ex)
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

app.MapPost("/api/inventory/waste", async (Falah.Application.Inventory.RecordWasteRequest request, RecordWasteHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Inventory.InventoryConflictException ex)
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

app.MapPost("/api/recipes", async (Falah.Application.Inventory.SaveRecipeRequest request, SaveRecipeHandler handler, CancellationToken ct) =>
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

app.MapGet("/api/recipes", async (InventoryQueryService query, CancellationToken ct) => Results.Ok(await query.ListRecipesAsync(ct)));

app.MapGet("/api/recipes/{id}", async (string id, InventoryQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetRecipeAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/recipes/{id}/cost", async (string id, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await query.GetRecipeCostAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// sellingPricePiastres is optional: omit it and the system reads the product's own price rather
// than making the caller already know the answer to half the question.
app.MapGet("/api/recipes/{id}/profitability", async (string id, long? sellingPricePiastres, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await query.GetMenuItemProfitabilityAsync(id, sellingPricePiastres, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// The recipes screen asked for a cost and then a margin, per recipe, and still had to look up every
// ingredient's name and cost itself to say anything about WHY a cost was incomplete. These three
// answer the whole screen: the menu's costing coverage, every row, and one recipe line by line.
app.MapGet("/api/recipes/cost-summary", async (string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await query.ListRecipeCostSummariesAsync(ct));
});

app.MapGet("/api/recipes/cost-coverage", async (string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await query.GetRecipeCostCoverageAsync(ct));
});

app.MapGet("/api/recipes/{id}/cost-detail", async (string id, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await query.GetRecipeCostDetailAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/prep-recipes", async (Falah.Application.Inventory.SavePrepRecipeRequest request, SavePrepRecipeHandler handler, CancellationToken ct) =>
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

app.MapGet("/api/prep-recipes", async (InventoryQueryService query, CancellationToken ct) => Results.Ok(await query.ListPrepRecipesAsync(ct)));

app.MapGet("/api/prep-recipes/{id}/cost", async (string id, string actorUserId, FalahDbContext db, InventoryQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await query.GetPrepRecipeCostAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/order-packaging-rules", async (Falah.Application.Inventory.SaveOrderPackagingRuleRequest request, SaveOrderPackagingRuleHandler handler, CancellationToken ct) =>
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

app.MapGet("/api/order-packaging-rules", async (InventoryQueryService query, CancellationToken ct) => Results.Ok(await query.ListOrderPackagingRulesAsync(ct)));

app.MapPost("/api/customers/upsert", async (Falah.Application.Customers.UpsertCustomerRequest request, UpsertCustomerHandler handler, CancellationToken ct) =>
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
});

// Live, read-only lookup - recognizes a repeat caller the moment enough digits are typed, before
// checkout and before any name is entered. Never creates or changes anything.
app.MapGet("/api/customers/lookup", async (string phone, CustomerQueryService query, CancellationToken ct) =>
{
    var result = await query.FindByPhoneAsync(phone, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/api/customers/{id}", async (string id, CustomerQueryService query, CancellationToken ct) =>
{
    var result = await query.GetByIdAsync(id, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapPost("/api/delivery/drivers", async (Falah.Application.Delivery.CreateDriverRequest request, CreateDriverHandler handler, CancellationToken ct) =>
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

app.MapGet("/api/delivery/drivers", async (DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListActiveDriversAsync(ct)));

app.MapGet("/api/delivery/drivers/all", async (DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListAllDriversAsync(ct)));

app.MapGet("/api/delivery/services", async (DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListActiveDeliveryServicesAsync(ct)));

app.MapGet("/api/delivery/services/all", async (DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListAllDeliveryServicesAsync(ct)));

app.MapPost("/api/delivery/services", async (Falah.Application.Delivery.CreateDeliveryServiceRequest request, CreateDeliveryServiceHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPut("/api/delivery/services/{serviceId}", async (string serviceId, Falah.Application.Delivery.UpdateDeliveryServiceRequest body, UpdateDeliveryServiceHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { ServiceId = serviceId };
        return Results.Ok(await handler.HandleAsync(request, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/delivery/drivers/{driverId}/set-active", async (string driverId, Falah.Application.Delivery.SetDriverActiveRequest body, SetDriverActiveHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { DriverId = driverId };
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/delivery/{id}/ready", async (string id, Falah.Application.Delivery.MarkDeliveryReadyRequest body, MarkDeliveryReadyHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { DeliveryId = id };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
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

app.MapPost("/api/delivery/{id}/assign", async (string id, Falah.Application.Delivery.AssignDeliveryRequest body, AssignDeliveryHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { DeliveryId = id };
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
    catch (Falah.Application.Delivery.DeliveryConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/delivery/{id}/reassign", async (string id, Falah.Application.Delivery.ReassignDeliveryRequest body, ReassignDeliveryHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { DeliveryId = id };
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
    catch (Falah.Application.Delivery.DeliveryConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/delivery/{id}/result", async (string id, Falah.Application.Delivery.RecordDeliveryResultRequest body, RecordDeliveryResultHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { DeliveryId = id };
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
    catch (Falah.Application.Delivery.DeliveryConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

app.MapPost("/api/delivery/drivers/{driverId}/settle", async (string driverId, Falah.Application.Delivery.SettleDriverCustodyRequest body, SettleDriverCustodyHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { DriverId = driverId };
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
    catch (Falah.Application.Delivery.DeliveryConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
});

// The live dispatch board: every delivery not yet in a terminal state, including the `New` ones a
// checkout just created. Read-only sibling of /api/delivery/ready - see ListDispatchBoardAsync.
app.MapGet("/api/delivery/board", async (DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListDispatchBoardAsync(ct)));

app.MapGet("/api/delivery/ready", async (DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListReadyForDispatchAsync(ct)));

app.MapGet("/api/delivery/drivers/{driverId}/active", async (string driverId, DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListActiveForDriverAsync(driverId, ct)));

app.MapGet("/api/delivery/drivers/{driverId}/account", async (string driverId, DeliveryQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetDriverAccountAsync(driverId, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/delivery/{id}", async (string id, DeliveryQueryService query, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await query.GetOneAsync(id, ct));
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapGet("/api/delivery/{id}/timeline", async (string id, DeliveryQueryService query, CancellationToken ct) =>
    Results.Ok(await query.GetTimelineAsync(id, ct)));

app.MapPost("/api/orders/checkout", async (Falah.Application.Orders.CheckoutOrderRequest request, CheckoutOrderHandler handler, OrderAutoPrintService autoPrint, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        // After the sale is committed, never inside it. TryPrintAsync cannot throw - see its own
        // doc comment for why a printing problem must not be able to undo a paid order. It reports
        // what happened so the station can say so rather than leaving a missing ticket silent.
        var printed = await autoPrint.TryPrintAsync(request, result, ct);
        return Results.Ok(result with { PrintJobsQueued = printed });
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

app.MapPost("/api/orders/cancel-pending-electronic-payment", async (Falah.Application.Orders.CancelPendingElectronicPaymentRequest request, CancelPendingElectronicPaymentHandler handler, CancellationToken ct) =>
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

// Rate limited like the login screen, and for the same reason: this endpoint now accepts a PIN,
// and a PIN is short enough to guess if guessing is free.
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

// Ported from AppContextType.visibleOrders in lib/store.tsx: a holder of 'orders_view_all'
// (owner/manager) browses the full history; everyone else is scoped to the system's currently
// open shift only (not filtered by cashier identity - matching the reference app's own
// `order.shiftId === activeShift.id` exactly), so a cashier can still serve a customer about work
// they actually did without turning this into a browsable record of previous shifts or other
// cashiers' work. A real gap found during the legacy-parity audit: no list endpoint existed here
// at all, so this permission boundary had no enforcement point to live on.
app.MapGet("/api/orders", async (string actorUserId, FalahDbContext db, CancellationToken ct) =>
{
    var actorUser = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct);
    if (actorUser is null) return Results.NotFound(new { error = "المستخدم غير موجود." });
    var actor = OrderMapper.ToActor(actorUser);

    IQueryable<OrderEntity> query = db.Orders.AsNoTracking().Include(o => o.Items);
    if (!AuthorizationPolicy.HasPermission(actor, "orders_view_all"))
    {
        var openShift = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == "Open", ct);
        query = openShift is null ? query.Where(o => false) : query.Where(o => o.ShiftId == openShift.Id);
    }
    var orders = await query.OrderByDescending(o => o.SequenceNumber).ToListAsync(ct);
    return Results.Ok(orders.Select(OrderMapper.ToDto));
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

// ---- Product photos ----
// The bytes are served on their own endpoint rather than inside the menu, so a till fetches each
// image once and never re-downloads the whole catalogue's pictures to refresh a price. The version
// segment in the path is what makes that safe: a replaced photo is a different URL.
app.MapGet("/api/products/{id}/image", async (string id, FalahDbContext db, CancellationToken ct) =>
{
    var image = await db.ProductImages.AsNoTracking().FirstOrDefaultAsync(i => i.ProductId == id, ct);
    return image is null ? Results.NotFound() : Results.File(image.Bytes, image.ContentType);
});

app.MapPut("/api/products/{id}/image", async (string id, Falah.Application.Menu.SetProductImageRequest body, SetProductImageHandler handler, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await handler.HandleAsync(body with { ProductId = id }, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapDelete("/api/products/{id}/image", async (string id, string actorUserId, RemoveProductImageHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(new Falah.Application.Menu.RemoveProductImageRequest(actorUserId, id), ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// Narrowly-scoped write, kept alongside the full menu management CRUD below rather than folded
// into it: assigns/clears one product's preparation station only, without needing to know or
// resend every other field on the product - a lighter-weight path for exactly this one change.
app.MapPut("/api/products/{id}/preparation-station", async (string id, Falah.Application.Printing.SetProductStationRequest request, SetProductStationHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(id, request, ct);
        return Results.Ok();
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

// ---- Menu management (categories/products/variants) ----

app.MapGet("/api/categories/all", async (MenuQueryService query, CancellationToken ct) => Results.Ok(await query.ListAllCategoriesAsync(ct)));

app.MapPost("/api/categories", async (Falah.Application.Menu.CreateCategoryRequest request, CreateCategoryHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPut("/api/categories/{id}", async (string id, Falah.Application.Menu.UpdateCategoryRequest body, UpdateCategoryHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(body with { CategoryId = id }, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/categories/{id}/active", async (string id, Falah.Application.Menu.SetCategoryActiveRequest body, SetCategoryActiveHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(body with { CategoryId = id }, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/products/all", async (MenuQueryService query, CancellationToken ct) => Results.Ok(await query.ListAllProductsAsync(ct)));

app.MapGet("/api/products/{id}", async (string id, MenuQueryService query, CancellationToken ct) =>
{
    var product = await query.GetProductAsync(id, ct);
    return product is null ? Results.NotFound() : Results.Ok(product);
});

app.MapPost("/api/products", async (Falah.Application.Menu.CreateProductRequest request, CreateProductHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPut("/api/products/{id}", async (string id, Falah.Application.Menu.UpdateProductRequest body, UpdateProductHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(body with { ProductId = id }, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/products/{id}/availability", async (string id, Falah.Application.Menu.SetProductAvailabilityRequest body, SetProductAvailabilityHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(body with { ProductId = id }, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// ---- Tax rules ----

app.MapGet("/api/tax-rules", async (TaxRuleQueryService query, CancellationToken ct) => Results.Ok(await query.ListAsync(ct)));

app.MapPost("/api/tax-rules", async (Falah.Application.Pricing.CreateTaxRuleRequest request, CreateTaxRuleHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPut("/api/tax-rules/{id}", async (string id, Falah.Application.Pricing.UpdateTaxRuleRequest body, UpdateTaxRuleHandler handler, CancellationToken ct) =>
{
    try
    {
        await handler.HandleAsync(body with { TaxRuleId = id }, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// ---- Cash drawer (hardware boundary) - Cashier machine only by architecture, never touches
// Treasury/payment/printing; a drawer that fails to physically open can never affect what was
// already sold or collected. See CashDrawerOpenEntity's own doc comment. ----

app.MapPost("/api/cash-drawer/open", async (Falah.Application.CashDrawer.OpenCashDrawerRequest request, OpenCashDrawerHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/cash-drawer/report-result", async (Falah.Application.CashDrawer.ReportCashDrawerResultRequest request, ReportCashDrawerResultHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/cash-drawer/cash-movement", async (Falah.Application.CashDrawer.CashierCashMovementRequest request, CashierCashMovementHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Treasury.TreasuryConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// ---- Price rules / offers ----

app.MapGet("/api/price-rules", async (PriceRuleQueryService query, CancellationToken ct) => Results.Ok(await query.ListAsync(ct)));

app.MapPost("/api/price-rules", async (Falah.Application.Pricing.SavePriceRuleRequest request, SavePriceRuleHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// Pure read, derived the same way every balance in this system is derived - by summing posted
// movements, never from a separately-maintained running total that could drift.
app.MapGet("/api/treasury/balance", async (string accountId, FalahDbContext db, CancellationToken ct) =>
{
    // A name that is not an account gets a 404, never a balance of zero. Zero is an answer about
    // money; "no such account" is an answer about the question, and a caller that mistypes an
    // account id deserves to be told rather than shown an empty till.
    if (!Falah.Domain.Financial.TreasuryAccounts.All.Contains(accountId))
        return Results.NotFound(new { error = "لا يوجد حساب خزنة بهذا المعرّف." });

    var movements = await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsAsync(db, accountId, ct);
    var balance = Falah.Domain.Financial.FinancialRules.TreasuryBalanceFromMovements(movements, accountId);
    return Results.Ok(new { accountId, balancePiastres = balance.Piastres });
});

// Balances for all five fixed accounts in one read - a treasury screen showing five separately-timed
// reads can render a picture that never actually existed. Derived from the movement log by the same
// FinancialRules the individual balance endpoint uses; no stored running total anywhere.
app.MapGet("/api/treasury/balances", async (string actorUserId, FalahDbContext db, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;

    var accounts = new[]
    {
        Falah.Domain.Financial.TreasuryAccounts.Main, Falah.Domain.Financial.TreasuryAccounts.Drawer,
        Falah.Domain.Financial.TreasuryAccounts.Bank, Falah.Domain.Financial.TreasuryAccounts.InstaPay,
        Falah.Domain.Financial.TreasuryAccounts.Wallet,
    };
    var balances = new List<Falah.Application.Treasury.TreasuryAccountBalanceDto>(accounts.Length);
    foreach (var accountId in accounts)
    {
        var movements = await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsAsync(db, accountId, ct);
        var balance = Falah.Domain.Financial.FinancialRules.TreasuryBalanceFromMovements(movements, accountId);
        balances.Add(new(accountId, balance.Piastres));
    }
    return Results.Ok(balances);
});

// The balances above say what the restaurant HOLDS. This says what is out there and not in any
// account yet: an electronic transfer nobody has confirmed, and cash a driver is still carrying.
app.MapGet("/api/treasury/position", async (string actorUserId, FalahDbContext db, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;

    var accounts = new[]
    {
        Falah.Domain.Financial.TreasuryAccounts.Main, Falah.Domain.Financial.TreasuryAccounts.Drawer,
        Falah.Domain.Financial.TreasuryAccounts.Bank, Falah.Domain.Financial.TreasuryAccounts.InstaPay,
        Falah.Domain.Financial.TreasuryAccounts.Wallet,
    };
    var balances = new List<Falah.Application.Treasury.TreasuryAccountBalanceDto>(accounts.Length);
    foreach (var accountId in accounts)
    {
        var movements = await Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsAsync(db, accountId, ct);
        var balance = Falah.Domain.Financial.FinancialRules.TreasuryBalanceFromMovements(movements, accountId);
        balances.Add(new(accountId, balance.Piastres));
    }

    var pending = await db.Orders.AsNoTracking()
        .Where(o => o.ElectronicPaymentStatus == "Pending" && o.ElectronicPaymentAmountPiastres != null)
        .Select(o => o.ElectronicPaymentAmountPiastres!.Value)
        .ToListAsync(ct);

    // Expected, not collected: what the driver is meant to bring back is what the restaurant is
    // owed, and a custody row that is open has not been reconciled against anything yet.
    var custody = await db.DriverCustodies.AsNoTracking()
        .Where(c => c.Status != nameof(Falah.Domain.Delivery.CustodyStatus.Settled))
        .Select(c => new { c.DriverId, c.ExpectedPiastres })
        .ToListAsync(ct);

    return Results.Ok(new Falah.Application.Treasury.TreasuryPositionDto(
        balances,
        balances.Sum(b => b.BalancePiastres),
        pending.Sum(), pending.Count,
        custody.Sum(c => c.ExpectedPiastres), custody.Select(c => c.DriverId).Distinct().Count()));
});

// The treasury ledger itself - the posted movements behind those balances. Read-only over the same
// rows every financial command writes, so the ledger and the balances can never disagree.
app.MapGet("/api/treasury/movements", async (string actorUserId, string? accountId, int? limit, FalahDbContext db, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;

    var query = db.TreasuryMovements.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(accountId))
        query = query.Where(m => m.SourceAccountId == accountId || m.DestinationAccountId == accountId);

    var rows = await query.OrderByDescending(m => m.CreatedAtUtc).Take(limit is > 0 and <= 500 ? limit.Value : 100).ToListAsync(ct);
    return Results.Ok(rows.Select(m => new Falah.Application.Treasury.TreasuryMovementDto(
        m.Id, m.Type, m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres, m.ReferenceType,
        m.ReferenceId, m.ShiftId, m.Reason, m.CreatedBy, m.ApprovedBy, m.CreatedAtUtc)).ToList());
});

app.MapGet("/api/treasury/handovers", async (int limit, FalahDbContext db, CancellationToken ct) =>
{
    var rows = await db.CashHandovers.AsNoTracking().OrderByDescending(h => h.CreatedAtUtc).Take(limit <= 0 ? 50 : limit).ToListAsync(ct);
    return Results.Ok(rows.Select(h => new
    {
        h.Id, h.FromAccountId, h.ToAccountId, h.AmountPiastres, h.HandedBy, h.ReceivedBy,
        h.ShiftId, h.Notes, h.TreasuryMovementId, h.CreatedAtUtc,
    }));
});

app.MapGet("/api/users", async (FalahDbContext db, CancellationToken ct) =>
{
    var users = await db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
    return Results.Ok(users.Select(ToUserSummary));
});

// PIN travels in the request body, never a query string - the lock screen's entire login contract
// is "this PIN alone" (no username), matching the reference app's unlockScreen exactly. The PIN is
// hashed, not stored/compared in plain text (see PinHasher's own doc comment for why a slow KDF
// matters for a low-entropy secret) - since a per-user random salt means no two hashes for the same
// PIN are ever equal, the lookup can no longer be a single indexed SQL equality check; it verifies
// against every active user's own hash instead (realistic staff counts make this trivially cheap).
// Rate-limited (see the "login" policy registered above) as a generic technical safeguard against
// PIN brute-forcing - not a restaurant policy, just standard API hygiene for an unauthenticated,
// low-entropy-secret endpoint.
app.MapPost("/api/auth/login-by-pin", async (Falah.Application.Users.LoginByPinRequest request, FalahDbContext db, Falah.Infrastructure.Authorization.SessionService sessions, CancellationToken ct) =>
{
    var candidates = await db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
    var user = candidates.FirstOrDefault(u => Falah.Domain.Authorization.PinHasher.Verify(request.Pin, u.PinHash, u.PinSalt, u.PinIterations));
    if (user is null)
    {
        db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = "pin_login_failed", Actor = "unknown",
            EntityId = "login", CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        await db.SaveChangesAsync(ct);
        return Results.Unauthorized();
    }
    var token = await sessions.IssueAsync(user.Id, ct);
    var summary = ToUserSummary(user);
    return Results.Ok(new Falah.Application.Users.LoginResultDto(
        summary.Id, summary.Name, summary.Role, summary.Permissions, token));
}).RequireRateLimiting("login");

// Ends the session behind the presented token. Logging out has to actually stop the token working,
// not merely clear the screen - a station left unlocked is exactly when that matters.
app.MapPost("/api/auth/logout", async (HttpContext http, Falah.Infrastructure.Authorization.SessionService sessions, CancellationToken ct) =>
{
    var header = http.Request.Headers.Authorization.ToString();
    var token = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase) ? header["Bearer ".Length..].Trim() : null;
    await sessions.RevokeAsync(token, ct);
    return Results.Ok();
});

// ---- User management (all gated on users_manage - see RequireUsersManageAsync's own doc comment) ----

app.MapGet("/api/users/all", async (string actorUserId, FalahDbContext db, UserManagementQueryService query, CancellationToken ct) =>
    await RequireUsersManageAsync(db, actorUserId, ct) ?? Results.Ok(await query.ListAllAsync(ct)));

app.MapGet("/api/users/{id}", async (string id, string actorUserId, FalahDbContext db, UserManagementQueryService query, CancellationToken ct) =>
{
    var gate = await RequireUsersManageAsync(db, actorUserId, ct);
    if (gate is not null) return gate;
    var user = await query.GetAsync(id, ct);
    return user is null ? Results.NotFound() : Results.Ok(user);
});

app.MapGet("/api/permissions/catalog", (UserManagementQueryService query) => Results.Ok(query.GetPermissionCatalog()));

app.MapPost("/api/users", async (Falah.Application.Users.CreateUserRequest request, CreateUserHandler handler, CancellationToken ct) =>
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

app.MapPost("/api/users/{id}/active", async (string id, Falah.Application.Users.SetUserActiveRequest body, SetUserActiveHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { UserId = id };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
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

app.MapPost("/api/users/{id}/change-pin", async (string id, Falah.Application.Users.ChangePinRequest body, ChangePinHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { UserId = id };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
    }
    catch (Falah.Domain.Common.DomainException ex)
    {
        return Results.UnprocessableEntity(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
}).RequireRateLimiting("login"); // self-service path is proven only by the current PIN - same brute-force exposure as login itself

app.MapPost("/api/users/{id}/permissions", async (string id, Falah.Application.Users.UpdateUserPermissionsRequest body, UpdateUserPermissionsHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { UserId = id };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
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

// ---- Reports (all gated on reports_view - see RequireReportsAccessAsync's own doc comment) ----

// businessDate stays supported so existing callers are untouched; from/to widen the same report to
// a week, a month or any span without stitching single days together client-side.
// One read that answers "how is the restaurant doing" - composed from the same queries the
// individual screens use, never a second computation of the same figures. Gated on reports_view
// like every other report.
app.MapGet("/api/reports/owner-dashboard", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, FalahDbContext db, OwnerDashboardQueryService dashboard, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    var range = Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate)
        ?? Falah.Domain.Common.BusinessDateRange.SingleDay(Falah.Domain.Common.BusinessClock.Today());
    return Results.Ok(await dashboard.GetAsync(range, ct));
});

app.MapGet("/api/reports/sales", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, string? shiftId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetSalesReportAsync(
        Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate), shiftId, ct)));

app.MapGet("/api/reports/tax", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, string? shiftId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetTaxReportAsync(
        Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate), shiftId, ct)));

app.MapGet("/api/reports/expenses", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, string? shiftId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetExpenseReportAsync(
        Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate), shiftId, ct)));

app.MapGet("/api/reports/refunds", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, string? shiftId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetRefundActivityAsync(
        Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate), shiftId, ct)));

app.MapGet("/api/reports/business-day/{id}", async (string id, string actorUserId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await reports.GetBusinessDayReconciliationAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/reports/top-items", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetTopSellingItemsAsync(from, to, ct)));

app.MapGet("/api/reports/category-sales", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetCategorySalesAsync(from, to, ct)));

app.MapGet("/api/reports/food-cost", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetFoodCostReportAsync(from, to, ct)));

app.MapGet("/api/reports/significant-actions", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetSignificantActionsAsync(from, to, ct)));

app.MapGet("/api/reports/shifts/{id}", async (string id, string actorUserId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await reports.GetShiftSummaryAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/reports/delivery-outcomes", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetDeliveryOutcomesReportAsync(from, to, ct)));

// ---- Printing: station-based print job routing/tracking. Creating or reporting on a job NEVER
// touches the order/payment/inventory it describes - see CreatePrintJobsHandler's own doc comment
// for the critical invariant this whole feature exists to preserve. ----

app.MapPost("/api/orders/{id}/print-jobs", async (string id, Falah.Application.Printing.CreatePrintJobsRequest body, CreatePrintJobsHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { OrderId = id };
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Printing.PrintingConflictException ex)
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

app.MapGet("/api/orders/{id}/print-jobs", async (string id, PrintJobQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListJobsForOrderAsync(id, ct)));

app.MapPost("/api/print-jobs/{id}/report-result", async (string id, Falah.Application.Printing.ReportPrintJobResultRequest body, ReportPrintJobResultHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { PrintJobId = id };
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Printing.PrintingConflictException ex)
    {
        return Results.Conflict(new { error = ex.Message });
    }
    catch (InvalidOperationException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
});

app.MapGet("/api/print-jobs/pending", async (string route, PrintJobQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListPendingJobsForRouteAsync(route, ct)));

app.MapGet("/api/print-jobs/{id}", async (string id, PrintJobQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetJobDetailsAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/print-jobs/{id}/ticket", async (string id, PrintJobQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.ComposeTicketContentAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/suppliers", async (Falah.Application.Purchasing.AddSupplierRequest request, AddSupplierHandler handler, CancellationToken ct) =>
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

app.MapGet("/api/suppliers", async (PurchasingQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListSuppliersAsync(ct)));

app.MapGet("/api/suppliers/{id}", async (string id, PurchasingQueryService query, CancellationToken ct) =>
{
    var supplier = await query.GetSupplierAsync(id, ct);
    return supplier is null ? Results.NotFound() : Results.Ok(supplier);
});

app.MapGet("/api/suppliers/{id}/ledger", async (string id, PurchasingQueryService query, CancellationToken ct) =>
    Results.Ok(await query.GetSupplierLedgerAsync(id, ct)));

app.MapPost("/api/purchases", async (Falah.Application.Purchasing.ReceivePurchaseRequest request, ReceivePurchaseHandler handler, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Purchasing.PurchasingConflictException ex)
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

app.MapGet("/api/purchases", async (PurchasingQueryService query, CancellationToken ct) =>
    Results.Ok(await query.ListPurchasesAsync(ct)));

// Registered before /api/purchases/{id} so the literal segment is not swallowed by the id route.
app.MapGet("/api/purchases/payables-aging", async (PurchasingQueryService query, CancellationToken ct) =>
    Results.Ok(await query.GetPayablesAgingAsync(ct)));

app.MapGet("/api/purchases/{id}", async (string id, PurchasingQueryService query, CancellationToken ct) =>
{
    var purchase = await query.GetPurchaseAsync(id, ct);
    return purchase is null ? Results.NotFound() : Results.Ok(purchase);
});

app.MapPut("/api/purchases/{id}/due-date", async (string id, Falah.Application.Purchasing.SetPurchaseDueDateRequest body, SetPurchaseDueDateHandler handler, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await handler.HandleAsync(body with { PurchaseId = id }, ct));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/purchases/{id}/payments", async (string id, Falah.Application.Purchasing.PayPurchaseRequest body, PayPurchaseHandler handler, CancellationToken ct) =>
{
    try
    {
        var request = body with { PurchaseId = id };
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (Falah.Application.Purchasing.PurchasingConflictException ex)
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

// A liveness/connectivity probe the client polls to show Online/Offline - deliberately checks the
// database too, since a server process that is "up" but cannot reach its own database is not
// actually usable.
app.MapGet("/health", async (FalahDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct) ? Results.Ok(new { status = "ok" }) : Results.StatusCode(503));

app.Run();

static Falah.Application.Users.UserSummaryDto ToUserSummary(UserEntity user) => new(
    user.Id, user.Name, user.Role,
    user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList());

// Ported from the reference app's own `hasPermission('reports_view')` gate on CashScreen/
// CostingScreen/ShiftsScreen/InventoryScreen/DeliveryTablesScreen - every screen that shows
// aggregate figures (reports, shift totals, cost/profit numbers, driver accounts) requires this
// ONE permission, checked here server-side rather than only hiding the number client-side. Gated
// by permission (or "all"), never by role name, matching every other authorization check in this
// codebase.
static async Task<IResult?> RequireReportsAccessAsync(FalahDbContext db, string actorUserId, CancellationToken ct)
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct);
    if (user is null) return Results.NotFound(new { error = "المستخدم غير موجود." });
    var permissions = user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (!permissions.Contains("all") && !permissions.Contains("reports_view"))
        return Results.UnprocessableEntity(new { error = "ليس لديك صلاحية عرض التقارير." });
    return null;
}

// Read-side gate for the user-management list/detail endpoints - these expose every user's role,
// active flag, and full permission list, so (unlike GET /api/users, the login screen's own
// active-only picker) they are not open to just anyone.
static async Task<IResult?> RequireUsersManageAsync(FalahDbContext db, string actorUserId, CancellationToken ct)
{
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == actorUserId, ct);
    if (user is null) return Results.NotFound(new { error = "المستخدم غير موجود." });
    var permissions = user.Permissions.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    if (!permissions.Contains("all") && !permissions.Contains("users_manage"))
        return Results.UnprocessableEntity(new { error = "ليس لديك صلاحية إدارة المستخدمين." });
    return null;
}

public partial class Program { }
