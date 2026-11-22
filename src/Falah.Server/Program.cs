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
using Microsoft.Extensions.DependencyInjection.Extensions;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Lets this same executable run as a Windows service, so the till's back end comes up with the
// machine and is restarted by Windows if it ever dies mid-shift - a cashier cannot ring up a sale
// while it is down, and nobody in a kitchen is going to notice a console window has closed.
// A no-op when launched normally (dotnet run, or double-clicking the exe), so development and the
// installed service run the exact same build. See tools/install-falah-service.ps1.
builder.Host.UseWindowsService(options => options.ServiceName = "FalahServer");

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
builder.Services.AddScoped<DatabaseActivityScope>(sp => new DatabaseActivityScope(
    sp.GetRequiredService<IConfiguration>().GetConnectionString("Falah")
        ?? @"Server=.\SQLEXPRESS;Database=Falah;Trusted_Connection=True;TrustServerCertificate=True;",
    sp.GetRequiredService<IConfiguration>().GetValue("Database:MaintenanceDrainTimeoutMilliseconds", 5000)));
builder.Services.AddScoped<IApprovalRegistry, EfApprovalRegistry>();
// The clock approvals and PIN limits are judged by; a test host may register its own.
builder.Services.TryAddSingleton(TimeProvider.System);

builder.Services.AddScoped<MenuPricingService>();
builder.Services.AddScoped<InventoryDeductionService>();
builder.Services.AddScoped<CheckoutOrderHandler>();
builder.Services.AddScoped<OpenShiftHandler>();
builder.Services.AddScoped<PrepareShiftCloseHandler>();
builder.Services.AddScoped<CloseShiftHandler>();
builder.Services.AddScoped<RefundOrderHandler>();
builder.Services.AddScoped<ConfirmElectronicPaymentHandler>();
builder.Services.AddScoped<CancelPendingElectronicPaymentHandler>();
builder.Services.AddScoped<CancelUndispatchedDeliveryHandler>();
builder.Services.AddScoped<IssueManagerApprovalHandler>();
// One per process: the PIN guessing allowance must be shared by every request, or it limits nothing.
builder.Services.AddSingleton(sp => new ApprovalPinAttemptGuard(sp.GetRequiredService<IConfiguration>(), TimeProvider.System));
// One per process for the same reason; its counts live in the audit trail, so they outlast a restart.
builder.Services.AddSingleton(sp => new PinChoiceGuard(sp.GetRequiredService<IConfiguration>(), TimeProvider.System));
// Login-by-PIN guessing (Security:Login:*). Rebuilt from the audit trail on first use after a start.
builder.Services.AddSingleton(sp => new Falah.Infrastructure.Authorization.LoginAttemptGuard(
    sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<TimeProvider>(), sp.GetRequiredService<IServiceScopeFactory>()));
builder.Services.AddScoped<MenuQueryService>();
builder.Services.AddScoped<OpenBusinessDayHandler>();
builder.Services.AddScoped<DepositHandler>();
builder.Services.AddScoped<WithdrawalHandler>();
builder.Services.AddScoped<TransferHandler>();
builder.Services.AddScoped<SafeDropHandler>();
builder.Services.AddScoped<AddExpenseHandler>();
builder.Services.AddScoped<ReverseExpenseHandler>();
builder.Services.AddScoped<CorrectTreasuryMovementHandler>();
builder.Services.AddScoped<RecordSupplierCreditHandler>();
builder.Services.AddScoped<ClassifyCollectionShortfallHandler>();
builder.Services.AddScoped<AcknowledgeShiftHandoverHandler>();
builder.Services.AddScoped<OwnerFinancialReportQueryService>();
builder.Services.AddScoped<Falah.Infrastructure.Reports.Export.ReportSnapshotBuilder>();
builder.Services.AddScoped<Falah.Infrastructure.Reports.Export.ReportExportService>();
builder.Services.AddScoped<CreateEmployeeHandler>();
builder.Services.AddScoped<UpdateEmployeeHandler>();
builder.Services.AddScoped<EmployeeQueryService>();
builder.Services.AddScoped<RecordManualLedgerEntryHandler>();
builder.Services.AddScoped<RecordEmployeeCashOutHandler>();
builder.Services.AddScoped<PayEmployeeHandler>();
builder.Services.AddScoped<SalaryAccrualService>();
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

// The backup nobody remembers to take. Before this, a backup only ever happened when an operator
// opened the Backup screen and pressed the button, and the owner dashboard's "last backup was N
// hours ago" warning was the entire safety net. See ScheduledBackupService for why it is an age
// check on an interval rather than a nightly alarm - a till is switched off at night.
builder.Services.AddHostedService<ScheduledBackupService>();
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
builder.Services.AddScoped<DailyStatementQueryService>();
builder.Services.AddScoped<CreatePrintJobsHandler>();
builder.Services.AddScoped<Falah.Infrastructure.Printing.ReprintDocumentHandler>();
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
builder.Services.AddScoped<CashDrawerKickQueue>();
builder.Services.AddScoped<CashierCashMovementHandler>();

// Cross-origin requests are refused unless an origin is configured (Cors:AllowedOrigins). The tills
// and print agents are native programs, which CORS does not concern; allowing any origin only let a
// web page opened on any staff PC on the LAN drive the API through that browser - send PIN guesses
// to login-by-pin and read back the session token of a right one. Read from the final configuration
// like the rate limits above, so a host's own settings (and a test's) are the ones applied.
builder.Services.AddCors();
builder.Services.AddOptions<Microsoft.AspNetCore.Cors.Infrastructure.CorsOptions>().Configure<IConfiguration>((options, configuration) =>
{
    var origins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? Array.Empty<string>();
    options.AddDefaultPolicy(p =>
    {
        if (origins.Length > 0) p.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader();
    });
});

var app = builder.Build();
// A script-only invocation opens no listener/database connection. Normal hosting fails before
// database startup if a non-loopback cleartext endpoint was configured.
if (string.IsNullOrWhiteSpace(app.Configuration["Database:MigrationScript"]))
    Falah.Server.ServerTransportGuard.ValidateBindings(app.Configuration, app.Urls);
if (Falah.Server.ServerTransportGuard.AllowsRemoteHttp(app.Configuration))
    app.Logger.LogCritical("Security:Transport:AllowInsecureRemoteHttp is enabled. Network PINs and session tokens may travel without TLS. Remove this override after configuring HTTPS.");
app.Use(async (context, next) =>
{
    if (!Falah.Server.ServerTransportGuard.AllowsRemoteHttp(app.Configuration) && Falah.Server.ServerTransportGuard.IsInsecureNetworkRequest(context))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        await context.Response.WriteAsJsonAsync(new { error = "الاتصال بالخادم عبر الشبكة يتطلب HTTPS.", retryable = false });
        return;
    }
    await next();
});
app.UseCors();
app.UseRateLimiter();

