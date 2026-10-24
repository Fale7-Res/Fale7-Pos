# src/Falah.Server/Program.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. **STATIC_FILE_REVIEWED**. Reviewed every line 1-2305 and **188 route lambda bodies**, startup/middleware callbacks and helpers. No runtime/API writes.

Git blob SHA-256 `b8da9bce367055e5e5e2e606ae341a93cb686418f6c4441c15287a8c616a2e60`. Worktree SHA-256 `5b11d5b03d7010d890808029d561880a056c6ea922223b70567fc657b7261fc6`. LF-normalized equality: True. Original incomplete historical artifact preserved elsewhere.

## Startup and local functions

- Top-level service/DI registration1-196: actual Infrastructure backend; SQL fallback defaults Falah; CORS permits arbitrary origins; rate limit policy registered per IP.
- Startup try/catch199-237: unconditional schema migration, RCSI switch WITH ROLLBACK IMMEDIATE and seeder execute before clients; scheduled backup registered and enabled in config. Never launch against unknown DB. Logging filters target fields but exception object may contain external details.
- Exception middleware252-286: maps binding failures400 and unhandled500 with trace reference; registered after authentication, so authentication failures are outside wrapper.
- Login-rate options callback52-60: final host config wins; range validation absent here; invalid settings can break startup.
- ToUserSummary2256-2258: splits comma permissions, excludes credential fields.
- ResolveOrderNumbersAsync2270-2280: distinct batched order references, returns empty dictionary if none; avoids N+1, query limited by upstream selected rows, no shared transaction with first read.
- RequireReportsAccessAsync2282-2290: user exists and all/reports_view, returns422 for denial; relies middleware actor binding.
- RequireUsersManageAsync2295-2302: all/users_manage; same binding dependency.

## Cross-cutting results

Fully read 2305 lines in bounded chunks; all 188 route lambdas and top-level startup/middleware plus four explicit utility functions reasoned. Detailed endpoint matrix distinguishes route/body identity rewriting, actual helper permission calls, error mapping, read volume, cross-request snapshot limits and financial/printing side effects. No duplicate exact HTTP verb+route registration found. Security conclusions requiring exploit validation remain coordinator-owned.

Default appsettings enables automatic backup outside repository; startup DB mutation and printer tool actions were not executed. Most command validation is delegated; many sensitive GETs omit policy gates that analogous aggregate endpoints enforce. Parent security coordinator owns security severity/validation. Five account reads are sequential, so plural treasury consistency claim is unsupported by transaction code. Print success remains transport-only; checkout print durability gap remains after transaction commit. Payroll general status endpoint reaches Paid without payment (FIN-006).

## Actual endpoint body review

### POST `/api/shifts/open` — lines 288-310

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/shifts/current` — lines 311-319

Reads first Open row and opener name; 404 distinguishes none. Shared system-wide shift assumption requires single-open enforcement elsewhere; no per-user filtering.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/shifts/prepare-close` — lines 320-336

Read-only expected count preview returns sensitive drawer expectation; endpoint itself has no permission/actor parameter. Handler owns authorization; prepare must not be confused with committed close.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/shifts/close` — lines 337-357

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/business-day/open` — lines 358-375

Body DTO is forwarded without endpoint-side semantic validation. Administrative handler owns lifecycle permission, validation and persistence; no endpoint-specific policy metadata. IDs in body do not prove caller authority.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity. Runtime: NOT TESTED.

### GET `/api/business-day/current` — lines 376-383

Uses BusinessDayGate authoritative active selection then query detail; no active404. Separate reads can race close, acceptable transient missing but not atomic snapshot.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/business-days` — lines 384-386

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/business-day/{id}` — lines 387-392

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/business-day/{id}/close-blockers` — lines 393-398

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/business-day/{id}/close` — lines 399-420

