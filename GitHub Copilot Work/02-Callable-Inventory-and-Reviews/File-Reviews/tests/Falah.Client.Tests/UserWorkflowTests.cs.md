# File Review: tests/Falah.Client.Tests/UserWorkflowTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/UserWorkflowTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:13:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:13:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [13-13]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 13-13.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:15:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:15:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Json(object, System.Net.HttpStatusCode)
- **Lines:** [15-15]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.Json(object, System.Net.HttpStatusCode) across lines 15-15.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [18-23]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>) across lines 18-23.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:19:54:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:19:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [19-22]
- **Behavior:** Executes logic for lambda expression across lines 19-22.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:25:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:25:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.User_creation_retains_the_original_identity_pin_and_permissions_after_lost_confirmation()
- **Lines:** [25-53]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.User_creation_retains_the_original_identity_pin_and_permissions_after_lost_confirmation() across lines 25-53.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:29:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:29:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [29-35]
- **Behavior:** Executes logic for lambda expression across lines 29-35.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:39:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:39:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [39-39]
- **Behavior:** Inline expression lambda 'vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = true;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:46:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:46:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [46-46]
- **Behavior:** Inline expression lambda 'vm.NewUserPermissions.Single(p => p.Key == "pos").IsGranted = false;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:50:59:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:50:59:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [50-50]
- **Behavior:** Executes logic for lambda expression across lines 50-50.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:55:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:55:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Existing_user_writes_retry_the_original_decision_and_target(bool)
- **Lines:** [55-78]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.Existing_user_writes_retry_the_original_decision_and_target(bool) across lines 55-78.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:61:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:61:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [61-66]
- **Behavior:** Executes logic for lambda expression across lines 61-66.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:68:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:68:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [68-68]
- **Behavior:** Inline expression lambda 'vm.Permissions.Single(p => p.Key == "pos").IsGranted = false;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:74:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:74:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** Inline expression lambda 'vm.Permissions.Single(p => p.Key == "pos").IsGranted = true;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:80:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:80:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Lost_self_pin_confirmation_is_reconciled_to_the_same_user_without_rewriting_the_pin()
- **Lines:** [80-106]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.Lost_self_pin_confirmation_is_reconciled_to_the_same_user_without_rewriting_the_pin() across lines 80-106.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:84:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:84:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [84-93]
- **Behavior:** Executes logic for lambda expression across lines 84-93.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:108:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:108:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.A_pin_proof_for_a_different_user_does_not_confirm_the_pending_change()
- **Lines:** [108-125]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.A_pin_proof_for_a_different_user_does_not_confirm_the_pending_change() across lines 108-125.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:112:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:112:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [112-118]
- **Behavior:** Executes logic for lambda expression across lines 112-118.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:127:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:127:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.UserWorkflowTests.Administrative_pin_reset_cannot_target_the_owner_and_creation_cannot_grant_unheld_permissions()
- **Lines:** [127-140]
- **Behavior:** Executes logic for Falah.Client.Tests.UserWorkflowTests.Administrative_pin_reset_cannot_target_the_owner_and_creation_cannot_grant_unheld_permissions() across lines 127-140.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:131:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:131:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [131-131]
- **Behavior:** Executes logic for lambda expression across lines 131-131.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/UserWorkflowTests.cs:137:38:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/UserWorkflowTests.cs:137:38:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [137-137]
- **Behavior:** Inline expression lambda 'vm.NewUserPermissions.Single(p => p.Key == "all").IsGranted = true;'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
