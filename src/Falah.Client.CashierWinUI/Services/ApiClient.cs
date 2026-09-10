using System.Net.Http;
using System.Net.Http.Json;
using Falah.Application.Approvals;
using Falah.Application.CashDrawer;
using Falah.Application.Delivery;
using Falah.Application.Expenses;
using Falah.Application.Menu;
using Falah.Application.Orders;
using Falah.Application.Printing;
using Falah.Application.Shifts;
using Falah.Application.Treasury;
using Falah.Application.Users;

namespace Falah.Client.CashierWinUI.Services;

/// <summary>
/// The ONLY way this WinUI client ever talks to the restaurant's data - every call goes to
/// Falah.Server over HTTP. There is no SQL Server connection string anywhere in this project, and
/// there must never be one: the server remains the sole financial authority (prices, totals,
/// idempotency, OrderNumber, inventory) even when this client happens to run on the same physical
/// machine as the Host.
/// </summary>
public sealed partial class ApiClient
{
    private readonly HttpClient _http;
    public ApiClient(HttpClient http) => _http = http;

    public ApiClient(string baseAddress = "http://localhost:5151")
    {
        _http = new HttpClient { BaseAddress = new Uri(baseAddress), Timeout = TimeSpan.FromSeconds(10) };
    }

    /// <summary>Where product photos are fetched from. Images are the one thing this client loads
    /// by URL rather than through a typed call, because an Image control does its own download -
    /// so it needs the address, not the bytes.</summary>
    public Uri? ProductImageUri(string productId, string? version) =>
        string.IsNullOrEmpty(version) || _http.BaseAddress is null
            ? null
            // The version rides in the query string purely so a replaced photo is a different URL;
            // the server ignores it and serves whatever the current image is.
            : new Uri(_http.BaseAddress, $"/api/products/{Uri.EscapeDataString(productId)}/image?v={Uri.EscapeDataString(version)}");

    public async Task<bool> PingHealthAsync(CancellationToken ct = default)
    {
        try
        {
            using var response = await _http.GetAsync("/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public sealed record ApiResult<T>(bool Success, T? Value, string? Error, bool OutcomeUnknown = false, int StatusCode = 0);

    private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
    {
        if (response.IsSuccessStatusCode)
        {
            var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
            return value is null ? new(false, default, "لم يصل تأكيد صالح للعملية. أعد المحاولة للتحقق.", true)
                : new ApiResult<T>(true, value, null, false, (int)response.StatusCode);
        }

        string? message = null;
        try
        {
            var body = await response.Content.ReadFromJsonAsync<ErrorBody>(cancellationToken: ct);
            message = body?.error;
        }
        catch
        {
            // Body wasn't the expected {error} shape - fall through to the generic message below.
        }
        message ??= response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "رمز الدخول غير صحيح.",
            System.Net.HttpStatusCode.NotFound => "العنصر المطلوب غير موجود.",
            _ => $"حدث خطأ غير متوقع من السيرفر ({(int)response.StatusCode}).",
        };
        return new ApiResult<T>(false, default, message, (int)response.StatusCode >= 500, (int)response.StatusCode);
    }

    private static async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            return await ReadResultAsync<T>(response, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or System.IO.IOException)
        {
            return new(false, default, "تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية.", true);
        }
    }

    private static async Task<ApiResult<bool>> SendEmptyAsync(Func<Task<HttpResponseMessage>> send, CancellationToken ct)
    {
        try
        {
            using var response = await send();
            return response.IsSuccessStatusCode ? new(true, true, null, StatusCode: (int)response.StatusCode)
                : await ReadResultAsync<bool>(response, ct);
        }
        catch (Exception ex) when (ex is HttpRequestException or OperationCanceledException or System.Text.Json.JsonException or System.IO.IOException)
        {
            return new(false, false, "تعذر التحقق من نتيجة الاتصال. أعد المحاولة بنفس البيانات للتحقق من العملية.", true);
        }
    }

    public Task<ApiResult<CurrentShiftDto>> GetCurrentShiftAsync(CancellationToken ct = default) =>
        SendAsync<CurrentShiftDto>(() => _http.GetAsync("/api/shifts/current", ct), ct);
    public Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>> GetTaxRulesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Pricing.TaxRuleDto>>(() => _http.GetAsync("/api/tax-rules", ct), ct);
    public Task<ApiResult<Falah.Application.Settings.SettingsDto>> GetSettingsAsync(CancellationToken ct = default) =>
        SendAsync<Falah.Application.Settings.SettingsDto>(() => _http.GetAsync("/api/settings", ct), ct);
    public Task<ApiResult<Falah.Application.Customers.CustomerDto>> LookupCustomerAsync(string phone, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.GetAsync($"/api/customers/lookup?phone={Uri.EscapeDataString(phone)}", ct), ct);
    public Task<ApiResult<Falah.Application.Customers.CustomerDto>> SaveCustomerAsync(Falah.Application.Customers.UpsertCustomerRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Customers.CustomerDto>(() => _http.PostAsJsonAsync("/api/customers/upsert", request, ct), ct);

    private sealed record ErrorBody(string? error);

    /// <summary>The session token this client presents on every later request. Set once at login
    /// and cleared at logout; without it the server refuses everything, which is the point - the
    /// acting user is now something the server establishes rather than something we assert.</summary>
    private string? _sessionToken;

    public async Task<ApiResult<UserSummaryDto>> LoginByPinAsync(string pin, CancellationToken ct = default)
    {
        var result = await SendAsync<LoginResultDto>(
            () => _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct), ct);
        if (!result.Success || result.Value is null)
            return new ApiResult<UserSummaryDto>(false, null, result.Error, result.OutcomeUnknown, result.StatusCode);

        _sessionToken = result.Value.Token;
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sessionToken);

        return new ApiResult<UserSummaryDto>(true,
            new UserSummaryDto(result.Value.Id, result.Value.Name, result.Value.Role, result.Value.Permissions),
            null, false, result.StatusCode);
    }

