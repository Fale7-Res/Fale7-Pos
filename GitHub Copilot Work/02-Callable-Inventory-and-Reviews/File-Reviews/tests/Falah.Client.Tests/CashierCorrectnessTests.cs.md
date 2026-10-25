# File Review: tests/Falah.Client.Tests/CashierCorrectnessTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/CashierCorrectnessTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:12:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:12:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Money_entry_preserves_piastre_units(string, long)
- **Lines:** [12-16]
- **Behavior:** Theory verifying localized numeric parsing and piastre unit integrity in `CashInput.TryParse`.
  - Scenario: Multilingual currency input: Eastern Arabic digits (`"١٢٣٫٤٥"` -> 12345 piastres), Persian digits (`"۱۲۳.۴۵"` -> 12345), sub-unit fractions (`"0.01"` -> 1 piastre), and whitespace padding (`" 200 "` -> 20000 piastres).
  - Production code exercised: `CashInput.TryParse`.
  - Assertions: Verifies parsing succeeds (`True`) and matches integer piastres without floating point inaccuracies or locale-dependent truncation.
  - Gaps: Does not test Eastern Arabic thousands separator (U+066C).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Ambiguous_or_unrepresentable_money_is_rejected(string)
- **Lines:** [18-22]
- **Behavior:** Theory verifying rejection of invalid, ambiguous, or overflow money inputs in `CashInput.TryParse`.
  - Scenario: Negative input validation: sub-piastre precision (`"1.001"`), negative numbers (`"-1"`), currency letters (`"100 ج"`), ambiguous thousands commas (`"1,000"`), and 64-bit integer overflow (`"92233720368547758.08"`).
  - Production code exercised: `CashInput.TryParse`.
  - Assertions: Asserts `TryParse` returns `false` for all invalid input formats.
  - Gaps: Does not test scientific notation (`"1e2"`) or non-breaking spaces.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Exact_tender_and_change_use_egp_to_piastre_conversion()
- **Lines:** [24-35]
- **Behavior:** Asserts tender amount initialization, Eastern Arabic digit parsing, and change calculation in `CashierViewModel`.
  - Scenario: Cashier tender workflow. Cart total is 12550 piastres (125.50 EGP).
  - Production code exercised: `CashierViewModel.OpenCheckoutCommand`, `CashierViewModel.AmountPaidText`, `CashierViewModel.ChangeDuePiastres`, `CashierViewModel.QuickCashCommand`.
  - Assertions: Initial checkout sets `AmountPaidText == "125.50"`; entering Arabic digits `"٢٠٠"` updates `ChangeDuePiastres == 7450` (74.50 EGP); executing `QuickCashCommand("exact")` sets change to 0.
  - Gaps: Does not test underpayment validation where paid amount is less than grand total.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:27:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:27:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [27-27]
- **Behavior:** HTTP mock handler returning 404 NotFound to ensure no unexpected network traffic occurs during tender calculations.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:37:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:37:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Touch_keypad_replaces_the_initial_exact_amount()
- **Lines:** [37-45]
- **Behavior:** Asserts touchscreen numeric keypad ergonomics during checkout tender entry.
  - Scenario: Touchscreen POS keypad interaction.
  - Production code exercised: `CashierViewModel.OpenCheckoutCommand`, `CashierViewModel.PressAmountKeyCommand`.
  - Assertions: Initial exact tender amount (`125.50`) is replaced entirely upon first numeric key press rather than concatenated; pressing '2', '0', '0' sets `AmountPaidText == "200"` and recalculates `ChangeDuePiastres == 7450`.
  - Gaps: Does not test decimal point keypad entry or backspace keypad button.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:40:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:40:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [40-40]
- **Behavior:** HTTP mock handler returning 404 NotFound for keypad test setup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:47:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:47:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Lost_checkout_response_retries_identical_payload_despite_changed_fields()
- **Lines:** [47-68]
- **Behavior:** Asserts order checkout idempotency, form locking, and cart protection during network response loss.
  - Scenario: Error path & recovery: checkout request commits on server but response is lost (`HttpRequestException`).
  - Production code exercised: `CashierViewModel.SubmitPaymentCommand`, `CashierViewModel.CloseCheckoutCommand`, `CashierViewModel.ClearCartCommand`, `AppSession.HasUnresolvedOperation`.
  - Assertions: Verifies `CanEditOperation == false` and `HasUnresolvedOperation == true`; modifying `AmountPaidText` to `"invalid"` is ignored on resubmit; both HTTP requests send identical payloads (`bodies[0] == bodies[1]`); closing checkout dialog and clearing cart are locked out while unresolved (`IsCheckoutOpen == true`, cart items retained).
  - Gaps: Does not test server returning duplicate order conflict (409) if client generates a new request ID.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:51:36:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:51:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [51-54]