// Wrapped so a startup failure says WHAT failed and WHERE it was pointing. Running as a Windows
// service there is no console to read: the service simply refuses to start, and without this the
// only trace is a generic "service terminated unexpectedly" in the event log. Nothing is swallowed -
// the exception is rethrown after it has been logged, so the service still fails to start.
try
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<FalahDbContext>();
    // Generate a reviewable, idempotent script without opening a database connection.
    if (app.Configuration["Database:MigrationScript"] is { Length: > 0 } scriptPath)
    {
        var migrator = Microsoft.EntityFrameworkCore.Infrastructure.AccessorExtensions.GetService<Microsoft.EntityFrameworkCore.Migrations.IMigrator>(db);
        File.WriteAllText(scriptPath, migrator.GenerateScript(options: Microsoft.EntityFrameworkCore.Migrations.MigrationsSqlGenerationOptions.Idempotent));
        return;
    }
    var migrateOnStartup = app.Configuration.GetValue<bool?>("Database:MigrateOnStartup") ?? app.Environment.IsDevelopment();
    if (migrateOnStartup) await db.Database.MigrateAsync();
    else if ((await db.Database.GetPendingMigrationsAsync()).Any())
        throw new InvalidOperationException("هناك تحديثات لقاعدة البيانات لم تُطبّق. راجع السكربت وخذ نسخة احتياطية أولًا؛ Database:MigrateOnStartup معطل في الإنتاج.");
    // READ_COMMITTED_SNAPSHOT is the SQL Server equivalent of what WAL gave the SQLite version of
    // this system: Call Center's reads proceed from a consistent snapshot without waiting on a
    // Cashier write in progress, and vice versa - only actual writers ever block each other. Runs
    // once at startup before any real client connects, so ROLLBACK IMMEDIATE never has a live
    // transaction to actually roll back.
    db.Database.ExecuteSqlRaw(
        "IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE database_id = DB_ID() AND is_read_committed_snapshot_on = 1) " +
        "ALTER DATABASE CURRENT SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK IMMEDIATE;");
    // The demo users' PINs are published in this code, so they are seeded only on a development host
    // (and test hosts, which run as Development) or when an installation opts in on purpose. Running
    // locally is not a reason by itself: the restaurant's own service runs on this same machine.
    var seedDemoUsers = app.Environment.IsDevelopment() || app.Configuration.GetValue("Seed:DemoUsers", false);
    await DemoDataSeeder.SeedIfEmptyAsync(db, seedDemoUsers);

    var startupLog = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Falah.Server.Startup");
    var ownerCreated = false;
    try
    {
        ownerCreated = await InitialOwner.EnsureAsync(db, app.Configuration["Bootstrap:OwnerPin"]);
    }
    catch (Falah.Domain.Common.DomainException weak)
    {
        startupLog.LogCritical("لم يُنشأ حساب المالك الأول: الرمز في Bootstrap:OwnerPin مرفوض ({Reason}). اختر رمزاً آخر وأعد تشغيل الخدمة.", weak.Message);
    }
    if (ownerCreated)
        startupLog.LogWarning("تم إنشاء حساب المالك الأول من إعداد Bootstrap:OwnerPin. احذف هذا الإعداد من الخادم الآن وغيّر رمز المالك من شاشة المستخدمين.");
    else if (!await db.Users.AnyAsync())
        startupLog.LogCritical("لا يوجد أي مستخدم في قاعدة البيانات، فلا يمكن لأحد تسجيل الدخول. اضبط Bootstrap__OwnerPin على الخادم ثم أعد تشغيل خدمة FalahServer لإنشاء حساب المالك الأول.");
    else if (!string.IsNullOrWhiteSpace(app.Configuration["Bootstrap:OwnerPin"]))
        startupLog.LogWarning("الإعداد Bootstrap:OwnerPin ما زال مضبوطاً على الخادم ويتم تجاهله لوجود مستخدمين. احذفه.");

    // Never changed here - deactivating or re-keying accounts is the owner's decision. Said loudly
    // (event log + the security endpoint) so it cannot go unnoticed on a live till.
    if (!app.Environment.IsDevelopment()
        && await DefaultCredentials.ActiveUsersWithDemoPinsAsync(db) is { Count: > 0 } exposed)
    {
        startupLog.LogCritical("حسابات نشطة ما زالت تستخدم رموز الدخول التجريبية المنشورة: {Users}. غيّر رموزها أو أوقفها قبل التشغيل الفعلي.",
            string.Join("، ", exposed));
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("Falah.Server.Startup");
    // The server name and database, never the credentials - this line lands in the Windows event
    // log, which is read by whoever is standing at the till, not only by a developer.
    var target = app.Configuration.GetConnectionString("Falah") is { } cs
        ? string.Join("; ", cs.Split(';').Where(part =>
            part.TrimStart().StartsWith("Server", StringComparison.OrdinalIgnoreCase)
            || part.TrimStart().StartsWith("Database", StringComparison.OrdinalIgnoreCase)))
        : "(الاتصال الافتراضي)";

    logger.LogCritical(ex,
        "فشل بدء تشغيل خادم فالح: تعذر تهيئة قاعدة البيانات [{Target}]. "
        + "تأكد أن خدمة SQL Server تعمل وأن الخادم يملك صلاحية الوصول لقاعدة البيانات، ثم أعد تشغيل خدمة FalahServer.",
        target);
    throw;
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
//
// Registered BEFORE the authentication middleware below, so it wraps it: a session lookup that
// fails (SQL Server down or restarting) used to skip this handler entirely.
app.UseExceptionHandler(branch => branch.Run(async context =>
{
    var feature = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>();
    var reference = context.TraceIdentifier;

    // A request the framework could not bind (a required query value missing, a body that is not
    // valid JSON) is the caller's mistake, not a server fault: answer 400 and log it as a warning,
    // so it neither alarms the operator with "unexpected error" nor hides real 500s in the log.
    if (feature?.Error is BadHttpRequestException badRequest)
    {
        app.Logger.LogWarning("Rejected malformed {Method} {Path}: {Message} (reference {Reference})",
            context.Request.Method, context.Request.Path, badRequest.Message, reference);
        context.Response.StatusCode = badRequest.StatusCode;
        context.Response.ContentType = "application/json; charset=utf-8";
        await context.Response.WriteAsJsonAsync(new
        {
            error = "الطلب ناقص أو غير صالح. حدّث البرنامج وأعد المحاولة، وإن تكرر أبلغ الدعم بالرقم المرجعي.",
            reference,
        });
        return;
    }

    // A constraint rejection is definitive: SQL refused the write. Never expose table/key values.
    if (feature?.Error is DbUpdateException { InnerException: Microsoft.Data.SqlClient.SqlException { Number: 547 } })
    {
        app.Logger.LogWarning(feature.Error, "Constraint rejected {Method} {Path} (reference {Reference})", context.Request.Method, context.Request.Path, reference);
        context.Response.StatusCode = StatusCodes.Status409Conflict;
        await context.Response.WriteAsJsonAsync(new { error = "تعذر حفظ التعديل لأن البيانات مرتبطة بسجلات أخرى أو تغيّرت. حدّث البيانات وراجع التعديل.", reference, retryable = false });
        return;
    }

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

// Session resolution itself accesses the target database, so acquire before authentication.
app.Use(async (context, next) =>
{
    if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)) { await next(); return; }
    var activity = context.RequestServices.GetRequiredService<DatabaseActivityScope>();
    try
    {
        var restoreIntent = HttpMethods.IsPost(context.Request.Method)
            && context.Request.Path.StartsWithSegments("/api/backups", StringComparison.OrdinalIgnoreCase)
            && (context.Request.Path.Value?.EndsWith("/restore", StringComparison.OrdinalIgnoreCase) ?? false);
        await activity.EnterRequestAsync(restoreIntent, context.RequestAborted);
        await next();
    }
    catch (DatabaseMaintenanceBusyException ex)
    {
        context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await context.Response.WriteAsJsonAsync(new { error = ex.Message, retryable = false });
    }
    finally { await activity.DisposeAsync(); }
});
app.UseMiddleware<Falah.Server.AuthenticationMiddleware>();

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
    if (shift is null) return Results.NotFound(new { error = "لا توجد وردية مفتوحة حاليًا." });
    var openedByName = (await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == shift.OpenedByUserId, ct))?.Name;
    return Results.Ok(new Falah.Application.Shifts.CurrentShiftDto(
        shift.Id, shift.ShiftName, shift.OpenedByUserId, shift.OpeningCashPiastres, shift.OpenedAtUtc, openedByName));
});

