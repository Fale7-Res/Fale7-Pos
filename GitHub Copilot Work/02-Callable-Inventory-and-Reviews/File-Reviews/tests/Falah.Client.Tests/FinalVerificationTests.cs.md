# File Review: tests/Falah.Client.Tests/FinalVerificationTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/FinalVerificationTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:13:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:13:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 13-13.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:16:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:16:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [16-17]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Json(string, System.Net.HttpStatusCode) across lines 16-17.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:19:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:19:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Http(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [19-20]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Http(Func<HttpRequestMessage, Task<HttpResponseMessage>>) across lines 19-20.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:22:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:22:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [22-23]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>) across lines 22-23.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:25:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:25:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.ReadFixture(string)
- **Lines:** [25-38]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.ReadFixture(string) across lines 25-38.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:40:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:40:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Secondary_read_failure_is_visible_and_retry_recovers(string, string)
- **Lines:** [40-72]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Secondary_read_failure_is_visible_and_retry_recovers(string, string) across lines 40-72.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:55:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:55:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [55-59]
- **Behavior:** Executes logic for lambda expression across lines 55-59.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:65:9:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:65:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Load()
- **Lines:** [65-65]
- **Behavior:** Executes logic for Load() across lines 65-65.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:74:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:74:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Missing_blocker_response_never_means_business_day_can_close()
- **Lines:** [74-90]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Missing_blocker_response_never_means_business_day_can_close() across lines 74-90.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:78:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:78:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [78-80]
- **Behavior:** Executes logic for lambda expression across lines 78-80.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:92:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:92:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Failed_expenses_are_not_presented_as_a_zero_expense_report()
- **Lines:** [92-101]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Failed_expenses_are_not_presented_as_a_zero_expense_report() across lines 92-101.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:95:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:95:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [95-97]
- **Behavior:** Executes logic for lambda expression across lines 95-97.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:106:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:106:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Stepping_a_day_switches_the_screen_onto_that_day()
- **Lines:** [106-129]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Stepping_a_day_switches_the_screen_onto_that_day() across lines 106-129.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:110:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:110:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [110-113]
- **Behavior:** Executes logic for lambda expression across lines 110-113.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:119:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:119:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [119-119]
- **Behavior:** Executes logic for lambda expression across lines 119-119.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:128:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:128:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [128-128]
- **Behavior:** Executes logic for lambda expression across lines 128-128.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:132:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:132:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Choosing_a_week_asks_every_report_for_the_whole_span()
- **Lines:** [132-150]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Choosing_a_week_asks_every_report_for_the_whole_span() across lines 132-150.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:136:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:136:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [136-139]
- **Behavior:** Executes logic for lambda expression across lines 136-139.
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
- **Behavior:** Executes logic for lambda expression across lines 148-148.
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
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.WaitForIdleAsync(Falah.Client.CashierWinUI.ViewModels.ReportsViewModel) across lines 154-157.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:159:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:159:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Changing_report_date_during_a_read_reloads_a_consistent_new_date()
- **Lines:** [159-184]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Changing_report_date_during_a_read_reloads_a_consistent_new_date() across lines 159-184.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:165:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:165:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [165-169]
- **Behavior:** Executes logic for lambda expression across lines 165-169.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:182:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:182:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [182-182]
- **Behavior:** Executes logic for lambda expression across lines 182-182.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:183:49:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:183:49:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [183-183]
- **Behavior:** Executes logic for lambda expression across lines 183-183.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Printing_empty_state_is_notified_after_loading_finishes()
- **Lines:** [186-200]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Printing_empty_state_is_notified_after_loading_finishes() across lines 186-200.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:191:48:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:191:48:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [191-194]
- **Behavior:** Executes logic for lambda expression across lines 191-194.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:196:31:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:196:31:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [196-196]
- **Behavior:** Executes logic for lambda expression across lines 196-196.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:202:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:202:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Payroll_does_not_show_a_failed_line_read_as_zero_payable()
- **Lines:** [202-213]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Payroll_does_not_show_a_failed_line_read_as_zero_payable() across lines 202-213.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:205:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:205:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [205-206]
- **Behavior:** Executes logic for lambda expression across lines 205-206.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:215:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:215:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Payroll_approval_permission_alone_does_not_bypass_the_management_gate()
- **Lines:** [215-225]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Payroll_approval_permission_alone_does_not_bypass_the_management_gate() across lines 215-225.
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
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Control_center_view_permission_cannot_change_the_close_checklist() across lines 227-244.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:231:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:231:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [231-234]
- **Behavior:** Executes logic for lambda expression across lines 231-234.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:246:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:246:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Treasury_retries_original_payload_and_blocks_abandonment_after_an_unknown_outcome(string)
- **Lines:** [246-282]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Treasury_retries_original_payload_and_blocks_abandonment_after_an_unknown_outcome(string) across lines 246-282.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:254:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:254:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [254-260]
- **Behavior:** Executes logic for lambda expression across lines 254-260.
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
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Treasury_definite_rejection_allows_correction_and_releases_the_session() across lines 284-297.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:287:33:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:287:33:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [287-287]
- **Behavior:** Executes logic for lambda expression across lines 287-287.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:299:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:299:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_recovers_each_operators_own_draft_after_switching_back()
- **Lines:** [299-315]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_recovers_each_operators_own_draft_after_switching_back() across lines 299-315.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:302:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:302:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [302-302]
- **Behavior:** Executes logic for lambda expression across lines 302-302.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:317:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:317:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Electronic_payment_decision_cannot_switch_or_be_abandoned_after_response_loss(bool)
- **Lines:** [317-353]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Electronic_payment_decision_cannot_switch_or_be_abandoned_after_response_loss(bool) across lines 317-353.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:323:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:323:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [323-329]
- **Behavior:** Executes logic for lambda expression across lines 323-329.
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
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_reprint_retries_the_original_request_and_prevents_abandonment() across lines 355-385.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:359:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:359:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [359-364]
- **Behavior:** Executes logic for lambda expression across lines 359-364.
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
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_keeps_unresolved_request_after_a_later_rejection() across lines 387-424.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:391:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:391:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [391-396]
- **Behavior:** Executes logic for lambda expression across lines 391-396.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:426:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:426:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_lock_waits_for_an_in_flight_operation_then_returns_to_login()
- **Lines:** [426-447]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_lock_waits_for_an_in_flight_operation_then_returns_to_login() across lines 426-447.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:429:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:429:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [429-429]
- **Behavior:** Executes logic for lambda expression across lines 429-429.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:449:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:449:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_no_longer_requests_its_own_tickets_after_checkout()
- **Lines:** [449-498]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_no_longer_requests_its_own_tickets_after_checkout() across lines 449-498.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:462:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:462:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [462-476]
- **Behavior:** Executes logic for lambda expression across lines 462-476.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:500:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:500:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_says_so_when_the_server_could_not_queue_the_tickets()
- **Lines:** [500-527]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_says_so_when_the_server_could_not_queue_the_tickets() across lines 500-527.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:506:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:506:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [506-511]
- **Behavior:** Executes logic for lambda expression across lines 506-511.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:529:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:529:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.FinalVerificationTests.Call_center_estimate_uses_the_existing_tax_rules(string, long)
- **Lines:** [529-544]
- **Behavior:** Executes logic for Falah.Client.Tests.FinalVerificationTests.Call_center_estimate_uses_the_existing_tax_rules(string, long) across lines 529-544.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/FinalVerificationTests.cs:534:81:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/FinalVerificationTests.cs:534:81:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [534-536]
- **Behavior:** Executes logic for lambda expression across lines 534-536.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
