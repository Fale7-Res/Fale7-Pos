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
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Chain:** `src/Falah.Server/Controllers/`, `src/Falah.Domain/Authorization/Actor.cs`, `src/Falah.Infrastructure/Orders/OrderMapper.cs:ToActor`.
- **Vulnerability Mechanism:**
  When deserializing incoming request payloads, ASP.NET Core minimal APIs and controller action parameter binders deserialize request bodies using `JsonSerializerOptions.PropertyNameCaseInsensitive = true`. Several internal command request models carry an optional or fallback `Actor` property. If an attacker submits a raw JSON payload specifying `{"actor": {"id": "usr-admin", "role": "Admin", "permissions": ["*"]}}`, the model binder instantiates the admin actor directly from the request body rather than binding from the authenticated claims principal.
- **Impact:** Privilege escalation: An ordinary cashier can execute manager-level actions (e.g. comping entire orders, applying arbitrary discounts, or wiping audit logs) by injecting an `actor` block into the JSON body.
- **Remediation:** Remove all `Actor`, `UserId`, or `Permissions` properties from wire DTOs. Authoritative user context must be constructed exclusively in server middleware from the cryptographically verified session token.

### SEC-002: PIN-Bearing User-Management Requests Stored as Unencrypted JSON
- **Severity:** `MEDIUM` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Chain:** `src/Falah.Domain/Authorization/PinHasher.cs`, `src/Falah.Infrastructure/Users/CreateUserHandler.cs`, `src/Falah.Infrastructure/Audit/AuditEventRepository.cs`.
- **Vulnerability Mechanism:**
  While `PinHasher.cs` properly hashes employee PINs using PBKDF2/SHA-256 for persistent user authentication records, the surrounding audit trail and request diagnostic logging capture the entire incoming HTTP command payload. In `CreateUserHandler` and `UpdateUserHandler`, the raw JSON payload—including the plaintext PIN string—is serialized into `AuditEvent.Details` or retained request caches.
- **Impact:** Credential exposure: Anyone with read access to the audit logs, error logs, or database backups can read every employee and manager PIN in plaintext.
- **Remediation:** Implement sensitive property masking on request DTOs (`[JsonIgnore]` or redacting PIN fields prior to audit logging).

### SEC-003: Authenticated Employee Reads Expose Complete Sensitive Profiles (PII & Salary)
- **Severity:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Source Chain:** `src/Falah.Server/Endpoints/EmployeeEndpoints.cs`, `src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`.
- **Vulnerability Mechanism:**
  The `GET /api/employees` and `GET /api/employees/{id}` endpoints require only general authentication (`pos` or `cashier` session). They return the full `EmployeeDto`, which contains national ID numbers, base salary, hourly overtime rates, home addresses, and phone numbers. There is no role-based filtering or field-level redaction restricting sensitive HR/payroll fields to managers with `employees_manage` or `payroll_view` permissions.
- **Impact:** Privacy violation and internal reconnaissance: Any cashier or dishwasher with a POS login can inspect the exact salaries, phone numbers, and national IDs of all coworkers.
- **Remediation:** Split the DTO into `PublicEmployeeSummaryDto` (Id, Name, Department, ActiveStatus) for operational till selection, and `FullEmployeeProfileDto` (Salary, National ID, Bank/Wallet) strictly protected by `employees_manage` authorization.

---

## 3. Backdoor, Hardcoded Secrets & Obfuscation Scan

An exhaustive static search across all 561 C# source files, configuration files, and SQL migration scripts was conducted to detect intentional backdoors, hardcoded development bypasses, or obfuscated payloads:
- **Hardcoded Master Passwords:** None found. No universal backdoor PIN exists in `PinHasher.cs` or authentication handlers.
- **Hidden Debug Endpoints:** No hidden `/api/debug` or `/api/backdoor` endpoints exist.
- **Unsafe SQL String Concatenation:** Query handlers predominantly use EF Core parameterized queries or Dapper parameter dictionaries. The only dynamic SQL queries occur in `FromSqlRaw` in `ConfirmElectronicPaymentHandler` and `OrderSequenceService`, both of which use parameterized formats (`{0}`).
- **Authentication Bypass Risk:** High via `SEC-001` body injection, but no deliberate intentional backdoor logic was identified.

---

Detailed findings reference: `GitHub Copilot Work/11-Master-Findings/RECONCILED_MASTER_FINDINGS.json`
