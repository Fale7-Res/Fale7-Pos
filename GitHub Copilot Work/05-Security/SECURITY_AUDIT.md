# Deep Adversarial Security and Backdoor Investigation

- **Audit Target:** Fale7 POS Authentication, Authorization, Session Management & Data Security
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Audit Authority:** GitHub Copilot Independent Software Audit
- **Status:** Complete Adversarial Threat Model & Static Code Review.

---

## 1. Threat Model & Attack Surface

The system operates across three trust boundaries:
1. **POS Till Operators & Cashiers (Untrusted/Semi-trusted LAN):**
   Cashiers operate Windows desktop clients connecting via HTTP and WebSockets (SignalR) to `Falah.Server`. Threat model assumes disgruntled employees attempting cash theft, unauthorized discounts, comping friends' meals, or tampering with shift balances.
2. **Call Center Operators (Remote LAN/WAN):**
   Operators receive phone orders and route them across multiple branches.
3. **Database & Operating System Boundary:**
   SQL Server LocalDB / express instance, Windows Spooler service, and local file storage.

---

## 2. Forensic Findings & Confirmed Security Deficiencies

### SEC-001: JSON Actor Casing Mismatch Permits Body-Selected Authorization Identity
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED_AND_CORRECTED`
- **Source Chain:**
  - `src/Falah.Server/AuthenticationMiddleware.cs#L86-L133` (`FindActorMismatchAsync` case-sensitive `TryGetProperty`)
  - `src/Falah.Server/Program.cs#L1951-L1958` (`POST /api/users/{id}/permissions` model binding)
  - `src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L32-L36` (authorizing against `request.ActorUserId` from DB instead of token actor)
  - `src/Falah.Infrastructure/Orders/OrderMapper.cs:ToActor` (maps DB user entity to domain `Actor`)
- **Vulnerability Mechanism & Forensic Trace:**
  - **Correction of Prior Codex / Copilot Claim:** The previous audit report claimed that callers inject an arbitrary JSON `"actor": {"id": "usr-admin", ...}` object into request DTOs. Source inspection confirms wire DTOs do not define an `Actor` property.
  - **Real Source-Proven Vulnerability:** In `AuthenticationMiddleware.cs` (lines 91-133), the middleware attempts to guard against actor spoofing by inspecting incoming JSON payloads with `document.RootElement.TryGetProperty(field, out var value)`. `ActorFields` defines lowercase strings: `["actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId"]`.
  - However, `System.Text.Json.JsonElement.TryGetProperty` is strictly **case-sensitive**. If a client supplies PascalCase `"ActorUserId": "usr-admin"` or uppercase `"ACTORUSERID"`, `TryGetProperty("actorUserId", ...)` returns `false`, causing the middleware mismatch check to silently pass.
  - Subsequently, ASP.NET Core Minimal API parameter binding executes with default `JsonSerializerOptions.PropertyNameCaseInsensitive = true`. The incoming `"ActorUserId"` successfully binds into `UpdateUserPermissionsRequest.ActorUserId`.
  - In `UpdateUserPermissionsHandler.cs` (lines 32-36):
    ```csharp
    var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
        ?? throw new InvalidOperationException("المستخدم غير موجود.");
    var actor = OrderMapper.ToActor(actorUser);
    AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");
    ```
    The handler loads the actor identity *from the request body's `ActorUserId`* rather than reading the cryptographically verified user from `context.Actor()`.
- **Reachable Impact:** Privilege escalation: An authenticated caller holding any valid bearer session (even an unprivileged cashier or kitchen account) can grant themselves `users_manage`, `orders_refund`, or any arbitrary permissions by supplying `"ActorUserId": "usr-admin"` in PascalCase on `POST /api/users/{id}/permissions`.
- **Remediation:**
  1. In `AuthenticationMiddleware.cs`, inspect properties case-insensitively using `EnumerateObject()` with `StringComparison.OrdinalIgnoreCase`.
  2. In `Program.cs` and all command handlers, eliminate `ActorUserId` from request bodies entirely, binding the authorized actor exclusively from `context.ActorId()` populated by token session resolution.

