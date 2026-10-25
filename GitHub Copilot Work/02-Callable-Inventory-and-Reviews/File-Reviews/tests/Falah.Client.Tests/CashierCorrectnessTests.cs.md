# File Review: tests/Falah.Client.Tests/CashierCorrectnessTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/CashierCorrectnessTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:12:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:12:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Money_entry_preserves_piastre_units(string, long)
- **Lines:** [12-16]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Money_entry_preserves_piastre_units(string, long) across lines 12-16.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Ambiguous_or_unrepresentable_money_is_rejected(string)
- **Lines:** [18-22]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Ambiguous_or_unrepresentable_money_is_rejected(string) across lines 18-22.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Exact_tender_and_change_use_egp_to_piastre_conversion()
- **Lines:** [24-35]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Exact_tender_and_change_use_egp_to_piastre_conversion() across lines 24-35.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:27:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:27:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [27-27]
- **Behavior:** Executes logic for lambda expression across lines 27-27.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Touch_keypad_replaces_the_initial_exact_amount()
- **Lines:** [37-45]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Touch_keypad_replaces_the_initial_exact_amount() across lines 37-45.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:40:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:40:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [40-40]
- **Behavior:** Executes logic for lambda expression across lines 40-40.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:47:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:47:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Lost_checkout_response_retries_identical_payload_despite_changed_fields()
- **Lines:** [47-68]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Lost_checkout_response_retries_identical_payload_despite_changed_fields() across lines 47-68.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:51:36:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:51:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-54]
- **Behavior:** Executes logic for lambda expression across lines 51-54.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:70:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:70:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Lost_cash_movement_response_retries_even_if_form_is_now_invalid()
- **Lines:** [70-81]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Lost_cash_movement_response_retries_even_if_form_is_now_invalid() across lines 70-81.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:74:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:74:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** Executes logic for lambda expression across lines 74-74.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:83:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:83:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Malformed_success_response_is_unresolved(string)
- **Lines:** [83-91]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Malformed_success_response_is_unresolved(string) across lines 83-91.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:87:60:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:87:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [87-88]
- **Behavior:** Executes logic for lambda expression across lines 87-88.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:93:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:93:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Draft_is_retained_per_user_across_navigation()
- **Lines:** [93-101]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Draft_is_retained_per_user_across_navigation() across lines 93-101.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:96:36:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:96:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [96-96]
- **Behavior:** Executes logic for lambda expression across lines 96-96.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:103:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:103:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Tax_snapshot_uses_existing_domain_rounding_for_exact_tender()
- **Lines:** [103-115]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Tax_snapshot_uses_existing_domain_rounding_for_exact_tender() across lines 103-115.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:106:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:106:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [106-109]
- **Behavior:** Executes logic for lambda expression across lines 106-109.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:117:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:117:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Lost_refund_response_preserves_id_amount_and_reason()
- **Lines:** [117-131]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Lost_refund_response_preserves_id_amount_and_reason() across lines 117-131.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:121:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:121:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [121-121]
- **Behavior:** Executes logic for lambda expression across lines 121-121.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:133:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:133:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Unauthorized_discount_never_reaches_checkout_api()
- **Lines:** [133-143]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Unauthorized_discount_never_reaches_checkout_api() across lines 133-143.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:137:36:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:137:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [137-137]
- **Behavior:** Executes logic for lambda expression across lines 137-137.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:145:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:145:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Create(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [145-150]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Create(Func<HttpRequestMessage, Task<HttpResponseMessage>>) across lines 145-150.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:153:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:153:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [153-153]
- **Behavior:** Executes logic for Falah.Client.Tests.CashierCorrectnessTests.Handler.SendAsync(HttpRequestMessage, CancellationToken) across lines 153-153.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
