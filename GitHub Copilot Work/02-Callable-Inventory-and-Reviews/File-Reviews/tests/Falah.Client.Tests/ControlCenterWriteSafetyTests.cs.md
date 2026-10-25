# File Review: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:13:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:13:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Executes logic for Falah.Client.Tests.ControlCenterWriteSafetyTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 13-13.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [15-16]
- **Behavior:** Executes logic for Falah.Client.Tests.ControlCenterWriteSafetyTests.Json(string, System.Net.HttpStatusCode) across lines 15-16.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Unknown_management_day_write_retains_its_exact_request_until_confirmation(string)
- **Lines:** [18-84]
- **Behavior:** Executes logic for Falah.Client.Tests.ControlCenterWriteSafetyTests.Unknown_management_day_write_retains_its_exact_request_until_confirmation(string) across lines 18-84.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:29:9:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:29:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Day()
- **Lines:** [29-29]
- **Behavior:** Executes logic for Day() across lines 29-29.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:30:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:30:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [30-48]
- **Behavior:** Executes logic for lambda expression across lines 30-48.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:78:28:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:78:28:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [78-78]
- **Behavior:** Inline expression lambda 'Assert.All(bodies, b => Assert.Equal(bodies[0], b));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:86:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:86:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Unknown_day_history_disables_open_close_and_reopen()
- **Lines:** [86-97]
- **Behavior:** Executes logic for Falah.Client.Tests.ControlCenterWriteSafetyTests.Unknown_day_history_disables_open_close_and_reopen() across lines 86-97.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:89:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:89:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Executes logic for lambda expression across lines 89-89.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
