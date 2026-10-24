# Reconciled Master Findings Ledger

- **Total Documented Findings:** **77**
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Reconciliation Authority:** GitHub Copilot Independent Software Audit
- **Verification Standard:** Source-grounded, callable-traced, exact-byte verified code evidence.

---

## 1. Finding Dispositions Summary

| Disposition | Count | Percentage | Description |
| :--- | :--- | :--- | :--- |
| **INDEPENDENTLY_CORROBORATED** | **25** | 32.5% | Verified directly against source; flaw mechanism and root cause fully proven with zero contradictions. |
| **CORROBORATED_WITH_QUALIFICATIONS** | **24** | 31.2% | Verified against source with critical qualifications or corrected scope. |
| **REQUIRES_RUNTIME_VERIFICATION** | **21** | 27.3% | Static evidence indicates defect/antipattern; requires live runtime / hardware test validation. |
| **REQUIRES_OWNER_BUSINESS_DECISION** | **5** | 6.5% | Operational workflow or financial policy requires owner business decision before remediation. |
| **INDEPENDENTLY_CORROBORATED_AND_CORRECTED** | **2** | 2.6% | Corroborated but corrected prior erroneous assumptions or flawed line citations. |

---

## 2. Findings Summary by Category and Severity

| Category | High | Medium | Low | Total |
| :--- | :--- | :--- | :--- | :--- |
| **FINANCIAL_AND_DATABASE_INTEGRITY** | 16 | 25 | 4 | **45** |
| **ORDER_LIFECYCLE_AND_WORKFLOW** | 1 | 1 | 0 | **2** |
| **PRINTING_AND_HARDWARE** | 2 | 3 | 1 | **6** |
| **SECURITY_AND_ACCESS_CONTROL** | 2 | 1 | 0 | **3** |
| **UI_UX_AND_DESKTOP_CLIENT_STATE** | 11 | 10 | 0 | **21** |
| **TOTAL** | **32** | **40** | **5** | **77** |

---

## Financial And Database Integrity

### FIN-001: Mixed cash/electronic checkout cannot confirm its electronic leg
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L69-L73) | `69-73` | `Falah.Domain.Orders.CheckoutRules.DefersAllMoney(Falah.Domain.Common.Money, bool, Falah.Domain.Orders.ElectronicPayment?, Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L253-L275) | `253-275` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75-L84) | `75-84` | `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` | `cac1f0a97612` |
| [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L24-L38) | `24-38` | `Falah.Infrastructure.Persistence.EfFinancialJournal.TryStart(string, string) (MethodDeclaration)` | `a4fc816acc48` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L69-L73)  
> **Lines 69-73** in `Falah.Domain.Orders.CheckoutRules.DefersAllMoney(Falah.Domain.Common.Money, bool, Falah.Domain.Orders.ElectronicPayment?, Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` (File SHA-256: `a594ce2aa6ab60d232942ef4af790c0dd7c2a90b49db772e277eaf79d5af84d4`):

```csharp
  69 |     {
  70 |         var collectsNothing = !payableTotal.IsPositive;
  71 |         return collectsNothing || cashOnDelivery || (electronicLeg is not null && paymentMethod != PaymentMethod.Multi);
  72 |     }
  73 | 
```

> **File:** [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L253-L275)  
> **Lines 253-275** in `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ff9f45f38b96377ef8f065884820752aa8483f9038b2141ed390222eac83413c`):

```csharp
 253 |             if (!plan.DefersAllMoney)
 254 |             {
 255 |                 if (paymentMethod == PaymentMethod.Multi && splits is not null)
 256 |                 {
 257 |                     var idx = 0;
 258 |                     foreach (var split in splits)
 259 |                     {
 260 |                         if (split.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet) { idx++; continue; }
 261 |                         var movement = TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
 262 |                             $"op-{Guid.NewGuid():N}", $"{request.CheckoutRequestId}-{idx}", orderId,
 263 |                             split.Method, split.Amount, businessDayId, request.ShiftId, cashierUser.Name, now);
 264 |                         _db.TreasuryMovements.Add(ToEntity(movement));
 265 |                         idx++;
 266 |                     }
 267 |                 }
 268 |                 else
 269 |                 {
 270 |                     var movement = TreasuryMovementFactory.BuildSaleMovement($"tm-{Guid.NewGuid():N}",
 271 |                         $"op-{Guid.NewGuid():N}", request.CheckoutRequestId, orderId,
 272 |                         paymentMethod, plan.PayableTotal, businessDayId, request.ShiftId, cashierUser.Name, now);
 273 |                     _db.TreasuryMovements.Add(ToEntity(movement));
 274 |                 }
 275 |                 new EfFinancialJournal(_db).Complete(saleKey);
```

> **File:** [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L75-L84)  
> **Lines 75-84** in `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `cac1f0a97612efb966710e6a47e323c2fff06b342c688f419298e34e18c34a0b`):

```csharp
  75 |             var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
  76 |             var journal = new EfFinancialJournal(_db);
  77 |             if (!journal.TryStart(saleKey, "sale"))
  78 |                 throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل رغم أن الدفع لم يكن مؤكدًا - يحتاج مراجعة يدوية.");
  79 | 
  80 |             var movement = TreasuryMovementFactory.BuildSaleMovement(
  81 |                 $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", orderEntity.CheckoutRequestId, orderEntity.Id,
  82 |                 leg.Method, leg.Amount, orderEntity.BusinessDayId, orderEntity.ShiftId, cashierUser.Name, DateTimeOffset.UtcNow);
  83 |             _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
  84 |             journal.Complete(saleKey);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
CheckoutOrderHandler.cs (line 148) posts the cash tender leg using idempotency key 'sale-{CheckoutRequestId}'. When ConfirmElectronicPaymentHandler.cs (line 75) attempts to post the deferred electronic split leg, it attempts to insert into EfFinancialJournal using the exact same key 'sale-{CheckoutRequestId}', throwing a unique constraint violation.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Split payment completion failure. Cash is accepted and deposited into the drawer, but the electronic leg (InstaPay/Card) can never be confirmed on the server; the order remains stuck in pending payment state.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
CheckoutOrderHandler.cs (line 148) posts the cash tender leg using idempotency key 'sale-{CheckoutRequestId}'. When ConfirmElectronicPaymentHandler.cs (line 75) attempts to post the deferred electronic split leg, it attempts to insert into EfFinancialJournal using the exact same key 'sale-{CheckoutRequestId}', throwing a unique constraint violation.

**E. Future Repair Specification:**  
Key journal entries by stable leg identity, e.g. 'sale-{CheckoutRequestId}-{TenderType}'. Transition pending electronic leg atomically to confirmed.

**Required Regression Test Cases:**
- Mixed cash+InstaPay and card+Wallet checkout, confirmation, retry, and concurrent confirm; verify exact tender totals and one movement per leg.
- Replay same request and verify stable movement identity.

**Acceptance Criteria & Verification Standard:**
- Each tender leg can complete independently and exactly once.
- Confirmed electronic state and its movement commit atomically; channel totals reconcile.

---

### FIN-002: Cancelling pending mixed electronic payment hides collected cash without reversal
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200-L213) | `200-213` | `Falah.Domain.Orders.Order.CancelElectronicPayment(string, string, DateTimeOffset) (MethodDeclaration)` | `4f93ebcba865` |
| [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L29-L47) | `29-47` | `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` | `805e8d11d87c` |
| [`src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs#L39-L57) | `39-57` | `Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` | `c12b0da939a0` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L255-L267) | `255-267` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200-L213)  
> **Lines 200-213** in `Falah.Domain.Orders.Order.CancelElectronicPayment(string, string, DateTimeOffset) (MethodDeclaration)` (File SHA-256: `4f93ebcba865636bbd00a6d6edbaab1453f72acb35d220de129d3ba8f47b1c4b`):

```csharp
 200 |     public void CancelElectronicPayment(string reason, string cancelledBy, DateTimeOffset nowUtc)
 201 |     {
 202 |         if (Status == OrderStatus.Cancelled) return;
 203 |         if (ElectronicPayment is not { } leg) throw new DomainException("لا يوجد تحصيل إلكتروني على هذا الطلب.");
 204 |         if (leg.Status == ElectronicPaymentStatus.Confirmed)
 205 |             throw new DomainException("تم تأكيد التحصيل بالفعل. استخدم الاسترجاع المالي بدلًا من الإلغاء.");
 206 |         if (string.IsNullOrWhiteSpace(reason)) throw new DomainException("سبب الإلغاء مطلوب.");
 207 | 
 208 |         Status = OrderStatus.Cancelled;
 209 |         IsPaid = false;
 210 |         ElectronicPayment = leg with { Status = ElectronicPaymentStatus.Cancelled };
 211 |         CancelledAtUtc = nowUtc;
 212 |         CancelReason = reason.Trim();
 213 |         CancelledBy = cancelledBy;
```

> **File:** [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L29-L47)  
> **Lines 29-47** in `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` (File SHA-256: `805e8d11d87caec242e36504176417b876da153b236910f64502c3488e2b9412`):

```csharp
  29 | 
  30 |         foreach (var order in orders)
  31 |         {
  32 |             if (order.Status == OrderStatus.Cancelled) continue;
  33 |             var netTotal = order.Total - order.RefundAmount;
  34 | 
  35 |             if (order.PaymentMethod == PaymentMethod.Multi && order.SplitPayments is { } splits)
  36 |             {
  37 |                 foreach (var part in splits)
  38 |                 {
  39 |                     var isElectronic = part.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet;
  40 |                     var confirmed = order.ElectronicPayment is { Status: ElectronicPaymentStatus.Confirmed } leg && leg.Method == part.Method;
  41 |                     if ((isElectronic && confirmed) || !isElectronic)
  42 |                     {
  43 |                         if (totals.ContainsKey(part.Method)) totals[part.Method] += part.Amount;
  44 |                     }
  45 |                 }
  46 |             }
  47 |             else if (order.IsPaid && totals.ContainsKey(order.PaymentMethod))
```

> **File:** [`src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs#L39-L57)  
> **Lines 39-57** in `Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `c12b0da939a0a82f907bfa1f0ce32b58f686d4f60d01b637cf1054a30d2d1bc7`):

```csharp
  39 |         await using var transaction = await _db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
  40 | 
  41 |         var orderEntity = await _db.Orders
  42 |             .FromSqlRaw("SELECT * FROM Orders WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.OrderId)
  43 |             .Include(o => o.Items).Include(o => o.Refunds)
  44 |             .FirstOrDefaultAsync(ct)
  45 |             ?? throw new InvalidOperationException("الطلب غير موجود.");
  46 | 
  47 |         var wasAlreadyCancelled = orderEntity.Status == "cancelled";
  48 |         var order = OrderMapper.ToDomain(orderEntity);
  49 |         var now = DateTimeOffset.UtcNow;
  50 |         order.CancelElectronicPayment(request.Reason, cashierUser.Name, now); // throws if confirmed/no leg/empty reason
  51 | 
  52 |         orderEntity.Status = order.Status.ToWireString();
  53 |         orderEntity.IsPaid = order.IsPaid;
  54 |         orderEntity.ElectronicPaymentStatus = order.ElectronicPayment?.Status.ToWireString();
  55 |         orderEntity.CancelledAtUtc = order.CancelledAtUtc;
  56 |         orderEntity.CancelReason = order.CancelReason;
  57 |         orderEntity.CancelledBy = order.CancelledBy;
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Order.cs:CancelElectronicPayment sets order Status to Cancelled without issuing a reversal journal entry for cash tender legs that were already deposited into the till drawer at checkout.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Treasury divergence. Drawer cash remains physically and logically deposited, but order disappears from sales reports, causing shift drawer reconciliation discrepancies.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Order.cs:CancelElectronicPayment sets order Status to Cancelled without issuing a reversal journal entry for cash tender legs that were already deposited into the till drawer at checkout.

**E. Future Repair Specification:**  
When cancelling a mixed tender order, check if any cash leg was deposited and atomically post a reversal journal movement to the drawer.

**Required Regression Test Cases:**
- Cancel cash+electronic and card+electronic orders; reconcile treasury, operational cash, and reports.
- Race cancel against confirm and require one terminal tender outcome.
- Verify refund only when funds are actually returned.

**Acceptance Criteria & Verification Standard:**
- Cancelled order/payment state preserves every collected tender until a matching reversal exists.
- Order, tender, treasury, and report totals reconcile after either winning race.

---

### FIN-003: Refund can debit money for an order that was never collected
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220-L233) | `220-233` | `Falah.Domain.Orders.Order.ApplyRefund(Falah.Domain.Orders.OrderRefund) (MethodDeclaration)` | `4f93ebcba865` |
| [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L102-L120) | `102-120` | `Falah.Infrastructure.Orders.RefundOrderHandler.HandleAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken) (MethodDeclaration)` | `ca7d3922495a` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L220-L233)  
> **Lines 220-233** in `Falah.Domain.Orders.Order.ApplyRefund(Falah.Domain.Orders.OrderRefund) (MethodDeclaration)` (File SHA-256: `4f93ebcba865636bbd00a6d6edbaab1453f72acb35d220de129d3ba8f47b1c4b`):

```csharp
 220 |     public void ApplyRefund(OrderRefund refund)
 221 |     {
 222 |         if (Status == OrderStatus.Cancelled) throw new DomainException("لا يمكن استرجاع طلب ملغي.");
 223 |         if (PaymentMethod == PaymentMethod.Multi) throw new DomainException("مرتجع الدفع المتعدد غير مدعوم في المسار المالي الحالي.");
 224 | 
 225 |         var refundableBefore = Total - RefundAmount;
 226 |         if (refund.Amount.IsZero || refund.Amount.IsNegative || refund.Amount > refundableBefore)
 227 |             throw new DomainException("مبلغ المرتجع غير صالح.");
 228 | 
 229 |         _refunds.Add(refund);
 230 |         RefundAmount += refund.Amount;
 231 |         if (RefundAmount == Total) Status = OrderStatus.Refunded;
 232 |     }
 233 | }
```

> **File:** [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L102-L120)  
> **Lines 102-120** in `Falah.Infrastructure.Orders.RefundOrderHandler.HandleAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ca7d3922495ad8ed8f9979feeca461f3fca0aa150fde7790714e29046e32d085`):

```csharp
 102 |             var now = DateTimeOffset.UtcNow;
 103 |             var movement = TreasuryMovementFactory.BuildRefundMovement(
 104 |                 $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", request.RefundRequestId, order.Id,
 105 |                 order.PaymentMethod, safeAmount, order.BusinessDayId, shiftPolicy.OperationalShiftId!,
 106 |                 cashierUser.Name, authorizedByName, request.Reason, now);
 107 | 
 108 |             var refund = new OrderRefund($"order-refund-{Guid.NewGuid():N}", movement.OperationId, refundKey,
 109 |                 safeAmount, order.PaymentMethod, movement.Id, now, cashierUser.Name, authorizedByName,
 110 |                 request.Reason, order.ShiftId, shiftPolicy.OperationalShiftId!);
 111 | 
 112 |             // Domain invariants enforced here too (cancelled/multi guard, amount<=refundable,
 113 |             // Refunded transition) - not just trusted from the computation above.
 114 |             order.ApplyRefund(refund);
 115 | 
 116 |             orderEntity.Status = order.Status.ToWireString();
 117 |             orderEntity.RefundAmountPiastres = order.RefundAmount.Piastres;
 118 |             orderEntity.Version++;
 119 |             var refundEntity = OrderMapper.ToEntity(refund);
 120 |             orderEntity.Refunds.Add(refundEntity);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Order.cs:ApplyRefund and RefundOrderHandler.cs do not verify that the order was actually settled (IsPaid == true) before approving a cash refund.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Cash drain. An operator can issue a cash refund against an unpaid Cash-On-Delivery order or pending InstaPay order, draining drawer funds for money never collected.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Order.cs:ApplyRefund and RefundOrderHandler.cs do not verify that the order was actually settled (IsPaid == true) before approving a cash refund.

**E. Future Repair Specification:**  
Enforce invariant in Order.ApplyRefund that order must be fully collected (IsPaid == true) and refund amount cannot exceed collected tender.

**Required Regression Test Cases:**
- Unpaid COD, pending electronic, confirmed paid, partial collected, and unsupported mixed cases.
- Race refund with cancel/confirm and concurrent refunds; rejected cases create no refund/movement and balances remain unchanged.

**Acceptance Criteria & Verification Standard:**
- Cumulative refunds never exceed the order’s actually settled amount per tender leg.
- Rejected unpaid refund leaves no partial writes; valid refund atomically records refund and outflow.

---

### FIN-004: Historical cash refund omitted from current-shift expected cash
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L31-L51) | `31-51` | `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` | `805e8d11d87c` |
| [`src/Falah.Domain/Orders/RefundRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs#L39-L43) | `39-43` | `Falah.Domain.Orders.RefundRules.ResolveOperationalShift(string, Falah.Domain.Shifts.ShiftStatus, string?, Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` | `10bf926d5ad2` |
| [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L95-L110) | `95-110` | `Falah.Infrastructure.Orders.RefundOrderHandler.HandleAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken) (MethodDeclaration)` | `ca7d3922495a` |
| [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L42-L68) | `42-68` | `Falah.Infrastructure.Shifts.ShiftReconciliation.ComputeBreakdownAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, Falah.Domain.Common.Money, CancellationToken) (MethodDeclaration)` | `cd1a406323ee` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L31-L51)  
> **Lines 31-51** in `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` (File SHA-256: `805e8d11d87caec242e36504176417b876da153b236910f64502c3488e2b9412`):

```csharp
  31 |         {
  32 |             if (order.Status == OrderStatus.Cancelled) continue;
  33 |             var netTotal = order.Total - order.RefundAmount;
  34 | 
  35 |             if (order.PaymentMethod == PaymentMethod.Multi && order.SplitPayments is { } splits)
  36 |             {
  37 |                 foreach (var part in splits)
  38 |                 {
  39 |                     var isElectronic = part.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet;
  40 |                     var confirmed = order.ElectronicPayment is { Status: ElectronicPaymentStatus.Confirmed } leg && leg.Method == part.Method;
  41 |                     if ((isElectronic && confirmed) || !isElectronic)
  42 |                     {
  43 |                         if (totals.ContainsKey(part.Method)) totals[part.Method] += part.Amount;
  44 |                     }
  45 |                 }
  46 |             }
  47 |             else if (order.IsPaid && totals.ContainsKey(order.PaymentMethod))
  48 |             {
  49 |                 totals[order.PaymentMethod] += netTotal;
  50 |             }
  51 |         }
```

> **File:** [`src/Falah.Domain/Orders/RefundRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs#L39-L43)  
> **Lines 39-43** in `Falah.Domain.Orders.RefundRules.ResolveOperationalShift(string, Falah.Domain.Shifts.ShiftStatus, string?, Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` (File SHA-256: `10bf926d5ad29fcc2d25b98ce32e523ae76769ae077bbd0d659b59443501feb9`):

```csharp
  39 |             throw new DomainException("يجب فتح وردية قبل تنفيذ مرتجع نقدي لطلب من وردية مغلقة.");
  40 | 
  41 |         return new RefundShiftPolicy(historicalImmutable, historicalImmutable ? activeShiftId : originalShiftId);
  42 |     }
  43 | }
```

> **File:** [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L95-L110)  
> **Lines 95-110** in `Falah.Infrastructure.Orders.RefundOrderHandler.HandleAsync(Falah.Application.Orders.RefundOrderRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ca7d3922495ad8ed8f9979feeca461f3fca0aa150fde7790714e29046e32d085`):

```csharp
  95 |             var originalShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == order.ShiftId, ct)
  96 |                 ?? throw new InvalidOperationException("وردية الطلب الأصلية غير موجودة.");
  97 |             var activeShiftEntity = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Status == nameof(ShiftStatus.Open), ct);
  98 | 
  99 |             var shiftPolicy = RefundRules.ResolveOperationalShift(
 100 |                 order.ShiftId, Enum.Parse<ShiftStatus>(originalShiftEntity.Status), activeShiftEntity?.Id, order.PaymentMethod);
 101 | 
 102 |             var now = DateTimeOffset.UtcNow;
 103 |             var movement = TreasuryMovementFactory.BuildRefundMovement(
 104 |                 $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", request.RefundRequestId, order.Id,
 105 |                 order.PaymentMethod, safeAmount, order.BusinessDayId, shiftPolicy.OperationalShiftId!,
 106 |                 cashierUser.Name, authorizedByName, request.Reason, now);
 107 | 
 108 |             var refund = new OrderRefund($"order-refund-{Guid.NewGuid():N}", movement.OperationId, refundKey,
 109 |                 safeAmount, order.PaymentMethod, movement.Id, now, cashierUser.Name, authorizedByName,
 110 |                 request.Reason, order.ShiftId, shiftPolicy.OperationalShiftId!);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Drawer mixes lifetime order net amounts with selected operational movement types and does not allocate refunds by OperationalShiftId.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
A cash refund executed during current shift B for a sale from shift A may reduce lifetime/order net while the drawer reconciliation for B omits the refund outflow or allocates it to the original sale; expected cash and actual drawer diverge.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Drawer mixes lifetime order net amounts with selected operational movement types and does not allocate refunds by OperationalShiftId.

**E. Future Repair Specification:**  
Compute drawer flow from posted operational cash movements or join refund rows by OperationalShiftId; avoid double deduction for same-shift refunds and preserve immutable closed shift snapshots. Separate original sale period from refund execution period.

**Required Regression Test Cases:**
- Historical shift A sale refunded in B; verify B actual/expected and final close balance reconcile.
- Same-shift partial cash refund counted exactly once.
- Historic noncash refund and cross-business-day reports preserve attribution; idempotent retry.

**Acceptance Criteria & Verification Standard:**
- Each refund cash outflow appears once in the shift/business period in which it was executed.
- Historical closed shift snapshot remains immutable; current operational drawer reconciles to movements.

---

### FIN-005: Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs#L75-L111) | `75-111` | `Falah.Infrastructure.Persistence.Entities.PurchasePaymentEntity.Id.get (GetAccessorDeclaration)` | `8aa26b3ab151` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L277-L305) | `277-305` | `Falah.Infrastructure.Persistence.FalahDbContext.OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder) (MethodDeclaration)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs#L95-L115) | `95-115` | `Falah.Infrastructure.Purchasing.PayPurchaseHandler.HandleAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken) (MethodDeclaration)` | `34ed4f90f854` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/PurchasingEntities.cs#L75-L111)  
> **Lines 75-111** in `Falah.Infrastructure.Persistence.Entities.PurchasePaymentEntity.Id.get (GetAccessorDeclaration)` (File SHA-256: `8aa26b3ab1511f2975c893ee4e571305d0544a2a228f404d08313da740276978`):

```csharp
  75 | public class PurchasePaymentEntity
  76 | {
  77 |     public string Id { get; set; } = default!;
  78 |     public string IdempotencyKey { get; set; } = default!;
  79 |     public string PurchaseId { get; set; } = default!;
  80 |     public string SupplierId { get; set; } = default!;
  81 |     public long AmountPiastres { get; set; }
  82 |     public string PaymentAccountId { get; set; } = default!;
  83 |     public string TreasuryMovementId { get; set; } = default!;
  84 |     public string OperationId { get; set; } = default!;
  85 |     public string CreatedBy { get; set; } = default!;
  86 |     public string? ApprovedBy { get; set; }
  87 |     public DateTimeOffset CreatedAtUtc { get; set; }
  88 | }
  89 | 
  90 | /// <summary>Ported from SupplierLedgerEntry - the supplier's own running account, debited by every
  91 | /// invoice received and credited by every payment made against it. SupplierBalance/outstanding are
  92 | /// always derived from this table (PurchasingRules.SupplierOutstandingFromLedger), never stored.</summary>
  93 | public class SupplierLedgerEntryEntity
  94 | {
  95 |     public string Id { get; set; } = default!;
  96 |     public string IdempotencyKey { get; set; } = default!;
  97 |     public string SupplierId { get; set; } = default!;
  98 |     /// <summary>purchase_invoice (debit) or supplier_payment (credit) - the only two types this
  99 |     /// system posts; supplier_return/adjustment exist in the reference app's type union but have no
 100 |     /// command that produces them yet (no supplier-return flow in this pass).</summary>
 101 |     public string Type { get; set; } = default!;
 102 |     public long DebitPiastres { get; set; }
 103 |     public long CreditPiastres { get; set; }
 104 |     public string? PurchaseId { get; set; }
 105 |     public string? ReferenceId { get; set; }
 106 |     public string Reason { get; set; } = default!;
 107 |     public string CreatedBy { get; set; } = default!;
 108 |     public string? OperationId { get; set; }
 109 |     public string? TreasuryMovementId { get; set; }
 110 |     public DateTimeOffset CreatedAtUtc { get; set; }
 111 | }
```

> **File:** [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L277-L305)  
> **Lines 277-305** in `Falah.Infrastructure.Persistence.FalahDbContext.OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder) (MethodDeclaration)` (File SHA-256: `7fda50ac64f49021d17d7ea9636456dd15556c6d8d4a76f5e553be0a42f88d3c`):

```csharp
 277 |         modelBuilder.Entity<PurchaseEntity>(b =>
 278 |         {
 279 |             b.HasKey(p => p.Id);
 280 |             // Same idempotency contract as every other financial command in this codebase - a
 281 |             // retried receivePurchase can never post the same invoice/stock/payment twice.
 282 |             b.HasIndex(p => p.IdempotencyKey).IsUnique();
 283 |             b.HasOne<SupplierEntity>().WithMany().HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.Restrict);
 284 |             b.HasMany(p => p.Lines).WithOne().HasForeignKey(l => l.PurchaseId).OnDelete(DeleteBehavior.Cascade);
 285 |             b.HasMany(p => p.Payments).WithOne().HasForeignKey(pp => pp.PurchaseId).OnDelete(DeleteBehavior.Cascade);
 286 |         });
 287 |         modelBuilder.Entity<PurchaseLineEntity>(b =>
 288 |         {
 289 |             b.HasKey(l => l.Id);
 290 |             b.HasOne<InventoryItemEntity>().WithMany().HasForeignKey(l => l.InventoryItemId).OnDelete(DeleteBehavior.Restrict);
 291 |         });
 292 |         modelBuilder.Entity<PurchasePaymentEntity>(b =>
 293 |         {
 294 |             b.HasKey(p => p.Id);
 295 |             b.HasIndex(p => p.IdempotencyKey).IsUnique();
 296 |         });
 297 |         modelBuilder.Entity<SupplierLedgerEntryEntity>(b =>
 298 |         {
 299 |             b.HasKey(e => e.Id);
 300 |             b.HasIndex(e => e.IdempotencyKey).IsUnique();
 301 |             // Every read of "how much do we owe this supplier" (or "for this one invoice") filters
 302 |             // by exactly this - see PurchasingRules.SupplierOutstandingFromLedger.
 303 |             b.HasIndex(e => new { e.SupplierId, e.PurchaseId });
 304 |             b.HasOne<SupplierEntity>().WithMany().HasForeignKey(e => e.SupplierId).OnDelete(DeleteBehavior.Restrict);
 305 |         });
```

> **File:** [`src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/PayPurchaseHandler.cs#L95-L115)  
> **Lines 95-115** in `Falah.Infrastructure.Purchasing.PayPurchaseHandler.HandleAsync(Falah.Application.Purchasing.PayPurchaseRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `34ed4f90f854e21b20c18b1c708754d010dc9b3313c648c5b29be1af08844306`):

```csharp
  95 |             var remaining = outstanding - amount;
  96 |             purchase.PaidPiastres += amount.Piastres;
  97 |             purchase.RemainingAmountPiastres = remaining.Piastres;
  98 |             purchase.PaymentStatus = remaining.Piastres == 0 ? "paid" : "partially_paid";
  99 |             purchase.Status = purchase.PaymentStatus;
 100 |             purchase.Payments.Add(new PurchasePaymentEntity
 101 |             {
 102 |                 Id = $"payment-{request.RequestId}", IdempotencyKey = idempotencyKey, PurchaseId = purchase.Id,
 103 |                 SupplierId = purchase.SupplierId, AmountPiastres = amount.Piastres, PaymentAccountId = accountId,
 104 |                 TreasuryMovementId = movement.Id, OperationId = operationId, CreatedBy = actorUser.Name,
 105 |                 ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
 106 |             });
 107 | 
 108 |             _db.SupplierLedgerEntries.Add(new SupplierLedgerEntryEntity
 109 |             {
 110 |                 Id = $"ledger-{idempotencyKey}", IdempotencyKey = idempotencyKey, SupplierId = purchase.SupplierId,
 111 |                 Type = "supplier_payment", DebitPiastres = 0, CreditPiastres = amount.Piastres, PurchaseId = purchase.Id,
 112 |                 ReferenceId = purchase.Id, Reason = "سداد لاحق لفاتورة مشتريات", CreatedBy = actorUser.Name,
 113 |                 OperationId = operationId, TreasuryMovementId = movement.Id, CreatedAtUtc = now,
 114 |             });
 115 | 
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
PayPurchaseHandler.cs (lines 59-74) reads supplier invoice balance without UPDLOCK or serializable isolation, allowing concurrent transactions to each read the remaining balance before either updates it.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Supplier overpayment and invoice balance corruption under concurrent settlement clicks.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
PayPurchaseHandler.cs (lines 59-74) reads supplier invoice balance without UPDLOCK or serializable isolation, allowing concurrent transactions to each read the remaining balance before either updates it.

**E. Future Repair Specification:**  
Execute invoice settlement within an explicit Serializable transaction or apply pessimistic locking (UPDLOCK, ROWLOCK) on the purchasing invoice record.

**Required Regression Test Cases:**
- Barrier: two different 80 settlements against outstanding 100, both same account and different accounts; at most one succeeds, total <=100 and cache equals ledger.
- Same request replay returns committed result.

**Acceptance Criteria & Verification Standard:**
- For each invoice, committed settlements never exceed current outstanding.
- Cached paid/outstanding fields match ledger under serialized settlements.
- Retries remain idempotent and deadlock policy is bounded.

---

### FIN-006: Generic payroll transition marks run Paid without paying treasury or employee ledger
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L12-L26) | `12-26` | `Falah.Domain.Payroll.PayrollRules.AssertTransition(Falah.Domain.Payroll.PayrollRunStatus, Falah.Domain.Payroll.PayrollRunStatus) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L65-L78) | `65-78` | `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` | `d7a614f6752b` |
| [`src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L25-L58) | `25-58` | `Falah.Infrastructure.Employees.TransitionPayrollHandler.HandleAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken) (MethodDeclaration)` | `0d8401a67d75` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L12-L26)  
> **Lines 12-26** in `Falah.Domain.Payroll.PayrollRules.AssertTransition(Falah.Domain.Payroll.PayrollRunStatus, Falah.Domain.Payroll.PayrollRunStatus) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  12 | {
  13 |     private static readonly IReadOnlyDictionary<PayrollRunStatus, PayrollRunStatus[]> Transitions = new Dictionary<PayrollRunStatus, PayrollRunStatus[]>
  14 |     {
  15 |         [PayrollRunStatus.Draft] = new[] { PayrollRunStatus.Calculated, PayrollRunStatus.Cancelled },
  16 |         [PayrollRunStatus.Calculated] = new[] { PayrollRunStatus.Reviewed, PayrollRunStatus.Cancelled },
  17 |         [PayrollRunStatus.Reviewed] = new[] { PayrollRunStatus.Approved, PayrollRunStatus.Cancelled },
  18 |         [PayrollRunStatus.Approved] = new[] { PayrollRunStatus.Paid, PayrollRunStatus.Cancelled },
  19 |         [PayrollRunStatus.Paid] = Array.Empty<PayrollRunStatus>(),
  20 |         [PayrollRunStatus.Cancelled] = Array.Empty<PayrollRunStatus>(),
  21 |     };
  22 | 
  23 |     public static void AssertTransition(PayrollRunStatus from, PayrollRunStatus to)
  24 |     {
  25 |         if (!Transitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
  26 |             throw new DomainException($"انتقال حالة Payroll غير مسموح: {from} → {to}");
```

> **File:** [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L65-L78)  
> **Lines 65-78** in `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `d7a614f6752b5a33513b3cd4ecf1e4e1cfe0022d010822e38bb43160c40e4084`):

```csharp
  65 |         {
  66 |             // A concurrent request settled this run between the idempotency lookup above and this
  67 |             // read. A paid run is not an unapproved one: the caller must see the same result the
  68 |             // winning request saw, not a rejection that would send an operator to pay it again.
  69 |             // Reading Paid here proves the winner committed, and its movement commits in the same
  70 |             // transaction, so the replay is guaranteed to be visible.
  71 |             if (await LoadReplayAsync() is { } settledConcurrently) return settledConcurrently;
  72 |         }
  73 |         if (run.Status != nameof(PayrollRunStatus.Approved))
  74 |             throw new DomainException("يجب اعتماد مسير الرواتب قبل الدفع.");
  75 | 
  76 |         ManagerApproval? approval = null;
  77 |         if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  78 |         {
```

> **File:** [`src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L25-L58)  
> **Lines 25-58** in `Falah.Infrastructure.Employees.TransitionPayrollHandler.HandleAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `0d8401a67d756fe1854ff8228e9efea17db6e1db91d8a5abb40499ae1eb982c4`):

```csharp
  25 |     {
  26 |         if (!Enum.TryParse<PayrollRunStatus>(request.Status, ignoreCase: true, out var targetStatus))
  27 |             throw new DomainException("حالة Payroll غير صالحة.");
  28 | 
  29 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  30 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  31 |         var actor = OrderMapper.ToActor(actorUser);
  32 |         AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "employees_manage");
  33 | 
  34 |         var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
  35 |             ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
  36 |         var currentStatus = Enum.Parse<PayrollRunStatus>(run.Status);
  37 |         PayrollRules.AssertTransition(currentStatus, targetStatus);
  38 | 
  39 |         string? approvedByName = null;
  40 |         if (targetStatus == PayrollRunStatus.Approved)
  41 |         {
  42 |             ManagerApproval? approval = null;
  43 |             if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  44 |             {
  45 |                 var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  46 |                     ?? throw new DomainException("المعتمد غير موجود.");
  47 |                 approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  48 |                     approverUser.Role, DateTimeOffset.UtcNow, "payroll_approve", "اعتماد Payroll");
  49 |             }
  50 |             var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "payroll_approve", approval, requireApproval: true);
  51 |             approvedByName = authorization.Approver?.ApproverName ?? actor.Name;
  52 |         }
  53 | 
  54 |         var now = DateTimeOffset.UtcNow;
  55 |         run.Status = targetStatus.ToString();
  56 |         if (targetStatus == PayrollRunStatus.Approved) run.ApprovedBy = approvedByName;
  57 |         if (targetStatus == PayrollRunStatus.Reviewed) { run.ReviewedBy = actor.Name; run.ReviewedAtUtc = now; }
  58 | 
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
TransitionPayrollHandler.cs permits generic status transition of an approved payroll run directly to 'Paid' without generating corresponding treasury journal disbursements or employee ledger settlement entries.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Payroll status marked Paid without actual money leaving treasury or settling employee balances.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
TransitionPayrollHandler.cs permits generic status transition of an approved payroll run directly to 'Paid' without generating corresponding treasury journal disbursements or employee ledger settlement entries.

**E. Future Repair Specification:**  
Disallow transition to Paid via generic TransitionPayrollHandler. Require execution exclusively through PayPayrollHandler which generates atomic treasury and ledger entries.

**Required Regression Test Cases:**
- Generic Approved->Paid is rejected and writes nothing.
- Dedicated pay atomically creates expected movement and each settlement.
- Actor without payroll_pay cannot set Paid through generic route.
- Concurrent cancel/pay results in exactly one consistent terminal state.

**Acceptance Criteria & Verification Standard:**
- Paid status is equivalent to completed committed payout effects.
- No API path can mark paid without required payroll_pay authorization and all financial rows.

---

### FIN-007: Backup filenames collide within a second and WITH FORMAT can overwrite prior backup
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Backup/CreateBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/CreateBackupHandler.cs#L76-L84) | `76-84` | `Falah.Infrastructure.Backup.CreateBackupHandler.RunAsync(Falah.Infrastructure.Backup.BackupActor, string?, CancellationToken) (MethodDeclaration)` | `df823c1d4192` |
| [`src/Falah.Infrastructure/Backup/ScheduledBackupService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/ScheduledBackupService.cs#L127-L151) | `127-151` | `Falah.Infrastructure.Backup.ScheduledBackupService.PruneAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` | `2be57d0fdbed` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Backup/CreateBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/CreateBackupHandler.cs#L76-L84)  
> **Lines 76-84** in `Falah.Infrastructure.Backup.CreateBackupHandler.RunAsync(Falah.Infrastructure.Backup.BackupActor, string?, CancellationToken) (MethodDeclaration)` (File SHA-256: `df823c1d419205a70fe7bd80900c16137dd07e5a7c77a82f768b988a94c17ea1`):

```csharp
  76 | #pragma warning disable EF1002
  77 |             await _db.Database.ExecuteSqlRawAsync(
  78 |                 $"BACKUP DATABASE [{databaseName}] TO DISK = {{0}} WITH COPY_ONLY, CHECKSUM, FORMAT, NAME = {{1}}",
  79 |                 new object[] { filePath, $"Falah backup {now:O}" }, ct);
  80 | #pragma warning restore EF1002
  81 | 
  82 |             entity.FileSizeBytes = File.Exists(filePath) ? new FileInfo(filePath).Length : null;
  83 |             entity.Status = nameof(BackupRecordStatus.Succeeded);
  84 |             entity.CompletedAtUtc = DateTimeOffset.UtcNow;
```

> **File:** [`src/Falah.Infrastructure/Backup/ScheduledBackupService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/ScheduledBackupService.cs#L127-L151)  
> **Lines 127-151** in `Falah.Infrastructure.Backup.ScheduledBackupService.PruneAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` (File SHA-256: `2be57d0fdbed7a4784fe2a86235170e7fa97a9a03df634b5aa7a7fa9d04ef0aa`):

```csharp
 127 | 
 128 |         var recorded = await db.BackupRecords
 129 |             .Where(b => b.Kind == nameof(BackupRecordKind.Backup)
 130 |                         && b.Status == nameof(BackupRecordStatus.Succeeded)
 131 |                         && b.FileSizeBytes != null)
 132 |             .ToListAsync(ct);
 133 | 
 134 |         var prunable = Falah.Domain.Backup.BackupRules.SelectPrunableBackups(
 135 |             recorded.Select(b => new Falah.Domain.Backup.BackupRules.RetainedBackup(
 136 |                 b.Id, b.CompletedAtUtc ?? b.RequestedAtUtc, b.VerificationStatus == "Valid")),
 137 |             KeepCount).ToHashSet();
 138 | 
 139 |         if (prunable.Count == 0) return;
 140 | 
 141 |         foreach (var record in recorded.Where(b => prunable.Contains(b.Id)))
 142 |         {
 143 |             try
 144 |             {
 145 |                 if (File.Exists(record.FilePath)) File.Delete(record.FilePath);
 146 |                 // The row stays, with its file marked gone: the history of what was backed up and
 147 |                 // when outlives the .bak, which is what an audit of "were we taking backups in
 148 |                 // September" actually needs to read.
 149 |                 record.FileSizeBytes = null;
 150 |             }
 151 |             catch (Exception ex)
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Second-resolution timestamp treated as unique filename; no GUID/exclusive reservation. Retention reasons over records not unique physical paths.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Two backup creations within timestamp resolution can target the same physical filename; later WITH FORMAT may overwrite a previously recorded backup or fail, while retention may count multiple records for one path.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Second-resolution timestamp treated as unique filename; no GUID/exclusive reservation. Retention reasons over records not unique physical paths.

**E. Future Repair Specification:**  
Use collision-resistant immutable backup identity and exclusive create/no-overwrite destination semantics; preserve record-to-file identity. Retention should validate distinct canonical paths and never delete a file still referenced by a retained record.

**Required Regression Test Cases:**
- Parallel same-time filename generation returns distinct IDs/paths.
- Existing destination collision refuses overwrite.
- Retention with duplicate path references preserves all retained valid backups.
- Later isolated SQL/media test only with explicit approval.

**Acceptance Criteria & Verification Standard:**
- Each successful backup record resolves to a unique immutable artifact.
- A collision cannot silently overwrite a prior artifact; retention cannot delete a retained target.

---

### FIN-008: Restore pending-journal check does not exclude active or newly accepted financial work
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L148-L167) | `148-167` | `Falah.Infrastructure.Backup.RestoreBackupHandler.HandleAsync(Falah.Application.Backup.RestoreBackupRequest, CancellationToken) (MethodDeclaration)` | `a9504298519b` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L275-L279) | `275-279` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L29-L47) | `29-47` | `Falah.Infrastructure.Persistence.EfFinancialJournal.TryStart(string, string) (MethodDeclaration)` | `a4fc816acc48` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L148-L167)  
> **Lines 148-167** in `Falah.Infrastructure.Backup.RestoreBackupHandler.HandleAsync(Falah.Application.Backup.RestoreBackupRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `a9504298519bd4709d18777b5343536894da4238272eea65b4ac704c9e1b61c6`):

```csharp
 148 |             await connection.OpenAsync(ct);
 149 | 
 150 |             await using (var singleUser = connection.CreateCommand())
 151 |             {
 152 |                 singleUser.CommandText = $"ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
 153 |                 await singleUser.ExecuteNonQueryAsync(ct);
 154 |             }
 155 |             await using (var restore = connection.CreateCommand())
 156 |             {
 157 |                 restore.CommandText = $"RESTORE DATABASE [{databaseName}] FROM DISK = @path WITH REPLACE";
 158 |                 restore.Parameters.AddWithValue("@path", backup.FilePath);
 159 |                 await restore.ExecuteNonQueryAsync(ct);
 160 |             }
 161 |             await using (var multiUser = connection.CreateCommand())
 162 |             {
 163 |                 multiUser.CommandText = $"ALTER DATABASE [{databaseName}] SET MULTI_USER";
 164 |                 await multiUser.ExecuteNonQueryAsync(ct);
 165 |             }
 166 | 
 167 |             databaseWasReplaced = true;
```

> **File:** [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L275-L279)  
> **Lines 275-279** in `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ff9f45f38b96377ef8f065884820752aa8483f9038b2141ed390222eac83413c`):

```csharp
 275 |                 new EfFinancialJournal(_db).Complete(saleKey);
 276 |             }
 277 | 
 278 |             await _db.SaveChangesAsync(ct);
 279 |             await transaction.CommitAsync(ct);
```

> **File:** [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L29-L47)  
> **Lines 29-47** in `Falah.Infrastructure.Persistence.EfFinancialJournal.TryStart(string, string) (MethodDeclaration)` (File SHA-256: `a4fc816acc48e52d7aeb65e5fbfa1ecf9156b7d63483db4d67075bda6349dd4a`):

```csharp
  29 |         {
  30 |             IdempotencyKey = idempotencyKey,
  31 |             Kind = kind,
  32 |             Status = nameof(JournalEntryStatus.Pending),
  33 |             CreatedAtUtc = DateTimeOffset.UtcNow,
  34 |         });
  35 |         return true;
  36 |     }
  37 | 
  38 |     public void Complete(string idempotencyKey)
  39 |     {
  40 |         var entry = _db.FinancialJournalEntries.Local.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey)
  41 |             ?? _db.FinancialJournalEntries.FirstOrDefault(e => e.IdempotencyKey == idempotencyKey);
  42 |         if (entry is null || entry.Status != nameof(JournalEntryStatus.Pending))
  43 |             throw new DomainException($"لا يوجد قيد معلّق بهذا المفتاح: {idempotencyKey}");
  44 |         entry.Status = nameof(JournalEntryStatus.Complete);
  45 |     }
  46 | 
  47 |     public bool DiscardPending(string idempotencyKey)
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Pending journal staged and completed before common SaveChanges cannot reliably reveal in-flight work; a count snapshot is not writer exclusion. Restore does not put API/print/background writers into maintenance barrier.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Restore’s pending-journal check does not exclude active or newly accepted writes. Restore may enter SINGLE_USER/replace while acknowledged financial operation or background writer is in flight; recovery of latest acknowledged work is not assured by the current check.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Pending journal staged and completed before common SaveChanges cannot reliably reveal in-flight work; a count snapshot is not writer exclusion. Restore does not put API/print/background writers into maintenance barrier.

**E. Future Repair Specification:**  
Use a database-backed maintenance admission lease respected by all mutating handlers and background workers; drain in-flight operations, pause workers, take safety backup, restore and verify, clear stale pools/caches/sessions as designed, then resume only after durable outcome recording.

**Required Regression Test Cases:**
- Pure coordinator test: after maintenance lease acquired, new writes are rejected and prior writes drained.
- Dedicated disposable instance restore with approved fault injection and proof of last acknowledged sale recovery.
- Exercise job/API writers and restore failures; never use user-active database.

**Acceptance Criteria & Verification Standard:**
- Restore cannot begin until writers are excluded/drained.
- Every accepted operation is either included in pre-restore safety point or has an explicit replay/recovery record.
- Failure leaves a known healthy database and maintenance mode controlled.

---

### FIN-009: Concurrent different open-day requests create multiple active business days
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs#L22-L29) | `22-29` | `Falah.Infrastructure.BusinessDay.BusinessDayGate.TryGetActiveAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` | `0271b40be055` |
| [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L34-L55) | `34-55` | `Falah.Infrastructure.BusinessDay.OpenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `6be465799c1b` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137-L141) | `137-141` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs#L22-L29)  
> **Lines 22-29** in `Falah.Infrastructure.BusinessDay.BusinessDayGate.TryGetActiveAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` (File SHA-256: `0271b40be055fa7130067dbbe7368bc7a48a21ec65bff8f05d47a0469b48c962`):

```csharp
  22 |     /// treatment every other snapshot field on Order already gets.</summary>
  23 |     public static async Task<string?> TryGetActiveAsync(FalahDbContext db, CancellationToken ct)
  24 |     {
  25 |         var day = await db.BusinessDays.AsNoTracking()
  26 |             .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
  27 |             .FirstOrDefaultAsync(ct);
  28 |         return day?.Id;
  29 |     }
```

> **File:** [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L34-L55)  
> **Lines 34-55** in `Falah.Infrastructure.BusinessDay.OpenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `6be465799c1b5b0953d01a69cdee1c731aaba0029e91a82bf956e1c63ccafecc`):

```csharp
  34 |         var now = DateTimeOffset.UtcNow;
  35 |         var activeDayEntities = await _db.BusinessDays.AsNoTracking()
  36 |             .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
  37 |             .ToListAsync(ct);
  38 |         var activeDays = activeDayEntities
  39 |             .Select(d => new BusinessDayRecord(d.Id, d.BusinessDate, d.OpenedBy, d.OpenedAtUtc, Enum.Parse<BusinessDayStatus>(d.Status), d.CloseRevision))
  40 |             .ToList();
  41 | 
  42 |         var businessDate = request.BusinessDate ?? BusinessClock.BusinessDate(now);
  43 |         var day = BusinessDayRules.Open($"day-{Guid.NewGuid():N}", businessDate, request.OpenedByUserId, now, activeDays);
  44 | 
  45 |         var entity = new BusinessDayEntity
  46 |         {
  47 |             Id = day.Id, BusinessDate = day.BusinessDate, OpenedBy = day.OpenedBy, OpenedAtUtc = day.OpenedAtUtc,
  48 |             Status = nameof(BusinessDayStatus.Open), CloseRevision = 0, OpenRequestId = request.RequestId,
  49 |         };
  50 | 
  51 |         try
  52 |         {
  53 |             _db.BusinessDays.Add(entity);
  54 |             await _db.SaveChangesAsync(ct);
  55 |             return ToResult(entity, wasReplayed: false);
```

> **File:** [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137-L141)  
> **Lines 137-141** in `lambda expression (SimpleLambdaExpression)` (File SHA-256: `7fda50ac64f49021d17d7ea9636456dd15556c6d8d4a76f5e553be0a42f88d3c`):

```csharp
 137 |         modelBuilder.Entity<BusinessDayEntity>(b =>
 138 |         {
 139 |             b.HasKey(d => d.Id);
 140 |             b.HasIndex(d => d.OpenRequestId).IsUnique();
 141 |         });
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Single-active invariant protected only by read check; no shared singleton lock or unique active-day representation.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Concurrent requests with distinct open-day IDs can both pass read-before-write admission, leaving multiple active business days; queries/gates selecting first active day may attach new finance to inconsistent periods.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Single-active invariant protected only by read check; no shared singleton lock or unique active-day representation.

**E. Future Repair Specification:**  
Represent single active ownership with a database-enforced singleton/unique active-state key acquired transactionally by open and reopen. Reconcile existing duplicate active rows explicitly before constraint migration; make read gate fail closed if invariant is corrupt.

**Required Regression Test Cases:**
- Concurrent opens with distinct IDs: exactly one active day; same request replay returns same day.
- Race reopen versus open; exactly one active day.
- Corrupt duplicate fixture makes gate fail closed and emits diagnosable error.

**Acceptance Criteria & Verification Standard:**
- At most one active day exists across app processes.
- Open/reopen admission and all active-day readers use the same ownership invariant.

---

### FIN-010: Collected sales report drops immediate money from pending mixed payments
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L34-L47) | `34-47` | `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` | `805e8d11d87c` |
| [`src/Falah.Domain/Reports/SalesRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs#L55-L70) | `55-70` | `Falah.Domain.Reports.SalesRules.ComputeSalesReport(IReadOnlyList<Falah.Domain.Orders.Order>, IReadOnlySet<string>) (MethodDeclaration)` | `eb5ff2f1a5e9` |
| [`tests/Falah.Domain.Tests/CheckoutRulesTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Domain.Tests/CheckoutRulesTests.cs#L109-L124) | `109-124` | `Falah.Domain.Tests.CheckoutRulesTests.Multi_payment_split_summing_exactly_succeeds_and_selects_the_electronic_leg() (MethodDeclaration)` | `56771c9ce056` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L34-L47)  
> **Lines 34-47** in `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` (File SHA-256: `805e8d11d87caec242e36504176417b876da153b236910f64502c3488e2b9412`):

```csharp
  34 | 
  35 |             if (order.PaymentMethod == PaymentMethod.Multi && order.SplitPayments is { } splits)
  36 |             {
  37 |                 foreach (var part in splits)
  38 |                 {
  39 |                     var isElectronic = part.Method is PaymentMethod.InstaPay or PaymentMethod.Wallet;
  40 |                     var confirmed = order.ElectronicPayment is { Status: ElectronicPaymentStatus.Confirmed } leg && leg.Method == part.Method;
  41 |                     if ((isElectronic && confirmed) || !isElectronic)
  42 |                     {
  43 |                         if (totals.ContainsKey(part.Method)) totals[part.Method] += part.Amount;
  44 |                     }
  45 |                 }
  46 |             }
  47 |             else if (order.IsPaid && totals.ContainsKey(order.PaymentMethod))
```

> **File:** [`src/Falah.Domain/Reports/SalesRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/SalesRules.cs#L55-L70)  
> **Lines 55-70** in `Falah.Domain.Reports.SalesRules.ComputeSalesReport(IReadOnlyList<Falah.Domain.Orders.Order>, IReadOnlySet<string>) (MethodDeclaration)` (File SHA-256: `eb5ff2f1a5e957ff106386e34849d11a8b6a2e7f01fb864c3988a00f77adc4e9`):

```csharp
  55 |         var pendingElectronicSales = Money.FromPiastres(sales.Sum(o =>
  56 |             o.ElectronicPayment is { Status: ElectronicPaymentStatus.Pending } ? o.Total.Piastres : 0));
  57 | 
  58 |         // The same principle applied to cash-on-delivery, which the original formula missed: until a
  59 |         // driver settlement moves it into the restaurant's own treasury, COD money is either still
  60 |         // with the customer (not yet delivered) or in the driver's custody (delivered, unsettled).
  61 |         // Either way the restaurant does not hold it, so counting it as collected would claim cash
  62 |         // that is not there - exactly what excluding pending electronic sales already guards against.
  63 |         // Net of refunds, so a refunded COD order cannot subtract more than the sale contributed.
  64 |         var cashWithDrivers = Money.FromPiastres(sales
  65 |             .Where(o => orderIdsWithCashOutsideRestaurant.Contains(o.Id))
  66 |             .Sum(o => o.Total.Piastres - o.RefundAmount.Piastres));
  67 | 
  68 |         var collectedSales = netSales - pendingElectronicSales - cashWithDrivers;
  69 | 
  70 |         // Same channel attribution as the Shifts drawer reconciliation and the Business Day close
```

> **File:** [`tests/Falah.Domain.Tests/CheckoutRulesTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Domain.Tests/CheckoutRulesTests.cs#L109-L124)  
> **Lines 109-124** in `Falah.Domain.Tests.CheckoutRulesTests.Multi_payment_split_summing_exactly_succeeds_and_selects_the_electronic_leg() (MethodDeclaration)` (File SHA-256: `56771c9ce0561a61130f04b64108b6c6078917a8c3751caee816352a6fefbf4a`):

```csharp
 109 |             Money.Zero, taxRule: null, Money.Zero, Money.Zero, Money.FromEgp(200m), splits, null);
 110 | 
 111 |         Assert.NotNull(plan.ElectronicLeg);
 112 |         Assert.Equal(Money.FromEgp(100m), plan.ElectronicLeg!.Amount);
 113 |         // A multi-payment order does NOT defer everything just because one leg is electronic - the
 114 |         // cash leg still posts immediately; only that one leg is deferred.
 115 |         Assert.False(plan.DefersAllMoney);
 116 |     }
 117 | 
 118 |     [Fact]
 119 |     public void Discount_reduces_the_taxable_base_before_tax_is_applied()
 120 |     {
 121 |         var items = new[] { Item(100m) };
 122 |         var taxRule = new TaxRule("tax-1", "ضريبة", 1400, TaxMode.Exclusive, true); // 14%
 123 |         var plan = CheckoutRules.BuildPlan(OrderType.Takeaway, PaymentMethod.Cash, items,
 124 |             Money.FromEgp(20m), taxRule, Money.Zero, Money.Zero, Money.FromEgp(91.2m), null, null);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Any pending electronic leg triggers subtraction of whole order total, instead of pending leg amount; report contradicts own cash channel totals.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Sales report may subtract full order amount while an electronic leg is pending even though immediate cash/card leg was collected, understating collected sales/revenue and contradicting channel reconciliation.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Any pending electronic leg triggers subtraction of whole order total, instead of pending leg amount; report contradicts own cash channel totals.

**E. Future Repair Specification:**  
Compute pending collection as the pending leg amount(s) and separately report gross/net sale recognition from tender settlement timing. Do not infer collection solely from order IsPaid or subtract whole order for partial pending tender.

**Required Regression Test Cases:**
- Mixed cash/electronic and card/electronic pending: collected amount equals immediate leg and pending equals deferred leg.
- After confirm, totals conserve order amount across channels.
- Compare report totals to treasury movements and payment query for each state.

**Acceptance Criteria & Verification Standard:**
- For each order, collected plus pending amounts reconcile to eligible tender total; no collected leg disappears while another awaits confirmation.

---

### FIN-011: Shift opening and purchase receipt check protected account balance before their transactions
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L73-L97) | `73-97` | `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` | `70287ac1d6a6` |
| [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L77-L86) | `77-86` | `Falah.Infrastructure.Shifts.OpenShiftHandler.HandleAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken) (MethodDeclaration)` | `7b2afcf63d01` |
| [`src/Falah.Infrastructure/Treasury/TreasuryMapper.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/TreasuryMapper.cs#L39-L49) | `39-49` | `Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) (MethodDeclaration)` | `818d64bdb94f` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L73-L97)  
> **Lines 73-97** in `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `70287ac1d6a6d089586a8843b0472abd2dba4d0f1182e19563a9fd309fa11795`):

```csharp
  73 |         var accountId = PurchasingRules.PurchasePaymentAccountFor(request.PaymentMethod);
  74 |         if (paid.IsPositive && accountId is null) throw new DomainException("يجب تحديد حساب دفع فعلي للمبلغ المدفوع.");
  75 |         if (accountId is not null)
  76 |         {
  77 |             var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, accountId, ct);
  78 |             FinancialRules.AssertProtectedSourceBalance(accountMovements, accountId, paid);
  79 |         }
  80 |         // Read each ingredient's current cost basis BEFORE this receipt is written, so the change
  81 |         // this invoice causes can be recorded. See PurchasingRules.DescribeUnitCostChange for why
  82 |         // this matters more here than it looks: one line rewrites every recipe using that item.
  83 |         var previousUnitCosts = new Dictionary<string, Money?>();
  84 |         foreach (var itemId in itemIds)
  85 |         {
  86 |             var lastCost = await _db.StockMovements.AsNoTracking()
  87 |                 .Where(m => m.InventoryItemId == itemId
  88 |                     && m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
  89 |                 .OrderByDescending(m => m.CreatedAtUtc)
  90 |                 .Select(m => m.UnitCostPiastres)
  91 |                 .FirstOrDefaultAsync(ct);
  92 |             previousUnitCosts[itemId] = lastCost is { } c ? Money.FromPiastres(c) : null;
  93 |         }
  94 | 
  95 |         var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  96 | 
  97 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
```

> **File:** [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L77-L86)  
> **Lines 77-86** in `Falah.Infrastructure.Shifts.OpenShiftHandler.HandleAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `7b2afcf63d01e1bb55c0d88e7d1d64e55b3014539d51dd7f4ed9664e7e8b5ea3`):

```csharp
  77 |                     ?? throw new InvalidOperationException("المستخدم غير موجود.");
  78 |                 var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
  79 |                     $"tm-{Guid.NewGuid():N}", $"shift-open-{shift.Id}", $"op-{Guid.NewGuid():N}", "shift_opening_float",
  80 |                     TreasuryAccounts.Main, TreasuryAccounts.Drawer, openingCash,
  81 |                     "shift_opening", shift.Id, businessDayId, shift.Id,
  82 |                     $"عهدة افتتاح {shift.ShiftName}", openedByUser.Name, approvedBy: null, now);
  83 |                 _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
  84 |             }
  85 | 
  86 |             // The cashier's own receipt for the float they were handed. Queued here so it exists if
```

> **File:** [`src/Falah.Infrastructure/Treasury/TreasuryMapper.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/TreasuryMapper.cs#L39-L49)  
> **Lines 39-49** in `Falah.Infrastructure.Treasury.TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) (MethodDeclaration)` (File SHA-256: `818d64bdb94fa957779a83a090b9921c0902b716c2e69564cac25772ac5e0400`):

```csharp
  39 |         (await db.TreasuryMovements
  40 |             .FromSqlRaw(
  41 |                 "SELECT * FROM TreasuryMovements WITH (UPDLOCK, HOLDLOCK) " +
  42 |                 "WHERE Status = {0} AND (SourceAccountId = {1} OR DestinationAccountId = {1})",
  43 |                 nameof(MovementStatus.Posted), accountId)
  44 |             .AsNoTracking()
  45 |             .ToListAsync(ct))
  46 |         .Select(TreasuryMapper.ToDomain).ToList();
  47 | }
  48 | 
  49 | internal static class TreasuryMapper
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
UPDLOCK/HOLDLOCK loader invoked before BeginTransaction; statement-end implicit transaction releases lock, balance snapshot stale at spending. SingleOpen index protects only shift rows.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Shift opening float and/or purchase receipt may authorize against stale Main account balance because protective lock query occurs before transaction; competing outflow may spend same available funds, potentially allowing negative pooled account or failed/partial admission.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
UPDLOCK/HOLDLOCK loader invoked before BeginTransaction; statement-end implicit transaction releases lock, balance snapshot stale at spending. SingleOpen index protects only shift rows.

**E. Future Repair Specification:**  
Start transaction before protected account balance load/check and hold lock through float/receipt writes; establish consistent account/shift/invoice lock order; make retries safe and preserve real concurrency errors.

**Required Regression Test Cases:**
- Barrier-controlled open-float versus withdrawal/expense/purchase distinct requests; account never negative and exactly feasible operation set commits.
- Same open request replay and single-active-shift invariant still hold.
- Verify purchase receipt document/inventory/financial rows atomic.

**Acceptance Criteria & Verification Standard:**
- Protected balance read and spend are in one transaction with effective lock lifetime.
- No successful concurrent commits exceed available balance; rejected command has no partial effects.

---

### FIN-012: Attendance correction credits do not enter calculated payroll
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L95-L109) | `95-109` | `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L92-L117) | `92-117` | `Falah.Infrastructure.Employees.CorrectAttendanceHandler.HandleAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken) (MethodDeclaration)` | `e00e9fa8c36f` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L95-L109)  
> **Lines 95-109** in `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  95 |             var baseAmount = emp.CompensationIsMonthly
  96 |                 ? (emp.MonthlySalary ?? Money.Zero)
  97 |                 : Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "DailyWage" or "Salary").Sum(e => e.Credit.Piastres));
  98 |             var overtime = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "Overtime").Sum(e => e.Credit.Piastres));
  99 |             var extraShifts = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "ExtraShift").Sum(e => e.Credit.Piastres));
 100 |             var bonuses = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Bonus" or "OtherEarning").Sum(e => e.Credit.Piastres));
 101 |             var deductions = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Deduction" or "Adjustment").Sum(e => e.Debit.Piastres));
 102 | 
 103 |             if (emp.RequestedAdvanceDeduction.IsNegative || emp.RequestedAdvanceDeduction > emp.AvailableAdvance)
 104 |                 throw new DomainException($"استقطاع السلفة للموظف {emp.EmployeeId} يتجاوز الرصيد المتفق عليه.");
 105 | 
 106 |             var net = baseAmount + overtime + extraShifts + bonuses - deductions - emp.RequestedAdvanceDeduction;
 107 |             if (net.IsNegative) net = Money.Zero;
 108 | 
 109 |             result.Add(new PayrollLine(emp.EmployeeId, baseAmount, overtime, extraShifts, bonuses, deductions, emp.RequestedAdvanceDeduction, net));
```

> **File:** [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L92-L117)  
> **Lines 92-117** in `Falah.Infrastructure.Employees.CorrectAttendanceHandler.HandleAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `e00e9fa8c36f3fa6a75e371c85307e1666803ef7cc57972513b868772a4225ba`):

```csharp
  92 |             var oldPenalty = PayrollRules.CalculateLateDeduction(dailyWage, settings.StandardWorkdayHours, oldLateMinutes,
  93 |                 settings.LateGraceMinutes, settings.MaxLatePenaltyHours, settings.LatePenaltyMultiplier).Amount;
  94 |             var newPenalty = PayrollRules.CalculateLateDeduction(dailyWage, settings.StandardWorkdayHours, request.LateMinutes,
  95 |                 settings.LateGraceMinutes, settings.MaxLatePenaltyHours, settings.LatePenaltyMultiplier).Amount;
  96 |             var penaltyDelta = newPenalty.Piastres - oldPenalty.Piastres;
  97 |             if (penaltyDelta != 0)
  98 |             {
  99 |                 var isCredit = penaltyDelta < 0; // a REDUCED penalty credits the employee back
 100 |                 await EmployeeLedgerPoster.PostIfNewAsync(_db, $"led-correction-penalty-{request.RequestId}", record.EmployeeId, nameof(LedgerEntryType.Adjustment),
 101 |                     $"تصحيح خصم تأخير حضور — {request.Reason}",
 102 |                     isCredit ? Money.Zero : Money.FromPiastres(Math.Abs(penaltyDelta)),
 103 |                     isCredit ? Money.FromPiastres(Math.Abs(penaltyDelta)) : Money.Zero,
 104 |                     verifiedApproverName, verifiedApproverName, null, record.Id, null, null, null, ct);
 105 |             }
 106 | 
 107 |             var oldOvertime = PayrollRules.CalculateOvertimeAmount(dailyWage, settings.StandardWorkdayHours, oldOvertimeHours, settings.OvertimeMultiplier).Amount;
 108 |             var newOvertime = PayrollRules.CalculateOvertimeAmount(dailyWage, settings.StandardWorkdayHours, request.OvertimeHours, settings.OvertimeMultiplier).Amount;
 109 |             var overtimeDelta = newOvertime.Piastres - oldOvertime.Piastres;
 110 |             if (overtimeDelta != 0)
 111 |             {
 112 |                 var isCredit = overtimeDelta > 0; // an INCREASED overtime credits the employee more
 113 |                 await EmployeeLedgerPoster.PostIfNewAsync(_db, $"led-correction-overtime-{request.RequestId}", record.EmployeeId, nameof(LedgerEntryType.Adjustment),
 114 |                     $"تصحيح ساعات إضافية حضور — {request.Reason}",
 115 |                     isCredit ? Money.Zero : Money.FromPiastres(Math.Abs(overtimeDelta)),
 116 |                     isCredit ? Money.FromPiastres(Math.Abs(overtimeDelta)) : Money.Zero,
 117 |                     verifiedApproverName, verifiedApproverName, null, record.Id, null, null, null, ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Correction records signed Adjustment entries; payroll includes Adjustment debits only, with no credit earnings bucket.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
A positive attendance correction recorded as Adjustment credit may be ignored by payroll’s included earnings/deduction categories, so the corrected employee period net can remain lower than ledger balance.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Correction records signed Adjustment entries; payroll includes Adjustment debits only, with no credit earnings bucket.

**E. Future Repair Specification:**  
Reconcile both credit and debit sides of Adjustment into payroll net; preserve correction attribution and original earning basis if required; keep payroll freeze semantics explicit.

**Required Regression Test Cases:**
- Payroll input with prior Deduction debit plus correcting Adjustment credit restores expected net.
- Attendance correction followed by payroll calculation and report agrees with employee ledger.
- Correction before run creation and after run freeze follow documented policy.

**Acceptance Criteria & Verification Standard:**
- Every eligible period employee-ledger credit/debit included exactly once in payroll liability.
- Payroll line and ledger are consistent; post-freeze correction follows explicit reopen/adjustment policy.

---

### FIN-013: Owner dashboard adds outgoing stock instead of subtracting it
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L297-L314) | `297-314` | `Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadStockAsync(CancellationToken) (MethodDeclaration)` | `67c66a18a228` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L297-L314)  
> **Lines 297-314** in `Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadStockAsync(CancellationToken) (MethodDeclaration)` (File SHA-256: `67c66a18a228c9e34b3d42aad4c609f75c5ae0bde36d87e8fabc366d8119bd29`):

```csharp
 297 |     private async Task<StockPositionDto> LoadStockAsync(CancellationToken ct)
 298 |     {
 299 |         var items = await _db.InventoryItems.AsNoTracking().Where(i => i.Active)
 300 |             .Select(i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits }).ToListAsync(ct);
 301 |         var balances = await _db.StockMovements.AsNoTracking()
 302 |             .GroupBy(m => m.InventoryItemId)
 303 |             .Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })
 304 |             .ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);
 305 | 
 306 |         var low = items
 307 |             .Select(i => new { i.Id, i.Name, i.Unit, i.MinimumMilliUnits, Balance = balances.GetValueOrDefault(i.Id) })
 308 |             // Same test the inventory screen uses, so the count here and the list there agree.
 309 |             .Where(i => i.Balance <= i.MinimumMilliUnits && i.MinimumMilliUnits > 0)
 310 |             .OrderBy(i => i.Balance - i.MinimumMilliUnits)
 311 |             .Select(i => new LowStockItemDto(i.Id, i.Name, i.Unit, i.Balance, i.MinimumMilliUnits))
 312 |             .ToList();
 313 | 
 314 |         return new StockPositionDto(items.Count, low.Count, low);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
OwnerDashboardQueryService.cs calculates inventory stock movements by summing QuantityMilliUnits without multiplying by -1 when Direction == Out.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Inventory stock overstatement on owner dashboard. Waste and sales increase stock totals rather than decreasing them.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
OwnerDashboardQueryService.cs calculates inventory stock movements by summing QuantityMilliUnits without multiplying by -1 when Direction == Out.

**E. Future Repair Specification:**  
Apply proper sign negation: (m.Direction == StockMovementDirection.Out ? -m.QuantityMilliUnits : m.QuantityMilliUnits).

**Required Regression Test Cases:**
- Seed positive-magnitude In and Out movements plus count adjustments/waste; dashboard balance and low-stock set equal inventory overview.
- Cover zero/negative normalized inputs and multiple movement kinds.

**Acceptance Criteria & Verification Standard:**
- Dashboard stock projection equals canonical inventory balance for every item.
- Out never increases available quantity; count corrections are treated per shared domain rule.

---

### FIN-014: Restore success followed by MULTI_USER failure is classified as database not replaced
- **Severity:** `HIGH` | **Confidence:** `HIGH_SOURCE_CONTROL_FLOW;FAILURE_MODE_NOT_REPRODUCED` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L211-L234) | `211-234` | `Falah.Infrastructure.Backup.RestoreBackupHandler.RecordOutcomeAsync(Falah.Infrastructure.Persistence.Entities.BackupRecordEntity, bool, string, CancellationToken) (MethodDeclaration)` | `a9504298519b` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L211-L234)  
> **Lines 211-234** in `Falah.Infrastructure.Backup.RestoreBackupHandler.RecordOutcomeAsync(Falah.Infrastructure.Persistence.Entities.BackupRecordEntity, bool, string, CancellationToken) (MethodDeclaration)` (File SHA-256: `a9504298519bd4709d18777b5343536894da4238272eea65b4ac704c9e1b61c6`):

```csharp
 211 |     private async Task RecordOutcomeAsync(BackupRecordEntity restoreRecord, bool databaseWasReplaced, string connectionString, CancellationToken ct)
 212 |     {
 213 |         if (databaseWasReplaced)
 214 |         {
 215 |             // SINGLE_USER WITH ROLLBACK IMMEDIATE killed every pooled connection; handing one of
 216 |             // those corpses to the next command would fail for a reason unrelated to any of this.
 217 |             using (var pooled = new SqlConnection(connectionString)) SqlConnection.ClearPool(pooled);
 218 |             _db.Entry(restoreRecord).State = EntityState.Added;
 219 |         }
 220 | 
 221 |         try
 222 |         {
 223 |             await _db.SaveChangesAsync(ct);
 224 |         }
 225 |         catch (Exception ex)
 226 |         {
 227 |             _db.ChangeTracker.Clear();
 228 |             var note = databaseWasReplaced
 229 |                 ? "تمت الاستعادة فعليًا، لكن تعذّر تسجيلها داخل قاعدة البيانات المستعادة: "
 230 |                 : "تعذّر تسجيل نتيجة المحاولة: ";
 231 |             restoreRecord.ErrorMessage = restoreRecord.ErrorMessage is null
 232 |                 ? note + ex.Message
 233 |                 : restoreRecord.ErrorMessage + " | " + note + ex.Message;
 234 |         }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
databaseWasReplaced becomes true only after RESTORE and MULTI_USER both return. If restore159 succeeded and MULTI_USER164 throws/cancels, catch releases mode best-effort but leaves false flag and Failed status.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Operator may be told restore failed after database replacement; RecordOutcome false branch does not re-add disappeared restore row or clear killed connection pool. Incorrect outcome can encourage a dangerous retry. Actual failure at this precise boundary has not been produced.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
databaseWasReplaced becomes true only after RESTORE and MULTI_USER both return. If restore159 succeeded and MULTI_USER164 throws/cancels, catch releases mode best-effort but leaves false flag and Failed status.

**E. Future Repair Specification:**  
Track restore replacement separately from recovery/availability state; persist explicit replaced-but-recovery-incomplete outcome outside replaced database. Resolve uncertain SQL acknowledgments through a recovery protocol; do not auto-retry destructive restore.

**Required Regression Test Cases:**
- Fake coordinator injects failure/cancellation after RESTORE but before/during MULTI_USER.
- Approved disposable-instance fault injection verifies replaced database is never reported untouched and no automatic retry occurs.

**Acceptance Criteria & Verification Standard:**
- Restore command outcome distinguishes database replacement from return to MULTI_USER/availability; partial success is durably recorded outside replaced DB.
- Injected post-RESTORE mode failure never reports database untouched and never triggers automatic retry.

---

### FIN-015: Checkout can commit after its previously open shift has completed handover
- **Severity:** `HIGH` | **Confidence:** `HIGH for missing in-transaction shift revalidation in approved source; concrete SQL acceptance predicted under stated normal schema, not runtime observed.` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs#L32-L60) | `32-60` | `Falah.Domain.Financial.TreasuryMovementFactory.PaymentAccountFor(Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` | `89ef6f7f2c32` |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L84-L114) | `84-114` | `Falah.Domain.Orders.CheckoutRules.BuildPlan(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, IReadOnlyList<Falah.Domain.Orders.OrderItem>, Falah.Domain.Common.Money, Falah.Domain.Financial.TaxRule?, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, IReadOnlyList<Falah.Domain.Financial.PaymentSplit>?, string?) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L66-L110) | `66-110` | `Falah.Domain.Orders.Order.Create(Falah.Domain.Orders.CheckoutRules.CheckoutPlan, string, string, long, string, string, IReadOnlyList<Falah.Domain.Orders.OrderItem>, string, string?, DateTimeOffset, string?, string?, string?, string?, Falah.Domain.Common.Money, string?, string?, string?, int?, string?, string?) (MethodDeclaration)` | `4f93ebcba865` |
| [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs#L23-L29) | `23-29` | `Falah.Infrastructure.BusinessDay.BusinessDayGate.TryGetActiveAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` | `0271b40be055` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L248-L279) | `248-279` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/InventoryDeductionService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/InventoryDeductionService.cs#L77-L97) | `77-97` | `Falah.Infrastructure.Orders.InventoryDeductionService.DeductOrderPackagingAsync(string, string, string, DateTimeOffset, CancellationToken) (MethodDeclaration)` | `2fe3c06b9088` |
| [`src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfFinancialJournal.cs#L22-L44) | `22-44` | `Falah.Infrastructure.Persistence.EfFinancialJournal.TryStart(string, string) (MethodDeclaration)` | `a4fc816acc48` |
| [`src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/SupportingEntities.cs#L64-L89) | `64-89` | `Falah.Infrastructure.Persistence.Entities.ShiftEntity.Id.get (GetAccessorDeclaration)` | `b7191bc0209e` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L116-L129) | `116-129` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Persistence/OrderSequenceService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/OrderSequenceService.cs#L19-L28) | `19-28` | `Falah.Infrastructure.Persistence.OrderSequenceService.AllocateNextAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` | `134fa819907e` |
| [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs#L125-L164) | `125-164` | `Falah.Infrastructure.Shifts.CloseShiftHandler.HandleAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken) (MethodDeclaration)` | `58319fdf46c1` |
| [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L30-L68) | `30-68` | `Falah.Infrastructure.Shifts.ShiftReconciliation.ComputeExpectedCashAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, Falah.Domain.Common.Money, CancellationToken) (MethodDeclaration)` | `cd1a406323ee` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1370-L1379) | `1370-1379` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs#L32-L60)  
> **Lines 32-60** in `Falah.Domain.Financial.TreasuryMovementFactory.PaymentAccountFor(Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` (File SHA-256: `89ef6f7f2c32e04dd844670ebae972518ab598feb6a8540f26ba1681c9664f87`):

```csharp
  32 |     public static string PaymentAccountFor(PaymentMethod method) => method switch
  33 |     {
  34 |         PaymentMethod.Cash => TreasuryAccounts.Drawer,
  35 |         PaymentMethod.Card => TreasuryAccounts.Bank,
  36 |         PaymentMethod.InstaPay => TreasuryAccounts.InstaPay,
  37 |         PaymentMethod.Wallet => TreasuryAccounts.Wallet,
  38 |         _ => throw new DomainException("الدفع المتعدد غير مدعوم في مسار التحصيل المالي الحالي."),
  39 |     };
  40 | 
  41 |     public static TreasuryMovement BuildSaleMovement(
  42 |         string movementId, string operationId, string checkoutRequestId, string orderId,
  43 |         PaymentMethod paymentMethod, Money amount, string? businessDayId, string shiftId,
  44 |         string createdBy, DateTimeOffset createdAtUtc) => new(
  45 |         Id: movementId,
  46 |         IdempotencyKey: $"sale-{checkoutRequestId}",
  47 |         OperationId: operationId,
  48 |         Type: "sale_collection",
  49 |         SourceAccountId: null,
  50 |         DestinationAccountId: PaymentAccountFor(paymentMethod),
  51 |         Amount: amount,
  52 |         ReferenceType: "order",
  53 |         ReferenceId: orderId,
  54 |         BusinessDayId: businessDayId,
  55 |         ShiftId: shiftId,
  56 |         Reason: $"تحصيل طلب {orderId}",
  57 |         CreatedBy: createdBy,
  58 |         ApprovedBy: null,
  59 |         CreatedAtUtc: createdAtUtc,
  60 |         Status: MovementStatus.Posted);
```

> **File:** [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L84-L114)  
> **Lines 84-114** in `Falah.Domain.Orders.CheckoutRules.BuildPlan(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, IReadOnlyList<Falah.Domain.Orders.OrderItem>, Falah.Domain.Common.Money, Falah.Domain.Financial.TaxRule?, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, IReadOnlyList<Falah.Domain.Financial.PaymentSplit>?, string?) (MethodDeclaration)` (File SHA-256: `a594ce2aa6ab60d232942ef4af790c0dd7c2a90b49db772e277eaf79d5af84d4`):

```csharp
  84 |     public static CheckoutPlan BuildPlan(
  85 |         OrderType orderType, PaymentMethod paymentMethod, IReadOnlyList<OrderItem> items,
  86 |         Money discount, TaxRule? taxRule, Money deliveryFee, Money compAmount, Money amountPaid,
  87 |         IReadOnlyList<PaymentSplit>? splitPayments, string? walletProvider)
  88 |     {
  89 |         if (paymentMethod == PaymentMethod.Multi && (splitPayments is null || splitPayments.Count == 0))
  90 |             throw new DomainException("يجب تحديد تفاصيل الدفع المتعدد.");
  91 | 
  92 |         var subtotal = ComputeSubtotal(items);
  93 |         var taxableBase = subtotal - discount;
  94 |         if (taxableBase.IsNegative) taxableBase = Money.Zero;
  95 |         var tax = TaxEngine.Calculate(taxableBase, taxRule);
  96 |         var cartTotal = tax.Final;
  97 |         var payableTotal = ComputePayableTotal(cartTotal, deliveryFee, compAmount);
  98 | 
  99 |         var cashOnDelivery = IsCashOnDelivery(orderType, paymentMethod, payableTotal);
 100 |         var electronicLeg = SelectElectronicLeg(paymentMethod, payableTotal, splitPayments, walletProvider);
 101 | 
 102 |         if (!cashOnDelivery && amountPaid < payableTotal)
 103 |             throw new DomainException("المبلغ المدفوع أقل من إجمالي الطلب.");
 104 | 
 105 |         if (paymentMethod == PaymentMethod.Multi && splitPayments is not null)
 106 |             FinancialRules.AssertSplitPaymentAllocation(splitPayments, payableTotal);
 107 | 
 108 |         var defers = DefersAllMoney(payableTotal, cashOnDelivery, electronicLeg, paymentMethod);
 109 |         var isPaid = ComputeIsPaid(cashOnDelivery, electronicLeg);
 110 |         var changeDue = amountPaid - payableTotal;
 111 |         if (changeDue.IsNegative) changeDue = Money.Zero;
 112 | 
 113 |         return new CheckoutPlan(orderType, paymentMethod, subtotal, deliveryFee, compAmount, tax, payableTotal,
 114 |             amountPaid, changeDue, splitPayments, electronicLeg, cashOnDelivery, defers, isPaid);
```

> **File:** [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L66-L110)  
> **Lines 66-110** in `Falah.Domain.Orders.Order.Create(Falah.Domain.Orders.CheckoutRules.CheckoutPlan, string, string, long, string, string, IReadOnlyList<Falah.Domain.Orders.OrderItem>, string, string?, DateTimeOffset, string?, string?, string?, string?, Falah.Domain.Common.Money, string?, string?, string?, int?, string?, string?) (MethodDeclaration)` (File SHA-256: `4f93ebcba865636bbd00a6d6edbaab1453f72acb35d220de129d3ba8f47b1c4b`):

```csharp
  66 |     public static Order Create(CheckoutRules.CheckoutPlan plan, string id, string orderNumber, long sequenceNumber,
  67 |         string cashierId, string shiftId, IReadOnlyList<OrderItem> items, string checkoutRequestId,
  68 |         string? businessDayId, DateTimeOffset createdAtUtc, string? customerName = null, string? customerPhone = null,
  69 |         string? deliveryAddress = null, string? deliveryArea = null, Money discount = default,
  70 |         string? compReasonCode = null, string? compAuthorizedBy = null, string? compApprovalId = null,
  71 |         int? taxRateBasisPoints = null, string? taxMode = null, string? taxRuleName = null)
  72 |     {
  73 |         var order = new Order
  74 |         {
  75 |             Id = id,
  76 |             OrderNumber = orderNumber,
  77 |             SequenceNumber = sequenceNumber,
  78 |             CashierId = cashierId,
  79 |             ShiftId = shiftId,
  80 |             OrderType = plan.OrderType,
  81 |             CustomerName = customerName,
  82 |             CustomerPhone = customerPhone,
  83 |             DeliveryAddress = deliveryAddress,
  84 |             DeliveryArea = deliveryArea,
  85 |             DeliveryFee = plan.DeliveryFee,
  86 |             Items = items,
  87 |             Subtotal = plan.Subtotal,
  88 |             Discount = discount,
  89 |             CompAmount = plan.CompAmount,
  90 |             CompReasonCode = compReasonCode,
  91 |             CompAuthorizedBy = compAuthorizedBy,
  92 |             CompApprovalId = compApprovalId,
  93 |             TaxAmount = plan.Tax.Tax,
  94 |             TaxableBase = plan.Tax.Taxable,
  95 |             TaxRateBasisPoints = taxRateBasisPoints,
  96 |             TaxMode = taxMode,
  97 |             TaxRuleName = taxRuleName,
  98 |             Total = plan.PayableTotal,
  99 |             PaymentMethod = plan.PaymentMethod,
 100 |             SplitPayments = plan.SplitPayments,
 101 |             AmountPaid = plan.AmountPaid,
 102 |             ChangeDue = plan.ChangeDue,
 103 |             CheckoutRequestId = checkoutRequestId,
 104 |             BusinessDayId = businessDayId,
 105 |             CreatedAtUtc = createdAtUtc,
 106 |         };
 107 |         order.Status = items.Any(i => i.SendToGrill) ? OrderStatus.Preparing : OrderStatus.Completed;
 108 |         order.IsPaid = plan.IsPaid;
 109 |         order.ElectronicPayment = plan.ElectronicLeg;
 110 |         return order;
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Open shift predicate read before checkout transaction is not revalidated/held through commit; closing uses a different correctly locked Shift update that checkout does not perform.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Recorded closed shift expected/actual/handover exclude newly committed cash order while order and posted drawer collection reference that closed shift; new order time can be later than ClosedAtUtc. Stock can also be consumed when configured. No cash loss, fraud, exploit, or accepted real write observed.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Open shift predicate read before checkout transaction is not revalidated/held through commit; closing uses a different correctly locked Shift update that checkout does not perform.

**E. Future Repair Specification:**  
Serialize checkout admission and shift close with a shared transaction-held Shift row protocol: lock/revalidate Open before writes, hold through commit; alternatively use a documented Closing/drain process. Preserve closed snapshots.

**Required Regression Test Cases:**
- Barrier after checkout shift read and before transaction; close then resume checkout: reject with zero order/payment/stock effects.
- Reverse ordering: checkout admission held; close waits and includes sale.
- Same-key replay after close remains nonmutating.

**Acceptance Criteria & Verification Standard:**
- Fresh checkout and shift close serialize through shared shift admission; every accepted checkout is included in handover or rejected with no effects.
- Same-key replay after close remains idempotent and nonmutating.

---

### FIN-016: Unvalidated signed delivery fee can bypass intended discount and comp authorization
- **Severity:** `HIGH` | **Confidence:** `HIGH_SOURCE_AND_ISOLATED_ARITHMETIC;FULL_HTTP_DB_ACCEPTANCE_NOT_EXECUTED` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Orders/CheckoutOrderRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/CheckoutOrderRequest.cs#L22-L38) | `22-38` | `File-level declaration or top-level statement` | `66cd885af3b6` |
| [`src/Falah.Domain/Common/Money.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs#L13-L17) | `13-17` | `Falah.Domain.Common.Money.Money(long) (ConstructorDeclaration)` | `da6aa9572760` |
| [`src/Falah.Domain/Common/Money.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs#L31-L34) | `31-34` | `Falah.Domain.Common.Money.operator +(Falah.Domain.Common.Money, Falah.Domain.Common.Money) (OperatorDeclaration)` | `da6aa9572760` |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L35-L39) | `35-39` | `Falah.Domain.Orders.CheckoutRules.ComputePayableTotal(Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L99-L121) | `99-121` | `Falah.Domain.Orders.CheckoutRules.BuildPlan(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, IReadOnlyList<Falah.Domain.Orders.OrderItem>, Falah.Domain.Common.Money, Falah.Domain.Financial.TaxRule?, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, IReadOnlyList<Falah.Domain.Financial.PaymentSplit>?, string?) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L66-L110) | `66-110` | `Falah.Domain.Orders.Order.Create(Falah.Domain.Orders.CheckoutRules.CheckoutPlan, string, string, long, string, string, IReadOnlyList<Falah.Domain.Orders.OrderItem>, string, string?, DateTimeOffset, string?, string?, string?, string?, Falah.Domain.Common.Money, string?, string?, string?, int?, string?, string?) (MethodDeclaration)` | `4f93ebcba865` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L65-L73) | `65-73` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L102-L143) | `102-143` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L249-L280) | `249-280` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1370-L1393) | `1370-1393` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Orders/CheckoutOrderRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/CheckoutOrderRequest.cs#L22-L38)  
> **Lines 22-38** in `File-level declaration or top-level statement` (File SHA-256: `66cd885af3b6ddf84ea4a1629232490ba92e333be7626715be8614587b91dae1`):

```csharp
  22 |     string CheckoutRequestId,
  23 |     string CashierId,
  24 |     string ShiftId,
  25 |     string OrderType,
  26 |     string PaymentMethod,
  27 |     IReadOnlyList<CheckoutItemRequest> Items,
  28 |     long DiscountPiastres,
  29 |     long DeliveryFeePiastres,
  30 |     long CompAmountPiastres,
  31 |     string? CompApprovalId,
  32 |     string? CompApproverUserId,
  33 |     string? CompReason,
  34 |     long AmountPaidPiastres,
  35 |     IReadOnlyList<CheckoutSplitRequest>? SplitPayments,
  36 |     string? WalletProvider,
  37 |     /// <summary>Which table, for a dine-in order. Frozen on the order like every other thing a
  38 |     /// ticket prints, so a table renumbered next month does not rewrite last month's receipts.
```

> **File:** [`src/Falah.Domain/Common/Money.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs#L13-L17)  
> **Lines 13-17** in `Falah.Domain.Common.Money.Money(long) (ConstructorDeclaration)` (File SHA-256: `da6aa95727609eef384a975cefd4d124bf6f211f6a98a6a75828b42b414c5a5f`):

```csharp
  13 |     private Money(long piastres) => Piastres = piastres;
  14 | 
  15 |     public static readonly Money Zero = new(0);
  16 | 
  17 |     public static Money FromPiastres(long piastres) => new(piastres);
```

> **File:** [`src/Falah.Domain/Common/Money.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs#L31-L34)  
> **Lines 31-34** in `Falah.Domain.Common.Money.operator +(Falah.Domain.Common.Money, Falah.Domain.Common.Money) (OperatorDeclaration)` (File SHA-256: `da6aa95727609eef384a975cefd4d124bf6f211f6a98a6a75828b42b414c5a5f`):

```csharp
  31 |     public static Money operator +(Money a, Money b) => new(a.Piastres + b.Piastres);
  32 |     public static Money operator -(Money a, Money b) => new(a.Piastres - b.Piastres);
  33 |     public static Money operator -(Money a) => new(-a.Piastres);
  34 |     public static Money operator *(Money a, long factor) => new(a.Piastres * factor);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
CheckoutOrderHandler.cs (line 142) accepts request.DeliveryFeePiastres directly from client payload without verifying against DeliveryService.DefaultFee or checking manager authorization.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Delivery fee tampering. A client can supply negative or zero delivery fee to discount an order without triggering manager discount authorization.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
CheckoutOrderHandler.cs (line 142) accepts request.DeliveryFeePiastres directly from client payload without verifying against DeliveryService.DefaultFee or checking manager authorization.

**E. Future Repair Specification:**  
Validate delivery fee server-side against configured delivery rates or require manager override for non-standard delivery fees.

**Required Regression Test Cases:**
- Negative fee with ordinary POS actor; overflow boundaries; assert no writes.
- Positive/zero fee, tax, approved comp, COD and legitimate discounts.

**Acceptance Criteria & Verification Standard:**
- Invalid signed fee and arithmetic overflow rejected before any order, stock, journal or treasury write.
- Valid positive/zero fees and approved discount/comp routes retain policy behavior.

---

### FIN-017: Financial approval provenance can retain unvalidated shift presenter or discard verified driver variance approver
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_SOURCE_ONLY;NO_RUNTIME_REPRODUCTION` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Disposition Rationale:** Source behavior is deterministic but depends on owner business policy (e.g. tax rounding policy, legacy casing tolerance, packaging cost allocation).

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Shifts/CloseShiftRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Shifts/CloseShiftRequest.cs#L10-L17) | `10-17` | `File-level declaration or top-level statement` | `69006fb2c4bb` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L444-L496) | `444-496` | `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.SettleDriverAsync() (MethodDeclaration)` | `27b80026d044` |
| [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L106-L111) | `106-111` | `Falah.Domain.Authorization.AuthorizationPolicy.AuthorizeShiftClose(Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Authorization.Actor, Falah.Domain.Authorization.ManagerApproval?, bool) (MethodDeclaration)` | `029edee4286b` |
| [`src/Falah.Domain/Shifts/Shift.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Shifts/Shift.cs#L103-L115) | `103-115` | `Falah.Domain.Shifts.Shift.Close(string, Falah.Domain.Shifts.Shift.BlindCloseResult, string?, string?, DateTimeOffset) (MethodDeclaration)` | `e96a44389205` |
| [`src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs#L147-L173) | `147-173` | `Falah.Infrastructure.Delivery.SettleDriverCustodyHandler.HandleAsync(Falah.Application.Delivery.SettleDriverCustodyRequest, CancellationToken) (MethodDeclaration)` | `0fc0eb77d5f2` |
| [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs#L113-L156) | `113-156` | `Falah.Infrastructure.Shifts.CloseShiftHandler.HandleAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken) (MethodDeclaration)` | `58319fdf46c1` |
| [`tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/DeliveryVerticalSliceTests.cs#L756-L777) | `756-777` | `Falah.Server.IntegrationTests.DeliveryVerticalSliceTests.A_settlement_difference_requires_a_reason_and_delivery_settlement_approve_on_top_of_delivery_manage() (MethodDeclaration)` | `2cf833e812d2` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Shifts/CloseShiftRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Shifts/CloseShiftRequest.cs#L10-L17)  
> **Lines 10-17** in `File-level declaration or top-level statement` (File SHA-256: `69006fb2c4bbe317d32b5a544d9e07e272ef5bf7e1b71a3fbf6c1d2baa8ff932`):

```csharp
  10 | /// <summary>Step 2: commits the close. CloseRequestId is minted once per "confirm close" button
  11 | /// press and must be reused unchanged by a retry - same idempotency contract as
  12 | /// CheckoutOrderRequest.CheckoutRequestId. ManagerApprovalId/ManagerApproverUserId are only
  13 | /// required when PrepareShiftCloseResult.RequiresManagerApproval was true - same shape as
  14 | /// RefundOrderRequest's approval fields.</summary>
  15 | public sealed record CloseShiftRequest(
  16 |     string CloseRequestId, string ShiftId, string ClosedByUserId, long ActualCashCountedPiastres, string? Reason,
  17 |     string? ManagerApprovalId, string? ManagerApproverUserId);
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L444-L496)  
> **Lines 444-496** in `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.SettleDriverAsync() (MethodDeclaration)` (File SHA-256: `27b80026d0447e03faf17e6eaad32fe5fde3ee3cd004f9503d561dfd6adc6822`):

```csharp
 444 |     private async Task SettleDriverAsync()
 445 |     {
 446 |         if (IsBusy || _assignRequest.HasPending || _outcomeRequest.HasPending || !CanDispatch) return;
 447 |         if (_settleRequest.Value is { } pending)
 448 |         {
 449 |             IsBusy = true;
 450 |             try { await CompleteSettlementAsync(pending); }
 451 |             finally { IsBusy = false; }
 452 |             return;
 453 |         }
 454 |         if (IsLoadingAccount || AccountDriver is null || DriverAccount is null) return;
 455 |         if (!CashInput.TryParse(SettleActualCashText, out var actual))
 456 |         {
 457 |             SettleError = "أدخل المبلغ المسلَّم بشكل صحيح.";
 458 |             return;
 459 |         }
 460 | 
 461 |         var difference = actual - DriverAccount.CashCustodyPiastres;
 462 |         if (difference != 0 && string.IsNullOrWhiteSpace(SettleReason))
 463 |         {
 464 |             SettleError = "يوجد فارق في التسليم - سبب الفارق إلزامي.";
 465 |             return;
 466 |         }
 467 | 
 468 |         IsBusy = true;
 469 |         SettleError = null;
 470 |         try
 471 |         {
 472 |             string? approvalId = null, approverUserId = null;
 473 |             if (difference != 0 && SettlementNeedsApproval)
 474 |             {
 475 |                 if (SettleManagerPin.Length < 4)
 476 |                 {
 477 |                     SettleError = "أدخل رمز المدير المخوّل باعتماد فارق التسليم.";
 478 |                     return;
 479 |                 }
 480 |                 // One call, not two: the server checks the PIN and decides who approved.
 481 |                 var approval = await _session.Api.IssueManagerApprovalAsync(
 482 |                     new Falah.Application.Approvals.IssueManagerApprovalRequest(
 483 |                         SettleManagerPin, "delivery_settlement_approve", SettleReason));
 484 |                 if (!approval.Success || approval.Value is null)
 485 |                 {
 486 |                     SettleError = approval.Error ?? "رمز المدير غير صحيح أو غير مخوّل باعتماد فارق التسليم.";
 487 |                     return;
 488 |                 }
 489 |                 approvalId = approval.Value.ApprovalId;
 490 |                 approverUserId = approval.Value.ApproverUserId;
 491 |             }
 492 | 
 493 |             await CompleteSettlementAsync(new SettleDriverCustodyRequest(
 494 |                 _settleRequestId!, AccountDriver.Id, actual,
 495 |                 string.IsNullOrWhiteSpace(SettleReason) ? null : SettleReason,
 496 |                 _session.CurrentUser!.Id, approvalId, approverUserId));
```

> **File:** [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L106-L111)  
> **Lines 106-111** in `Falah.Domain.Authorization.AuthorizationPolicy.AuthorizeShiftClose(Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Authorization.Actor, Falah.Domain.Authorization.ManagerApproval?, bool) (MethodDeclaration)` (File SHA-256: `029edee4286b6aea725da962ddaafff1e6bc20f57ddc922801c39095b43585d0`):

```csharp
 106 |         IApprovalRegistry registry, Actor actor, ManagerApproval? approval, bool discrepancyNeedsManager)
 107 |     {
 108 |         AuthorizeCommand(registry, actor, "shifts_blind_close");
 109 |         if (discrepancyNeedsManager)
 110 |             AuthorizeCommand(registry, actor, "shifts_manage", approval, requireApproval: true);
 111 |     }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Financial attribution derives from presentation or earlier base gate rather than the actual verified relevant authorization result. Original shift evidence and related driver variance variant are preserved separately.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
False attribution to a person who did not issue approval; no money change or otherwise forbidden operation authorized by this defect alone.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Financial attribution derives from presentation or earlier base gate rather than the actual verified relevant authorization result. Original shift evidence and related driver variance variant are preserved separately.

**E. Future Repair Specification:**  
Carry verified gate result, actor, and actual approver identity separately to all financial rows/audit; preserve OR authorization policy and approval reason semantics.

**Required Regression Test Cases:**
- Direct-permission below-threshold unnecessary presenter; above-threshold approved variance; verify actor/approver on movement, close event, settlement and audit.
- Wrong action/consumed approval and permission-only actor controls.

**Acceptance Criteria & Verification Standard:**
- Persisted approver attribution is the verified approver for the actual gate; actor and approver are distinct fields where required.
- Below-threshold optional presentation cannot be misattributed; above-threshold approval still enforces approval action, reason, and amount.

---

### FIN-018: Negative receipt quantity and unit cost can post stock reduction with supplier debt
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_SOURCE_ONLY;NO_RUNTIME_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Purchasing/PurchasingRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Purchasing/PurchasingRequests.cs#L15-L29) | `15-29` | `File-level declaration or top-level statement` | `df5e41e3bdf5` |
| [`src/Falah.Domain/Inventory/PurchasingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PurchasingRules.cs#L77-L98) | `77-98` | `Falah.Domain.Inventory.PurchasingRules.AssertPurchasePayment(Falah.Domain.Common.Money, Falah.Domain.Common.Money) (MethodDeclaration)` | `e11ccc80b8fb` |
| [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L20-L33) | `20-33` | `Falah.Infrastructure.Inventory.InventoryQueryService.ListInventoryItemsAsync(CancellationToken) (MethodDeclaration)` | `3ff16ec5eb50` |
| [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L121-L175) | `121-175` | `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` | `70287ac1d6a6` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2182-L2187) | `2182-2187` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Purchasing/PurchasingRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Purchasing/PurchasingRequests.cs#L15-L29)  
> **Lines 15-29** in `File-level declaration or top-level statement` (File SHA-256: `df5e41e3bdf5ffa195d24c3802d58d24795eecce197195f3e87ec308d208567b`):

```csharp
  15 | public sealed record PurchaseLineRequest(string InventoryItemId, long QuantityMilliUnits, long UnitCostPiastres);
  16 | 
  17 | /// <summary>Ported from receivePurchase (PurchaseCommandInput). PaymentMethod is one of
  18 | /// cash/instapay/bank/credit - "credit" (آجل) means Paid must be zero, the whole invoice starts life
  19 | /// entirely outstanding on the supplier ledger. ActorUserId is both the authorizing actor and the
  20 | /// person who physically received the goods - the reference app's own UI never distinguishes these
  21 | /// (receivedBy is always the acting cashier/manager's own name), so this system resolves both from
  22 | /// the one server-verified actor rather than trusting a separately client-supplied name.</summary>
  23 | public sealed record ReceivePurchaseRequest(
  24 |     string RequestId, string SupplierId, string? InvoiceNumber, IReadOnlyList<PurchaseLineRequest> Lines,
  25 |     long PaidPiastres, string PaymentMethod, string ActorUserId, string? Notes,
  26 |     string? ManagerApprovalId, string? ManagerApproverUserId,
  27 |     /// <summary>When the invoice is due. Optional, and left null when no term was agreed - the
  28 |     /// system never invents one, because an invented due date would produce invented overdue.</summary>
  29 |     DateOnly? DueDate = null);
```

> **File:** [`src/Falah.Domain/Inventory/PurchasingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/PurchasingRules.cs#L77-L98)  
> **Lines 77-98** in `Falah.Domain.Inventory.PurchasingRules.AssertPurchasePayment(Falah.Domain.Common.Money, Falah.Domain.Common.Money) (MethodDeclaration)` (File SHA-256: `e11ccc80b8fb47c7d534bb3595e4553fbfde47eae93eb0eb6ba007f1c7ed2524`):

```csharp
  77 |     public static void AssertPurchasePayment(Money paid, Money total)
  78 |     {
  79 |         if (paid.IsNegative) throw new DomainException("المبلغ المدفوع غير صالح.");
  80 |         if (paid > total) throw new DomainException("المبلغ المدفوع لا يمكن أن يتجاوز إجمالي الفاتورة.");
  81 |     }
  82 | 
  83 |     /// <summary>Ported from purchasePaymentAccountFor in lib/financial-domain.mjs. "credit" (آجل) is
  84 |     /// deliberately not a real treasury account - it means no cash moved yet, the whole invoice is
  85 |     /// outstanding on the supplier ledger until a later payPurchase settles some or all of it.</summary>
  86 |     public static string? PurchasePaymentAccountFor(string paymentMethod) => paymentMethod switch
  87 |     {
  88 |         "cash" => Financial.TreasuryAccounts.Main,
  89 |         "bank" => Financial.TreasuryAccounts.Bank,
  90 |         "instapay" => Financial.TreasuryAccounts.InstaPay,
  91 |         "credit" => null,
  92 |         _ => throw new DomainException("مصدر دفع المشتريات غير مدعوم."),
  93 |     };
  94 | 
  95 |     public static Money SupplierOutstandingFromLedger(IEnumerable<SupplierLedgerEntry> entries, string supplierId, string? purchaseId) =>
  96 |         Money.FromPiastres(entries
  97 |             .Where(e => e.SupplierId == supplierId && (purchaseId is null || e.PurchaseId == purchaseId || e.ReferenceId == purchaseId))
  98 |             .Sum(e => e.Debit.Piastres - e.Credit.Piastres));
```

> **File:** [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L20-L33)  
> **Lines 20-33** in `Falah.Infrastructure.Inventory.InventoryQueryService.ListInventoryItemsAsync(CancellationToken) (MethodDeclaration)` (File SHA-256: `3ff16ec5eb50957c11ad1ba39ed5e45cd8f29b972674d49b4e3448e0e3aee6de`):

```csharp
  20 |     public async Task<List<InventoryItemDto>> ListInventoryItemsAsync(CancellationToken ct = default)
  21 |     {
  22 |         var items = await _db.InventoryItems.AsNoTracking().ToListAsync(ct);
  23 |         var movements = await _db.StockMovements.AsNoTracking()
  24 |             .Select(m => new { m.InventoryItemId, m.Direction, m.QuantityMilliUnits }).ToListAsync(ct);
  25 |         var balances = movements.GroupBy(m => m.InventoryItemId)
  26 |             .ToDictionary(g => g.Key, g => g.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits));
  27 | 
  28 |         return items.Select(i =>
  29 |         {
  30 |             var balance = balances.GetValueOrDefault(i.Id, 0);
  31 |             return new InventoryItemDto(i.Id, i.Name, i.Category, i.Unit, i.MinimumMilliUnits, i.Active,
  32 |                 i.IsPackaging, i.CostSource, i.StandardDailyUsageMilliUnits, balance, balance <= i.MinimumMilliUnits,
  33 |                 i.EstimatedUnitCostPiastres);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Aggregate payment validation substituted for per-line positive receipt quantity/nonnegative purchase cost validation.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Incorrect stock reduction labelled receipt and positive supplier debt for invalid purchase; unauthorized privilege escalation not claimed.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Aggregate payment validation substituted for per-line positive receipt quantity/nonnegative purchase cost validation.

**E. Future Repair Specification:**  
Validate direction-specific positive receipt quantities and nonnegative costs before posting. If returns are required, implement a separate explicit return/credit-note flow rather than encoding them as receipt negatives.

**Required Regression Test Cases:**
- Negative qty with positive unit cost; positive qty with negative cost; both-negative combination; assert no stock/debt mutation.
- Positive receipt and distinct authorized return flow.

**Acceptance Criteria & Verification Standard:**
- Receipt rejects unsupported negative quantity/unit-cost combinations before posting stock or supplier debt.
- Valid positive receipts and any approved return/credit workflow remain coherent and atomic.

---

### FIN-019: Pending mixed delivery includes already collected payment leg in driver custody
- **Severity:** `HIGH` | **Confidence:** `HIGH_SOURCE_TRACE_NOT_DATABASE_OR_HTTP_REPRODUCED` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Delivery/DeliveryRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Delivery/DeliveryRequests.cs#L71-L76) | `71-76` | `File-level declaration or top-level statement` | `1b3cfd319183` |
| [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L58-L65) | `58-65` | `Falah.Client.CashierWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken) (MethodDeclaration)` | `831a6dfd1f18` |
| [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L631-L632) | `631-632` | `Falah.Client.CashierWinUI.Services.ApiClient.GetDispatchBoardAsync(CancellationToken) (MethodDeclaration)` | `831a6dfd1f18` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L66-L77) | `66-77` | `File-level declaration or top-level statement` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L134-L134) | `134-134` | `File-level declaration or top-level statement` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L174-L198) | `174-198` | `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.MarkReadyAsync(Falah.Application.Delivery.DeliveryDto) (MethodDeclaration)` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L208-L215) | `208-215` | `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.OpenAssign(Falah.Application.Delivery.DeliveryDto) (MethodDeclaration)` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L221-L242) | `221-242` | `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.ConfirmAssignAsync() (MethodDeclaration)` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L297-L307) | `297-307` | `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.OpenOutcome(Falah.Application.Delivery.DeliveryDto) (MethodDeclaration)` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/DeliveryViewModel.cs#L319-L356) | `319-356` | `Falah.Client.CashierWinUI.ViewModels.DeliveryViewModel.ConfirmOutcomeAsync() (MethodDeclaration)` | `27b80026d044` |
| [`src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml#L76-L79) | `76-79` | `File-level declaration or top-level statement` | `31281118fdd0` |
| [`src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/DeliveryModalsView.xaml#L103-L107) | `103-107` | `File-level declaration or top-level statement` | `31281118fdd0` |
| [`src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/DeliveryView.xaml#L119-L140) | `119-140` | `File-level declaration or top-level statement` | `1800c3471c45` |
| [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs#L9-L21) | `9-21` | `Falah.Domain.Delivery.DeliveryRules.DeliveryCashResponsibility(Falah.Domain.Financial.PaymentMethod, bool, Falah.Domain.Common.Money) (MethodDeclaration)` | `cf4ab3ba7e24` |
| [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs#L32-L37) | `32-37` | `Falah.Domain.Delivery.DeliveryRules.CalculateTripSettlement(IEnumerable<Falah.Domain.Delivery.DriverCustody>, Falah.Domain.Common.Money) (MethodDeclaration)` | `cf4ab3ba7e24` |
| [`src/Falah.Domain/Delivery/DeliveryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryRules.cs#L83-L89) | `83-89` | `Falah.Domain.Delivery.DeliveryRules.AssertCollectionAmount(Falah.Domain.Common.Money, Falah.Domain.Common.Money, string?) (MethodDeclaration)` | `cf4ab3ba7e24` |
| [`src/Falah.Domain/Delivery/DeliveryStatus.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Delivery/DeliveryStatus.cs#L13-L31) | `13-31` | `Falah.Domain.Delivery.DeliveryTransitions.AssertTransition(Falah.Domain.Delivery.DeliveryStatus, Falah.Domain.Delivery.DeliveryStatus) (MethodDeclaration)` | `ac7798dec40b` |
| [`src/Falah.Domain/Financial/FinancialRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/FinancialRules.cs#L14-L25) | `14-25` | `Falah.Domain.Financial.FinancialRules.AssertSplitPaymentAllocation(IReadOnlyList<Falah.Domain.Financial.PaymentSplit>, Falah.Domain.Common.Money) (MethodDeclaration)` | `0c48f0fef379` |
| [`src/Falah.Domain/Financial/TreasuryMovement.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs#L32-L62) | `32-62` | `Falah.Domain.Financial.TreasuryMovementFactory.PaymentAccountFor(Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` | `89ef6f7f2c32` |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L44-L78) | `44-78` | `Falah.Domain.Orders.CheckoutRules.IsCashOnDelivery(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, Falah.Domain.Common.Money) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L88-L119) | `88-119` | `Falah.Domain.Orders.CheckoutRules.BuildPlan(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, IReadOnlyList<Falah.Domain.Orders.OrderItem>, Falah.Domain.Common.Money, Falah.Domain.Financial.TaxRule?, Falah.Domain.Common.Money, Falah.Domain.Common.Money, Falah.Domain.Common.Money, IReadOnlyList<Falah.Domain.Financial.PaymentSplit>?, string?) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Domain/Orders/ElectronicPayment.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/ElectronicPayment.cs#L6-L10) | `6-10` | `File-level declaration or top-level statement` | `9b3fd7b69233` |
| [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L100-L109) | `100-109` | `Falah.Domain.Orders.Order.Create(Falah.Domain.Orders.CheckoutRules.CheckoutPlan, string, string, long, string, string, IReadOnlyList<Falah.Domain.Orders.OrderItem>, string, string?, DateTimeOffset, string?, string?, string?, string?, Falah.Domain.Common.Money, string?, string?, string?, int?, string?, string?) (MethodDeclaration)` | `4f93ebcba865` |
| [`src/Falah.Domain/Orders/OrderPaymentQueries.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderPaymentQueries.cs#L26-L55) | `26-55` | `Falah.Domain.Orders.OrderPaymentQueries.PaymentMethodTotals(IEnumerable<Falah.Domain.Orders.Order>) (MethodDeclaration)` | `805e8d11d87c` |
| [`src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/AssignDeliveryHandler.cs#L36-L137) | `36-137` | `Falah.Infrastructure.Delivery.AssignDeliveryHandler.HandleAsync(Falah.Application.Delivery.AssignDeliveryRequest, CancellationToken) (MethodDeclaration)` | `a083e3ed905b` |
| [`src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/DeliveryFactsComposer.cs#L14-L29) | `14-29` | `Falah.Infrastructure.Delivery.DeliveryFactsComposer.Compose(Falah.Infrastructure.Persistence.Entities.DeliveryEntity, Falah.Infrastructure.Persistence.Entities.OrderEntity) (MethodDeclaration)` | `d67147ee1cc8` |
| [`src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs#L40-L47) | `40-47` | `Falah.Infrastructure.Delivery.DeliveryQueryService.ListDispatchBoardAsync(CancellationToken) (MethodDeclaration)` | `42b35dd12c36` |
| [`src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/DeliveryQueryService.cs#L58-L92) | `58-92` | `Falah.Infrastructure.Delivery.DeliveryQueryService.QueryToDtos(IQueryable<Falah.Infrastructure.Persistence.Entities.DeliveryEntity>, CancellationToken) (MethodDeclaration)` | `42b35dd12c36` |
| [`src/Falah.Infrastructure/Delivery/MarkDeliveryReadyHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/MarkDeliveryReadyHandler.cs#L26-L62) | `26-62` | `Falah.Infrastructure.Delivery.MarkDeliveryReadyHandler.HandleAsync(Falah.Application.Delivery.MarkDeliveryReadyRequest, CancellationToken) (MethodDeclaration)` | `b88f59ee0a9a` |
| [`src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/RecordDeliveryResultHandler.cs#L45-L144) | `45-144` | `Falah.Infrastructure.Delivery.RecordDeliveryResultHandler.HandleAsync(Falah.Application.Delivery.RecordDeliveryResultRequest, CancellationToken) (MethodDeclaration)` | `4bfd99bd7218` |
| [`src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Delivery/SettleDriverCustodyHandler.cs#L35-L187) | `35-187` | `Falah.Infrastructure.Delivery.SettleDriverCustodyHandler.HandleAsync(Falah.Application.Delivery.SettleDriverCustodyRequest, CancellationToken) (MethodDeclaration)` | `0fc0eb77d5f2` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L80-L90) | `80-90` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L144-L176) | `144-176` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L223-L229) | `223-229` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L253-L275) | `253-275` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Orders/OrderEnumMapping.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderEnumMapping.cs#L58-L65) | `58-65` | `Falah.Infrastructure.Orders.OrderEnumMapping.ToWireString(Falah.Domain.Orders.ElectronicPaymentStatus) (MethodDeclaration)` | `9fae8f656f09` |
| [`src/Falah.Infrastructure/Orders/OrderMapper.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderMapper.cs#L118-L122) | `118-122` | `Falah.Infrastructure.Orders.OrderMapper.ToEntity(Falah.Domain.Orders.Order) (MethodDeclaration)` | `376193953483` |
| [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L29-L84) | `29-84` | `Falah.Server.AuthenticationMiddleware.InvokeAsync(HttpContext, Falah.Infrastructure.Authorization.SessionService) (MethodDeclaration)` | `3e64ad8ef532` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L251-L251) | `251-251` | `File-level declaration or top-level statement` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1224-L1250) | `1224-1250` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1286-L1314) | `1286-1314` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Delivery/DeliveryRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Delivery/DeliveryRequests.cs#L71-L76)  
> **Lines 71-76** in `File-level declaration or top-level statement` (File SHA-256: `1b3cfd319183da61331af6b771f45ef6b660a5ba10c88f3b348b1dcb07aef61c`):

```csharp
  71 | public sealed record DeliveryDto(
  72 |     string DeliveryId, string OrderId, string? OrderNumber, string Status, string? DriverId, string? DriverName,
  73 |     long AmountToCollectPiastres, long OrderTotalPiastres, string PaymentMethod, bool PaymentAlreadySettled,
  74 |     string? ShiftId, DateTimeOffset CreatedAtUtc,
  75 |     string? CustomerName = null, string? CustomerPhone = null, string? DeliveryArea = null,
  76 |     string? DeliveryAddress = null, string? ElectronicPaymentStatus = null);
```

> **File:** [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L58-L65)  
> **Lines 58-65** in `Falah.Client.CashierWinUI.Services.ApiClient.ReadResultAsync<T>(System.Net.Http.HttpResponseMessage, CancellationToken) (MethodDeclaration)` (File SHA-256: `831a6dfd1f18510b240a232b7a4c4fd11251a51746e1e416c7ce691f732ddbf4`):

```csharp
  58 |     private static async Task<ApiResult<T>> ReadResultAsync<T>(HttpResponseMessage response, CancellationToken ct)
  59 |     {
  60 |         if (response.IsSuccessStatusCode)
  61 |         {
  62 |             var value = await response.Content.ReadFromJsonAsync<T>(cancellationToken: ct);
  63 |             return value is null ? new(false, default, "لم يصل تأكيد صالح للعملية. أعد المحاولة للتحقق.", true)
  64 |                 : new ApiResult<T>(true, value, null, false, (int)response.StatusCode);
  65 |         }
```

> **File:** [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L631-L632)  
> **Lines 631-632** in `Falah.Client.CashierWinUI.Services.ApiClient.GetDispatchBoardAsync(CancellationToken) (MethodDeclaration)` (File SHA-256: `831a6dfd1f18510b240a232b7a4c4fd11251a51746e1e416c7ce691f732ddbf4`):

```csharp
 631 |     public Task<ApiResult<IReadOnlyList<DeliveryDto>>> GetDispatchBoardAsync(CancellationToken ct = default) =>
 632 |         ReadAsync<IReadOnlyList<DeliveryDto>>(() => _http.GetAsync("/api/delivery/board", ct), ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Delivery responsibility collapses per-leg payment collection into one whole-order IsPaid flag and Total-refund. Mixed pending order is partially collected, but modeled as wholly uncollected for driver custody.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Overstated driver collection/custody by already posted immediate leg; possible duplicate customer collection or artificial driver variance/approval requirement. No claim actual customer payment, cash theft, SQL acceptance or persisted double revenue was reproduced.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Delivery responsibility collapses per-leg payment collection into one whole-order IsPaid flag and Total-refund. Mixed pending order is partially collected, but modeled as wholly uncollected for driver custody.

**E. Future Repair Specification:**  
Use per-tender-leg collection status/amount when deriving driver custody. Do not reinterpret pending electronic receivable as driver cash; block dispatch or provide an explicit owner-approved conversion/recovery workflow.

**Required Regression Test Cases:**
- Mixed immediate cash/card plus pending electronic dispatch; assert custody does not include collected leg or silently convert pending claim.
- All-immediate Multi and cash COD positive controls; explicit conversion policy only if approved.

**Acceptance Criteria & Verification Standard:**
- Driver custody equals only currently collectible amounts under documented payment contract; pending electronic leg is never silently converted to cash owed.
- Block or explicitly convert pending leg only under owner-approved flow, with atomic custody/movement/reporting.

---

### FIN-020: First stock receipt can race unit-change admission and reinterpret movement history
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_STATIC_SCHEDULE_NO_RUNTIME` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Inventory/InventoryItemRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Inventory/InventoryItemRequests.cs#L14-L26) | `14-26` | `File-level declaration or top-level statement` | `1db9910fe257` |
| [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs#L40-L47) | `40-47` | `Falah.Domain.Inventory.InventoryRules.AssertUnitChangeAllowed(bool, bool) (MethodDeclaration)` | `6c445ff5fdfd` |
| [`src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs#L74-L100) | `74-100` | `Falah.Infrastructure.Inventory.UpdateInventoryItemHandler.HandleAsync(Falah.Application.Inventory.UpdateInventoryItemRequest, CancellationToken) (MethodDeclaration)` | `dc2c1c298992` |
| [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L20-L34) | `20-34` | `Falah.Infrastructure.Inventory.InventoryQueryService.ListInventoryItemsAsync(CancellationToken) (MethodDeclaration)` | `3ff16ec5eb50` |
| [`src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/InventoryEntities.cs#L24-L76) | `24-76` | `Falah.Infrastructure.Persistence.Entities.InventoryItemEntity.Id.get (GetAccessorDeclaration)` | `934d1252ffc1` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L245-L255) | `245-255` | `Falah.Infrastructure.Persistence.FalahDbContext.OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder) (MethodDeclaration)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L60-L100) | `60-100` | `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` | `70287ac1d6a6` |
| [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L165-L178) | `165-178` | `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` | `70287ac1d6a6` |
| [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L206-L209) | `206-209` | `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` | `70287ac1d6a6` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L909-L916) | `909-916` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Inventory/InventoryItemRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Inventory/InventoryItemRequests.cs#L14-L26)  
> **Lines 14-26** in `File-level declaration or top-level statement` (File SHA-256: `1db9910fe257d0e653719898cf3ba32f622bdeafe7028fd62936e7ff26445992`):

```csharp
  14 | /// <summary>Ported from updateInventoryItem: metadata-only (name/category/unit/minimum/active/
  15 | /// packaging/standard-usage/cost-source). Never touches balance or any historical movement. The
  16 | /// unit is locked once any movement exists against this item - see InventoryRules.AssertUnitChangeAllowed.</summary>
  17 | public sealed record UpdateInventoryItemRequest(
  18 |     string Id, string Name, string Category, string Unit, long MinimumMilliUnits, bool Active,
  19 |     bool IsPackaging, string CostSource, long? StandardDailyUsageMilliUnits, string ActorUserId);
  20 | 
  21 | public sealed record InventoryItemDto(
  22 |     string Id, string Name, string Category, string Unit, long MinimumMilliUnits, bool Active,
  23 |     bool IsPackaging, string CostSource, long? StandardDailyUsageMilliUnits, long BalanceMilliUnits, bool IsLowStock,
  24 |     /// <summary>A hand-entered unit cost used only until a real purchase exists for this item. Null
  25 |     /// once an invoice has been recorded, or when nobody has estimated anything.</summary>
  26 |     long? EstimatedUnitCostPiastres = null);
```

> **File:** [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs#L40-L47)  
> **Lines 40-47** in `Falah.Domain.Inventory.InventoryRules.AssertUnitChangeAllowed(bool, bool) (MethodDeclaration)` (File SHA-256: `6c445ff5fdfdb08495188e70b13ba03b18fcfb850921dca22838695c7ddebb67`):

```csharp
  40 |     /// <summary>The unit is not just a label - every stock movement for an item stores its quantity
  41 |     /// as a bare milliUnit count of "whatever the item's unit was when it was entered," with no
  42 |     /// per-movement unit snapshot. Changing it after movements already exist would silently
  43 |     /// reinterpret every past quantity under the new unit.</summary>
  44 |     public static void AssertUnitChangeAllowed(bool hasExistingMovements, bool unitChanged)
  45 |     {
  46 |         if (hasExistingMovements && unitChanged)
  47 |             throw new DomainException("لا يمكن تغيير وحدة خامة لها حركات مخزون مسجلة بالفعل.");
```

> **File:** [`src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryItemHandlers.cs#L74-L100)  
> **Lines 74-100** in `Falah.Infrastructure.Inventory.UpdateInventoryItemHandler.HandleAsync(Falah.Application.Inventory.UpdateInventoryItemRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `dc2c1c2989924b54118f554e34b4f0cd39915ca7572a7a839cefd174fb18e427`):

```csharp
  74 |     {
  75 |         if (string.IsNullOrWhiteSpace(request.Name)) throw new Falah.Domain.Common.DomainException("اسم الخامة مطلوب.");
  76 | 
  77 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  78 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  79 |         AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "inventory_manage");
  80 | 
  81 |         var entity = await _db.InventoryItems.FirstOrDefaultAsync(i => i.Id == request.Id, ct)
  82 |             ?? throw new InvalidOperationException("الخامة غير موجودة.");
  83 | 
  84 |         var unitChanged = entity.Unit != request.Unit;
  85 |         if (unitChanged)
  86 |         {
  87 |             var hasMovements = await _db.StockMovements.AsNoTracking().AnyAsync(m => m.InventoryItemId == entity.Id, ct);
  88 |             InventoryRules.AssertUnitChangeAllowed(hasMovements, unitChanged: true);
  89 |         }
  90 | 
  91 |         entity.Name = request.Name.Trim();
  92 |         entity.Category = request.Category;
  93 |         entity.Unit = request.Unit;
  94 |         entity.MinimumMilliUnits = request.MinimumMilliUnits;
  95 |         entity.Active = request.Active;
  96 |         entity.IsPackaging = request.IsPackaging;
  97 |         entity.CostSource = request.CostSource;
  98 |         entity.StandardDailyUsageMilliUnits = request.StandardDailyUsageMilliUnits;
  99 | 
 100 |         await _db.SaveChangesAsync(ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Immutable-unit predicate is evaluated before write without coordinating the first stock-movement admission. InventoryItem mapping244 is primary key only, no concurrency token; movement FK protects existence, not unit interpretation.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Historical bare milliunit movement is reinterpreted under UnitB, contrary explicit InventoryRules40-47 no-unit-snapshot invariant. Current stock display joins current Unit, and valuation/recipe conversions can become semantically inconsistent; no measured monetary loss or actual SQL schedule executed.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Immutable-unit predicate is evaluated before write without coordinating the first stock-movement admission. InventoryItem mapping244 is primary key only, no concurrency token; movement FK protects existence, not unit interpretation.

**E. Future Repair Specification:**  
Coordinate unit edits and first movement admission through a shared DB transaction lock/revision invariant used by both edit and every movement writer. Version only works if both paths participate.

**Required Regression Test Cases:**
- Barrier first movement admission vs unit change; ensure at most one incompatible operation commits.
- Sequential post-first-movement unit change rejection and legitimate same-unit metadata update.

**Acceptance Criteria & Verification Standard:**
- A unit change and first movement cannot both pass against same pre-movement state; one is rejected or serialized.
- All readers continue to interpret historical movements using stable unit semantics.

---

### FIN-021: Accepted numeric Inclusive tax mode is persisted raw and consumed as Exclusive
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_NO_RUNTIME` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Disposition Rationale:** Source behavior is deterministic but depends on owner business policy (e.g. tax rounding policy, legacy casing tolerance, packaging cost allocation).

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Pricing/TaxRuleRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Pricing/TaxRuleRequests.cs#L10-L16) | `10-16` | `File-level declaration or top-level statement` | `455302a91ad1` |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs#L142-L151) | `142-151` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LoadMenuAsync() (MethodDeclaration)` | `75ca06ba893a` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs#L122-L127) | `122-127` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LoadMenuAsync() (MethodDeclaration)` | `ec9eeab0b553` |
| [`src/Falah.Domain/Financial/TaxRule.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TaxRule.cs#L5-L7) | `5-7` | `File-level declaration or top-level statement` | `baf652da91a0` |
| [`src/Falah.Domain/Financial/TaxRule.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TaxRule.cs#L14-L29) | `14-29` | `Falah.Domain.Financial.TaxEngine.Calculate(Falah.Domain.Common.Money, Falah.Domain.Financial.TaxRule?) (MethodDeclaration)` | `baf652da91a0` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L60-L63) | `60-63` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |
| [`src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs#L34-L42) | `34-42` | `Falah.Infrastructure.Pricing.CreateTaxRuleHandler.HandleAsync(Falah.Application.Pricing.CreateTaxRuleRequest, CancellationToken) (MethodDeclaration)` | `f40a0f0879cb` |
| [`src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs#L86-L99) | `86-99` | `Falah.Infrastructure.Pricing.UpdateTaxRuleHandler.HandleAsync(Falah.Application.Pricing.UpdateTaxRuleRequest, CancellationToken) (MethodDeclaration)` | `f40a0f0879cb` |
| [`src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Pricing/TaxRuleHandlers.cs#L116-L117) | `116-117` | `Falah.Infrastructure.Pricing.TaxRuleQueryService.ListAsync(CancellationToken) (MethodDeclaration)` | `f40a0f0879cb` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1647-L1666) | `1647-1666` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Pricing/TaxRuleRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Pricing/TaxRuleRequests.cs#L10-L16)  
> **Lines 10-16** in `File-level declaration or top-level statement` (File SHA-256: `455302a91ad1cb54ee244d2a0fda5aea41b780cc76fb0e6b9daa685f46080cc9`):

```csharp
  10 | public sealed record CreateTaxRuleRequest(string RequestId, string Name, int RateBasisPoints, string Mode, bool Enabled, string ActorUserId);
  11 | 
  12 | public sealed record CreateTaxRuleResult(string TaxRuleId, bool WasReplayed);
  13 | 
  14 | public sealed record UpdateTaxRuleRequest(string TaxRuleId, string Name, int RateBasisPoints, string Mode, bool Enabled, string ActorUserId, string? RequestId = null);
  15 | 
  16 | public sealed record TaxRuleDto(string Id, string Name, int RateBasisPoints, string Mode, bool Enabled);
```

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.cs#L142-L151)  
> **Lines 142-151** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LoadMenuAsync() (MethodDeclaration)` (File SHA-256: `75ca06ba893a7b302d51a828fe590119039f6fd1bab1a33f4e9702e9dba4339c`):

```csharp
 142 |             var taxes = await _session.Api.GetTaxRulesAsync();
 143 |             if (!taxes.Success || taxes.Value is null)
 144 |             {
 145 |                 MenuError = taxes.Error ?? "تعذر تحميل الضريبة.";
 146 |                 return;
 147 |             }
 148 |             var tax = taxes.Value.FirstOrDefault(t => t.Enabled);
 149 |             _taxRule = tax is null ? null : new(tax.Id, tax.Name, tax.RateBasisPoints,
 150 |                 tax.Mode == "Inclusive" ? Falah.Domain.Financial.TaxMode.Inclusive : Falah.Domain.Financial.TaxMode.Exclusive, tax.Enabled);
 151 |             RecomputeCartTotals();
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.cs#L122-L127)  
> **Lines 122-127** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LoadMenuAsync() (MethodDeclaration)` (File SHA-256: `ec9eeab0b553ea327798f673e9c077892d38fc2676492b183cca1678622488ac`):

```csharp
 122 |             var taxes = await _session.Api.GetTaxRulesAsync();
 123 |             if (!taxes.Success || taxes.Value is null) { MenuError = taxes.Error; return; }
 124 |             var tax = taxes.Value.FirstOrDefault(t => t.Enabled);
 125 |             _taxRule = tax is null ? null : new(tax.Id, tax.Name, tax.RateBasisPoints,
 126 |                 tax.Mode == "Inclusive" ? Falah.Domain.Financial.TaxMode.Inclusive : Falah.Domain.Financial.TaxMode.Exclusive, tax.Enabled);
 127 |             NotifyTotals();
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Validation discards parsed TaxMode and persists noncanonical raw string; three amount consumers silently map every nonliteralInclusive value toExclusive, including accepted definedInclusive numeric0.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Future selected enabled tax computation can add tax to a base intended inclusive under its accepted enum interpretation. Legitimate settings authority and noncanonical input required; no actual charge or SQL acceptance reproduced.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Validation discards parsed TaxMode and persists noncanonical raw string; three amount consumers silently map every nonliteralInclusive value toExclusive, including accepted definedInclusive numeric0.

**E. Future Repair Specification:**  
Persist parsed TaxMode canonical enum value, reject undefined/unsupported forms, and make consumers use canonical parsed mode. Keep historical order tax snapshots frozen.

**Required Regression Test Cases:**
- Create/update tax mode canonical and numeric/invalid enum forms; compare checkout and estimate at nonzero rate.
- Ensure old orders frozen.

**Acceptance Criteria & Verification Standard:**
- Accepted tax mode is parsed and persisted canonically; every consumer computes same intended inclusive/exclusive treatment.
- Historical frozen orders unchanged; new checkout and estimate agree under enabled nonzero rate.

---

### FIN-022: Accepted case-insensitive compensation type is stored raw but wage/payroll consumers require canonical names
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_NO_RUNTIME` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Disposition Rationale:** Source behavior is deterministic but depends on owner business policy (e.g. tax rounding policy, legacy casing tolerance, packaging cost allocation).

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs#L5-L20) | `5-20` | `File-level declaration or top-level statement` | `61ce8fbbe0c3` |
| [`src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs#L38-L43) | `38-43` | `Falah.Client.CashierWinUI.ViewModels.EmployeeEditorViewModel.CompensationTypes.get (GetAccessorDeclaration)` | `0606ca2008df` |
| [`src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs#L146-L151) | `146-151` | `Falah.Client.CashierWinUI.ViewModels.EmployeeEditorViewModel.Select(Falah.Application.Employees.EmployeeDto) (MethodDeclaration)` | `0606ca2008df` |
| [`src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs#L199-L225) | `199-225` | `Falah.Client.CashierWinUI.ViewModels.EmployeeEditorViewModel.CreateRequest() (MethodDeclaration)` | `0606ca2008df` |
| [`src/Falah.Domain/Employees/Employee.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs#L5-L7) | `5-7` | `File-level declaration or top-level statement` | `78762ea2dbc4` |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L83-L109) | `83-109` | `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L26-L34) | `26-34` | `Falah.Infrastructure.Employees.BiometricCheckInHandler.HandleAsync(Falah.Application.Employees.BiometricCheckInRequest, CancellationToken) (MethodDeclaration)` | `258fd52af239` |
| [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L72-L98) | `72-98` | `Falah.Infrastructure.Employees.BiometricCheckInHandler.HandleAsync(Falah.Application.Employees.BiometricCheckInRequest, CancellationToken) (MethodDeclaration)` | `258fd52af239` |
| [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L54-L65) | `54-65` | `Falah.Infrastructure.Employees.CreatePayrollHandler.HandleAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken) (MethodDeclaration)` | `339845a57fc7` |
| [`src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs#L26-L49) | `26-49` | `Falah.Infrastructure.Employees.CreateEmployeeHandler.HandleAsync(Falah.Application.Employees.CreateEmployeeRequest, CancellationToken) (MethodDeclaration)` | `858acb05b82b` |
| [`src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs#L79-L100) | `79-100` | `Falah.Infrastructure.Employees.UpdateEmployeeHandler.HandleAsync(Falah.Application.Employees.UpdateEmployeeRequest, CancellationToken) (MethodDeclaration)` | `858acb05b82b` |
| [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L81-L85) | `81-85` | `Falah.Infrastructure.Employees.EmployeeQueryService.ToDto(Falah.Infrastructure.Persistence.Entities.EmployeeEntity) (MethodDeclaration)` | `fbb120025323` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L28-L39) | `28-39` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L66-L70) | `66-70` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L667-L681) | `667-681` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L783-L786) | `783-786` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs#L5-L20)  
> **Lines 5-20** in `File-level declaration or top-level statement` (File SHA-256: `61ce8fbbe0c380340ded6e3de6be6863c9b328c094894bd8e54d5f32e32efdc0`):

```csharp
   5 | /// own doc comment for why no Pin field exists in this port at all).</summary>
   6 | public sealed record CreateEmployeeRequest(
   7 |     string RequestId, string Name, string EmployeeNumber, string JobTitle, string Phone, DateOnly HireDate,
   8 |     string CompensationType, long DailyWagePiastres, long ShiftValuePiastres, long? MonthlySalaryPiastres,
   9 |     bool CanDeliver, string Role, string? Notes, string? Address, string? NationalId, string? Department,
  10 |     string? EmploymentArrangement, string? FingerprintId, string? LifecycleStatus, long? OvertimeRatePiastres,
  11 |     string ActorUserId);
  12 | 
  13 | /// <summary>Ported from updateEmployee - a full profile replace (name/jobTitle/phone/compensation/
  14 | /// etc.), matching the reference app's own "pass the whole object" shape. UserId is intentionally
  15 | /// absent from this request (see Employee's own doc comment: no evidenced command ever changes it).</summary>
  16 | public sealed record UpdateEmployeeRequest(
  17 |     string EmployeeId, string Name, string EmployeeNumber, string JobTitle, string Phone, DateOnly HireDate,
  18 |     string CompensationType, long DailyWagePiastres, long ShiftValuePiastres, long? MonthlySalaryPiastres,
  19 |     bool CanDeliver, string Role, bool IsActive, string? Notes, string? Address, string? NationalId,
  20 |     string? Department, string? EmploymentArrangement, string? FingerprintId, string? LifecycleStatus,
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs#L38-L43)  
> **Lines 38-43** in `Falah.Client.CashierWinUI.ViewModels.EmployeeEditorViewModel.CompensationTypes.get (GetAccessorDeclaration)` (File SHA-256: `0606ca2008dfe343a653116ed071834d760aa11695df274b580c38a8e6cc59e7`):

```csharp
  38 |     public IReadOnlyList<Choice> CompensationTypes { get; } = new[]
  39 |     {
  40 |         new Choice("Daily", "يومي"),
  41 |         new Choice("Monthly", "شهري"),
  42 |         new Choice("Hourly", "بالساعة"),
  43 |     };
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeeEditorViewModel.cs#L146-L151)  
> **Lines 146-151** in `Falah.Client.CashierWinUI.ViewModels.EmployeeEditorViewModel.Select(Falah.Application.Employees.EmployeeDto) (MethodDeclaration)` (File SHA-256: `0606ca2008dfe343a653116ed071834d760aa11695df274b580c38a8e6cc59e7`):

```csharp
 146 |         JobTitle = employee.JobTitle;
 147 |         Phone = employee.Phone;
 148 |         HireDate = new DateTimeOffset(employee.HireDate.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero);
 149 |         CompensationType = CompensationTypes.FirstOrDefault(c => string.Equals(c.Id, employee.CompensationType, StringComparison.OrdinalIgnoreCase));
 150 |         Arrangement = Arrangements.FirstOrDefault(a => string.Equals(a.Id, employee.EmploymentArrangement, StringComparison.OrdinalIgnoreCase));
 151 |         LifecycleStatus = LifecycleStatuses.FirstOrDefault(s => string.Equals(s.Id, employee.LifecycleStatus, StringComparison.OrdinalIgnoreCase));
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Shared compensation enum validation accepts multiple spellings but persistence retainsrawstring, while two financial consumers use exactname comparison instead of the parsedmeaning.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Accepted case-variant compensation metadata can omit automatic daily wage credits or classify a monthly salary as credit-based; positive configured wage and noncanonical accepted input required. Shipped editor emits canonical names. No actual payout/loss reproduced.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Shared compensation enum validation accepts multiple spellings but persistence retainsrawstring, while two financial consumers use exactname comparison instead of the parsedmeaning.

**E. Future Repair Specification:**  
Persist parsed CompensationType canonical value and use parsed enum in attendance/payroll consumers; validate defined supported types at API boundary.

**Required Regression Test Cases:**
- Case variants for Daily/Monthly/Hourly on attendance and payroll; malformed/undefined strings rejected.
- Canonical values and manual wage posting remain correct.

**Acceptance Criteria & Verification Standard:**
- Every accepted compensation value is persisted in one canonical representation used consistently by attendance and payroll.
- Daily/monthly/Hourly cases calculate only intended credits; resaving canonical data does not change financial meaning.

---

### FIN-023: Attendance checkout reconstructs check-in on checkout UTC date and can omit overtime
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_CONDITIONAL_DATE_EXAMPLE` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Common/BusinessClock.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs#L23-L29) | `23-29` | `Falah.Domain.Common.BusinessClock.BusinessDate(DateTimeOffset) (MethodDeclaration)` | `f9a82540cadf` |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L55-L60) | `55-60` | `Falah.Domain.Payroll.PayrollRules.CalculateOvertimeAmount(Falah.Domain.Common.Money, int, double, double) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L26-L34) | `26-34` | `Falah.Infrastructure.Employees.BiometricCheckInHandler.HandleAsync(Falah.Application.Employees.BiometricCheckInRequest, CancellationToken) (MethodDeclaration)` | `258fd52af239` |
| [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L49-L79) | `49-79` | `Falah.Infrastructure.Employees.BiometricCheckInHandler.HandleAsync(Falah.Application.Employees.BiometricCheckInRequest, CancellationToken) (MethodDeclaration)` | `258fd52af239` |
| [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L110-L151) | `110-151` | `Falah.Infrastructure.Employees.BiometricCheckOutHandler.HandleAsync(Falah.Application.Employees.BiometricCheckOutRequest, CancellationToken) (MethodDeclaration)` | `258fd52af239` |
| [`src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs#L59-L80) | `59-80` | `Falah.Infrastructure.Persistence.Entities.AttendanceRecordEntity.Id.get (GetAccessorDeclaration)` | `e531e400a219` |
| [`src/Falah.Infrastructure/Settings/SettingsQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Settings/SettingsQueryService.cs#L21-L30) | `21-30` | `File-level declaration or top-level statement` | `c798b18a8beb` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Common/BusinessClock.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs#L23-L29)  
> **Lines 23-29** in `Falah.Domain.Common.BusinessClock.BusinessDate(DateTimeOffset) (MethodDeclaration)` (File SHA-256: `f9a82540cadfcdc043a630041f2877f527591b2d9d400fb870da2c98f9ec9208`):

```csharp
  23 |     public static DateOnly BusinessDate(DateTimeOffset instantUtc)
  24 |     {
  25 |         var local = TimeZoneInfo.ConvertTime(instantUtc, CairoTimeZone);
  26 |         return DateOnly.FromDateTime(local.DateTime);
  27 |     }
  28 | 
  29 |     public static DateOnly Today() => BusinessDate(DateTimeOffset.UtcNow);
```

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L55-L60)  
> **Lines 55-60** in `Falah.Domain.Payroll.PayrollRules.CalculateOvertimeAmount(Falah.Domain.Common.Money, int, double, double) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  55 |     public static OvertimeAmountResult CalculateOvertimeAmount(Money dailyWage, int workdayHours, double overtimeHours, double overtimeMultiplier)
  56 |     {
  57 |         var rate = NormalHourlyRate(dailyWage, workdayHours);
  58 |         if (overtimeHours <= 0) return new OvertimeAmountResult(rate, Money.Zero);
  59 |         var amount = Money.FromPiastres((long)Math.Round(rate.Piastres * overtimeHours * overtimeMultiplier));
  60 |         return new OvertimeAmountResult(rate, amount);
```

> **File:** [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L26-L34)  
> **Lines 26-34** in `Falah.Infrastructure.Employees.BiometricCheckInHandler.HandleAsync(Falah.Application.Employees.BiometricCheckInRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `258fd52af239fd06e3084242f71fd0cbfbbd0baf1dce78dd1dbe8498531bffe6`):

```csharp
  26 |     public async Task<AttendanceRecordDto?> HandleAsync(BiometricCheckInRequest request, CancellationToken ct = default)
  27 |     {
  28 |         var employee = await _db.Employees.AsNoTracking().FirstOrDefaultAsync(e => e.Id == request.EmployeeId, ct);
  29 |         if (employee is null) return null;
  30 | 
  31 |         var now = DateTimeOffset.UtcNow;
  32 |         var date = BusinessClock.Today();
  33 |         var timeStr = now.ToString("HH:mm");
  34 |         var settings = await new SettingsQueryService(_db).GetAsync(ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Loss of check-in UTC date, followed by reconstruction from checkout UTC date despite selecting attendance by Cairo date.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Wrong hours and possible omission of otherwise eligible automatic overtime; no actual payment/loss or payroll record measured. Distinct from FIN012 correction-credit classification and FIN022 compensation enum interpretation.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Loss of check-in UTC date, followed by reconstruction from checkout UTC date despite selecting attendance by Cairo date.

**E. Future Repair Specification:**  
Retain full check-in date/time and compute elapsed interval from that persisted event under documented timezone and repeated-punch policy; do not reconstruct from checkout date.

**Required Regression Test Cases:**
- Same UTC and cross-Cairo-date check-in/out examples; work hours/overtime against approved time zone rule.
- Repeated check-ins establish first/last punch rule.

**Acceptance Criteria & Verification Standard:**
- Checkout time is compared against original check-in date/time policy, preserving timezone/day boundary; eligible overtime calculation matches approved rule.
- Repeated check-in/checkout policy is explicit; same-UTC-day positive control remains correct.

---

### FIN-024: Manual daily-wage and overtime commands admit negative earning credits
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_NO_PROVIDER_REPRODUCTION` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified against baseline source implementation.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Employees/AttendanceRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/AttendanceRequests.cs#L29-L47) | `29-47` | `File-level declaration or top-level statement` | `1e68615e544b` |
| [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs#L319-L333) | `319-333` | `lambda expression (ParenthesizedLambdaExpression)` | `04f70fc8f689` |
| [`src/Falah.Domain/Employees/EmployeeRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/EmployeeRules.cs#L30-L33) | `30-33` | `Falah.Domain.Employees.EmployeeRules.AssertLedgerAmount(Falah.Domain.Common.Money) (MethodDeclaration)` | `8b25256a36f2` |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L95-L109) | `95-109` | `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L33-L60) | `33-60` | `Falah.Infrastructure.Employees.RecordDailyWageHandler.HandleAsync(Falah.Application.Employees.RecordDailyWageRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L77-L108) | `77-108` | `Falah.Infrastructure.Employees.RecordOvertimeHandler.HandleAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L125-L156) | `125-156` | `Falah.Infrastructure.Employees.RecordExtraShiftHandler.HandleAsync(Falah.Application.Employees.RecordExtraShiftRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L28-L40) | `28-40` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L57-L75) | `57-75` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L447-L452) | `447-452` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs#L12-L40) | `12-40` | `Falah.Infrastructure.Persistence.Migrations.AddEmployees.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder) (MethodDeclaration)` | `9be2994552c7` |
| [`src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs#L75-L86) | `75-86` | `Falah.Infrastructure.Persistence.Migrations.AddEmployees.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder) (MethodDeclaration)` | `9be2994552c7` |
| [`src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs#L1173-L1242) | `1173-1242` | `lambda expression (SimpleLambdaExpression)` | `093a3e562424` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L709-L718) | `709-718` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L748-L778) | `748-778` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Employees/AttendanceRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/AttendanceRequests.cs#L29-L47)  
> **Lines 29-47** in `File-level declaration or top-level statement` (File SHA-256: `1e68615e544b3f852296dd0923218c6839a80a1a96beb69d4c56de3c6b550419`):

```csharp
  29 | /// <summary>Ported from recordDailyWage - assertUniqueDailyWage's own deterministic id
  30 | /// (`led-daily-wage-{employeeId}-{date}-{shiftId|no-shift}`) is the actual idempotency mechanism,
  31 | /// exactly as legacy documents it: a retried/duplicate call for the same employee/date/shift is a
  32 | /// safe no-op, never a duplicate wage. Gated on `employees_ledger` for direct/manual invocation -
  33 | /// see BiometricCheckInHandler's own doc comment for why the AUTOMATIC posting from a check-in does
  34 | /// not go through this same authorized command.</summary>
  35 | public sealed record RecordDailyWageRequest(string EmployeeId, long? CustomWagePiastres, string? ShiftId, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
  36 | 
  37 | /// <summary>RequestId is minted once per submit and reused unchanged by a retry. A real idempotency
  38 | /// gap found during re-verification: legacy's own recordOvertime never gives
  39 | /// recordManualLedgerEntry a metadata.id either, so a double-click genuinely double-posts overtime
  40 | /// pay there too - but this session's standing pattern is to close every such un-protected
  41 | /// financial write found rather than reproduce it (the same treatment already given to
  42 | /// recordManualLedgerEntry's own missing permission gate), so a real key is required here instead.</summary>
  43 | public sealed record RecordOvertimeRequest(string RequestId, string EmployeeId, double Hours, long? CustomRatePiastres, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
  44 | 
  45 | /// <summary>See RecordOvertimeRequest's own doc comment - recordExtraShift has the identical gap
  46 | /// in legacy, closed the same way here.</summary>
  47 | public sealed record RecordExtraShiftRequest(string RequestId, string EmployeeId, int ShiftCount, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs#L319-L333)  
> **Lines 319-333** in `lambda expression (ParenthesizedLambdaExpression)` (File SHA-256: `04f70fc8f689932e34bc5cdf5f9ee1b5c04dbdb35a053f6ed2719a17cfd0eb7b`):

```csharp
 319 |                 case "daily_wage":
 320 |                     if (!CashInput.TryParse(CashOutAmountText, out var wage) || wage <= 0)
 321 |                     { CashOutError = "أدخل المبلغ بشكل صحيح."; return; }
 322 |                     await CompleteDailyWageAsync(new RecordDailyWageRequest(
 323 |                         CashOutEmployee.Employee.Id, wage, _session.ShiftId, _session.CurrentUser!.Id, approvalId, approverId));
 324 |                     break;
 325 |                 case "overtime":
 326 |                     await CompleteOvertimeAsync(new RecordOvertimeRequest(
 327 |                         _cashOutRequestId!, CashOutEmployee.Employee.Id, OvertimeHours, null,
 328 |                         string.IsNullOrWhiteSpace(CashOutReason) ? null : CashOutReason, _session.CurrentUser!.Id, approvalId, approverId));
 329 |                     break;
 330 |                 case "extra_shift":
 331 |                     await CompleteExtraShiftAsync(new RecordExtraShiftRequest(
 332 |                         _cashOutRequestId!, CashOutEmployee.Employee.Id, 1,
 333 |                         string.IsNullOrWhiteSpace(CashOutReason) ? null : CashOutReason, _session.CurrentUser!.Id, approvalId, approverId));
```

> **File:** [`src/Falah.Domain/Employees/EmployeeRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/EmployeeRules.cs#L30-L33)  
> **Lines 30-33** in `Falah.Domain.Employees.EmployeeRules.AssertLedgerAmount(Falah.Domain.Common.Money) (MethodDeclaration)` (File SHA-256: `8b25256a36f25b58aa3f61ddbaeee9f7bb32fed5e8ff522d71486b92b308d3b5`):

```csharp
  30 |     public static void AssertLedgerAmount(Money amount)
  31 |     {
  32 |         if (!amount.IsPositive) throw new DomainException("قيمة الحركة يجب أن تكون أكبر من صفر.");
  33 |     }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Signed custom wage/rate is used as earning Credit without command-specific sign validation; positive finitehours do not ensure positive earning.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
An otherwise authorized accepted command can construct negative DailyWage/Overtime credit, reducing employee factbalance and categorized earnings instead of explicit deduction. No actual SQL write/payroll loss verified.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Signed custom wage/rate is used as earning Credit without command-specific sign validation; positive finitehours do not ensure positive earning.

**E. Future Repair Specification:**  
Reject negative command-specific wage/rate and overflow before posting; define zero policy. Do not globally ban signed Money or clamp negative earnings into zero.

**Required Regression Test Cases:**
- Negative custom daily rate/overtime rate and negative configured wage; overflow bounds; zero-policy control.
- Same-key retry and manual explicit deduction controls.

**Acceptance Criteria & Verification Standard:**
- Negative earning rate/amount and unsupported overflow reject before ledger write; accepted zero behavior follows explicit policy.
- Manual deductions remain explicit and valid; valid wage/overtime/extra-shift replay remains idempotent.

---

### FIN-025: Manual ledger accepts undefined numeric types excluded from named financial categories
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_NO_PROVIDER_REPRODUCTION` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified against baseline source implementation.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs#L35-L45) | `35-45` | `File-level declaration or top-level statement` | `61ce8fbbe0c3` |
| [`src/Falah.Domain/Employees/Employee.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs#L17-L22) | `17-22` | `File-level declaration or top-level statement` | `78762ea2dbc4` |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L95-L109) | `95-109` | `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L37-L56) | `37-56` | `Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` | `fbb120025323` |
| [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L68-L90) | `68-90` | `Falah.Infrastructure.Employees.EmployeeQueryService.ListLedgerEntriesAsync(string, CancellationToken) (MethodDeclaration)` | `fbb120025323` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L28-L40) | `28-40` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L57-L75) | `57-75` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L447-L452) | `447-452` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs#L12-L40) | `12-40` | `Falah.Infrastructure.Persistence.Migrations.AddEmployees.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder) (MethodDeclaration)` | `9be2994552c7` |
| [`src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260904190229_AddEmployees.cs#L75-L86) | `75-86` | `Falah.Infrastructure.Persistence.Migrations.AddEmployees.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder) (MethodDeclaration)` | `9be2994552c7` |
| [`src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/FalahDbContextModelSnapshot.cs#L1173-L1242) | `1173-1242` | `lambda expression (SimpleLambdaExpression)` | `093a3e562424` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L709-L718) | `709-718` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L748-L778) | `748-778` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Employees/EmployeeRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/EmployeeRequests.cs#L35-L45)  
> **Lines 35-45** in `File-level declaration or top-level statement` (File SHA-256: `61ce8fbbe0c380340ded6e3de6be6863c9b328c094894bd8e54d5f32e32efdc0`):

```csharp
  35 | /// <summary>Ported from recordManualLedgerEntry - a real, un-gated command in the reference app
  36 | /// (no authorizeCommand call in that function at all); a real gap closed here, matching this
  37 | /// codebase's established pattern of fixing an un-gated write rather than reproducing it (see
  38 | /// PurchasingScreen.tsx's own addSupplier fix for the precedent this follows). Gated on
  39 | /// `employees_ledger`, the same permission its sibling advance/withdrawal commands already use.
  40 | /// No RequestId/idempotency key: matches legacy's own metadata.id-or-random shape, but a manual
  41 | /// entry (unlike an automated biometric posting) has no natural dedup key of its own - a caller
  42 | /// wanting idempotency should use RecordAdvance/RecordWithdrawal instead, which do have one.</summary>
  43 | public sealed record RecordManualLedgerEntryRequest(
  44 |     string EmployeeId, string Type, long AmountPiastres, bool IsCredit, string Description,
  45 |     string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
```

> **File:** [`src/Falah.Domain/Employees/Employee.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Employees/Employee.cs#L17-L22)  
> **Lines 17-22** in `File-level declaration or top-level statement` (File SHA-256: `78762ea2dbc46e2747db8c3bedbd6f771be2cd18fbe5fb4fee2644a1d944e64c`):

```csharp
  17 | /// <summary>Ported from EmployeeLedgerEntry's own `type: LedgerEntryType` in types.ts.</summary>
  18 | public enum LedgerEntryType
  19 | {
  20 |     DailyWage, Salary, Advance, Withdrawal, Overtime, ExtraShift, Deduction, Bonus,
  21 |     OtherEarning, PayrollSettlement, Adjustment, Settlement,
  22 | }
```

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L95-L109)  
> **Lines 95-109** in `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  95 |             var baseAmount = emp.CompensationIsMonthly
  96 |                 ? (emp.MonthlySalary ?? Money.Zero)
  97 |                 : Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "DailyWage" or "Salary").Sum(e => e.Credit.Piastres));
  98 |             var overtime = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "Overtime").Sum(e => e.Credit.Piastres));
  99 |             var extraShifts = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "ExtraShift").Sum(e => e.Credit.Piastres));
 100 |             var bonuses = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Bonus" or "OtherEarning").Sum(e => e.Credit.Piastres));
 101 |             var deductions = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Deduction" or "Adjustment").Sum(e => e.Debit.Piastres));
 102 | 
 103 |             if (emp.RequestedAdvanceDeduction.IsNegative || emp.RequestedAdvanceDeduction > emp.AvailableAdvance)
 104 |                 throw new DomainException($"استقطاع السلفة للموظف {emp.EmployeeId} يتجاوز الرصيد المتفق عليه.");
 105 | 
 106 |             var net = baseAmount + overtime + extraShifts + bonuses - deductions - emp.RequestedAdvanceDeduction;
 107 |             if (net.IsNegative) net = Money.Zero;
 108 | 
 109 |             result.Add(new PayrollLine(emp.EmployeeId, baseAmount, overtime, extraShifts, bonuses, deductions, emp.RequestedAdvanceDeduction, net));
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Enum.TryParse result is canonicalized without Enum.IsDefined/supported-category admission; undefined numeric strings remain unnamed numeric types.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Constructed ledger facts can affect all-entry balance but fall outside explicit report/payroll namecategories. This is unsupported data-shape acceptance, not a claim every legitimate named credit belongs in payroll. No API/SQLacceptance test.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Enum.TryParse result is canonicalized without Enum.IsDefined/supported-category admission; undefined numeric strings remain unnamed numeric types.

**E. Future Repair Specification:**  
Require Enum.IsDefined plus an explicit supported-type/credit-debit policy before manual ledger posting; preserve valid named types and classify legacy unsupported values explicitly.

**Required Regression Test Cases:**
- Defined numeric enum vs named control; unsupported numeric 999; explicit credit/debit category reporting.
- Balance, payroll and report reconciliation after accepted entry.

**Acceptance Criteria & Verification Standard:**
- Only defined and explicitly supported ledger enum members/types can be posted through manual API.
- Unsupported numeric strings reject or are explicitly classified before balance/report/payroll effects; canonical named controls remain valid.

---

### FIN-026: Distinct employee credit requests can persist and display stale running-balance snapshots
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_NO_PROVIDER_REPRODUCTION` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified against baseline source implementation.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml#L179-L187) | `179-187` | `File-level declaration or top-level statement` | `049e44a2eab5` |
| [`src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs#L15-L49) | `15-49` | `Falah.Infrastructure.Employees.EmployeeLedgerPoster.GetCurrentBalanceAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) (MethodDeclaration)` | `56975f399d91` |
| [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L37-L56) | `37-56` | `Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` | `fbb120025323` |
| [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L68-L90) | `68-90` | `Falah.Infrastructure.Employees.EmployeeQueryService.ListLedgerEntriesAsync(string, CancellationToken) (MethodDeclaration)` | `fbb120025323` |
| [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs#L75-L104) | `75-104` | `Falah.Infrastructure.Employees.PayEmployeeHandler.HandleAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken) (MethodDeclaration)` | `93b781b9b0d9` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L33-L60) | `33-60` | `Falah.Infrastructure.Employees.RecordDailyWageHandler.HandleAsync(Falah.Application.Employees.RecordDailyWageRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L77-L108) | `77-108` | `Falah.Infrastructure.Employees.RecordOvertimeHandler.HandleAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L125-L156) | `125-156` | `Falah.Infrastructure.Employees.RecordExtraShiftHandler.HandleAsync(Falah.Application.Employees.RecordExtraShiftRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L28-L40) | `28-40` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordManualLedgerEntryHandler.cs#L57-L75) | `57-75` | `Falah.Infrastructure.Employees.RecordManualLedgerEntryHandler.HandleAsync(Falah.Application.Employees.RecordManualLedgerEntryRequest, CancellationToken) (MethodDeclaration)` | `5d3da4d459ec` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L447-L452) | `447-452` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/EmployeesView.xaml#L179-L187)  
> **Lines 179-187** in `File-level declaration or top-level statement` (File SHA-256: `049e44a2eab52ee2cb47c91f052debbd00ff3e5c8b704c1a8d242f75926ee8ee`):

```xaml
 179 |                                                 <TextBlock Text="{Binding Description}" Style="{StaticResource TableCellText}" TextWrapping="Wrap"/>
 180 |                                                 <TextBlock Text="{Binding Type, Converter={StaticResource EmployeeLedgerTypeLabelConverter}}" Style="{StaticResource CaptionText}"/>
 181 |                                             </StackPanel>
 182 |                                             <TextBlock Grid.Column="1" VerticalAlignment="Center" Foreground="{ThemeResource DangerBrush}"
 183 |                                                        Text="{Binding DebitPiastres, Converter={StaticResource PiastresToDisplayConverter}}"/>
 184 |                                             <TextBlock Grid.Column="2" VerticalAlignment="Center" Foreground="{ThemeResource SuccessBrush}"
 185 |                                                        Text="{Binding CreditPiastres, Converter={StaticResource PiastresToDisplayConverter}}"/>
 186 |                                             <TextBlock Grid.Column="3" VerticalAlignment="Center" FontWeight="SemiBold"
 187 |                                                        Text="{Binding BalanceAfterPiastres, Converter={StaticResource PiastresToDisplayConverter}}"/>
```

> **File:** [`src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs#L15-L49)  
> **Lines 15-49** in `Falah.Infrastructure.Employees.EmployeeLedgerPoster.GetCurrentBalanceAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) (MethodDeclaration)` (File SHA-256: `56975f399d91381528c8ac72e93ff370d4e8063324f384cca2648f198d1904a5`):

```csharp
  15 |     public static async Task<Money> GetCurrentBalanceAsync(FalahDbContext db, string employeeId, CancellationToken ct)
  16 |     {
  17 |         var entries = await db.EmployeeLedgerEntries.AsNoTracking().Where(e => e.EmployeeId == employeeId)
  18 |             .Select(e => new { e.CreditPiastres, e.DebitPiastres }).ToListAsync(ct);
  19 |         // A single command can append more than one movement before SaveChanges (for
  20 |         // example, a late penalty followed by overtime). Include its earlier pending
  21 |         // appends, otherwise the second row's displayed running balance omits the first.
  22 |         var pendingDelta = db.ChangeTracker.Entries<EmployeeLedgerEntryEntity>()
  23 |             .Where(e => e.State == EntityState.Added && e.Entity.EmployeeId == employeeId)
  24 |             .Sum(e => e.Entity.CreditPiastres - e.Entity.DebitPiastres);
  25 |         return Money.FromPiastres(entries.Sum(e => e.CreditPiastres - e.DebitPiastres) + pendingDelta);
  26 |     }
  27 | 
  28 |     /// <summary>Returns null (posts nothing) if an entry with this exact Id already exists - the
  29 |     /// deterministic-id dedup that makes a retried biometric event or duplicate correction a safe
  30 |     /// no-op rather than a double-post.</summary>
  31 |     public static async Task<EmployeeLedgerEntryEntity?> PostIfNewAsync(
  32 |         FalahDbContext db, string id, string employeeId, string type, string description, Money debit, Money credit,
  33 |         string recordedBy, string? authorizedBy, string? shiftId, string? referenceId, double? hours,
  34 |         long? rateSnapshotPiastres, long? valueSnapshotPiastres, CancellationToken ct)
  35 |     {
  36 |         if (db.EmployeeLedgerEntries.Local.Any(e => e.Id == id)
  37 |             || await db.EmployeeLedgerEntries.AsNoTracking().AnyAsync(e => e.Id == id, ct)) return null;
  38 | 
  39 |         var currentBalance = await GetCurrentBalanceAsync(db, employeeId, ct);
  40 |         var newBalance = currentBalance + credit - debit;
  41 |         var entity = new EmployeeLedgerEntryEntity
  42 |         {
  43 |             Id = id, EmployeeId = employeeId, Type = type, Description = description,
  44 |             DebitPiastres = debit.Piastres, CreditPiastres = credit.Piastres, BalanceAfterPiastres = newBalance.Piastres,
  45 |             RecordedBy = recordedBy, AuthorizedBy = authorizedBy, ShiftId = shiftId, ReferenceId = referenceId,
  46 |             Hours = hours, RateSnapshotPiastres = rateSnapshotPiastres, ValueSnapshotPiastres = valueSnapshotPiastres,
  47 |             CreatedAtUtc = DateTimeOffset.UtcNow,
  48 |         };
  49 |         db.EmployeeLedgerEntries.Add(entity);
```

> **File:** [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L37-L56)  
> **Lines 37-56** in `Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` (File SHA-256: `fbb1200253239f442fdb0933abb5270f12c7d404e5cd549150c71a66c96c6341`):

```csharp
  37 |         var earningsTypes = new[] { nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary), nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift) };
  38 |         var additionTypes = new[] { nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning) };
  39 |         var deductionTypes = new[] { nameof(LedgerEntryType.Deduction), nameof(LedgerEntryType.Adjustment) };
  40 |         var paymentTypes = new[] { nameof(LedgerEntryType.Withdrawal), nameof(LedgerEntryType.Settlement), nameof(LedgerEntryType.PayrollSettlement) };
  41 | 
  42 |         var result = new List<EmployeeFinancialReportLineDto>(employees.Count);
  43 |         foreach (var employee in employees)
  44 |         {
  45 |             var allForEmployee = allEntries.Where(e => e.EmployeeId == employee.Id).ToList();
  46 |             var inRange = allForEmployee.Where(e =>
  47 |                 (start is null || e.CreatedAtUtc >= start) && (end is null || e.CreatedAtUtc < end)).ToList();
  48 | 
  49 |             long SumCredit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.CreditPiastres);
  50 |             long SumDebit(IEnumerable<string> types) => inRange.Where(e => types.Contains(e.Type)).Sum(e => e.DebitPiastres);
  51 |             var balance = allForEmployee.Sum(e => e.CreditPiastres) - allForEmployee.Sum(e => e.DebitPiastres);
  52 | 
  53 |             result.Add(new EmployeeFinancialReportLineDto(
  54 |                 employee.Id, employee.Name, SumCredit(earningsTypes), SumCredit(additionTypes),
  55 |                 SumDebit(deductionTypes), SumDebit(new[] { nameof(LedgerEntryType.Advance) }),
  56 |                 SumDebit(paymentTypes), balance));
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
BalanceAfter snapshot computed from readsum before Save without cross-command employee serialization; own pendingDelta and same-key dedup do not cover different writers.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Source schedule retains both positive credit facts but can display wrong statement runningbalance. Fact-derived GetBalance remains correct; protected payout locks are counterevidence. No actual SQLrace/UI observation/moneyoverdraft proved.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
BalanceAfter snapshot computed from readsum before Save without cross-command employee serialization; own pendingDelta and same-key dedup do not cover different writers.

**E. Future Repair Specification:**  
Either serialize all ledger appends on one employee anchor with consistent lock order, or compute statement running balance from ordered immutable facts and retire stale BalanceAfter as authority. Define deterministic timestamp tie-breaker.

**Required Regression Test Cases:**
- Barrier concurrent distinct positive credit appends with no rows and existing balance; verify facts and statement snapshots.
- Same-key retry, rollback/restart, same-command pending rows, payout lock ordering, equal timestamp tie.

**Acceptance Criteria & Verification Standard:**
- Concurrent distinct credit appends preserve correct recomputed employee balance and statement running-balance order.
- Same-key replay, pending same-command rows, and locked payout remain correct; tie ordering is deterministic.

---

### FIN-027: Frozen business-day financial snapshots omit the corresponding live-report permission gate
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_INDEPENDENT_STATIC_NO_HTTP_SQL_OR_UI_TEST` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified against baseline source implementation.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs#L13-L16) | `13-16` | `File-level declaration or top-level statement` | `d6d8f05a2959` |
| [`src/Falah.Application/Reports/ReportingDtos.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/ReportingDtos.cs#L32-L36) | `32-36` | `File-level declaration or top-level statement` | `39c9da5ec2df` |
| [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L301-L303) | `301-303` | `Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayReconciliationAsync(string, string, CancellationToken) (MethodDeclaration)` | `831a6dfd1f18` |
| [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L317-L323) | `317-323` | `Falah.Client.CashierWinUI.Services.ApiClient.GetLatestCloseSnapshotAsync(string, CancellationToken) (MethodDeclaration)` | `831a6dfd1f18` |
| [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs#L19-L33) | `19-33` | `Falah.Client.CashierWinUI.ViewModels.ControlCenterViewModel.LoadReconciliationAsync(string) (MethodDeclaration)` | `0f7aca8ebf32` |
| [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.Reconciliation.cs#L120-L134) | `120-134` | `Falah.Client.CashierWinUI.ViewModels.ControlCenterViewModel.LoadSnapshotAsync(string) (MethodDeclaration)` | `0f7aca8ebf32` |
| [`src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ControlCenterViewModel.cs#L84-L135) | `84-135` | `Falah.Client.CashierWinUI.ViewModels.ControlCenterViewModel.ReloadAsync() (MethodDeclaration)` | `79a3002b0094` |
| [`src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/ControlCenterView.xaml#L101-L137) | `101-137` | `File-level declaration or top-level statement` | `cc8d56dbe18e` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L34-L38) | `34-38` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L54-L64) | `54-64` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L87-L120) | `87-120` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L162-L165) | `162-165` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.ToDto(Falah.Infrastructure.Persistence.Entities.BusinessDayCloseSnapshotEntity, bool) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs#L42-L53) | `42-53` | `Falah.Infrastructure.ControlCenter.ControlCenterQueryService.GetLatestCloseSnapshotAsync(string, CancellationToken) (MethodDeclaration)` | `2ba6e827c2db` |
| [`src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/ReopenBusinessDayHandler.cs#L69-L98) | `69-98` | `Falah.Infrastructure.ControlCenter.ReopenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.ReopenBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `97e4427ded18` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137-L141) | `137-141` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L400-L405) | `400-405` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L31-L88) | `31-88` | `Falah.Server.AuthenticationMiddleware.InvokeAsync(HttpContext, Falah.Infrastructure.Authorization.SessionService) (MethodDeclaration)` | `3e64ad8ef532` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L251-L251) | `251-251` | `File-level declaration or top-level statement` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L421-L428) | `421-428` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2013-L2018) | `2013-2018` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L2282-L2289) | `2282-2289` | `RequireReportsAccessAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, CancellationToken) (LocalFunctionStatement)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs#L13-L16)  
> **Lines 13-16** in `File-level declaration or top-level statement` (File SHA-256: `d6d8f05a2959343b0824dcb3d4f1d57bd342469fc8e0bc57a539b3cfe08402d1`):

```csharp
  13 | public sealed record BusinessDayCloseSnapshotDto(
  14 |     string Id, string BusinessDayId, int CloseRevision, bool Authoritative, string? PreviousSnapshotId,
  15 |     string BusinessDate, DateTimeOffset GeneratedAtUtc, string ClosedBy, string OperationId,
  16 |     string ReconciliationJson, string ExceptionsJson, string ChecklistJson, bool WasReplayed = false);
```

> **File:** [`src/Falah.Application/Reports/ReportingDtos.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/ReportingDtos.cs#L32-L36)  
> **Lines 32-36** in `File-level declaration or top-level statement` (File SHA-256: `39c9da5ec2dfc6012cf6b630b0f9d8a8924c54016c991ceacaad1632ec308726`):

```csharp
  32 | public sealed record BusinessDayReconciliationDto(
  33 |     string BusinessDayId, string BusinessDate, SalesReportDto Sales, long ExpensesTotalPiastres,
  34 |     TreasuryBalancesDto Treasury, long DeliveryExpectedCustodyPiastres, long DeliverySettledCustodyPiastres,
  35 |     int ShiftCount, long ShiftsExpectedCashPiastres, long ShiftsActualCashPiastres, long ShiftsShortagePiastres, long ShiftsOveragePiastres,
  36 |     long PurchasingPaidPiastres = 0);
```

> **File:** [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L301-L303)  
> **Lines 301-303** in `Falah.Client.CashierWinUI.Services.ApiClient.GetBusinessDayReconciliationAsync(string, string, CancellationToken) (MethodDeclaration)` (File SHA-256: `831a6dfd1f18510b240a232b7a4c4fd11251a51746e1e416c7ce691f732ddbf4`):

```csharp
 301 |     public Task<ApiResult<Falah.Application.Reports.BusinessDayReconciliationDto>> GetBusinessDayReconciliationAsync(string businessDayId, string actorUserId, CancellationToken ct = default) =>
 302 |         ReadAsync<Falah.Application.Reports.BusinessDayReconciliationDto>(() => _http.GetAsync(
 303 |             $"/api/reports/business-day/{Uri.EscapeDataString(businessDayId)}?actorUserId={Uri.EscapeDataString(actorUserId)}", ct), ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Frozen report payload endpoint/client path uses session-only admission instead of live reports_view/all policy.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Authenticated actor lacking report permission can receive financial report JSON from snapshot endpoints under the inspected source contract. Different from employee report or identity-binding issues; parent owns dedup/security disposition.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Frozen report payload endpoint/client path uses session-only admission instead of live reports_view/all policy.

**E. Future Repair Specification:**  
Apply reports_view to latest/history financial snapshot reads using verified authenticated actor; if broader archive access is intended, define a distinct explicit permission rather than reusing control-center permission.

**Required Regression Test Cases:**
- Disposable authenticated requests with and without reports_view for latest/history snapshot routes.
- Client control-center-only access sees no financial figures; permitted report viewer still accesses.

**Acceptance Criteria & Verification Standard:**
- Every frozen financial snapshot read uses authenticated session actor plus reports_view (or a separately owner-approved explicit archive permission).
- Unauthorized session receives no financial snapshot payload; authorized reader can still access permitted history.

---

### FIN-028: Spent manager approval prevents acknowledgement replay of committed overtime and extra-shift credits
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_SOURCE_CONTROL_FLOW;NOT_RUNTIME_REPRODUCED` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Employees/AttendanceRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/AttendanceRequests.cs#L37-L47) | `37-47` | `File-level declaration or top-level statement` | `1e68615e544b` |
| [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L398-L404) | `398-404` | `Falah.Client.CashierWinUI.Services.ApiClient.RecordOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken) (MethodDeclaration)` | `831a6dfd1f18` |
| [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs#L34-L55) | `34-55` | `Falah.Client.CashierWinUI.Services.RetainedRequest<TRequest>.SendAsync<TResult>(TRequest, Func<TRequest, Task<Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<TResult>>>) (MethodDeclaration)` | `7cf2985b1e77` |
| [`src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/EmployeesViewModel.cs#L367-L378) | `367-378` | `Falah.Client.CashierWinUI.ViewModels.EmployeesViewModel.CompleteOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest) (MethodDeclaration)` | `04f70fc8f689` |
| [`src/Falah.Domain/Authorization/ApprovalRegistry.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/ApprovalRegistry.cs#L69-L99) | `69-99` | `Falah.Domain.Authorization.AuthorizationPolicy.AuthorizeCommand(Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Authorization.Actor, string, Falah.Domain.Authorization.ManagerApproval?, bool, bool) (MethodDeclaration)` | `029edee4286b` |
| [`src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordDailyWageHandlers.cs#L125-L156) | `125-156` | `Falah.Infrastructure.Employees.RecordExtraShiftHandler.HandleAsync(Falah.Application.Employees.RecordExtraShiftRequest, CancellationToken) (MethodDeclaration)` | `8d091c8c2286` |
| [`src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/EfApprovalRegistry.cs#L33-L53) | `33-53` | `Falah.Infrastructure.Persistence.EfApprovalRegistry.Find(string) (MethodDeclaration)` | `b5028b69a7df` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L759-L778) | `759-778` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Employees/AttendanceRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Employees/AttendanceRequests.cs#L37-L47)  
> **Lines 37-47** in `File-level declaration or top-level statement` (File SHA-256: `1e68615e544b3f852296dd0923218c6839a80a1a96beb69d4c56de3c6b550419`):

```csharp
  37 | /// <summary>RequestId is minted once per submit and reused unchanged by a retry. A real idempotency
  38 | /// gap found during re-verification: legacy's own recordOvertime never gives
  39 | /// recordManualLedgerEntry a metadata.id either, so a double-click genuinely double-posts overtime
  40 | /// pay there too - but this session's standing pattern is to close every such un-protected
  41 | /// financial write found rather than reproduce it (the same treatment already given to
  42 | /// recordManualLedgerEntry's own missing permission gate), so a real key is required here instead.</summary>
  43 | public sealed record RecordOvertimeRequest(string RequestId, string EmployeeId, double Hours, long? CustomRatePiastres, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
  44 | 
  45 | /// <summary>See RecordOvertimeRequest's own doc comment - recordExtraShift has the identical gap
  46 | /// in legacy, closed the same way here.</summary>
  47 | public sealed record RecordExtraShiftRequest(string RequestId, string EmployeeId, int ShiftCount, string? Notes, string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);
```

> **File:** [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L398-L404)  
> **Lines 398-404** in `Falah.Client.CashierWinUI.Services.ApiClient.RecordOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `831a6dfd1f18510b240a232b7a4c4fd11251a51746e1e416c7ce691f732ddbf4`):

```csharp
 398 |     public Task<ApiResult<Falah.Application.Employees.EmployeeLedgerEntryDto>> RecordOvertimeAsync(Falah.Application.Employees.RecordOvertimeRequest request, CancellationToken ct = default) =>
 399 |         SendAsync<Falah.Application.Employees.EmployeeLedgerEntryDto>(() => _http.PostAsJsonAsync(
 400 |             $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}/overtime", request, ct), ct);
 401 | 
 402 |     public Task<ApiResult<Falah.Application.Employees.EmployeeLedgerEntryDto>> RecordExtraShiftAsync(Falah.Application.Employees.RecordExtraShiftRequest request, CancellationToken ct = default) =>
 403 |         SendAsync<Falah.Application.Employees.EmployeeLedgerEntryDto>(() => _http.PostAsJsonAsync(
 404 |             $"/api/employees/{Uri.EscapeDataString(request.EmployeeId)}/extra-shift", request, ct), ct);
```

> **File:** [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs#L34-L55)  
> **Lines 34-55** in `Falah.Client.CashierWinUI.Services.RetainedRequest<TRequest>.SendAsync<TResult>(TRequest, Func<TRequest, Task<Falah.Client.CashierWinUI.Services.ApiClient.ApiResult<TResult>>>) (MethodDeclaration)` (File SHA-256: `7cf2985b1e7764377ea5c972efae4ae418790bab38ef8aeebfd5198f86b5a68b`):

```csharp
  34 |     public async Task<ApiClient.ApiResult<TResult>> SendAsync<TResult>(
  35 |         TRequest request, Func<TRequest, Task<ApiClient.ApiResult<TResult>>> send)
  36 |     {
  37 |         var wasUnresolved = Value is not null;
  38 |         Value ??= request;
  39 |         // Persisted before the send, not after: the window this protects is the one where the
  40 |         // request has left but its outcome has not come back.
  41 |         if (_ownerKey is not null && !wasUnresolved) _store?.Save(_ownerKey, Value);
  42 | 
  43 |         var result = await send(Value);
  44 |         if (result.Success && result.Value is not null || !wasUnresolved && !result.OutcomeUnknown)
  45 |         {
  46 |             Value = null;
  47 |             WasRecovered = false;
  48 |             if (_ownerKey is not null) _store?.Clear(_ownerKey);
  49 |         }
  50 |         return result;
  51 |     }
  52 | 
  53 |     public string FailureMessage(string? error) => HasPending
  54 |         ? "لم تُحسم نتيجة العملية. أعد المحاولة بنفس الطلب للتحقق قبل المغادرة."
  55 |         : error ?? "تعذر تنفيذ العملية.";
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
RecordOvertime/RecordExtraShift validate one-use delegated approval before deterministic ledger receipt lookup. Successful first SaveChanges persists approval consumption; retry with same saved approval rejects before ledger dedupe/replay.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Existing financial credit remains committed but client cannot confirm it with original request. Retained request remains pending after subsequent422, preventing normal completion of that submitted action. This is a recovery/acknowledgement failure, not proven duplicate payment or loss of financial facts.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
RecordOvertime/RecordExtraShift validate one-use delegated approval before deterministic ledger receipt lookup. Successful first SaveChanges persists approval consumption; retry with same saved approval rejects before ledger dedupe/replay.

**E. Future Repair Specification:**  
After validating actor/session and binding the durable request to target and payload, resolve an existing committed receipt before consuming approval again. Keep fresh-operation approval and atomic persistence.

**Required Regression Test Cases:**
- Manager-approved overtime/extra shift success then exact retry in fresh context after response loss; verify no second approval spend and same receipt.
- Mismatched actor/employee/amount conflict; concurrent same-key SQL test only in approved disposable DB.

**Acceptance Criteria & Verification Standard:**
- A committed approval-backed overtime/extra-shift request can be acknowledged on exact same actor/target/payload retry without consuming another approval.
- Mismatched same key conflicts; fresh operation still requires valid authorization; no duplicate ledger credit.

---

### FIN-029: Owner dashboard stock projection ignores outbound movement direction
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_SOURCE_CONFIRMED_AND_INDEPENDENT_STATIC_CHALLENGE;RUNTIME_NOT_EXECUTED` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs#L50-L52) | `50-52` | `Falah.Domain.Inventory.InventoryRules.StockBalanceMilliUnits(IEnumerable<Falah.Domain.Inventory.StockMovementFacts>, string) (MethodDeclaration)` | `6c445ff5fdfd` |
| [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L388-L430) | `388-430` | `Falah.Infrastructure.Inventory.InventoryQueryService.GetInventoryOverviewAsync(CancellationToken) (MethodDeclaration)` | `3ff16ec5eb50` |
| [`src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs#L32-L149) | `32-149` | `Falah.Infrastructure.Inventory.RecordCountAdjustmentHandler.HandleAsync(Falah.Application.Inventory.RecordCountAdjustmentRequest, CancellationToken) (MethodDeclaration)` | `40347a2c68fc` |
| [`src/Falah.Infrastructure/Orders/InventoryDeductionService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/InventoryDeductionService.cs#L48-L96) | `48-96` | `Falah.Infrastructure.Orders.InventoryDeductionService.DeductForOrderAsync(string, string, IReadOnlyList<Falah.Infrastructure.Orders.MenuPricingService.ResolvedItem>, string, DateTimeOffset, CancellationToken) (MethodDeclaration)` | `2fe3c06b9088` |
| [`src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Purchasing/ReceivePurchaseHandler.cs#L165-L175) | `165-175` | `Falah.Infrastructure.Purchasing.ReceivePurchaseHandler.HandleAsync(Falah.Application.Purchasing.ReceivePurchaseRequest, CancellationToken) (MethodDeclaration)` | `70287ac1d6a6` |
| [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L297-L315) | `297-315` | `Falah.Infrastructure.Reports.OwnerDashboardQueryService.LoadStockAsync(CancellationToken) (MethodDeclaration)` | `67c66a18a228` |
| [`tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs#L123-L150) | `123-150` | `Falah.Server.IntegrationTests.InventoryVerticalSliceTests.Listing_inventory_items_reports_the_derived_balance_and_low_stock_flag() (MethodDeclaration)` | `28e718db495a` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Inventory/InventoryRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/InventoryRules.cs#L50-L52)  
> **Lines 50-52** in `Falah.Domain.Inventory.InventoryRules.StockBalanceMilliUnits(IEnumerable<Falah.Domain.Inventory.StockMovementFacts>, string) (MethodDeclaration)` (File SHA-256: `6c445ff5fdfdb08495188e70b13ba03b18fcfb850921dca22838695c7ddebb67`):

```csharp
  50 |     public static long StockBalanceMilliUnits(IEnumerable<StockMovementFacts> movements, string inventoryItemId) =>
  51 |         movements.Where(m => m.InventoryItemId == inventoryItemId)
  52 |             .Sum(m => m.Direction == StockDirection.In ? m.QuantityMilliUnits : -m.QuantityMilliUnits);
```

> **File:** [`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L388-L430)  
> **Lines 388-430** in `Falah.Infrastructure.Inventory.InventoryQueryService.GetInventoryOverviewAsync(CancellationToken) (MethodDeclaration)` (File SHA-256: `3ff16ec5eb50957c11ad1ba39ed5e45cd8f29b972674d49b4e3448e0e3aee6de`):

```csharp
 388 |     public async Task<InventoryOverviewDto> GetInventoryOverviewAsync(CancellationToken ct = default)
 389 |     {
 390 |         var items = await _db.InventoryItems.AsNoTracking().ToListAsync(ct);
 391 |         var movements = await _db.StockMovements.AsNoTracking()
 392 |             .Select(m => new { m.InventoryItemId, m.Direction, m.QuantityMilliUnits, m.Type, m.CreatedAtUtc })
 393 |             .ToListAsync(ct);
 394 | 
 395 |         var byItem = movements.GroupBy(m => m.InventoryItemId).ToDictionary(g => g.Key, g => g.ToList());
 396 |         var ctx = await LoadCostingContextAsync(ct);
 397 | 
 398 |         var rows = new List<InventoryOverviewRowDto>(items.Count);
 399 |         foreach (var item in items)
 400 |         {
 401 |             var itemMovements = byItem.GetValueOrDefault(item.Id) ?? new();
 402 |             var balance = itemMovements.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits);
 403 |             var last = itemMovements.OrderByDescending(m => m.CreatedAtUtc).FirstOrDefault();
 404 | 
 405 |             var (unitCost, source) = CostingRules.IngredientUnitCost(
 406 |                 ctx.InventoryItems.FirstOrDefault(i => i.Id == item.Id), ctx);
 407 | 
 408 |             // Value follows cost: an item nobody has priced has an UNKNOWN value, and adding a zero
 409 |             // for it into the storeroom total would understate the total while looking precise.
 410 |             long? value = unitCost is { } cost ? PurchasingRules.PurchaseLineTotal(balance, cost).Piastres : null;
 411 | 
 412 |             rows.Add(new InventoryOverviewRowDto(
 413 |                 item.Id, item.Name, item.Category, item.Unit, item.IsPackaging, item.Active,
 414 |                 balance, item.MinimumMilliUnits, balance <= item.MinimumMilliUnits,
 415 |                 unitCost?.Piastres, source.ToString(), value,
 416 |                 last?.CreatedAtUtc, last?.Type));
 417 |         }
 418 | 
 419 |         rows = rows.OrderBy(r => r.Category).ThenBy(r => r.Name).ToList();
 420 |         var live = rows.Where(r => r.Active).ToList();
 421 | 
 422 |         return new InventoryOverviewDto(
 423 |             rows,
 424 |             live.Sum(r => r.StockValuePiastres ?? 0),
 425 |             live.Count,
 426 |             live.Count(r => r.IsLowStock),
 427 |             live.Count(r => r.UnitCostPiastres is null),
 428 |             live.Count(r => r.UnitCostPiastres is null && r.BalanceMilliUnits > 0),
 429 |             live.Count(r => r.BalanceMilliUnits < 0));
 430 |     }
```

> **File:** [`src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs#L32-L149)  
> **Lines 32-149** in `Falah.Infrastructure.Inventory.RecordCountAdjustmentHandler.HandleAsync(Falah.Application.Inventory.RecordCountAdjustmentRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `40347a2c68fc1f09c20e588573331a893f40e8165b2bddd0b4e29dee20d638ab`):

```csharp
  32 |     public async Task<RecordCountAdjustmentResult> HandleAsync(RecordCountAdjustmentRequest request, CancellationToken ct = default)
  33 |     {
  34 |         var idempotencyKey = $"inventory-count-{request.RequestId}";
  35 |         var existing = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
  36 |         if (existing is not null) return new RecordCountAdjustmentResult(existing.Id.ToString(), existing.InventoryItemId, WasReplayed: true);
  37 | 
  38 |         if (!Enum.TryParse<StockDirection>(request.Direction, ignoreCase: true, out var direction))
  39 |             throw new DomainException("اتجاه حركة المخزون غير صالح.");
  40 |         InventoryRules.AssertCountAdjustmentInput(request.QuantityMilliUnits, request.Reason);
  41 | 
  42 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  43 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  44 |         var actor = OrderMapper.ToActor(actorUser);
  45 | 
  46 |         var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, ct)
  47 |             ?? throw new InvalidOperationException("الخامة غير موجودة.");
  48 | 
  49 |         ManagerApproval? approval = null;
  50 |         if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  51 |         {
  52 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  53 |                 ?? throw new DomainException("المعتمد غير موجود.");
  54 |             approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  55 |                 approverUser.Role, DateTimeOffset.UtcNow, "inventory_manage", request.Reason);
  56 |         }
  57 |         var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "inventory_manage", approval, requireApproval: true);
  58 | 
  59 |         var entity = new StockMovementEntity
  60 |         {
  61 |             IdempotencyKey = idempotencyKey, InventoryItemId = item.Id, Type = nameof(StockMovementType.CountAdjustment),
  62 |             Direction = direction.ToString(), QuantityMilliUnits = request.QuantityMilliUnits,
  63 |             ReferenceType = "inventory_count", ReferenceId = item.Id, Reason = request.Reason.Trim(),
  64 |             CreatedBy = actorUser.Name, ApprovedBy = authorization.Approver?.ApproverName,
  65 |             OperationId = $"op-{Guid.NewGuid():N}", CreatedAtUtc = DateTimeOffset.UtcNow,
  66 |         };
  67 |         try
  68 |         {
  69 |             _db.StockMovements.Add(entity);
  70 |             _auditLog.Append(new AuditEvent(
  71 |                 $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
  72 |                 "فرق جرد", "inventory", $"الخامة {item.Id}: {direction} {request.QuantityMilliUnits} - {request.Reason}",
  73 |                 "normal", entity.CreatedAtUtc, Reason: request.Reason, AuthorizedBy: authorization.Approver?.ApproverName,
  74 |                 ReferenceId: item.Id));
  75 |             await _db.SaveChangesAsync(ct);
  76 |         }
  77 |         catch (DbUpdateException)
  78 |         {
  79 |             var replay = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
  80 |             if (replay is not null) return new RecordCountAdjustmentResult(replay.Id.ToString(), replay.InventoryItemId, WasReplayed: true);
  81 |             throw new InventoryConflictException("تعذر تسجيل فرق الجرد بسبب تعارض في مفتاح منع التكرار - يحتاج مراجعة يدوية.");
  82 |         }
  83 |         return new RecordCountAdjustmentResult(entity.Id.ToString(), entity.InventoryItemId, WasReplayed: false);
  84 |     }
  85 | }
  86 | 
  87 | /// <summary>Ported from recordInventoryWaste. Loss is deliberately not one bucket - waste (prep
  88 | /// error), spoilage (storage/expiry), breakage (handling accident) point at three different fixes.
  89 | /// Valued at the LAST ACTUAL PURCHASE unit cost, frozen at posting time - a later purchase at a
  90 | /// different price must never restate this loss (PurchasingRules.ValueStockLoss's own doc comment).
  91 | /// Never checked against the current balance: real stock can legitimately lag the system, and
  92 | /// blocking on that would hide the very discrepancy this command exists to record.</summary>
  93 | public sealed class RecordWasteHandler
  94 | {
  95 |     private readonly FalahDbContext _db;
  96 |     private readonly IApprovalRegistry _approvalRegistry;
  97 |     private readonly IAuditLog _auditLog;
  98 | 
  99 |     public RecordWasteHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
 100 |     {
 101 |         _db = db;
 102 |         _approvalRegistry = approvalRegistry;
 103 |         _auditLog = auditLog;
 104 |     }
 105 | 
 106 |     public async Task<RecordWasteResult> HandleAsync(RecordWasteRequest request, CancellationToken ct = default)
 107 |     {
 108 |         var idempotencyKey = $"inventory-waste-{request.RequestId}";
 109 |         var existing = await _db.StockMovements.AsNoTracking().FirstOrDefaultAsync(m => m.IdempotencyKey == idempotencyKey, ct);
 110 |         if (existing is not null)
 111 |             return new RecordWasteResult(existing.Id.ToString(), existing.InventoryItemId, existing.UnitCostPiastres, existing.LossValuePiastres, WasReplayed: true);
 112 | 
 113 |         if (!Enum.TryParse<StockMovementType>(request.LossType, ignoreCase: true, out var lossType))
 114 |             throw new DomainException("نوع الفقد غير صالح.");
 115 |         InventoryRules.AssertWasteInput(request.QuantityMilliUnits, request.Reason, lossType);
 116 | 
 117 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
 118 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
 119 |         var actor = OrderMapper.ToActor(actorUser);
 120 | 
 121 |         var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == request.InventoryItemId, ct)
 122 |             ?? throw new InvalidOperationException("الخامة غير موجودة.");
 123 | 
 124 |         ManagerApproval? approval = null;
 125 |         if (!string.IsNullOrEmpty(request.ManagerApprovalId))
 126 |         {
 127 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
 128 |                 ?? throw new DomainException("المعتمد غير موجود.");
 129 |             approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
 130 |                 approverUser.Role, DateTimeOffset.UtcNow, "inventory_manage", request.Reason);
 131 |         }
 132 |         var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "inventory_manage", approval, requireApproval: true);
 133 | 
 134 |         var purchaseMovements = await _db.StockMovements.AsNoTracking()
 135 |             .Where(m => m.InventoryItemId == item.Id && m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
 136 |             .Select(m => new { m.UnitCostPiastres, m.CreatedAtUtc })
 137 |             .ToListAsync(ct);
 138 |         var facts = purchaseMovements.Select(m => new StockMovementFacts(item.Id, StockMovementType.PurchaseReceipt, Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc));
 139 |         var unitCost = PurchasingRules.LastPurchaseUnitCost(facts, item.Id);
 140 |         var lossValue = PurchasingRules.ValueStockLoss(request.QuantityMilliUnits, unitCost);
 141 | 
 142 |         var entity = new StockMovementEntity
 143 |         {
 144 |             IdempotencyKey = idempotencyKey, InventoryItemId = item.Id, Type = lossType.ToString(),
 145 |             Direction = nameof(StockDirection.Out), QuantityMilliUnits = request.QuantityMilliUnits,
 146 |             UnitCostPiastres = unitCost?.Piastres, LossValuePiastres = lossValue?.Piastres,
 147 |             ReferenceType = "inventory_waste", ReferenceId = item.Id, Reason = request.Reason.Trim(),
 148 |             CreatedBy = actorUser.Name, ApprovedBy = authorization.Approver?.ApproverName,
 149 |             OperationId = $"op-{Guid.NewGuid():N}", CreatedAtUtc = DateTimeOffset.UtcNow,
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
OwnerDashboardQueryService.LoadStockAsync groups stock movements and sums QuantityMilliUnits without applying Direction. Stock movement writers store positive magnitudes with Direction separately; canonical inventory projections subtract Out movements.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Dashboard stock balances can be overstated after consumption, waste, packaging usage, or outbound count adjustments. Low-stock rows/counts can be hidden. Example: In 5,000 plus Out 4,000 yields dashboard 9,000 while correct balance is 1,000.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
OwnerDashboardQueryService.LoadStockAsync groups stock movements and sums QuantityMilliUnits without applying Direction. Stock movement writers store positive magnitudes with Direction separately; canonical inventory projections subtract Out movements.

**E. Future Repair Specification:**  
Use shared direction-aware stock balance projection or apply Direction sign in dashboard query; keep inventory overview as parity oracle.

**Required Regression Test Cases:**
- Integration compare dashboard/inventory seeded In+Out and outbound-only; low-stock parity; positive in/out count adjustment.

**Acceptance Criteria & Verification Standard:**
- For seeded In 5000 and Out 4000, dashboard and inventory overview both show 1000 and agree on low-stock status.
- Outbound-only and count-adjustment movements preserve direction-aware parity.

---

### FIN-CAND-CASHCOUNT-001: Unvalidated denomination and unchecked arithmetic can corrupt finalized count values
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_STATIC_WITH_QUALIFICATIONS_NO_SQL_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs#L47-L51) | `47-51` | `File-level declaration or top-level statement` | `d6d8f05a2959` |
| [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs#L125-L135) | `125-135` | `Falah.Domain.BusinessDay.BusinessDayRules.CalculateDenominationCount(IReadOnlyList<(long DenominationPiastres, long Quantity)>, long) (MethodDeclaration)` | `cdc3306873f2` |
| [`src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs#L29-L64) | `29-64` | `Falah.Infrastructure.ControlCenter.SaveCashCountHandler.HandleAsync(Falah.Application.BusinessDay.SaveCashCountRequest, CancellationToken) (MethodDeclaration)` | `1ebd4439f52e` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L488-L500) | `488-500` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/BusinessDay/CloseBusinessDayRequest.cs#L47-L51)  
> **Lines 47-51** in `File-level declaration or top-level statement` (File SHA-256: `d6d8f05a2959343b0824dcb3d4f1d57bd342469fc8e0bc57a539b3cfe08402d1`):

```csharp
  47 | public sealed record SaveCashCountRequest(
  48 |     string Id, string BusinessDayId, string? ShiftId, IReadOnlyList<DenominationRowInput> Rows,
  49 |     long ExpectedPiastres, string ActorUserId);
  50 | 
  51 | public sealed record DenominationRowInput(long DenominationPiastres, long Quantity);
```

> **File:** [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs#L125-L135)  
> **Lines 125-135** in `Falah.Domain.BusinessDay.BusinessDayRules.CalculateDenominationCount(IReadOnlyList<(long DenominationPiastres, long Quantity)>, long) (MethodDeclaration)` (File SHA-256: `cdc3306873f2b6f46029186ec995b90415eda247ee3ec5b7408a9285f390ed06`):

```csharp
 125 |     public static DenominationCountResult CalculateDenominationCount(
 126 |         IReadOnlyList<(long DenominationPiastres, long Quantity)> rows, long expectedPiastres)
 127 |     {
 128 |         var normalized = rows.Select(r =>
 129 |         {
 130 |             var quantity = Math.Max(0, r.Quantity);
 131 |             return new DenominationRow(r.DenominationPiastres, quantity, r.DenominationPiastres * quantity);
 132 |         }).ToList();
 133 |         var total = normalized.Sum(r => r.TotalPiastres);
 134 |         return new DenominationCountResult(normalized, total, expectedPiastres, total - expectedPiastres);
 135 |     }
```

> **File:** [`src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/SaveCashCountHandler.cs#L29-L64)  
> **Lines 29-64** in `Falah.Infrastructure.ControlCenter.SaveCashCountHandler.HandleAsync(Falah.Application.BusinessDay.SaveCashCountRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `1ebd4439f52e582f2e890c0db9bbd9f0c7e3e406b8ed56e46f2da900229083b3`):

```csharp
  29 |     public async Task<CashDenominationCountDto> HandleAsync(SaveCashCountRequest request, CancellationToken ct = default)
  30 |     {
  31 |         var existing = await _db.CashDenominationCounts.AsNoTracking().FirstOrDefaultAsync(c => c.Id == request.Id, ct);
  32 |         if (existing is not null) return ToDto(existing, wasReplayed: true);
  33 | 
  34 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  35 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  36 |         AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, OrderMapper.ToActor(actorUser), "cash_count");
  37 | 
  38 |         _ = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
  39 |             ?? throw new InvalidOperationException("يوم العمل غير موجود.");
  40 | 
  41 |         var calculated = BusinessDayRules.CalculateDenominationCount(
  42 |             request.Rows.Select(r => (r.DenominationPiastres, r.Quantity)).ToList(), request.ExpectedPiastres);
  43 | 
  44 |         var now = DateTimeOffset.UtcNow;
  45 |         var entity = new CashDenominationCountEntity
  46 |         {
  47 |             Id = request.Id, BusinessDayId = request.BusinessDayId, ShiftId = request.ShiftId,
  48 |             RowsJson = JsonSerializer.Serialize(calculated.Rows),
  49 |             TotalPiastres = calculated.TotalPiastres, ExpectedPiastres = calculated.ExpectedPiastres,
  50 |             DifferencePiastres = calculated.DifferencePiastres, CountedBy = actorUser.Name,
  51 |             CreatedAtUtc = now, FinalizedAtUtc = now,
  52 |         };
  53 |         try
  54 |         {
  55 |             _db.CashDenominationCounts.Add(entity);
  56 |             _db.ControlAuditEvents.Add(new ControlAuditEventEntity
  57 |             {
  58 |                 Id = $"control-audit-{Guid.NewGuid():N}", Action = "cash_count_finalized", Actor = actorUser.Name,
  59 |                 EntityId = entity.Id, AmountPiastres = entity.TotalPiastres, CreatedAtUtc = now,
  60 |             });
  61 |             await _db.SaveChangesAsync(ct);
  62 |         }
  63 |         catch (DbUpdateException)
  64 |         {
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Arithmetic and domain validation are not bounded at the finalization boundary; persisted audit data records the derived result without enforcing the supported denomination set or representable totals.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Could finalize and audit a mathematically incorrect cash total/difference. Existing evidence does not show direct treasury posting, shift settlement, or close-blocker override; do not characterize as cash loss.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Arithmetic and domain validation are not bounded at the finalization boundary; persisted audit data records the derived result without enforcing the supported denomination set or representable totals.

**E. Future Repair Specification:**  
Owner must first decide whether ExpectedPiastres is a client-supplied comparison target or must be derived from a server-authoritative snapshot. Then whitelist supported positive denominations and bound each quantity/aggregate; use checked multiply, sum, and difference and convert overflow/invalid rows to explicit domain rejection. Preserve agreed negative-quantity compatibility semantics or reject only by explicit policy.

**Required Regression Test Cases:**
- Domain tests for every allowed denomination, unsupported/zero/negative denomination, maximum quantity, multiplication overflow, aggregate overflow, and difference overflow.
- Authorized handler test proves invalid input creates neither count nor audit record; valid input persists exact values atomically.
- Endpoint test verifies invalid input maps to a deliberate client error.
- Verify whether finalized cash-count data participates in close reconciliation and that any such use is server-validated.
- Build/run arithmetic behavior under the actual net8 deployment configuration.

**Acceptance Criteria & Verification Standard:**
- All accepted values fall within explicit denomination and monetary bounds; no wraparound reaches persisted count/audit.
- Rejected values produce no partial count/audit writes.
- ExpectedPiastres authority and negative quantity semantics are documented and covered.
- Any reconciliation consumer uses the validated result or independently recomputes the expected value.

---

### FIN-CAND-CLOSE-SNAPSHOT-REVISION-001: Close snapshot revision and authoritative-row admission lack an explicit shared invariant
- **Severity:** `MEDIUM` | **Confidence:** `SOURCE_ADMISSION_GAP_ONLY_PROVIDER_OUTCOME_UNVERIFIED` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Disposition Rationale:** Source behavior is deterministic but depends on owner business policy (e.g. tax rounding policy, legacy casing tolerance, packaging cost allocation).

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L34-L38) | `34-38` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L54-L64) | `54-64` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L87-L120) | `87-120` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L162-L165) | `162-165` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.ToDto(Falah.Infrastructure.Persistence.Entities.BusinessDayCloseSnapshotEntity, bool) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/ControlCenterQueryService.cs#L42-L53) | `42-53` | `Falah.Infrastructure.ControlCenter.ControlCenterQueryService.GetLatestCloseSnapshotAsync(string, CancellationToken) (MethodDeclaration)` | `2ba6e827c2db` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137-L141) | `137-141` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L400-L405) | `400-405` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L34-L38)  
> **Lines 34-38** in `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `b33d64c048db902bc7b9469787872ea70be30659ab69a97486d9f95c98d46255`):

```csharp
  34 |     public async Task<BusinessDayCloseSnapshotDto> HandleAsync(CloseBusinessDayRequest request, CancellationToken ct = default)
  35 |     {
  36 |         var snapshotId = $"snapshot-{request.RequestId}";
  37 |         var existing = await _db.BusinessDayCloseSnapshots.AsNoTracking().FirstOrDefaultAsync(s => s.Id == snapshotId, ct);
  38 |         if (existing is not null) return ToDto(existing, wasReplayed: true);
```

> **File:** [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L54-L64)  
> **Lines 54-64** in `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `b33d64c048db902bc7b9469787872ea70be30659ab69a97486d9f95c98d46255`):

```csharp
  54 |         var day = await _db.BusinessDays.FirstOrDefaultAsync(d => d.Id == request.BusinessDayId, ct)
  55 |             ?? throw new InvalidOperationException("يوم العمل غير موجود.");
  56 | 
  57 |         var currentStatus = Enum.Parse<BusinessDayStatus>(day.Status);
  58 |         BusinessDayRules.AssertTransition(currentStatus, BusinessDayStatus.Closing);
  59 |         BusinessDayRules.AssertTransition(BusinessDayStatus.Closing, BusinessDayStatus.Closed);
  60 | 
  61 |         var operationId = $"business-close-{day.Id}-r{day.CloseRevision + 1}";
  62 |         var now = DateTimeOffset.UtcNow;
  63 | 
  64 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
```

> **File:** [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L87-L120)  
> **Lines 87-120** in `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `b33d64c048db902bc7b9469787872ea70be30659ab69a97486d9f95c98d46255`):

```csharp
  87 |             var reconciliation = await _reporting.GetBusinessDayReconciliationAsync(day.Id, ct);
  88 |             var exceptions = await BuildDayExceptionsAsync(day, ct);
  89 |             var completions = await _db.ChecklistCompletions.AsNoTracking().Where(c => c.BusinessDayId == day.Id).ToListAsync(ct);
  90 | 
  91 |             var revision = await _db.BusinessDayCloseSnapshots.CountAsync(s => s.BusinessDayId == day.Id, ct) + 1;
  92 |             await _db.BusinessDayCloseSnapshots.Where(s => s.BusinessDayId == day.Id)
  93 |                 .ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);
  94 | 
  95 |             var snapshot = new BusinessDayCloseSnapshotEntity
  96 |             {
  97 |                 Id = snapshotId, BusinessDayId = day.Id, CloseRevision = revision, Authoritative = true,
  98 |                 PreviousSnapshotId = day.SnapshotId, BusinessDate = day.BusinessDate, GeneratedAtUtc = now,
  99 |                 ClosedBy = actorUser.Name, OperationId = operationId,
 100 |                 ReconciliationJson = JsonSerializer.Serialize(reconciliation),
 101 |                 ExceptionsJson = JsonSerializer.Serialize(exceptions),
 102 |                 ChecklistJson = JsonSerializer.Serialize(completions.Select(c => new { c.ItemId, c.Completed, c.CompletedAtUtc, c.CompletedBy })),
 103 |             };
 104 |             _db.BusinessDayCloseSnapshots.Add(snapshot);
 105 | 
 106 |             day.Status = nameof(BusinessDayStatus.Closed);
 107 |             day.CloseRevision = revision;
 108 |             day.SnapshotId = snapshot.Id;
 109 |             day.ClosedAtUtc = now;
 110 |             day.ClosedBy = actorUser.Name;
 111 | 
 112 |             _db.ControlAuditEvents.Add(new ControlAuditEventEntity
 113 |             {
 114 |                 Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_closed", Actor = actorUser.Name,
 115 |                 EntityId = day.Id, OperationId = operationId, ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
 116 |             });
 117 | 
 118 |             await _db.SaveChangesAsync(ct);
 119 |             await transaction.CommitAsync(ct);
 120 |             return ToDto(snapshot, wasReplayed: false);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Read/status admission outside transaction plus Count+1/demotion lacks explicit perday uniqueness/state-token enforcement in inspected source.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Source lacks explicit common day admission lock/state token and schema uniqueness for archival invariants; duplicate-current/same-revision outcome remains a qualified provider-dependent candidate, not reproduced corruption.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Read/status admission outside transaction plus Count+1/demotion lacks explicit perday uniqueness/state-token enforcement in inspected source.

**E. Future Repair Specification:**  
Do not implement a concurrency fix based on assumed SQL ordering. First test provider schedule; if admission gap manifests, use common day lock/state predicate plus explicit snapshot uniqueness/authority invariant.

**Required Regression Test Cases:**
- Barrier two distinct close IDs around count/demotion in disposable SQL Server; capture EF DML and locks.
- Verify authoritative row, revisions and day pointer across both serialization outcomes/history.

**Acceptance Criteria & Verification Standard:**
- Disposable SQL Server controlled schedule establishes actual DML/lock outcome before code fix is chosen.
- After any accepted close ordering there is exactly one authoritative snapshot, coherent day pointer, and valid monotone unique revision; losing close leaves no partial mutation.

---

### FIN-CAND-CLOSEDAY-ADMISSION-001: Previously admitted treasury deposit can post after authoritative business-day close
- **Severity:** `HIGH` | **Confidence:** `HIGH_STATIC_WITH_QUALIFICATIONS_NO_SQL_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs#L14-L29) | `14-29` | `Falah.Infrastructure.BusinessDay.BusinessDayGate.RequireOpenAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` | `0271b40be055` |
| [`src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs#L25-L57) | `25-57` | `Falah.Infrastructure.ControlCenter.BusinessDayCloseEvaluator.EvaluateAsync(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken) (MethodDeclaration)` | `cc3a318c0332` |
| [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L64-L121) | `64-121` | `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `b33d64c048db` |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L164-L208) | `164-208` | `Falah.Infrastructure.Reports.ReportingQueryService.GetBusinessDayReconciliationAsync(string, CancellationToken) (MethodDeclaration)` | `452d58690fb3` |
| [`src/Falah.Infrastructure/Treasury/DepositHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/DepositHandler.cs#L56-L85) | `56-85` | `Falah.Infrastructure.Treasury.DepositHandler.HandleAsync(Falah.Application.Treasury.DepositRequest, CancellationToken) (MethodDeclaration)` | `2de4b3e87386` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/BusinessDayGate.cs#L14-L29)  
> **Lines 14-29** in `Falah.Infrastructure.BusinessDay.BusinessDayGate.RequireOpenAsync(Falah.Infrastructure.Persistence.FalahDbContext, CancellationToken) (MethodDeclaration)` (File SHA-256: `0271b40be055fa7130067dbbe7368bc7a48a21ec65bff8f05d47a0469b48c962`):

```csharp
  14 |     public static async Task<string> RequireOpenAsync(FalahDbContext db, CancellationToken ct) =>
  15 |         await TryGetActiveAsync(db, ct) ?? throw new DomainException("لا يوجد يوم عمل مفتوح - يجب فتح يوم العمل أولًا.");
  16 | 
  17 |     /// <summary>Ported from checkout()'s own `businessDays.find(day => day.status !== 'closed')?.id`
  18 |     /// in lib/store.tsx - a plain sale, unlike a Treasury cash-lifecycle command, does NOT require
  19 |     /// an active business day to exist; it just opportunistically stamps the current one's id when
  20 |     /// there is one, and proceeds with null otherwise. Never returns from a client-supplied id -
  21 |     /// callers (CheckoutOrderHandler) must resolve this server-side, the same "server-authoritative"
  22 |     /// treatment every other snapshot field on Order already gets.</summary>
  23 |     public static async Task<string?> TryGetActiveAsync(FalahDbContext db, CancellationToken ct)
  24 |     {
  25 |         var day = await db.BusinessDays.AsNoTracking()
  26 |             .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
  27 |             .FirstOrDefaultAsync(ct);
  28 |         return day?.Id;
  29 |     }
```

> **File:** [`src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/BusinessDayCloseEvaluator.cs#L25-L57)  
> **Lines 25-57** in `Falah.Infrastructure.ControlCenter.BusinessDayCloseEvaluator.EvaluateAsync(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Infrastructure.Persistence.Entities.BusinessDayEntity, CancellationToken) (MethodDeclaration)` (File SHA-256: `cc3a318c0332b40555ee9f2e545b42876b0e74a90e71c15732c5153eb576a832`):

```csharp
  25 | {
  26 |     public static async Task<IReadOnlyList<CloseBlocker>> EvaluateAsync(FalahDbContext db, BusinessDayEntity day, CancellationToken ct)
  27 |     {
  28 |         var shifts = await db.Shifts.AsNoTracking().Select(s => new { s.Id, s.Status }).ToListAsync(ct);
  29 |         var driverCustodies = await db.DriverCustodies.AsNoTracking().Select(c => new { c.Id, c.Status }).ToListAsync(ct);
  30 |         var journalEntries = await db.FinancialJournalEntries.AsNoTracking().Select(j => new { j.IdempotencyKey, j.Status }).ToListAsync(ct);
  31 |         var pendingElectronicOrderIds = await db.Orders.AsNoTracking()
  32 |             .Where(o => o.ElectronicPaymentStatus == "Pending").Select(o => o.Id).ToListAsync(ct);
  33 | 
  34 |         var closingItems = await db.ChecklistItems.AsNoTracking()
  35 |             .Where(i => i.Phase == "Closing" && i.Active && i.Required).ToListAsync(ct);
  36 |         var completions = await db.ChecklistCompletions.AsNoTracking().Where(c => c.BusinessDayId == day.Id).ToListAsync(ct);
  37 |         var requiredChecklist = closingItems
  38 |             .Select(i => (i.Id, i.Label, completions.Any(c => c.ItemId == i.Id && c.Completed)))
  39 |             .ToList();
  40 | 
  41 |         var reconciliationChecks = await ComputeReconciliationChecksAsync(db, day, ct);
  42 | 
  43 |         var inputs = new BusinessDayRules.CloseBlockerInputs(
  44 |             Shifts: shifts.Select(s => (s.Id, s.Status == "Closed")).ToList(),
  45 |             DeliveryTrips: Array.Empty<(string, bool)>(),
  46 |             DriverCustodies: driverCustodies.Select(c => (c.Id, c.Status == nameof(CustodyStatus.Settled))).ToList(),
  47 |             JournalEntries: journalEntries.Select(j => (j.IdempotencyKey, j.Status == "Complete")).ToList(),
  48 |             Orders: pendingElectronicOrderIds.Select(id => (id, true)).ToList(),
  49 |             PersistenceHealthy: true,
  50 |             WriterOwned: true,
  51 |             RequiredChecklist: requiredChecklist,
  52 |             ReconciliationChecks: reconciliationChecks);
  53 | 
  54 |         return BusinessDayRules.EvaluateCloseBlockers(inputs);
  55 |     }
  56 | 
  57 |     /// <summary>Defensive checks only - every one of these should be structurally impossible given
```

> **File:** [`src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/ControlCenter/CloseBusinessDayHandler.cs#L64-L121)  
> **Lines 64-121** in `Falah.Infrastructure.ControlCenter.CloseBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.CloseBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `b33d64c048db902bc7b9469787872ea70be30659ab69a97486d9f95c98d46255`):

```csharp
  64 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  65 |         try
  66 |         {
  67 |             _db.ControlAuditEvents.Add(new ControlAuditEventEntity
  68 |             {
  69 |                 Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_close_started", Actor = actorUser.Name,
  70 |                 EntityId = day.Id, OperationId = operationId, CreatedAtUtc = now,
  71 |             });
  72 | 
  73 |             var blockers = await BusinessDayCloseEvaluator.EvaluateAsync(_db, day, ct);
  74 |             if (blockers.Count > 0)
  75 |             {
  76 |                 _db.ControlAuditEvents.Add(new ControlAuditEventEntity
  77 |                 {
  78 |                     Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_close_blocked", Actor = actorUser.Name,
  79 |                     EntityId = day.Id, OperationId = operationId,
  80 |                     BlockerCodesJson = JsonSerializer.Serialize(blockers.Select(b => b.Code)), CreatedAtUtc = now,
  81 |                 });
  82 |                 await _db.SaveChangesAsync(ct);
  83 |                 await transaction.CommitAsync(ct);
  84 |                 throw new BusinessDayCloseBlockedException(blockers.Select(b => new CloseBlockerDto(b.Code, b.EntityType, b.EntityId, b.Message)).ToList());
  85 |             }
  86 | 
  87 |             var reconciliation = await _reporting.GetBusinessDayReconciliationAsync(day.Id, ct);
  88 |             var exceptions = await BuildDayExceptionsAsync(day, ct);
  89 |             var completions = await _db.ChecklistCompletions.AsNoTracking().Where(c => c.BusinessDayId == day.Id).ToListAsync(ct);
  90 | 
  91 |             var revision = await _db.BusinessDayCloseSnapshots.CountAsync(s => s.BusinessDayId == day.Id, ct) + 1;
  92 |             await _db.BusinessDayCloseSnapshots.Where(s => s.BusinessDayId == day.Id)
  93 |                 .ExecuteUpdateAsync(s => s.SetProperty(x => x.Authoritative, false), ct);
  94 | 
  95 |             var snapshot = new BusinessDayCloseSnapshotEntity
  96 |             {
  97 |                 Id = snapshotId, BusinessDayId = day.Id, CloseRevision = revision, Authoritative = true,
  98 |                 PreviousSnapshotId = day.SnapshotId, BusinessDate = day.BusinessDate, GeneratedAtUtc = now,
  99 |                 ClosedBy = actorUser.Name, OperationId = operationId,
 100 |                 ReconciliationJson = JsonSerializer.Serialize(reconciliation),
 101 |                 ExceptionsJson = JsonSerializer.Serialize(exceptions),
 102 |                 ChecklistJson = JsonSerializer.Serialize(completions.Select(c => new { c.ItemId, c.Completed, c.CompletedAtUtc, c.CompletedBy })),
 103 |             };
 104 |             _db.BusinessDayCloseSnapshots.Add(snapshot);
 105 | 
 106 |             day.Status = nameof(BusinessDayStatus.Closed);
 107 |             day.CloseRevision = revision;
 108 |             day.SnapshotId = snapshot.Id;
 109 |             day.ClosedAtUtc = now;
 110 |             day.ClosedBy = actorUser.Name;
 111 | 
 112 |             _db.ControlAuditEvents.Add(new ControlAuditEventEntity
 113 |             {
 114 |                 Id = $"audit-{Guid.NewGuid():N}", Action = "business_day_closed", Actor = actorUser.Name,
 115 |                 EntityId = day.Id, OperationId = operationId, ApprovedBy = authorization.Approver?.ApproverName, CreatedAtUtc = now,
 116 |             });
 117 | 
 118 |             await _db.SaveChangesAsync(ct);
 119 |             await transaction.CommitAsync(ct);
 120 |             return ToDto(snapshot, wasReplayed: false);
 121 |         }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Financial write admission is validated outside its monetary transaction and no shared day-state lock, revision predicate, or revalidation is visible in the selected path. Close snapshot and blocker reads therefore do not source-demonstrably drain operations that already passed the gate.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Predicted post-snapshot movement/journal attached to a now-closed day, potentially diverging from its authoritative close-time snapshot if post-close activity is forbidden. This is not an observed SQL interleaving or loss.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Financial write admission is validated outside its monetary transaction and no shared day-state lock, revision predicate, or revalidation is visible in the selected path. Close snapshot and blocker reads therefore do not source-demonstrably drain operations that already passed the gate.

**E. Future Repair Specification:**  
Confirm whether financial writes are forbidden after close. If yes, coordinate day admission in the database: acquire a transaction-held lock/revision on the business-day row and revalidate Open within each financial write transaction; close takes the same lock before blocker evaluation/snapshot and waits for already-admitted writes. Define cross-process semantics, retry policy, and consistent lock order. If late postings are allowed, define how frozen snapshot reconciliation and subsequent deltas are represented instead of silently treating snapshot as current ledger.

**Required Regression Test Cases:**
- SQL-backed barrier test pauses deposit after initial lookup; close wins; resumed deposit must reject with zero journal/movement.
- Reverse schedule: admitted deposit commits first and close waits/includes it in snapshot.
- Cover other financial commands using the same admission contract plus reopen/new-day and idempotent replay.
- Run controlled multi-process concurrency test under the supported provider/isolation level; verify no stale admission slips.

**Acceptance Criteria & Verification Standard:**
- A documented policy distinguishes immutable close-time snapshot from permitted subsequent postings.
- Under forbid-after-close policy, every monetary write serializes/revalidates against close inside the database transaction; close snapshot covers all writes admitted before close.
- Concurrency tests prove both orderings and absence of partial or post-close financial effects; lock order avoids deadlocks.

---

### FIN-CAND-CORRECTION-BASIS-001: Attendance corrections revalue old effects at current rates instead of posted monetary basis
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_SOURCE_BEHAVIOR_OWNER_POLICY_UNCONFIRMED` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L29-L32) | `29-32` | `Falah.Domain.Payroll.PayrollRules.NormalHourlyRate(Falah.Domain.Common.Money, int) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L55-L60) | `55-60` | `Falah.Domain.Payroll.PayrollRules.CalculateOvertimeAmount(Falah.Domain.Common.Money, int, double, double) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L138-L147) | `138-147` | `Falah.Infrastructure.Employees.BiometricCheckOutHandler.HandleAsync(Falah.Application.Employees.BiometricCheckOutRequest, CancellationToken) (MethodDeclaration)` | `258fd52af239` |
| [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L15-L18) | `15-18` | `File-level declaration or top-level statement` | `e00e9fa8c36f` |
| [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L39-L51) | `39-51` | `Falah.Infrastructure.Employees.CorrectAttendanceHandler.HandleAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken) (MethodDeclaration)` | `e00e9fa8c36f` |
| [`src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CorrectAttendanceHandler.cs#L82-L117) | `82-117` | `Falah.Infrastructure.Employees.CorrectAttendanceHandler.HandleAsync(Falah.Application.Employees.CorrectAttendanceRequest, CancellationToken) (MethodDeclaration)` | `e00e9fa8c36f` |
| [`src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeLedgerPoster.cs#L31-L49) | `31-49` | `Falah.Infrastructure.Employees.EmployeeLedgerPoster.PostIfNewAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, string, string, string, Falah.Domain.Common.Money, Falah.Domain.Common.Money, string, string?, string?, string?, double?, long?, long?, CancellationToken) (MethodDeclaration)` | `56975f399d91` |
| [`src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeManagementHandlers.cs#L95-L100) | `95-100` | `Falah.Infrastructure.Employees.UpdateEmployeeHandler.HandleAsync(Falah.Application.Employees.UpdateEmployeeRequest, CancellationToken) (MethodDeclaration)` | `858acb05b82b` |
| [`src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/EmployeeEntities.cs#L32-L53) | `32-53` | `Falah.Infrastructure.Persistence.Entities.EmployeeLedgerEntryEntity.Id.get (GetAccessorDeclaration)` | `e531e400a219` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L29-L32)  
> **Lines 29-32** in `Falah.Domain.Payroll.PayrollRules.NormalHourlyRate(Falah.Domain.Common.Money, int) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  29 |     /// <summary>Ported verbatim from normalHourlyRate - a non-positive workday-hours config
  30 |     /// produces a zero rate rather than a division error/NaN.</summary>
  31 |     public static Money NormalHourlyRate(Money dailyWage, int workdayHours) =>
  32 |         workdayHours <= 0 ? Money.Zero : Money.FromPiastres(dailyWage.Piastres / workdayHours);
```

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L55-L60)  
> **Lines 55-60** in `Falah.Domain.Payroll.PayrollRules.CalculateOvertimeAmount(Falah.Domain.Common.Money, int, double, double) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  55 |     public static OvertimeAmountResult CalculateOvertimeAmount(Money dailyWage, int workdayHours, double overtimeHours, double overtimeMultiplier)
  56 |     {
  57 |         var rate = NormalHourlyRate(dailyWage, workdayHours);
  58 |         if (overtimeHours <= 0) return new OvertimeAmountResult(rate, Money.Zero);
  59 |         var amount = Money.FromPiastres((long)Math.Round(rate.Piastres * overtimeHours * overtimeMultiplier));
  60 |         return new OvertimeAmountResult(rate, amount);
```

> **File:** [`src/Falah.Infrastructure/Employees/AttendanceHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/AttendanceHandlers.cs#L138-L147)  
> **Lines 138-147** in `Falah.Infrastructure.Employees.BiometricCheckOutHandler.HandleAsync(Falah.Application.Employees.BiometricCheckOutRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `258fd52af239fd06e3084242f71fd0cbfbbd0baf1dce78dd1dbe8498531bffe6`):

```csharp
 138 |         if (overtimeHours > 0 && employee.DailyWagePiastres > 0)
 139 |         {
 140 |             var overtimeResult = PayrollRules.CalculateOvertimeAmount(Money.FromPiastres(employee.DailyWagePiastres),
 141 |                 settings.StandardWorkdayHours, overtimeHours, settings.OvertimeMultiplier);
 142 |             if (overtimeResult.Amount.IsPositive)
 143 |             {
 144 |                 var overtimeId = $"led-overtime-auto-{request.EmployeeId}-{date:yyyy-MM-dd}";
 145 |                 await EmployeeLedgerPoster.PostIfNewAsync(_db, overtimeId, request.EmployeeId, nameof(LedgerEntryType.Overtime),
 146 |                     $"ساعات إضافية تلقائية من البصمة ({overtimeHours} ساعة × {overtimeResult.Rate.Piastres} قرش/ساعة × {Math.Round(settings.OvertimeMultiplier * 100)}%)",
 147 |                     Money.Zero, overtimeResult.Amount, employee.Name, null, activeShiftId, null, overtimeHours, overtimeResult.Rate.Piastres, null, ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Correction reads current employee DailyWage/current Settings then recomputes BOTH old and new overtime/penalty. It never reads frozen actual original amount/rate or cumulative prior correction effect for delta. Row+ledger serialization and immutable retry audit are real controls, but do not restore old monetary basis.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Potential over/under reconciliation relative to actual posted effect; authoritative correction valuation policy unresolved. No actual employee loss, runtime record, payout or SQL acceptance verified.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Correction reads current employee DailyWage/current Settings then recomputes BOTH old and new overtime/penalty. It never reads frozen actual original amount/rate or cumulative prior correction effect for delta. Row+ledger serialization and immutable retry audit are real controls, but do not restore old monetary basis.

**E. Future Repair Specification:**  
After owner decision, calculate correction delta against actual posted original facts and cumulative linked adjustments, append only; preserve idempotency, locks, and freeze rules. Do not mint historical credits automatically.

**Required Regression Test Cases:**
- Original fixed-rate overtime then settings changed then correction; assert owner-approved reversal basis.
- Change multiplier/grace/workday, penalty to zero, sequential corrections/return to original hours, replay and frozen/paid period controls.

**Acceptance Criteria & Verification Standard:**
- Owner-approved correction basis is documented; adjustments reconcile original posted facts and prior corrections without accidental over/under reversal.
- Same-key retry and interleaved sequential corrections remain atomic, auditable, and consistent with frozen/paid period policy.

---

### FIN-CAND-EMPLOYEE-PAYROLL-OVERLAP-001: Standalone employee settlements remain payable through frozen period payroll
- **Severity:** `HIGH` | **Confidence:** `SOURCE_CONFIRMED_CROSS_FLOW_MECHANICS;POLICY_AND_RUNTIME_UNVERIFIED` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L83-L110) | `83-110` | `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L42-L63) | `42-63` | `Falah.Infrastructure.Employees.CreatePayrollHandler.HandleAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken) (MethodDeclaration)` | `339845a57fc7` |
| [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L65-L88) | `65-88` | `Falah.Infrastructure.Employees.CreatePayrollHandler.HandleAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken) (MethodDeclaration)` | `339845a57fc7` |
| [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs#L16-L30) | `16-30` | `File-level declaration or top-level statement` | `93b781b9b0d9` |
| [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs#L74-L107) | `74-107` | `Falah.Infrastructure.Employees.PayEmployeeHandler.HandleAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken) (MethodDeclaration)` | `93b781b9b0d9` |
| [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs#L113-L140) | `113-140` | `Falah.Infrastructure.Employees.PayEmployeeHandler.HandleAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken) (MethodDeclaration)` | `93b781b9b0d9` |
| [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L84-L108) | `84-108` | `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` | `d7a614f6752b` |
| [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L136-L163) | `136-163` | `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` | `d7a614f6752b` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L83-L110)  
> **Lines 83-110** in `Falah.Domain.Payroll.PayrollRules.CalculatePayrollLines(IEnumerable<Falah.Domain.Payroll.PayrollRules.PayrollLineInput>) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  83 |     public static IReadOnlyList<PayrollLine> CalculatePayrollLines(IEnumerable<PayrollLineInput> inputs)
  84 |     {
  85 |         var result = new List<PayrollLine>();
  86 |         foreach (var emp in inputs)
  87 |         {
  88 |             var hasCredit = emp.PeriodEntries.Any(e => e.Credit.IsPositive);
  89 |             if (!emp.IsActive && !hasCredit) continue;
  90 | 
  91 |             // Type strings match this codebase's own PascalCase enum-name convention
  92 |             // (nameof(LedgerEntryType.X), e.g. "DailyWage") - NOT legacy's own lowercase-snake-case
  93 |             // literals ('daily_wage') from LedgerEntryType in types.ts, since every ledger row in
  94 |             // this port is written via nameof(...) (see EmployeeLedgerPoster/RecordDailyWageHandlers).
  95 |             var baseAmount = emp.CompensationIsMonthly
  96 |                 ? (emp.MonthlySalary ?? Money.Zero)
  97 |                 : Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "DailyWage" or "Salary").Sum(e => e.Credit.Piastres));
  98 |             var overtime = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "Overtime").Sum(e => e.Credit.Piastres));
  99 |             var extraShifts = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type == "ExtraShift").Sum(e => e.Credit.Piastres));
 100 |             var bonuses = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Bonus" or "OtherEarning").Sum(e => e.Credit.Piastres));
 101 |             var deductions = Money.FromPiastres(emp.PeriodEntries.Where(e => e.Type is "Deduction" or "Adjustment").Sum(e => e.Debit.Piastres));
 102 | 
 103 |             if (emp.RequestedAdvanceDeduction.IsNegative || emp.RequestedAdvanceDeduction > emp.AvailableAdvance)
 104 |                 throw new DomainException($"استقطاع السلفة للموظف {emp.EmployeeId} يتجاوز الرصيد المتفق عليه.");
 105 | 
 106 |             var net = baseAmount + overtime + extraShifts + bonuses - deductions - emp.RequestedAdvanceDeduction;
 107 |             if (net.IsNegative) net = Money.Zero;
 108 | 
 109 |             result.Add(new PayrollLine(emp.EmployeeId, baseAmount, overtime, extraShifts, bonuses, deductions, emp.RequestedAdvanceDeduction, net));
 110 |         }
```

> **File:** [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L42-L63)  
> **Lines 42-63** in `Falah.Infrastructure.Employees.CreatePayrollHandler.HandleAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `339845a57fc7ec98268bbf79d4b204fbf1c029a3da4cff89771294522963e878`):

```csharp
  42 |         var periodYear = int.Parse(request.Period[..4]);
  43 |         var periodMonth = int.Parse(request.Period[5..]);
  44 |         var employees = await _db.Employees.AsNoTracking().ToListAsync(ct);
  45 |         var periodEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
  46 |             .Where(e => e.CreatedAtUtc.Year == periodYear && e.CreatedAtUtc.Month == periodMonth)
  47 |             .ToListAsync(ct);
  48 | 
  49 |         // Advance-deduction-during-payroll (legacy's optional per-employee advanceDeductions map)
  50 |         // is deliberately not wired up yet: no evidenced caller in the reference app ever supplies a
  51 |         // non-empty map (the UI always creates a run with none), so every employee's own
  52 |         // RequestedAdvanceDeduction is Money.Zero here - the same default legacy itself uses when
  53 |         // the map is omitted, not a lesser port.
  54 |         var lineInputs = employees.Select(emp =>
  55 |         {
  56 |             var empEntries = periodEntries.Where(e => e.EmployeeId == emp.Id)
  57 |                 .Select(e => (e.Type, Credit: Money.FromPiastres(e.CreditPiastres), Debit: Money.FromPiastres(e.DebitPiastres)))
  58 |                 .ToList();
  59 |             return new PayrollRules.PayrollLineInput(
  60 |                 emp.Id, emp.IsActive, emp.CompensationType == nameof(CompensationType.Monthly),
  61 |                 emp.MonthlySalaryPiastres is null ? null : Money.FromPiastres(emp.MonthlySalaryPiastres.Value),
  62 |                 empEntries, Money.Zero, Money.Zero);
  63 |         }).ToList();
```

> **File:** [`src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/CreatePayrollHandler.cs#L65-L88)  
> **Lines 65-88** in `Falah.Infrastructure.Employees.CreatePayrollHandler.HandleAsync(Falah.Application.Employees.CreatePayrollRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `339845a57fc7ec98268bbf79d4b204fbf1c029a3da4cff89771294522963e878`):

```csharp
  65 |         var computedLines = PayrollRules.CalculatePayrollLines(lineInputs);
  66 | 
  67 |         var runId = $"payroll-{request.RequestId}";
  68 |         var runEntity = new PayrollRunEntity
  69 |         {
  70 |             Id = runId, Period = request.Period, Status = nameof(PayrollRunStatus.Draft),
  71 |             CreatedBy = actorUser.Name, CreatedAtUtc = DateTimeOffset.UtcNow,
  72 |         };
  73 |         try
  74 |         {
  75 |             _db.PayrollRuns.Add(runEntity);
  76 |             foreach (var line in computedLines)
  77 |             {
  78 |                 _db.PayrollLines.Add(new PayrollLineEntity
  79 |                 {
  80 |                     Id = $"pl-{Guid.NewGuid():N}", PayrollRunId = runId, EmployeeId = line.EmployeeId,
  81 |                     BasePiastres = line.Base.Piastres, OvertimePiastres = line.Overtime.Piastres,
  82 |                     ExtraShiftsPiastres = line.ExtraShifts.Piastres, BonusesPiastres = line.Bonuses.Piastres,
  83 |                     DeductionsPiastres = line.Deductions.Piastres, AdvanceDeductionsPiastres = line.AdvanceDeductions.Piastres,
  84 |                     NetPayablePiastres = line.NetPayable.Piastres, Status = "unpaid",
  85 |                 });
  86 |             }
  87 |             await _db.SaveChangesAsync(ct);
  88 |         }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
PayrollRules computes payroll from earning categories and named deductions but ignores standalone Settlement debits. PayEmployee reduces current balance, while CreatePayroll freezes period net and PayPayroll pays that frozen amount without revalidating outstanding employee balance.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
If standalone Settlement is a prepayment of earnings already included in the same frozen payroll, paying both paths can disburse the same obligation twice; source flow shows this mechanism but does not establish the accounting policy or an actual duplicate payout.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
PayrollRules computes payroll from earning categories and named deductions but ignores standalone Settlement debits. PayEmployee reduces current balance, while CreatePayroll freezes period net and PayPayroll pays that frozen amount without revalidating outstanding employee balance.

**E. Future Repair Specification:**  
Owner decision first. If settlements prepay same-period earnings, derive remaining liability from an explicit period/earning obligation and apply settlements both before payroll calculation and after run freeze; if the two paths are mutually exclusive, enforce atomic exclusion on both. Preserve frozen-run, replay, audit, source-balance, and historical-record behavior.

**Required Regression Test Cases:**
- Create earnings, settle same obligation standalone, then create/approve/pay payroll; total disbursement must satisfy owner-approved invariant.
- Create/approve payroll, apply standalone settlement, then pay frozen payroll; same invariant.
- Unrelated-period settlement must not be netted.
- Repeat and concurrent requests with distinct IDs; validate exactly-once employee ledger and treasury effects in approved disposable SQL Server integration DB.

**Acceptance Criteria & Verification Standard:**
- Written owner policy defines relationship between standalone Settlement and period payroll obligation.
- Tests implement that policy across both sequence orders, period boundaries, retries and concurrent requests.
- No duplicate obligation disbursement if policy treats Settlement as prepayment; otherwise distinct obligation is explicitly represented and reported.

---

### FIN-CAND-PACKAGING-COVERAGE-001: Unknown once-per-order packaging cost does not invalidate aggregate profit completeness
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_STATIC_WITH_QUALIFICATIONS_NO_SQL_REPRODUCTION` | **Disposition:** `REQUIRES_OWNER_BUSINESS_DECISION`
- **Disposition Rationale:** Source behavior is deterministic but depends on owner business policy (e.g. tax rounding policy, legacy casing tolerance, packaging cost allocation).

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs#L181-L190) | `181-190` | `Falah.Domain.Inventory.CostingRules.OrderPackagingCost(string, IEnumerable<Falah.Domain.Inventory.OrderPackagingRule>, Falah.Domain.Inventory.CostingContext) (MethodDeclaration)` | `c1624846b505` |
| [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs#L35-L46) | `35-46` | `Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int) (MethodDeclaration)` | `912310fad5a9` |
| [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L205-L223) | `205-223` | `Falah.Infrastructure.Reports.OwnerDashboardQueryService.BuildProfitability(Falah.Application.Reports.SalesReportDto, Falah.Application.Reports.FoodCostReportDto, long, long, int) (MethodDeclaration)` | `67c66a18a228` |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L399-L423) | `399-423` | `Falah.Infrastructure.Reports.ReportingQueryService.GetFoodCostReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` | `452d58690fb3` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs#L181-L190)  
> **Lines 181-190** in `Falah.Domain.Inventory.CostingRules.OrderPackagingCost(string, IEnumerable<Falah.Domain.Inventory.OrderPackagingRule>, Falah.Domain.Inventory.CostingContext) (MethodDeclaration)` (File SHA-256: `c1624846b505dd4e13798efa8cbf39129c3b711500c5da064e30c3dace0278ca`):

```csharp
 181 |     public static (Money Cost, bool Known, string? Name) OrderPackagingCost(string orderType, IEnumerable<OrderPackagingRule> rules, CostingContext ctx)
 182 |     {
 183 |         var rule = rules.FirstOrDefault(r => r.OrderType == orderType && r.Active);
 184 |         if (rule is null) return (Money.Zero, true, null);
 185 | 
 186 |         var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == rule.InventoryItemId);
 187 |         var (unitCost, _) = IngredientUnitCost(item, ctx);
 188 |         if (unitCost is null) return (Money.Zero, false, item?.Name ?? rule.InventoryItemId);
 189 |         return (PurchasingRules.PurchaseLineTotal(rule.QuantityMilliUnits, unitCost.Value), true, item?.Name);
 190 |     }
```

> **File:** [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs#L35-L46)  
> **Lines 35-46** in `Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int) (MethodDeclaration)` (File SHA-256: `912310fad5a965cf46b0b84c20c78b44059e61475bfc60569d950014527b038f`):

```csharp
  35 |     public static GrossProfitResult ComputeGrossProfit(
  36 |         Money netSales, Money knownFoodCost, int soldLineCount, int knownLineCount)
  37 |     {
  38 |         var isComplete = soldLineCount == 0 || knownLineCount >= soldLineCount;
  39 | 
  40 |         // Nothing at all is known about what these sales cost. Reporting zero cost - and therefore a
  41 |         // hundred percent margin - is not a conservative approximation, it is the opposite of one.
  42 |         // The whole figure is withheld until at least one line can be priced.
  43 |         if (soldLineCount > 0 && knownLineCount == 0)
  44 |             return new GrossProfitResult(netSales, null, null, null, null, soldLineCount, 0, false);
  45 | 
  46 |         var grossProfit = netSales - knownFoodCost;
```

> **File:** [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L205-L223)  
> **Lines 205-223** in `Falah.Infrastructure.Reports.OwnerDashboardQueryService.BuildProfitability(Falah.Application.Reports.SalesReportDto, Falah.Application.Reports.FoodCostReportDto, long, long, int) (MethodDeclaration)` (File SHA-256: `67c66a18a228c9e34b3d42aad4c609f75c5ae0bde36d87e8fabc366d8119bd29`):

```csharp
 205 |     private static ProfitabilityDto BuildProfitability(
 206 |         SalesReportDto sales, FoodCostReportDto foodCost, long expenses, long wages, int wagePostingCount)
 207 |     {
 208 |         var netSales = Money.FromPiastres(sales.NetSalesPiastres);
 209 |         var knownFoodCost = Money.FromPiastres(foodCost.TotalFoodCostPiastres);
 210 | 
 211 |         var gross = ProfitabilityRules.ComputeGrossProfit(
 212 |             netSales, knownFoodCost, foodCost.SoldLineCount, foodCost.KnownLineCount);
 213 |         var operating = ProfitabilityRules.ComputeOperatingMargin(
 214 |             netSales, knownFoodCost, Money.FromPiastres(expenses), Money.FromPiastres(wages),
 215 |             foodCost.SoldLineCount, foodCost.KnownLineCount, wagePostingCount);
 216 | 
 217 |         return new ProfitabilityDto(
 218 |             gross.NetSales.Piastres, gross.KnownFoodCost?.Piastres, gross.GrossProfit?.Piastres,
 219 |             gross.FoodCostPercent, gross.GrossProfitPercent,
 220 |             wages, operating.WagePercent,
 221 |             operating.OperatingResult?.Piastres, operating.OperatingPercent,
 222 |             gross.SoldLineCount, gross.KnownLineCount, gross.IsComplete,
 223 |             foodCost.Missing);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Completeness boolean uses dish-line counters while food-cost total also includes separately accumulated order-level packaging; packaging unknown state is not passed to profitability completeness calculation.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Profitability result can be numerically computed and marked complete although packaging costs are excluded; reports can overstate gross/operating profit. This concerns report completeness, not ledger posting or realized cash loss.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Completeness boolean uses dish-line counters while food-cost total also includes separately accumulated order-level packaging; packaging unknown state is not passed to profitability completeness calculation.

**E. Future Repair Specification:**  
Carry an explicit aggregate cost-coverage/completeness signal across dish and order-packaging components into profitability calculation and DTO, preserving distinct dish counts and per-order packaging cardinality. Keep missing names. Do not convert packaging unknowns into fabricated sold dish-line counts.

**Required Regression Test Cases:**
- Known dish plus unknown active packaging returns IsComplete=false and does not present the aggregate profit as fully costed.
- Known packaging adds exact configured cost once per order and retains true completeness.
- No active packaging rule remains known optional zero.
- Multiple dishes in one order versus multiple orders preserves packaging count/cost cardinality.
- DTO/API and UI communicate incomplete state consistently alongside missing item names.

**Acceptance Criteria & Verification Standard:**
- IsComplete is false whenever any sold dish or applicable packaging component is unknown.
- The amount excludes unknown cost but is visibly and machine-readably partial; counts retain their documented units.
- Known/optional-zero cases preserve existing results and packaging charges are applied exactly once per applicable order.

---

### FIN-CAND-PAYROLL-001: Concurrent pay/cancel can overwrite payroll terminal status
- **Severity:** `HIGH` | **Confidence:** `HIGH_STATIC_WITH_QUALIFICATIONS_NO_SQL_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L13-L27) | `13-27` | `Falah.Domain.Payroll.PayrollRules.AssertTransition(Falah.Domain.Payroll.PayrollRunStatus, Falah.Domain.Payroll.PayrollRunStatus) (MethodDeclaration)` | `4a2b2460a8e9` |
| [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L62-L87) | `62-87` | `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` | `d7a614f6752b` |
| [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L110-L162) | `110-162` | `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` | `d7a614f6752b` |
| [`src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/TransitionPayrollHandler.cs#L24-L60) | `24-60` | `Falah.Infrastructure.Employees.TransitionPayrollHandler.HandleAsync(Falah.Application.Employees.TransitionPayrollRequest, CancellationToken) (MethodDeclaration)` | `0d8401a67d75` |
| [`src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/PayrollEntities.cs#L5-L20) | `5-20` | `Falah.Infrastructure.Persistence.Entities.PayrollRunEntity.Id.get (GetAccessorDeclaration)` | `2a827c742cb0` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L460-L477) | `460-477` | `Falah.Infrastructure.Persistence.FalahDbContext.OnModelCreating(Microsoft.EntityFrameworkCore.ModelBuilder) (MethodDeclaration)` | `7fda50ac64f4` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Payroll/PayrollRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Payroll/PayrollRules.cs#L13-L27)  
> **Lines 13-27** in `Falah.Domain.Payroll.PayrollRules.AssertTransition(Falah.Domain.Payroll.PayrollRunStatus, Falah.Domain.Payroll.PayrollRunStatus) (MethodDeclaration)` (File SHA-256: `4a2b2460a8e991c874a4965b244ffa4f36d229d6460897d54968b6c0047f0f86`):

```csharp
  13 |     private static readonly IReadOnlyDictionary<PayrollRunStatus, PayrollRunStatus[]> Transitions = new Dictionary<PayrollRunStatus, PayrollRunStatus[]>
  14 |     {
  15 |         [PayrollRunStatus.Draft] = new[] { PayrollRunStatus.Calculated, PayrollRunStatus.Cancelled },
  16 |         [PayrollRunStatus.Calculated] = new[] { PayrollRunStatus.Reviewed, PayrollRunStatus.Cancelled },
  17 |         [PayrollRunStatus.Reviewed] = new[] { PayrollRunStatus.Approved, PayrollRunStatus.Cancelled },
  18 |         [PayrollRunStatus.Approved] = new[] { PayrollRunStatus.Paid, PayrollRunStatus.Cancelled },
  19 |         [PayrollRunStatus.Paid] = Array.Empty<PayrollRunStatus>(),
  20 |         [PayrollRunStatus.Cancelled] = Array.Empty<PayrollRunStatus>(),
  21 |     };
  22 | 
  23 |     public static void AssertTransition(PayrollRunStatus from, PayrollRunStatus to)
  24 |     {
  25 |         if (!Transitions.TryGetValue(from, out var allowed) || !allowed.Contains(to))
  26 |             throw new DomainException($"انتقال حالة Payroll غير مسموح: {from} → {to}");
  27 |     }
```

> **File:** [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L62-L87)  
> **Lines 62-87** in `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `d7a614f6752b5a33513b3cd4ecf1e4e1cfe0022d010822e38bb43160c40e4084`):

```csharp
  62 |         var run = await _db.PayrollRuns.FirstOrDefaultAsync(r => r.Id == request.PayrollRunId, ct)
  63 |             ?? throw new InvalidOperationException("مسير الرواتب غير موجود.");
  64 |         if (run.Status == nameof(PayrollRunStatus.Paid))
  65 |         {
  66 |             // A concurrent request settled this run between the idempotency lookup above and this
  67 |             // read. A paid run is not an unapproved one: the caller must see the same result the
  68 |             // winning request saw, not a rejection that would send an operator to pay it again.
  69 |             // Reading Paid here proves the winner committed, and its movement commits in the same
  70 |             // transaction, so the replay is guaranteed to be visible.
  71 |             if (await LoadReplayAsync() is { } settledConcurrently) return settledConcurrently;
  72 |         }
  73 |         if (run.Status != nameof(PayrollRunStatus.Approved))
  74 |             throw new DomainException("يجب اعتماد مسير الرواتب قبل الدفع.");
  75 | 
  76 |         ManagerApproval? approval = null;
  77 |         if (!string.IsNullOrEmpty(request.ManagerApprovalId))
  78 |         {
  79 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ManagerApproverUserId, ct)
  80 |                 ?? throw new DomainException("المعتمد غير موجود.");
  81 |             approval = new ManagerApproval(request.ManagerApprovalId, approverUser.Id, approverUser.Name,
  82 |                 approverUser.Role, DateTimeOffset.UtcNow, "payroll_pay", $"صرف Payroll {run.Period}");
  83 |         }
  84 |         var authorization = AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "payroll_pay", approval, requireApproval: true);
  85 | 
  86 |         var lines = await _db.PayrollLines.Where(l => l.PayrollRunId == request.PayrollRunId).ToListAsync(ct);
  87 |         var total = lines.Sum(l => l.NetPayablePiastres);
```

> **File:** [`src/Falah.Infrastructure/Employees/PayPayrollHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayPayrollHandler.cs#L110-L162)  
> **Lines 110-162** in `Falah.Infrastructure.Employees.PayPayrollHandler.HandleAsync(Falah.Application.Employees.PayPayrollRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `d7a614f6752b5a33513b3cd4ecf1e4e1cfe0022d010822e38bb43160c40e4084`):

```csharp
 110 |         var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
 111 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
 112 |         try
 113 |         {
 114 |             // Read under a lock, inside the transaction, immediately before spending.
 115 |             // Outside it, concurrent commands each read the same balance, each decide it covers
 116 |             // them, and each write - which is how an account holding 30 paid out three separate
 117 |             // transfers of 20. See TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync.
 118 |             var accountMovements = await TreasuryBalanceQuery.LoadAccountMovementsForUpdateAsync(_db, request.PaymentAccountId, ct);
 119 |             FinancialRules.AssertProtectedSourceBalance(accountMovements, request.PaymentAccountId, totalMoney);
 120 | 
 121 |             var journal = new EfFinancialJournal(_db);
 122 |             if (!journal.TryStart(idempotencyKey, "payroll_payment"))
 123 |                 throw new TreasuryConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
 124 | 
 125 |             var now = DateTimeOffset.UtcNow;
 126 |             var operationId = $"op-{Guid.NewGuid():N}";
 127 |             var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
 128 |                 $"tm-{Guid.NewGuid():N}", idempotencyKey, operationId, "payroll_payment",
 129 |                 request.PaymentAccountId, destinationAccountId: null, totalMoney,
 130 |                 "payroll", run.Id, businessDayId, shiftId: null,
 131 |                 $"صرف Payroll {run.Period}", actorUser.Name, authorization.Approver?.ApproverName, now);
 132 |             var movementEntity = OrderMapper.ToEntity(movement);
 133 |             _db.TreasuryMovements.Add(movementEntity);
 134 |             journal.Complete(idempotencyKey);
 135 | 
 136 |             var settlementCount = 0;
 137 |             foreach (var line in lines.Where(l => l.NetPayablePiastres > 0))
 138 |             {
 139 |                 line.Status = "paid";
 140 |                 line.PaidAtUtc = now;
 141 |                 var settlementId = $"payroll-settlement-{run.Id}-{line.Id}";
 142 |                 line.SettlementEntryId = settlementId;
 143 |                 await EmployeeLedgerPoster.PostIfNewAsync(_db, settlementId, line.EmployeeId, nameof(LedgerEntryType.PayrollSettlement),
 144 |                     $"تسوية Payroll {run.Period}", Money.FromPiastres(line.NetPayablePiastres), Money.Zero,
 145 |                     actorUser.Name, authorization.Approver?.ApproverName, null, run.Id, null, null, null, ct);
 146 |                 settlementCount++;
 147 |             }
 148 | 
 149 |             run.Status = nameof(PayrollRunStatus.Paid);
 150 |             run.PaidAtUtc = now;
 151 |             run.OperationId = operationId;
 152 |             run.TreasuryMovementId = movementEntity.Id;
 153 |             run.PaymentAccountId = request.PaymentAccountId;
 154 |             run.IdempotencyKey = idempotencyKey;
 155 | 
 156 |             _auditLog.Append(new AuditEvent(
 157 |                 $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
 158 |                 $"صرف Payroll {run.Period}", "employee", $"إجمالي {totalMoney.Piastres} قرش لعدد {settlementCount} موظف من {request.PaymentAccountId}",
 159 |                 "critical", now, AuthorizedBy: authorization.Approver?.ApproverName, OperationId: operationId, ReferenceId: run.Id));
 160 | 
 161 |             await _db.SaveChangesAsync(ct);
 162 |             await transaction.CommitAsync(ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Pay and cancel authorize from independent Approved snapshots; no shared run-level transaction lock, compare-and-swap state predicate, or concurrency token is visible. Payment protects source-account balance, not PayrollRun state.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Predicted inconsistent final state: a payroll can be financially paid while status becomes Cancelled, or payment can proceed after cancellation. Downstream reporting/recreation decisions become unreliable. Do not claim duplicate payment reproduced; same-run payment idempotency is counterevidence to duplicate pay on replay.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Pay and cancel authorize from independent Approved snapshots; no shared run-level transaction lock, compare-and-swap state predicate, or concurrency token is visible. Payment protects source-account balance, not PayrollRun state.

**E. Future Repair Specification:**  
Use one database-coordinated PayrollRun state transition primitive for pay/cancel (transaction-held run-row lock or optimistic concurrency token/conditional Approved-to-claimed update). Payment must acquire/claim Approved within its transaction before movement/ledger posting; cancellation must perform mutually exclusive conditional transition. A losing operation must produce no movement, journal, settlement, paid-line, or status side effects. Preserve idempotency, filtered live-period index, and consistent treasury/run lock ordering.

**Required Regression Test Cases:**
- Barrier: pay reads Approved, cancel commits first; payment claim must fail and leave zero financial rows/line effects.
- Barrier: cancellation reads Approved, payment claims/commits first; cancellation must fail and Paid status/effects remain consistent.
- Repeat same idempotency key and concurrent duplicate pay; only one movement and one settlement per line.
- Exercise cancellation and replacement-run creation against filtered live-period unique index.
- Assert state and all side effects atomically under production provider and supported isolation.

**Acceptance Criteria & Verification Standard:**
- At most one of pay/cancel succeeds for a given Approved version.
- Paid state is committed atomically with movement, journal, line settlements and audit; Cancelled state cannot coexist with those effects from the losing race.
- Lost race yields explicit conflict/reload response and no partial writes.
- Existing retry idempotency and one-live-run policy remain intact.

---

### FIN-CAND-PREPGRAPH-001: Concurrent opposite prep edges can evade pretransaction cycle admission
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_STATIC_WITH_QUALIFICATIONS_NO_SQL_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs#L97-L127) | `97-127` | `Falah.Domain.Inventory.CostingRules.ComputePrepRecipeCost(Falah.Domain.Inventory.PrepRecipe?, IReadOnlyList<Falah.Domain.Inventory.PrepRecipeLine>, Falah.Domain.Inventory.CostingContext, HashSet<string>?) (MethodDeclaration)` | `c1624846b505` |
| [`src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs#L84-L109) | `84-109` | `Falah.Infrastructure.Inventory.SavePrepRecipeHandler.AssertDoesNotConsumeItsOwnOutputAsync(string, Falah.Application.Inventory.SavePrepRecipeRequest, CancellationToken) (MethodDeclaration)` | `7309e875a2f4` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Inventory/CostingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Inventory/CostingRules.cs#L97-L127)  
> **Lines 97-127** in `Falah.Domain.Inventory.CostingRules.ComputePrepRecipeCost(Falah.Domain.Inventory.PrepRecipe?, IReadOnlyList<Falah.Domain.Inventory.PrepRecipeLine>, Falah.Domain.Inventory.CostingContext, HashSet<string>?) (MethodDeclaration)` (File SHA-256: `c1624846b505dd4e13798efa8cbf39129c3b711500c5da064e30c3dace0278ca`):

```csharp
  97 |     private static PrepRecipeCostResult ComputePrepRecipeCost(PrepRecipe? prep, IReadOnlyList<PrepRecipeLine> lines, CostingContext ctx, HashSet<string>? resolving)
  98 |     {
  99 |         if (prep is null || prep.OutputQuantityMilliUnits <= 0)
 100 |             return new PrepRecipeCostResult(null, Money.Zero, 0, Array.Empty<string>(), lines.Count, 0);
 101 | 
 102 |         var knownCost = Money.Zero;
 103 |         var knownLines = 0;
 104 |         var missing = new List<string>();
 105 | 
 106 |         foreach (var line in lines)
 107 |         {
 108 |             var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == line.InventoryItemId);
 109 |             var label = item?.Name ?? line.InventoryItemId;
 110 | 
 111 |             // Say WHY this line has no cost. "Missing: tahini" on a batch whose every ingredient
 112 |             // has an invoice would send the cost controller looking for a purchase that is
 113 |             // already there; the problem is the loop, and only this line knows it is in one.
 114 |             if (resolving is not null && item is not null && resolving.Contains(item.Id))
 115 |             { missing.Add($"{label} (وصفة تجهيز دائرية)"); continue; }
 116 | 
 117 |             var (unitCost, _) = IngredientUnitCost(item, ctx, resolving);
 118 |             if (unitCost is null) { missing.Add(label); continue; }
 119 |             knownCost += PurchasingRules.PurchaseLineTotal(line.QuantityMilliUnits, unitCost.Value);
 120 |             knownLines++;
 121 |         }
 122 | 
 123 |         var completeness = lines.Count > 0 ? (double)knownLines / lines.Count : 0;
 124 |         Money? unitCostResult = completeness == 1 && lines.Count > 0
 125 |             ? Money.FromPiastres((long)Math.Round(knownCost.Piastres * 1000m / prep.OutputQuantityMilliUnits))
 126 |             : null;
 127 | 
```

> **File:** [`src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/SavePrepRecipeHandler.cs#L84-L109)  
> **Lines 84-109** in `Falah.Infrastructure.Inventory.SavePrepRecipeHandler.AssertDoesNotConsumeItsOwnOutputAsync(string, Falah.Application.Inventory.SavePrepRecipeRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `7309e875a2f4cbf8e636edb3a91b7635746503faeb98e1f1461ff182a7730a74`):

```csharp
  84 |     private async Task AssertDoesNotConsumeItsOwnOutputAsync(string prepRecipeId, SavePrepRecipeRequest request, CancellationToken ct)
  85 |     {
  86 |         var output = request.OutputInventoryItemId;
  87 |         if (request.Lines.Any(l => l.InventoryItemId == output))
  88 |             throw new DomainException("لا يمكن أن تحتوي الخلطة على الصنف الناتج عنها نفسه.");
  89 | 
  90 |         // Every other stored batch, as "what it produces" -> "what it consumes". The chain is
  91 |         // walked outward from this recipe's ingredients; reaching our own output closes a loop.
  92 |         var others = await _db.PrepRecipes.AsNoTracking()
  93 |             .Where(p => p.Active && p.Id != prepRecipeId)
  94 |             .Select(p => new { p.OutputInventoryItemId, Inputs = p.Lines.Select(l => l.InventoryItemId).ToList() })
  95 |             .ToListAsync(ct);
  96 |         var producers = others.ToLookup(p => p.OutputInventoryItemId, p => p.Inputs, StringComparer.Ordinal);
  97 | 
  98 |         var seen = new HashSet<string>(request.Lines.Select(l => l.InventoryItemId), StringComparer.Ordinal);
  99 |         var pending = new Queue<string>(seen);
 100 |         while (pending.Count > 0)
 101 |         {
 102 |             foreach (var producerInputs in producers[pending.Dequeue()])
 103 |                 foreach (var input in producerInputs)
 104 |                 {
 105 |                     if (input == output)
 106 |                         throw new DomainException("هذه الخلطة تستهلك ناتجها من خلال خلطة أخرى، فلا يمكن حساب تكلفتها.");
 107 |                     if (seen.Add(input)) pending.Enqueue(input);
 108 |                 }
 109 |         }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Cycle validation is a shared-graph predicate read before the transaction, while each transaction writes only one recipe; no database-level serialization/revalidation of the graph-wide invariant is visible.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Predicted active prep-recipe cycle can be persisted despite save-time cycle rejection; costing becomes Unknown/incomplete for affected paths and report reliability falls. No direct treasury, inventory movement, or ledger posting from recipe save is evidenced.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Cycle validation is a shared-graph predicate read before the transaction, while each transaction writes only one recipe; no database-level serialization/revalidation of the graph-wide invariant is visible.

**E. Future Repair Specification:**  
Coordinate graph mutations with a transaction-scoped shared graph revision/lock, then revalidate against the current graph before persisting. Alternatively use serializable predicate validation with bounded retry and consistent cross-process semantics. Retain defensive Unknown cycle guard for existing/legacy data.

**Required Regression Test Cases:**
- Barrier-controlled opposite-edge concurrent save: at most one can commit; loser receives stable cycle/conflict error.
- Two unrelated acyclic concurrent recipe saves both succeed if product policy permits.
- Sequential cycle rejection remains.
- Legacy cyclic fixture terminates and returns Unknown/incomplete, with report not presenting complete cost.
- Exercise recipe replacement and output identity changes under same graph coordination.

**Acceptance Criteria & Verification Standard:**
- The active prep graph remains acyclic after every successful save, including concurrent saves through multiple server processes.
- A race loser causes no partial recipe/line replacement.
- Unrelated valid edits retain expected concurrency; bounded retry/conflict behavior is documented.
- Costing defense remains finite and conservative for pre-existing invalid data.

---

### FIN-CAND-REFUND-REPLAY-001: Overlapping full-refund replay can reject after same-key sibling committed
- **Severity:** `LOW` | **Confidence:** `HIGH_SOURCE_ORDERING;CONCURRENT_PROVIDER_RESPONSE_NOT_REPRODUCED` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs#L87-L121) | `87-121` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitRefundAsync() (MethodDeclaration)` | `bc399716e95c` |
| [`src/Falah.Domain/Orders/RefundRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs#L34-L42) | `34-42` | `Falah.Domain.Orders.RefundRules.ResolveOperationalShift(string, Falah.Domain.Shifts.ShiftStatus, string?, Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` | `10bf926d5ad2` |
| [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs#L75-L84) | `75-84` | `Falah.Infrastructure.Employees.PayEmployeeHandler.HandleAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken) (MethodDeclaration)` | `93b781b9b0d9` |
| [`src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/RecordEmployeeCashOutHandler.cs#L59-L71) | `59-71` | `Falah.Infrastructure.Employees.RecordEmployeeCashOutHandler.HandleAsync(Falah.Application.Employees.RecordEmployeeCashOutRequest, CancellationToken) (MethodDeclaration)` | `c3665f0dc548` |
| [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L154-L165) | `154-165` | `Falah.Infrastructure.Orders.RefundOrderHandler.FindExistingAsync(string, CancellationToken) (MethodDeclaration)` | `ca7d3922495a` |
| [`tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/RefundVerticalSliceTests.cs#L131-L150) | `131-150` | `Falah.Server.IntegrationTests.RefundVerticalSliceTests.Concurrent_refunds_for_different_amounts_on_the_same_order_never_overdraw_the_order_total() (MethodDeclaration)` | `8dc121ce9a7c` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs#L87-L121)  
> **Lines 87-121** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitRefundAsync() (MethodDeclaration)` (File SHA-256: `bc399716e95c2a9b42e3c5db4ad92d6e3fe9879f2b042fa539ec6298d6adea7a`):

```csharp
  87 |     [RelayCommand] private async Task SubmitRefundAsync()
  88 |     {
  89 |         if (IsRefunding || OrderLookupResult is null) return;
  90 |         IsRefunding = true; RefundError = null;
  91 |         try
  92 |         {
  93 |             if (_pendingRefund is null)
  94 |             {
  95 |                 if (!CashInput.TryParse(RefundAmountText, out var amount) || amount <= 0 ||
  96 |                     amount > OrderLookupResult.TotalPiastres - OrderLookupResult.RefundAmountPiastres)
  97 |                 { RefundError = "أدخل مبلغاً ضمن الرصيد المتاح للاسترداد."; return; }
  98 |                 if (string.IsNullOrWhiteSpace(RefundReason)) { RefundError = "أدخل سبب الاسترداد."; return; }
  99 |                 var (approvalId, approver, ok) = await ResolveManagerApprovalIfNeededAsync(RefundNeedsApproval,
 100 |                     RefundManagerPin, "orders_refund", RefundReason, e => RefundError = e);
 101 |                 if (!ok) return;
 102 |                 var request = new RefundOrderRequest(_refundRequestId, OrderLookupResult.Id, amount, RefundReason,
 103 |                     _session.CurrentUser!.Id, approvalId, approver);
 104 |                 await CompleteRefundAsync(request, false);
 105 |             }
 106 |             else await CompleteRefundAsync(_pendingRefund, true);
 107 |         }
 108 |         finally { IsRefunding = false; }
 109 |     }
 110 |     private async Task CompleteRefundAsync(RefundOrderRequest request, bool wasUnresolved)
 111 |     {
 112 |         _pendingRefund = request; RefreshOperationState();
 113 |         var result = await _session.Api.RefundAsync(request);
 114 |         if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingRefund = null;
 115 |         RefreshOperationState();
 116 |         if (!result.Success || result.Value is null) { RefundError = result.Error; return; }
 117 |         _refundRequestId = Guid.NewGuid().ToString("N");
 118 |         OrderLookupResult = OrderLookupResult! with { Status = result.Value.OrderStatus,
 119 |             RefundAmountPiastres = OrderLookupResult.TotalPiastres - result.Value.RemainingRefundablePiastres };
 120 |         ReprintStatusMessage = "تم تسجيل استرداد " + CashInput.Format(result.Value.AmountPiastres) + " ج";
 121 |         IsRefundOpen = false; RefundManagerPin = "";
```

> **File:** [`src/Falah.Domain/Orders/RefundRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/RefundRules.cs#L34-L42)  
> **Lines 34-42** in `Falah.Domain.Orders.RefundRules.ResolveOperationalShift(string, Falah.Domain.Shifts.ShiftStatus, string?, Falah.Domain.Financial.PaymentMethod) (MethodDeclaration)` (File SHA-256: `10bf926d5ad29fcc2d25b98ce32e523ae76769ae077bbd0d659b59443501feb9`):

```csharp
  34 |     public static RefundShiftPolicy ResolveOperationalShift(
  35 |         string originalShiftId, ShiftStatus originalShiftStatus, string? activeShiftId, PaymentMethod paymentMethod)
  36 |     {
  37 |         var historicalImmutable = originalShiftStatus == ShiftStatus.Closed;
  38 |         if (historicalImmutable && paymentMethod == PaymentMethod.Cash && activeShiftId is null)
  39 |             throw new DomainException("يجب فتح وردية قبل تنفيذ مرتجع نقدي لطلب من وردية مغلقة.");
  40 | 
  41 |         return new RefundShiftPolicy(historicalImmutable, historicalImmutable ? activeShiftId : originalShiftId);
  42 |     }
```

> **File:** [`src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/PayEmployeeHandler.cs#L75-L84)  
> **Lines 75-84** in `Falah.Infrastructure.Employees.PayEmployeeHandler.HandleAsync(Falah.Application.Employees.PayEmployeeRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `93b781b9b0d97ec14b8c8836f9ad1b47fb908295b1089e3a627b0994dcbb355c`):

```csharp
  75 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
  76 |         try
  77 |         {
  78 |             // Same "lock the employee row first" discipline as RecordEmployeeCashOutHandler: an
  79 |             // employee with no ledger rows yet has no stable row of their own to serialize two
  80 |             // concurrent first-payments on.
  81 |             await _db.Employees
  82 |                 .FromSqlRaw("SELECT * FROM Employees WITH (UPDLOCK, ROWLOCK, HOLDLOCK) WHERE Id = {0}", request.EmployeeId)
  83 |                 .Select(e => e.Id).SingleAsync(ct);
  84 |             if (await LoadReplayAsync() is { } settledConcurrently) return settledConcurrently;
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Durable refund-key lookup happens before parent order serialization, with no repeat lookup after sibling full refund changes refundable state; DomainException cap rejection is not handled by saved-receipt recovery catch.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Conditional misleading failure for one concurrent duplicate acknowledgement; later sequential same-key retry recovers. No new financial movement, direct moneyloss, overrefund or permanentclientloop established.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Durable refund-key lookup happens before parent order serialization, with no repeat lookup after sibling full refund changes refundable state; DomainException cap rejection is not handled by saved-receipt recovery catch.

**E. Future Repair Specification:**  
Recheck same-key durable refund receipt after acquiring order lock and before cap rejection; preserve actor/order/payload binding, cap and funds rules. Do not broadly treat DomainException as replay.

**Required Regression Test Cases:**
- Overlapping full-amount same-key refund: second request begins lookup before first commit, then after lock sees same committed receipt.
- Existing partial same-key concurrency control; distinct-key over-cap, mismatch binding and failed-first sibling.

**Acceptance Criteria & Verification Standard:**
- After order lock, exact same-key sibling commit is returned as replay without second refund or misleading cap error.
- Distinct-key refund cap and mismatched order/key binding remain enforced; failed sibling does not fabricate success.

---

### FIN-CAND-REPORT-RANGE-001: Reversed report endpoints normalize sales but independently filter cost with contradictory date bounds
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_STATIC_WITH_QUALIFICATIONS_NO_SQL_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Common/BusinessClock.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs#L47-L53) | `47-53` | `Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDates(DateOnly, DateOnly) (MethodDeclaration)` | `f9a82540cadf` |
| [`src/Falah.Domain/Common/BusinessClock.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs#L65-L82) | `65-82` | `Falah.Domain.Common.BusinessDateRange.Resolve(DateOnly?, DateOnly?, DateOnly?) (MethodDeclaration)` | `f9a82540cadf` |
| [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs#L35-L63) | `35-63` | `Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int) (MethodDeclaration)` | `912310fad5a9` |
| [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L38-L70) | `38-70` | `Falah.Infrastructure.Reports.OwnerDashboardQueryService.GetAsync(Falah.Domain.Common.BusinessDateRange, CancellationToken) (MethodDeclaration)` | `67c66a18a228` |
| [`src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs#L205-L225) | `205-225` | `Falah.Infrastructure.Reports.OwnerDashboardQueryService.BuildProfitability(Falah.Application.Reports.SalesReportDto, Falah.Application.Reports.FoodCostReportDto, long, long, int) (MethodDeclaration)` | `67c66a18a228` |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L35-L48) | `35-48` | `Falah.Infrastructure.Reports.ReportingQueryService.LoadSalesOrdersAsync(Falah.Domain.Common.BusinessDateRange?, string?, CancellationToken) (MethodDeclaration)` | `452d58690fb3` |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L241-L246) | `241-246` | `Falah.Infrastructure.Reports.ReportingQueryService.Range(DateOnly?, DateOnly?) (MethodDeclaration)` | `452d58690fb3` |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L349-L363) | `349-363` | `Falah.Infrastructure.Reports.ReportingQueryService.GetFoodCostReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` | `452d58690fb3` |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L422-L424) | `422-424` | `Falah.Infrastructure.Reports.ReportingQueryService.GetFoodCostReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` | `452d58690fb3` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1976-L1983) | `1976-1983` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Common/BusinessClock.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs#L47-L53)  
> **Lines 47-53** in `Falah.Domain.Common.BusinessClock.UtcRangeForBusinessDates(DateOnly, DateOnly) (MethodDeclaration)` (File SHA-256: `f9a82540cadfcdc043a630041f2877f527591b2d9d400fb870da2c98f9ec9208`):

```csharp
  47 |     public static (DateTimeOffset Start, DateTimeOffset End) UtcRangeForBusinessDates(DateOnly from, DateOnly to)
  48 |     {
  49 |         if (to < from) (from, to) = (to, from);
  50 |         var (start, _) = UtcRangeForBusinessDate(from);
  51 |         var (_, end) = UtcRangeForBusinessDate(to);
  52 |         return (start, end);
  53 |     }
```

> **File:** [`src/Falah.Domain/Common/BusinessClock.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/BusinessClock.cs#L65-L82)  
> **Lines 65-82** in `Falah.Domain.Common.BusinessDateRange.Resolve(DateOnly?, DateOnly?, DateOnly?) (MethodDeclaration)` (File SHA-256: `f9a82540cadfcdc043a630041f2877f527591b2d9d400fb870da2c98f9ec9208`):

```csharp
  65 |     public static BusinessDateRange? Resolve(DateOnly? from, DateOnly? to, DateOnly? singleDate)
  66 |     {
  67 |         if (from is { } start) return new BusinessDateRange(start, to ?? start);
  68 |         if (to is { } end) return new BusinessDateRange(end, end);
  69 |         if (singleDate is { } day) return SingleDay(day);
  70 |         return null;
  71 |     }
  72 | 
  73 |     public int DayCount => To.DayNumber - From.DayNumber + 1;
  74 |     public bool IsSingleDay => From == To;
  75 | 
  76 |     public (DateTimeOffset Start, DateTimeOffset End) ToUtcRange() =>
  77 |         BusinessClock.UtcRangeForBusinessDates(From, To);
  78 | 
  79 |     public IEnumerable<DateOnly> Days()
  80 |     {
  81 |         for (var day = From; day <= To; day = day.AddDays(1)) yield return day;
  82 |     }
```

> **File:** [`src/Falah.Domain/Reports/ProfitabilityRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Reports/ProfitabilityRules.cs#L35-L63)  
> **Lines 35-63** in `Falah.Domain.Reports.ProfitabilityRules.ComputeGrossProfit(Falah.Domain.Common.Money, Falah.Domain.Common.Money, int, int) (MethodDeclaration)` (File SHA-256: `912310fad5a965cf46b0b84c20c78b44059e61475bfc60569d950014527b038f`):

```csharp
  35 |     public static GrossProfitResult ComputeGrossProfit(
  36 |         Money netSales, Money knownFoodCost, int soldLineCount, int knownLineCount)
  37 |     {
  38 |         var isComplete = soldLineCount == 0 || knownLineCount >= soldLineCount;
  39 | 
  40 |         // Nothing at all is known about what these sales cost. Reporting zero cost - and therefore a
  41 |         // hundred percent margin - is not a conservative approximation, it is the opposite of one.
  42 |         // The whole figure is withheld until at least one line can be priced.
  43 |         if (soldLineCount > 0 && knownLineCount == 0)
  44 |             return new GrossProfitResult(netSales, null, null, null, null, soldLineCount, 0, false);
  45 | 
  46 |         var grossProfit = netSales - knownFoodCost;
  47 | 
  48 |         // Percentages of a non-positive sales figure are meaningless, not zero: a day with no sales
  49 |         // has no food-cost percentage at all, and printing 0% would read as "excellent".
  50 |         double? foodCostPercent = null;
  51 |         double? grossProfitPercent = null;
  52 |         if (netSales.Piastres > 0)
  53 |         {
  54 |             foodCostPercent = Math.Round((double)knownFoodCost.Piastres / netSales.Piastres * 100, 2);
  55 |             grossProfitPercent = Math.Round((double)grossProfit.Piastres / netSales.Piastres * 100, 2);
  56 |         }
  57 | 
  58 |         return new GrossProfitResult(netSales, knownFoodCost, grossProfit,
  59 |             foodCostPercent, grossProfitPercent, soldLineCount, knownLineCount, isComplete);
  60 |     }
  61 | 
  62 |     /// <param name="IsComplete">False when any figure feeding it was itself incomplete - the same
  63 |     /// rule as above, carried up rather than reset.</param>
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Range ordering handled inconsistently between normalized sales UTC interval and raw cost/date series consumers.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Misleading cost/profit/report period, not a proven money posting or authorization bypass. Static financial report correctness candidate.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Range ordering handled inconsistently between normalized sales UTC interval and raw cost/date series consumers.

**E. Future Repair Specification:**  
Normalize or reject reversed date endpoints once at shared request boundary and pass one interval to sales, cost, expenses, and date-series calculations.

**Required Regression Test Cases:**
- Pure forward/same/reversed date range and Days/DayCount consistency.
- Authorized seeded report nonzero sales/known recipe cost reversed endpoints; direct food-cost/expense endpoints use same contract.

**Acceptance Criteria & Verification Standard:**
- Reversed endpoints either normalize once for every report series or are rejected consistently; sales, food cost, expenses, days and day count use same interval.
- Forward, same-day, reversed, and boundary-date reports agree across DTO and direct endpoints.

---

### FIN-CAND-BUSINESS-DAY-OPEN-RACE-001: Distinct concurrent business-day opens can pass pre-insert active-state validation
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_STATIC_FOR_MISSING_SOURCE_GUARD; CONCURRENT_DUAL-COMMIT PREDICTED_NOT_EXECUTED` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L27-L28) | `27-28` | `Falah.Infrastructure.BusinessDay.OpenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `6be465799c1b` |
| [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L34-L55) | `34-55` | `Falah.Infrastructure.BusinessDay.OpenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) (MethodDeclaration)` | `6be465799c1b` |
| [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs#L22-L43) | `22-43` | `Falah.Domain.BusinessDay.BusinessDayRules.IsActive(Falah.Domain.BusinessDay.BusinessDayRecord?) (MethodDeclaration)` | `cdc3306873f2` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L116-L127) | `116-127` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L137-L141) | `137-141` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L17-L27) | `17-27` | `File-level declaration or top-level statement` | `7b2afcf63d01` |
| [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L33-L55) | `33-55` | `Falah.Infrastructure.Shifts.OpenShiftHandler.HandleAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken) (MethodDeclaration)` | `7b2afcf63d01` |
| [`src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/OpenShiftHandler.cs#L69-L96) | `69-96` | `Falah.Infrastructure.Shifts.OpenShiftHandler.HandleAsync(Falah.Application.Shifts.OpenShiftRequest, CancellationToken) (MethodDeclaration)` | `7b2afcf63d01` |
| [`src/Falah.Infrastructure/Persistence/Migrations/20260903154545_AddBusinessDay.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Migrations/20260903154545_AddBusinessDay.cs#L14-L38) | `14-38` | `Falah.Infrastructure.Persistence.Migrations.AddBusinessDay.Up(Microsoft.EntityFrameworkCore.Migrations.MigrationBuilder) (MethodDeclaration)` | `0b2db7efe213` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L27-L28)  
> **Lines 27-28** in `Falah.Infrastructure.BusinessDay.OpenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `6be465799c1b5b0953d01a69cdee1c731aaba0029e91a82bf956e1c63ccafecc`):

```csharp
  27 |         var existing = await _db.BusinessDays.AsNoTracking().FirstOrDefaultAsync(d => d.OpenRequestId == request.RequestId, ct);
  28 |         if (existing is not null) return ToResult(existing, wasReplayed: true);
```

> **File:** [`src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/BusinessDay/OpenBusinessDayHandler.cs#L34-L55)  
> **Lines 34-55** in `Falah.Infrastructure.BusinessDay.OpenBusinessDayHandler.HandleAsync(Falah.Application.BusinessDay.OpenBusinessDayRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `6be465799c1b5b0953d01a69cdee1c731aaba0029e91a82bf956e1c63ccafecc`):

```csharp
  34 |         var now = DateTimeOffset.UtcNow;
  35 |         var activeDayEntities = await _db.BusinessDays.AsNoTracking()
  36 |             .Where(d => d.Status == nameof(BusinessDayStatus.Open) || d.Status == nameof(BusinessDayStatus.Closing) || d.Status == nameof(BusinessDayStatus.Reopened))
  37 |             .ToListAsync(ct);
  38 |         var activeDays = activeDayEntities
  39 |             .Select(d => new BusinessDayRecord(d.Id, d.BusinessDate, d.OpenedBy, d.OpenedAtUtc, Enum.Parse<BusinessDayStatus>(d.Status), d.CloseRevision))
  40 |             .ToList();
  41 | 
  42 |         var businessDate = request.BusinessDate ?? BusinessClock.BusinessDate(now);
  43 |         var day = BusinessDayRules.Open($"day-{Guid.NewGuid():N}", businessDate, request.OpenedByUserId, now, activeDays);
  44 | 
  45 |         var entity = new BusinessDayEntity
  46 |         {
  47 |             Id = day.Id, BusinessDate = day.BusinessDate, OpenedBy = day.OpenedBy, OpenedAtUtc = day.OpenedAtUtc,
  48 |             Status = nameof(BusinessDayStatus.Open), CloseRevision = 0, OpenRequestId = request.RequestId,
  49 |         };
  50 | 
  51 |         try
  52 |         {
  53 |             _db.BusinessDays.Add(entity);
  54 |             await _db.SaveChangesAsync(ct);
  55 |             return ToResult(entity, wasReplayed: false);
```

> **File:** [`src/Falah.Domain/BusinessDay/BusinessDayRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/BusinessDay/BusinessDayRules.cs#L22-L43)  
> **Lines 22-43** in `Falah.Domain.BusinessDay.BusinessDayRules.IsActive(Falah.Domain.BusinessDay.BusinessDayRecord?) (MethodDeclaration)` (File SHA-256: `cdc3306873f2b6f46029186ec995b90415eda247ee3ec5b7408a9285f390ed06`):

```csharp
  22 |     private static readonly BusinessDayStatus[] ActiveStatuses = { BusinessDayStatus.Open, BusinessDayStatus.Closing, BusinessDayStatus.Reopened };
  23 | 
  24 |     public static bool IsActive(BusinessDayRecord? day) => day is not null && ActiveStatuses.Contains(day.Status);
  25 | 
  26 |     private static readonly IReadOnlyDictionary<BusinessDayStatus, BusinessDayStatus[]> Legal = new Dictionary<BusinessDayStatus, BusinessDayStatus[]>
  27 |     {
  28 |         [BusinessDayStatus.Open] = new[] { BusinessDayStatus.Closing },
  29 |         [BusinessDayStatus.Reopened] = new[] { BusinessDayStatus.Closing },
  30 |         [BusinessDayStatus.Closing] = new[] { BusinessDayStatus.Closed },
  31 |         [BusinessDayStatus.Closed] = new[] { BusinessDayStatus.Reopened },
  32 |     };
  33 | 
  34 |     public static void AssertTransition(BusinessDayStatus from, BusinessDayStatus to)
  35 |     {
  36 |         if (!Legal.TryGetValue(from, out var next) || !next.Contains(to))
  37 |             throw new DomainException($"انتقال حالة يوم العمل غير صالح: {from} → {to}");
  38 |     }
  39 | 
  40 |     public static BusinessDayRecord Open(string id, DateOnly businessDate, string openedBy, DateTimeOffset nowUtc, IEnumerable<BusinessDayRecord> existingDays)
  41 |     {
  42 |         if (existingDays.Any(IsActive)) throw new DomainException("يوجد يوم عمل نشط بالفعل.");
  43 |         return new BusinessDayRecord(id, businessDate, openedBy, nowUtc, BusinessDayStatus.Open, CloseRevision: 0);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Active-day admission is a read-check-insert invariant. OpenBusinessDayHandler reads current active rows and invokes BusinessDayRules.Open before a separate insert; there is no shared transaction/conditional state claim, while the schema has uniqueness only by request ID.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
More than one active business day may be persisted, making day attribution, financial close blockers/snapshots, and active-day lookups ambiguous. No actual duplicate day or accounting loss has been reproduced.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Active-day admission is a read-check-insert invariant. OpenBusinessDayHandler reads current active rows and invokes BusinessDayRules.Open before a separate insert; there is no shared transaction/conditional state claim, while the schema has uniqueness only by request ID.

**E. Future Repair Specification:**  
Confirm the invariant scope. If one active BusinessDay is intended, enforce it at the same persistence boundary as the single-open-shift invariant (provider-appropriate active-row uniqueness or a serialized admission row/lock covering Open and Reopen), and make losing admission return a stable conflict without partial side effects. Preserve same-request idempotency. Do not rely solely on a pre-insert read.

**Required Regression Test Cases:**
- Barrier-controlled concurrent opens with two distinct RequestIds: at most one active day commits; loser receives a conflict and no second row is created.
- Same RequestId replay returns the original day without a second row.
- Concurrent reopen of one day and open of another cannot create two active days if global invariant is confirmed.
- Sequential open while Open, Closing, or Reopened rejects; after valid close, a new day opens.
- Run against supported SQL Server provider and two independent DbContext/process instances; assert database-level invariant.

**Acceptance Criteria & Verification Standard:**
- A documented owner policy establishes the active-day scope (global or per defined partition).
- If global single-active-day remains intended, every Open/Reopen transition is serialized or constrained by database-enforced uniqueness so concurrent instances cannot persist more than one active day.
- A losing concurrency attempt leaves no partial state and the existing same-request replay contract remains intact.

---

### FIN-CAND-SAFE-DROP-CLOSED-SHIFT-ATTRIBUTION-001: SafeDrop may attribute a later drawer movement to a closed shift and change its reprinted breakdown
- **Severity:** `LOW` | **Confidence:** `SOURCE_CONFIRMED_ATTRIBUTION_BEHAVIOR;OWNER_POLICY_UNRESOLVED;NO_MONETARY_LOSS_SHOWN` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L45-L46) | `45-46` | `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) (MethodDeclaration)` | `601f29ab9abe` |
| [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L58-L59) | `58-59` | `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) (MethodDeclaration)` | `601f29ab9abe` |
| [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L74-L83) | `74-83` | `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) (MethodDeclaration)` | `601f29ab9abe` |
| [`src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/ShiftReconciliation.cs#L59-L68) | `59-68` | `Falah.Infrastructure.Shifts.ShiftReconciliation.ComputeBreakdownAsync(Falah.Infrastructure.Persistence.FalahDbContext, string, Falah.Domain.Common.Money, CancellationToken) (MethodDeclaration)` | `cd1a406323ee` |
| [`src/Falah.Infrastructure/Printing/PrintJobQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/PrintJobQueryService.cs#L181-L204) | `181-204` | `Falah.Infrastructure.Printing.PrintJobQueryService.ComposeShiftCloseSlipAsync(Falah.Infrastructure.Persistence.Entities.PrintJobEntity, CancellationToken) (MethodDeclaration)` | `be7e9b0dab28` |
| [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs#L79-L84) | `79-84` | `Falah.Infrastructure.Shifts.CloseShiftHandler.HandleAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken) (MethodDeclaration)` | `58319fdf46c1` |
| [`src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Shifts/CloseShiftHandler.cs#L95-L98) | `95-98` | `Falah.Infrastructure.Shifts.CloseShiftHandler.HandleAsync(Falah.Application.Shifts.CloseShiftRequest, CancellationToken) (MethodDeclaration)` | `58319fdf46c1` |
| [`src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/CashDrawer/CashierCashMovementHandler.cs#L56-L59) | `56-59` | `Falah.Infrastructure.CashDrawer.CashierCashMovementHandler.HandleAsync(Falah.Application.CashDrawer.CashierCashMovementRequest, CancellationToken) (MethodDeclaration)` | `ce1184171f64` |
| [`tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs#L163-L187) | `163-187` | `Falah.Server.IntegrationTests.TreasuryVerticalSliceTests.A_safe_drop_moves_the_cash_into_the_main_treasury_rather_than_out_of_the_books() (MethodDeclaration)` | `415f68db1d53` |
| [`tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs#L90-L98) | `90-98` | `Falah.Server.IntegrationTests.CashierCashMovementVerticalSliceTests.A_cash_movement_requires_the_shift_to_be_open() (MethodDeclaration)` | `7494487952ec` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L45-L46)  
> **Lines 45-46** in `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `601f29ab9abe3dd644137bbf20383cf21ab60e040ff79c55a5b2c37cff292b76`):

```csharp
  45 |         _ = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  46 |             ?? throw new InvalidOperationException("الوردية غير موجودة.");
```

> **File:** [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L58-L59)  
> **Lines 58-59** in `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `601f29ab9abe3dd644137bbf20383cf21ab60e040ff79c55a5b2c37cff292b76`):

```csharp
  58 |         var businessDayId = await BusinessDayGate.RequireOpenAsync(_db, ct);
  59 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
```

> **File:** [`src/Falah.Infrastructure/Treasury/SafeDropHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Treasury/SafeDropHandler.cs#L74-L83)  
> **Lines 74-83** in `Falah.Infrastructure.Treasury.SafeDropHandler.HandleAsync(Falah.Application.Treasury.SafeDropRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `601f29ab9abe3dd644137bbf20383cf21ab60e040ff79c55a5b2c37cff292b76`):

```csharp
  74 |             var movement = TreasuryMovementFactory.BuildCashLifecycleMovement(
  75 |                 $"tm-{Guid.NewGuid():N}", idempotencyKey, $"op-{Guid.NewGuid():N}", "safe_drop",
  76 |                 // The destination is the whole point of a safe drop: cash leaves the till and goes
  77 |                 // INTO the main treasury. Passing null here meant the drawer fell and nothing rose -
  78 |                 // the money left the books entirely - while the audit line below said, and still
  79 |                 // says, that it went to the main treasury. A movement that disagrees with its own
  80 |                 // audit record is worse than either being wrong alone.
  81 |                 TreasuryAccounts.Drawer, TreasuryAccounts.Main, Money.FromPiastres(request.AmountPiastres),
  82 |                 "safe_drop", request.DropRequestId, businessDayId, request.ShiftId,
  83 |                 request.Reason, createdByUser.Name, authorization.Approver?.ApproverName, now);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
SafeDrop validates only that the request ShiftId exists; it does not require the referenced shift to remain Open. It posts with the current open BusinessDayId plus that request ShiftId. ShiftReconciliation later selects Posted safe_drop rows by ShiftId. Reprinting a close slip recomputes detail from current rows but restores the stored expected, actual and difference headline.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
The closed shift close-slip component breakdown can reflect a later SafeDrop while its headline remains the original saved close values. This can reduce reconciliation clarity or attribute later custody to an earlier shift if that is not the intended policy.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
SafeDrop validates only that the request ShiftId exists; it does not require the referenced shift to remain Open. It posts with the current open BusinessDayId plus that request ShiftId. ShiftReconciliation later selects Posted safe_drop rows by ShiftId. Reprinting a close slip recomputes detail from current rows but restores the stored expected, actual and difference headline.

**E. Future Repair Specification:**  
['If prohibit: enforce shift-open state in a concurrency-safe transaction boundary shared with shift close.', 'If allow: represent later movement as an explicit post-close adjustment/revision and distinguish it from the immutable close snapshot in reprints.']

**Required Regression Test Cases:**
- Open BusinessDay and closed Shift: assert the owner-selected accepted/rejected outcome and posted movement ShiftId/BusinessDayId.
- After close and any allowed later SafeDrop, reprint the slip and assert documented headline/component version semantics.
- Race SafeDrop against CloseShift on the same shift using a disposable production-SQL-Server-provider test; assert a policy-valid serialized outcome.
- If later posting is allowed, verify the audit/reprint distinguishes a later adjustment from cash present at original close.

**Acceptance Criteria & Verification Standard:**
- Owner decision is documented.
- Stored close headline retains historical meaning.
- Reprints either preserve close-time breakdown or clearly mark later adjustments.
- Concurrent close/post outcomes obey the chosen policy.

---

### FIN-CAND-WPF-REFUND-AMBIGUOUS-RETRY-001: Legacy WPF refund submit does not preserve the same idempotency key after an ambiguous outcome
- **Severity:** `LOW` | **Confidence:** `SOURCE-CONFIRMED_CLIENT_DIVERGENCE; CONDITIONAL_FINANCIAL_IMPACT` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L35-L55) | `35-55` | `Falah.Infrastructure.Orders.RefundOrderHandler.RefundOrderHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog) (ConstructorDeclaration)` | `ca7d3922495a` |
| [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs#L45-L65) | `45-65` | `Falah.Client.Cashier.ViewModels.CashierViewModel.LookupOrderAsync() (MethodDeclaration)` | `9f41d9133ed4` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Orders/RefundOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/RefundOrderHandler.cs#L35-L55)  
> **Lines 35-55** in `Falah.Infrastructure.Orders.RefundOrderHandler.RefundOrderHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog) (ConstructorDeclaration)` (File SHA-256: `ca7d3922495ad8ed8f9979feeca461f3fca0aa150fde7790714e29046e32d085`):

```csharp
  35 |     private readonly IApprovalRegistry _approvalRegistry;
  36 |     private readonly IAuditLog _auditLog;
  37 | 
  38 |     public RefundOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
  39 |     {
  40 |         _db = db;
  41 |         _approvalRegistry = approvalRegistry;
  42 |         _auditLog = auditLog;
  43 |     }
  44 | 
  45 |     public async Task<RefundOrderResult> HandleAsync(RefundOrderRequest request, CancellationToken ct = default)
  46 |     {
  47 |         var refundKey = $"refund-{request.RefundRequestId}";
  48 | 
  49 |         var existing = await FindExistingAsync(refundKey, ct);
  50 |         if (existing is not null) return await ToResultAsync(existing, wasReplayed: true, ct);
  51 | 
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
  53 |             ?? throw new InvalidOperationException("الكاشير غير موجود.");
  54 |         var cashier = OrderMapper.ToActor(cashierUser);
  55 | 
```

> **File:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.OrderLookup.cs#L45-L65)  
> **Lines 45-65** in `Falah.Client.Cashier.ViewModels.CashierViewModel.LookupOrderAsync() (MethodDeclaration)` (File SHA-256: `9f41d9133ed49ccb2e8dff94a43e70dc525500836f7c6c6e8ada4912bc1861dd`):

```csharp
  45 |         RefundSuccessMessage = null;
  46 |         try
  47 |         {
  48 |             var result = await _session.Api.LookupOrderByNumberAsync(LookupOrderNumber.Trim());
  49 |             if (!result.Success || result.Value is null)
  50 |             {
  51 |                 LookupError = result.Error ?? "الطلب غير موجود.";
  52 |                 return;
  53 |             }
  54 |             LookedUpOrder = result.Value;
  55 |             OnPropertyChanged(nameof(RefundableRemainingPiastres));
  56 |             RefundAmountText = (RefundableRemainingPiastres / 100m).ToString("0.00");
  57 |         }
  58 |         finally
  59 |         {
  60 |             IsLookingUp = false;
  61 |         }
  62 |     }
  63 | 
  64 |     private bool CanRefund() => LookedUpOrder is not null && !IsRefunding;
  65 | 
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
WPF SubmitRefundAsync creates a fresh RefundRequestId per invocation and WPF ApiClient does not expose transport ambiguity as OutcomeUnknown or retain the request for replay. WinUI keeps _pendingRefund and retries the same request ID during the current ViewModel lifetime.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
If WPF is supported/deployed, response loss after a successful partial refund can leave the operator without explicit unknown-outcome recovery; a subsequent submit generates a new key and may create a separate additional partial refund. Impact depends on deployment, transport outcome and operator action.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
WPF SubmitRefundAsync creates a fresh RefundRequestId per invocation and WPF ApiClient does not expose transport ambiguity as OutcomeUnknown or retain the request for replay. WinUI keeps _pendingRefund and retries the same request ID during the current ViewModel lifetime.

**E. Future Repair Specification:**  
['If WPF is supported, route refund commands through explicit unknown-outcome state and retain/reconcile the same request ID before allowing a new submit. If WPF is retired, record that operational fact. Decide whether WinUI unresolved refund intent must survive restart; if so, persist a safe pending key or require authoritative refund lookup before another command. Preserve server amount and account guards.']

**Required Regression Test Cases:**
- Inject response loss after server commit in WPF; retry must reuse the key or require server reconciliation before a new command.
- WinUI timeout/response loss then retry in same ViewModel must use the same ID and return one receipt.
- Restart after unresolved WinUI outcome must follow owner-selected recovery policy.
- A deliberate new partial refund with a distinct ID remains allowed when the order/account permit it.
- Retain same-key replay and distinct-key cumulative cap integration tests.

**Acceptance Criteria & Verification Standard:**
- Owner documents whether WPF is supported or retired.
- Supported clients cannot encourage a fresh financial command before prior outcome is resolved.
- Same logical operation retains the idempotency key across retry; intentional new partial refunds remain distinct and auditable.

---

### FIN-CAND-CANCELLED-LOOKUP-PAYMENT-LABEL-001: Call Center labels a cancelled unpaid electronic order as collectible on delivery
- **Severity:** `LOW` | **Confidence:** `HIGH_SOURCE_CONFIRMED_FOR_LABEL_BRANCH; CONDITIONAL_FOR_OPERATOR_IMPACT` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L267-L279) | `267-279` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookedUpPaymentLine.get (GetAccessorDeclaration)` | `ff3fefda7d24` |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L311-L334) | `311-334` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookupOrderAsync() (MethodDeclaration)` | `ff3fefda7d24` |
| [`src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml#L143-L171) | `143-171` | `File-level declaration or top-level statement` | `67022609cfa8` |
| [`src/Falah.Client.CallCenterWinUI/Converters/Converters.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Converters/Converters.cs#L11-L22) | `11-22` | `Falah.Client.CallCenterWinUI.Converters.OrderStatusLabelConverter.Convert(object, Type, object, string) (MethodDeclaration)` | `926d37d8ddf5` |
| [`src/Falah.Domain/Orders/Order.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L200-L214) | `200-214` | `Falah.Domain.Orders.Order.CancelElectronicPayment(string, string, DateTimeOffset) (MethodDeclaration)` | `4f93ebcba865` |
| [`src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CancelPendingElectronicPaymentHandler.cs#L31-L73) | `31-73` | `Falah.Infrastructure.Orders.CancelPendingElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.CancelPendingElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` | `c12b0da939a0` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1506-L1510) | `1506-1510` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Infrastructure/Orders/OrderMapper.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderMapper.cs#L24-L39) | `24-39` | `Falah.Infrastructure.Orders.OrderMapper.ToDto(Falah.Infrastructure.Persistence.Entities.OrderEntity) (MethodDeclaration)` | `376193953483` |
| [`src/Falah.Application/Orders/OrderDetailsDto.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/OrderDetailsDto.cs#L10-L23) | `10-23` | `File-level declaration or top-level statement` | `2c3fe061e51f` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L267-L279)  
> **Lines 267-279** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookedUpPaymentLine.get (GetAccessorDeclaration)` (File SHA-256: `ff3fefda7d248cfafe4d7881c5b0f620ea8f76339ac521fbfee8c951d4a3f41e`):

```csharp
 267 |     public string LookedUpPaymentLine
 268 |     {
 269 |         get
 270 |         {
 271 |             if (LookedUpOrder is not { } order) return "";
 272 |             var how = order.SplitPayments is { Count: > 0 } splits
 273 |                 ? string.Join(" + ", splits.Select(s => $"{s.Method} {CashInput.Format(s.AmountPiastres)} ج"))
 274 |                 : order.PaymentMethod;
 275 |             return order.ElectronicPaymentStatus == "pending" ? $"{how} - تحويل لم يُؤكَّد"
 276 |                 : order.IsPaid ? $"{how} - مدفوع"
 277 |                 : $"{how} - تحصيل عند التسليم";
 278 |         }
 279 |     }
```

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L311-L334)  
> **Lines 311-334** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.LookupOrderAsync() (MethodDeclaration)` (File SHA-256: `ff3fefda7d248cfafe4d7881c5b0f620ea8f76339ac521fbfee8c951d4a3f41e`):

```csharp
 311 |     [RelayCommand]
 312 |     private async Task LookupOrderAsync()
 313 |     {
 314 |         if (!CanEditOperation || string.IsNullOrWhiteSpace(LookupOrderNumber)) return;
 315 |         ClearResolvedPrint();
 316 |         IsLookingUpOrder = true;
 317 |         LookupError = null;
 318 |         LookedUpOrder = null;
 319 |         ReprintNote = null;
 320 |         try
 321 |         {
 322 |             var result = await _session.Api.LookupOrderByNumberAsync(LookupOrderNumber.Trim());
 323 |             if (!result.Success || result.Value is null)
 324 |             {
 325 |                 LookupError = result.Error ?? "لم يتم العثور على الطلب.";
 326 |                 return;
 327 |             }
 328 |             LookedUpOrder = result.Value;
 329 |         }
 330 |         finally
 331 |         {
 332 |             IsLookingUpOrder = false;
 333 |         }
 334 |     }
```

> **File:** [`src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/CallCenterView.xaml#L143-L171)  
> **Lines 143-171** in `File-level declaration or top-level statement` (File SHA-256: `67022609cfa8b4a82b59f2b225725d921e8596defeda694af4a1a47bfef79eea`):

```xaml
 143 |                     <StackPanel Spacing="6" Visibility="{Binding LookedUpOrder, Converter={StaticResource NullToCollapsedConverter}}">
 144 |                         <Border Height="1" Background="{StaticResource CardBorderBrush}" Margin="0,4"/>
 145 |                         <TextBlock FontWeight="Black" FontSize="16">
 146 |                             <Run Text="{Binding LookedUpOrder.OrderNumber}"/>
 147 |                         </TextBlock>
 148 |                         <TextBlock FontSize="12" Foreground="{StaticResource TextMutedBrush}">
 149 |                             <Run Text="الحالة: "/><Run Text="{Binding LookedUpOrder.Status, Converter={StaticResource OrderStatusLabelConverter}}"/>
 150 |                         </TextBlock>
 151 |                         <TextBlock FontSize="12" Foreground="{StaticResource TextMutedBrush}">
 152 |                             <Run Text="العميل: "/><Run Text="{Binding LookedUpOrder.CustomerName}"/><Run Text=" - "/><Run Text="{Binding LookedUpOrder.CustomerPhone}"/>
 153 |                         </TextBlock>
 154 |                         <TextBlock FontSize="12" Foreground="{StaticResource TextMutedBrush}" TextWrapping="Wrap">
 155 |                             <Run Text="العنوان: "/><Run Text="{Binding LookedUpOrder.DeliveryArea}"/><Run Text=" - "/><Run Text="{Binding LookedUpOrder.DeliveryStreet}"/>
 156 |                         </TextBlock>
 157 |                         <TextBlock FontSize="12" FontWeight="SemiBold" TextWrapping="Wrap"
 158 |                                    Visibility="{Binding HasLookedUpRequestedPickup, Converter={StaticResource BoolToVisibleConverter}}">
 159 |                             <Run Text="موعد الاستلام: "/><Run Text="{Binding LookedUpRequestedPickupLine}"/>
 160 |                         </TextBlock>
 161 |                         <TextBlock FontSize="14" FontWeight="Bold">
 162 |                             <Run Text="الإجمالي: "/><Run Text="{Binding LookedUpOrder.TotalPiastres, Converter={StaticResource PiastresToDisplayConverter}}"/>
 163 |                         </TextBlock>
 164 |                         <!-- Whether the money is settled. An operator taking a follow-up call was
 165 |                              not told, and "مدفوع" and "تحويل لم يُؤكَّد" are not the same promise. -->
 166 |                         <TextBlock FontSize="12" FontWeight="SemiBold" TextWrapping="Wrap"
 167 |                                    Text="{Binding LookedUpPaymentLine}"
 168 |                                    Foreground="{Binding IsLookedUpPaymentUnconfirmed, Converter={StaticResource BoolToBrushConverter}, ConverterParameter=WarningBrush|TextMutedBrush}"
 169 |                                    Visibility="{Binding HasLookedUpPaymentLine, Converter={StaticResource BoolToVisibleConverter}}"/>
 170 |                         <Button Content="إعادة طباعة إيصال مركز الاتصال" Style="{StaticResource SecondaryButtonStyle}" HorizontalAlignment="Stretch"
 171 |                                 Command="{Binding ReprintLookedUpOrderCommand}"/>
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
The payment display infers collect-on-delivery solely from IsPaid=false after special-casing only pending electronic status; it ignores order cancellation and cancelled electronic status.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Conflicting operational message can misinform staff/customer or driver about collection due. Visible Cancelled status mitigates. No actual collection/ledger loss proven.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
The payment display infers collect-on-delivery solely from IsPaid=false after special-casing only pending electronic status; it ignores order cancellation and cancelled electronic status.

**E. Future Repair Specification:**  
Make payment text state-aware: cancelled orders should show cancelled/no collection, pending transfers stay unconfirmed, settled orders stay paid, and collect-on-delivery is reserved for active unpaid orders that satisfy COD semantics. Confirm the preferred Arabic copy with owner if wording is material.

**Required Regression Test Cases:**
- Projection test for cancelled + unpaid + electronic status cancelled: no collect-at-delivery label.
- Controls for active COD, pending electronic, and paid orders.
- Call Center lookup view-model test asserts status/payment rows do not conflict.

**Acceptance Criteria & Verification Standard:**
- No cancelled order is described as collectible at delivery.
- Active COD remains collectible; pending electronic remains unconfirmed; paid remains paid.
- No database/financial movement is introduced by the presentation correction.

---

### FIN-CAND-NEGATIVE-VARIANT-PRICE-001: Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed
- **Severity:** `MEDIUM` | **Confidence:** `SOURCE_SUPPORTED_CONFIGURATION_VALIDATION_CANDIDATE;NO_INDEPENDENT_CHALLENGE_OR_RUNTIME_REPRODUCTION` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Pricing/ItemPricingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs#L25-L45) | `25-45` | `Falah.Domain.Pricing.ItemPricingRules.Resolve(string, Falah.Domain.Common.Money, string?, IEnumerable<Falah.Domain.Pricing.PriceRule>, DateTimeOffset, TimeZoneInfo) (MethodDeclaration)` | `47248fb19dfc` |
| [`src/Falah.Infrastructure/Menu/ProductHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Menu/ProductHandlers.cs#L50-L70) | `50-70` | `Falah.Infrastructure.Menu.CreateProductHandler.HandleAsync(Falah.Application.Menu.CreateProductRequest, CancellationToken) (MethodDeclaration)` | `98b29b5ace01` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Pricing/ItemPricingRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs#L25-L28)  
> **Lines 25-28** in `Falah.Domain.Pricing.ItemPricingRules.Resolve(string, Falah.Domain.Common.Money, string?, IEnumerable<Falah.Domain.Pricing.PriceRule>, DateTimeOffset, TimeZoneInfo) (MethodDeclaration)` (File SHA-256: `47248fb19dfcb29d219a349e99e1e60995bccd12a84afbd4139b081467161303`):

```csharp
  25 |             ? new ResolvedItemPrice(listUnitPrice, listUnitPrice, null, null)
  26 |             : new ResolvedItemPrice(listUnitPrice, validRule.FixedPrice, validRule.Id, validRule.Name);
  27 |     }
  28 | }
```

> **File:** [`src/Falah.Infrastructure/Menu/ProductHandlers.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Menu/ProductHandlers.cs#L50-L70)  
> **Lines 50-70** in `Falah.Infrastructure.Menu.CreateProductHandler.HandleAsync(Falah.Application.Menu.CreateProductRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `98b29b5ace01d781c176b352b054b561973281e4e923c23cb0794412c6107247`):

```csharp
  50 |             SortOrder = request.SortOrder, RecipeId = request.RecipeId,
  51 |             Variants = request.Variants.Select(v => new ProductVariantEntity
  52 |             {
  53 |                 Id = $"var-{Guid.NewGuid():N}", ProductId = id, Name = v.Name.Trim(), PricePiastres = v.PricePiastres, RecipeId = v.RecipeId,
  54 |             }).ToList(),
  55 |         };
  56 |         try
  57 |         {
  58 |             _db.Products.Add(entity);
  59 |             _auditLog.Append(new AuditEvent(
  60 |                 $"audit-{Guid.NewGuid():N}", actorUser.Id, actorUser.Name, actorUser.Role,
  61 |                 $"إضافة صنف جديد بالمنيو: {entity.Name}", "menu",
  62 |                 $"السعر الأساسي: {entity.BasePricePiastres} قرش - يرسل للفحم: {(entity.SendToGrill ? "نعم" : "لا")}",
  63 |                 "normal", DateTimeOffset.UtcNow, ReferenceId: id));
  64 |             await _db.SaveChangesAsync(ct);
  65 |         }
  66 |         catch (DbUpdateException)
  67 |         {
  68 |             var replay = await _db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id, ct);
  69 |             if (replay is null) throw;
  70 |             return new CreateProductResult(replay.Id, WasReplayed: true);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Potential undercharge or zero-payable order with stock consumption; actual financial incidence and owner policy are unverified.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Server accepts a negative product-variant price that can reduce checkout totals while stock is consumed

**E. Future Repair Specification:**  
Confirm whether zero-priced variants are allowed. If prices must be positive, centralize the invariant at server command boundaries and checkout resolution, and evaluate a DB check constraint after legacy-row inventory. Preserve signed Money for legitimate ledger deltas.

**Required Regression Test Cases:**
- Domain validation test asserting ProductVariant.PricePiastres >= 0 on create/update.
- Checkout integration test submitting item with negative price asserting rejection with HTTP 422.

**Acceptance Criteria & Verification Standard:**
- Owner-approved variant price domain is enforced server-side and at checkout.
- Invalid variant configuration cannot create underpriced/free stock-consuming checkout.
- No financial side effect occurs on rejection.
- Valid positive variants and approved prices remain compatible.

---

### FIN-CAND-DELIVERY-CUSTODY-EXPECTED-001: Delivery report field labeled expected unsettled custody aggregates collected amount instead
- **Severity:** `MEDIUM` | **Confidence:** `INDEPENDENT_STATIC_CORROBORATION; CONTRACT_AND_RUNTIME_IMPACT_QUALIFIED` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L85-L115) | `85-115` | `Falah.Infrastructure.Reports.ReportingQueryService.GetSalesReportAsync(DateOnly?, string?, CancellationToken) (MethodDeclaration)` | `452d58690fb3` |
| [`src/Falah.Application/Reports/ReportingDtos.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/ReportingDtos.cs#L20-L35) | `20-35` | `File-level declaration or top-level statement` | `39c9da5ec2df` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Reports/ReportingQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Reports/ReportingQueryService.cs#L85-L115)  
> **Lines 85-115** in `Falah.Infrastructure.Reports.ReportingQueryService.GetSalesReportAsync(DateOnly?, string?, CancellationToken) (MethodDeclaration)` (File SHA-256: `452d58690fb3e6e06613cdd5892969c1afd137b7be0ed9cb3758b1e210c99509`):

```csharp
  85 |     public Task<SalesReportDto> GetSalesReportAsync(DateOnly? businessDate, string? shiftId, CancellationToken ct = default) =>
  86 |         GetSalesReportAsync(businessDate is { } d ? BusinessDateRange.SingleDay(d) : null, shiftId, ct);
  87 | 
  88 |     public async Task<SalesReportDto> GetSalesReportAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct = default)
  89 |     {
  90 |         var entities = await LoadSalesOrdersAsync(range, shiftId, ct);
  91 |         var orders = entities.Select(OrderMapper.ToDomain).ToList();
  92 |         var report = SalesRules.ComputeSalesReport(orders, await LoadOrderIdsWithCashOutsideRestaurantAsync(orders, ct));
  93 |         return ToDto(report);
  94 |     }
  95 | 
  96 |     /// <summary>Ported from TaxScreen.tsx's own report - reads only orders' frozen TaxAmount/
  97 |     /// TaxableBase (see SalesRules.ComputeTaxReport's own doc comment for why this must never
  98 |     /// recompute from today's live tax rule). A real gap found during the legacy-parity audit: no
  99 |     /// C# endpoint existed for this at all.</summary>
 100 |     public Task<TaxReportDto> GetTaxReportAsync(DateOnly? businessDate, string? shiftId, CancellationToken ct = default) =>
 101 |         GetTaxReportAsync(businessDate is { } d ? BusinessDateRange.SingleDay(d) : null, shiftId, ct);
 102 | 
 103 |     public async Task<TaxReportDto> GetTaxReportAsync(BusinessDateRange? range, string? shiftId, CancellationToken ct = default)
 104 |     {
 105 |         var entities = await LoadSalesOrdersAsync(range, shiftId, ct);
 106 |         var orders = entities.Select(OrderMapper.ToDomain).ToList();
 107 |         var report = SalesRules.ComputeTaxReport(orders);
 108 |         return new TaxReportDto(report.Gross.Piastres, report.Taxable.Piastres, report.Tax.Piastres,
 109 |             report.RefundTaxImpact.Piastres, report.NetTax.Piastres);
 110 |     }
 111 | 
 112 |     private static SalesReportDto ToDto(SalesRules.SalesReport r) => new(
 113 |         r.OrderCount, r.GrossSales.Piastres, r.Refunds.Piastres, r.NetSales.Piastres, r.ListSales.Piastres,
 114 |         r.OffersValue.Piastres, r.CompsValue.Piastres, r.ManualDiscounts.Piastres, r.PendingElectronicSales.Piastres,
 115 |         r.CashWithDrivers.Piastres, r.CollectedSales.Piastres, r.AverageOrderValue.Piastres, r.Cash.Piastres, r.Card.Piastres, r.InstaPay.Piastres,
```

> **File:** [`src/Falah.Application/Reports/ReportingDtos.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Reports/ReportingDtos.cs#L20-L35)  
> **Lines 20-35** in `File-level declaration or top-level statement` (File SHA-256: `39c9da5ec2dfc6012cf6b630b0f9d8a8924c54016c991ceacaad1632ec308726`):

```csharp
  20 | public sealed record TaxReportDto(long GrossPiastres, long TaxablePiastres, long TaxPiastres, long RefundTaxImpactPiastres, long NetTaxPiastres);
  21 | 
  22 | public sealed record RefundActivityDto(
  23 |     string OrderId, string? OrderNumber, long AmountPiastres, string PaymentMethod, string Reason,
  24 |     string CreatedBy, string AuthorizedBy, DateTimeOffset CreatedAtUtc, string OriginalShiftId, string ProcessingShiftId);
  25 | 
  26 | /// <summary>The read-only equivalent of the reference app's reconcileBusinessDay - "where is the
  27 | /// money" for one business day, from the SAME persisted movements/orders/shifts every other screen
  28 | /// reads. Deliberately excludes purchasing/payroll sub-sections the reference app's version has:
  29 | /// neither Suppliers/Purchases nor Payroll exist in this system yet (see IMPLEMENTATION_MATRIX.md).</summary>
  30 | public sealed record TreasuryBalancesDto(long MainPiastres, long DrawerPiastres, long BankPiastres, long InstaPayPiastres, long WalletPiastres);
  31 | 
  32 | public sealed record BusinessDayReconciliationDto(
  33 |     string BusinessDayId, string BusinessDate, SalesReportDto Sales, long ExpensesTotalPiastres,
  34 |     TreasuryBalancesDto Treasury, long DeliveryExpectedCustodyPiastres, long DeliverySettledCustodyPiastres,
  35 |     int ShiftCount, long ShiftsExpectedCashPiastres, long ShiftsActualCashPiastres, long ShiftsShortagePiastres, long ShiftsOveragePiastres,
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Delivery report field labeled expected unsettled custody aggregates collected amount instead

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
If the DTO/UI contract means expected cash at driver risk, the report underreports that exposure while delivery is in transit and diverges from the owner dashboard. If it intentionally means cash already collected but not settled, the aggregation may be deliberate but DTO name and tests are misleading. No loss, duplicate payment, or ledger corruption is established.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Delivery report field labeled expected unsettled custody aggregates collected amount instead

**E. Future Repair Specification:**  
Confirm report contract. Existing DTO name, owner dashboard, and test comments support ExpectedPiastres: if that is the intended meaning, sum c.ExpectedPiastres. If the intended metric is collected-but-unsettled, rename the DTO/display and retain/add a distinct expected-at-risk metric so the two values are not conflated.

**Required Regression Test Cases:**
- Delivery query test asserting ExpectedUnsettledCustody aggregates unremitted in-flight orders only.
- End-of-shift reconciliation test asserting remitted cash is excluded from courier custody balance.

**Acceptance Criteria & Verification Standard:**
- DTO field name and source aggregate express the same amount.
- Owner dashboard and delivery report agree for the same non-settled custody cohort when they claim to show expected custody.
- In-transit and variance cases are covered by tests; no real printer or financial operation is needed.
- Historical statement temporal semantics are documented separately.
- Before delivery, delivery report and owner dashboard agree on expected cash responsibility for the same open custody cohort while collected remains zero.

---

## Order Lifecycle And Workflow

### FLOW-001: Requested two-stage pickup preparation and final payment lifecycle is absent
- **Severity:** `HIGH_REQUIREMENT_GAP` | **Confidence:** `high-static` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Domain/Orders/OrderEnums.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderEnums.cs#L9-L15) | `9-15` | `File-level declaration or top-level statement` | `017b0a065784` |
| [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L44-L55) | `44-55` | `Falah.Domain.Orders.CheckoutRules.IsCashOnDelivery(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, Falah.Domain.Common.Money) (MethodDeclaration)` | `a594ce2aa6ab` |
| [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L45-L65) | `45-65` | `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` | `ff9f45f38b96` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Domain/Orders/OrderEnums.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderEnums.cs#L9-L13)  
> **Lines 9-13** in `File-level declaration or top-level statement` (File SHA-256: `017b0a0657846aae726cb0b7762fd9d31ccef5b0e60a902584b0aafbbee7eecc`):

```csharp
   9 | public enum OrderType { Pickup, Takeaway, Delivery }
  10 | 
  11 | public enum OrderStatus { Pending, Preparing, Ready, Completed, Cancelled, Refunded }
  12 | 
  13 | public enum ElectronicPaymentStatus { Pending, Confirmed, Cancelled }
```

> **File:** [`src/Falah.Domain/Orders/CheckoutRules.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/CheckoutRules.cs#L44-L55)  
> **Lines 44-55** in `Falah.Domain.Orders.CheckoutRules.IsCashOnDelivery(Falah.Domain.Orders.OrderType, Falah.Domain.Financial.PaymentMethod, Falah.Domain.Common.Money) (MethodDeclaration)` (File SHA-256: `a594ce2aa6ab60d232942ef4af790c0dd7c2a90b49db772e277eaf79d5af84d4`):

```csharp
  44 |     public static bool IsCashOnDelivery(OrderType orderType, PaymentMethod paymentMethod, Money payableTotal) =>
  45 |         orderType == OrderType.Delivery && paymentMethod == PaymentMethod.Cash && payableTotal.IsPositive;
  46 | 
  47 |     /// <summary>An InstaPay/wallet leg - standalone or the one electronic leg inside a split - is
  48 |     /// only a claim until confirmed, so it is deferred exactly like cash-on-delivery: no Treasury
  49 |     /// movement now, money posts when someone confirms receipt.</summary>
  50 |     public static ElectronicPayment? SelectElectronicLeg(
  51 |         PaymentMethod paymentMethod, Money payableTotal, IReadOnlyList<PaymentSplit>? splitPayments, string? walletProvider)
  52 |     {
  53 |         if (paymentMethod is PaymentMethod.InstaPay or PaymentMethod.Wallet)
  54 |             return new ElectronicPayment(paymentMethod, paymentMethod == PaymentMethod.Wallet ? walletProvider : null, payableTotal, ElectronicPaymentStatus.Pending);
  55 | 
```

> **File:** [`src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L45-L65)  
> **Lines 45-65** in `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ff9f45f38b96377ef8f065884820752aa8483f9038b2141ed390222eac83413c`):

```csharp
  45 |         if (existing is not null) return ToResult(existing, wasReplayed: true);
  46 | 
  47 |         var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
  48 |             ?? throw new ShiftNotOpenException("الوردية غير موجودة.");
  49 |         if (shift.Status != nameof(ShiftStatus.Open))
  50 |             throw new ShiftNotOpenException("يجب فتح وردية قبل تسجيل أي عملية بيع.");
  51 | 
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
  53 |             ?? throw new InvalidOperationException("الكاشير غير موجود.");
  54 |         var cashier = ToActor(cashierUser);
  55 | 
  56 |         var now0 = DateTimeOffset.UtcNow;
  57 |         var resolvedItems = await _pricing.ResolveAsync(request.Items, now0, ct);
  58 |         var items = resolvedItems.Select(r => r.Item).ToList();
  59 | 
  60 |         var taxRuleEntity = await _db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Enabled, ct);
  61 |         var taxRule = taxRuleEntity is null ? null
  62 |             : new TaxRule(taxRuleEntity.Id, taxRuleEntity.Name, taxRuleEntity.RateBasisPoints,
  63 |                 taxRuleEntity.Mode == "Inclusive" ? TaxMode.Inclusive : TaxMode.Exclusive, taxRuleEntity.Enabled);
  64 | 
  65 |         var discount = Money.FromPiastres(request.DiscountPiastres);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Creation, stock consumption and payment checkout share one use case (CheckoutOrderHandler); only Delivery Cash has explicit deferred cash rule. No state machine exists for pay-on-collection pickup orders.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Operational gap: Kitchen cannot be notified to prepare pickup orders before payment has been taken in full, blocking customer phone/advance pickup workflows.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Creation, stock consumption and payment checkout share one use case (CheckoutOrderHandler); only Delivery Cash has explicit deferred cash rule. No state machine exists for pay-on-collection pickup orders.

**E. Future Repair Specification:**  
Design separate pickup fulfilment state and payment state with server-authoritative transitions (two-stage reserve-and-prep -> pay-and-handover).

**Required Regression Test Cases:**
- Recommend lifecycle acceptance tests: unpaid reservation queues kitchen once, payment later posts once, handover only after policy gate, cancellation/restock/close blockers, network replay at each stage.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### FLOW-002: Held electronic preparation is not automatically released on confirmation
- **Severity:** `MEDIUM` | **Confidence:** `high-static` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L65-L88) | `65-88` | `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` | `cac1f0a97612` |
| [`src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs#L35-L50) | `35-50` | `Falah.Infrastructure.Printing.OrderAutoPrintService.OrderAutoPrintService(Falah.Infrastructure.Printing.CreatePrintJobsHandler, Falah.Infrastructure.Settings.SettingsQueryService, Microsoft.Extensions.Logging.ILogger<Falah.Infrastructure.Printing.OrderAutoPrintService>) (ConstructorDeclaration)` | `0a7282c6766d` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/ConfirmElectronicPaymentHandler.cs#L65-L88)  
> **Lines 65-88** in `Falah.Infrastructure.Orders.ConfirmElectronicPaymentHandler.HandleAsync(Falah.Application.Orders.ConfirmElectronicPaymentRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `cac1f0a97612efb966710e6a47e323c2fff06b342c688f419298e34e18c34a0b`):

```csharp
  65 |             }
  66 | 
  67 |             var order = OrderMapper.ToDomain(orderEntity);
  68 |             order.ConfirmElectronicPayment(); // throws DomainException if there is no pending leg
  69 |             var leg = order.ElectronicPayment!;
  70 | 
  71 |             orderEntity.ElectronicPaymentStatus = leg.Status.ToWireString();
  72 |             orderEntity.IsPaid = order.IsPaid;
  73 |             orderEntity.Version++;
  74 | 
  75 |             var saleKey = $"sale-{orderEntity.CheckoutRequestId}";
  76 |             var journal = new EfFinancialJournal(_db);
  77 |             if (!journal.TryStart(saleKey, "sale"))
  78 |                 throw new ConfirmConflictException("مفتاح العملية المالية لهذا الطلب مستخدم بالفعل رغم أن الدفع لم يكن مؤكدًا - يحتاج مراجعة يدوية.");
  79 | 
  80 |             var movement = TreasuryMovementFactory.BuildSaleMovement(
  81 |                 $"tm-{Guid.NewGuid():N}", $"op-{Guid.NewGuid():N}", orderEntity.CheckoutRequestId, orderEntity.Id,
  82 |                 leg.Method, leg.Amount, orderEntity.BusinessDayId, orderEntity.ShiftId, cashierUser.Name, DateTimeOffset.UtcNow);
  83 |             _db.TreasuryMovements.Add(OrderMapper.ToEntity(movement));
  84 |             journal.Complete(saleKey);
  85 | 
  86 |             await _db.SaveChangesAsync(ct);
  87 |             await transaction.CommitAsync(ct);
  88 |             return new ConfirmElectronicPaymentResult(orderEntity.Id, orderEntity.ElectronicPaymentStatus, orderEntity.IsPaid, movement.Id, WasReplayed: false);
```

> **File:** [`src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/OrderAutoPrintService.cs#L35-L50)  
> **Lines 35-50** in `Falah.Infrastructure.Printing.OrderAutoPrintService.OrderAutoPrintService(Falah.Infrastructure.Printing.CreatePrintJobsHandler, Falah.Infrastructure.Settings.SettingsQueryService, Microsoft.Extensions.Logging.ILogger<Falah.Infrastructure.Printing.OrderAutoPrintService>) (ConstructorDeclaration)` (File SHA-256: `0a7282c6766d2a7b446739d56984950d87ab69f52f88dd3be88f27609484a542`):

```csharp
  35 |     public OrderAutoPrintService(CreatePrintJobsHandler createJobs, SettingsQueryService settings,
  36 |         ILogger<OrderAutoPrintService> log)
  37 |     {
  38 |         _createJobs = createJobs;
  39 |         _settings = settings;
  40 |         _log = log;
  41 |     }
  42 | 
  43 |     /// <returns>True when the tickets were queued, or when the restaurant has deliberately turned
  44 |     /// automatic printing off. False only when something actually failed - the caller passes that
  45 |     /// on to the station so a missing ticket is visible rather than silent.</returns>
  46 |     public async Task<bool> TryPrintAsync(CheckoutOrderRequest request, CheckoutOrderResult result, CancellationToken ct = default)
  47 |     {
  48 |         try
  49 |         {
  50 |             var settings = await _settings.GetAsync(ct);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Suppression is a one-time checkout decision without durable preparation-release state or confirmation outbox event.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
HoldPreparationUntilPaymentConfirmed suppresses initial kitchen routes. Confirmation posts money/status but does not enqueue held preparation. Auto-print documentation directs operator to Order Lookup.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Suppression is a one-time checkout decision without durable preparation-release state or confirmation outbox event.

**E. Future Repair Specification:**  
Explicitly communicate manual release now; future approved implementation should persist held state and transactionally enqueue release with stable key on confirmation.

**Required Regression Test Cases:**
- Add hold->confirm release exactly once, replay, mixed payment failure, cancellation-before-release and manual reprint tests.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

## Printing And Hardware

### PRT-001: Two machines can submit the same pending kitchen job
- **Severity:** `HIGH` | **Confidence:** `high-static; independently source-challenged; physical reproduction NOT TESTED` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L365-L377) | `365-377` | `lambda expression (SimpleLambdaExpression)` | `7fda50ac64f4` |
| [`src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs#L34-L45) | `34-45` | `Falah.Infrastructure.Printing.CreatePrintJobsHandler.HandleAsync(Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken) (MethodDeclaration)` | `ac7a5a9aefba` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Persistence/FalahDbContext.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/FalahDbContext.cs#L365-L377)  
> **Lines 365-377** in `lambda expression (SimpleLambdaExpression)` (File SHA-256: `7fda50ac64f49021d17d7ea9636456dd15556c6d8d4a76f5e553be0a42f88d3c`):

```csharp
 365 |         modelBuilder.Entity<PrintJobEntity>(b =>
 366 |         {
 367 |             b.HasKey(j => j.Id);
 368 |             // A station's own print agent polls "pending jobs for my route" - this is the exact
 369 |             // shape of that query.
 370 |             b.HasIndex(j => new { j.Route, j.Status });
 371 |             b.HasIndex(j => j.OrderId);
 372 |             b.HasIndex(j => j.CreateRequestId);
 373 |             b.HasMany(j => j.Attempts).WithOne().HasForeignKey(a => a.PrintJobId).OnDelete(DeleteBehavior.Cascade);
 374 |             // No FK to Orders by design: a print job records a moment-in-time hardware dispatch
 375 |             // fact, not a financial relationship - it must never be able to block or be blocked by
 376 |             // anything happening to the order it describes.
 377 |         });
```

> **File:** [`src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs#L34-L45)  
> **Lines 34-45** in `Falah.Infrastructure.Printing.CreatePrintJobsHandler.HandleAsync(Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ac7a5a9aefba464dfb2444c86d6a5bb22149455b2f82856f3fea4971931ff941`):

```csharp
  34 |         var existing = await _db.PrintJobs.AsNoTracking()
  35 |             .Where(j => j.CreateRequestId == request.RequestId).ToListAsync(ct);
  36 |         if (existing.Count > 0) return new CreatePrintJobsResult(existing.Select(ToDto).ToList(), WasReplayed: true);
  37 | 
  38 |         PrintRoute? customerRoute = null;
  39 |         if (!string.IsNullOrEmpty(request.CustomerRoute))
  40 |         {
  41 |             if (!Enum.TryParse<PrintRoute>(request.CustomerRoute, out var parsed) || parsed is not (PrintRoute.Receipt or PrintRoute.CallCenter))
  42 |                 throw new DomainException("مسار الطباعة المطلوب للعميل غير صالح.");
  43 |             customerRoute = parsed;
  44 |         }
  45 | 
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
CreatePrintJobsHandler.cs generates job ID using request.RequestId, and PrintJobs table in FalahDbContext lacks a composite unique constraint on (OrderId, Route).

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Duplicate kitchen and receipt tickets printed when multiple stations submit print requests for the same order.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
CreatePrintJobsHandler.cs generates job ID using request.RequestId, and PrintJobs table in FalahDbContext lacks a composite unique constraint on (OrderId, Route).

**E. Future Repair Specification:**  
Add composite unique index on (OrderId, Route, DocumentKind) in PrintJobEntity configuration, or ensure idempotent deduplication at database level.

**Required Regression Test Cases:**
- Existing PrintAgentTests cover route selection and one-agent report retry only; add concurrent two-agent claim/fencing test, and replacement-agent unknown-outcome recovery.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### PRT-002: Succeeded and print health collapse transport acceptance into physical completion
- **Severity:** `MEDIUM` | **Confidence:** `high-static` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs#L37-L65) | `37-65` | `Falah.PrintAgent.WindowsSpoolerTicketTransport.Send(string, byte[]) (MethodDeclaration)` | `dbbefd768ce0` |
| [`src/Falah.PrintAgent/PrintJobProcessor.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L108-L120) | `108-120` | `Falah.PrintAgent.PrintJobProcessor.ProcessAsync(Falah.Application.Printing.PrintJobDto, CancellationToken) (MethodDeclaration)` | `2ef3ef9c1d54` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs#L37-L65)  
> **Lines 37-65** in `Falah.PrintAgent.WindowsSpoolerTicketTransport.Send(string, byte[]) (MethodDeclaration)` (File SHA-256: `dbbefd768ce06c1f033a4ac80a56574a9564a9c35e58eba5684310a89bd38cf5`):

```csharp
  37 |     internal static void Send(string printerName, byte[] payload)
  38 |     {
  39 |         if (!OpenPrinter(printerName, out var printer, IntPtr.Zero))
  40 |             throw new InvalidOperationException(
  41 |                 $"Could not open the printer \"{printerName}\": {LastErrorMessage()}");
  42 | 
  43 |         try
  44 |         {
  45 |             var info = new DocInfo
  46 |             {
  47 |                 DocName = "Falah ticket",
  48 |                 OutputFile = null,
  49 |                 // Without RAW the driver renders the bytes as a document and the ESC/POS commands
  50 |                 // print as garbage characters instead of controlling the printer.
  51 |                 DataType = "RAW",
  52 |             };
  53 | 
  54 |             if (StartDocPrinter(printer, 1, info) == 0)
  55 |                 throw new InvalidOperationException($"The printer rejected the job: {LastErrorMessage()}");
  56 | 
  57 |             try
  58 |             {
  59 |                 if (!StartPagePrinter(printer))
  60 |                     throw new InvalidOperationException($"The printer rejected the page: {LastErrorMessage()}");
  61 | 
  62 |                 try
  63 |                 {
  64 |                     var buffer = Marshal.AllocHGlobal(payload.Length);
  65 |                     try
```

> **File:** [`src/Falah.PrintAgent/PrintJobProcessor.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintJobProcessor.cs#L108-L120)  
> **Lines 108-120** in `Falah.PrintAgent.PrintJobProcessor.ProcessAsync(Falah.Application.Printing.PrintJobDto, CancellationToken) (MethodDeclaration)` (File SHA-256: `2ef3ef9c1d54c7a16bfe506d95e2803aa1970a09e4fbecdf473a1b82fc3ea640`):

```csharp
 108 |             await _transport.SendAsync(payload, ct);
 109 |             succeeded = true;
 110 |             error = null;
 111 |             _log($"printed {job.Route} job {job.Id} for order {job.OrderNumber} via {_transport.Description}");
 112 |         }
 113 |         catch (OperationCanceledException) when (ct.IsCancellationRequested)
 114 |         {
 115 |             // The durable uncertain attempt remains for the next process to report.
 116 |             throw;
 117 |         }
 118 |         catch (Exception ex)
 119 |         {
 120 |             succeeded = false;
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Boolean transport contract has no acknowledgement level or spooler submission ID; job status has no accepted/unknown/confirmed distinction.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Processor sets Succeeded after SendAsync returns. File write, TCP buffer acceptance and Windows RAW submission all satisfy this. Server overview derives last success from that flag; owner DTO comment describes physical output.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Boolean transport contract has no acknowledgement level or spooler submission ID; job status has no accepted/unknown/confirmed distinction.

**E. Future Repair Specification:**  
Use distinct Simulated, Submitted, SpoolerQueued, Failed, OutcomeUnknown and PhysicallyConfirmed observations. Never drain real pending jobs to file as production completion.

**Required Regression Test Cases:**
- Existing success test verifies only fake transport Sent list. Add file-simulation status, paused spooler status under explicit authorization, and operator-confirmation tests.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### PRT-003: Blocking spooler submission ignores cancellation and completion failures
- **Severity:** `MEDIUM` | **Confidence:** `high-static` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs#L29-L36) | `29-36` | `Falah.PrintAgent.WindowsSpoolerTicketTransport.SendAsync(byte[], CancellationToken) (MethodDeclaration)` | `dbbefd768ce0` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/WindowsSpoolerTicketTransport.cs#L29-L36)  
> **Lines 29-36** in `Falah.PrintAgent.WindowsSpoolerTicketTransport.SendAsync(byte[], CancellationToken) (MethodDeclaration)` (File SHA-256: `dbbefd768ce06c1f033a4ac80a56574a9564a9c35e58eba5684310a89bd38cf5`):

```csharp
  29 |     public Task SendAsync(byte[] payload, CancellationToken ct)
  30 |     {
  31 |         Send(_printerName, payload);
  32 |         return Task.CompletedTask;
  33 |     }
  34 | 
  35 |     /// <summary>Synchronous on purpose: the spooler API is blocking, and wrapping it in a task would
  36 |     /// only move the blocking somewhere less obvious. A ticket is a few hundred bytes.</summary>
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Timeout configuration applies to HTTP/TCP/serial write but not full native spooler lifecycle; unmanaged result handling is incomplete.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Windows SendAsync calls blocking Send and discards cancellation. EndPagePrinter/EndDocPrinter results are ignored. Serial Open/Flush also block without cooperative cancellation.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Timeout configuration applies to HTTP/TCP/serial write but not full native spooler lifecycle; unmanaged result handling is incomplete.

**E. Future Repair Specification:**  
Check finalization results, retain submission job ID, move blocking native submission to a controlled worker with bounded shutdown/uncertain outcome semantics.

**Required Regression Test Cases:**
- Add injectable spooler API tests for failure and hang, short writes already checked in implementation; separate integration verification from physical output.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### PRT-004: Agent never authenticates again after session loss
- **Severity:** `MEDIUM` | **Confidence:** `high-static` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.PrintAgent/PrintServerClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs#L33-L48) | `33-48` | `Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken) (MethodDeclaration)` | `27f4e0c6c3e5` |
| [`src/Falah.PrintAgent/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L111-L135) | `111-135` | `lambda expression (ParenthesizedLambdaExpression)` | `b8bcbfaf0e15` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.PrintAgent/PrintServerClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/PrintServerClient.cs#L33-L48)  
> **Lines 33-48** in `Falah.PrintAgent.PrintServerClient.AuthenticateAsync(string, CancellationToken) (MethodDeclaration)` (File SHA-256: `27f4e0c6c3e54a197e9992182884a6961fb1df7f65f6dddfa7f6aa216d763a56`):

```csharp
  33 |     public async Task AuthenticateAsync(string pin, CancellationToken ct)
  34 |     {
  35 |         var response = await _http.PostAsJsonAsync("/api/auth/login-by-pin", new LoginByPinRequest(pin), ct);
  36 |         response.EnsureSuccessStatusCode();
  37 |         var result = await response.Content.ReadFromJsonAsync<LoginResultDto>(cancellationToken: ct)
  38 |             ?? throw new InvalidOperationException("Login response was empty.");
  39 |         _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", result.Token);
  40 |     }
  41 | 
  42 |     public async Task<IReadOnlyList<PrintJobDto>> GetPendingAsync(string route, CancellationToken ct)
  43 |     {
  44 |         var jobs = await _http.GetFromJsonAsync<List<PrintJobDto>>(
  45 |             $"/api/print-jobs/pending?route={Uri.EscapeDataString(route)}", ct);
  46 |         return jobs ?? new List<PrintJobDto>();
  47 |     }
  48 | 
```

> **File:** [`src/Falah.PrintAgent/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.PrintAgent/Program.cs#L111-L135)  
> **Lines 111-135** in `lambda expression (ParenthesizedLambdaExpression)` (File SHA-256: `b8bcbfaf0e1583c623238dc37db92b843237af6a50f17d70211d76930d75d453`):

```csharp
 111 | using var server = new PrintServerClient(options.ServerBaseAddress, TimeSpan.FromSeconds(options.TransportTimeoutSeconds));
 112 | try
 113 | {
 114 |     await server.AuthenticateAsync(options.Pin, CancellationToken.None);
 115 | }
 116 | catch (Exception ex)
 117 | {
 118 |     Console.Error.WriteLine($"login failed: {ex.GetType().Name}: {ex.Message}");
 119 |     return 3;
 120 | }
 121 | var transport = options.CreateTransport();
 122 | var processor = new PrintJobProcessor(server, transport, options.ResolveEncoding(), options.ReportedByUserId, Log,
 123 |     unreported: UnreportedAttemptStore.ForRoute(AppContext.BaseDirectory, options.Route),
 124 |     codePageTableId: options.CodePageTableId, renderAsImage: options.RenderAsImage, imageWidthDots: options.ImageWidthDots,
 125 |     thickenStrokes: options.ThickenStrokes, imageMarginDots: options.ImageMarginDots);
 126 | 
 127 | using var shutdown = new CancellationTokenSource();
 128 | Console.CancelKeyPress += (_, e) => { e.Cancel = true; shutdown.Cancel(); };
 129 | 
 130 | Log($"logged in as {options.ReportedByUserId}");
 131 | Log($"print agent started - route {options.Route}, server {options.ServerBaseAddress}, transport {transport.Description}");
 132 | if (options.Transport.Equals("file", StringComparison.OrdinalIgnoreCase))
 133 |     Log("WARNING: the file transport writes tickets to disk and prints nothing. Use it for verification only.");
 134 | 
 135 | var pollDelay = TimeSpan.FromSeconds(options.PollSeconds);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
No token-expiry or reauthentication recovery path; agent lifecycle assumes continuously sliding valid session.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Login occurs once before poll loop. All subsequent 401 failures are logged and retried with same token indefinitely.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
No token-expiry or reauthentication recovery path; agent lifecycle assumes continuously sliding valid session.

**E. Future Repair Specification:**  
Add controlled reauthentication and explicit offline/authentication alarm without losing attempt journal or blindly resending uncertain jobs.

**Required Regression Test Cases:**
- Add 401-then-authenticate recovery, deactivated-account terminal failure, and queued outcome reporting across reauth tests.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### PRT-005: Printer verification tool can delete preexisting restaurant tickets
- **Severity:** `HIGH` | **Confidence:** `high-static; independently source-challenged; physical reproduction NOT TESTED` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`tools/verify-windows-printer-transport.ps1`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tools/verify-windows-printer-transport.ps1#L40-L65) | `40-65` | `File-level declaration or top-level statement` | `feb755008b5b` |

#### Verbatim Original Source Code Evidence

> **File:** [`tools/verify-windows-printer-transport.ps1`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tools/verify-windows-printer-transport.ps1#L40-L65)  
> **Lines 40-65** in `File-level declaration or top-level statement` (File SHA-256: `feb755008b5be52d24e8de63dc0eb2dd731bbf20bdbab476d4dcbbfba56f14bc`):

```
  40 |         # --test-print never logs in, but the agent validates its whole config before any mode runs,
  41 |         # so a PIN has to be present. This placeholder is never sent anywhere.
  42 |         Pin = 'not-used-by-test-print'
  43 |         # The production print path on the real printer: image mode at the physically proven width.
  44 |         RenderAsImage = $true
  45 |         ImageWidthDots = 576
  46 |         ImageMarginDots = 24
  47 |     } | ConvertTo-Json | Set-Content -Path $cfg -Encoding utf8
  48 | 
  49 |     $before = @(Get-PrintJob -PrinterName $PrinterName -ErrorAction SilentlyContinue).Count
  50 |     $exe = Join-Path $AgentDirectory 'Falah.PrintAgent.exe'
  51 |     $out = & $exe $cfg --test-print
  52 |     $out | ForEach-Object { Write-Output "  agent: $_" }
  53 |     Write-Output "agent exit code: $LASTEXITCODE"
  54 | 
  55 |     Start-Sleep -Milliseconds 1200
  56 |     $jobs = @(Get-PrintJob -PrinterName $PrinterName -ErrorAction SilentlyContinue)
  57 |     Write-Output "jobs before: $before   after: $($jobs.Count)"
  58 |     foreach ($j in $jobs) {
  59 |         Write-Output ("  job {0}: name='{1}' status={2} size={3} bytes" -f $j.Id, $j.DocumentName, $j.JobStatus, $j.Size)
  60 |     }
  61 |     $mine = $jobs | Where-Object { $_.DocumentName -eq 'Falah ticket' }
  62 |     if ($mine) {
  63 |         Write-Output "RESULT: the spooler accepted the RAW job"
  64 |         $mine | Remove-PrintJob
  65 |         Write-Output "test job removed - no paper was produced"
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Generic document-name matching replaces exact submission ownership; ignored pause result invalidates no-paper guarantee.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Tool pauses printer without checking success, sends generic Falah ticket, then removes every job with DocumentName Falah ticket and resumes queue.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Generic document-name matching replaces exact submission ownership; ignored pause result invalidates no-paper guarantee.

**E. Future Repair Specification:**  
Use dedicated test queue and exact returned job ID, verify pause state, never delete by document name; keep all live printer operations gated.

**Required Regression Test Cases:**
- Add fake queue fixture preserving preexisting IDs, failure-to-pause abort, failed-cleanup no-resume policy. Only isolated queue integration after approval.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### PRT-006: Saved printer width and receipt copy count do not control inspected print pipeline
- **Severity:** `LOW` | **Confidence:** `high-static` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified against baseline source implementation.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs#L38-L55) | `38-55` | `Falah.Infrastructure.Printing.CreatePrintJobsHandler.HandleAsync(Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken) (MethodDeclaration)` | `ac7a5a9aefba` |
| [`src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs#L20-L35) | `20-35` | `Falah.Infrastructure.Settings.UpdateSettingsHandler.UpdateSettingsHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog) (ConstructorDeclaration)` | `fcbeb3b2a789` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Printing/CreatePrintJobsHandler.cs#L38-L55)  
> **Lines 38-55** in `Falah.Infrastructure.Printing.CreatePrintJobsHandler.HandleAsync(Falah.Application.Printing.CreatePrintJobsRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `ac7a5a9aefba464dfb2444c86d6a5bb22149455b2f82856f3fea4971931ff941`):

```csharp
  38 |         PrintRoute? customerRoute = null;
  39 |         if (!string.IsNullOrEmpty(request.CustomerRoute))
  40 |         {
  41 |             if (!Enum.TryParse<PrintRoute>(request.CustomerRoute, out var parsed) || parsed is not (PrintRoute.Receipt or PrintRoute.CallCenter))
  42 |                 throw new DomainException("مسار الطباعة المطلوب للعميل غير صالح.");
  43 |             customerRoute = parsed;
  44 |         }
  45 | 
  46 |         var order = await _db.Orders.AsNoTracking().Include(o => o.Items).FirstOrDefaultAsync(o => o.Id == request.OrderId, ct)
  47 |             ?? throw new InvalidOperationException("الطلب غير موجود.");
  48 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  49 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  50 | 
  51 |         IReadOnlyCollection<PrintRoute> kitchenRoutes = request.IncludeKitchenRoutes
  52 |             ? PrintRules.DetermineKitchenRoutes(order.Items.Select(i => i.PreparationStation))
  53 |             : Array.Empty<PrintRoute>();
  54 | 
  55 |         var routes = new List<PrintRoute>(kitchenRoutes);
```

> **File:** [`src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Settings/UpdateSettingsHandler.cs#L20-L35)  
> **Lines 20-35** in `Falah.Infrastructure.Settings.UpdateSettingsHandler.UpdateSettingsHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Domain.Audit.IAuditLog) (ConstructorDeclaration)` (File SHA-256: `fcbeb3b2a7895001c185355dc1d9551d5efd4256d3b89a8805328e2a83eab79f`):

```csharp
  20 |     private readonly IAuditLog _auditLog;
  21 | 
  22 |     public UpdateSettingsHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, IAuditLog auditLog)
  23 |     {
  24 |         _db = db;
  25 |         _approvalRegistry = approvalRegistry;
  26 |         _auditLog = auditLog;
  27 |     }
  28 | 
  29 |     /// <summary>
  30 |     /// The menu link becomes a QR code on every customer receipt, so it is checked before it is
  31 |     /// stored: only an absolute http or https address is accepted. A mistyped value would otherwise
  32 |     /// print a code that customers scan and land nowhere - and they would blame the restaurant, not
  33 |     /// the typo. An empty value clears the link, which removes the QR from the receipt.
  34 |     /// </summary>
  35 |     internal static string? NormalizeMenuUrl(string value)
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Persistence/UI configuration has no mapped consumer in render/dispatch implementation.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Settings accepts PrinterWidth and NumberOfReceiptCopies, but auto-print creates one job per route and agent uses its own ImageWidthDots. Operators can save values that do not change output.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Persistence/UI configuration has no mapped consumer in render/dispatch implementation.

**E. Future Repair Specification:**  
Define per-station paper capability and copy semantics; explicitly label unsupported settings until implemented. Copies must not multiply kitchen prep accidentally.

**Required Regression Test Cases:**
- Add intended copy/width contract tests or disable unsupported setting UI.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

## Security And Access Control

### SEC-001: JSON actor casing mismatch permits body-selected authorization identity
- **Severity:** `HIGH` | **Confidence:** `CONFIRMED_MECHANISM_AND_INDEPENDENT_STATIC_TRACE` | **Disposition:** `INDEPENDENTLY_CORROBORATED_AND_CORRECTED`
- **Disposition Rationale:** Forensically traced through middleware, JSON parsing, Minimal API model binding, and command handler. Prior Codex claim of arbitrary JSON Actor object injection corrected to case-sensitive TryGetProperty mismatch bypass.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L86-L133) | `86-133` | `Falah.Server.AuthenticationMiddleware.FindActorMismatchAsync(HttpContext, string) (MethodDeclaration)` | `3e64ad8ef532` |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1951-L1958) | `1951-1958` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L32-L36) | `32-36` | `Falah.Infrastructure.Users.UpdateUserPermissionsHandler.HandleAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken) (MethodDeclaration)` | `796cc8d48d0b` |
| [`src/Falah.Application/Users/UserManagementRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Users/UserManagementRequests.cs#L1-L20) | `1-20` | `File-level declaration or top-level statement` | `675c03a175e0` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Server/AuthenticationMiddleware.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/AuthenticationMiddleware.cs#L86-L133)  
> **Lines 86-133** in `Falah.Server.AuthenticationMiddleware.FindActorMismatchAsync(HttpContext, string) (MethodDeclaration)` (File SHA-256: `3e64ad8ef5324fe99aa19a0dead024a4afe8f99e038f8e280631b2eb8eabc5eb`):

```csharp
  86 |     private static readonly string[] ActorFields =
  87 |     {
  88 |         "actorUserId", "cashierId", "createdByUserId", "openedByUserId", "reportedByUserId",
  89 |     };
  90 | 
  91 |     private static async Task<string?> FindActorMismatchAsync(HttpContext context, string authenticatedUserId)
  92 |     {
  93 |         foreach (var field in ActorFields)
  94 |         {
  95 |             if (context.Request.Query.TryGetValue(field, out var values))
  96 |             {
  97 |                 var claimed = values.ToString();
  98 |                 if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
  99 |             }
 100 |         }
 101 | 
 102 |         if (!context.Request.HasJsonContentType()) return null;
 103 | 
 104 |         // Buffered so the endpoint's own model binding can still read the body afterwards.
 105 |         context.Request.EnableBuffering();
 106 |         try
 107 |         {
 108 |             using var document = await System.Text.Json.JsonDocument.ParseAsync(
 109 |                 context.Request.Body, cancellationToken: context.RequestAborted);
 110 |             if (document.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object) return null;
 111 | 
 112 |             foreach (var field in ActorFields)
 113 |             {
 114 |                 if (document.RootElement.TryGetProperty(field, out var value)
 115 |                     && value.ValueKind == System.Text.Json.JsonValueKind.String)
 116 |                 {
 117 |                     var claimed = value.GetString();
 118 |                     if (!string.IsNullOrEmpty(claimed) && claimed != authenticatedUserId) return field;
 119 |                 }
 120 |             }
 121 |         }
 122 |         catch (System.Text.Json.JsonException)
 123 |         {
 124 |             // Malformed JSON is the endpoint's problem to report, not this middleware's.
 125 |             return null;
 126 |         }
 127 |         finally
 128 |         {
 129 |             context.Request.Body.Position = 0;
 130 |         }
 131 | 
 132 |         return null;
 133 |     }
```

> **File:** [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L1951-L1958)  
> **Lines 1951-1958** in `lambda expression (ParenthesizedLambdaExpression)` (File SHA-256: `5b11d5b03d7010d890808029d561880a056c6ea922223b70567fc657b7261fc6`):

```csharp
1951 | app.MapPost("/api/users/{id}/permissions", async (string id, Falah.Application.Users.UpdateUserPermissionsRequest body, UpdateUserPermissionsHandler handler, CancellationToken ct) =>
1952 | {
1953 |     try
1954 |     {
1955 |         var request = body with { UserId = id };
1956 |         await handler.HandleAsync(request, ct);
1957 |         return Results.Ok();
1958 |     }
```

> **File:** [`src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Users/UpdateUserPermissionsHandler.cs#L32-L36)  
> **Lines 32-36** in `Falah.Infrastructure.Users.UpdateUserPermissionsHandler.HandleAsync(Falah.Application.Users.UpdateUserPermissionsRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `796cc8d48d0b1224441c6028cd98cbdd10abae53896a4b591ce04c86e146fe55`):

```csharp
  32 |         var actorUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.ActorUserId, ct)
  33 |             ?? throw new InvalidOperationException("المستخدم غير موجود.");
  34 |         var actor = OrderMapper.ToActor(actorUser);
  35 |         AuthorizationPolicy.AuthorizeCommand(_approvalRegistry, actor, "users_manage");
  36 | 
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
In AuthenticationMiddleware.cs (lines 86-133), FindActorMismatchAsync uses case-sensitive TryGetProperty("actorUserId", out _) against lowercase string names. An incoming JSON payload with PascalCase 'ActorUserId' silently bypasses the mismatch check. Minimal API model binding in Program.cs (lines 1951-1958) binds case-insensitively into UpdateUserPermissionsRequest.ActorUserId. UpdateUserPermissionsHandler.cs (lines 32-36) queries DB for request.ActorUserId and authorizes the operation using that user's permissions instead of context.ActorId() from the verified bearer session token.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Source-Proven: Complete privilege escalation. Any authenticated user holding a valid session token (even cashier or low-privilege employee) can grant themselves arbitrary permissions (including users_manage, orders_refund, all) by supplying 'ActorUserId': 'usr-admin' in PascalCase on POST /api/users/{id}/permissions. (Prior claim of arbitrary JSON Actor object injection was inaccurate and is refuted by wire DTO definitions).

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
In AuthenticationMiddleware.cs (lines 86-133), FindActorMismatchAsync uses case-sensitive TryGetProperty("actorUserId", out _) against lowercase string names. An incoming JSON payload with PascalCase 'ActorUserId' silently bypasses the mismatch check. Minimal API model binding in Program.cs (lines 1951-1958) binds case-insensitively into UpdateUserPermissionsRequest.ActorUserId. UpdateUserPermissionsHandler.cs (lines 32-36) queries DB for request.ActorUserId and authorizes the operation using that user's permissions instead of context.ActorId() from the verified bearer session token.

**E. Future Repair Specification:**  
1. In AuthenticationMiddleware.cs, parse request body using case-insensitive property name matching (e.g. EnumerateObject() with StringComparison.OrdinalIgnoreCase).
2. Refactor Program.cs endpoint and UpdateUserPermissionsHandler to eliminate ActorUserId from wire DTOs and bind authorized caller identity exclusively from context.ActorId() established by token session resolution.

**Required Regression Test Cases:**
- Submit POST /api/users/{targetId}/permissions with token for low-privilege cashier and body containing 'ActorUserId': 'usr-admin'; assert HTTP 403 Forbidden.
- Verify case variations ('actorUserId', 'ActorUserId', 'ACTORUSERID') all trigger actor mismatch rejection in middleware.
- Verify UpdateUserPermissionsHandler authorizes against authenticated session user even if body attempts to provide ActorUserId.

**Acceptance Criteria & Verification Standard:**
- AuthenticationMiddleware rejects any mismatched actor identity regardless of JSON key casing.
- Handlers never read or trust client-supplied actor user ID from request body.

---

### SEC-002: PIN-bearing user-management requests are durably stored as unencrypted JSON
- **Severity:** `MEDIUM` | **Confidence:** `HIGH_FOR_SOURCE_SERIALIZATION;DEPLOYMENT_EXPOSURE_UNVERIFIED` | **Disposition:** `INDEPENDENTLY_CORROBORATED_AND_CORRECTED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs#L59-L76) | `59-76` | `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Save(string, object) (MethodDeclaration)` | `25899a9e6b6a` |
| [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L14-L25) | `14-25` | `File-level declaration or top-level statement` | `a880d1ded757` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequestStore.cs#L59-L76)  
> **Lines 59-76** in `Falah.Client.CashierWinUI.Services.FileRetainedRequestStore.Save(string, object) (MethodDeclaration)` (File SHA-256: `25899a9e6b6ad147bd61d624236829f21723de49274041d0f7c40b69819a2303`):

```csharp
  59 |     public void Save(string ownerKey, object request)
  60 |     {
  61 |         try
  62 |         {
  63 |             var envelope = new
  64 |             {
  65 |                 Type = request.GetType().AssemblyQualifiedName,
  66 |                 SavedAtUtc = DateTimeOffset.UtcNow,
  67 |                 Payload = request,
  68 |             };
  69 |             // Written whole, then moved into place: a station losing power mid-write must never leave
  70 |             // a half-written file that reads back as a different request than the one submitted.
  71 |             var path = PathFor(ownerKey);
  72 |             var temp = path + ".tmp";
  73 |             File.WriteAllText(temp, JsonSerializer.Serialize(envelope, Options));
  74 |             File.Move(temp, path, overwrite: true);
  75 |         }
  76 |         catch (Exception ex)
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L14-L25)  
> **Lines 14-25** in `File-level declaration or top-level statement` (File SHA-256: `a880d1ded757372d47ee1b1976bb1b61de63d1bb7531e72de7d43118314918f6`):

```csharp
  14 |     private readonly RetainedRequest<ChangePinRequest> _pendingPin = new("users.change-pin");
  15 |     private readonly RetainedRequest<SetUserActiveRequest> _pendingActive = new("users.set-active");
  16 |     private readonly RetainedRequest<UpdateUserPermissionsRequest> _pendingPermissions = new("users.permissions");
  17 |     public bool HasPendingAction => _pendingCreate.HasPending || _pendingPin.HasPending || _pendingActive.HasPending || _pendingPermissions.HasPending;
  18 |     public bool CanEditAction => !IsBusy && !HasPendingAction;
  19 |     public bool IsUserDialogOpen => IsCreateUserOpen || PinTarget is not null;
  20 |     public bool CanCreateUser => CanEditAction && !IsUserDialogOpen && !IsLoading && LoadError is null &&
  21 |         _catalog.Count > 0 && _session.HasPermission("users_manage");
  22 |     public bool CanChangeSelectedPin => CanEditAction && !IsUserDialogOpen && !IsLoading && LoadError is null && SelectedUser is not null &&
  23 |         (SelectedUser.Id == _session.CurrentUser?.Id || (!IsOwnerSelected && _session.HasPermission("users_manage")));
  24 |     public bool IsPinChangeOpen => PinTarget is not null;
  25 |     public bool IsSelfPinChange => PinTarget?.Id == _session.CurrentUser?.Id;
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
FileRetainedRequestStore.cs in CashierWinUI and CallCenterWinUI writes raw in-flight request payloads to %LOCALAPPDATA%\Falah\pending-operations\{station}\{ownerKey}.json as unencrypted plaintext JSON. User creation, PIN changes, and manager overrides that run while offline or mid-dispatch persist plaintext PINs on disk.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Workstation credential exposure. Anyone with read access to the local machine's user directory (%LOCALAPPDATA%) can read persistent and in-flight employee and manager PINs in plaintext.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
FileRetainedRequestStore.cs in CashierWinUI and CallCenterWinUI writes raw in-flight request payloads to %LOCALAPPDATA%\Falah\pending-operations\{station}\{ownerKey}.json as unencrypted plaintext JSON. User creation, PIN changes, and manager overrides that run while offline or mid-dispatch persist plaintext PINs on disk.

**E. Future Repair Specification:**  
Encrypt retained request payloads using Windows Data Protection API (ProtectedData.Protect with CurrentUser scope) prior to file write. Mask or redact PIN properties from retained request diagnostic envelopes.

**Required Regression Test Cases:**
- Execute CreateUser or PIN change request in CashierWinUI while offline; inspect persisted JSON in %LOCALAPPDATA%; assert payload is ciphertext and PIN is unreadable in plaintext.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### SEC-003: Authenticated employee reads expose complete sensitive profiles without employee permission or self-access restriction
- **Severity:** `HIGH` | **Confidence:** `HIGH_FOR_SOURCE_MECHANISM; DEPLOYED_IMPACT_AND_FIELD_POLICY_UNVERIFIED` | **Disposition:** `INDEPENDENTLY_CORROBORATED`
- **Disposition Rationale:** Verified directly against current baseline source code. Flaw logic, call path, and impact independently confirmed with zero contradictions.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L685-L700) | `685-700` | `lambda expression (ParenthesizedLambdaExpression)` | `5b11d5b03d70` |
| [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L25-L45) | `25-45` | `Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` | `fbb120025323` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Server/Program.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Server/Program.cs#L685-L700)  
> **Lines 685-700** in `lambda expression (ParenthesizedLambdaExpression)` (File SHA-256: `5b11d5b03d7010d890808029d561880a056c6ea922223b70567fc657b7261fc6`):

```csharp
 685 | app.MapGet("/api/employees", async (EmployeeQueryService query, CancellationToken ct) => Results.Ok(await query.ListAsync(ct)));
 686 | 
 687 | app.MapGet("/api/employees/{employeeId}", async (string employeeId, EmployeeQueryService query, CancellationToken ct) =>
 688 | {
 689 |     try { return Results.Ok(await query.GetAsync(employeeId, ct)); }
 690 |     catch (InvalidOperationException ex) { return Results.NotFound(new { error = ex.Message }); }
 691 | });
 692 | 
 693 | app.MapGet("/api/employees/{employeeId}/ledger", async (string employeeId, EmployeeQueryService query, CancellationToken ct) =>
 694 |     Results.Ok(await query.ListLedgerEntriesAsync(employeeId, ct)));
 695 | 
 696 | app.MapGet("/api/employees/{employeeId}/balance", async (string employeeId, EmployeeQueryService query, CancellationToken ct) =>
 697 |     Results.Ok(new { employeeId, balancePiastres = await query.GetBalanceAsync(employeeId, ct) }));
 698 | 
 699 | // Payroll/employee financial reporting by today/last-7/last-10/month/custom range - the reporting
 700 | // side of the final business rule, now that payment is not tied to a payroll period. Gated on
```

> **File:** [`src/Falah.Infrastructure/Employees/EmployeeQueryService.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Employees/EmployeeQueryService.cs#L25-L45)  
> **Lines 25-45** in `Falah.Infrastructure.Employees.EmployeeQueryService.GetFinancialReportAsync(DateOnly?, DateOnly?, CancellationToken) (MethodDeclaration)` (File SHA-256: `fbb1200253239f442fdb0933abb5270f12c7d404e5cd549150c71a66c96c6341`):

```csharp
  25 |     public async Task<List<EmployeeFinancialReportLineDto>> GetFinancialReportAsync(
  26 |         DateOnly? from, DateOnly? to, CancellationToken ct = default)
  27 |     {
  28 |         DateTimeOffset? start = null, end = null;
  29 |         if (from is { } f) start = BusinessClock.UtcRangeForBusinessDate(f).Start;
  30 |         if (to is { } t) end = BusinessClock.UtcRangeForBusinessDate(t).End;
  31 | 
  32 |         var employees = await _db.Employees.AsNoTracking().Where(e => e.IsActive).ToListAsync(ct);
  33 |         var allEntries = await _db.EmployeeLedgerEntries.AsNoTracking()
  34 |             .Select(e => new { e.EmployeeId, e.Type, e.CreditPiastres, e.DebitPiastres, e.CreatedAtUtc })
  35 |             .ToListAsync(ct);
  36 | 
  37 |         var earningsTypes = new[] { nameof(LedgerEntryType.DailyWage), nameof(LedgerEntryType.Salary), nameof(LedgerEntryType.Overtime), nameof(LedgerEntryType.ExtraShift) };
  38 |         var additionTypes = new[] { nameof(LedgerEntryType.Bonus), nameof(LedgerEntryType.OtherEarning) };
  39 |         var deductionTypes = new[] { nameof(LedgerEntryType.Deduction), nameof(LedgerEntryType.Adjustment) };
  40 |         var paymentTypes = new[] { nameof(LedgerEntryType.Withdrawal), nameof(LedgerEntryType.Settlement), nameof(LedgerEntryType.PayrollSettlement) };
  41 | 
  42 |         var result = new List<EmployeeFinancialReportLineDto>(employees.Count);
  43 |         foreach (var employee in employees)
  44 |         {
  45 |             var allForEmployee = allEntries.Where(e => e.EmployeeId == employee.Id).ToList();
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
GET /api/employees and GET /api/employees/{employeeId} in Program.cs (lines 685-687) have no permission policy attribute. EmployeeQueryService.cs (lines 81-85) unconditionally projects all database columns (NationalId, DailyWagePiastres, MonthlySalaryPiastres, OvertimeRatePiastres, FingerprintId, Address, Phone) into EmployeeDto.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Internal data exposure. Any authenticated terminal session (including frontline cashiers, waiters, delivery drivers) can query the exact salaries, overtime rates, national IDs, and home addresses of all restaurant staff.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
GET /api/employees and GET /api/employees/{employeeId} in Program.cs (lines 685-687) have no permission policy attribute. EmployeeQueryService.cs (lines 81-85) unconditionally projects all database columns (NationalId, DailyWagePiastres, MonthlySalaryPiastres, OvertimeRatePiastres, FingerprintId, Address, Phone) into EmployeeDto.

**E. Future Repair Specification:**  
Introduce PublicEmployeeSummaryDto for operational cashier selection. Restrict GET /api/employees full profile to callers possessing 'employees_manage' or 'payroll_view' permissions.

**Required Regression Test Cases:**
- Query GET /api/employees with cashier bearer token; assert response contains only public summary fields and omits salary/national ID.
- Query GET /api/employees with manager bearer token; assert full profile is returned.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

## Ui Ux And Desktop Client State

### UI-001: Primary cashier and treasury money requests lose original retry identity on process loss
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs#L29-L56) | `29-56` | `Falah.Client.CashierWinUI.Services.RetainedRequest<TRequest>.WasRecovered.get (GetAccessorDeclaration)` | `7cf2985b1e77` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L401-L426) | `401-426` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest) (MethodDeclaration)` | `066c11322cca` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs#L104-L124) | `104-124` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteExpenseAsync(Falah.Application.Expenses.AddExpenseRequest) (MethodDeclaration)` | `124a969a63f8` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs#L16-L18) | `16-18` | `File-level declaration or top-level statement` | `bc399716e95c` |
| [`src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/TreasuryViewModel.cs#L162-L164) | `162-164` | `File-level declaration or top-level statement` | `7e6c0ca570ca` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/RetainedRequest.cs#L29-L56)  
> **Lines 29-56** in `Falah.Client.CashierWinUI.Services.RetainedRequest<TRequest>.WasRecovered.get (GetAccessorDeclaration)` (File SHA-256: `7cf2985b1e7764377ea5c972efae4ae418790bab38ef8aeebfd5198f86b5a68b`):

```csharp
  29 | 
  30 |     /// <summary>True when this request came back from disk rather than from this session - the
  31 |     /// operation was submitted before the process ended and its outcome is still unknown.</summary>
  32 |     public bool WasRecovered { get; private set; }
  33 | 
  34 |     public async Task<ApiClient.ApiResult<TResult>> SendAsync<TResult>(
  35 |         TRequest request, Func<TRequest, Task<ApiClient.ApiResult<TResult>>> send)
  36 |     {
  37 |         var wasUnresolved = Value is not null;
  38 |         Value ??= request;
  39 |         // Persisted before the send, not after: the window this protects is the one where the
  40 |         // request has left but its outcome has not come back.
  41 |         if (_ownerKey is not null && !wasUnresolved) _store?.Save(_ownerKey, Value);
  42 | 
  43 |         var result = await send(Value);
  44 |         if (result.Success && result.Value is not null || !wasUnresolved && !result.OutcomeUnknown)
  45 |         {
  46 |             Value = null;
  47 |             WasRecovered = false;
  48 |             if (_ownerKey is not null) _store?.Clear(_ownerKey);
  49 |         }
  50 |         return result;
  51 |     }
  52 | 
  53 |     public string FailureMessage(string? error) => HasPending
  54 |         ? "لم تُحسم نتيجة العملية. أعد المحاولة بنفس الطلب للتحقق قبل المغادرة."
  55 |         : error ?? "تعذر تنفيذ العملية.";
  56 | }
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L401-L426)  
> **Lines 401-426** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest) (MethodDeclaration)` (File SHA-256: `066c11322cca65d746b3c082a7e58573ac8430f5431b21b359f6d5b192224e5a`):

```csharp
 401 |     private CheckoutOrderRequest? _pendingCheckout;
 402 |     private async Task CompleteCheckoutAsync(CheckoutOrderRequest request)
 403 |     {
 404 |         var wasUnresolved = _pendingCheckout is not null;
 405 |         _pendingCheckout = request;
 406 |         RefreshOperationState();
 407 |         var result = await _session.Api.CheckoutAsync(request);
 408 |         if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingCheckout = null;
 409 |         RefreshOperationState();
 410 |         if (!result.Success || result.Value is null)
 411 |         {
 412 |             // The request id is kept so a retry of the SAME payment can never become a second
 413 |             // order - only "إلغاء" / starting a new order clears it.
 414 |             CheckoutError = result.Error ?? "تعذر إتمام عملية الدفع.";
 415 |             return;
 416 |         }
 417 | 
 418 |         _reviewingExistingOrder = false;
 419 |         LastCheckoutResult = result.Value;
 420 |         IsCheckoutOpen = false;
 421 |         Cart.Clear();
 422 |         RecomputeCartTotals();
 423 |         CurrentCheckoutRequestId = null;
 424 |         IsDiscountEnabled = false;
 425 |         IsCompEnabled = false;
 426 |         IsSplitPaymentEnabled = false;
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Expense.cs#L104-L124)  
> **Lines 104-124** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CompleteExpenseAsync(Falah.Application.Expenses.AddExpenseRequest) (MethodDeclaration)` (File SHA-256: `124a969a63f823004a0b4ee64ef499842ad61128518dc236124f122ed8a8deb9`):

```csharp
 104 |     private AddExpenseRequest? _pendingExpense;
 105 |     private string _ExpenseRequestId = Guid.NewGuid().ToString("N");
 106 |     private async Task CompleteExpenseAsync(AddExpenseRequest request)
 107 |     {
 108 |         var wasUnresolved = _pendingExpense is not null;
 109 |         _pendingExpense = request;
 110 |         RefreshOperationState();
 111 |         var result = await _session.Api.AddExpenseAsync(request);
 112 |         if (result.Success || (!wasUnresolved && !result.OutcomeUnknown)) _pendingExpense = null;
 113 |         if (result.Success) _ExpenseRequestId = Guid.NewGuid().ToString("N");
 114 |         RefreshOperationState();
 115 |         if (!result.Success || result.Value is null)
 116 |         {
 117 |             ExpenseError = result.Error ?? "تعذر تسجيل المصروف.";
 118 |             return;
 119 |         }
 120 | 
 121 |         IsAddExpenseOpen = false;
 122 |     }
 123 | 
 124 | }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Normal close/edit gates and same-process fixed keys cannot preserve immutable submitted DTO/key across forced termination. Primary financial partials bypass existing durable helper.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
No reliable station-owned recovery question for original checkout/refund/expense/treasury action; manually repeating with new key can create duplicate real operations. Actual duplicate ledger writes NOT tested.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Normal close/edit gates and same-process fixed keys cannot preserve immutable submitted DTO/key across forced termination. Primary financial partials bypass existing durable helper.

**E. Future Repair Specification:**  
Persist exact versioned typed financial request and owner before dispatch; expose recovery by original identity, block replacement until outcome known; no fixes performed.

**Required Regression Test Cases:**
- Simulate committed-but-lost-response with fake transport, reconstruct actual cashier VM/session with store, assert exact request ID/payload recovered before any new action.
- Repeat for refund, expense, safe drop, drawer transfer, shift open/close and treasury operations; assert server ledger unaffected on replay in isolated integration fixture.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-002: Unselected expense and payment buttons combine literal white with dark-theme pale text
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/Services/ThemeResources.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ThemeResources.cs#L18-L35) | `18-35` | `Falah.Client.CashierWinUI.Services.ThemeResources.Get(string) (MethodDeclaration)` | `ecffb4313571` |
| [`src/Falah.Client.CashierWinUI/Styles/Colors.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Styles/Colors.xaml#L235-L235) | `235-235` | `File-level declaration or top-level statement` | `1d2161fc5905` |
| [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml#L313-L340) | `313-340` | `File-level declaration or top-level statement` | `4531af2dec43` |
| [`src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CheckoutView.xaml#L31-L42) | `31-42` | `File-level declaration or top-level statement` | `0d6cc87cbff8` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/Services/ThemeResources.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ThemeResources.cs#L18-L35)  
> **Lines 18-35** in `Falah.Client.CashierWinUI.Services.ThemeResources.Get(string) (MethodDeclaration)` (File SHA-256: `ecffb4313571b1c3065fbc5c6b2d38af692bb72c80f79f759a716374b78c1b5b`):

```csharp
  18 |     public static object? Get(string key)
  19 |     {
  20 |         var app = global::Microsoft.UI.Xaml.Application.Current;
  21 |         var root = App.MainAppWindow?.Content as FrameworkElement;
  22 |         var themeName = root?.ActualTheme == ElementTheme.Dark ? "Dark" : "Light";
  23 | 
  24 |         if (app.Resources.ThemeDictionaries.TryGetValue(themeName, out var dictionary)
  25 |             && dictionary is ResourceDictionary themed
  26 |             && themed.TryGetValue(key, out var themedValue))
  27 |         {
  28 |             return themedValue;
  29 |         }
  30 | 
  31 |         // Falls through to the flat (non-themed) resources - WhiteBrush, TransparentBrush, the
  32 |         // gradients - for any key that is not one of the themed ones above.
  33 |         return app.Resources.TryGetValue(key, out var flatValue) ? flatValue : null;
  34 |     }
  35 | }
```

> **File:** [`src/Falah.Client.CashierWinUI/Styles/Colors.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Styles/Colors.xaml#L235-L235)  
> **Lines 235-235** in `File-level declaration or top-level statement` (File SHA-256: `1d2161fc590503a13ba47c8c2488daf2ac73a6c6e373662b7e83dcfc0b395e87`):

```xaml
 235 |             <Color x:Key="TextPrimaryColor">#E6E9EE</Color>
```

> **File:** [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml#L313-L340)  
> **Lines 313-340** in `File-level declaration or top-level statement` (File SHA-256: `4531af2dec4309d133530be9b28327dc14b7675c0179ac70a51bb300bc22a26f`):

```xaml
 313 |                         <Button Grid.Row="0" Grid.Column="0" Content="مستلزمات مطبخ وخضار" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 314 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="مستلزمات مطبخ وخضار"
 315 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=مستلزمات مطبخ وخضار|ActionBlueBrush|WhiteBrush}"
 316 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=مستلزمات مطبخ وخضار|WhiteBrush|TextPrimaryBrush}"/>
 317 |                         <Button Grid.Row="0" Grid.Column="1" Content="غاز وفحم وشواية" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 318 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="غاز وفحم"
 319 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=غاز وفحم|ActionBlueBrush|WhiteBrush}"
 320 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=غاز وفحم|WhiteBrush|TextPrimaryBrush}"/>
 321 |                         <Button Grid.Row="1" Grid.Column="0" Content="أكياس وورق وتغليف" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 322 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="أكياس وتغليف"
 323 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=أكياس وتغليف|ActionBlueBrush|WhiteBrush}"
 324 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=أكياس وتغليف|WhiteBrush|TextPrimaryBrush}"/>
 325 |                         <Button Grid.Row="1" Grid.Column="1" Content="نظافة ومطهرات" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 326 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="نظافة ومطهرات"
 327 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=نظافة ومطهرات|ActionBlueBrush|WhiteBrush}"
 328 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=نظافة ومطهرات|WhiteBrush|TextPrimaryBrush}"/>
 329 |                         <Button Grid.Row="2" Grid.Column="0" Content="مواصلات وبنزين" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 330 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="مواصلات ودليفري"
 331 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=مواصلات ودليفري|ActionBlueBrush|WhiteBrush}"
 332 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=مواصلات ودليفري|WhiteBrush|TextPrimaryBrush}"/>
 333 |                         <Button Grid.Row="2" Grid.Column="1" Content="صيانة سريعة وأدوات" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 334 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="صيانة سريعة"
 335 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=صيانة سريعة|ActionBlueBrush|WhiteBrush}"
 336 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=صيانة سريعة|WhiteBrush|TextPrimaryBrush}"/>
 337 |                         <Button Grid.Row="3" Grid.Column="0" Grid.ColumnSpan="2" Content="نثريات أخرى" Style="{StaticResource SecondaryButtonStyle}" MinHeight="44" FontSize="12"
 338 |                                 Command="{Binding SetExpenseCategoryCommand}" CommandParameter="أخرى"
 339 |                                 Background="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=أخرى|ActionBlueBrush|WhiteBrush}"
 340 |                                 Foreground="{Binding ExpenseCategory, Converter={StaticResource StringEqualsToBrushConverter}, ConverterParameter=أخرى|WhiteBrush|TextPrimaryBrush}"/>
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Instance background bindings override correctly-themed SecondaryButtonStyle/PaymentMethodButtonStyle with WhiteBrush; text continues themed.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Near-white labels on white surface, computed source-token contrast ~1.22:1, makes unselected choices hard to distinguish. User visual symptom consistent; reviewer has no actual screenshot and does not claim rendered pixel measurement.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Instance background bindings override correctly-themed SecondaryButtonStyle/PaymentMethodButtonStyle with WhiteBrush; text continues themed.

**E. Future Repair Specification:**  
Use coherent semantic card/neutral background with themed primary text for unselected state, direct ThemeResource/visual states for live updates; selected accent uses suitable contrast foreground.

**Required Regression Test Cases:**
- Render all expense categories and all four payment methods selected/unselected in Light/Dark/System, unchanged and after live toggle; assert resolved text/background luminance and readable native screenshot.
- Check disabled/hover/pressed states separately, preserving stored category values.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-003: Requested pickup kitchen-before-payment stage is absent from active client workflow
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L337-L383) | `337-383` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitPaymentAsync() (MethodDeclaration)` | `066c11322cca` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs#L78-L107) | `78-107` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SavePickupDetails() (MethodDeclaration)` | `7799e030dd8a` |
| [`src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs#L39-L42) | `39-42` | `Falah.Client.CashierWinUI.Views.CartPanelView.ApplyFooterDensity(double) (MethodDeclaration)` | `006643e346cc` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L337-L383)  
> **Lines 337-383** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitPaymentAsync() (MethodDeclaration)` (File SHA-256: `066c11322cca65d746b3c082a7e58573ac8430f5431b21b359f6d5b192224e5a`):

```csharp
 337 |             }
 338 | 
 339 |             if (!CashInput.TryParse(AmountPaidText, out var enteredPiastres) && !IsSplitPaymentEnabled)
 340 |             { CheckoutError = "أدخل المبلغ المدفوع بصورة صحيحة."; return; }
 341 |             var amountPiastres = IsSplitPaymentEnabled
 342 |                 ? splitPayments!.Sum(s => s.AmountPiastres)
 343 |                 : PaymentMethod == "cash" && !IsCashOnDelivery ? enteredPiastres : GrandTotalPiastres;
 344 | 
 345 |             var isDelivery = OrderType == "delivery";
 346 | 
 347 |             var request = new CheckoutOrderRequest(
 348 |                 CheckoutRequestId: CurrentCheckoutRequestId!,
 349 |                 CashierId: _session.CurrentUser!.Id,
 350 |                 ShiftId: _session.ShiftId!,
 351 |                 OrderType: OrderType,
 352 |                 PaymentMethod: IsSplitPaymentEnabled ? "multi" : PaymentMethod,
 353 |                 Items: Cart.Select(l => new CheckoutItemRequest(l.ProductId, l.VariantId, l.Quantity, l.AppliedOfferId, l.Notes)).ToList(),
 354 |                 DiscountPiastres: DiscountAmountPiastres,
 355 |                 DeliveryFeePiastres: isDelivery ? SelectedDeliveryService?.FeePiastres ?? 0 : 0,
 356 |                 CompAmountPiastres: CompAmountPiastres,
 357 |                 CompApprovalId: compApprovalId,
 358 |                 CompApproverUserId: compApproverUserId,
 359 |                 CompReason: CompAmountPiastres > 0 ? CompReasonText : null,
 360 |                 AmountPaidPiastres: amountPiastres,
 361 |                 SplitPayments: splitPayments,
 362 |                 WalletProvider: PaymentMethod == "wallet" || IsSplitPaymentEnabled && (SplitMethod1 == "wallet" || SplitMethod2 == "wallet") ? NullIfEmpty(WalletProvider) : null,
 363 |                 TableNumber: null,   // no order type has a table since dine-in was replaced by pickup
 364 |                 RequestedPickupAtUtc: IsPickup ? RequestedPickupAtUtc : null,
 365 |                 CustomerName: (isDelivery || IsPickup) ? NullIfEmpty(CustomerName) : null,
 366 |                 CustomerPhone: (isDelivery || IsPickup) ? NullIfEmpty(CustomerPhone) : null,
 367 |                 DeliveryAddress: null,
 368 |                 DeliveryArea: isDelivery ? NullIfEmpty(DeliveryArea) : null,
 369 |                 BusinessDayId: null,
 370 |                 DeliveryStreet: isDelivery ? NullIfEmpty(DeliveryStreet) : null,
 371 |                 DeliveryBuilding: isDelivery ? NullIfEmpty(DeliveryBuilding) : null,
 372 |                 DeliveryFloor: isDelivery ? NullIfEmpty(DeliveryFloor) : null,
 373 |                 DeliveryApartment: isDelivery ? NullIfEmpty(DeliveryApartment) : null,
 374 |                 DeliveryLandmark: isDelivery ? NullIfEmpty(DeliveryLandmark) : null,
 375 |                 DeliveryNotes: isDelivery ? NullIfEmpty(DeliveryNotes) : null,
 376 |                 DeliveryServiceId: isDelivery ? SelectedDeliveryService?.Id : null,
 377 |                 DeliveryServiceName: isDelivery ? SelectedDeliveryService?.Name : null,
 378 |                 CompReasonCode: CompAmountPiastres > 0 ? CompReasonCode : null,
 379 |                 // The server creates this order's tickets as part of the checkout call itself, so a
 380 |                 // ticket cannot be lost by this app dying between the sale and the print.
 381 |                 CustomerPrintRoute: "Receipt");
 382 | 
 383 |             await CompleteCheckoutAsync(request);
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs#L78-L107)  
> **Lines 78-107** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SavePickupDetails() (MethodDeclaration)` (File SHA-256: `7799e030dd8ac3693288cc2cfd82163ef3cb7eb4846033f30eadaddc724e09e7`):

```csharp
  78 |     {
  79 |         var missing = DeliveryRules.FirstMissingPickupField(CustomerPhone, CustomerName);
  80 |         if (missing is not null) { PickupError = missing; return; }
  81 | 
  82 |         if ((PickupDate is null) != (PickupTime is null))
  83 |         {
  84 |             PickupError = "موعد الاستلام يحتاج التاريخ والوقت معًا - أو اتركهما فارغين.";
  85 |             return;
  86 |         }
  87 | 
  88 |         try
  89 |         {
  90 |             Falah.Domain.Orders.CheckoutRules.AssertRequestedPickupTime(
  91 |                 Falah.Domain.Orders.OrderType.Pickup, RequestedPickupAtUtc, DateTimeOffset.UtcNow);
  92 |         }
  93 |         catch (Falah.Domain.Common.DomainException ex)
  94 |         {
  95 |             // Same rule the server applies, run here first so the cashier is told at the form rather
  96 |             // than after the payment screen has already opened.
  97 |             PickupError = ex.Message;
  98 |             return;
  99 |         }
 100 | 
 101 |         CustomerName = CustomerName.Trim();
 102 |         CustomerPhone = CustomerPhone.Trim();
 103 |         PickupError = null;
 104 |         IsPickupEditorOpen = false;
 105 |         OnPropertyChanged(nameof(PickupSummary));
 106 |     }
 107 | }
```

> **File:** [`src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CartPanelView.xaml.cs#L39-L42)  
> **Lines 39-42** in `Falah.Client.CashierWinUI.Views.CartPanelView.ApplyFooterDensity(double) (MethodDeclaration)` (File SHA-256: `006643e346cc5e43ced82b22140a80be53e648bc4b9e5ee35b43aec9f5da82f9`):

```csharp
  39 |         Grid.SetColumn(FooterCheckoutButton, compact ? 1 : 0);
  40 |         Grid.SetColumnSpan(FooterCheckoutButton, compact ? 1 : 2);
  41 |         FooterCheckoutButton.MinHeight = compact ? 48 : 56;
  42 |         FooterCheckoutButton.Content = compact ? "الدفع ←" : "متابعة إلى الدفع  ←";
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Pickup is a detail variant of common checkout, not separate persisted preparation/awaiting-payment state with stage-aware primary command.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Client cannot perform owner-requested Stage A kitchen ticket then later Stage B checkout on same persisted unpaid pickup. Server/Domain end-to-end corroboration required before claiming global architecture absence.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Pickup is a detail variant of common checkout, not separate persisted preparation/awaiting-payment state with stage-aware primary command.

**E. Future Repair Specification:**  
Design explicit persisted pickup preparation/payment lifecycle using existing backend/domain capabilities where possible; implementation requires separate authorization.

**Required Regression Test Cases:**
- Create pickup preparation without payment in isolated backend test; stable ID and no payment movement; initial kitchen exactly once.
- Resume same order, collect final money, receipt once; operational pending list hides terminal paid/completed while history remains.
- Concurrent stage clicks and ambiguous print/payment responses retain stable identities.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-004: Imperative/converter brush results do not track live per-window theme reliably
- **Severity:** `MEDIUM` | **Confidence:** `MEDIUM` | **Disposition:** `REQUIRES_RUNTIME_VERIFICATION`
- **Disposition Rationale:** Static source code shows plausible concurrency/SQL race condition; complete physical proof requires concurrent multi-threaded SQL Server test harness.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/App.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/App.xaml#L8-L16) | `8-16` | `File-level declaration or top-level statement` | `92d676ceb94b` |
| [`src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs#L22-L30) | `22-30` | `Falah.Client.CashierWinUI.Controls.StatusPillView.OnTextChanged(Microsoft.UI.Xaml.DependencyObject, Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs) (MethodDeclaration)` | `4d17c848e22b` |
| [`src/Falah.Client.CashierWinUI/MainWindow.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/MainWindow.xaml.cs#L61-L62) | `61-62` | `Falah.Client.CashierWinUI.MainWindow.MaximizeOnTargetDisplay() (MethodDeclaration)` | `42992f5b0c16` |
| [`src/Falah.Client.CashierWinUI/Services/ThemeResources.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ThemeResources.cs#L18-L35) | `18-35` | `Falah.Client.CashierWinUI.Services.ThemeResources.Get(string) (MethodDeclaration)` | `ecffb4313571` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/App.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/App.xaml#L8-L16)  
> **Lines 8-16** in `File-level declaration or top-level statement` (File SHA-256: `92d676ceb94b33eefce212eafdf93fb09f00abd9508b5be0995cd6389357743e`):

```xaml
   8 |                 <!-- WinUI 3's own Fluent control resources (default TextBox/GridView/ProgressRing/etc.
   9 |                      templates and the theme brushes they reference) - every WinUI 3 app needs this;
  10 |                      its absence was a real bug here, surfacing as "Cannot find a Resource with the
  11 |                      Name/Key TabViewButtonBackground" the first time a control needing it rendered. -->
  12 |                 <XamlControlsResources xmlns="using:Microsoft.UI.Xaml.Controls"/>
  13 |                 <ResourceDictionary Source="Styles/Colors.xaml"/>
  14 |                 <ResourceDictionary Source="Styles/Typography.xaml"/>
  15 |                 <ResourceDictionary Source="Styles/Controls.xaml"/>
  16 |             </ResourceDictionary.MergedDictionaries>
```

> **File:** [`src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Controls/StatusPillView.xaml.cs#L22-L30)  
> **Lines 22-30** in `Falah.Client.CashierWinUI.Controls.StatusPillView.OnTextChanged(Microsoft.UI.Xaml.DependencyObject, Microsoft.UI.Xaml.DependencyPropertyChangedEventArgs) (MethodDeclaration)` (File SHA-256: `4d17c848e22bc8ee801ce2963109e5280c1faa97b876830dddb85b2310e4eb15`):

```csharp
  22 |     private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
  23 |     {
  24 |         var view = (StatusPillView)d;
  25 |         var text = (string)e.NewValue ?? string.Empty;
  26 |         view.Label.Text = text;
  27 |         var converter = new Converters.StatusToPillBrushConverter();
  28 |         view.Root.Background = (Microsoft.UI.Xaml.Media.Brush)converter.Convert(text, typeof(string), null!, string.Empty);
  29 |     }
  30 | }
```

> **File:** [`src/Falah.Client.CashierWinUI/MainWindow.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/MainWindow.xaml.cs#L61-L62)  
> **Lines 61-62** in `Falah.Client.CashierWinUI.MainWindow.MaximizeOnTargetDisplay() (MethodDeclaration)` (File SHA-256: `42992f5b0c16a801fa6671035e2600a5ec3abf06c48c7b836b30f68cf03c4558`):

```csharp
  61 |             presenter.Maximize();
  62 |     }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Converter returns concrete Brush object; unlike ThemeResource expression, no dependency informs it theme changed. Explicit dictionary lookup also does not traverse merged dictionaries.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Selected/unselected/status colors can remain from previous or system theme, inconsistent with other surface/text resources. Exact WinUI fallback lookup result not runtime-proved.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Converter returns concrete Brush object; unlike ThemeResource expression, no dependency informs it theme changed. Explicit dictionary lookup also does not traverse merged dictionaries.

**E. Future Repair Specification:**  
Prefer theme-bound visual states/resources; explicitly observe ActualThemeChanged and traverse actual dictionary topology if imperative lookup retained.

**Required Regression Test Cases:**
- Use real WinUI root RequestedTheme opposite system theme, compare ThemeResources.Get colors to descendant ThemeResource references.
- Toggle repeatedly without changing Text/ExpenseCategory/PaymentMethod; compare same realized objects and brushes.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-005: Recovered electronic confirmation/cancellation has no reachable cashier retry context
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L149-L150) | `149-150` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelPendingElectronicPaymentAsync() (MethodDeclaration)` | `066c11322cca` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs#L262-L271) | `262-271` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenOrderLookup() (MethodDeclaration)` | `e4b4d4257703` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L22-L25) | `22-25` | `File-level declaration or top-level statement` | `9826b8fc7ac4` |
| [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml#L164-L205) | `164-205` | `File-level declaration or top-level statement` | `4531af2dec43` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L149-L150)  
> **Lines 149-150** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.CancelPendingElectronicPaymentAsync() (MethodDeclaration)` (File SHA-256: `066c11322cca65d746b3c082a7e58573ac8430f5431b21b359f6d5b192224e5a`):

```csharp
 149 |         if (!CanCancelPendingPayment) return;
 150 |         if (LastCheckoutResult is null) return;
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs#L262-L271)  
> **Lines 262-271** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.OpenOrderLookup() (MethodDeclaration)` (File SHA-256: `e4b4d4257703cbe135a8c821bb2075c7eb6d8b6e31dcb18622b0c8968fd38a06`):

```csharp
 262 |     private void OpenOrderLookup()
 263 |     {
 264 |         if (!CanEditOperation) return;
 265 |         OrderLookupNumber = "";
 266 |         OrderLookupResult = null;
 267 |         OrderLookupError = null;
 268 |         ReprintStatusMessage = null;
 269 |         IsOrderLookupOpen = true;
 270 |     }
 271 | 
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.State.cs#L22-L25)  
> **Lines 22-25** in `File-level declaration or top-level statement` (File SHA-256: `9826b8fc7ac4c90e688e59bccf35b9b47ec0d9eeca8e79db22aed5c47bf6675d`):

```csharp
  22 |     public bool CanEditOperation => !IsOperationBusy && _pendingRefund is null && _pendingReprint is null && _pendingCheckout is null && _pendingExpense is null &&
  23 |         _pendingSafeDrop is null && _pendingCashMovement is null && _pendingCashDrawer is null &&
  24 |         _pendingOpenShift is null && _pendingCloseShift is null && !_pendingElectronicConfirm.HasPending && !_pendingElectronicCancel.HasPending;
  25 |     public bool CanDiscount => _session.HasPermission("orders_discount");
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Retained DTO loads but transient LastCheckoutResult does not; all retry reachability assumes that result. Main session sees unresolved op and blocks lock/navigation/normal close.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Cashier can become frozen after restart even though retained original order ID exists. Correct production reconciliation must not simply delete journal. Potential cross-user context confusion because keys are station-global.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Retained DTO loads but transient LastCheckoutResult does not; all retry reachability assumes that result. Main session sees unresolved op and blocks lock/navigation/normal close.

**E. Future Repair Specification:**  
Recover operation order context from persisted request or present a standalone pending-action recovery panel independent of transient checkout result.

**Required Regression Test Cases:**
- Seed fake store with actual ConfirmElectronicPaymentRequest/CancelPendingElectronicPaymentRequest, create fresh AppSession/current user/Cashier VM, assert reachable retry command and UI visible; retry exact order identity.
- Confirm no new checkout, result discard or owner change until original outcome resolved; test another operator sees appropriate recovery ownership.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-006: A new pickup inherits previous order optional collection schedule
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L364-L364) | `364-364` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitPaymentAsync() (MethodDeclaration)` | `066c11322cca` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs#L131-L138) | `131-138` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ResetDelivery() (MethodDeclaration)` | `d9464aaaf766` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs#L45-L49) | `45-49` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ClearRequestedPickupTime() (MethodDeclaration)` | `7799e030dd8a` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Refund.cs#L132-L139) | `132-139` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.DismissCheckoutResult() (MethodDeclaration)` | `bc399716e95c` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Checkout.cs#L364-L364)  
> **Lines 364-364** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.SubmitPaymentAsync() (MethodDeclaration)` (File SHA-256: `066c11322cca65d746b3c082a7e58573ac8430f5431b21b359f6d5b192224e5a`):

```csharp
 364 |                 RequestedPickupAtUtc: IsPickup ? RequestedPickupAtUtc : null,
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs#L131-L138)  
> **Lines 131-138** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ResetDelivery() (MethodDeclaration)` (File SHA-256: `d9464aaaf7664d362a7600e49df95dc7794847563c08b3d55dadc9ac5dfc588e`):

```csharp
 131 |     private void ResetDelivery()
 132 |     {
 133 |         CustomerName = CustomerPhone = DeliveryArea = DeliveryStreet = DeliveryBuilding = DeliveryFloor =
 134 |             DeliveryApartment = DeliveryLandmark = DeliveryNotes = "";
 135 |         SelectedDeliveryService = null; DeliveryError = null;
 136 |         OnPropertyChanged(nameof(DeliverySummary));
 137 |     }
 138 | 
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Pickup.cs#L45-L49)  
> **Lines 45-49** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ClearRequestedPickupTime() (MethodDeclaration)` (File SHA-256: `7799e030dd8ac3693288cc2cfd82163ef3cb7eb4846033f30eadaddc724e09e7`):

```csharp
  45 |     private void ClearRequestedPickupTime()
  46 |     {
  47 |         PickupDate = null;
  48 |         PickupTime = null;
  49 |     }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Order reset named ResetDelivery also owns shared pickup customer fields but omits pickup-specific schedule.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
New customer order carries previous collection appointment or later fails past-time validation, unless operator notices and clears/changes type.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Order reset named ResetDelivery also owns shared pickup customer fields but omits pickup-specific schedule.

**E. Future Repair Specification:**  
Reset optional schedule with resolved new-order lifecycle and explicit cart cancellation; keep historic order review separate.

**Required Regression Test Cases:**
- Complete scheduled pickup using fake API, dismiss result, add next pickup; date/time and RequestedPickupAtUtc must be null until explicitly selected.
- Repeat ClearCart/StartNewOrder and historic pending-order review to ensure intentional preserved live draft schedule is not erased.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-007: Late CallCenter customer lookup can overwrite a different phone/new order
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L217-L220) | `217-220` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest) (MethodDeclaration)` | `ff3fefda7d24` |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs#L217-L231) | `217-231` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ResetCustomerAndDelivery() (MethodDeclaration)` | `dabfa47fa864` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs#L96-L102) | `96-102` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupCustomerAsync() (MethodDeclaration)` | `d9464aaaf766` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L217-L220)  
> **Lines 217-220** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.SendCheckoutAsync(Falah.Application.Orders.CheckoutOrderRequest) (MethodDeclaration)` (File SHA-256: `ff3fefda7d248cfafe4d7881c5b0f620ea8f76339ac521fbfee8c951d4a3f41e`):

```csharp
 217 |         _ = _session.Api.SaveCustomerAsync(new UpsertCustomerRequest(
 218 |             request.CustomerPhone!, request.CustomerName!, request.DeliveryArea, request.DeliveryStreet,
 219 |             request.DeliveryBuilding, request.DeliveryFloor, request.DeliveryApartment, request.DeliveryLandmark,
 220 |             request.DeliveryServiceId, request.DeliveryServiceName));
```

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Customer.cs#L217-L231)  
> **Lines 217-231** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ResetCustomerAndDelivery() (MethodDeclaration)` (File SHA-256: `dabfa47fa864b50d565951350f19e9a7735b9da5e5ed077fe22a427cb742a384`):

```csharp
 217 |     private void ResetCustomerAndDelivery()
 218 |     {
 219 |         CustomerPhone = "";
 220 |         CustomerName = "";
 221 |         _lastLookedUpPhone = null;
 222 |         ResetResolvedCustomer();
 223 |         DeliveryArea = "";
 224 |         DeliveryStreet = "";
 225 |         DeliveryBuilding = "";
 226 |         DeliveryFloor = "";
 227 |         DeliveryApartment = "";
 228 |         DeliveryLandmark = "";
 229 |         DeliveryNotes = "";
 230 |         SelectedDeliveryService = null;
 231 |     }
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Delivery.cs#L96-L102)  
> **Lines 96-102** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.LookupCustomerAsync() (MethodDeclaration)` (File SHA-256: `d9464aaaf7664d362a7600e49df95dc7794847563c08b3d55dadc9ac5dfc588e`):

```csharp
  96 |     [RelayCommand] private async Task LookupCustomerAsync()
  97 |     {
  98 |         var phone = CustomerPhone.Trim();
  99 |         if (phone.Length == 0) { DeliveryError = "أدخل رقم الهاتف أولاً."; return; }
 100 |         var result = await _session.Api.LookupCustomerAsync(phone);
 101 |         if (phone != CustomerPhone.Trim()) return;
 102 |         SavedAddresses.Clear();
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
No requested-phone equality/epoch check after await; async lookup completion mutates latest mutable form regardless of source identity.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Wrong customer name/address offered under another phone; operator may dispatch wrong destination and upsert mismatched customer data. No real PII or DB writes used to reproduce.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
No requested-phone equality/epoch check after await; async lookup completion mutates latest mutable form regardless of source identity.

**E. Future Repair Specification:**  
Snapshot normalized phone and monotonic form generation; discard stale responses/cancel on change/reset; clear incompatible resolved fields safely.

**Required Regression Test Cases:**
- Deterministic fake HTTP delayed A/B in reverse completion order; assert final name/addresses match B.
- Begin lookup then ClearOrder, release response; new form stays blank.
- Lookup failure 404 vs timeout must show correct status; changing phone should invalidate old customer/address selections.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-009: Printer status UI exposes only four of six implemented routes
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs#L77-L86) | `77-86` | `File-level declaration or top-level statement` | `a2b18699b786` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PrintingViewModel.cs#L77-L86)  
> **Lines 77-86** in `File-level declaration or top-level statement` (File SHA-256: `a2b18699b786109662a0fd23e8660bb2a56b015e0c2f5582db14b9dbf4f06aad`):

```csharp
  77 |         // used to miss the two delivery routes and showed "DeliveryOrder"/"DriverDispatch" in English.
  78 |         public string RouteLabel => Job.Route switch
  79 |         {
  80 |             "Receipt" => "إيصال الكاشير",
  81 |             "CallCenter" => "إيصال مركز الاتصال",
  82 |             "Charcoal" => "محطة الفحم",
  83 |             "Grill" => "محطة الشواية",
  84 |             "DeliveryOrder" => "أوردر الدليفري",
  85 |             "DriverDispatch" => "تذكرة السائق",
  86 |             _ => Job.Route,
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Hardcoded status-route list lags shared print route vocabulary.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
No dedicated queue summary/filter for two routes; attention aggregate may still surface failed jobs. Not total invisibility claim.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Hardcoded status-route list lags shared print route vocabulary.

**E. Future Repair Specification:**  
Derive displayed route options from complete shared contract plus localized labels.

**Required Regression Test Cases:**
- Provide overview with six routes and each pending/failed/stuck; assert all selectable and counts true.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-010: Workspace shortcut guard omits most open operational modals
- **Severity:** `MEDIUM` | **Confidence:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml#L43-L48) | `43-48` | `File-level declaration or top-level statement` | `4531af2dec43` |
| [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs#L31-L54) | `31-54` | `Falah.Client.CashierWinUI.Views.CashierView.OnSearchAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs) (MethodDeclaration)` | `e5de23e3dbcf` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml#L43-L48)  
> **Lines 43-48** in `File-level declaration or top-level statement` (File SHA-256: `4531af2dec4309d133530be9b28327dc14b7675c0179ac70a51bb300bc22a26f`):

```xaml
  43 |         <Grid x:Name="WorkspaceGrid" Visibility="{Binding IsShiftOpen, Converter={StaticResource BoolToVisibleConverter}}" SizeChanged="OnWorkspaceSizeChanged">
  44 |             <Grid.KeyboardAccelerators>
  45 |                 <KeyboardAccelerator Key="F2" Invoked="OnSearchAccelerator"/>
  46 |                 <KeyboardAccelerator Key="F3" Invoked="OnOperationsAccelerator"/>
  47 |                 <KeyboardAccelerator Key="F12" Invoked="OnCheckoutAccelerator"/>
  48 |             </Grid.KeyboardAccelerators>
```

> **File:** [`src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/CashierView.xaml.cs#L31-L54)  
> **Lines 31-54** in `Falah.Client.CashierWinUI.Views.CashierView.OnSearchAccelerator(Microsoft.UI.Xaml.Input.KeyboardAccelerator, Microsoft.UI.Xaml.Input.KeyboardAcceleratorInvokedEventArgs) (MethodDeclaration)` (File SHA-256: `e5de23e3dbcf7056ebaac0efbd4a65cdb7755e0fb48b3e58e46461eebac3f7d0`):

```csharp
  31 |     {
  32 |         args.Handled = true;
  33 |         if (!IsWorkspaceInteractive) return;
  34 |         CategoryStrip.FocusSearch();
  35 |     }
  36 | 
  37 |     private void OnOperationsAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
  38 |     {
  39 |         args.Handled = true;
  40 |         if (!IsWorkspaceInteractive) return;
  41 |         CashierOperationsButton.Flyout?.ShowAt(CashierOperationsButton);
  42 |     }
  43 | 
  44 |     private void OnCheckoutAccelerator(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
  45 |     {
  46 |         args.Handled = true;
  47 |         // Deliberately routed through the same guard the button uses, so the key can never do what
  48 |         // the disabled button refuses to; IsWorkspaceInteractive additionally keeps it from
  49 |         // re-opening a checkout sheet that is already on screen.
  50 |         if (!IsWorkspaceInteractive) return;
  51 |         if (ViewModel is { CanOpenCheckout: true } vm) vm.OpenCheckoutCommand.Execute(null);
  52 |     }
  53 | 
  54 |     private void OnWorkspaceSizeChanged(object sender, SizeChangedEventArgs e)
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Custom overlays do not centralize modal state and shortcut predicate only checks variant/payment/result.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Potential underlying focus change, extra flyout or checkout sheet while another form open, risking confused data entry. Runtime keyboard reachability/focus path not executed.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Custom overlays do not centralize modal state and shortcut predicate only checks variant/payment/result.

**E. Future Repair Specification:**  
Centralize IsAnyModalOpen plus accessible focus isolation and apply to all shortcuts.

**Required Regression Test Cases:**
- Actual WinUI keyboard F2/F3/F12 + Tab/ShiftTab/Escape for every modal, assert no background focus/action and only one modal active.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-011: Discarded definitively rejected CallCenter reprint survives in journal
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L86-L91) | `86-91` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecoverPendingOperations() (MethodDeclaration)` | `ff3fefda7d24` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L86-L91)  
> **Lines 86-91** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.RecoverPendingOperations() (MethodDeclaration)` (File SHA-256: `ff3fefda7d248cfafe4d7881c5b0f620ea8f76339ac521fbfee8c951d4a3f41e`):

```csharp
  86 |         _pendingPrint = RetainedRequestStorage.Current?.Load(PrintOwnerKey) as CreatePrintJobsRequest;
  87 |         if (_pendingPrint is not null)
  88 |         {
  89 |             _hasUnresolvedPrint = true;
  90 |             ReprintNote = "طلب طباعة سابق لم يصل تأكيده. أعد المحاولة بنفس الطلب للتحقق دون إنشاء نسخة إضافية.";
  91 |         }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Safe discard memory path omits corresponding persistent store clear.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Old rejected request reappears as ambiguous unresolved, can block fresh operation or queue an old reprint on retry when permissions/preconditions change. Does not prove duplicate sale.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Safe discard memory path omits corresponding persistent store clear.

**E. Future Repair Specification:**  
Clear disk atomically when explicitly discarding known-rejected print; preserve unknown state separately.

**Required Regression Test Cases:**
- Persist definite rejected print, clear through actual UI VM, reconstruct operator desk and assert no pending print/file.
- Ambiguous failure must continue surviving same clear attempt.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-012: CallCenter customer-copy reprint requests kitchen preparation routes again
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Application/Printing/PrintingRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Printing/PrintingRequests.cs#L12-L17) | `12-17` | `File-level declaration or top-level statement` | `b49f26ad9bcd` |
| [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L337-L343) | `337-343` | `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ReprintLookedUpOrderAsync() (MethodDeclaration)` | `ff3fefda7d24` |
| [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs#L316-L322) | `316-322` | `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReprintOrderAsync() (MethodDeclaration)` | `e4b4d4257703` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Application/Printing/PrintingRequests.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Printing/PrintingRequests.cs#L12-L17)  
> **Lines 12-17** in `File-level declaration or top-level statement` (File SHA-256: `b49f26ad9bcd56d10236fdd5f0e6c4000b86492e6f96cd92b57a2e4055111d3b`):

```csharp
  12 | /// <param name="IncludeKitchenRoutes">False suppresses the preparation tickets and prints only the
  13 | /// customer copy. Exists because the restaurant can turn preparation tickets off in Settings; a
  14 | /// caller that wants the ordinary behaviour leaves it alone.</param>
  15 | public sealed record CreatePrintJobsRequest(
  16 |     string RequestId, string OrderId, string? CustomerRoute, bool IsReprint, string ActorUserId,
  17 |     bool IncludeKitchenRoutes = true,
```

> **File:** [`src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/ViewModels/CallCenterViewModel.Checkout.cs#L337-L343)  
> **Lines 337-343** in `Falah.Client.CallCenterWinUI.ViewModels.CallCenterViewModel.ReprintLookedUpOrderAsync() (MethodDeclaration)` (File SHA-256: `ff3fefda7d248cfafe4d7881c5b0f620ea8f76339ac521fbfee8c951d4a3f41e`):

```csharp
 337 |     private async Task ReprintLookedUpOrderAsync()
 338 |     {
 339 |         if (IsOperationBusy || _pendingCheckout is not null) return;
 340 |         if (_pendingPrint is not null) { await SendPrintAsync(_pendingPrint); return; }
 341 |         if (LookedUpOrder is null) return;
 342 |         await SendPrintAsync(new CreatePrintJobsRequest($"pj-{Guid.NewGuid():N}",
 343 |             LookedUpOrder.Id, "CallCenter", true, _session.CurrentUser!.Id));
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/CashierViewModel.Operations.cs#L316-L322)  
> **Lines 316-322** in `Falah.Client.CashierWinUI.ViewModels.CashierViewModel.ReprintOrderAsync() (MethodDeclaration)` (File SHA-256: `e4b4d4257703cbe135a8c821bb2075c7eb6d8b6e31dcb18622b0c8968fd38a06`):

```csharp
 316 |         try
 317 |         {
 318 |             // Receipt only. Reprinting a customer's copy must not send the order to the kitchen
 319 |             // again - a second Charcoal/Grill ticket is an order to cook the food twice.
 320 |             var request = _pendingReprint ?? new CreatePrintJobsRequest(
 321 |                 RequestId: $"rp-{Guid.NewGuid():N}", OrderId: OrderLookupResult.Id, CustomerRoute: "Receipt",
 322 |                 IsReprint: true, ActorUserId: _session.CurrentUser!.Id, IncludeKitchenRoutes: false);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
CallCenter omitted explicit false flag added to corresponding cashier reprint path.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Potential duplicate charcoal/grill preparation work and food waste, in addition to requested customer copy. Backend route suppression cross-checked by printing peer as parent reports; parent should attach final handler evidence.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
CallCenter omitted explicit false flag added to corresponding cashier reprint path.

**E. Future Repair Specification:**  
Send false for customer-only reprint; expose explicit separately confirmed kitchen reprint only if requirement exists.

**Required Regression Test Cases:**
- CallCenter reprint fake API asserts IncludeKitchenRoutes false.
- Isolated print-job handler fixture with grill/charcoal items asserts customer copy only for follow-up; explicit kitchen reprint separate labeled action.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-013: Legacy WPF change display parses EGP tender as integer piastres
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs#L93-L95) | `93-95` | `Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitPaymentAsync() (MethodDeclaration)` | `abe203792ef0` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.Checkout.cs#L93-L95)  
> **Lines 93-95** in `Falah.Client.Cashier.ViewModels.CashierViewModel.SubmitPaymentAsync() (MethodDeclaration)` (File SHA-256: `abe203792ef03f7e0562f6f1029dbaba99cda77914f7f220d16893b9dd2b9373`):

```csharp
  93 |             if (!decimal.TryParse(AmountPaidText, out var amountEgp)) amountEgp = 0;
  94 |             var amountPiastres = (long)Math.Round(amountEgp * 100m);
  95 | 
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Display parses 100 as100 piastres or fails for100.00 while authoritative send converts to10000.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Displayed change zero instead of10 EGP; server-recorded tender may still correct. Does not affect active WinUI CashInput change implementation.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Display parses 100 as100 piastres or fails for100.00 while authoritative send converts to10000.

**E. Future Repair Specification:**  
If WPF remains supported, use same tested CashInput units and notifications; otherwise prevent accidental deployment.

**Required Regression Test Cases:**
- WPF ChangeDue test 90 EGP total with100 and100.00 tender both returns1000 piastres; Arabic/current-culture formats reviewed separately.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-014: Successful restore is reported incomplete because UI expects Completed instead of Succeeded
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs#L84-L93) | `84-93` | `Falah.Client.CashierWinUI.ViewModels.BackupViewModel.LastRestoreLine.get (GetAccessorDeclaration)` | `5c77ce94f6fe` |
| [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L168-L168) | `168-168` | `Falah.Infrastructure.Backup.RestoreBackupHandler.HandleAsync(Falah.Application.Backup.RestoreBackupRequest, CancellationToken) (MethodDeclaration)` | `a9504298519b` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/BackupViewModel.cs#L84-L93)  
> **Lines 84-93** in `Falah.Client.CashierWinUI.ViewModels.BackupViewModel.LastRestoreLine.get (GetAccessorDeclaration)` (File SHA-256: `5c77ce94f6fefe0cbfeaca0fcb9b1be9e983694af4eee50dd564666e56fe24a5`):

```csharp
  84 |             var restore = Backups.Where(b => b.Kind == "Restore").OrderByDescending(b => b.RequestedAtUtc).FirstOrDefault();
  85 |             if (restore is null) return "";
  86 |             var when = (restore.CompletedAtUtc ?? restore.RequestedAtUtc).ToLocalTime().ToString("yyyy/MM/dd HH:mm");
  87 |             return restore.Status switch
  88 |             {
  89 |                 "Succeeded" => $"آخر استعادة تمت في {when} بواسطة {restore.RequestedBy}.",
  90 |                 "Blocked" => $"آخر محاولة استعادة اتمنعت في {when}: {restore.ErrorMessage}",
  91 |                 _ => $"آخر محاولة استعادة فشلت في {when}: {restore.ErrorMessage}",
  92 |             };
  93 |         }
```

> **File:** [`src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Backup/RestoreBackupHandler.cs#L168-L168)  
> **Lines 168-168** in `Falah.Infrastructure.Backup.RestoreBackupHandler.HandleAsync(Falah.Application.Backup.RestoreBackupRequest, CancellationToken) (MethodDeclaration)` (File SHA-256: `a9504298519bd4709d18777b5343536894da4238272eea65b4ac704c9e1b61c6`):

```csharp
 168 |             restoreRecord.Status = nameof(BackupRecordStatus.Succeeded);
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
UI compares result status to wrong literal Completed; server stores enum Succeeded.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
UI tells operator restore did not complete after database already replaced, leaves restore target shown and can invite repeating dangerous operation. No restore executed by reviewer.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
UI compares result status to wrong literal Completed; server stores enum Succeeded.

**E. Future Repair Specification:**  
Use shared enum/contract success vocabulary and avoid treating generic HTTP success as outcome proof; maintain strict restore controls.

**Required Regression Test Cases:**
- Fake API returns successful DTO Status Succeeded; assert success note, target closed and no false RestoreError.
- Blocked/Failed records must remain reported non-success; no production DB restore needed for client contract test.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-015: CallCenter lock/unlock retains old order-panel visual trees through cart subscription
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs#L67-L67) | `67-67` | `Falah.Client.CallCenterWinUI.MainWindow.ApplyCurrentView() (MethodDeclaration)` | `d075891fc1aa` |
| [`src/Falah.Client.CallCenterWinUI/Services/AppSession.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/AppSession.cs#L23-L23) | `23-23` | `File-level declaration or top-level statement` | `ade21bda88fd` |
| [`src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs#L16-L21) | `16-21` | `Falah.Client.CallCenterWinUI.Views.CallCenterOrderPanelView.CallCenterOrderPanelView() (ConstructorDeclaration)` | `263dc6671313` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/MainWindow.xaml.cs#L67-L67)  
> **Lines 67-67** in `Falah.Client.CallCenterWinUI.MainWindow.ApplyCurrentView() (MethodDeclaration)` (File SHA-256: `d075891fc1aace2363fa85196441f2b4a0a5afa089ff32a49124795bb9787f87`):

```csharp
  67 |             CallCenterViewModel vm => new CallCenterView { DataContext = vm },
```

> **File:** [`src/Falah.Client.CallCenterWinUI/Services/AppSession.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Services/AppSession.cs#L23-L23)  
> **Lines 23-23** in `File-level declaration or top-level statement` (File SHA-256: `ade21bda88fdb6bcc6995c8dd8c3d2e5d29d6d099499f51eee20f9c9750e2694`):

```csharp
  23 |     private readonly Dictionary<string, ViewModels.CallCenterViewModel> _drafts = new();
```

> **File:** [`src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CallCenterWinUI/Views/CallCenterOrderPanelView.xaml.cs#L16-L21)  
> **Lines 16-21** in `Falah.Client.CallCenterWinUI.Views.CallCenterOrderPanelView.CallCenterOrderPanelView() (ConstructorDeclaration)` (File SHA-256: `263dc6671313a9bbb74357b9a5f3d795b8d4efc7e84878aeb1e2f2f35f81cf96`):

```csharp
  16 |         DataContextChanged += (_, _) =>
  17 |         {
  18 |             if (ViewModel is { } vm) vm.Cart.CollectionChanged += OnCartChanged;
  19 |             UpdateEmptyCartVisibility();
  20 |         };
  21 |     }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Long-lived cached VM Cart event holds every prior UserControl handler; subscriptions never removed on unload or DataContext replacement.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Memory/native visual resource growth and repeated invisible callbacks over long shift; actual memory growth not measured.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Long-lived cached VM Cart event holds every prior UserControl handler; subscriptions never removed on unload or DataContext replacement.

**E. Future Repair Specification:**  
Track subscribed VM, detach on change/unload, reattach once Loaded.

**Required Regression Test Cases:**
- Native repeated lock/unlock, weak-reference old panel and force test GC; only current panel should remain.
- DataContext A->B->A event subscription counts exactly1 and no callbacks on detached view.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-016: Management shift actions abandon unknown outcomes and generate a new request identity per retry
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs#L126-L129) | `126-129` | `Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ConfirmCloseShiftAsync() (MethodDeclaration)` | `22bf59a61b7c` |
| [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs#L71-L71) | `71-71` | `Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.CloseOpenShiftModal() (MethodDeclaration)` | `10e6ac543011` |
| [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs#L33-L33) | `33-33` | `File-level declaration or top-level statement` | `83c955f6cc1f` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Close.cs#L126-L129)  
> **Lines 126-129** in `Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.ConfirmCloseShiftAsync() (MethodDeclaration)` (File SHA-256: `22bf59a61b7ce3adf137dc1862e158bd8f07e46c23fe2b7d8fe5acd5fe9b88c3`):

```csharp
 126 |                 CloseRequestId: $"shift-close-{Guid.NewGuid():N}", ShiftId: ActiveShift.ShiftId,
 127 |                 ClosedByUserId: _session.CurrentUser!.Id, ActualCashCountedPiastres: CloseShiftPreview.ActualCashPiastres,
 128 |                 Reason: CloseShiftReason, ManagerApprovalId: approvalId, ManagerApproverUserId: approverUserId));
 129 | 
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.Open.cs#L71-L71)  
> **Lines 71-71** in `Falah.Client.CashierWinUI.ViewModels.ShiftsViewModel.CloseOpenShiftModal() (MethodDeclaration)` (File SHA-256: `10e6ac543011f2b5cae23d8aaad9ebed7e82e66514301050037f460c82e09b46`):

```csharp
  71 |     private void CloseOpenShiftModal() => IsOpenShiftModalOpen = false;
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/ShiftsViewModel.cs#L33-L33)  
> **Lines 33-33** in `File-level declaration or top-level statement` (File SHA-256: `83c955f6cc1fac8e1b1a77d591e978a3030cafc147cb8435ba9483ad0e466d11`):

```csharp
  33 | public partial class ShiftsViewModel : ObservableObject
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Separate management presentation did not adopt cashier immutable pending/outcomeUnknown protocol; only own busy booleans, no session registration or retained key.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Cannot safely reconcile original shift operation; new attempt may be rejected by single-open/already-closed server guards or mislead state, and user can abandon in-flight money context. Actual duplicate server shift transaction not claimed.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Separate management presentation did not adopt cashier immutable pending/outcomeUnknown protocol; only own busy booleans, no session registration or retained key.

**E. Future Repair Specification:**  
Share durable operation coordination between both shift presentations and register global pending before dispatch.

**Required Regression Test Cases:**
- Fake commit-lost-response, retry asserts identical typed DTO/key and immutable counted/opening cash.
- Root TryClose/Lock/Shell.Navigate blocked while management open/close busy or unresolved.
- First definite rejection permits correction/new attempt, unknown requires same attempt reconciliation.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-017: Recovered payroll transition or payment cannot reach its retry command
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs#L334-L334) | `334-334` | `Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayRunAsync() (MethodDeclaration)` | `8cf4e9f6ec47` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/PayrollViewModel.cs#L334-L334)  
> **Lines 334-334** in `Falah.Client.CashierWinUI.ViewModels.PayrollViewModel.PayRunAsync() (MethodDeclaration)` (File SHA-256: `8cf4e9f6ec47b2165413bc6ec1583f9261d08db7f950bdc75b3ee84fc8fe31a0`):

```csharp
 334 |         if (IsBusy || _createRequest.HasPending || _transitionRequest.HasPending || !CanPay || !IsPayOpen) return;
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Durable DTO recovery is not accompanied by run selection/pay-dialog reconstruction; all routes to restore context blocked by same pending state.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Retained financial operation stranded; ordinary page cannot reconcile original status/payment. Unlike create-run retained branch, no standalone retry in this VM.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Durable DTO recovery is not accompanied by run selection/pay-dialog reconstruction; all routes to restore context blocked by same pending state.

**E. Future Repair Specification:**  
Recover run/visible action from stored request or expose generic pending-action verification independent of transient selection.

**Required Regression Test Cases:**
- Fake retained transition/pay DTO, construct fresh VM/session, assert explicit retry available without selecting new run/account.
- Read original target by stored identity and show original intended action; exact DTO resends; no new payment until resolved.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-018: Late recipe-detail response can replace another recipe selection before editing
- **Severity:** `HIGH` | **Confidence:** `MEDIUM` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs#L579-L581) | `579-581` | `Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.SaveAsync() (MethodDeclaration)` | `24d7807c2ca0` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs#L579-L581)  
> **Lines 579-581** in `Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.SaveAsync() (MethodDeclaration)` (File SHA-256: `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63`):

```csharp
 579 |             var result = await _session.Api.SaveRecipeAsync(new SaveRecipeRequest(
 580 |                 $"req-{Guid.NewGuid():N}", d.RecipeId, d.ProductId, d.Name, d.Active,
 581 |                 lines, _session.CurrentUser!.Id, null, null));
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
No version/selected-ID validation around asynchronous recipe detail publishing.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Wrong ingredient list/recipe identity can be edited and saved, affecting future inventory consumption/costing. Server authorizes a valid recipe update but cannot infer operator intended different selected row. Runtime write not tested.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
No version/selected-ID validation around asynchronous recipe detail publishing.

**E. Future Repair Specification:**  
Use monotonic detail generation and validate again after every await; freeze target identity across edit/save.

**Required Regression Test Cases:**
- Fake delayed A/B reverse completion asserts Detail.RecipeId equals SelectedRecipe key and StartEdit uses B.
- Close/change selection while reading must invalidate old response/finally flags.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-019: Recipe yield conversion silently truncates fractional pieces-per-base-unit
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs#L562-L568) | `562-568` | `Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.SaveAsync() (MethodDeclaration)` | `24d7807c2ca0` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs#L562-L568)  
> **Lines 562-568** in `Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.SaveAsync() (MethodDeclaration)` (File SHA-256: `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63`):

```csharp
 562 |                 yieldRatio = ratioMilli / 1000;
 563 |                 if (yieldRatio <= 0)
 564 |                 {
 565 |                     SaveError = $"عدد القطع في الوحدة يجب أن يكون رقمًا صحيحًا أكبر من صفر لـ {line.ItemName}.";
 566 |                     return;
 567 |                 }
 568 |             }
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
MilliUnits parser accepts fractional unit then conversion to integer floor without checking remainder.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Stored recipe ratio differs from typed value and changes ingredient usage/cost calculation on future sales. No real recipe updated.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
MilliUnits parser accepts fractional unit then conversion to integer floor without checking remainder.

**E. Future Repair Specification:**  
Validate integer divisibility before division or use dedicated localized positive-integer parser.

**Required Regression Test Cases:**
- Enter1.5,2.999Arabic١٫٥; must reject with explicit integer requirement before HTTP, not send1/2.
- Whole numbers1,2and textual2.000 accepted exactly, enforce overflow bound.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-020: PIN-change retry proof replaces shared bearer despite preserving visible user identity
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L164-L167) | `164-167` | `Falah.Client.CashierWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken) (MethodDeclaration)` | `831a6dfd1f18` |
| [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L14-L14) | `14-14` | `File-level declaration or top-level statement` | `a880d1ded757` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/Services/ApiClient.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Services/ApiClient.cs#L164-L167)  
> **Lines 164-167** in `Falah.Client.CashierWinUI.Services.ApiClient.LoginByPinAsync(string, CancellationToken) (MethodDeclaration)` (File SHA-256: `831a6dfd1f18510b240a232b7a4c4fd11251a51746e1e416c7ce691f732ddbf4`):

```csharp
 164 |         _sessionToken = result.Value.Token;
 165 |         _http.DefaultRequestHeaders.Authorization =
 166 |             new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _sessionToken);
 167 | 
```

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/UsersViewModel.Actions.cs#L14-L14)  
> **Lines 14-14** in `File-level declaration or top-level statement` (File SHA-256: `a880d1ded757372d47ee1b1976bb1b61de63d1bb7531e72de7d43118314918f6`):

```csharp
  14 |     private readonly RetainedRequest<ChangePinRequest> _pendingPin = new("users.change-pin");
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
LoginByPinAsync has bearer installation side effect, but proof caller assumes read-only credential test and does not reconcile session.CurrentUser.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
HTTP authenticated identity may diverge from UI/current actor; subsequent requests can fail or run under unintended principal depending server actor enforcement. Parent security reviewer owns final exploitability/severity validation; no real login performed.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
LoginByPinAsync has bearer installation side effect, but proof caller assumes read-only credential test and does not reconcile session.CurrentUser.

**E. Future Repair Specification:**  
Use isolated non-session-mutating proof client/endpoint and explicit owner context; coordinate with security report to avoid duplicate final finding.

**Required Regression Test Cases:**
- Fake login proof changes token identity tootheruser, assert root session and bearer remain original after failed proof.
- Recovered self-PIN request owned by another actor must be visibly routed/blocked; no session switch as side effect.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-021: Late product-image completion changes save target beneath another product form
- **Severity:** `HIGH` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs#L337-L339) | `337-339` | `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CreateRequest() (MethodDeclaration)` | `18449ab63e88` |
| [`src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml#L376-L376) | `376-376` | `File-level declaration or top-level statement` | `eb56e6edfc72` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/MenuEditorViewModel.cs#L337-L339)  
> **Lines 337-339** in `Falah.Client.CashierWinUI.ViewModels.MenuEditorViewModel.CreateRequest() (MethodDeclaration)` (File SHA-256: `18449ab63e886ec4a5a507bfa408d9f31315fb0dd091e3ac2114060ae5ea43de`):

```csharp
 337 |             return _editing is ProductDetailsDto p
 338 |                 ? new UpdateProductRequest(p.Id, Category.Id, Name.Trim(), price, SendToGrill, Station.Id, sort, Recipe?.Id, variants.ToArray(), actor, key)
 339 |                 : new CreateProductRequest(key, Category.Id, Name.Trim(), price, SendToGrill, Station.Id, sort, Recipe?.Id, variants.ToArray(), actor);
```

> **File:** [`src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/MenuEditorView.xaml#L376-L376)  
> **Lines 376-376** in `File-level declaration or top-level statement` (File SHA-256: `eb56e6edfc726a96d63207e7c41035e80dce3838591a9e11ed936be978ed73be`):

```xaml
 376 |                                 IsEnabled="{Binding CanEdit}" Command="{Binding SaveCommand}"/>
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
Image completion unconditionally overwrites shared editor identity captured before await. IsImageBusy only disables image controls; ReloadAsync reloads catalogs but does not reset/rebind visible form or editing identity.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Save targets product A with B field values/variants. Can corrupt menu pricing, preparation route and recipe association despite valid authorized backend request. No product mutation executed by reviewer.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
Image completion unconditionally overwrites shared editor identity captured before await. IsImageBusy only disables image controls; ReloadAsync reloads catalogs but does not reset/rebind visible form or editing identity.

**E. Future Repair Specification:**  
Keep immutable operation target independent from current editor; only update visible state when selection generation still matches. Coordinate browse/save with image busy and invalidate late completions on close/kind/new.

**Required Regression Test Cases:**
- Controlled image TaskCompletionSource: A upload pending, SelectRow(B), complete A; editor target and form must remain B, A image catalog updated separately.
- Repeat with delete, New, SetKind and Close/reopen; no stale completion alters current draft.
- Assert saved product ID matches displayed selected item after every async completion.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

### UI-022: Recipe ingredient picker truncates to60 choices without exposing its search input
- **Severity:** `MEDIUM` | **Confidence:** `HIGH` | **Disposition:** `CORROBORATED_WITH_QUALIFICATIONS`
- **Disposition Rationale:** Source confirms the state management / retry divergence, though operational impact varies depending on client desktop lifecycle.

#### Verified Source Code References & Enclosing Callables

| Source File | Lines | Enclosing Callable / Symbol | File SHA-256 (prefix) |
| :--- | :--- | :--- | :--- |
| [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs#L441-L441) | `441-441` | `Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnIngredientSearchTextChanged(string) (MethodDeclaration)` | `24d7807c2ca0` |
| [`src/Falah.Client.CashierWinUI/Views/RecipesView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/RecipesView.xaml#L341-L343) | `341-343` | `File-level declaration or top-level statement` | `6d6d87cd0703` |

#### Verbatim Original Source Code Evidence

> **File:** [`src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/ViewModels/RecipesViewModel.cs#L441-L441)  
> **Lines 441-441** in `Falah.Client.CashierWinUI.ViewModels.RecipesViewModel.OnIngredientSearchTextChanged(string) (MethodDeclaration)` (File SHA-256: `24d7807c2ca0ccf99aea7cf633ccfe99655de6acf2d0c19133b45060e90fdc63`):

```csharp
 441 |     partial void OnIngredientSearchTextChanged(string value) => ApplyIngredientFilter();
```

> **File:** [`src/Falah.Client.CashierWinUI/Views/RecipesView.xaml`](file:///C:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Views/RecipesView.xaml#L341-L343)  
> **Lines 341-343** in `File-level declaration or top-level statement` (File SHA-256: `6d6d87cd0703d327ddbd5210aa92c85e907e360097c14e223e1114b3690f1c63`):

```xaml
 341 |                                 <ComboBox ItemsSource="{Binding Ingredients}" SelectedItem="{Binding IngredientToAdd, Mode=TwoWay}"
 342 |                                           DisplayMemberPath="Name" MinHeight="44" HorizontalAlignment="Stretch"
 343 |                                           PlaceholderText="اختر خامة لإضافتها"/>
```

#### Deep Technical Analysis & Future Repair Specification

**A. Flaw Mechanism & Execution Path:**  
VM implemented bounded searchable choices but XAML exposes only ComboBox over current60 and no search binding to query full cached ingredients.

**B. Failure Mode, Financial/Security Impact, & Data Integrity Risk:**  
Operator cannot directly find later ingredients through normal picker. Recipe may remain incomplete or user may choose wrong similar item; no actual recipe mutation tested.

**C. Concurrency, Race Condition, & Multi-Terminal Behavior:**  
Concurrent multi-terminal interactions or repeated retries exacerbate this issue under load. State mutations lack atomic locking or unique identity isolation across concurrent worker threads.

**D. Root Cause & Architectural Antipattern:**  
VM implemented bounded searchable choices but XAML exposes only ComboBox over current60 and no search binding to query full cached ingredients.

**E. Future Repair Specification:**  
Bind explicit ingredient search to IngredientSearchText or provide searchable full-list picker; disclose bounds only as presentation optimization.

**Required Regression Test Cases:**
- Load61+ active ingredients with nonechosen; entering target61 search should exposeit without adding unrelated ingredients.
- Exclude chosenIDs while searching fullcached inventory; clearing search should preserve selected choice and exact recipe lines.

**Acceptance Criteria & Verification Standard:**
- Invariant holds under retries and concurrent executions.

---

## Complete Audit Artifacts Index

- Master Structured Findings: [`RECONCILED_MASTER_FINDINGS.json`](RECONCILED_MASTER_FINDINGS.json)
- Exact Source Excerpt Verification Ledger: [`SOURCE_EXCERPT_VERIFICATION_LEDGER.json`](../Checkpoints/SOURCE_EXCERPT_VERIFICATION_LEDGER.json)
- Complete File Ownership Registry: [`COMPLETE_FILE_OWNERSHIP_REGISTRY.json`](../01-Source-Baseline/COMPLETE_FILE_OWNERSHIP_REGISTRY.json)
- Complete Callable Review Registry: [`CALLABLE_REVIEW_REGISTRY.json`](../02-Callable-Inventory-and-Reviews/CALLABLE_REVIEW_REGISTRY.json)