    /// <summary>Ends the session server-side. Locking the screen without this would leave a working
    /// token behind, which is exactly the state a shared till should not be left in.</summary>
    public async Task LogoutAsync(CancellationToken ct = default)
    {
        if (_sessionToken is null) return;
        try { await _http.PostAsync("/api/auth/logout", content: null, ct); }
        catch { /* the local session ends either way; a stranded token expires on its own. */ }
        _sessionToken = null;
        _http.DefaultRequestHeaders.Authorization = null;
    }

    public async Task<ApiResult<MenuResponse>> GetMenuAsync(CancellationToken ct = default)
    {
        return await SendAsync<MenuResponse>(() => _http.GetAsync("/api/menu", ct), ct);
    }

    public async Task<ApiResult<OpenShiftResult>> OpenShiftAsync(OpenShiftRequest request, CancellationToken ct = default)
    {
        return await SendAsync<OpenShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/open", request, ct), ct);
    }

    public async Task<ApiResult<PrepareShiftCloseResult>> PrepareShiftCloseAsync(PrepareShiftCloseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<PrepareShiftCloseResult>(() => _http.PostAsJsonAsync("/api/shifts/prepare-close", request, ct), ct);
    }

    public async Task<ApiResult<CloseShiftResult>> CloseShiftAsync(CloseShiftRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CloseShiftResult>(() => _http.PostAsJsonAsync("/api/shifts/close", request, ct), ct);
    }

    public async Task<ApiResult<CheckoutOrderResult>> CheckoutAsync(CheckoutOrderRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CheckoutOrderResult>(() => _http.PostAsJsonAsync("/api/orders/checkout", request, ct), ct);
    }

    public async Task<ApiResult<RefundOrderResult>> RefundAsync(RefundOrderRequest request, CancellationToken ct = default)
    {
        return await SendAsync<RefundOrderResult>(() => _http.PostAsJsonAsync("/api/orders/refund", request, ct), ct);
    }

    public async Task<ApiResult<ConfirmElectronicPaymentResult>> ConfirmElectronicPaymentAsync(ConfirmElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<ConfirmElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/confirm-electronic-payment", request, ct), ct);
    }

    public async Task<ApiResult<CancelPendingElectronicPaymentResult>> CancelPendingElectronicPaymentAsync(CancelPendingElectronicPaymentRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CancelPendingElectronicPaymentResult>(() => _http.PostAsJsonAsync("/api/orders/cancel-pending-electronic-payment", request, ct), ct);
    }

    public async Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>> GetActiveDeliveryServicesAsync(CancellationToken ct = default)
    {
        return await SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services", ct), ct);
    }