Path ID wins. CloseBlocked returns422 with specific blockers; domain422/missing404. Snapshot/closure transaction belongs handler.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/business-day/{id}/close-snapshot` — lines 421-426

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/business-day/{id}/close-snapshots` — lines 427-429

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/business-day/{id}/reopen` — lines 430-447

Path IDs overwrite matching body IDs before handler call. Administrative handler owns lifecycle permission, validation and persistence; no endpoint-specific policy metadata. IDs in body do not prove caller authority.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/business-day/{id}/checklist` — lines 448-450

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/business-day/{id}/checklist/{itemId}` — lines 451-468

Path IDs overwrite matching body IDs before handler call. Administrative handler owns lifecycle permission, validation and persistence; no endpoint-specific policy metadata. IDs in body do not prove caller authority.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/settings` — lines 469-470

Authenticated direct read; no endpoint-specific helper permission gate found. Read DTO/query service contract and missing/error behavior reviewed; no write occurs in this lambda. Detailed permission/data-volume verification belongs downstream/security analysis.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### PUT `/api/settings` — lines 471-481

Body DTO is forwarded without endpoint-side semantic validation. Administrative handler owns lifecycle permission, validation and persistence; no endpoint-specific policy metadata. IDs in body do not prove caller authority.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/business-day/cash-count-denominations` — lines 482-484

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/business-day/{id}/cash-counts` — lines 485-487

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/business-day/{id}/cash-counts` — lines 488-499

Path IDs overwrite matching body IDs before handler call. Administrative handler owns lifecycle permission, validation and persistence; no endpoint-specific policy metadata. IDs in body do not prove caller authority.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/business-day/{id}/exceptions` — lines 500-505

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/control-exceptions/{exceptionId}/status` — lines 506-523

Path IDs overwrite matching body IDs before handler call. Administrative handler owns lifecycle permission, validation and persistence; no endpoint-specific policy metadata. IDs in body do not prove caller authority.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/control-center/audit-events` — lines 524-530

Authenticated control read without explicit control_center/reports helper at endpoint. Snapshot/cash-count/exceptions can carry financial state; query authorization not established by absence of writes. Requested entity and optional limit passed as bound input.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/audit/events` — lines 531-541

Explicit audit_view OR control_center gate, passes filters and default200 to service. Sensitive event old/new fields require query redaction; gate itself actor-bound by middleware.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/treasury/deposit` — lines 542-562

Body DTO is forwarded without endpoint-side semantic validation. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/treasury/withdraw` — lines 563-583

Body DTO is forwarded without endpoint-side semantic validation. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/treasury/transfer` — lines 584-604

Body DTO is forwarded without endpoint-side semantic validation. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/treasury/safe-drop` — lines 605-625

Body DTO is forwarded without endpoint-side semantic validation. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/expenses` — lines 626-647

Body DTO is forwarded without endpoint-side semantic validation. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/expenses` — lines 648-666

Authenticated direct read; no endpoint-specific helper permission gate found. Read DTO/query service contract and missing/error behavior reviewed; no write occurs in this lambda. Detailed permission/data-volume verification belongs downstream/security analysis.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/employees` — lines 667-673

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/employees/{employeeId}` — lines 674-684

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/employees` — lines 685-686

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/employees/{employeeId}` — lines 687-692

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/employees/{employeeId}/ledger` — lines 693-695

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/employees/{employeeId}/balance` — lines 696-701

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/employees/financial-report` — lines 702-708

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/employees/{employeeId}/ledger-entries` — lines 709-719

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/employees/{employeeId}/cash-out` — lines 720-735

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/employees/{employeeId}/pay` — lines 736-747

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/employees/{employeeId}/daily-wage` — lines 748-758

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/employees/{employeeId}/overtime` — lines 759-769

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/employees/{employeeId}/extra-shift` — lines 770-782

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/attendance/check-in` — lines 783-788

Body DTO is forwarded without endpoint-side semantic validation. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/attendance/check-out` — lines 789-795

Body DTO is forwarded without endpoint-side semantic validation. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/attendance/{recordId}/correct` — lines 796-806

Path IDs overwrite matching body IDs before handler call. Handler owns employee/attendance permissions, financial effect and replay. Endpoint cannot establish real biometric-device provenance.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/attendance` — lines 807-811

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/payroll-runs` — lines 812-818

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/payroll-runs` — lines 819-820

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/payroll-runs/{runId}` — lines 821-826

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/payroll-runs/{runId}/lines` — lines 827-829

