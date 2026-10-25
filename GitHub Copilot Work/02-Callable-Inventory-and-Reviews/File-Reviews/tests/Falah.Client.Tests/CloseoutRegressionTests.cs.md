# File Review: tests/Falah.Client.Tests/CloseoutRegressionTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/CloseoutRegressionTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:16:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:16:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [16-16]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 16-16.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [18-19]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Json(string, System.Net.HttpStatusCode) across lines 18-19.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:20:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:20:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [20-22]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>) across lines 20-22.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Pickup_checkout_sends_the_identity_collected_by_the_form()
- **Lines:** [24-44]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Pickup_checkout_sends_the_identity_collected_by_the_form() across lines 24-44.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:28:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:28:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [28-31]
- **Behavior:** Executes logic for lambda expression across lines 28-31.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:46:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:46:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Uncertain_safe_drop_keeps_the_exact_request_and_blocks_editing()
- **Lines:** [46-66]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Uncertain_safe_drop_keeps_the_exact_request_and_blocks_editing() across lines 46-66.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:50:53:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:50:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [50-53]
- **Behavior:** Executes logic for lambda expression across lines 50-53.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:68:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:68:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Daily_statement_reloads_if_the_date_changes_while_loading()
- **Lines:** [68-88]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Daily_statement_reloads_if_the_date_changes_while_loading() across lines 68-88.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:74:54:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:74:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-78]
- **Behavior:** Executes logic for lambda expression across lines 74-78.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Daily_statement_never_shows_a_response_for_a_date_no_longer_selected()
- **Lines:** [89-117]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.Daily_statement_never_shows_a_response_for_a_date_no_longer_selected() across lines 89-117.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:97:54:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:97:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [97-105]
- **Behavior:** Executes logic for lambda expression across lines 97-105.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:121:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:121:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.The_shift_and_expense_reprint_buttons_only_ask_for_a_reprint()
- **Lines:** [121-151]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.The_shift_and_expense_reprint_buttons_only_ask_for_a_reprint() across lines 121-151.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:125:9:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:125:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Record(HttpRequestMessage)
- **Lines:** [125-130]
- **Behavior:** Executes logic for Record(HttpRequestMessage) across lines 125-130.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:145:27:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:145:27:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Executes logic for lambda expression across lines 145-145.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:146:83:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:146:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Executes logic for lambda expression across lines 146-146.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:147:78:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:147:78:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Executes logic for lambda expression across lines 147-147.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:148:27:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:148:27:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [148-148]
- **Behavior:** Inline expression lambda 'Assert.All(calls, c => Assert.False(string.IsNullOrWhiteSpace(c.Body.Reason)));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:150:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:150:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [150-150]
- **Behavior:** Executes logic for lambda expression across lines 150-150.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:154:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:154:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.The_open_shift_dialog_offers_the_configured_default_and_sends_what_was_typed()
- **Lines:** [154-182]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.The_open_shift_dialog_offers_the_configured_default_and_sends_what_was_typed() across lines 154-182.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:158:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:158:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [158-170]
- **Behavior:** Executes logic for lambda expression across lines 158-170.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.The_offered_float_is_capped_by_what_the_main_treasury_holds()
- **Lines:** [186-201]
- **Behavior:** Executes logic for Falah.Client.Tests.CloseoutRegressionTests.The_offered_float_is_capped_by_what_the_main_treasury_holds() across lines 186-201.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:189:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:189:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [189-195]
- **Behavior:** Executes logic for lambda expression across lines 189-195.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
