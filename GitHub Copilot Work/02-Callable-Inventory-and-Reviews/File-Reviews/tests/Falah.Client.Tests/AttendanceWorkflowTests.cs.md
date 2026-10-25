# File Review: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/AttendanceWorkflowTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:14:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:14:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [14-14]
- **Behavior:** Test transport mock delegating HTTP request execution directly to the test-provided `send` delegate callback, enabling payload inspection, response mocking, and error simulation without socket I/O.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Json(object, System.Net.HttpStatusCode)
- **Lines:** [16-16]
- **Behavior:** Test helper factory instantiating an `HttpResponseMessage` containing serialized JSON content and the designated HTTP status code (defaults to `HttpStatusCode.OK`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:17:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:17:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [17-19]
- **Behavior:** Test setup factory constructing an `AppSession` backed by an `ApiClient` configured with base address `http://fixture.invalid`. Seeds `CurrentUser` as manager (`actor`, permissions `["attendance_manage", "employees_manage"]`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:20:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:20:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Record(string)
- **Lines:** [20-21]
- **Behavior:** Test helper constructing a sample `AttendanceRecordDto` with ID, employee details, shift `صباحية`, status `Present`, 8 standard work hours, and biometric verification.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:23:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:23:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Attendance_retry_keeps_the_original_record_values_and_reason_until_confirmed()
- **Lines:** [23-52]
- **Behavior:** Integration test asserting attendance correction write durability, Arabic digit parsing, and modal state locking during network failure.
  - Scenario: Error path & recovery with localized Arabic numerals. Parses Eastern Arabic digits `"١٠"` (10) and `"٢٫٥"` (2.5) with Arabic decimal comma.
  - Production code exercised: `AttendanceViewModel.OpenCorrectionCommand`, `AttendanceViewModel.SaveCorrectionCommand`, `AttendanceViewModel.CloseCorrectionCommand`, `AttendanceViewModel.NextDayCommand`, `CorrectAttendanceRequest`.
  - Assertions: Upon network loss (`HttpRequestException("confirmation lost")`), asserts `HasUnresolvedOperation == true` and `CanEditCorrection == false`; modal close and date navigation are blocked (`vm.Date` does not advance; `CorrectionRecord.Id == "attendance"`); modifying inputs to invalid values and resubmitting sends the original parsed payload (`10` work hours, `2.5` overtime hours) across all attempts (`bodies[0] == bodies[1] == bodies[2]`); resolves cleanly on 3rd attempt, closing dialog and refreshing record to 14 hours.
  - Gaps: Does not test negative overtime or total daily hours exceeding 24; does not test attendance record deletion or biometric override logging.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:27:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:27:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [27-34]
- **Behavior:** HTTP mock handler recording correction payloads: throws `HttpRequestException` on 1st request, returns 422 UnprocessableEntity on 2nd, and returns updated record with 10 work hours on 3rd.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:47:59:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:47:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Inline assertion predicate `b => Assert.Equal(bodies[0], b)` validating byte-level request payload repeatability across retries.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:54:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:54:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Date_change_during_read_discards_stale_records_and_loads_the_selected_day()
- **Lines:** [54-70]
- **Behavior:** Asserts race condition resilience in `AttendanceViewModel` when navigating dates during an active read.
  - Scenario: Asynchronous race condition during date navigation.
  - Production code exercised: `AttendanceViewModel.LoadAsync`, `AttendanceViewModel.NextDayCommand`, `AttendanceViewModel.Date`, `AttendanceViewModel.Records`.
  - Assertions: Pauses initial day load via `TaskCompletionSource`. User clicks `NextDayCommand`. Stale response for previous day (`"old-day"`) is discarded when completed late; query for new day (`"new-day"`) is executed, and records collection displays only `new-day` items.
  - Gaps: Does not test rapid repeated date stepping (e.g., clicking 5 times in succession).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:59:50:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:59:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [59-63]
- **Behavior:** HTTP mock handler tracking query paths, delaying 1st request via `TaskCompletionSource`, and returning day 2 records on subsequent requests.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Confirmed_correction_survives_a_failed_followup_read()
- **Lines:** [72-81]
- **Behavior:** Asserts partial failure resilience when attendance correction write succeeds but subsequent GET refresh fails.
  - Scenario: Partial failure recovery: write succeeds (200 OK) but background GET reload fails with 503 ServiceUnavailable.
  - Production code exercised: `AttendanceViewModel.SaveCorrectionCommand`, `AttendanceViewModel.ActionNote`, `AttendanceViewModel.LoadError`.
  - Assertions: Correction modal closes (`IsCorrectionOpen == false`), success notification is preserved in `ActionNote`, read failure is noted in `LoadError`, and view model does not falsely report empty records (`HasRecords == false`, `HasNoRecords == false`).
  - Gaps: Missing explicit retry button to re-fetch attendance list without re-submitting write.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:75:50:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:75:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-76]
- **Behavior:** HTTP mock returning 503 ServiceUnavailable for GET read requests while returning 200 OK with `Record()` for mutation writes.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:83:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:83:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Invalid_correction_does_not_write(string, string)
- **Lines:** [83-94]
- **Behavior:** Theory testing client-side input validation guards on attendance correction.
  - Scenario: Negative validation testing with invalid inputs: ("NaN", "review"), ("-1", "review"), ("8", "").
  - Production code exercised: `AttendanceViewModel.SaveCorrectionCommand`, `AttendanceViewModel.CorrectionError`.
  - Assertions: Zero HTTP write requests reach the network (`writes == 0`) and `CorrectionError` is populated with validation failure messages.
  - Gaps: Does not test hours exceeding 24 or whitespace-only reasons.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:90:50:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:90:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** HTTP spy incrementing `writes` counter to verify that client-side validation prevents HTTP network transmission.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:96:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:96:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Attendance_write_cannot_be_opened_without_the_actual_permission()
- **Lines:** [96-103]
- **Behavior:** Asserts role-based authorization guard on attendance correction.
  - Scenario: Security authorization check. User with only `pos` permission attempts to open correction modal.
  - Production code exercised: `AttendanceViewModel.OpenCorrectionCommand`, `AttendanceViewModel.IsCorrectionOpen`.
  - Assertions: `IsCorrectionOpen` remains `false` without making any network calls.
  - Gaps: Does not test server-side 403 Forbidden handling if client-side check is bypassed.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:99:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:99:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [99-99]
- **Behavior:** Mock handler throwing `InvalidOperationException` if any unexpected HTTP request is dispatched.
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:105:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:105:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Employee(string)
- **Lines:** [105-108]
- **Behavior:** Helper factory constructing an `EmployeeRow` instance wrapping an `EmployeeDto` with specified ID.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:110:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:110:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Audit_filter_changes_while_loading_only_publish_the_latest_filter_result()
- **Lines:** [110-127]
- **Behavior:** Asserts race condition filtering in `AuditViewModel` when audit filter properties change during an in-flight read.
  - Scenario: User filter modification concurrency.
  - Production code exercised: `AuditViewModel.LoadAsync`, `AuditViewModel.SelectedCategory`, `AuditViewModel.SelectedSeverity`, `AuditViewModel.LoadError`.
  - Assertions: Pauses 1st query with `TaskCompletionSource`. User selects category `attendance` and severity `warning`. Stale 1st response error (503) is suppressed; latest filter query results are published without displaying `LoadError`.
  - Gaps: Does not test free-text search filter debounce timing.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:115:45:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:115:45:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [115-119]
- **Behavior:** HTTP mock coordinating asynchronous audit log queries via `TaskCompletionSource`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:121:52:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:121:52:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [121-121]
- **Behavior:** Inline expression lambda 'vm.SelectedCategory = vm.Categories.Single(c => c.Value == "attendance");'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:122:52:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:122:52:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [122-122]
- **Behavior:** Inline expression lambda 'vm.SelectedSeverity = vm.Severities.Single(s => s.Value == "warning");'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:129:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:129:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Ledger_failure_is_not_an_empty_ledger_and_retry_recovers()
- **Lines:** [129-139]
- **Behavior:** Asserts error state vs empty state distinction in `EmployeesViewModel` ledger view.
  - Scenario: Ledger read failure and retry recovery.
  - Production code exercised: `EmployeesViewModel.OpenLedgerCommand`, `EmployeesViewModel.RetryLedgerCommand`, `EmployeesViewModel.LedgerError`, `EmployeesViewModel.HasNoLedgerEntries`.
  - Assertions: 503 ServiceUnavailable on ledger fetch populates `LedgerError` and ensures `HasNoLedgerEntries == false` (preventing confusion between a network failure and a zero-balance ledger). Executing `RetryLedgerCommand` recovers and transitions `HasNoLedgerEntries` to `true`.
  - Gaps: Does not test ledger pagination or large entry sets.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:133:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:133:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [133-134]
- **Behavior:** HTTP mock returning 503 ServiceUnavailable on 1st ledger read and empty array on 2nd read.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:141:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:141:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.A_closed_ledger_read_cannot_overwrite_the_next_employees_result()
- **Lines:** [141-155]
- **Behavior:** Asserts employee selection isolation in `EmployeesViewModel` ledger when navigating between employees.
  - Scenario: Asynchronous race condition. Ledger read for employee "one" is pending; user closes ledger and opens ledger for employee "two".
  - Production code exercised: `EmployeesViewModel.OpenLedgerCommand`, `EmployeesViewModel.CloseLedgerCommand`, `EmployeesViewModel.LedgerEmployee`.
  - Assertions: When delayed read for employee "one" fails with 503, it does not overwrite the active ledger of employee "two" (`LedgerEmployee.Employee.Id == "two"`, `LedgerError == null`).
  - Gaps: Does not test ledger caching across employee switches.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:145:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:145:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-146]
- **Behavior:** HTTP mock delaying employee "one" ledger read via `TaskCompletionSource` while allowing employee "two" to complete immediately.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