Authenticated direct employee/payroll read with no endpoint-level employees_manage/reports gate (except separately reviewed financial-report). May carry pay/attendance/identity details. Handler/query body must be challenged for data visibility; no DB mutation in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/payroll-runs/{runId}/transition` — lines 830-840

Path run wins over body, forwards unrestricted status. FIN-006 Approved->Paid bypass confirmed: only employees_manage in handler for Paid and no financial postings. UI normal transition stops at Approved; API remains reachable.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/payroll-runs/{runId}/pay` — lines 841-854

Dedicated financial payroll handler exists; maps TreasuryConflict409. Existence does not close general transition bypass.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/backups` — lines 855-861

GET exposes backup record metadata including file paths to authenticated session; POST can create external disk backup via gated handler. Never invoke in audit; endpoint success is not restore proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/backups` — lines 862-863

GET exposes backup record metadata including file paths to authenticated session; POST can create external disk backup via gated handler. Never invoke in audit; endpoint success is not restore proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/backups/{id}` — lines 864-869

Authenticated direct read; no endpoint-specific helper permission gate found. Read DTO/query service contract and missing/error behavior reviewed; no write occurs in this lambda. Detailed permission/data-volume verification belongs downstream/security analysis.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/backups/{id}/verify` — lines 870-880

Path record wins; VERIFYONLY operation is DB/server work despite name verify. Audit never executes; handler authorization required.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/backups/{id}/restore` — lines 881-891

Path record wins; handler performs destructive restore with explicit confirmation/operational gates. Do not invoke; control plane permissions and transaction consequences require independent financial/security review.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/inventory/items` — lines 892-908

Body DTO is forwarded without endpoint-side semantic validation. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/inventory/items/{id}` — lines 909-926

Path IDs overwrite matching body IDs before handler call. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/inventory/items` — lines 927-929

No endpoint-specific reports/inventory gate; session authentication only at this layer. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/inventory/items/{id}/balance` — lines 930-933

No endpoint-specific reports/inventory gate; session authentication only at this layer. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### PUT `/api/inventory/items/{id}/estimated-cost` — lines 934-944

Path IDs overwrite matching body IDs before handler call. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/inventory/items/{id}/cost` — lines 945-947

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/inventory/movements` — lines 948-952

No endpoint-specific reports/inventory gate; session authentication only at this layer. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/inventory/overview` — lines 953-959

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/inventory/items/{id}/detail` — lines 960-967

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/inventory/loss-summary` — lines 968-970

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/inventory/items/{id}/variance` — lines 971-973

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/inventory/items/{id}/shared-consumption` — lines 974-981

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/inventory/count-adjustments` — lines 982-1002

Body DTO is forwarded without endpoint-side semantic validation. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/inventory/waste` — lines 1003-1023

Body DTO is forwarded without endpoint-side semantic validation. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/recipes` — lines 1024-1040

Body DTO is forwarded without endpoint-side semantic validation. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/recipes` — lines 1041-1042

No endpoint-specific reports/inventory gate; session authentication only at this layer. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/recipes/{id}` — lines 1043-1048

No endpoint-specific reports/inventory gate; session authentication only at this layer. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/recipes/{id}/cost` — lines 1049-1058

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/recipes/{id}/profitability` — lines 1059-1069

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/recipes/cost-summary` — lines 1070-1076

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/recipes/cost-coverage` — lines 1077-1083

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/recipes/{id}/cost-detail` — lines 1084-1091

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/prep-recipes` — lines 1092-1108

Body DTO is forwarded without endpoint-side semantic validation. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/prep-recipes` — lines 1109-1110

No endpoint-specific reports/inventory gate; session authentication only at this layer. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/prep-recipes/{id}/cost` — lines 1111-1118

Explicit reports gate present. Read service projects inventory/recipe facts; bounds, null cost and historical/current meaning must be enforced downstream. No mutation here; query errors may reach global500 if uncaught.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/order-packaging-rules` — lines 1119-1135

