# Substantive Callable Audit: `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`

- **Source File:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `0a7282c6766d2a7b446739d56984950d87ab69f52f88dd3be88f27609484a542`
- **Auditor Role:** Printing & Operational Workflow Specialist
- **Total File Callables:** **2** (1 Constructor + 1 Method)
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

[`OrderAutoPrintService`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs) is the server-side orchestrator that converts a committed order checkout into the required physical tickets across customer and kitchen routing channels.

### Load-Bearing Operational Invariants:
1. **Server-Side Execution Invariant:** Runs on the central server rather than on the client station (cashier terminal or call center desktop). This ensures kitchen preparation tickets do not depend on the client machine surviving the millisecond after checkout confirmation.
2. **Post-Commit Out-of-Transaction Isolation:** Invoked strictly **after** the order checkout database transaction has committed. A downstream printer failure, spooler queue jam, or network timeout cannot roll back or abort a financial transaction where money has already changed hands.
3. **Fail-Safe Exception Suppression:** Catches all exceptions (`catch (Exception ex)`), logs the error with order context, and returns `false`. An operator can always reprint missing tickets from Order Lookup, but failing the order itself would corrupt financial truth.
4. **Idempotency Derivation:** Derives its `RequestId` directly from `request.CheckoutRequestId` (`$"auto-{request.CheckoutRequestId}"`). A replayed checkout request returns the existing print jobs rather than generating duplicate kitchen tickets.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `OrderAutoPrintService.OrderAutoPrintService(CreatePrintJobsHandler, SettingsQueryService, ILogger<OrderAutoPrintService>)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:35:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `35-41`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior, Inputs & Return Values:**  
Constructs the auto-print service with its required dependencies:
- `createJobs` ([`CreatePrintJobsHandler`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs)): The low-level print job entity generator and persister.
- `settings` ([`SettingsQueryService`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Settings/SettingsQueryService.cs)): Query service retrieving restaurant-wide printing toggles (`AutoPrintReceipt`, `AutoPrintPreparationTicket`, `HoldPreparationUntilPaymentConfirmed`).
- `log` (`ILogger<OrderAutoPrintService>`): Diagnostic logger for tracing print queueing events and capturing suppressed exceptions.
Assigns private fields `_createJobs`, `_settings`, and `_log`.

**B. Callers, Dependencies & Side Effects:**  
Injected into `Program.cs` checkout endpoint (`POST /api/orders/checkout` at [`Program.cs:1370`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1370)). Scoped service lifecycle.

**C. Spooler Interaction, Offline Station Buffering & Hardware Invariants:**  
Initializes service dependencies before checkout handling.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Thread-safe dependency assignment within scoped DI container.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Instantiated and tested across checkout integration tests in `OrderCheckoutTests.cs`.
- *Missing Tests:* None.

> **Verbatim Code Excerpt (Lines 35-41):**
```csharp
    public OrderAutoPrintService(CreatePrintJobsHandler createJobs, SettingsQueryService settings,
        ILogger<OrderAutoPrintService> log)
    {
        _createJobs = createJobs;
        _settings = settings;
        _log = log;
    }
