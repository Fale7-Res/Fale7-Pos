# File Review: tests/Falah.Client.Tests/FinalVerificationTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/FinalVerificationTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:13:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:13:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Test transport mock delegating outgoing HTTP requests directly to the test-provided `send` callback, facilitating in-memory request capturing and status code simulation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [16-17]
- **Behavior:** Test helper constructing an `HttpResponseMessage` containing UTF-8 JSON content with the specified status code (default `HttpStatusCode.OK`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:19:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:19:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Http(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [19-20]
- **Behavior:** Test helper constructing an `HttpClient` instance wrapping the custom in-memory `Handler` configured with base address `http://fixture.invalid`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:22:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:22:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [22-23]
- **Behavior:** Test setup factory creating an `AppSession` backed by an `ApiClient` with `CurrentUser` set as manager `actor` holding `all` permissions.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:25:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:25:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.ReadFixture(string)
- **Lines:** [25-38]
- **Behavior:** Fixture router returning canned JSON responses across all domain query endpoints (/api/menu, /api/business-day/current, /api/reports/*, /api/treasury/position, /api/inventory/overview, /api/recipes/cost-coverage).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:40:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:40:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Secondary_read_failure_is_visible_and_retry_recovers(string, string)
- **Lines:** [40-72]
- **Behavior:** Theory verifying graceful degradation and retry recovery when secondary read endpoints fail across 7 management view models.
  - Scenario: Partial read failure: secondary data endpoints return 503 ServiceUnavailable while primary endpoints succeed.
  - Production code exercised: `MenuViewModel`, `PurchasingViewModel`, `TreasuryViewModel`, `UsersViewModel`, `RecipesViewModel`, `InventoryViewModel`, `DeliveryViewModel`.
  - Assertions: Verifies `hitFailure == true` (ensuring secondary endpoint was called); `LoadError` is populated and visible to the user without crashing the UI; clearing failure and reloading clears `LoadError`.
  - Gaps: Does not test offline caching where stale secondary data is shown with an indicator.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:55:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:55:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [55-59]
- **Behavior:** HTTP mock interceptor failing designated `failedPath` with 503 ServiceUnavailable while returning canned fixture JSON for other paths.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:65:9:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:65:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Load()
- **Lines:** [65-65]
- **Behavior:** Local asynchronous helper invoking `LoadAsync` via reflection across diverse view model types.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:74:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:74:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Missing_blocker_response_never_means_business_day_can_close()
- **Lines:** [74-90]
- **Behavior:** Asserts safety-critical business day close invariant when the close blocker endpoint fails.
  - Scenario: Error path & fail-safe invariant: `/api/business-day/current/close-blockers` query returns 503 ServiceUnavailable.
  - Production code exercised: `ControlCenterViewModel.LoadAsync`, `ControlCenterViewModel.CanClose`, `ControlCenterViewModel.LoadError`.
  - Assertions: Verifies `LoadError` is displayed and `CanClose` is strictly `false`; recovering blocker endpoint allows `CanClose == true`. Proves missing blocker response is never interpreted as 'no blockers exist'.
  - Gaps: Does not test network timeout during the actual CloseBusinessDayCommand write.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:78:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:78:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [78-80]
- **Behavior:** HTTP mock interceptor returning 503 ServiceUnavailable for close-blockers queries and fixture data for others.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:92:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:92:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Failed_expenses_are_not_presented_as_a_zero_expense_report()
- **Lines:** [92-101]
- **Behavior:** Asserts financial reporting integrity when expense queries fail.
  - Scenario: Error path: `/api/reports/expenses` returns 503 ServiceUnavailable during report loading.
  - Production code exercised: `ReportsViewModel.LoadAsync`, `ReportsViewModel.LoadError`, `ReportsViewModel.HasData`.
  - Assertions: Verifies `LoadError` is populated and `HasData` is `false`, ensuring failed expense queries are never presented as a 0-expense report (preventing false profitability reports).
  - Gaps: Does not test partial sales/expense report rendering where available data is shown with a warning.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:95:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:95:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [95-97]
- **Behavior:** HTTP mock interceptor returning 503 ServiceUnavailable specifically for expense report requests.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:106:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:106:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Stepping_a_day_switches_the_screen_onto_that_day()
- **Lines:** [106-129]
- **Behavior:** Asserts date stepping synchronization between header display and query date range in `ReportsViewModel`.
  - Scenario: User date stepping interaction.
  - Production code exercised: `ReportsViewModel.PreviousDayCommand`, `ReportsViewModel.BusinessDate`, `ReportsViewModel.IsSpecificDay`.
  - Assertions: Initial view opens on 'today' (`IsSpecificDay == false`); clicking `PreviousDayCommand` steps `BusinessDate` by -1 day, activates `IsSpecificDay == true`, and verifies all outgoing report requests contain `today.AddDays(-1)` in query strings.
  - Gaps: Does not test stepping past maximum historical archive dates.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:110:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:110:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [110-113]
- **Behavior:** HTTP mock interceptor recording request URIs and returning canned fixture JSON for reports.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:119:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:119:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [119-119]
- **Behavior:** Inline assertion predicate `r => Assert.Contains(today.ToString("yyyy-MM-dd"), r.Query)` validating query date string.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:128:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:128:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [128-128]
- **Behavior:** Inline assertion predicate `r => Assert.Contains(today.AddDays(-1).ToString("yyyy-MM-dd"), r.Query)` validating query date string.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:132:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:132:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Choosing_a_week_asks_every_report_for_the_whole_span()
- **Lines:** [132-150]
- **Behavior:** Asserts multi-day period queries request full date spans across all reports in `ReportsViewModel`.
  - Scenario: Multi-day period selection (last 7 days).
  - Production code exercised: `ReportsViewModel.SelectedPeriod`, `ReportsViewModel.Periods`.
  - Assertions: Selecting `last7` period generates query strings with explicit `from=yyyy-MM-dd` and `to=yyyy-MM-dd` spanning the entire 7-day range rather than querying a single day twice.
  - Gaps: Does not test custom date range picker inputs.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:136:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:136:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [136-139]
- **Behavior:** HTTP mock interceptor recording URIs for period query evaluation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:143:46:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:143:46:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [143-143]
- **Behavior:** Inline expression lambda 'vm.SelectedPeriod = vm.Periods.First(p => p.Key == "last7");'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:148:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:148:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [148-148]
- **Behavior:** Inline assertion predicate validating `from` query parameter date formatting.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:149:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:149:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [149-149]
- **Behavior:** Inline expression lambda 'Assert.All(requests, r => Assert.Contains($"to={today:yyyy-MM-dd}", r.Query));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:154:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:154:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.WaitForIdleAsync(Falah.Client.CashierWinUI.ViewModels.ReportsViewModel)
- **Lines:** [154-157]
- **Behavior:** Asynchronous test helper polling `ReportsViewModel` until `IsLoading == false` and data is populated, replacing fixed sleeps with deterministic polling.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:159:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:159:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Changing_report_date_during_a_read_reloads_a_consistent_new_date()
- **Lines:** [159-184]
- **Behavior:** Asserts race condition resilience in `ReportsViewModel` when date selection changes while a multi-endpoint read is in flight.
  - Scenario: User navigation concurrency during 10-endpoint report load.
  - Production code exercised: `ReportsViewModel.LoadAsync`, `ReportsViewModel.NextDayCommand`, `ReportsViewModel.BusinessDate`.
  - Assertions: Pauses initial read via `TaskCompletionSource`; user clicks `NextDayCommand`; when initial read releases, view model automatically initiates a full reload for the new date (20 total HTTP calls); requests 1-10 target initial date, requests 11-20 target new date; `LoadError == null` and `HasData == true`.
  - Gaps: Does not test cancellation of in-flight HTTP requests via CancellationToken.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:165:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:165:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [165-169]
- **Behavior:** HTTP mock coordinating multi-endpoint report loading via `TaskCompletionSource`.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:182:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:182:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [182-182]
- **Behavior:** Assertion predicate verifying first 10 requests query initial report date.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:183:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:183:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [183-183]
- **Behavior:** Assertion predicate verifying subsequent 10 requests query updated report date.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Printing_empty_state_is_notified_after_loading_finishes()
- **Lines:** [186-200]
- **Behavior:** Asserts that `PrintingViewModel` correctly notifies property changed for empty state after loading completes.
  - Scenario: Printing overview load completion notification.
  - Production code exercised: `PrintingViewModel.LoadAsync`, `PrintingViewModel.HasNoPending`.
  - Assertions: Subscribes to `PropertyChanged`; verifies `HasNoPending` notification is raised and evaluates to `true` when overview response contains 0 pending jobs.
  - Gaps: Does not test UI update notification when a job transitions from pending to failed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:191:48:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:191:48:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [191-194]
- **Behavior:** HTTP mock returning overview JSON with 0 pending, stuck, or failed print jobs.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:196:31:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:196:31:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [196-196]
- **Behavior:** PropertyChanged event handler lambda tracking whether `HasNoPending` notification fired.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:202:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:202:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Payroll_does_not_show_a_failed_line_read_as_zero_payable()
- **Lines:** [202-213]
- **Behavior:** Asserts that failed payroll line reads display placeholder dashes rather than false zero payable amounts.
  - Scenario: Partial failure: payroll run lines endpoint returns 503 ServiceUnavailable.
  - Production code exercised: `PayrollViewModel.SelectedRun`, `PayrollViewModel.SelectedRunTotalDisplay`, `PayrollViewModel.LoadError`.
  - Assertions: Verifies `LoadError` is populated and `SelectedRunTotalDisplay == "—"` (preventing accounting confusion between a failed read and a zero-payable payroll run).
  - Gaps: Does not test manual retry command on failed payroll lines.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:205:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:205:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [205-206]
- **Behavior:** HTTP mock returning 503 ServiceUnavailable for payroll line requests.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:215:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:215:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Payroll_approval_permission_alone_does_not_bypass_the_management_gate()
- **Lines:** [215-225]
- **Behavior:** Asserts role-based authorization gate preventing unprivileged approval of draft payroll runs.
  - Scenario: Negative authorization test. User holds `payroll_approve` permission but lacks management permissions.
  - Production code exercised: `PayrollViewModel.CanAdvance`, `PayrollViewModel.SelectedRun`.
  - Assertions: `CanAdvance` is `false`, verifying approval permission alone cannot advance draft runs without proper role authorization.
  - Gaps: Does not test server-side 403 Forbidden enforcement.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:218:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:218:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [218-218]
- **Behavior:** Inline expression lambda 'var session = Session(_ => Task.FromResult(Json("[]")));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:227:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:227:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Control_center_view_permission_cannot_change_the_close_checklist()
- **Lines:** [227-244]
- **Behavior:** Asserts authorization defense preventing view-only users from altering business day close checklists.
  - Scenario: Negative security test. User possesses view-only `control_center` permission.
  - Production code exercised: `ControlCenterViewModel.CanControl`, `ControlCenterViewModel.CanClose`, `ControlCenterViewModel.ToggleChecklistItemCommand`.
  - Assertions: `CanControl` and `CanClose` are `false`; executing `ToggleChecklistItemCommand` dispatches zero HTTP mutations (`writes == 0`).
  - Gaps: Does not test server-side rejection if client command execution is bypassed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:231:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:231:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [231-234]
- **Behavior:** HTTP spy tracking mutation requests and returning canned fixture JSON.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:246:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:246:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Treasury_retries_original_payload_and_blocks_abandonment_after_an_unknown_outcome(string)
- **Lines:** [246-282]
- **Behavior:** Theory asserting treasury mutation write idempotency and modal locking across deposit, withdraw, and transfer actions.
  - Scenario: Error path & recovery: treasury write experiences network timeout (`HttpRequestException`), followed by 422 rejection, then success.
  - Production code exercised: `TreasuryViewModel.OpenTreasuryActionCommand`, `TreasuryViewModel.SubmitTreasuryActionCommand`, `TreasuryViewModel.CloseTreasuryActionCommand`, `AppSession.OperationStateChanged`.
  - Assertions: Connection drop sets `HasUnresolvedOperation == true`, `CanEditAction == false`; closing modal or switching actions is blocked (`OpenAction` remains active); input tampering is ignored on resubmit; all 3 payloads sent are identical byte-for-byte; clears unresolved state upon success; fires `OperationStateChanged` notifications.
  - Gaps: Does not test currency rounding for fractional piastre entries.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:254:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:254:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [254-260]
- **Behavior:** HTTP mock capturing treasury request bodies, throwing on call 1, returning 422 on call 2, and returning replay confirmation on call 3.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:261:42:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:261:42:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [261-261]
- **Behavior:** Inline expression lambda 'session.OperationStateChanged += () => notifications++;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:276:28:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:276:28:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [276-276]
- **Behavior:** Inline expression lambda 'Assert.All(bodies, b => Assert.Equal(bodies[0], b));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:284:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:284:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Treasury_definite_rejection_allows_correction_and_releases_the_session()
- **Lines:** [284-297]
- **Behavior:** Asserts that definite business rejections in treasury operations allow input correction and release the session.
  - Scenario: Definite rejection error path (422 UnprocessableEntity).
  - Production code exercised: `TreasuryViewModel.SubmitTreasuryActionCommand`, `TreasuryViewModel.CloseTreasuryActionCommand`, `TreasuryViewModel.CanEditAction`.
  - Assertions: Verifies `CanEditAction == true`, `HasUnresolvedOperation == false`, and `ActionError` is populated; user can close modal or correct inputs.
  - Gaps: Does not test specific domain error code mappings.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:287:33:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:287:33:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [287-287]
- **Behavior:** HTTP mock returning 422 UnprocessableEntity with error `"invalid"`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:299:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:299:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_recovers_each_operators_own_draft_after_switching_back()
- **Lines:** [299-315]
- **Behavior:** Asserts call center cart draft isolation per operator.
  - Scenario: Fast operator switching in CallCenterWinUI.
  - Production code exercised: `CallCenterWinUI.Services.AppSession.GetOrderDesk`.
  - Assertions: Switching operator to 'second' yields an empty cart; switching back to 'first' restores original cart items and customer name.
  - Gaps: Does not test session memory bounds when many operators switch.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:302:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:302:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [302-302]
- **Behavior:** HTTP mock returning empty array for call center setup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:317:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:317:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Electronic_payment_decision_cannot_switch_or_be_abandoned_after_response_loss(bool)
- **Lines:** [317-353]
- **Behavior:** Theory verifying electronic payment decision locking during network response loss.
  - Scenario: Error path & recovery for electronic payment confirmation vs cancellation (`confirm` parameterized as `true`/`false`).
  - Production code exercised: `CashierViewModel.ConfirmElectronicPaymentCommand`, `CashierViewModel.CancelPendingElectronicPaymentCommand`, `CashierViewModel.StartNewOrderCommand`, `CashierViewModel.DismissCheckoutResultCommand`.
  - Assertions: Network loss sets `HasUnresolvedOperation == true` and `CanEditOperation == false`; executing the opposite command is ignored (`requests.Count == 1`); dismissing checkout result or starting new order is locked out; modifying cancel reason is ignored; retry sends identical payload across all 3 attempts; clears lock upon confirmation.
  - Gaps: Does not test simultaneous webhook confirmation arriving from payment provider while client retries.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:323:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:323:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [323-329]
- **Behavior:** HTTP mock recording electronic payment mutation requests, throwing on call 1, returning 422 on call 2, and returning confirmed status on call 3.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:350:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:350:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [350-350]
- **Behavior:** Inline expression lambda 'Assert.All(requests, r => Assert.Equal(requests[0], r));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:355:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:355:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_reprint_retries_the_original_request_and_prevents_abandonment()
- **Lines:** [355-385]
- **Behavior:** Asserts call center order reprint retry idempotency and dialog lock safety.
  - Scenario: Error path & recovery during call center reprint request.
  - Production code exercised: `OrderDeskViewModel.ReprintLookedUpOrderCommand`, `OrderDeskViewModel.RetryPrintCommand`, `OrderDeskViewModel.CloseOrderLookupCommand`.
  - Assertions: Lost response sets `HasPendingPrint == true`, `CanEditOperation == false`, `HasUnresolvedOperation == true`; closing lookup modal is blocked; retrying dispatches identical requests across all 3 attempts (`bodies[0] == bodies[1] == bodies[2]`); unlocks UI upon successful replay.
  - Gaps: Does not test printer queue saturation responses.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:359:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:359:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [359-364]
- **Behavior:** HTTP mock capturing reprint requests, throwing on call 1, returning 403 on call 2, and returning replay confirmation on call 3.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:381:28:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:381:28:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [381-381]
- **Behavior:** Inline expression lambda 'Assert.All(bodies, b => Assert.Equal(bodies[0], b));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:387:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:387:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_keeps_unresolved_request_after_a_later_rejection()
- **Lines:** [387-424]
- **Behavior:** Asserts that call center checkout retains unresolved requests even after a subsequent shift-closed rejection.
  - Scenario: Error path: checkout response lost, retry returns 422 'shift closed'.
  - Production code exercised: `OrderDeskViewModel.SubmitOrderCommand`, `MainViewModel.LockCommand`, `MainViewModel.TryClose`.
  - Assertions: Request remains unresolved; station locking (`LockCommand`) and window closing (`TryClose`) are blocked (`IsCloseBlocked == true`); cart cannot be cleared; retrying resubmits exact original payload (`bodies[0] == bodies[1]`).
  - Gaps: Does not test shift re-opening or emergency supervisor manager override.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:391:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:391:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [391-396]
- **Behavior:** HTTP mock recording checkout requests, throwing on call 1, and returning 422 'shift closed' on retry.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:426:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:426:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_lock_waits_for_an_in_flight_operation_then_returns_to_login()
- **Lines:** [426-447]
- **Behavior:** Asserts call center station lock waits for in-flight checkout operations before logging out.
  - Scenario: User initiates terminal lock while an asynchronous checkout is processing.
  - Production code exercised: `MainViewModel.LockCommand`, `MainViewModel.TryClose`, `OrderDeskViewModel.IsProcessingCheckout`.
  - Assertions: While `IsProcessingCheckout == true`, `TryClose()` returns `false` and `LockCommand` is blocked (remains on `CurrentView = desk`, `IsLoggedIn == true`); once checkout completes, `TryClose()` succeeds and `LockCommand` navigates to `LoginViewModel` (`IsLoggedIn == false`).
  - Gaps: Does not test automatic inactivity screen lock timeout.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:429:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:429:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [429-429]
- **Behavior:** HTTP mock returning empty JSON for call center lock setup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:449:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:449:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_no_longer_requests_its_own_tickets_after_checkout()
- **Lines:** [449-498]
- **Behavior:** Asserts architectural invariant: call center client no longer dispatches separate print requests after checkout.
  - Scenario: Call center checkout auto-print architecture verification.
  - Production code exercised: `OrderDeskViewModel.SubmitOrderCommand`, `OrderDeskViewModel.HasCheckoutResult`, `OrderDeskViewModel.HasPendingPrint`.
  - Assertions: Tickets are queued server-side inside the checkout transaction (`OrderAutoPrintService`); client makes exactly 1 checkout call and 0 print calls (`Assert.Empty(prints)`); `HasPendingPrint == false`, `HasPrintWarning == false`, and cart is cleared, allowing operator to start a new order immediately.
  - Gaps: Does not test order cancellation before tickets are printed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:462:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:462:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [462-476]
- **Behavior:** HTTP mock intercepting checkout and print requests; returns `printJobsQueued: true` on checkout.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:500:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:500:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_says_so_when_the_server_could_not_queue_the_tickets()
- **Lines:** [500-527]
- **Behavior:** Asserts operator warning visibility when server indicates tickets could not be queued during checkout.
  - Scenario: Partial failure: checkout succeeds but server reports `printJobsQueued: false`.
  - Production code exercised: `OrderDeskViewModel.SubmitOrderCommand`, `OrderDeskViewModel.HasPrintWarning`, `OrderDeskViewModel.PrintWarningText`.
  - Assertions: Order completes successfully (`HasCheckoutResult == true`) without blocking the sale, but `HasPrintWarning == true` and `PrintWarningText` prompts operator to manually reprint.
  - Gaps: Does not test acoustic/visual alarm cues for failed print jobs.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:506:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:506:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [506-511]
- **Behavior:** HTTP mock returning `printJobsQueued: false` on checkout.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:529:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:529:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_estimate_uses_the_existing_tax_rules(string, long)
- **Lines:** [529-544]
- **Behavior:** Theory verifying call center tax calculation across Exclusive and Inclusive tax modes.
  - Scenario: Tax calculation modes in call center order desk: Exclusive (14% on 10001 -> 11401 piastres) and Inclusive (14% on 10001 -> 10001 piastres).
  - Production code exercised: `OrderDeskViewModel.RetryLoadMenuCommand`, `OrderDeskViewModel.GrandTotalPiastres`.
  - Assertions: Verifies `GrandTotalPiastres` matches domain calculations for each tax mode.
  - Gaps: Does not test compound taxes or tax exemption per customer.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:534:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:534:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [534-536]
- **Behavior:** HTTP mock returning empty menu and parameterized tax rule configuration.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