Body DTO is forwarded without endpoint-side semantic validation. Stock/recipe handler owns permissions, quantity/unit validation and transaction; no endpoint-level amount limits. Client-provided count deltas remain untrusted.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/order-packaging-rules` — lines 1136-1137

Authenticated direct read; no endpoint-specific helper permission gate found. Read DTO/query service contract and missing/error behavior reviewed; no write occurs in this lambda. Detailed permission/data-volume verification belongs downstream/security analysis.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/customers/upsert` — lines 1138-1152

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity. Runtime: NOT TESTED.

### GET `/api/customers/lookup` — lines 1153-1158

Authenticated customer lookup returns personal contact/address data without per-user permission/filter at endpoint. Phone lookup may be enumerated; null404 is expected. No upsert on GET.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/customers/{id}` — lines 1159-1164

Authenticated customer lookup returns personal contact/address data without per-user permission/filter at endpoint. Phone lookup may be enumerated; null404 is expected. No upsert on GET.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/drivers` — lines 1165-1181

Body DTO is forwarded without endpoint-side semantic validation. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/delivery/drivers` — lines 1182-1184

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/delivery/drivers/all` — lines 1185-1187

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/delivery/services` — lines 1188-1190

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/delivery/services/all` — lines 1191-1193

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/delivery/services` — lines 1194-1200

Body DTO is forwarded without endpoint-side semantic validation. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/delivery/services/{serviceId}` — lines 1201-1211

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/drivers/{driverId}/set-active` — lines 1212-1223

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/{id}/ready` — lines 1224-1241

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/{id}/assign` — lines 1242-1263

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/{id}/reassign` — lines 1264-1285

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/{id}/result` — lines 1286-1307

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/delivery/drivers/{driverId}/settle` — lines 1308-1331

Path IDs overwrite matching body IDs before handler call. Delivery handler owns state transition, custody/payment gates and replay key; response is operational state, not payment/physical print proof.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/delivery/board` — lines 1332-1334

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/delivery/ready` — lines 1335-1337

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/delivery/drivers/{driverId}/active` — lines 1338-1340

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/delivery/drivers/{driverId}/account` — lines 1341-1348

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/delivery/drivers/{driverId}/statistics` — lines 1349-1354

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/delivery/{id}` — lines 1355-1366

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/delivery/{id}/timeline` — lines 1367-1369

Authenticated direct delivery query; no reports_view/delivery_manage check in endpoint. Driver account/statistics expose financial/custody facts; dispatch reads expose customer address/phone. Read-only service call does not prove authorization. Parameter validation and list bounding delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/orders/checkout` — lines 1370-1394

Calls transactional checkout then best-effort automatic print outside transaction. Correctly surfaces PrintJobsQueued false but no durable postcommit outbox; process cut can lose dispatch until manual repair. FLOW-001 pickup lifecycle and FIN-001 split confirmation remain.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity. Runtime: NOT TESTED.

### POST `/api/orders/refund` — lines 1395-1415

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/orders/confirm-electronic-payment` — lines 1416-1436

Only confirms payment handler. No held preparation release, FLOW-002. HTTP maps confirm conflict409/domain422/missing404.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/orders/cancel-pending-electronic-payment` — lines 1437-1455

Cancels through existing handler; does not itself post money reversal. FIN-002 mixed-paid-leg cancellation independently corroborated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/approvals/issue` — lines 1456-1479

PIN-taking authenticated approval endpoint is NOT decorated RequireRateLimiting despite nearby claim. Handler checks all active hashes; no own limiter found. Action scope handled downstream; do not expose PIN in logs.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/orders` — lines 1480-1497

Explicit orders_view_all policy else currently Open shift; includes all items and unbounded ordered list. Direct detail/lookup siblings do not repeat this visibility policy.

Endpoint gate indicators: HasPermission. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/orders/{id}` — lines 1498-1505

Direct by-ID read returns frozen order/customer/payment details to any authenticated session; no orders_view_all/current-shift check here. Read-only does not mean visibility-authorized.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/orders/lookup` — lines 1506-1511

Lookup by human order number, no list visibility filter; static literal route takes precedence over generic id. Numbers are enumerable; security coordinator owns disclosure validation.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/menu` — lines 1512-1517

Catalog read forwards service data; all/retired management visibility not separately gated at endpoint. Checkout still resolves authoritative price server-side. No write here; errors/volume delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/products/{id}/image` — lines 1518-1523

