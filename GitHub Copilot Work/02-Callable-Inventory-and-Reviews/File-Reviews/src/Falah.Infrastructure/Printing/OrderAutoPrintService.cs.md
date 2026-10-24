# Substantive Callable Audit: `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`

- **Source File:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `0a7282c6766d2a7b446739d56984950d87ab69f52f88dd3be88f27609484a542`
- **Auditor Specialist:** `printing-workflow-auditor`
- **Total File Callables:** **2**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Service triggered upon order checkout to evaluate automatic ticket printing rules based on order type, payment method, and station settings.

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Printing.OrderAutoPrintService.OrderAutoPrintService(Falah.Infrastructure.Printing.CreatePrintJobsHandler, Falah.Infrastructure.Settings.SettingsQueryService, Microsoft.Extensions.Logging.ILogger<Falah.Infrastructure.Printing.OrderAutoPrintService>)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:35:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `35-41`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Printing.OrderAutoPrintService.OrderAutoPrintService(Falah.Infrastructure.Printing.CreatePrintJobsHandler, Falah.Infrastructure.Settings.SettingsQueryService, Microsoft.Extensions.Logging.ILogger<Falah.Infrastructure.Printing.OrderAutoPrintService>). Manages internal state transition within src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 35-41):**
```csharp
  35 |     public OrderAutoPrintService(CreatePrintJobsHandler createJobs, SettingsQueryService settings,
  36 |         ILogger<OrderAutoPrintService> log)
  37 |     {
  38 |         _createJobs = createJobs;
  39 |         _settings = settings;
  40 |         _log = log;
  41 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Printing.OrderAutoPrintService.TryPrintAsync(Falah.Application.Orders.CheckoutOrderRequest, Falah.Application.Orders.CheckoutOrderResult, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs:46:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `46-94`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FLOW-001`**

**A. Actual Behavior & Execution Path:**  
Evaluates settings and suppresses kitchen printing for electronic pending orders. Completely lacks a mechanism to dispatch prep tickets for unpaid pickup orders.

**B. Trust Boundaries & Validation:**  
CheckoutOrderResult from server.

**C. State Mutations & Side Effects:**  
Calls CreatePrintJobsHandler.HandleAsync.

**D. Concurrency & Transaction Implications:**  
Executes asynchronously after checkout transaction commits.

**E. Specialist Risks & Invariants:**  
FLAW FLOW-001: Branch pickup workflow lacks a 2-stage state machine; kitchen preparation cannot be dispatched without forcing immediate till payment.

> **Verbatim Code Excerpt (Lines 46-94):**
```csharp
  46 |     public async Task<bool> TryPrintAsync(CheckoutOrderRequest request, CheckoutOrderResult result, CancellationToken ct = default)
  47 |     {
  48 |         try
  49 |         {
  50 |             var settings = await _settings.GetAsync(ct);
  51 | 
  52 |             // A station that did not say which customer ticket it prints gets the till's receipt -
  53 |             // the Call Center is the only caller that says otherwise, and it says so explicitly.
  54 |             var customerRoute = settings.AutoPrintReceipt
  55 |                 ? string.IsNullOrWhiteSpace(request.CustomerPrintRoute) ? "Receipt" : request.CustomerPrintRoute
  56 |                 : null;
  57 | 
  58 |             // An electronic transfer the customer says they sent, that nobody has confirmed. Whether
  59 |             // the kitchen starts on it is a decision about how this restaurant runs - plenty start
  60 |             // immediately and plenty do not - so it is a setting, and it defaults to what this
  61 |             // system has always done. The customer's receipt prints either way; they are standing
  62 |             // there. When the transfer is confirmed the ticket can be sent from Order Lookup.
  63 |             var awaitingPayment = result.ElectronicPaymentStatus == "pending"
  64 |                                   || result.ElectronicPaymentStatus == "Pending";
  65 |             var kitchenRoutes = settings.AutoPrintPreparationTicket
  66 |                                 && !(awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed);
  67 | 
  68 |             if (awaitingPayment && settings.HoldPreparationUntilPaymentConfirmed && settings.AutoPrintPreparationTicket)
  69 |                 _log.LogInformation(
  70 |                     "Preparation ticket for order {OrderId} held until the electronic transfer is confirmed.",
  71 |                     result.OrderId);
  72 | 
  73 |             // Both switches off means the restaurant has deliberately turned automatic printing off.
  74 |             // Calling the handler with nothing to print would be an error, so this is where it stops.
  75 |             if (customerRoute is null && !kitchenRoutes) return true;
  76 | 
  77 |             await _createJobs.HandleAsync(new CreatePrintJobsRequest(
  78 |                 RequestId: $"auto-{request.CheckoutRequestId}",
  79 |                 OrderId: result.OrderId,
  80 |                 CustomerRoute: customerRoute,
  81 |                 IsReprint: false,
  82 |                 ActorUserId: request.CashierId,
  83 |                 IncludeKitchenRoutes: kitchenRoutes), ct);
  84 |             return true;
  85 |         }
  86 |         catch (Exception ex)
  87 |         {
  88 |             // Deliberately broad. Nothing this method can hit is worth failing a paid order over,
  89 |             // and the order's tickets remain reprintable from Order Lookup either way.
  90 |             _log.LogError(ex, "Automatic printing failed for order {OrderNumber} ({OrderId}); the sale itself is unaffected.",
  91 |                 result.OrderNumber, result.OrderId);
  92 |             return false;
  93 |         }
  94 |     }
```

---
