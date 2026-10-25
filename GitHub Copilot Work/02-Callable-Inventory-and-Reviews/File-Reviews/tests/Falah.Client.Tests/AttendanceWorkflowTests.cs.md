# File Review: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/AttendanceWorkflowTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:14:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:14:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [14-14]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 14-14.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Json(object, System.Net.HttpStatusCode)
- **Lines:** [16-16]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Json(object, System.Net.HttpStatusCode) across lines 16-16.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:17:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:17:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [17-19]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>) across lines 17-19.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:20:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:20:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Record(string)
- **Lines:** [20-21]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Record(string) across lines 20-21.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:23:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:23:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Attendance_retry_keeps_the_original_record_values_and_reason_until_confirmed()
- **Lines:** [23-52]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Attendance_retry_keeps_the_original_record_values_and_reason_until_confirmed() across lines 23-52.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:27:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:27:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [27-34]
- **Behavior:** Executes logic for lambda expression across lines 27-34.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:47:59:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:47:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [47-47]
- **Behavior:** Executes logic for lambda expression across lines 47-47.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:54:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:54:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Date_change_during_read_discards_stale_records_and_loads_the_selected_day()
- **Lines:** [54-70]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Date_change_during_read_discards_stale_records_and_loads_the_selected_day() across lines 54-70.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:59:50:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:59:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [59-63]
- **Behavior:** Executes logic for lambda expression across lines 59-63.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:72:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:72:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Confirmed_correction_survives_a_failed_followup_read()
- **Lines:** [72-81]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Confirmed_correction_survives_a_failed_followup_read() across lines 72-81.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:75:50:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:75:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [75-76]
- **Behavior:** Executes logic for lambda expression across lines 75-76.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:83:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:83:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Invalid_correction_does_not_write(string, string)
- **Lines:** [83-94]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Invalid_correction_does_not_write(string, string) across lines 83-94.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:90:50:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:90:50:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** Executes logic for lambda expression across lines 90-90.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:96:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:96:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Attendance_write_cannot_be_opened_without_the_actual_permission()
- **Lines:** [96-103]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Attendance_write_cannot_be_opened_without_the_actual_permission() across lines 96-103.
- **Validations:** Validation checks detected: True. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:99:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:99:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [99-99]
- **Behavior:** Executes logic for lambda expression across lines 99-99.
- **Validations:** Validation checks detected: False. Throws: InvalidOperationException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:105:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:105:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Employee(string)
- **Lines:** [105-108]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Employee(string) across lines 105-108.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:110:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:110:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.Audit_filter_changes_while_loading_only_publish_the_latest_filter_result()
- **Lines:** [110-127]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Audit_filter_changes_while_loading_only_publish_the_latest_filter_result() across lines 110-127.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:115:45:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:115:45:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [115-119]
- **Behavior:** Executes logic for lambda expression across lines 115-119.
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
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.Ledger_failure_is_not_an_empty_ledger_and_retry_recovers() across lines 129-139.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:133:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:133:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [133-134]
- **Behavior:** Executes logic for lambda expression across lines 133-134.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:141:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:141:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.AttendanceWorkflowTests.A_closed_ledger_read_cannot_overwrite_the_next_employees_result()
- **Lines:** [141-155]
- **Behavior:** Executes logic for Falah.Client.Tests.AttendanceWorkflowTests.A_closed_ledger_read_cannot_overwrite_the_next_employees_result() across lines 141-155.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:145:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/AttendanceWorkflowTests.cs:145:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-146]
- **Behavior:** Executes logic for lambda expression across lines 145-146.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