// The preview reveals what the drawer SHOULD hold - the one thing a blind count must not know in
// advance - so it is gated on the same permission the close itself requires. The request names no
// actor of its own, so the gate reads the authenticated session.
app.MapPost("/api/shifts/prepare-close", async (Falah.Application.Shifts.PrepareShiftCloseRequest request, HttpContext http, PrepareShiftCloseHandler handler, CancellationToken ct) =>
{
    var declarer = Falah.Server.HttpContextActorExtensions.Actor(http);
    if (!Falah.Domain.Authorization.AuthorizationPolicy.HasPermission(OrderMapper.ToActor(declarer), "shifts_blind_close"))
        return Results.UnprocessableEntity(new { error = "ليس لديك صلاحية تقفيل الوردية." });
    try
    {
        // Every count is recorded against whoever typed it, so a re-count is visible to the manager.
        var result = await handler.HandleAsync(request, (declarer.Id, declarer.Name), ct);
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

// Every cash count declared for a shift, in order (FIN5). Management information: a re-count after
// seeing the expected cash is legitimate after a miscount, and visible here either way.
// The float the last close left in the drawer, for the open dialog: read by whoever may open a shift.
app.MapGet("/api/shifts/drawer-float", async (FalahDbContext db, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "ليس لديك صلاحية فتح وردية.", "shifts_open_own", "shifts_manage")
    ?? Results.Ok(await Falah.Infrastructure.Shifts.DrawerFloat.LoadAsync(db, ct)));

// Who may receive a close's handed-over cash - names only, for the close dialog's picker: read by whoever may close.
app.MapGet("/api/shifts/handover-receivers", async (FalahDbContext db, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "ليس لديك صلاحية إغلاق وردية.", "shifts_blind_close", "shifts_manage")
    ?? Results.Ok(await Falah.Infrastructure.Shifts.DrawerFloat.ReceiversAsync(db, ct)));

app.MapGet("/api/shifts/{id}/cash-declarations", async (string id, FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية عرض عدّ الورديات.", "shifts_manage", "reports_view") is { } refused) return refused;
    var rows = await db.ControlAuditEvents.AsNoTracking()
        .Where(e => e.Action == PrepareShiftCloseHandler.DeclarationAuditAction && e.EntityId == id)
        .OrderBy(e => e.CreatedAtUtc).ToListAsync(ct);
    return Results.Ok(rows.Select((e, i) => new Falah.Application.Shifts.ShiftCashDeclarationDto(
        i + 1, e.AmountPiastres ?? 0, e.Actor, e.CreatedAtUtc)).ToList());
});

// UI-REQ-002: the frozen closing statement and the state of its paper. The cashier whose shift it is
// may read it (they were handed the same paper); anyone else needs shifts_manage or reports_view.
app.MapGet("/api/shifts/{id}/close-statement", async (string id, FalahDbContext db, PrintJobQueryService prints, HttpContext http, CancellationToken ct) =>
{
    var actor = Falah.Server.HttpContextActorExtensions.Actor(http);
    var shift = await db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id, ct);
    // Refuse before saying whether the shift exists: without shifts_manage/reports_view a session may read only a
    // shift it opened or closed, and an unknown id answers exactly like someone else's shift.
    var isOwn = shift is not null && (shift.OpenedByUserId == actor.Id || shift.ClosedByUserId == actor.Id);
    if (!isOwn && RequireAnyPermission(http, "ليس لديك صلاحية عرض ورقة إغلاق هذه الوردية.", "shifts_manage", "reports_view") is { } refused) return refused;
    if (shift is null) return Results.NotFound(new { error = "الوردية غير موجودة." });
    if (shift.ClosedAtUtc is null) return Results.UnprocessableEntity(new { error = "الوردية لم تُغلق بعد." });
    var (statement, recomputed) = await prints.LoadCloseStatementAsync(id, ct);
    var jobs = await db.PrintJobs.AsNoTracking()
        .Where(j => j.OrderId == id && j.OrderNumber == Falah.Domain.Printing.PrintRules.ShiftCloseLabel)
        .OrderBy(j => j.CreatedAtUtc)
        .Select(j => new Falah.Application.Shifts.ShiftClosePrintStatusDto(j.Id, j.Status, j.IsReprint, j.AttemptCount, j.LastError, j.CreatedAtUtc))
        .ToListAsync(ct);
    return Results.Ok(new Falah.Application.Shifts.ShiftCloseStatementResponse(statement, recomputed, jobs,
        PrintJobQueryService.ComposeCloseStatementPaper(statement, recomputed)));
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

const string ControlCenterRefusal = "ليس لديك صلاحية عرض بيانات إغلاق يوم العمل.";

app.MapGet("/api/business-days", async (ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, ControlCenterRefusal, "control_center", "business_day_close", "business_day_reopen", "reports_view") ?? Results.Ok(await query.ListBusinessDaysAsync(ct)));

app.MapGet("/api/business-day/{id}", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, ControlCenterRefusal, "control_center", "business_day_close", "business_day_reopen", "reports_view") is { } refused) return refused;
    try { return Results.Ok(await query.GetBusinessDayAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// Closing the day, and what the closed day looked like, are the Control Center's - not a till's.

app.MapGet("/api/business-day/{id}/close-blockers", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, ControlCenterRefusal, "business_day_close", "control_center") is { } refused) return refused;
    try { return Results.Ok(await query.PreviewCloseBlockersAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/business-day/{id}/close", async (string id, Falah.Application.BusinessDay.CloseBusinessDayRequest body, CloseBusinessDayHandler handler, SalaryAccrualService salaries, CancellationToken ct) =>
{
    try
    {
        var request = body with { BusinessDayId = id };
        var result = await handler.HandleAsync(request, ct);
        // After the close has committed, never inside it: the day's share of every monthly salary
        // (the owner's daily-accrual rule). It cannot fail the close; a missed day is posted by the
        // next close or by the payroll payout's true-up.
        await salaries.TryAccrueThroughAsync(DateOnly.Parse(result.BusinessDate), "استحقاق الرواتب اليومي", ct);
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

// The frozen reconciliation of a closed day: every sale, balance and shift difference.
app.MapGet("/api/business-day/{id}/close-snapshot", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, ControlCenterRefusal, "reports_view", "control_center", "business_day_close") is { } refused) return refused;
    var snapshot = await query.GetLatestCloseSnapshotAsync(id, ct);
    return snapshot is null ? Results.NotFound() : Results.Ok(snapshot);
});

app.MapGet("/api/business-day/{id}/close-snapshots", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, ControlCenterRefusal, "reports_view", "control_center", "business_day_close")
        ?? Results.Ok(await query.ListCloseSnapshotsAsync(id, ct)));

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

app.MapGet("/api/business-day/{id}/checklist", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, ControlCenterRefusal, "business_day_close", "control_center")
        ?? Results.Ok(await query.GetChecklistAsync(id, ct)));

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

app.MapGet("/api/settings", async (SettingsQueryService query, HttpContext http, CancellationToken ct) => Results.Ok(SettingsForReader(await query.GetAsync(ct), http)));

app.MapPut("/api/settings", async (Falah.Application.Settings.UpdateSettingsRequest request, UpdateSettingsHandler handler, HttpContext http, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(SettingsForReader(result, http));
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/business-day/cash-count-denominations", () =>
    Results.Ok(Falah.Domain.BusinessDay.BusinessDayRules.EgyptianDenominationsPiastres));

// Counted cash per count - the same management information as the balances it is checked against.
app.MapGet("/api/business-day/{id}/cash-counts", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, ControlCenterRefusal, "cash_count", "control_center", "reports_view")
        ?? Results.Ok(await query.ListCashCountsAsync(id, ct)));

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

app.MapGet("/api/business-day/{id}/exceptions", async (string id, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, ControlCenterRefusal, "control_center", "reports_view") is { } refused) return refused;
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

// The business-day and user-management trail - including failed logins by machine and PIN
// collisions by account. A review role's, never a till's.
app.MapGet("/api/control-center/audit-events", async (int? limit, ControlCenterQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "ليس لديك صلاحية عرض سجل المراجعة.", "audit_view", "control_center", "users_manage")
        ?? Results.Ok(await query.ListAuditEventsAsync(limit ?? 100, ct: ct)));

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

// AUD-04: a mistaken expense is cancelled by a linked reversal while its shift is open - see ReverseExpenseHandler.
app.MapPost("/api/expenses/{id}/reverse", async (string id, Falah.Application.Expenses.ReverseExpenseRequest request, ReverseExpenseHandler handler, CancellationToken ct) =>
{
    if (request.ExpenseId != id) return Results.BadRequest(new { error = "رقم المصروف في الرابط لا يطابق الطلب." });
    try
    {
        return Results.Ok(await handler.HandleAsync(request, ct));
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

app.MapGet("/api/employees", async (EmployeeQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListAsync(ct)));

app.MapGet("/api/employees/{employeeId}", async (string employeeId, EmployeeQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireOwner(http) is { } refused) return refused;
    try { return Results.Ok(await query.GetAsync(employeeId, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/employees/{employeeId}/ledger", async (string employeeId, EmployeeQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListLedgerEntriesAsync(employeeId, ct)));

app.MapGet("/api/employees/{employeeId}/balance", async (string employeeId, EmployeeQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(new { employeeId, balancePiastres = await query.GetBalanceAsync(employeeId, ct) }));

// PERF-0006 (2026-11-22): every balance in one read for the employees screen; same gate as one balance.
app.MapGet("/api/employees/balances", async (EmployeeQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.GetAllBalancesAsync(ct)));

// Payroll/employee financial reporting by today/last-7/last-10/month/custom range - the reporting
// side of the final business rule, now that payment is not tied to a payroll period. Gated on
// reports_view like every other report.
app.MapGet("/api/employees/financial-report", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, EmployeeQueryService query, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await query.GetFinancialReportAsync(from, to, ct));
});

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

// The standalone "PAY EMPLOYEE" flow - final pre-go-live business rule: pays down the running
// ledger balance at any time, any amount up to that balance, no payroll run required. See
// PayEmployeeHandler's own doc comment for exactly how this differs from cash-out's "withdrawal"
// kind (drawer-sourced, shift-scoped) and from Payroll's own bulk pay (period-run-gated).
app.MapPost("/api/employees/{employeeId}/pay", async (string employeeId, Falah.Application.Employees.PayEmployeeRequest body, PayEmployeeHandler handler, CancellationToken ct) =>
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
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return result is null ? Results.NotFound(new { error = "الموظف غير موجود." }) : Results.Ok(result);
    }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
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

app.MapGet("/api/attendance", async (string? employeeId, DateOnly? date, EmployeeQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListAttendanceAsync(employeeId, date, ct)));

// ---- Payroll ----

app.MapPost("/api/payroll-runs", async (Falah.Application.Employees.CreatePayrollRequest request, CreatePayrollHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/payroll-runs", async (PayrollQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListRunsAsync(ct)));

app.MapGet("/api/payroll-runs/{runId}", async (string runId, PayrollQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireOwner(http) is { } refused) return refused;
    try { return Results.Ok(await query.GetRunAsync(runId, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/payroll-runs/{runId}/lines", async (string runId, PayrollQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListLinesAsync(runId, ct)));

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

app.MapGet("/api/backups", async (BackupQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListAsync(ct)));

app.MapGet("/api/backups/{id}", async (string id, BackupQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireOwner(http) is { } refused) return refused;
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

app.MapGet("/api/inventory/items", async (InventoryQueryService query, HttpContext http, CancellationToken ct) =>
{
    var items = await query.ListInventoryItemsAsync(ct);
    return Results.Ok(CanReadCosts(http) ? items : items.Select(i => i with { EstimatedUnitCostPiastres = null }));
});

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

app.MapGet("/api/inventory/movements", async (string? inventoryItemId, int? limit, DateTimeOffset? from, DateTimeOffset? to, bool? lossesOnly, InventoryQueryService query, HttpContext http, CancellationToken ct) =>
{
    var movements = await query.ListStockMovementsAsync(inventoryItemId, limit ?? 50, ct, from, to, lossesOnly ?? false);
    return Results.Ok(CanReadCosts(http) ? movements : movements.Select(m => m with { UnitCostPiastres = null, LossValuePiastres = null }));
});

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

app.MapPost("/api/customers/upsert", async (Falah.Application.Customers.UpsertCustomerRequest request, UpsertCustomerHandler handler, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية حفظ بيانات العملاء.", "pos", "delivery_manage") is { } refused) return refused;
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
app.MapGet("/api/customers/lookup", async (string phone, CustomerQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية قراءة بيانات العملاء.", "pos", "delivery_manage") is { } refused) return refused;
    var result = await query.FindByPhoneAsync(phone, ct);
    return result is null ? Results.NotFound() : Results.Ok(result);
});

app.MapGet("/api/customers/{id}", async (string id, CustomerQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية قراءة بيانات العملاء.", "pos", "delivery_manage") is { } refused) return refused;
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
app.MapGet("/api/delivery/board", async (DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") ?? Results.Ok(await query.ListDispatchBoardAsync(ct)));

app.MapGet("/api/delivery/ready", async (DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") ?? Results.Ok(await query.ListReadyForDispatchAsync(ct)));

app.MapGet("/api/delivery/drivers/{driverId}/active", async (string driverId, DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") ?? Results.Ok(await query.ListActiveForDriverAsync(driverId, ct)));

app.MapGet("/api/delivery/drivers/{driverId}/account", async (string driverId, DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية عرض عهدة السائق.", "reports_view", "delivery_manage") is { } refused) return refused;
    try { return Results.Ok(await query.GetDriverAccountAsync(driverId, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// Driver statistics by today/last-7/month/custom period - reuses the same delivery/custody/
// settlement rows GetDriverAccountAsync already reads, scoped to a date range.
app.MapGet("/api/delivery/drivers/{driverId}/statistics", async (string driverId, DateTimeOffset? from, DateTimeOffset? to, DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية عرض عهدة السائق.", "reports_view", "delivery_manage") is { } refused) return refused;
    try { return Results.Ok(await query.GetDriverStatisticsAsync(driverId, from, to, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// UI-REQ-001: one driver's statement for one business day and payment filter. The paper is composed
// from the same query when the print agent asks for it.
app.MapGet("/api/delivery/drivers/{driverId}/daily-statement", async (string driverId, DateOnly businessDate, string? paymentFilter, DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية عرض كشف السائق.", "reports_view", "delivery_manage") is { } refused) return refused;
    try { return Results.Ok(await query.GetDriverDailyStatementAsync(driverId, businessDate, paymentFilter, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (ArgumentException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
});

app.MapPost("/api/delivery/drivers/{driverId}/daily-statement/print", async (string driverId, Falah.Application.Delivery.PrintDriverDailyStatementRequest request, FalahDbContext db, DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية طباعة كشف السائق.", "reports_view", "delivery_manage") is { } refused) return refused;
    if (string.IsNullOrWhiteSpace(request.RequestId)) return Results.BadRequest(new { error = "رقم الطلب مطلوب." });
    // Replay first: the same press is the same paper.
    var existing = await db.PrintJobs.AsNoTracking().FirstOrDefaultAsync(j => j.CreateRequestId == request.RequestId, ct);
    if (existing is not null) return Results.Ok(new { printJobId = existing.Id, wasReplayed = true });
    try { await query.GetDriverDailyStatementAsync(driverId, request.BusinessDate, request.PaymentFilter, ct); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (ArgumentException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    var job = Falah.Infrastructure.Printing.DocumentPrintJobs.Build(Falah.Domain.Printing.PrintDocumentKind.DriverStatement,
        PrintJobQueryService.DriverStatementKey(driverId, request.BusinessDate, request.PaymentFilter, request.Detailed),
        Falah.Domain.Printing.PrintRules.DriverStatementLabel, Falah.Server.HttpContextActorExtensions.Actor(http).Name, DateTimeOffset.UtcNow,
        requestId: request.RequestId);
    db.PrintJobs.Add(job);
    await db.SaveChangesAsync(ct);
    return Results.Ok(new { printJobId = job.Id, wasReplayed = false });
});

app.MapGet("/api/delivery/{id}", async (string id, DeliveryQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") is { } refused) return refused;
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

// FIN-0051 (owner decision 2026-11-22): cancel a cash-on-delivery order before it leaves with a driver.
app.MapPost("/api/orders/cancel-undispatched-delivery", async (Falah.Application.Orders.CancelUndispatchedDeliveryRequest request, CancelUndispatchedDeliveryHandler handler, CancellationToken ct) =>
{
    try { return Results.Ok(await handler.HandleAsync(request, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
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

// This endpoint accepts a manager's PIN from any session, and a PIN is short enough to guess if
// guessing is free. Wrong PINs are limited per session, per machine and server-wide
// (ApprovalPinAttemptGuard; RateLimit:ApprovalPin:*). Refused guesses get a 429 with a message the
// till can show - the "login" limiter's bare 429 would not tell the cashier what happened.
app.MapPost("/api/approvals/issue", async (Falah.Application.Approvals.IssueManagerApprovalRequest request, IssueManagerApprovalHandler handler, HttpContext http, CancellationToken ct) =>
{
    try
    {
        var actor = Falah.Server.HttpContextActorExtensions.Actor(http);
        var caller = new ApprovalCaller(actor.Id, actor.Name, ClientSource.Of(http.Connection.RemoteIpAddress));
        var result = await handler.HandleAsync(request, caller, ct);
        return Results.Ok(result);
    }
    catch (ApprovalGuessingLimitException ex)
    {
        http.Response.Headers.RetryAfter = ((int)Math.Ceiling(ex.RetryAfter.TotalSeconds)).ToString();
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status429TooManyRequests);
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
app.MapGet("/api/orders", async (string actorUserId, FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") is { } refused) return refused;
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
    return Results.Ok(await OrderReadProjection.MapAsync(db, orders, ct));
});

// Pure read: fetching an order to (re)print its receipt must never be able to touch financial
// state. There is no write path anywhere in this handler.
app.MapGet("/api/orders/{id}", async (string id, FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") is { } refused) return refused;
    var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == id, ct);
    return order is null ? Results.NotFound() : Results.Ok((await OrderReadProjection.MapAsync(db, new[] { order }, ct)).Single());
});

// A cashier reads the order number off what they already see on screen/receipt - GUIDs are not
// something a human types back in.
app.MapGet("/api/orders/lookup", async (string orderNumber, FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "بيانات العملاء متاحة لمتلقي الطلبات فقط.", "pos", "delivery_manage") is { } refused) return refused;
    var order = await db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.OrderNumber == orderNumber, ct);
    // "128" / "0128" / "١٢٨" for #0128: matched on the order's unique sequence number.
    if (order is null && Falah.Domain.Orders.OrderNumberInput.TryParseSequence(orderNumber, out var sequence))
        order = await db.Orders.AsNoTracking().Include(o => o.Items)
            .Where(o => o.SequenceNumber == sequence).OrderByDescending(o => o.CreatedAtUtc).FirstOrDefaultAsync(ct);
    return order is null
        ? Results.NotFound(new { error = $"لا يوجد طلب بالرقم {orderNumber.Trim()}. تأكد من الرقم المطبوع على الإيصال." })
        : Results.Ok((await OrderReadProjection.MapAsync(db, new[] { order }, ct)).Single());
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

// HW1: the print station wired to the drawer takes the open-drawer commands to send now (print_agent).
app.MapPost("/api/cash-drawer/claim", async (Falah.Application.CashDrawer.ClaimCashDrawerKicksRequest request, CashDrawerKickQueue queue, IConfiguration configuration, HttpContext http, CancellationToken ct) =>
{
    if (RequirePrintAgent(http) is { } refused) return refused;
    if (string.IsNullOrWhiteSpace(request.AgentId)) return Results.UnprocessableEntity(new { error = "معرّف جهاز الطباعة مطلوب." });
    var maxAge = TimeSpan.FromSeconds(Math.Max(1, configuration.GetValue("CashDrawer:KickMaxAgeSeconds", 30)));
    return Results.Ok(await queue.ClaimAsync(request.AgentId.Trim(), Falah.Server.HttpContextActorExtensions.Actor(http).Name, maxAge, ct));
});

app.MapPost("/api/cash-drawer/report-result", async (Falah.Application.CashDrawer.ReportCashDrawerResultRequest request, ReportCashDrawerResultHandler handler, HttpContext http, CancellationToken ct) =>
{
    if (RequirePrintAgent(http) is { } refused) return refused;
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
// Gated like every other balance read (/balances, /position): treasury_manage or reports_view, the
// same rule the tills already apply to their own screens. Ungated, any till session could read the
// drawer - the expected cash a blind count must not know in advance - or the main treasury.
app.MapGet("/api/treasury/balance", async (string accountId, FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    if (RequireAnyPermission(http, "ليس لديك صلاحية عرض أرصدة الخزينة.", "treasury_manage", "reports_view") is { } refused) return refused;

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
        // Stored lower-case (OrderEnumMapping.ToWireString); not left to the collation's case rules.
        .Where(o => o.ElectronicPaymentStatus == "pending" && o.ElectronicPaymentAmountPiastres != null)
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
// businessDate is optional and additive - the Treasury screen itself keeps asking for "the most
// recent N movements, all accounts" exactly as before; the daily statement is the first caller to
// pass a Cairo business date, so it can show only that day's movements without a second endpoint.
app.MapGet("/api/treasury/movements", async (string actorUserId, string? accountId, int? limit, DateOnly? businessDate, FalahDbContext db, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;

    var query = db.TreasuryMovements.AsNoTracking().AsQueryable();
    if (!string.IsNullOrWhiteSpace(accountId))
        query = query.Where(m => m.SourceAccountId == accountId || m.DestinationAccountId == accountId);
    if (businessDate is { } day)
    {
        var (dayStart, dayEnd) = Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDate(day);
        query = query.Where(m => m.CreatedAtUtc >= dayStart && m.CreatedAtUtc < dayEnd);
    }

    var rows = await query.OrderByDescending(m => m.CreatedAtUtc).Take(limit is > 0 and <= 500 ? limit.Value : 100).ToListAsync(ct);
    var orderNumbers = await ResolveOrderNumbersAsync(db, rows.Select(m => (m.ReferenceType, m.ReferenceId)), ct);
    return Results.Ok(rows.Select(m => new Falah.Application.Treasury.TreasuryMovementDto(
        m.Id, m.Type, m.SourceAccountId, m.DestinationAccountId, m.AmountPiastres, m.ReferenceType,
        m.ReferenceId, m.ShiftId, m.Reason, m.CreatedBy, m.ApprovedBy, m.CreatedAtUtc,
        orderNumbers.GetValueOrDefault(m.ReferenceId))).ToList());
});

app.MapGet("/api/treasury/handovers", async (int limit, FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    // Each handover is a shift's counted cash - management information, like the balances.
    if (RequireAnyPermission(http, "ليس لديك صلاحية عرض أرصدة الخزينة.", "treasury_manage", "reports_view") is { } refused) return refused;
    var rows = await db.CashHandovers.AsNoTracking().OrderByDescending(h => h.CreatedAtUtc).Take(limit <= 0 ? 50 : limit).ToListAsync(ct);
    return Results.Ok(rows.Select(h => new
    {
        h.Id, h.FromAccountId, h.ToAccountId, h.AmountPiastres, h.HandedBy, h.ReceivedBy,
        h.ShiftId, h.Notes, h.TreasuryMovementId, h.CreatedAtUtc,
    }));
});

app.MapGet("/api/users", async (FalahDbContext db, HttpContext http, CancellationToken ct) =>
{
    var users = await db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
    var canManage = RequireAnyPermission(http, "", "users_manage") is null;
    return Results.Ok(users.Select(u => canManage ? ToUserSummary(u)
        : new Falah.Application.Users.UserSummaryDto(u.Id, u.Name, "", Array.Empty<string>())));
});

// PIN travels in the request body, never a query string - the lock screen's entire login contract
// is "this PIN alone" (no username), matching the reference app's unlockScreen exactly. The PIN is
// hashed, not stored/compared in plain text (see PinHasher's own doc comment for why a slow KDF
// matters for a low-entropy secret) - since a per-user random salt means no two hashes for the same
// PIN are ever equal, the lookup can no longer be a single indexed SQL equality check; it verifies
// against every active user's own hash instead (realistic staff counts make this trivially cheap).
// Rate-limited (see the "login" policy registered above) as a generic technical safeguard against
// PIN brute-forcing - not a restaurant policy, just standard API hygiene for an unauthenticated,
// low-entropy-secret endpoint. Wrong PINs are counted per machine and server-wide on top of that
// (LoginAttemptGuard), and every attempt is written down with the machine it came from.
app.MapPost("/api/auth/login-by-pin", async (Falah.Application.Users.LoginByPinRequest request, FalahDbContext db, Falah.Infrastructure.Authorization.SessionService sessions, Falah.Infrastructure.Authorization.LoginAttemptGuard guard, HttpContext http, CancellationToken ct) =>
{
    var source = ClientSource.Of(http.Connection.RemoteIpAddress);
    try
    {
        using var verification = await guard.EnterVerificationAsync(ct);
        using var attempt = await guard.BeginAsync(source, ct);
        var candidates = await db.Users.AsNoTracking().Where(u => u.Active).ToListAsync(ct);
        var user = candidates.FirstOrDefault(u => PinHasher.Verify(request.Pin ?? string.Empty, u.PinHash, u.PinSalt, u.PinIterations));
        if (user is null)
        {
            attempt.Failed(); // PIN verification completed: audit/storage errors must not erase a real wrong guess.
            // The guess itself is never written down - only the machine it came from.
            db.ControlAuditEvents.Add(new ControlAuditEventEntity
            {
                Id = $"audit-{Guid.NewGuid():N}", Action = Falah.Infrastructure.Authorization.LoginAttemptGuard.FailedAction, Actor = "unknown",
                EntityId = source, Reason = "رمز دخول خاطئ", CreatedAtUtc = DateTimeOffset.UtcNow,
            });
            await db.SaveChangesAsync(ct);
            return Results.Unauthorized();
        }
        attempt.Succeeded();
        db.ControlAuditEvents.Add(new ControlAuditEventEntity
        {
            Id = $"audit-{Guid.NewGuid():N}", Action = Falah.Infrastructure.Authorization.LoginAttemptGuard.SucceededAction, Actor = user.Name,
            EntityId = source, Reason = user.Id, CreatedAtUtc = DateTimeOffset.UtcNow,
        });
        var token = await sessions.IssueAsync(user.Id, ct); // saves the audit row with the session
        var summary = ToUserSummary(user);
        return Results.Ok(new Falah.Application.Users.LoginResultDto(
            summary.Id, summary.Name, summary.Role, summary.Permissions, token));
    }
    catch (Falah.Infrastructure.Authorization.LoginThrottledException ex)
    {
        http.Response.Headers.RetryAfter = ((int)Math.Ceiling(ex.RetryAfter.TotalSeconds)).ToString();
        return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status429TooManyRequests);
    }
}).RequireRateLimiting("login");

// What a user administrator should know about who can log in (users_manage, from the session).
app.MapGet("/api/security/posture", async (HttpContext http, FalahDbContext db, Falah.Infrastructure.Authorization.LoginAttemptGuard guard, CancellationToken ct) =>
{
    var gate = await RequireUsersManageAsync(db, Falah.Server.HttpContextActorExtensions.ActorId(http), ct);
    if (gate is not null) return gate;

    var now = DateTimeOffset.UtcNow;
    var dayAgo = now.AddHours(-24);
    var wrongLogins = await db.ControlAuditEvents.CountAsync(e => e.Action == Falah.Infrastructure.Authorization.LoginAttemptGuard.FailedAction && e.CreatedAtUtc > dayAgo, ct);
    var wrongApprovals = await db.ControlAuditEvents.CountAsync(e => e.Action == ApprovalPinAttemptGuard.FailedAuditAction && e.CreatedAtUtc > dayAgo, ct);
    var monthAgo = now.AddDays(-30);
    var collisions = await db.ControlAuditEvents.AsNoTracking()
        .Where(e => e.Action == PinUniquenessChecker.CollisionAuditAction && e.CreatedAtUtc > monthAgo)
        .OrderByDescending(e => e.CreatedAtUtc).Take(50).ToListAsync(ct);
    var accountIds = collisions.Select(c => c.EntityId).Distinct().ToList();
    var names = await db.Users.AsNoTracking().Where(u => accountIds.Contains(u.Id)).ToDictionaryAsync(u => u.Id, u => u.Name, ct);

    return Results.Ok(new Falah.Application.Users.SecurityPostureDto(
        await DefaultCredentials.ActiveUsersWithDemoPinsAsync(db, ct),
        await guard.ListAsync(ct),
        wrongLogins,
        wrongApprovals,
        collisions.Select(c => new Falah.Application.Users.PinCollisionDto(c.EntityId, names.GetValueOrDefault(c.EntityId, c.EntityId), c.Actor, c.CreatedAtUtc)).ToList()));
});

// The machines whose wrong PINs are being counted, and letting one log in again - for a till stopped
// by somebody mistyping (or guessing) at it. users_manage, read from the session.
app.MapGet("/api/security/login-sources", async (HttpContext http, FalahDbContext db, Falah.Infrastructure.Authorization.LoginAttemptGuard guard, CancellationToken ct) =>
    await RequireUsersManageAsync(db, Falah.Server.HttpContextActorExtensions.ActorId(http), ct) ?? Results.Ok(await guard.ListAsync(ct)));

app.MapPost("/api/security/login-sources/clear", async (Falah.Application.Users.ClearLoginSourceRequest request, HttpContext http, FalahDbContext db, Falah.Infrastructure.Authorization.LoginAttemptGuard guard, CancellationToken ct) =>
{
    var actor = Falah.Server.HttpContextActorExtensions.Actor(http);
    var gate = await RequireUsersManageAsync(db, actor.Id, ct);
    if (gate is not null) return gate;
    if (string.IsNullOrWhiteSpace(request.Source)) return Results.UnprocessableEntity(new { error = "حدد الجهاز." });

    var source = request.Source.Trim();
    db.ControlAuditEvents.Add(new ControlAuditEventEntity
    {
        Id = $"audit-{Guid.NewGuid():N}", Action = Falah.Infrastructure.Authorization.LoginAttemptGuard.ClearedAction, Actor = actor.Name,
        EntityId = source, Reason = "إعادة السماح بتسجيل الدخول من هذا الجهاز", CreatedAtUtc = DateTimeOffset.UtcNow,
    });
    await db.SaveChangesAsync(ct);
    await guard.ClearAsync(source, ct);
    return Results.Ok();
});

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

app.MapPost("/api/users", async (Falah.Application.Users.CreateUserRequest request, CreateUserHandler handler, HttpContext http, CancellationToken ct) =>
{
    try
    {
        var result = await handler.HandleAsync(request, ct);
        return Results.Ok(result);
    }
    catch (PinChoiceLimitException ex)
    {
        return PinChoiceRefused(http, ex);
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

app.MapPost("/api/users/{id}/change-pin", async (string id, Falah.Application.Users.ChangePinRequest body, ChangePinHandler handler, HttpContext http, CancellationToken ct) =>
{
    try
    {
        var request = body with { UserId = id };
        await handler.HandleAsync(request, ct);
        return Results.Ok();
    }
    catch (PinChoiceLimitException ex)
    {
        return PinChoiceRefused(http, ex);
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

// "كشف اليوم" - the owner's single "what happened today" statement, one Cairo business date at a
// time (unlike owner-dashboard above, this never widens to a range - a daily statement is a daily
// statement). Composed entirely from DailyStatementQueryService, which itself composes the same
// queries every other report/screen already reads - see that class's own doc comment. Gated on
// reports_view like every other report.
app.MapGet("/api/reports/daily-statement", async (string actorUserId, DateOnly? businessDate, FalahDbContext db, DailyStatementQueryService statement, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await statement.GetAsync(businessDate ?? Falah.Domain.Common.BusinessClock.Today(), ct));
});

// Per-account cash flow for a span of Cairo calendar dates (one date by default): opening, ins and
// outs by movement type, closing. Gated on reports_view like every other report.
app.MapGet("/api/reports/cash-flow", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    var range = Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate)
        ?? Falah.Domain.Common.BusinessDateRange.SingleDay(Falah.Domain.Common.BusinessClock.Today());
    return Results.Ok(await reports.GetCashFlowAsync(range, ct));
});

// Orders voided in a span of Cairo calendar dates (one date by default), with what became of their food.
app.MapGet("/api/reports/voids", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    var range = Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate)
        ?? Falah.Domain.Common.BusinessDateRange.SingleDay(Falah.Domain.Common.BusinessClock.Today());
    return Results.Ok(await reports.GetVoidsAsync(range, ct));
});

// Every unconfirmed InstaPay / wallet payment now, with its amount and age - current state, not a period.
app.MapGet("/api/reports/pending-electronic", async (string actorUserId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await reports.GetPendingElectronicAsync(ct));
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

app.MapGet("/api/reports/daily-summary/{id}", async (string id, string actorUserId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    try { return Results.Ok(await reports.GetDailySummaryAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/reports/top-items", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetTopSellingItemsAsync(from, to, ct)));

app.MapGet("/api/reports/category-sales", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetCategorySalesAsync(from, to, ct)));

app.MapGet("/api/reports/product-hourly", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetProductHourlySalesAsync(from, to, ct)));

app.MapGet("/api/reports/food-cost", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetFoodCostReportAsync(from, to, ct)));

app.MapGet("/api/reports/significant-actions", async (string actorUserId, DateOnly? from, DateOnly? to, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetSignificantActionsAsync(from, to, ct)));

// The Shifts screen's history table - gated the same as the single-shift report below (reports_view),
// since every row carries the same sales/cash figures that report does. A cashier without reports_view
// reaches this same screen (it also opens/closes their own shift, which needs no report at all) but the
// client simply does not call this endpoint for them - see ShiftsViewModel.
app.MapGet("/api/reports/shifts", async (string actorUserId, FalahDbContext db, ReportingQueryService reports, CancellationToken ct) =>
    await RequireReportsAccessAsync(db, actorUserId, ct) ?? Results.Ok(await reports.GetShiftsHistoryAsync(ct: ct)));

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

// Reprints a slip that belongs to no order (shift opening/handover, expense voucher). Creates a
// print job and an audit entry; it never re-posts the document it reproduces.
app.MapPost("/api/print-jobs/reprint-document", async (
    Falah.Application.Printing.ReprintDocumentRequest body,
    Falah.Infrastructure.Printing.ReprintDocumentHandler handler, CancellationToken ct) =>
{
    try
    {
        return Results.Ok(await handler.HandleAsync(body, ct));
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

app.MapPost("/api/print-jobs/{id}/report-result", async (string id, Falah.Application.Printing.ReportPrintJobResultRequest body, ReportPrintJobResultHandler handler, HttpContext http, CancellationToken ct) =>
{
    if (RequirePrintAgent(http) is { } refused) return refused;
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

// Pending is what a station's agent asks for. This is what a PERSON needs: every route's queue,
// what failed, and what has been sitting long enough to mean the agent is not draining it.
app.MapGet("/api/print-jobs/overview", async (PrintJobQueryService query, CancellationToken ct) =>
    Results.Ok(await query.GetOverviewAsync(ct)));

app.MapGet("/api/print-jobs/pending", async (string route, PrintJobQueryService query, HttpContext http, CancellationToken ct) =>
    RequireAnyPermission(http, "ليس لديك صلاحية قراءة طابور الطباعة.", "print_agent", "settings_manage") ?? Results.Ok(await query.ListPendingJobsForRouteAsync(route, ct)));

// What an agent polls now: the pending jobs it will print, claimed for a lease so a second agent on
// the same route (another machine) is never handed them. /pending stays for agents built before this.
app.MapPost("/api/print-jobs/claim", async (Falah.Application.Printing.ClaimPrintJobsRequest request, PrintJobQueryService query, IConfiguration configuration, HttpContext http, CancellationToken ct) =>
{
    if (RequirePrintAgent(http) is { } refused) return refused;
    if (string.IsNullOrWhiteSpace(request.Route) || string.IsNullOrWhiteSpace(request.AgentId))
        return Results.UnprocessableEntity(new { error = "المسار ومعرّف وكيل الطباعة مطلوبان." });
    if (request.AgentId.Trim().Length > 160 || request.AgentId.Contains('|'))
        return Results.UnprocessableEntity(new { error = "معرّف وكيل الطباعة غير صالح أو أطول من 160 حرفًا." });
    var lease = TimeSpan.FromSeconds(configuration.GetValue("PrintJobs:ClaimLeaseSeconds", 120));
    return Results.Ok(await query.ClaimPendingJobsForRouteAsync(request.Route, request.AgentId.Trim(), lease, ct));
});

app.MapPost("/api/print-jobs/{id}/start", async (string id, Falah.Application.Printing.StartPrintJobRequest request, PrintJobQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequirePrintAgent(http) is { } refused) return refused;
    if (string.IsNullOrWhiteSpace(request.ClaimId)) return Results.UnprocessableEntity(new { error = "حجز الطباعة مطلوب." });
    return Results.Ok(new Falah.Application.Printing.StartPrintJobResult(await query.StartSendingAsync(id, request.ClaimId, ct)));
});

app.MapGet("/api/print-jobs/{id}", async (string id, PrintJobQueryService query, CancellationToken ct) =>
{
    try { return Results.Ok(await query.GetJobDetailsAsync(id, ct)); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/print-jobs/{id}/ticket", async (string id, PrintJobQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequirePrintAgent(http) is { } refused) return refused;
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

app.MapGet("/api/suppliers", async (PurchasingQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListSuppliersAsync(ct)));

app.MapGet("/api/suppliers/{id}", async (string id, PurchasingQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireOwner(http) is { } refused) return refused;
    var supplier = await query.GetSupplierAsync(id, ct);
    return supplier is null ? Results.NotFound() : Results.Ok(supplier);
});

app.MapGet("/api/suppliers/{id}/ledger", async (string id, PurchasingQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.GetSupplierLedgerAsync(id, ct)));

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

app.MapGet("/api/purchases", async (PurchasingQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.ListPurchasesAsync(ct)));

// Registered before /api/purchases/{id} so the literal segment is not swallowed by the id route.
app.MapGet("/api/purchases/payables-aging", async (PurchasingQueryService query, HttpContext http, CancellationToken ct) =>
    RequireOwner(http) ?? Results.Ok(await query.GetPayablesAgingAsync(ct)));

app.MapGet("/api/purchases/{id}", async (string id, PurchasingQueryService query, HttpContext http, CancellationToken ct) =>
{
    if (RequireOwner(http) is { } refused) return refused;
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

// ---- Financial corrections, shortfall classification, handover receipt, owner financial report ----
// Every one reads the actor from the session (the body's actorUserId is already checked against it by the
// middleware, and replaced here so nothing downstream can trust the body).

app.MapPost("/api/treasury/movements/{id}/correct", async (string id, Falah.Application.Treasury.CorrectTreasuryMovementRequest request, HttpContext http, CorrectTreasuryMovementHandler handler, CancellationToken ct) =>
{
    if (request.MovementId != id) return Results.BadRequest(new { error = "رقم الحركة في الرابط لا يطابق الطلب." });
    try { return Results.Ok(await handler.HandleAsync(request with { ActorUserId = Falah.Server.HttpContextActorExtensions.ActorId(http) }, ct)); }
    catch (Falah.Application.Treasury.TreasuryConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapPost("/api/purchases/{id}/credits", async (string id, Falah.Application.Purchasing.RecordSupplierCreditRequest request, HttpContext http, RecordSupplierCreditHandler handler, CancellationToken ct) =>
{
    if (request.PurchaseId != id) return Results.BadRequest(new { error = "رقم الفاتورة في الرابط لا يطابق الطلب." });
    try { return Results.Ok(await handler.HandleAsync(request with { ActorUserId = Falah.Server.HttpContextActorExtensions.ActorId(http) }, ct)); }
    catch (Falah.Application.Purchasing.PurchasingConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/delivery/collection-shortfalls", async (string actorUserId, FalahDbContext db, ClassifyCollectionShortfallHandler handler, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await handler.ListAsync(ct));
});

app.MapPost("/api/delivery/{deliveryId}/collection-shortfall/classify", async (string deliveryId, Falah.Application.Delivery.ClassifyCollectionShortfallRequest request, HttpContext http, ClassifyCollectionShortfallHandler handler, CancellationToken ct) =>
{
    if (request.DeliveryId != deliveryId) return Results.BadRequest(new { error = "رقم التوصيل في الرابط لا يطابق الطلب." });
    try { return Results.Ok(await handler.HandleAsync(request with { ActorUserId = Falah.Server.HttpContextActorExtensions.ActorId(http) }, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

app.MapGet("/api/shifts/handovers/pending", async (string actorUserId, FalahDbContext db, AcknowledgeShiftHandoverHandler handler, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    return Results.Ok(await handler.ListUnconfirmedAsync(ct));
});

app.MapPost("/api/shifts/{id}/handover/acknowledge", async (string id, Falah.Application.Shifts.AcknowledgeShiftHandoverRequest request, HttpContext http, AcknowledgeShiftHandoverHandler handler, CancellationToken ct) =>
{
    if (request.ShiftId != id) return Results.BadRequest(new { error = "رقم الوردية في الرابط لا يطابق الطلب." });
    try { return Results.Ok(await handler.HandleAsync(request with { ActorUserId = Falah.Server.HttpContextActorExtensions.ActorId(http) }, ct)); }
    catch (Falah.Domain.Common.DomainException ex) { return Results.UnprocessableEntity(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
});

// The owner's daily financial report: every KPI with its definition, basis and source records; missing data is
// reported as unavailable, never as zero. Calendar days (Cairo), one date by default.
app.MapGet("/api/reports/owner-financial", async (string actorUserId, DateOnly? businessDate, DateOnly? from, DateOnly? to, FalahDbContext db, OwnerFinancialReportQueryService report, CancellationToken ct) =>
{
    var denied = await RequireReportsAccessAsync(db, actorUserId, ct);
    if (denied is not null) return denied;
    var range = Falah.Domain.Common.BusinessDateRange.Resolve(from, to, businessDate)
        ?? Falah.Domain.Common.BusinessDateRange.SingleDay(Falah.Domain.Common.BusinessClock.Today());
    return Results.Ok(await report.GetAsync(range, ct));
});

// ---- Report export (Excel / PDF / Word / PNG) ----
// One snapshot per request, built from the same report services as the screens; every format is rendered from it.
// Authorisation is the screen's own rule for that report (ReportExportCatalog), checked here against the session's
// user - an export never shows more than the screen it comes from. The file name is generated, never taken from input.

app.MapGet("/api/reports/export/catalog", (HttpContext http) =>
    Results.Ok(Falah.Application.Reports.Export.ReportExportCatalog.Reports
        .Where(r => CanExportReport(http, r.Key))
        .Select(r => new Falah.Application.Reports.Export.ReportExportCatalogEntry(r.Key, r.Title, r.Description, Falah.Application.Reports.Export.ReportExportCatalog.AllFormats))
        .ToList()));

app.MapGet("/api/reports/export/snapshot", async (string report, DateOnly from, DateOnly to, HttpContext http,
    Falah.Infrastructure.Reports.Export.ReportExportService exports, CancellationToken ct) =>
{
    if (Falah.Infrastructure.Reports.Export.ReportExportService.Validate(report, null, from, to) is { } invalid) return Results.BadRequest(new { error = invalid });
    if (!CanExportReport(http, report)) return Results.UnprocessableEntity(new { error = "ليس لديك صلاحية هذا التقرير." });
    return Results.Ok(await exports.SnapshotAsync(report, from, to, Falah.Server.HttpContextActorExtensions.Actor(http).Name, ct));
});

app.MapGet("/api/reports/export", async (string report, string format, DateOnly from, DateOnly to, HttpContext http,
    Falah.Infrastructure.Reports.Export.ReportExportService exports, CancellationToken ct) =>
{
    if (Falah.Infrastructure.Reports.Export.ReportExportService.Validate(report, format, from, to) is { } invalid) return Results.BadRequest(new { error = invalid });
    if (!CanExportReport(http, report)) return Results.UnprocessableEntity(new { error = "ليس لديك صلاحية هذا التقرير." });
    var actor = Falah.Server.HttpContextActorExtensions.Actor(http);
    var file = await exports.ExportAsync(report, format, from, to, (actor.Id, actor.Name, actor.Role), ct);
    http.Response.Headers["X-Falah-Snapshot"] = file.Fingerprint;
    http.Response.Headers.CacheControl = "no-store";
    return Results.File(file.Content, file.ContentType, file.FileName);
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

// Claiming print jobs and reporting what happened to them (or to a cash-drawer kick) is the print
// agent's work: a till session that did it could take a route's tickets so they never print, or mark
// an unprinted ticket printed. Only print_agent (or "all", which existing agents logged in as a
// manager hold) may. Read from the session, like every other identity check.
static IResult? RequireOwner(HttpContext http) => RequireAnyPermission(http, "هذه البيانات متاحة للمالك فقط.");

// The same permission rule as the report's own screen (ReportExportCatalog.Reports.AnyOf; "all" always qualifies).
static bool CanExportReport(HttpContext http, string reportKey)
{
    var entry = Falah.Application.Reports.Export.ReportExportCatalog.Reports.FirstOrDefault(r => r.Key == reportKey);
    if (entry.Key is null) return false;
    var permissions = Falah.Server.HttpContextActorExtensions.Actor(http).Permissions
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return permissions.Contains("all") || entry.AnyOf.Any(permissions.Contains);
}
static bool CanReadCosts(HttpContext http) => RequireOwner(http) is null;
static Falah.Application.Settings.SettingsDto SettingsForReader(Falah.Application.Settings.SettingsDto settings, HttpContext http) =>
    RequireAnyPermission(http, "", "settings_manage") is null ? settings : settings with { CashDiscrepancyThresholdPiastres = 0 };

static IResult? RequirePrintAgent(HttpContext http) =>
    RequireAnyPermission(http, "هذه العملية لوكيل الطباعة فقط.", "print_agent");

// The session's user must hold one of these permissions (or "all"). For reads that carry money or
// security information and name no actor in the request: the session is the only identity there is.
static IResult? RequireAnyPermission(HttpContext http, string refusal, params string[] anyOf)
{
    var permissions = Falah.Server.HttpContextActorExtensions.Actor(http).Permissions
        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    return permissions.Contains("all") || anyOf.Any(permissions.Contains)
        ? null
        : Results.UnprocessableEntity(new { error = refusal });
}

// 429 with a message the screen can show and when to try again (PinChoiceGuard).
static IResult PinChoiceRefused(HttpContext http, PinChoiceLimitException ex)
{
    http.Response.Headers.RetryAfter = ((int)Math.Ceiling(ex.RetryAfter.TotalSeconds)).ToString();
    return Results.Json(new { error = ex.Message }, statusCode: StatusCodes.Status429TooManyRequests);
}

// Ported from the reference app's own `hasPermission('reports_view')` gate on CashScreen/
// CostingScreen/ShiftsScreen/InventoryScreen/DeliveryTablesScreen - every screen that shows
// aggregate figures (reports, shift totals, cost/profit numbers, driver accounts) requires this
// ONE permission, checked here server-side rather than only hiding the number client-side. Gated
// by permission (or "all"), never by role name, matching every other authorization check in this
// codebase.
/// <summary>Order id -> order NUMBER, for the treasury ledger's own reference column. One batched
/// lookup for a whole page of movements, never one query per row, and only for the references that
/// actually point at an order - everything else keeps its raw id, which is still the honest answer
/// for a reference that has no human-facing name.</summary>
static async Task<Dictionary<string, string>> ResolveOrderNumbersAsync(
    FalahDbContext db, IEnumerable<(string ReferenceType, string ReferenceId)> references, CancellationToken ct)
{
    var orderIds = references
        .Where(r => r.ReferenceType is "order" or "order_refund" && !string.IsNullOrEmpty(r.ReferenceId))
        .Select(r => r.ReferenceId).Distinct().ToList();
    if (orderIds.Count == 0) return new Dictionary<string, string>();
    return await db.Orders.AsNoTracking()
        .Where(o => orderIds.Contains(o.Id))
        .ToDictionaryAsync(o => o.Id, o => o.OrderNumber, ct);
}

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