    /// <summary>Scope is decided server-side: without orders_view_all the caller gets only the
    /// currently-open shift's orders.</summary>
    public Task<ApiResult<IReadOnlyList<OrderDetailsDto>>> GetOrdersAsync(string actorUserId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<OrderDetailsDto>>(() => _http.GetAsync(
            $"/api/orders?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    // ---------------- Menu / tax / pricing ----------------

    public Task<ApiResult<IReadOnlyList<Falah.Application.Pricing.PriceRuleDto>>> GetPriceRulesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Pricing.PriceRuleDto>>(() => _http.GetAsync("/api/price-rules", ct), ct);

    public Task<ApiResult<bool>> SetProductAvailabilityAsync(Falah.Application.Menu.SetProductAvailabilityRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/products/{Uri.EscapeDataString(request.ProductId)}/availability", request, ct), ct);

    // ---------------- Settings / backup / control centre / printing ----------------

    public Task<ApiResult<Falah.Application.Settings.SettingsDto>> UpdateSettingsAsync(Falah.Application.Settings.UpdateSettingsRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Settings.SettingsDto>(() => _http.PutAsJsonAsync("/api/settings", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Backup.BackupRecordDto>>> GetBackupsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Backup.BackupRecordDto>>(() => _http.GetAsync("/api/backups", ct), ct);

    public Task<ApiResult<Falah.Application.Backup.BackupRecordDto>> CreateBackupAsync(Falah.Application.Backup.CreateBackupRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Backup.BackupRecordDto>(() => _http.PostAsJsonAsync("/api/backups", request, ct), ct);

    public Task<ApiResult<Falah.Application.Backup.BackupRecordDto>> VerifyBackupAsync(Falah.Application.Backup.VerifyBackupRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Backup.BackupRecordDto>(() => _http.PostAsJsonAsync(
            $"/api/backups/{Uri.EscapeDataString(request.BackupRecordId)}/verify", request, ct), ct);

    /// <summary>The day currently in play, or a 404 that the caller must treat as the legitimate
    /// "no day open" state rather than an error.</summary>
    public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>> GetCurrentBusinessDayAsync(CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.BusinessDayDto>(() => _http.GetAsync("/api/business-day/current", ct), ct);

    public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>> GetBusinessDayAsync(string id, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.BusinessDayDto>(() => _http.GetAsync($"/api/business-day/{Uri.EscapeDataString(id)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.CloseBlockerDto>>> GetCloseBlockersAsync(string id, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.BusinessDay.CloseBlockerDto>>(() => _http.GetAsync(
            $"/api/business-day/{Uri.EscapeDataString(id)}/close-blockers", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.ChecklistItemStatusDto>>> GetChecklistAsync(string id, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.BusinessDay.ChecklistItemStatusDto>>(() => _http.GetAsync(
            $"/api/business-day/{Uri.EscapeDataString(id)}/checklist", ct), ct);

    public Task<ApiResult<bool>> CompleteChecklistItemAsync(Falah.Application.BusinessDay.CompleteChecklistItemRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/checklist/{Uri.EscapeDataString(request.ItemId)}", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.ControlExceptionDto>>> GetExceptionsAsync(string id, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.BusinessDay.ControlExceptionDto>>(() => _http.GetAsync(
            $"/api/business-day/{Uri.EscapeDataString(id)}/exceptions", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.BusinessDay.BusinessDayDto>>> GetBusinessDaysAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.BusinessDay.BusinessDayDto>>(() => _http.GetAsync("/api/business-days", ct), ct);

    public Task<ApiResult<Falah.Application.BusinessDay.OpenBusinessDayResult>> OpenBusinessDayAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.OpenBusinessDayResult>(() => _http.PostAsJsonAsync("/api/business-day/open", request, ct), ct);

    public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>> CloseBusinessDayAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.BusinessDayCloseSnapshotDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/close", request, ct), ct);

    public Task<ApiResult<Falah.Application.BusinessDay.BusinessDayDto>> ReopenBusinessDayAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.BusinessDay.BusinessDayDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/reopen", request, ct), ct);

    public Task<ApiResult<bool>> SetExceptionStatusAsync(Falah.Application.BusinessDay.SetExceptionStatusRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/control-exceptions/{Uri.EscapeDataString(request.ExceptionId)}/status", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<PrintJobDto>>> GetPendingPrintJobsAsync(string route, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<PrintJobDto>>(() => _http.GetAsync(
            $"/api/print-jobs/pending?route={Uri.EscapeDataString(route)}", ct), ct);

    // ---------------- Users / permissions / audit ----------------

    public Task<ApiResult<CreateUserResult>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default) =>
        SendAsync<CreateUserResult>(() => _http.PostAsJsonAsync("/api/users", request, ct), ct);

    public Task<ApiResult<bool>> ChangePinAsync(ChangePinRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/users/{Uri.EscapeDataString(request.UserId)}/change-pin", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<UserDetailsDto>>> GetAllUsersAsync(string actorUserId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<UserDetailsDto>>(() => _http.GetAsync(
            $"/api/users/all?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Domain.Authorization.PermissionDefinition>>> GetPermissionCatalogAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Domain.Authorization.PermissionDefinition>>(() => _http.GetAsync("/api/permissions/catalog", ct), ct);

    public Task<ApiResult<bool>> SetUserActiveAsync(SetUserActiveRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/users/{Uri.EscapeDataString(request.UserId)}/active", request, ct), ct);

    public Task<ApiResult<bool>> UpdateUserPermissionsAsync(UpdateUserPermissionsRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync(
            $"/api/users/{Uri.EscapeDataString(request.UserId)}/permissions", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Audit.AuditEventDto>>> GetAuditEventsAsync(
        string actorUserId, string? category = null, string? severity = null, int limit = 200, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Audit.AuditEventDto>>(() => _http.GetAsync(
            $"/api/audit/events?actorUserId={Uri.EscapeDataString(actorUserId)}&category={Uri.EscapeDataString(category ?? "")}&severity={Uri.EscapeDataString(severity ?? "")}&limit={limit}", ct), ct);

    // ---------------- Employees / attendance / payroll ----------------

    public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeDto>>> GetEmployeesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Employees.EmployeeDto>>(() => _http.GetAsync("/api/employees", ct), ct);

    public sealed record EmployeeBalanceDto(string EmployeeId, long BalancePiastres);

    public Task<ApiResult<EmployeeBalanceDto>> GetEmployeeBalanceAsync(string employeeId, CancellationToken ct = default) =>
        SendAsync<EmployeeBalanceDto>(() => _http.GetAsync($"/api/employees/{Uri.EscapeDataString(employeeId)}/balance", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.EmployeeLedgerEntryDto>>> GetEmployeeLedgerAsync(string employeeId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Employees.EmployeeLedgerEntryDto>>(() => _http.GetAsync(
            $"/api/employees/{Uri.EscapeDataString(employeeId)}/ledger", ct), ct);

    public Task<ApiResult<Falah.Application.Employees.EmployeeCashOutResult>> EmployeeCashOutAsync(Falah.Application.Employees.RecordEmployeeCashOutRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.EmployeeCashOutResult>(() => _http.PostAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}/cash-out", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.AttendanceRecordDto>>> GetAttendanceAsync(DateOnly? date = null, string? employeeId = null, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Employees.AttendanceRecordDto>>(() => _http.GetAsync(
            $"/api/attendance?employeeId={Uri.EscapeDataString(employeeId ?? "")}&date={(date is null ? "" : date.Value.ToString("yyyy-MM-dd"))}", ct), ct);

    public Task<ApiResult<Falah.Application.Employees.AttendanceRecordDto>> CorrectAttendanceAsync(Falah.Application.Employees.CorrectAttendanceRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.AttendanceRecordDto>(() => _http.PostAsJsonAsync(
            $"/api/attendance/{Uri.EscapeDataString(request.RecordId)}/correct", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.PayrollRunDto>>> GetPayrollRunsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Employees.PayrollRunDto>>(() => _http.GetAsync("/api/payroll-runs", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Employees.PayrollLineDto>>> GetPayrollLinesAsync(string runId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Employees.PayrollLineDto>>(() => _http.GetAsync(
            $"/api/payroll-runs/{Uri.EscapeDataString(runId)}/lines", ct), ct);

    public Task<ApiResult<Falah.Application.Employees.PayrollRunDto>> CreatePayrollRunAsync(Falah.Application.Employees.CreatePayrollRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.PayrollRunDto>(() => _http.PostAsJsonAsync("/api/payroll-runs", request, ct), ct);

    public Task<ApiResult<Falah.Application.Employees.PayrollRunDto>> TransitionPayrollAsync(Falah.Application.Employees.TransitionPayrollRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.PayrollRunDto>(() => _http.PostAsJsonAsync(
            $"/api/payroll-runs/{Uri.EscapeDataString(request.PayrollRunId)}/transition", request, ct), ct);

    public Task<ApiResult<Falah.Application.Employees.PayrollRunDto>> GetPayrollRunAsync(string runId, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.PayrollRunDto>(() => _http.GetAsync(
            $"/api/payroll-runs/{Uri.EscapeDataString(runId)}", ct), ct);

    public Task<ApiResult<Falah.Application.Employees.PayPayrollResult>> PayPayrollAsync(Falah.Application.Employees.PayPayrollRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Employees.PayPayrollResult>(() => _http.PostAsJsonAsync(
            $"/api/payroll-runs/{Uri.EscapeDataString(request.PayrollRunId)}/pay", request, ct), ct);

    // ---------------- Recipes / costing / loss ----------------

    public Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.RecipeDto>>> GetRecipesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Inventory.RecipeDto>>(() => _http.GetAsync("/api/recipes", ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.RecipeCostDto>> GetRecipeCostAsync(string recipeId, string actorUserId, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.RecipeCostDto>(() => _http.GetAsync(
            $"/api/recipes/{Uri.EscapeDataString(recipeId)}/cost?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.MenuItemProfitabilityDto>> GetRecipeProfitabilityAsync(string recipeId, long sellingPricePiastres, string actorUserId, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.MenuItemProfitabilityDto>(() => _http.GetAsync(
            $"/api/recipes/{Uri.EscapeDataString(recipeId)}/profitability?sellingPricePiastres={sellingPricePiastres}&actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    // The three below replaced a load that asked the server for a cost and then a margin for every
    // single recipe in turn - two hundred requests to draw one screen on this menu.

    public Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.RecipeCostSummaryDto>>> GetRecipeCostSummariesAsync(string actorUserId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Inventory.RecipeCostSummaryDto>>(() => _http.GetAsync(
            $"/api/recipes/cost-summary?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.RecipeCostCoverageDto>> GetRecipeCostCoverageAsync(string actorUserId, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.RecipeCostCoverageDto>(() => _http.GetAsync(
            $"/api/recipes/cost-coverage?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.RecipeCostDetailDto>> GetRecipeCostDetailAsync(string recipeId, string actorUserId, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.RecipeCostDetailDto>(() => _http.GetAsync(
            $"/api/recipes/{Uri.EscapeDataString(recipeId)}/cost-detail?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.RecipeDto>> SaveRecipeAsync(Falah.Application.Inventory.SaveRecipeRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.RecipeDto>(() => _http.PostAsJsonAsync("/api/recipes", request, ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.LossSummaryDto>> GetLossSummaryAsync(string actorUserId, DateOnly businessDate, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.LossSummaryDto>(() => _http.GetAsync(
            $"/api/inventory/loss-summary?businessDate={businessDate:yyyy-MM-dd}&actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    // ---------------- Purchasing / suppliers ----------------

    public Task<ApiResult<IReadOnlyList<Falah.Application.Purchasing.SupplierDto>>> GetSuppliersAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Purchasing.SupplierDto>>(() => _http.GetAsync("/api/suppliers", ct), ct);

    public Task<ApiResult<Falah.Application.Purchasing.AddSupplierResult>> AddSupplierAsync(Falah.Application.Purchasing.AddSupplierRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Purchasing.AddSupplierResult>(() => _http.PostAsJsonAsync("/api/suppliers", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Purchasing.SupplierLedgerEntryDto>>> GetSupplierLedgerAsync(string supplierId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Purchasing.SupplierLedgerEntryDto>>(() => _http.GetAsync(
            $"/api/suppliers/{Uri.EscapeDataString(supplierId)}/ledger", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Purchasing.PurchaseDto>>> GetPurchasesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Purchasing.PurchaseDto>>(() => _http.GetAsync("/api/purchases", ct), ct);

    public Task<ApiResult<Falah.Application.Purchasing.PurchaseDto>> ReceivePurchaseAsync(Falah.Application.Purchasing.ReceivePurchaseRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Purchasing.PurchaseDto>(() => _http.PostAsJsonAsync("/api/purchases", request, ct), ct);

    public Task<ApiResult<Falah.Application.Purchasing.PurchaseDto>> PayPurchaseAsync(Falah.Application.Purchasing.PayPurchaseRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Purchasing.PurchaseDto>(() => _http.PostAsJsonAsync(
            $"/api/purchases/{Uri.EscapeDataString(request.PurchaseId)}/payments", request, ct), ct);

    public Task<ApiResult<Falah.Application.Purchasing.PayablesAgingDto>> GetPayablesAgingAsync(CancellationToken ct = default) =>
        SendAsync<Falah.Application.Purchasing.PayablesAgingDto>(() => _http.GetAsync("/api/purchases/payables-aging", ct), ct);

    public Task<ApiResult<Falah.Application.Purchasing.PurchaseDto>> SetPurchaseDueDateAsync(Falah.Application.Purchasing.SetPurchaseDueDateRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Purchasing.PurchaseDto>(() => _http.PutAsJsonAsync(
            $"/api/purchases/{Uri.EscapeDataString(request.PurchaseId)}/due-date", request, ct), ct);

    // ---------------- Treasury (balances, ledger, owner-level money movement) ----------------

    public Task<ApiResult<IReadOnlyList<TreasuryAccountBalanceDto>>> GetTreasuryBalancesAsync(string actorUserId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<TreasuryAccountBalanceDto>>(() => _http.GetAsync(
            $"/api/treasury/balances?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<TreasuryMovementDto>>> GetTreasuryMovementsAsync(string actorUserId, string? accountId = null, int limit = 100, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<TreasuryMovementDto>>(() => _http.GetAsync(
            $"/api/treasury/movements?actorUserId={Uri.EscapeDataString(actorUserId)}&accountId={Uri.EscapeDataString(accountId ?? "")}&limit={limit}", ct), ct);

    public Task<ApiResult<DepositResult>> TreasuryDepositAsync(DepositRequest request, CancellationToken ct = default) =>
        SendAsync<DepositResult>(() => _http.PostAsJsonAsync("/api/treasury/deposit", request, ct), ct);

    public Task<ApiResult<WithdrawalResult>> TreasuryWithdrawAsync(WithdrawalRequest request, CancellationToken ct = default) =>
        SendAsync<WithdrawalResult>(() => _http.PostAsJsonAsync("/api/treasury/withdraw", request, ct), ct);

    public Task<ApiResult<TransferResult>> TreasuryTransferAsync(TransferRequest request, CancellationToken ct = default) =>
        SendAsync<TransferResult>(() => _http.PostAsJsonAsync("/api/treasury/transfer", request, ct), ct);

    // ---------------- Inventory (items, balances, physical counts, waste, movement history) ----------------

    public Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.InventoryItemDto>>> GetInventoryItemsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Inventory.InventoryItemDto>>(() => _http.GetAsync("/api/inventory/items", ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.CreateInventoryItemResult>> CreateInventoryItemAsync(Falah.Application.Inventory.CreateInventoryItemRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.CreateInventoryItemResult>(() => _http.PostAsJsonAsync("/api/inventory/items", request, ct), ct);

    public Task<ApiResult<bool>> UpdateInventoryItemAsync(Falah.Application.Inventory.UpdateInventoryItemRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PutAsJsonAsync($"/api/inventory/items/{Uri.EscapeDataString(request.Id)}", request, ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.RecordCountAdjustmentResult>> RecordCountAdjustmentAsync(Falah.Application.Inventory.RecordCountAdjustmentRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.RecordCountAdjustmentResult>(() => _http.PostAsJsonAsync("/api/inventory/count-adjustments", request, ct), ct);

    public Task<ApiResult<Falah.Application.Inventory.RecordWasteResult>> RecordWasteAsync(Falah.Application.Inventory.RecordWasteRequest request, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Inventory.RecordWasteResult>(() => _http.PostAsJsonAsync("/api/inventory/waste", request, ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Inventory.StockMovementDto>>> GetStockMovementsAsync(string? inventoryItemId = null, int limit = 50, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Inventory.StockMovementDto>>(() => _http.GetAsync(
            $"/api/inventory/movements?inventoryItemId={Uri.EscapeDataString(inventoryItemId ?? "")}&limit={limit}", ct), ct);

    // ---------------- Reports (read-only; every endpoint is gated on reports_view server-side) ----------------

    /// <summary>DateOnly is serialized as the server's own route/query format (yyyy-MM-dd), never a
    /// culture-formatted date - an Arabic/Egyptian locale would otherwise send a string the model
    /// binder cannot parse.</summary>
    private static string Day(DateOnly? d) => d is null ? "" : d.Value.ToString("yyyy-MM-dd");

    public Task<ApiResult<Falah.Application.Reports.SalesReportDto>> GetSalesReportAsync(string actorUserId, DateOnly? businessDate, string? shiftId = null, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Reports.SalesReportDto>(() => _http.GetAsync(
            $"/api/reports/sales?actorUserId={Uri.EscapeDataString(actorUserId)}&businessDate={Day(businessDate)}&shiftId={Uri.EscapeDataString(shiftId ?? "")}", ct), ct);

    public Task<ApiResult<Falah.Application.Reports.ExpenseReportDto>> GetExpenseReportAsync(string actorUserId, DateOnly? businessDate, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Reports.ExpenseReportDto>(() => _http.GetAsync(
            $"/api/reports/expenses?actorUserId={Uri.EscapeDataString(actorUserId)}&businessDate={Day(businessDate)}", ct), ct);

    public Task<ApiResult<Falah.Application.Reports.TaxReportDto>> GetTaxReportAsync(string actorUserId, DateOnly? businessDate, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Reports.TaxReportDto>(() => _http.GetAsync(
            $"/api/reports/tax?actorUserId={Uri.EscapeDataString(actorUserId)}&businessDate={Day(businessDate)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Reports.TopSellingItemDto>>> GetTopItemsAsync(string actorUserId, DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Reports.TopSellingItemDto>>(() => _http.GetAsync(
            $"/api/reports/top-items?actorUserId={Uri.EscapeDataString(actorUserId)}&from={Day(from)}&to={Day(to)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Reports.CategorySalesDto>>> GetCategorySalesAsync(string actorUserId, DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Reports.CategorySalesDto>>(() => _http.GetAsync(
            $"/api/reports/category-sales?actorUserId={Uri.EscapeDataString(actorUserId)}&from={Day(from)}&to={Day(to)}", ct), ct);

    public Task<ApiResult<Falah.Application.Reports.DeliveryOutcomesReportDto>> GetDeliveryOutcomesAsync(string actorUserId, DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Reports.DeliveryOutcomesReportDto>(() => _http.GetAsync(
            $"/api/reports/delivery-outcomes?actorUserId={Uri.EscapeDataString(actorUserId)}&from={Day(from)}&to={Day(to)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Reports.SignificantActionDto>>> GetSignificantActionsAsync(string actorUserId, DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Reports.SignificantActionDto>>(() => _http.GetAsync(
            $"/api/reports/significant-actions?actorUserId={Uri.EscapeDataString(actorUserId)}&from={Day(from)}&to={Day(to)}", ct), ct);

    public Task<ApiResult<IReadOnlyList<Falah.Application.Reports.RefundActivityDto>>> GetRefundActivityAsync(string actorUserId, DateOnly? businessDate, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<Falah.Application.Reports.RefundActivityDto>>(() => _http.GetAsync(
            $"/api/reports/refunds?actorUserId={Uri.EscapeDataString(actorUserId)}&businessDate={Day(businessDate)}", ct), ct);

    public Task<ApiResult<Falah.Application.Reports.FoodCostReportDto>> GetFoodCostReportAsync(string actorUserId, DateOnly? from, DateOnly? to, CancellationToken ct = default) =>
        SendAsync<Falah.Application.Reports.FoodCostReportDto>(() => _http.GetAsync(
            $"/api/reports/food-cost?actorUserId={Uri.EscapeDataString(actorUserId)}&from={Day(from)}&to={Day(to)}", ct), ct);

    // ---------------- Delivery dispatch (drivers, board, assignment, outcomes, settlement) ----------------

    public Task<ApiResult<IReadOnlyList<DriverDto>>> GetActiveDriversAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DriverDto>>(() => _http.GetAsync("/api/delivery/drivers", ct), ct);

    public Task<ApiResult<IReadOnlyList<DriverDto>>> GetAllDriversAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DriverDto>>(() => _http.GetAsync("/api/delivery/drivers/all", ct), ct);

    public Task<ApiResult<DriverDto>> CreateDriverAsync(CreateDriverRequest request, CancellationToken ct = default) =>
        SendAsync<DriverDto>(() => _http.PostAsJsonAsync("/api/delivery/drivers", request, ct), ct);

    public Task<ApiResult<DriverDto>> SetDriverActiveAsync(SetDriverActiveRequest request, CancellationToken ct = default) =>
        SendAsync<DriverDto>(() => _http.PostAsJsonAsync($"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId)}/set-active", request, ct), ct);

    /// <summary>Every delivery still in play, including the `New` ones a checkout just created -
    /// the live board. See DeliveryQueryService.ListDispatchBoardAsync.</summary>
    public Task<ApiResult<IReadOnlyList<DeliveryDto>>> GetDispatchBoardAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DeliveryDto>>(() => _http.GetAsync("/api/delivery/board", ct), ct);

    public Task<ApiResult<bool>> MarkDeliveryReadyAsync(MarkDeliveryReadyRequest request, CancellationToken ct = default) =>
        SendEmptyAsync(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/ready", request, ct), ct);

    public Task<ApiResult<AssignDeliveryResult>> AssignDeliveryAsync(AssignDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync<AssignDeliveryResult>(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/assign", request, ct), ct);

    public Task<ApiResult<ReassignDeliveryResult>> ReassignDeliveryAsync(ReassignDeliveryRequest request, CancellationToken ct = default) =>
        SendAsync<ReassignDeliveryResult>(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/reassign", request, ct), ct);

    public Task<ApiResult<RecordDeliveryResultResult>> RecordDeliveryResultAsync(RecordDeliveryResultRequest request, CancellationToken ct = default) =>
        SendAsync<RecordDeliveryResultResult>(() => _http.PostAsJsonAsync($"/api/delivery/{Uri.EscapeDataString(request.DeliveryId)}/result", request, ct), ct);

    public Task<ApiResult<SettleDriverCustodyResult>> SettleDriverCustodyAsync(SettleDriverCustodyRequest request, CancellationToken ct = default) =>
        SendAsync<SettleDriverCustodyResult>(() => _http.PostAsJsonAsync($"/api/delivery/drivers/{Uri.EscapeDataString(request.DriverId)}/settle", request, ct), ct);

    public Task<ApiResult<DriverAccountDto>> GetDriverAccountAsync(string driverId, CancellationToken ct = default) =>
        SendAsync<DriverAccountDto>(() => _http.GetAsync($"/api/delivery/drivers/{Uri.EscapeDataString(driverId)}/account", ct), ct);

    public Task<ApiResult<IReadOnlyList<DeliveryEventDto>>> GetDeliveryTimelineAsync(string deliveryId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DeliveryEventDto>>(() => _http.GetAsync($"/api/delivery/{Uri.EscapeDataString(deliveryId)}/timeline", ct), ct);

    public async Task<ApiResult<OrderDetailsDto>> LookupOrderByNumberAsync(string orderNumber, CancellationToken ct = default)
    {
        return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/lookup?orderNumber={Uri.EscapeDataString(orderNumber)}", ct), ct);
    }

    public async Task<ApiResult<OrderDetailsDto>> GetOrderAsync(string orderId, CancellationToken ct = default)
    {
        return await SendAsync<OrderDetailsDto>(() => _http.GetAsync($"/api/orders/{Uri.EscapeDataString(orderId)}", ct), ct);
    }

    public async Task<ApiResult<IssueManagerApprovalResult>> IssueManagerApprovalAsync(IssueManagerApprovalRequest request, CancellationToken ct = default)
    {
        return await SendAsync<IssueManagerApprovalResult>(() => _http.PostAsJsonAsync("/api/approvals/issue", request, ct), ct);
    }

    public async Task<ApiResult<AddExpenseResult>> AddExpenseAsync(AddExpenseRequest request, CancellationToken ct = default)
    {
        return await SendAsync<AddExpenseResult>(() => _http.PostAsJsonAsync("/api/expenses", request, ct), ct);
    }

    // ---------------- Cashier Operations (Safe Drop / Cash In-Out / Cash Drawer / Cash Count / Reprint) ----------------

    public async Task<ApiResult<SafeDropResult>> SafeDropAsync(SafeDropRequest request, CancellationToken ct = default)
    {
        return await SendAsync<SafeDropResult>(() => _http.PostAsJsonAsync("/api/treasury/safe-drop", request, ct), ct);
    }

    public async Task<ApiResult<CashierCashMovementResult>> CashierCashMovementAsync(CashierCashMovementRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CashierCashMovementResult>(() => _http.PostAsJsonAsync("/api/cash-drawer/cash-movement", request, ct), ct);
    }

    public async Task<ApiResult<CashDrawerOpenDto>> OpenCashDrawerAsync(OpenCashDrawerRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CashDrawerOpenDto>(() => _http.PostAsJsonAsync("/api/cash-drawer/open", request, ct), ct);
    }

    public sealed record TreasuryBalanceDto(string AccountId, long BalancePiastres);

    /// <summary>Read-only, no side effects - "Cash Count" from the Cashier Operations menu is
    /// literally this call against "tre-drawer", not a bespoke calculation kept in sync by hand.</summary>
    public async Task<ApiResult<TreasuryBalanceDto>> GetTreasuryBalanceAsync(string accountId, CancellationToken ct = default)
    {
        return await SendAsync<TreasuryBalanceDto>(() => _http.GetAsync($"/api/treasury/balance?accountId={Uri.EscapeDataString(accountId)}", ct), ct);
    }

    public async Task<ApiResult<CreatePrintJobsResult>> CreatePrintJobsAsync(string orderId, CreatePrintJobsRequest request, CancellationToken ct = default)
    {
        return await SendAsync<CreatePrintJobsResult>(() => _http.PostAsJsonAsync($"/api/orders/{Uri.EscapeDataString(orderId)}/print-jobs", request, ct), ct);
    }
}
