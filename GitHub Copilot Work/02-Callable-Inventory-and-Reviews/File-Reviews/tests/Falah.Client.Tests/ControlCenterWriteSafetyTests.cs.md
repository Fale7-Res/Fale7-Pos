# File Review: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:13:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:13:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Overrides HttpMessageHandler.SendAsync to delegate HTTP request execution directly to the injected asynchronous send callback (Func<HttpRequestMessage, Task<HttpResponseMessage>>), enabling custom test mock responses and request recording.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [15-16]
- **Behavior:** Helper method constructing an HttpResponseMessage configured with the specified HTTP status code (defaulting to 200 OK) and UTF-8 encoded application/json StringContent containing the provided JSON string.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ControlCenterWriteSafetyTests.Unknown_management_day_write_retains_its_exact_request_until_confirmation(string)
- **Lines:** [18-84]
- **Behavior:** Parameterized xUnit Theory test verifying that business day write actions ('open', 'close', 'reopen', 'checklist', 'exception') retain exact request payloads across transient failures. Simulates an initial network drop (HttpRequestException), a subsequent 422 UnprocessableEntity error, and verifies that session.HasUnresolvedOperation remains true, editing is prevented, identical request payloads are re-sent on retry, and state resets once confirmation succeeds.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:29:9:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:29:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Day()
- **Lines:** [29-29]
- **Behavior:** Formats a JSON string representation of a business day record incorporating the current test status variable ("Open" or "Closed"), ID "day", and business date "2026-09-08".
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:30:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:30:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [30-48]
- **Behavior:** Asynchronous mock request handler lambda that routes GET requests for business day history and current day endpoints, records POST/PUT write payloads into bodies, throws HttpRequestException on the first attempt to simulate response loss, returns 422 on the second attempt, and returns simulated success or replayed JSON on the third attempt.
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
- **Behavior:** xUnit Fact test asserting that when backend business day queries fail with 503 Service Unavailable, ControlCenterViewModel enforces safety guards by disabling CanOpenDay, CanClose, and CanReopenDay, and exposing a non-null LoadError.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:89:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ControlCenterWriteSafetyTests.cs:89:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Inline lambda expression returning a Task containing an HttpResponseMessage with HttpStatusCode.ServiceUnavailable and an empty JSON object body, simulating service downtime for control center tests.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---