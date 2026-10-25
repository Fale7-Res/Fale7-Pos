# File Review: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:17:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:17:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ManagementWriteSafetyTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [17-17]
- **Behavior:** Test transport mock delegating HTTP request handling directly to a captured `send` callback, facilitating in-memory request capturing and network exception injection.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:20:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:20:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ManagementWriteSafetyTests.Dto<T>(string)
- **Lines:** [20-21]
- **Behavior:** Generic JSON deserialization helper converting JSON fixture strings to strongly typed DTOs using web defaults.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:22:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:22:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ManagementWriteSafetyTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [22-23]
- **Behavior:** Test helper constructing an `HttpResponseMessage` containing UTF-8 JSON content with the specified status code (default `HttpStatusCode.OK`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:27:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:27:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ManagementWriteSafetyTests.CreateFlowAsync(string, Falah.Client.CashierWinUI.Services.AppSession)
- **Lines:** [27-115]
- **Behavior:** Flow harness factory constructing a `Flow` delegate tuple (Submit, CorruptInputs, Close, IsOpen, CanEdit) across 11 management operations: delivery driver assignment, outcome confirmation, driver settlement, payroll run creation, payroll disbursement, employee cash advance, inventory count, waste recording, supplier creation, purchase receiving, and purchase debt payment.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:37:28:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:37:28:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [37-37]
- **Behavior:** Submit delegate: invokes `vm.ConfirmAssignCommand.ExecuteAsync(null)` to dispatch driver assignment.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:37:78:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:37:78:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [37-37]
- **Behavior:** CorruptInputs delegate: nulls out `vm.AssignDriver` to simulate UI form tampering during an in-flight operation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:38:21:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:38:21:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [38-38]
- **Behavior:** Close delegate: invokes `vm.CloseAssignCommand.Execute(null)` to attempt dialog dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:38:64:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:38:64:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [38-38]
- **Behavior:** IsOpen delegate: inspects `vm.IsAssignOpen` to verify modal retention during unresolved write states.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:38:87:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:38:87:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [38-38]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to verify UI editing controls are locked.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:43:28:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:43:28:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [43-43]
- **Behavior:** Submit delegate: invokes `vm.ConfirmOutcomeCommand.ExecuteAsync(null)` to dispatch delivery outcome confirmation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:43:79:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:43:79:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [43-43]
- **Behavior:** CorruptInputs delegate: sets `vm.OutcomeCollectedText = "invalid"` to simulate input corruption.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:44:21:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:44:21:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** Close delegate: invokes `vm.CloseOutcomeCommand.Execute(null)` to attempt dialog dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:44:65:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:44:65:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** IsOpen delegate: inspects `vm.IsOutcomeOpen` to assert dialog remains open during pending operations.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:44:89:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:44:89:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [44-44]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to verify controls are disabled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:51:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:51:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** Submit delegate: invokes `vm.SettleDriverCommand.ExecuteAsync(null)` to dispatch driver custody settlement.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:51:73:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:51:73:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-51]
- **Behavior:** CorruptInputs delegate: sets `vm.SettleActualCashText = "invalid"` to corrupt custody cash amount.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:52:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:52:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [52-52]
- **Behavior:** Close delegate: invokes `vm.CloseDriverAccountCommand.Execute(null)` to attempt modal closure.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:52:67:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:52:67:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [52-52]
- **Behavior:** IsOpen delegate: inspects `vm.IsDriverAccountOpen` to verify driver account sheet remains visible.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:52:97:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:52:97:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [52-52]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to ensure settlement inputs cannot be modified while unresolved.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:60:28:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:60:28:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [60-60]
- **Behavior:** Submit delegate: invokes `vm.CreateRunCommand.ExecuteAsync(null)` to dispatch payroll run creation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:60:74:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:60:74:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [60-60]
- **Behavior:** CorruptInputs delegate: sets `vm.NewPeriod = "invalid"` to simulate period input corruption.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:61:21:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:61:21:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [61-61]
- **Behavior:** Close delegate: invokes `vm.SelectRunCommand.Execute(null)` to attempt clearing selected run.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:61:62:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:61:62:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [61-61]
- **Behavior:** IsOpen delegate: inspects `!vm.CanEditAction` to verify lock state on payroll run creation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:61:87:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:61:87:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [61-61]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to assert payroll form controls are locked.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:65:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:65:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [65-65]
- **Behavior:** Submit delegate: invokes `vm.PayRunCommand.ExecuteAsync(null)` to dispatch payroll run disbursement.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:65:67:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:65:67:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [65-65]
- **Behavior:** CorruptInputs delegate: switches `vm.PaymentAccount` to a different treasury account to simulate account hijacking.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:66:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:66:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [66-66]
- **Behavior:** Close delegate: invokes `vm.ClosePayCommand.Execute(null)` to attempt dialog dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:66:57:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:66:57:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [66-66]
- **Behavior:** IsOpen delegate: inspects `vm.IsPayOpen` to verify disbursement dialog remains locked open.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:66:77:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:66:77:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [66-66]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to verify payment account selectors are disabled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:79:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:79:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [79-79]
- **Behavior:** Inline expression lambda 'return new(() => vm.SubmitCashOutCommand.ExecuteAsync(null),'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:80:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:80:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [80-80]
- **Behavior:** Inline expression lambda '() => { vm.CashOutAmountText = "invalid"; session.ShiftId = null; },'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:81:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:81:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [81-81]
- **Behavior:** Close delegate: invokes `vm.CloseCashOutCommand.Execute(null)` to attempt employee advance dialog dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:81:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:81:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [81-81]
- **Behavior:** IsOpen delegate: inspects `vm.IsCashOutOpen` to verify cash out modal remains open.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:81:85:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:81:85:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [81-81]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to verify cash out amount inputs cannot be edited.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:89:28:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:89:28:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** Submit delegate: invokes `vm.SubmitCountCommand.ExecuteAsync(null)` to dispatch inventory count submission.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:89:76:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:89:76:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [89-89]
- **Behavior:** CorruptInputs delegate: sets `vm.CountActualText = "invalid"` to corrupt inventory count quantity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:90:21:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:90:21:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** Close delegate: invokes `vm.CloseCountCommand.Execute(null)` to attempt count modal dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:90:63:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:90:63:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** IsOpen delegate: inspects `vm.IsCountOpen` to verify count sheet remains displayed.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:90:85:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:90:85:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [90-90]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to verify count inputs are locked.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:93:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:93:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** Submit delegate: invokes `vm.SubmitWasteCommand.ExecuteAsync(null)` to dispatch waste entry registration.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:93:72:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:93:72:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [93-93]
- **Behavior:** CorruptInputs delegate: sets `vm.WasteQuantityText = "invalid"` to corrupt waste quantity input.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:94:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:94:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [94-94]
- **Behavior:** Close delegate: invokes `vm.CloseWasteCommand.Execute(null)` to attempt waste modal closure.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:94:59:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:94:59:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [94-94]
- **Behavior:** IsOpen delegate: inspects `vm.IsWasteOpen` to assert waste modal retention.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:94:81:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:94:81:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [94-94]
- **Behavior:** CanEdit delegate: inspects `vm.CanEditAction` to assert waste form editing is blocked.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:100:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:100:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** Submit delegate: invokes `purchasing.SubmitSupplierCommand.ExecuteAsync(null)` to dispatch supplier creation.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:100:83:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:100:83:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [100-100]
- **Behavior:** CorruptInputs delegate: clears `purchasing.SupplierName = ""` to corrupt supplier name.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:101:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:101:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [101-101]
- **Behavior:** Close delegate: invokes `purchasing.CloseAddSupplierCommand.Execute(null)` to attempt supplier modal dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:101:73:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:101:73:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [101-101]
- **Behavior:** IsOpen delegate: inspects `purchasing.IsSupplierOpen` to verify supplier dialog remains open.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:101:106:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:101:106:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [101-101]
- **Behavior:** CanEdit delegate: inspects `purchasing.CanEditAction` to ensure supplier form controls are disabled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:109:24:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:109:24:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [109-109]
- **Behavior:** Submit delegate: invokes `purchasing.SubmitReceiveCommand.ExecuteAsync(null)` to dispatch purchase intake command.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:109:82:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:109:82:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [109-109]
- **Behavior:** CorruptInputs delegate: sets `line.QuantityText = "invalid"` to corrupt invoice intake line quantity.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:110:17:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:110:17:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [110-110]
- **Behavior:** Close delegate: invokes `purchasing.CloseReceiveCommand.Execute(null)` to attempt receive modal closure.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:110:69:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:110:69:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [110-110]
- **Behavior:** IsOpen delegate: inspects `purchasing.IsReceiveOpen` to verify receive sheet remains active.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:110:101:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:110:101:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [110-110]
- **Behavior:** CanEdit delegate: inspects `purchasing.CanEditAction` to verify purchase intake lines cannot be edited.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:113:20:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:113:20:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [113-113]
- **Behavior:** Submit delegate: invokes `purchasing.SubmitPayCommand.ExecuteAsync(null)` to dispatch supplier debt payment.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:113:74:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:113:74:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [113-113]
- **Behavior:** CorruptInputs delegate: sets `purchasing.PayAmountText = "invalid"` to corrupt disbursement amount.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:114:13:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:114:13:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** Close delegate: invokes `purchasing.ClosePayCommand.Execute(null)` to attempt payment dialog dismissal.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:114:61:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:114:61:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** IsOpen delegate: inspects `purchasing.IsPayOpen` to verify debt payment dialog remains open.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:114:89:ParenthesizedLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:114:89:ParenthesizedLambdaExpression
- **Kind:** ParenthesizedLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [114-114]
- **Behavior:** CanEdit delegate: inspects `purchasing.CanEditAction` to verify payment inputs are disabled.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:117:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:117:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ManagementWriteSafetyTests.Lost_response_then_rejection_keeps_the_exact_write_and_blocks_abandonment(string)
- **Lines:** [117-166]
- **Behavior:** Comprehensive matrix theory validating write idempotency and dialog lock safety across 11 management flows.
  - Scenario: Error path & recovery under connection loss followed by 422 rejection.
  - Production code exercised: `DeliveryViewModel`, `PayrollViewModel`, `EmployeesViewModel`, `InventoryViewModel`, `PurchasingViewModel`, `AppSession.HasUnresolvedOperation`.
  - Assertions: Request 1 throws `HttpRequestException("response lost after commit")`; verifies `HasUnresolvedOperation == true`, `CanEditAction == false`, and dialog dismissal is blocked; corrupting form inputs is completely ignored on resubmit; request 2 returns 422 UnprocessableEntity; request 3 succeeds; all 3 HTTP payloads sent across the network are identical byte-for-byte (`requests[0] == requests[1] == requests[2]`); resolves cleanly and unlocks modal.
  - Gaps: Does not test network timeouts on multi-line receiving invoices with hundreds of items.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:132:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:132:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [132-140]
