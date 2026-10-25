# File Review: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/CallCenterDurabilityTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:21:5:ConstructorDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:21:5:ConstructorDeclaration
- **Kind:** ConstructorDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.CallCenterDurabilityTests()
- **Lines:** [21-26]
- **Behavior:** Initializes test fixture by creating an isolated temporary storage directory (falah-cc-retained-{Guid:N}), creating a FileRetainedRequestStore instance, and binding it to ambient CallCenterServices.RetainedRequestStorage.Current.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:28:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:28:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.Dispose()
- **Lines:** [28-32]
- **Behavior:** Tears down test fixture by resetting CallCenterServices.RetainedRequestStorage.Current to null and recursively deleting the isolated temporary storage directory.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:34:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:34:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.Checkout(string)
- **Lines:** [34-56]
- **Behavior:** Test factory helper producing a dummy CheckoutOrderRequest with specified CheckoutRequestId, cashier "usr-1", shift "shift-1", order type "delivery", cash payment, and sample delivery address metadata.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:58:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:58:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.A_checkout_left_in_flight_is_recovered_with_its_own_idempotency_key()
- **Lines:** [58-74]
- **Behavior:** Verifies that an unconfirmed in-flight checkout request saved to FileRetainedRequestStore survives and is deserialized back with identical CheckoutRequestId and serialized JSON payload for idempotent server resubmission.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:76:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:76:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.A_resolved_checkout_leaves_nothing_behind()
- **Lines:** [76-84]
- **Behavior:** Verifies that once a checkout operation completes and is cleared from the store via Clear(), loading returns null and the store file list is completely empty.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:86:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:86:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.Two_operators_never_recover_each_others_unresolved_checkout()
- **Lines:** [86-93]
- **Behavior:** Verifies per-operator isolation in FileRetainedRequestStore by asserting that a pending checkout stored under "usr-1" is not visible or recoverable under "usr-2".
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:95:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:95:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.A_print_request_left_in_flight_is_recovered_separately_from_the_checkout()
- **Lines:** [95-106]
- **Behavior:** Verifies that multiple concurrent operation types (CheckoutOrderRequest and CreatePrintJobsRequest) under the same user key prefix are stored, recovered, and enumerated independently without collision.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:108:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CallCenterDurabilityTests.cs:108:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CallCenterDurabilityTests.The_call_center_store_is_scoped_to_its_own_station()
- **Lines:** [108-118]
- **Behavior:** Asserts station directory path scoping: verifies CallCenter and Cashier request store directories are distinct and end with their respective station folder names.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
