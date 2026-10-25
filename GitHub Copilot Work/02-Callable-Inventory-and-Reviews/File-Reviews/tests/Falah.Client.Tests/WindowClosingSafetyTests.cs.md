# File Review: tests/Falah.Client.Tests/WindowClosingSafetyTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/WindowClosingSafetyTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/WindowClosingSafetyTests.cs:11:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/WindowClosingSafetyTests.cs:11:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.WindowClosingSafetyTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [11-12]
- **Behavior:** Executes logic for Falah.Client.Tests.WindowClosingSafetyTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 11-12.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/WindowClosingSafetyTests.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/WindowClosingSafetyTests.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.WindowClosingSafetyTests.Cashier_window_close_respects_both_pos_and_management_operations(bool)
- **Lines:** [15-32]
- **Behavior:** Executes logic for Falah.Client.Tests.WindowClosingSafetyTests.Cashier_window_close_respects_both_pos_and_management_operations(bool) across lines 15-32.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