GET returns stored bytes/content type with no explicit version or cache headers despite version-segment comment. PUT path product wins and DELETE uses actor query; handler validation owns size/media type. DTO binding plus auth buffering doubles base64 parse work.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### PUT `/api/products/{id}/image` — lines 1524-1533

GET returns stored bytes/content type with no explicit version or cache headers despite version-segment comment. PUT path product wins and DELETE uses actor query; handler validation owns size/media type. DTO binding plus auth buffering doubles base64 parse work.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### DELETE `/api/products/{id}/image` — lines 1534-1547

GET returns stored bytes/content type with no explicit version or cache headers despite version-segment comment. PUT path product wins and DELETE uses actor query; handler validation owns size/media type. DTO binding plus auth buffering doubles base64 parse work.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/products/{id}/preparation-station` — lines 1548-1566

Body DTO is forwarded without endpoint-side semantic validation. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/categories/all` — lines 1567-1568

Catalog read forwards service data; all/retired management visibility not separately gated at endpoint. Checkout still resolves authoritative price server-side. No write here; errors/volume delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/categories` — lines 1569-1579

Body DTO is forwarded without endpoint-side semantic validation. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/categories/{id}` — lines 1580-1590

Path IDs overwrite matching body IDs before handler call. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/categories/{id}/active` — lines 1591-1601

Path IDs overwrite matching body IDs before handler call. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/products/all` — lines 1602-1603

Catalog read forwards service data; all/retired management visibility not separately gated at endpoint. Checkout still resolves authoritative price server-side. No write here; errors/volume delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/products/{id}` — lines 1604-1609

Catalog read forwards service data; all/retired management visibility not separately gated at endpoint. Checkout still resolves authoritative price server-side. No write here; errors/volume delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/products` — lines 1610-1620

Body DTO is forwarded without endpoint-side semantic validation. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/products/{id}` — lines 1621-1631

Path IDs overwrite matching body IDs before handler call. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/products/{id}/availability` — lines 1632-1644

Path IDs overwrite matching body IDs before handler call. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/tax-rules` — lines 1645-1646

Catalog read forwards service data; all/retired management visibility not separately gated at endpoint. Checkout still resolves authoritative price server-side. No write here; errors/volume delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/tax-rules` — lines 1647-1657

Body DTO is forwarded without endpoint-side semantic validation. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### PUT `/api/tax-rules/{id}` — lines 1658-1672

Path IDs overwrite matching body IDs before handler call. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/cash-drawer/open` — lines 1673-1683

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/cash-drawer/report-result` — lines 1684-1694

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/cash-drawer/cash-movement` — lines 1695-1708

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/price-rules` — lines 1709-1710

Catalog read forwards service data; all/retired management visibility not separately gated at endpoint. Checkout still resolves authoritative price server-side. No write here; errors/volume delegated.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/price-rules` — lines 1711-1723

Body DTO is forwarded without endpoint-side semantic validation. Handler owns permission and valid catalog replacement/soft activation; historical snapshots must remain isolated. No separate local transaction in endpoint.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/treasury/balance` — lines 1724-1739

Checks fixed account catalog and returns404 for unknown, but omits reports_view check required by plural balances. Security coordinator owns read-gate gap.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/treasury/balances` — lines 1740-1762

Reports gate exists, but five sequential ledger queries are NOT one atomic view. Concurrent transfer between account reads can temporarily create impossible aggregate; no snapshot transaction despite comment promising consistent picture.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/treasury/position` — lines 1763-1805

Same five sequential account queries followed by pending electronic/custody reads. No common read snapshot; current exposure values can change mid-request. Pending status literal case depends SQL collation.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/treasury/movements` — lines 1806-1827

Reports gate, optional account/day filters, max500 enforced with default100, batched order labels avoids N+1. Date is created-at Cairo window, not stored BusinessDayId; label query is separate snapshot.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/treasury/handovers` — lines 1828-1837

