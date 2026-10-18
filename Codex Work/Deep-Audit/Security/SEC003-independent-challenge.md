# SEC-003 — independent source challenge

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. **Survives independent source challenge, with conditional impact/severity.** This is not a live exploit or a successful Codex Security scan. See [complete structured evidence](SEC003-independent-challenge.json) for exact hashes, symbol identities, source spans, all DTO fields, route matrix, counterevidence and tests.

## Confirmed mechanism

`Program.cs:251` registers AuthenticationMiddleware. `InvokeAsync:31-81` requires a valid session for `/api/` and rejects ordinary actor mismatches, but does not apply employee/read permissions. The employee list/detail wrappers (`Program.cs:685-691`) call EmployeeQueryService directly. `ListAsync/GetAsync:61-66` do not restrict permission, inactive rows or self ownership; `ToDto:81-85` maps compensation, contact, address, national identifier, fingerprint reference, notes and other HR fields. `EmployeeDto:24-29` contains those serializable fields, with no secret/PIN field or field suppression in the examined path.

The requester must already hold a valid active-user session and reach the backend. A profile path parameter identifies the requested employee; it is not an actor credential that the actor-mismatch check authenticates. A restricted client's hidden employee navigation cannot protect the direct HTTP endpoint. There is no need to depend on SEC-001 JSON-binding behavior to explain this missing read gate.

## Severity challenge and policy boundary

HIGH is defensible where real confidential HR/compensation/national-ID data is held and legitimate low-privilege POS users should not see it. The code establishes distinct employee/ledger/attendance/payroll permissions and a restricted employee navigation item (`ShellViewModel:132`), strongly supporting a separation expectation. Nevertheless no exact owner-approved field-level HR privacy policy was recovered, and no deployed data or restricted accounts were examined. If the owner intentionally shares a limited roster, that does not automatically authorize the full DTO; define the picker projection separately.

Do not claim anonymous access, leaked PINs/passwords, raw fingerprint templates, financial changes, code execution or a reproduced data extraction. FingerprintId is a reference string. UserId/Role in EmployeeDto are employee metadata, not a stolen authenticated token or proof of permission escalation. Source confidence is high; deployed exposure and exact policy-specific impact are unverified. No unconditional production CVSS score is assigned.

## Related routes and controls that actually exist

- Employee ledger and balance (`Program.cs:693-697`) are likewise direct reads without role/self checks. Ledger DTO exposes amounts, running balance, descriptions, actor/approver and reference/rate data; balance alone has narrower disclosure. A nonexistent balance target can sum an empty set to zero, so it does not universally return404.
- Attendance (`:807-808`) accepts optional employee/date filters; omitting them allows all query rows. The DTO includes attendance/correction details. The routine biometric-write comment describing device operation as authorization is not an explicit policy granting every authenticated user full attendance history.
- Payroll list/detail/lines (`:819-828`) have no read gate; direct line projection includes employee compensation components and net payable. Listing runs reveals run identities and metadata. The detail's CreatePayrollHandler.ToDto was not re-reviewed here; line/list mappings independently substantiate the disclosure family.
- Control-center audit (`:524-525`) exposes the newest narrow trail through `ListAuditEventsAsync:122-132`, including amounts, actors/approvers and reasons. This is not the entire general audit table.
- **Counterexample:** employee financial-report (`:702-707`) calls RequireReportsAccessAsync (`:2282-2290`), requiring `all` or `reports_view`; denial returns422. Its literal path is a separately mapped endpoint, not proof that all employee detail reads inherit its permission.
- **Counterexample:** general `/api/audit/events` (`:531-541`) checks `all`, `audit_view` or `control_center`. Its guard does not extend to the separate control-center audit wrapper. Do not label every audit read unguarded.

Same mechanism does not mean identical severity: private payroll lines and a limited attendance/status list need distinct impact/policy assessment. Parent should reconcile related instances into canonical scope without losing the original SEC-003 evidence.

## Limits and next validation

Only source and audit artifacts were read. No API request, login, session, employee record, database or physical printer was accessed. Existing server startup can migrate/seed, and database-backed fixtures can create databases, so neither was launched. Auth middleware still rejects invalid/revoked/expired/deactivated sessions; defaults such as loopback/private LAN reduce reachable population but do not implement field authorization.

Next approved isolated tests should exercise each route with a legitimate restricted user and synthetic HR values, assert denied fields/rows, then authorized HR/payroll/self-service and minimal picker workflows. Preserve anonymous401, unknown detail404 and the existing safe report/audit guards. Choose explicit server policies and minimal projections; do not implement a blanket reports_view grant without owner decision. No fix was applied.
