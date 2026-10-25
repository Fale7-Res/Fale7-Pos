# File Review: tests/Falah.Client.Tests/CloseoutRegressionTests.cs

## Overview
Substantive client architecture, thread-safety, and UI state audit of tests/Falah.Client.Tests/CloseoutRegressionTests.cs.

## Callables

### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:16:9:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:16:9:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Handler.SendAsync(HttpRequestMessage, CancellationToken)
- **Lines:** [16-16]
- **Behavior:** Test transport mock delegating HTTP request handling directly to the captured `Func<HttpRequestMessage, Task<HttpResponseMessage>>` delegate callback. Enables capturing request URIs, query strings, and serialized JSON payloads without socket I/O.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:18:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:18:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Json(string, System.Net.HttpStatusCode)
- **Lines:** [18-19]
- **Behavior:** Test helper factory instantiating an `HttpResponseMessage` containing UTF-8 JSON content with the specified status code (default `HttpStatusCode.OK`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:20:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:20:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Session(Func<HttpRequestMessage, Task<HttpResponseMessage>>)
- **Lines:** [20-22]
- **Behavior:** Test setup factory constructing an `AppSession` backed by an `ApiClient` configured with base address `http://fixture.invalid`. Seeds `CurrentUser` as manager (`actor`, permissions `["all"]`) and sets `ShiftId = "shift"`.
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:24:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:24:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Pickup_checkout_sends_the_identity_collected_by_the_form()
- **Lines:** [24-44]
- **Behavior:** Integration test asserting pickup order metadata binding in cashier checkout.
  - Scenario: Happy path for pickup order checkout.
  - Production code exercised: `CashierViewModel.SubmitPaymentCommand`, `CashierViewModel.OrderType`, `CashierViewModel.CustomerName`, `CashierViewModel.CustomerPhone`, `CheckoutOrderRequest`.
  - Assertions: Verifies `CheckoutOrderRequest` sent to `/api/orders/checkout` accurately maps customer form fields: `CustomerName == "سارة"`, `CustomerPhone == "01012345678"`, `OrderType == "pickup"`, while delivery-specific fields are cleared (`DeliveryArea == null`, `DeliveryFeePiastres == 0`).
  - Gaps: Does not test validation when customer phone or name contains leading/trailing whitespace or special characters; does not test dine-in (table number) order type mapping.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:28:47:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:28:47:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [28-31]
- **Behavior:** HTTP request interceptor deserializing and capturing `CheckoutOrderRequest` from the request body and returning HTTP 422 to halt sale execution before completion.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:46:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:46:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Uncertain_safe_drop_keeps_the_exact_request_and_blocks_editing()
- **Lines:** [46-66]
- **Behavior:** Asserts safe drop write idempotency and dialog lock during network response loss.
  - Scenario: Error path & recovery. Safe drop submission throws `HttpRequestException("response lost")` simulating dropped connection post-commit.
  - Production code exercised: `ControlCenterViewModel.SubmitSafeDropCommand`, `ControlCenterViewModel.CloseSafeDropCommand`, `ControlCenterViewModel.HasPendingOperation`, `ControlCenterViewModel.CanEditAction`.
  - Assertions: Verifies `HasPendingOperation` is true and `CanEditAction` is false; closing safe drop modal is blocked (`IsSafeDropOpen` remains true); editing `SafeDropAmountText` from "100" to "200" is ignored on resubmit, which transmits the exact original request (`sent[0] == sent[1]`).
  - Gaps: Does not test behavior when server returns definite rejection (e.g. insufficient cash in register drawer).
- **Validations:** Validation checks detected: True. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:50:53:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:50:53:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [50-53]
- **Behavior:** HTTP mock interceptor recording request bodies into `sent` list and throwing `HttpRequestException("response lost")` to simulate connection timeout post-write.
- **Validations:** Validation checks detected: False. Throws: HttpRequestException.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:68:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:68:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Daily_statement_reloads_if_the_date_changes_while_loading()
- **Lines:** [68-88]
- **Behavior:** Asserts asynchronous race condition handling in `DailyStatementViewModel` when date selection changes during an in-flight read.
  - Scenario: User navigation concurrency. User executes `PreviousDayCommand` while the initial day statement is still loading.
  - Production code exercised: `DailyStatementViewModel.LoadAsync`, `DailyStatementViewModel.PreviousDayCommand`, `DailyStatementViewModel.SelectedDate`.
  - Assertions: Coordinates request timing via `TaskCompletionSource`. Verifies that after the initial query completes, a second query is automatically dispatched for the newly selected date (`requests.Count == 2`), query string contains updated `yyyy-MM-dd` date, and `IsLoading` resets to false.
  - Gaps: Does not test multiple rapid date changes in quick succession (debouncing/cancellation token propagation).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:74:54:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:74:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [74-78]
- **Behavior:** HTTP mock interceptor recording query strings, pausing the first request with `TaskCompletionSource`, and returning HTTP 503 ServiceUnavailable.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:89:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:89:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.Daily_statement_never_shows_a_response_for_a_date_no_longer_selected()
- **Lines:** [89-117]
- **Behavior:** Asserts out-of-order asynchronous response filtering in `DailyStatementViewModel`.
  - Scenario: Out-of-order asynchronous completion. In-flight request for previous date completes late with an error response.
  - Production code exercised: `DailyStatementViewModel.LoadAsync`, `DailyStatementViewModel.PreviousDayCommand`, `DailyStatementViewModel.LoadError`.
  - Assertions: Halts request 1 (`"stale-day"`) and request 2 (`"selected-day"`) using `TaskCompletionSource`. Completes request 1 after user stepped date to day 2. Asserts `"stale-day"` error is never surfaced in `vm.LoadError`. Completes request 2 and asserts its error `"selected-day"` is displayed. Guarantees stale reads never corrupt the view state of the active date.
  - Gaps: Does not test stale success responses containing line items from another date.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: true, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:97:54:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:97:54:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [97-105]
- **Behavior:** HTTP mock coordinating two in-flight requests via `TaskCompletionSource` objects and returning distinct error payloads (`"stale-day"` vs `"selected-day"`).
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:121:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:121:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.The_shift_and_expense_reprint_buttons_only_ask_for_a_reprint()
- **Lines:** [121-151]
- **Behavior:** Asserts that document reprint commands on management screens purely request reprints without mutating business state.
  - Scenario: Audit compliance. Reprint buttons for shift open, shift close, and daily expenses are triggered.
  - Production code exercised: `ShiftsViewModel.ReprintShiftOpenCommand`, `ShiftsViewModel.ReprintShiftCloseCommand`, `DailyStatementViewModel.ReprintExpenseCommand`, `ReprintDocumentRequest`.
  - Assertions: Verifies all 3 commands dispatch exclusively to `/api/print-jobs/reprint-document`; payloads contain exact `DocumentKind` (`"ShiftOpen"`, `"ShiftClose"`, `"Expense"`), target `DocumentId` (`"shift-77"`, `"shift-77"`, `"exp-9"`), non-empty reasons, and unique client-minted `RequestId` values per click (3 distinct IDs). Confirms no shift state transitions or ledger adjustments occur.
  - Gaps: Does not test print server offline or printer queue error responses.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: true, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:125:9:LocalFunctionStatement

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:125:9:LocalFunctionStatement
- **Kind:** LocalFunctionStatement
- **Symbol:** Record(HttpRequestMessage)
- **Lines:** [125-130]
- **Behavior:** Local asynchronous helper deserializing `ReprintDocumentRequest`, recording `(Path, Body)` tuples into `calls` collection, and returning mock reprint response (`jobs: [], wasReplayed: false`).
- **Validations:** Validation checks detected: False. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: true

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:145:27:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:145:27:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [145-145]
- **Behavior:** Assertion predicate `c => Assert.Equal("/api/print-jobs/reprint-document", c.Path)` verifying that every reprint invocation targets the designated reprint endpoint.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:146:83:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:146:83:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [146-146]
- **Behavior:** Selector lambda `c => c.Body.DocumentKind` projecting document kind values for sequential assertion against expected kinds.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:147:78:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:147:78:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [147-147]
- **Behavior:** Selector lambda `c => c.Body.DocumentId` projecting document identifiers for equality assertion.
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
- **Behavior:** Selector lambda `c => c.Body.RequestId` projecting request IDs for distinct count evaluation.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: false, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:154:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:154:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.The_open_shift_dialog_offers_the_configured_default_and_sends_what_was_typed()
- **Lines:** [154-182]
- **Behavior:** Asserts shift open modal cash float initialization and custom input submission.
  - Scenario: Shift initialization workflow.
  - Production code exercised: `ShiftsViewModel.OpenOpenShiftModalCommand`, `ShiftsViewModel.ConfirmOpenShiftCommand`, `OpenShiftRequest`.
  - Assertions: Queries `/api/settings` (`defaultOpeningCashPiastres: 150000` = 1,500.00 EGP) and `/api/treasury/balance` (500,000 piastres). Modal displays default formatted float `"1500.00"`. Cashier overrides text with `"1750"`. Confirms that `OpenShiftRequest` sends `175000` piastres (1,750.00 EGP) to `/api/shifts/open`.
  - Gaps: Does not test non-numeric or negative text entry in opening cash textbox.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:158:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:158:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [158-170]
- **Behavior:** HTTP mock routing `/api/settings`, `/api/treasury/balance`, and `/api/shifts/open`, capturing deserialized `OpenShiftRequest` payload.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:186:5:MethodDeclaration

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:186:5:MethodDeclaration
- **Kind:** MethodDeclaration
- **Symbol:** Falah.Client.Tests.CloseoutRegressionTests.The_offered_float_is_capped_by_what_the_main_treasury_holds()
- **Lines:** [186-201]
- **Behavior:** Asserts financial integrity guard when opening cash exceeds main treasury balance.
  - Scenario: Low treasury balance edge case. Default float is 150,000 piastres (1,500 EGP) but main treasury only holds 40,000 piastres (400 EGP).
  - Production code exercised: `ShiftsViewModel.OpenOpenShiftModalCommand`, `ShiftsViewModel.OpeningCashText`.
  - Assertions: Verifies `vm.OpeningCashText` automatically caps the proposed opening float at `"400.00"` EGP so cashiers are not offered more cash than physically exists in treasury.
  - Gaps: Does not test behavior when treasury balance is 0 or negative.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
### Callable: tests/Falah.Client.Tests/CloseoutRegressionTests.cs:189:31:SimpleLambdaExpression

- **Canonical Callable ID:** tests/Falah.Client.Tests/CloseoutRegressionTests.cs:189:31:SimpleLambdaExpression
- **Kind:** SimpleLambdaExpression
- **Symbol:** lambda expression
- **Lines:** [189-195]
- **Behavior:** HTTP mock providing settings with 150,000 piastres default float and treasury balance of 40,000 piastres.
- **Validations:** Validation checks detected: True. Throws: None detected in body.
- **Risk Profile:** Affects Money: true, Affects Permissions: false, Affects Persistence: false

---