```

---

<a id="callable-2"></a>
### Callable 2: `OrderAutoPrintService.TryPrintAsync(CheckoutOrderRequest, CheckoutOrderResult, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:46:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `46-94`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FLOW-001`**

**A. Actual Behavior, Inputs & Return Values:**  
Evaluates configuration rules and queues print jobs for a completed checkout:
1. **Settings Lookup (Line 50):** Fetches restaurant printing configuration via `await _settings.GetAsync(ct)`.
2. **Customer Route Evaluation (Lines 54-56):** If `settings.AutoPrintReceipt` is true:
   - Uses `request.CustomerPrintRoute` if provided; otherwise defaults to `"Receipt"`.
   - If false, sets `customerRoute = null`.
3. **Electronic Payment & Kitchen Routing Decision (Lines 63-66):**
   - Checks if payment is pending: `awaitingPayment = result.ElectronicPaymentStatus == "pending" || result.ElectronicPaymentStatus == "Pending"`.
   - Resolves kitchen printing flag:
     `kitchenRoutes = settings.AutoPrintPreparationTicket && !(awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed)`.
   - If held, logs informational message (Lines 68-71):
     `"Preparation ticket for order {OrderId} held until the electronic transfer is confirmed."`
4. **All-Switches-Off Fast Exit (Line 75):** If `customerRoute is null && !kitchenRoutes`, returns `true` (deliberate configuration, not an error).
5. **Job Creation Dispatch (Lines 77-83):** Calls `_createJobs.HandleAsync` with:
   - `RequestId: $"auto-{request.CheckoutRequestId}"`
   - `OrderId: result.OrderId`
   - `CustomerRoute: customerRoute`
   - `IsReprint: false`
   - `ActorUserId: request.CashierId`
   - `IncludeKitchenRoutes: kitchenRoutes`
6. **Exception Suppression (Lines 86-93):** Catches `Exception ex`, logs error with order details, and returns `false`.
7. **Return Value:** `Task<bool>`. Returns `true` if jobs were queued or disabled; `false` if an unhandled failure occurred.

**B. Callers, Dependencies & Side Effects:**  
Called by `POST /api/orders/checkout` in `Program.cs:1378`. Interacts with `SettingsQueryService`, `CreatePrintJobsHandler`, and EF Core persistence.

**C. Detailed Analysis of Finding FLOW-001 (Two-Stage Electronic Payment Black Hole):**
- **The Workflow Policy:**
  When customers pay via electronic transfer (InstaPay, Vodafone Cash, digital wallet), restaurants often configure `HoldPreparationUntilPaymentConfirmed = true` so cooks do not waste expensive food before money is verified.
- **The Execution at Checkout (Lines 65-66):**
  At checkout time, `result.ElectronicPaymentStatus == "pending"`.
  Because `HoldPreparationUntilPaymentConfirmed` is enabled, `kitchenRoutes` evaluates to `false`.
  The customer receipt is printed, but **no kitchen jobs (`Charcoal`, `Grill`) are queued**.
- **The Operational Black Hole Upon Confirmation:**
  When the customer transfers money and the cashier verifies the bank SMS, the cashier clicks "Confirm Payment", which sends `POST /api/orders/confirm-electronic-payment`.
  Examining `Program.cs:1416-1435`:
  ```csharp
  app.MapPost("/api/orders/confirm-electronic-payment", async (ConfirmElectronicPaymentRequest request, ConfirmElectronicPaymentHandler handler, CancellationToken ct) =>
  {
      var result = await handler.HandleAsync(request, ct);
      return Results.Ok(result);
  });
  ```
  **`ConfirmElectronicPaymentHandler` updates the order status to paid, BUT NEVER TRIGGERS PRINTING!**
  Neither `OrderAutoPrintService` nor `CreatePrintJobsHandler` is invoked by the payment confirmation endpoint.
- **Consequence:**
  The kitchen preparation ticket is **held forever**. The order enters an operational black hole: marked paid in the database, but kitchen cooks never receive the preparation ticket. Food is not cooked until the customer complains at the counter, forcing staff to manually navigate to Order Lookup and click Reprint.

**D. Concurrency, Race Conditions & Transaction Boundaries:**  
Runs outside the checkout database transaction. The idempotency key `$"auto-{request.CheckoutRequestId}"` ensures that network retries of the checkout endpoint do not generate duplicate print jobs.

**E. Test Coverage & Missing Regression Tests:**  
- *Existing Tests:* Covered in `OrderAutoPrintTests.cs` (verifying settings toggles and electronic payment holding).
- *Missing Tests:* End-to-end integration test verifying that confirming an electronic payment triggers kitchen preparation ticket printing.

> **Verbatim Code Excerpt (Lines 46-94):**
```csharp
    public async Task<bool> TryPrintAsync(CheckoutOrderRequest request, CheckoutOrderResult result, CancellationToken ct = default)
    {
        try
        {
            var settings = await _settings.GetAsync(ct);

            // A station that did not say which customer ticket it prints gets the till's receipt -
            // the Call Center is the only caller that says otherwise, and it says so explicitly.
            var customerRoute = settings.AutoPrintReceipt
                ? string.IsNullOrWhiteSpace(request.CustomerPrintRoute) ? "Receipt" : request.CustomerPrintRoute
                : null;

            // An electronic transfer the customer says they sent, that nobody has confirmed. Whether
            // the kitchen starts on it is a decision about how this restaurant runs - plenty start
            // immediately and plenty do not - so it is a setting, and it defaults to what this
            // system has always done. The customer's receipt prints either way; they are standing
            // there. When the transfer is confirmed the ticket can be sent from Order Lookup.
            var awaitingPayment = result.ElectronicPaymentStatus == "pending"
                                  || result.ElectronicPaymentStatus == "Pending";
            var kitchenRoutes = settings.AutoPrintPreparationTicket
                                && !(awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed);

            if (awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed && settings.AutoPrintPreparationTicket)
                _log.LogInformation(
                    "Preparation ticket for order {OrderId} held until the electronic transfer is confirmed.",
                    result.OrderId);

            // Both switches off means the restaurant has deliberately turned automatic printing off.
            // Calling the handler with nothing to print would be an error, so this is where it stops.
            if (customerRoute is null && !kitchenRoutes) return true;

            await _createJobs.HandleAsync(new CreatePrintJobsRequest(
                RequestId: $"auto-{request.CheckoutRequestId}",
                OrderId: result.OrderId,
                CustomerRoute: customerRoute,
                IsReprint: false,
                ActorUserId: request.CashierId,
                IncludeKitchenRoutes: kitchenRoutes), ct);
            return true;
        }
        catch (Exception ex)
        {
            // Deliberately broad. Nothing this method can hit is worth failing a paid order over,
            // and the order's tickets remain reprintable from Order Lookup either way.
            _log.LogError(ex, "Automatic printing failed for order {OrderNumber} ({OrderId}); the sale itself is unaffected.",
                result.OrderNumber, result.OrderId);
            return false;
        }
    }
```

---

## 3. Subsystem Vulnerability & Integrity Analysis

| Finding ID | Vulnerability / Workflow Gap | Root Cause in `OrderAutoPrintService.cs` | Severity | Remediation Strategy |
| :--- | :--- | :--- | :--- | :--- |
| **FLOW-001** | Held Kitchen Tickets Never Printed Upon Payment Confirmation | When `HoldPreparationUntilPaymentConfirmed = true`, `kitchenRoutes` is set to false. However, `ConfirmElectronicPaymentHandler` contains zero printing invocation. The kitchen ticket is lost in a black hole. | **HIGH** | In `ConfirmElectronicPaymentHandler` (or `Program.cs`), invoke `CreatePrintJobsHandler` with `IncludeKitchenRoutes: true` and `RequestId: $"auto-confirm-{request.ConfirmRequestId}"` upon successful electronic payment confirmation. |
| **FLOW-002** | Silent Printing Failures | When `TryPrintAsync` catches an exception, it returns `false`, which sets `PrintJobsQueued = false` in `CheckoutOrderResult`. If the UI ignores this boolean flag, cashier is unaware that tickets failed to queue. | **MEDIUM** | Cashier UI must display a prominent warning banner when `PrintJobsQueued == false`, prompting operator to verify printer or queue manual reprint. |
