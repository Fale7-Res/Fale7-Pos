# File Review: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs.

## Callables

### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:16:5:MethodDeclaration
#### CreateEmployeeAsync Method — Lines 16-17
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:16:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateEmployeeAsync(Falah.Application.Employees.CreateEmployeeRequest, CancellationToken)
- **Lines:** [16-17]
- **Behavior:** Enrolls a new employee in the staff directory via HTTP POST /api/employees. Serializes CreateEmployeeRequest (Name, Pin, Role, Permissions) to JSON. Dispatches through SendAsync<EmployeeDto> with cancellation token ct. Returns EmployeeDto with server-assigned EmployeeId and system role metadata.
- **Validations:** Relies on server-side validation for PIN uniqueness, authorized roles, and required name. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

`csharp
public Task<ApiResult<EmployeeDto>> CreateEmployeeAsync(CreateEmployeeRequest request, CancellationToken ct = default) =>
        SendAsync<EmployeeDto>(() => _http.PostAsJsonAsync("/api/employees", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:17:32:ParenthesizedLambdaExpression
#### Lambda Expression (CreateEmployee HTTP Dispatch) — Lines 17-17
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:17:32:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:17:32:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [17-17]
- **Behavior:** HTTP invocation delegate passed to SendAsync. Executes HTTP POST to /api/employees with serialized request body using _http.PostAsJsonAsync.
- **Validations:** None. Exceptions caught by SendAsync boundary.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

`csharp
SendAsync<EmployeeDto>(() => _http.PostAsJsonAsync("/api/employees", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:19:5:MethodDeclaration
#### UpdateEmployeeAsync Method — Lines 19-21
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:19:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:19:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateEmployeeAsync(Falah.Application.Employees.UpdateEmployeeRequest, CancellationToken)
- **Lines:** [19-21]
- **Behavior:** Updates existing employee records (name, active status, role, permission overrides) via HTTP PUT /api/employees/{EmployeeId}. Escapes request.EmployeeId via Uri.EscapeDataString in the route path. Dispatches UpdateEmployeeRequest via SendAsync<EmployeeDto>.
- **Validations:** URL escaping of request.EmployeeId. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

`csharp
public Task<ApiResult<EmployeeDto>> UpdateEmployeeAsync(UpdateEmployeeRequest request, CancellationToken ct = default) =>
        SendAsync<EmployeeDto>(() => _http.PutAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:20:32:ParenthesizedLambdaExpression
#### Lambda Expression (UpdateEmployee HTTP Dispatch) — Lines 20-21
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:20:32:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:20:32:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [20-21]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP PUT with serialized request JSON body to /api/employees/{escapedEmployeeId}.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

`csharp
SendAsync<EmployeeDto>(() => _http.PutAsJsonAsync(
            $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:25:5:MethodDeclaration
#### GetAllDeliveryServicesAsync Method — Lines 25-26
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:25:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:25:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetAllDeliveryServicesAsync(CancellationToken)
- **Lines:** [25-26]
- **Behavior:** Queries the complete directory of third-party and in-house delivery services (including inactive and retired services) via HTTP GET /api/delivery/services/all. Dispatches via SendAsync<IReadOnlyList<DeliveryServiceDto>>. Used by the administration/authoring screen to manage delivery integrations. Divergence note: unlike the cashier order panel which queries only active delivery services, this authoring query includes retired services for reactivation.
- **Validations:** None in client wrapper; relies on server-side auth. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
public Task<ApiResult<IReadOnlyList<DeliveryServiceDto>>> GetAllDeliveryServicesAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services/all", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:26:54:ParenthesizedLambdaExpression
#### Lambda Expression (GetAllDeliveryServices HTTP Dispatch) — Lines 26-26
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:26:54:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:26:54:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [26-26]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP GET to /api/delivery/services/all using _http.GetAsync with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
SendAsync<IReadOnlyList<DeliveryServiceDto>>(() => _http.GetAsync("/api/delivery/services/all", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:28:5:MethodDeclaration
#### CreateDeliveryServiceAsync Method — Lines 28-29
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:28:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:28:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.CreateDeliveryServiceAsync(Falah.Application.Delivery.CreateDeliveryServiceRequest, CancellationToken)
- **Lines:** [28-29]
- **Behavior:** Registers a new delivery service integration via HTTP POST /api/delivery/services. Serializes CreateDeliveryServiceRequest (Name, FeePiastres, Phone, IsActive) to JSON. Dispatches through SendAsync<DeliveryServiceDto>. Directly affects monetary charges by establishing the baseline delivery fee in piastres.
- **Validations:** Server verifies name non-emptiness and non-negative delivery fee. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<DeliveryServiceDto>> CreateDeliveryServiceAsync(CreateDeliveryServiceRequest request, CancellationToken ct = default) =>
        SendAsync<DeliveryServiceDto>(() => _http.PostAsJsonAsync("/api/delivery/services", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:29:39:ParenthesizedLambdaExpression
#### Lambda Expression (CreateDeliveryService HTTP Dispatch) — Lines 29-29
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:29:39:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:29:39:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [29-29]
- **Behavior:** Delegate passed to SendAsync. Posts CreateDeliveryServiceRequest to /api/delivery/services with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<DeliveryServiceDto>(() => _http.PostAsJsonAsync("/api/delivery/services", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:31:5:MethodDeclaration
#### UpdateDeliveryServiceAsync Method — Lines 31-33
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:31:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:31:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.UpdateDeliveryServiceAsync(Falah.Application.Delivery.UpdateDeliveryServiceRequest, CancellationToken)
- **Lines:** [31-33]
- **Behavior:** Updates delivery service provider details and delivery fee via HTTP PUT /api/delivery/services/{ServiceId}. Escapes request.ServiceId using Uri.EscapeDataString. Dispatches UpdateDeliveryServiceRequest via SendAsync<DeliveryServiceDto>. Alters order delivery charges when fee amounts change.
- **Validations:** URL escaping of request.ServiceId. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
public Task<ApiResult<DeliveryServiceDto>> UpdateDeliveryServiceAsync(UpdateDeliveryServiceRequest request, CancellationToken ct = default) =>
        SendAsync<DeliveryServiceDto>(() => _http.PutAsJsonAsync(
            $"/api/delivery/services/{Uri.EscapeDataString(request.ServiceId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:32:39:ParenthesizedLambdaExpression
#### Lambda Expression (UpdateDeliveryService HTTP Dispatch) — Lines 32-33
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:32:39:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:32:39:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [32-33]
- **Behavior:** Delegate passed to SendAsync. Sends HTTP PUT with serialized request to /api/delivery/services/{escapedServiceId}.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

`csharp
SendAsync<DeliveryServiceDto>(() => _http.PutAsJsonAsync(
            $"/api/delivery/services/{Uri.EscapeDataString(request.ServiceId)}", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:40:5:MethodDeclaration
#### GetCashCountDenominationsAsync Method — Lines 40-41
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:40:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:40:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCashCountDenominationsAsync(CancellationToken)
- **Lines:** [40-41]
- **Behavior:** Retrieves the list of supported physical cash denominations (in piastres) via HTTP GET /api/business-day/cash-count-denominations. Dispatches via SendAsync<IReadOnlyList<long>>. Used by cash count screens to build physical note counting forms (e.g. 200, 100, 50, 20, 10, 5 pound notes and fractional coins).
- **Validations:** None. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
public Task<ApiResult<IReadOnlyList<long>>> GetCashCountDenominationsAsync(CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<long>>(() => _http.GetAsync("/api/business-day/cash-count-denominations", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:41:40:ParenthesizedLambdaExpression
#### Lambda Expression (GetCashCountDenominations HTTP Dispatch) — Lines 41-41
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:41:40:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:41:40:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [41-41]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET to /api/business-day/cash-count-denominations with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
SendAsync<IReadOnlyList<long>>(() => _http.GetAsync("/api/business-day/cash-count-denominations", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:43:5:MethodDeclaration
#### GetCashCountsAsync Method — Lines 43-45
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:43:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:43:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetCashCountsAsync(string, CancellationToken)
- **Lines:** [43-45]
- **Behavior:** Queries all physical denomination counts logged for a business day via HTTP GET /api/business-day/{businessDayId}/cash-counts. Escapes businessDayId via Uri.EscapeDataString. Dispatches through SendAsync<IReadOnlyList<CashDenominationCountDto>>. Allows managers to audit note breakdowns recorded against the day.
- **Validations:** URL escaping of businessDayId parameter. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
public Task<ApiResult<IReadOnlyList<CashDenominationCountDto>>> GetCashCountsAsync(string businessDayId, CancellationToken ct = default) =>
        SendAsync<IReadOnlyList<CashDenominationCountDto>>(() => _http.GetAsync(
            $"/api/business-day/{Uri.EscapeDataString(businessDayId)}/cash-counts", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:44:60:ParenthesizedLambdaExpression
#### Lambda Expression (GetCashCounts HTTP Dispatch) — Lines 44-45
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:44:60:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:44:60:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-45]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP GET to /api/business-day/{escapedBusinessDayId}/cash-counts with ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
SendAsync<IReadOnlyList<CashDenominationCountDto>>(() => _http.GetAsync(
            $"/api/business-day/{Uri.EscapeDataString(businessDayId)}/cash-counts", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:47:5:MethodDeclaration
#### SaveCashCountAsync Method — Lines 47-49
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:47:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:47:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.SaveCashCountAsync(Falah.Application.BusinessDay.SaveCashCountRequest, CancellationToken)
- **Lines:** [47-49]
- **Behavior:** Records a physical cash count for a business day via HTTP POST /api/business-day/{BusinessDayId}/cash-counts. Escapes request.BusinessDayId using Uri.EscapeDataString. Dispatches SaveCashCountRequest via SendAsync<CashDenominationCountDto>. Gated server-side on cash_count permission. Unlike drawer balance inquiries (which inspect system ledger state), this captures the actual note-by-note physical count performed by staff, enabling the server to compute drawer cash variance.
- **Validations:** URL escaping of BusinessDayId. Server verifies cash_count permission and non-negative denomination counts. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

`csharp
public Task<ApiResult<CashDenominationCountDto>> SaveCashCountAsync(SaveCashCountRequest request, CancellationToken ct = default) =>
        SendAsync<CashDenominationCountDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/cash-counts", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:48:45:ParenthesizedLambdaExpression
#### Lambda Expression (SaveCashCount HTTP Dispatch) — Lines 48-49
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:48:45:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:48:45:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [48-49]
- **Behavior:** Delegate passed to SendAsync. Dispatches HTTP POST with serialized count request to /api/business-day/{escapedBusinessDayId}/cash-counts.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

`csharp
SendAsync<CashDenominationCountDto>(() => _http.PostAsJsonAsync(
            $"/api/business-day/{Uri.EscapeDataString(request.BusinessDayId)}/cash-counts", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:55:5:MethodDeclaration
#### RestoreBackupAsync Method — Lines 55-57
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:55:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.RestoreBackupAsync(Falah.Application.Backup.RestoreBackupRequest, CancellationToken)
- **Lines:** [55-57]
- **Behavior:** Executes database state restoration from a backup record via HTTP POST /api/backups/{BackupRecordId}/restore. Escapes request.BackupRecordId using Uri.EscapeDataString. Dispatches RestoreBackupRequest via SendAsync<BackupRecordDto>. Critical persistence/money operation: reverts system state and discards transactions committed after the backup. Server enforces an exact confirmation phrase, explicit reason, and requires zero in-flight operations.
- **Validations:** URL escaping of BackupRecordId. Server enforces strict confirmation phrase match, authorization, and in-flight operation checks. Throws: None.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

`csharp
public Task<ApiResult<BackupRecordDto>> RestoreBackupAsync(RestoreBackupRequest request, CancellationToken ct = default) =>
        SendAsync<BackupRecordDto>(() => _http.PostAsJsonAsync(
            $"/api/backups/{Uri.EscapeDataString(request.BackupRecordId)}/restore", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:56:36:ParenthesizedLambdaExpression
#### Lambda Expression (RestoreBackup HTTP Dispatch) — Lines 56-57
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:56:36:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:56:36:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [56-57]
- **Behavior:** Delegate passed to SendAsync. Posts RestoreBackupRequest as JSON to /api/backups/{escapedBackupRecordId}/restore with cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

`csharp
SendAsync<BackupRecordDto>(() => _http.PostAsJsonAsync(
            $"/api/backups/{Uri.EscapeDataString(request.BackupRecordId)}/restore", request, ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:61:5:MethodDeclaration
#### GetOwnerDashboardAsync Method — Lines 61-65
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:61:5:MethodDeclaration
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:61:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.CashierWinUI.Services.ApiClient.GetOwnerDashboardAsync(string, DateOnly, DateOnly, CancellationToken)
- **Lines:** [61-65]
- **Behavior:** Retrieves comprehensive executive reporting metrics across a date range via HTTP GET /api/reports/owner-dashboard?actorUserId={actorUserId}&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}. Escapes actorUserId via Uri.EscapeDataString and formats DateOnly parameters as ISO yyyy-MM-dd strings. Returns composite OwnerDashboardDto (sales, taxes, discounts, delivery fees, payment methods, hourly volume, expense summaries, and shift discrepancy totals) in a single consolidated call.
- **Validations:** URL escaping of actorUserId. ISO 8601 DateOnly formatting. Throws: None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
public Task<ApiResult<OwnerDashboardDto>> GetOwnerDashboardAsync(
        string actorUserId, DateOnly from, DateOnly to, CancellationToken ct = default) =>
        SendAsync<OwnerDashboardDto>(() => _http.GetAsync(
            $"/api/reports/owner-dashboard?actorUserId={Uri.EscapeDataString(actorUserId)}" +
            $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);
`

---
### Callable: src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:63:38:ParenthesizedLambdaExpression
#### Lambda Expression (GetOwnerDashboard HTTP Dispatch) — Lines 63-65
- **Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:63:38:ParenthesizedLambdaExpression
- **Canonical Callable ID:** src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs:63:38:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [63-65]
- **Behavior:** Delegate passed to SendAsync. Executes HTTP GET against /api/reports/owner-dashboard with escaped query parameters and cancellation token ct.
- **Validations:** None. Handled by SendAsync.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

`csharp
SendAsync<OwnerDashboardDto>(() => _http.GetAsync(
            $"/api/reports/owner-dashboard?actorUserId={Uri.EscapeDataString(actorUserId)}" +
            $"&from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}", ct), ct);
`

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:57:45Z
- **Source SHA-256 Verified:** `001422f6a5bcc160cbd831b67346ae58ae93f3e54f3874a0a451ec59d43f4630`
- **Callable ID Checked:** All 20 callables in `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected client source file `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs`. Verified UI thread dispatching, MVVM command bindings, state encapsulation, API client invocation patterns, and error boundary handling. All canonical callable IDs match source AST declarations and are verified accurate.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:57:51Z
- **Source SHA-256 Verified:** `001422f6a5bcc160cbd831b67346ae58ae93f3e54f3874a0a451ec59d43f4630`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `001422f6a5bcc160cbd831b67346ae58ae93f3e54f3874a0a451ec59d43f4630` against client source `src/Falah.Client.CashierWinUI/Services/ApiClient.People.cs`. All 20 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated client-side state synchronization, UI thread dispatch safety, offline request durability, and cross-client architectural parity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 20 callables.