No reports gate in endpoint; positive limit has no upper cap. Reads financial amounts and identities. Any authenticated session can reach handler direct query.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/users` — lines 1838-1852

Active user summaries exclude PIN but expose role/permissions to any authenticated session. Comment calls this login picker although middleware blocks pre-login access. UI actual use requires peer crosscheck.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/auth/login-by-pin` — lines 1853-1874

Anonymous PIN login verifies against all active salted hashes, failed login writes audit event, success issues session. Fixed-window IP limiter applies. This is NOT read-only even for diagnostic script.

Endpoint gate indicators: RequireRateLimiting. Local explicit error shapes: Unauthorized. Runtime: NOT TESTED.

### POST `/api/auth/logout` — lines 1875-1884

Reads actual bearer token and revokes session. Authentication middleware already resolves it; never trusts supplied actor.

Endpoint gate indicators: RequireUsersManageAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/users/all` — lines 1885-1887

Explicit users_manage read gate using actor query matched by middleware, then read-only list.

Endpoint gate indicators: RequireUsersManageAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/users/{id}` — lines 1888-1895

Explicit users_manage gate then null404; privilege verification relies middleware actor consistency.

Endpoint gate indicators: RequireUsersManageAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/permissions/catalog` — lines 1896-1897

Read-only catalog exposes permission definitions to authenticated session. No permission gate; not equivalent to ability to grant them.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/users` — lines 1898-1914

Active user summaries exclude PIN but expose role/permissions to any authenticated session. Comment calls this login picker although middleware blocks pre-login access. UI actual use requires peer crosscheck.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/users/{id}/active` — lines 1915-1932

Path IDs overwrite matching body IDs before handler call. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/users/{id}/change-pin` — lines 1933-1950

Path user ID overwrites body. Handler distinguishes self/admin and owner rules; endpoint uses login rate limiter. PIN must stay out of retained logs/reports.

Endpoint gate indicators: RequireRateLimiting. Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/users/{id}/permissions` — lines 1951-1975

Path IDs overwrite matching body IDs before handler call. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/reports/owner-dashboard` — lines 1976-1989

Explicit reports gate, resolves from/to/businessDate else Cairo today. Composes multiple queries; absence of snapshot can show moving figures under concurrent writes.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/daily-statement` — lines 1990-1996

Explicit reports gate, single Cairo business date. Current balances/health versus historical day figures intentionally mixed by DTO; UI must preserve labels.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/sales` — lines 1997-2000

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/tax` — lines 2001-2004

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/expenses` — lines 2005-2008

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/refunds` — lines 2009-2012

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/business-day/{id}` — lines 2013-2020

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/reports/daily-summary/{id}` — lines 2021-2028

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/reports/top-items` — lines 2029-2031

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/category-sales` — lines 2032-2034

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/product-hourly` — lines 2035-2037

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/food-cost` — lines 2038-2040

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/significant-actions` — lines 2041-2047

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/shifts` — lines 2048-2050

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/reports/shifts/{id}` — lines 2051-2058

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/reports/delivery-outcomes` — lines 2059-2067

Explicit reports_view helper gate precedes query. Date/shift arguments are optional shape-bound values; underlying query owns valid range and historical/current semantics. No common read transaction at endpoint; large report range can cost memory/SQL. Missing-entity errors locally mapped only where shown.

Endpoint gate indicators: RequireReportsAccessAsync. Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/print-jobs/reprint-document` — lines 2068-2089

Reproduction of shift/expense document, not command replay. Handler owns kind/authorization; must not post money or stock.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/orders/{id}/print-jobs` — lines 2090-2111

Path order wins, arbitrary IncludeKitchenRoutes from caller, IsReprint does not suppress kitchen. UI CallCenter reprint omits flag false and queues preparation again; peer UI canonical finding.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/orders/{id}/print-jobs` — lines 2112-2114

Path order wins, arbitrary IncludeKitchenRoutes from caller, IsReprint does not suppress kitchen. UI CallCenter reprint omits flag false and queues preparation again; peer UI canonical finding.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/print-jobs/{id}/report-result` — lines 2115-2134

Path job wins over body; handler records boolean report with attempt key, no physical proof or station binding. Domain errors not caught locally (global500); conflict409/missing404.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, NotFound. Runtime: NOT TESTED.

### GET `/api/print-jobs/overview` — lines 2135-2137

No printing-specific permission check; service returns six routes/needs-attention. It reports transport outcome, not physical completion (PRT-002).

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/print-jobs/pending` — lines 2138-2140