- **Behavior:** HTTP mock interceptor recording `(Path, Body)` requests, throwing `HttpRequestException` on request 1, returning 422 on request 2, and returning 200 OK with replay response on request 3.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:157:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:157:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [157-157]
- **Behavior:** Inline expression lambda 'Assert.All(requests, request => Assert.Equal(requests[0], request));'.
- **Validations:** None.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:168:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:168:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.ManagementWriteSafetyTests.Lost_payroll_transition_reconciles_committed_status_without_advancing_a_second_step()
- **Lines:** [168-199]
- **Behavior:** Asserts idempotent payroll status transition recovery after network drop.
  - Scenario: Lost response during payroll status transition (`AdvanceRunCommand`). Server commits transition to `calculated`, but client experiences timeout.
  - Production code exercised: `PayrollViewModel.AdvanceRunCommand`, `PayrollViewModel.SelectRunCommand`, `PayrollViewModel.SelectedRun`.
  - Assertions: On retry, server returns 422 `{"error": "already transitioned"}`; client automatically queries `/api/payroll-runs/run` to reconcile state; first read fails with 503, retry returns `status: "calculated"`; verifies client reconciles status to `calculated` without erroneously advancing to the next workflow state (`writes.Count == 2`, `writes[0] == writes[1]`, `ActionError == null`).
  - Gaps: Does not test reconciliation when server returns 404 for deleted payroll run.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:173:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/ManagementWriteSafetyTests.cs:173:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [173-187]
- **Behavior:** HTTP mock interceptor simulating lost response on payroll transition POST, followed by 422 'already transitioned', and servicing payroll run status GET reconciliation.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