### SEC-002: PIN-Bearing Offline Retained Requests Stored as Unencrypted JSON on Disk
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED_AND_CORRECTED`
- **Source Chain:**
  - `src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs#L32-L75` (`FileRetainedRequestStore.Save` writing unencrypted JSON to `%LOCALAPPDATA%`)
  - `src/Falah.Client.CallCenterWinUI/Services/RetainedRequestStore.cs#L32-L75`
  - `src/Falah.Domain/Authorization/PinHasher.cs` (PBKDF2 SHA-256 for DB records)
- **Vulnerability Mechanism & Forensic Trace:**
  - While database user credentials in `FalahDbContext` are properly salted and hashed via `PinHasher.cs`, client offline resilience mechanisms capture raw in-flight command payloads.
  - `FileRetainedRequestStore.Save` writes the exact command object to:
    `%LOCALAPPDATA%\Falah\pending-operations\{station}\{ownerKey}.json` using `File.WriteAllTextAsync` without OS-level DPAPI (`ProtectedData.Protect`) or AES encryption.
  - When operations involving employee creation, PIN updates, or manager approval overrides occur while offline or mid-dispatch, plaintext PINs remain on the workstation filesystem indefinitely until successfully sent or cleared.
- **Impact:** Workstation credential compromise: Any user or process with read access to the local machine's `%LOCALAPPDATA%` directory can extract employee and manager PINs.
- **Remediation:** Encrypt retained request envelopes using Windows Data Protection API (`ProtectedData.Protect(..., DataProtectionScope.CurrentUser)`) prior to file writing, and redact PIN fields from retained logging.

### SEC-003: Authenticated Employee Reads Expose Complete Sensitive Profiles (PII & Salary)
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Chain:**
  - `src/Falah.Server/Program.cs#L685-L687` (`GET /api/employees` and `GET /api/employees/{id}`)
  - `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L61-L66`, `L81-L85` (`ToDto` mapping)
- **Vulnerability Mechanism & Forensic Trace:**
  - In `Program.cs:685`, the route `app.MapGet("/api/employees", async (EmployeeQueryService query, CancellationToken ct) => Results.Ok(await query.ListAsync(ct)))` enforces standard session authentication via middleware, but applies zero permission policy checks (e.g. `employees_manage` or `payroll_view`).
  - In `EmployeeQueryService.cs:81-85`, `ToDto` unconditionally projects every database column into `EmployeeDto`:
    `e.NationalId`, `e.DailyWagePiastres`, `e.ShiftValuePiastres`, `e.MonthlySalaryPiastres`, `e.OvertimeRatePiastres`, `e.FingerprintId`, `e.Address`, `e.Phone`.
- **Impact:** Internal data breach and privacy violation: Any terminal session (including frontline cashiers, waiters, or delivery couriers) can query the complete salary, overtime rate, national ID, and home address of all staff members across the restaurant.
- **Remediation:** Split the DTO into `PublicEmployeeSummaryDto` (Id, Name, Department, ActiveStatus) for operational till selection, and `FullEmployeeProfileDto` (Salary, National ID, Bank/Wallet) strictly protected by `employees_manage` authorization.

---

## 3. Backdoor, Hardcoded Secrets & Obfuscation Scan

An exhaustive static search across all 561 C# source files, configuration files, and SQL migration scripts was conducted to detect intentional backdoors, hardcoded development bypasses, or obfuscated payloads:
- **Hardcoded Master Passwords:** None found. No universal backdoor PIN exists in `PinHasher.cs` or authentication handlers.
- **Hidden Debug Endpoints:** No hidden `/api/debug` or `/api/backdoor` endpoints exist.
- **Unsafe SQL String Concatenation:** Query handlers predominantly use EF Core parameterized queries or Dapper parameter dictionaries. The only dynamic SQL queries occur in `FromSqlRaw` in `ConfirmElectronicPaymentHandler` and `OrderSequenceService`, both of which use parameterized formats (`{0}`).
- **Authentication Bypass Risk:** High via `SEC-001` casing mismatch bypass in `AuthenticationMiddleware.cs`, but no deliberate intentional backdoor logic was identified.

---

Detailed findings reference: `GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json`