Authenticated read-only unclaimed snapshot. No station credential/lease/claim; PRT-001 duplicate machine dispatch. Route values not validated here; empty list can mask typo.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/print-jobs/{id}` — lines 2141-2146

Authenticated direct read; no endpoint-specific helper permission gate found. Read DTO/query service contract and missing/error behavior reviewed; no write occurs in this lambda. Detailed permission/data-volume verification belongs downstream/security analysis.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/print-jobs/{id}/ticket` — lines 2147-2152

Read-only composition, no dispatch or attempt write. Content reconstructs live settings/payment state with frozen items; suitable starting point for future read-only preview, not archived byte identity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### POST `/api/suppliers` — lines 2153-2169

Body DTO is forwarded without endpoint-side semantic validation. Handler owns authorization, state and transactional/idempotency invariants. Endpoint success alone does not prove downstream hardware or durable financial consistency.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/suppliers` — lines 2170-2172

Authenticated purchasing query without endpoint purchasing_manage/reports gate; ledger and invoice debts exposed from service. No pagination/maximum range declared here. Read-only does not prove low sensitivity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/suppliers/{id}` — lines 2173-2178

Authenticated purchasing query without endpoint purchasing_manage/reports gate; ledger and invoice debts exposed from service. No pagination/maximum range declared here. Read-only does not prove low sensitivity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### GET `/api/suppliers/{id}/ledger` — lines 2179-2181

Authenticated purchasing query without endpoint purchasing_manage/reports gate; ledger and invoice debts exposed from service. No pagination/maximum range declared here. Read-only does not prove low sensitivity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### POST `/api/purchases` — lines 2182-2202

Body DTO is forwarded without endpoint-side semantic validation. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/api/purchases` — lines 2203-2206

Authenticated purchasing query without endpoint purchasing_manage/reports gate; ledger and invoice debts exposed from service. No pagination/maximum range declared here. Read-only does not prove low sensitivity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/purchases/payables-aging` — lines 2207-2209

Authenticated purchasing query without endpoint purchasing_manage/reports gate; ledger and invoice debts exposed from service. No pagination/maximum range declared here. Read-only does not prove low sensitivity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: no local error translation; global handler/framework applies. Runtime: NOT TESTED.

### GET `/api/purchases/{id}` — lines 2210-2215

Authenticated purchasing query without endpoint purchasing_manage/reports gate; ledger and invoice debts exposed from service. No pagination/maximum range declared here. Read-only does not prove low sensitivity.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: NotFound. Runtime: NOT TESTED.

### PUT `/api/purchases/{id}/due-date` — lines 2216-2225

Path IDs overwrite matching body IDs before handler call. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### POST `/api/purchases/{id}/payments` — lines 2226-2250

Path IDs overwrite matching body IDs before handler call. Handler owns protected source account lock, positive amount and journal idempotency. Endpoint only binds/marshals request; inspect independent financial findings.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: Conflict, UnprocessableEntity, NotFound. Runtime: NOT TESTED.

### GET `/health` — lines 2251-2253

CanConnectAsync verifies SQL availability only; does not verify expected database identity/schema, seed state, authenticated API, agent health or physical printer. Database exception may become 500 rather than 503. Anonymous by middleware.

Endpoint gate indicators: none (global authentication may apply). Local explicit error shapes: StatusCode(503). Runtime: NOT TESTED.

## Validation and remaining limits

Static route uniqueness checked by method/path; no duplicate exact registrations. Literal/generic siblings were checked as separate contracts, not claimed inaccessible by registration order. Records, date/time/numeric binding are not semantic validation. No tests added or run by this reviewer; a future isolated API matrix should verify unauthenticated401, correct/incorrect actor, missing400, denied422/403, not-found404, replay/conflict409 and actual successful domain result for every command, plus explicit protected read permissions, bounded list limits, complete transaction rollback and snapshot consistency. No existing database, source, service or printer modified.