- **Behavior:** HTTP mock recording serialized checkout payloads and throwing `HttpRequestException("Response lost after commit")`.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:70:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:70:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Lost_cash_movement_response_retries_even_if_form_is_now_invalid()
- **Lines:** [70-81]
- **Behavior:** Asserts cash movement retry idempotency even when UI input fields are invalidated post-timeout.
  - Scenario: Error path & UI mutation resilience during cash drop/out.
  - Production code exercised: `CashierViewModel.OpenCashMovementCommand`, `CashierViewModel.SubmitCashMovementCommand`.
  - Assertions: Cash out of 25 EGP experiences network drop; altering `CashMovementAmountText` to `"invalid"` and `CashMovementReason` to `""` does not break retry; resubmission sends exact original payload (`bodies[0] == bodies[1]`).
  - Gaps: Does not test register cash drawer insufficient funds validation.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:74:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:74:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-74]
- **Behavior:** HTTP mock recording cash movement payloads and throwing `HttpRequestException`.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:83:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:83:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Malformed_success_response_is_unresolved(string)
- **Lines:** [83-91]
- **Behavior:** Theory asserting defensive handling of malformed HTTP 200 OK responses ("null", "not-json").
  - Scenario: Fault injection on successful HTTP status codes.
  - Production code exercised: `ApiClient.GetCurrentShiftAsync`, `ApiClient.ApiResult.OutcomeUnknown`.
  - Assertions: Asserts `result.Success == false` and `result.OutcomeUnknown == true`, ensuring malformed bodies do not result in false success assumptions or silent state corruption.
  - Gaps: Does not test truncated JSON streaming responses.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:87:60:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:87:60:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [87-88]
- **Behavior:** HTTP mock returning HTTP 200 OK with the specified raw string body.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:93:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:93:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Draft_is_retained_per_user_across_navigation()
- **Lines:** [93-101]
- **Behavior:** Asserts multi-user cashier draft isolation in `AppSession`.
  - Scenario: Fast cashier switching on a shared station.
  - Production code exercised: `AppSession.GetCashier`, `AppSession.CurrentUser`.
  - Assertions: When `CurrentUser` is switched from `cashier` to `other`, `session.GetCashier()` instantiates a distinct view model; switching back to `cashier` restores the exact original view model instance with in-progress cart.
  - Gaps: Does not test memory reclamation or session expiration for inactive cashier drafts.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:96:36:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:96:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [96-96]
- **Behavior:** HTTP mock returning 404 NotFound for cashier switching setup.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:103:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:103:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Tax_snapshot_uses_existing_domain_rounding_for_exact_tender()
- **Lines:** [103-115]
- **Behavior:** Asserts exclusive tax snapshot calculation and integer rounding during cart checkout.
  - Scenario: Exclusive VAT (14%) computation on cart items.
  - Production code exercised: `CashierViewModel.LoadMenuAsync`, `CashierViewModel.GrandTotalPiastres`, `CashierViewModel.AmountPaidText`.
  - Assertions: 14% tax on 10001 piastres computes to 1400.14 piastres, rounding to 1400 piastres; `GrandTotalPiastres == 11401` (114.01 EGP) and default tender `AmountPaidText == "114.01"`.
  - Gaps: Does not test compound taxes (e.g. VAT + municipal tax).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:106:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:106:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [106-109]
- **Behavior:** HTTP mock returning empty menu categories for `/api/menu` and 14% exclusive tax rule for tax endpoints.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:117:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:117:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Lost_refund_response_preserves_id_amount_and_reason()
- **Lines:** [117-131]
- **Behavior:** Asserts refund write idempotency and dialog lock during network response loss.
  - Scenario: Error path & recovery for order refund submission.
  - Production code exercised: `CashierViewModel.OpenRefundCommand`, `CashierViewModel.SubmitRefundCommand`, `CashierViewModel.CancelRefundCommand`.
  - Assertions: Refund of 25 EGP experiences network drop; altering refund amount to `"bad"` and reason to `"changed"` is ignored on resubmit (`bodies[0] == bodies[1]`); canceling refund while unresolved is blocked (`IsRefundOpen == true`).
  - Gaps: Does not test refund exceeding original paid amount or partial refund balance tracking.
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:121:30:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:121:30:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [121-121]
- **Behavior:** HTTP mock recording refund payloads and throwing `HttpRequestException`.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:133:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:133:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Unauthorized_discount_never_reaches_checkout_api()
- **Lines:** [133-143]
- **Behavior:** Asserts client-side authorization gate preventing unauthorized discounts from dispatching to API.
  - Scenario: Negative security test. User with `pos` role lacking `discounts` permission attempts to apply 10 EGP discount.
  - Production code exercised: `CashierViewModel.SubmitPaymentCommand`, `CashierViewModel.IsDiscountEnabled`, `CashierViewModel.CheckoutError`.
  - Assertions: Zero network calls are dispatched (`calls == 0`) and `CheckoutError` is populated client-side.
  - Gaps: Does not test server-side 403 Forbidden enforcement if client validation is bypassed.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:137:36:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:137:36:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [137-137]
- **Behavior:** HTTP spy tracking API call count, returning 400 BadRequest if invoked.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:145:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:145:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Create(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [145-150]
- **Behavior:** Test fixture factory instantiating `(CashierViewModel, AppSession)` with base address `http://test`, mock user `cashier` holding `all` permissions, and `ShiftId == "shift"`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CashierCorrectnessTests.cs:153:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CashierCorrectnessTests.cs:153:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CashierCorrectnessTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [153-153]
- **Behavior:** Test transport mock delegating HTTP requests directly to the supplied `send` callback.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
