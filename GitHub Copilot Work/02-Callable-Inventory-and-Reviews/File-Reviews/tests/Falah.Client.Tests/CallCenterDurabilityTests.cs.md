# File Review: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/CallCenterDurabilityTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:21:5:ConstructorDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:21:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.CallCenterDurabilityTests()
- **Lines:** [21-26]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.CallCenterDurabilityTests() across lines 21-26.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:28:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:28:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.Dispose()
- **Lines:** [28-32]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.Dispose() across lines 28-32.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.Checkout(string)
- **Lines:** [34-56]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.Checkout(string) across lines 34-56.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:58:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:58:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.A_checkout_left_in_flight_is_recovered_with_its_own_idempotency_key()
- **Lines:** [58-74]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.A_checkout_left_in_flight_is_recovered_with_its_own_idempotency_key() across lines 58-74.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:76:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:76:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.A_resolved_checkout_leaves_nothing_behind()
- **Lines:** [76-84]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.A_resolved_checkout_leaves_nothing_behind() across lines 76-84.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:86:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:86:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.Two_operators_never_recover_each_others_unresolved_checkout()
- **Lines:** [86-93]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.Two_operators_never_recover_each_others_unresolved_checkout() across lines 86-93.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:95:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:95:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.A_print_request_left_in_flight_is_recovered_separately_from_the_checkout()
- **Lines:** [95-106]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.A_print_request_left_in_flight_is_recovered_separately_from_the_checkout() across lines 95-106.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:108:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:108:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.The_call_center_store_is_scoped_to_its_own_station()
- **Lines:** [108-118]
- **Behavior:** Executes logic for Falah.Client.Tests.CallCenterDurabilityTests.The_call_center_store_is_scoped_to_its_own_station() across lines 108-118.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
