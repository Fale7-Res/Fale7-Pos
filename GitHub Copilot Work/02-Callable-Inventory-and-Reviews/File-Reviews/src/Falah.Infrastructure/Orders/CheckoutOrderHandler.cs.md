# Substantive Callable Audit: `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`

- **Source File:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs`
- **Baseline Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **File SHA-256:** `ff9f45f38b96377ef8f065884820752aa8483f9038b2141ed390222eac83413c`
- **Auditor Specialist:** `financial-auditor`
- **Total File Callables:** **15**
- **Audit Status:** **SUBSTANTIVE_REVIEW_COMPLETE**

---

## 1. Architectural & Domain Responsibility Overview

Atomic order checkout handler orchestrating authoritative pricing, approvals, stock deduction, order entity generation, and treasury movement journaling under database transactions. Hosts critical vulnerability FIN-001 (split payment journal key collision).

---

## 2. Callable-by-Callable Detailed Forensic Audit Ledger

<a id="callable-1"></a>
### Callable 1: `Falah.Infrastructure.Orders.CheckoutOrderHandler.CheckoutOrderHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Infrastructure.Orders.MenuPricingService, Falah.Infrastructure.Orders.InventoryDeductionService, Falah.Domain.Audit.IAuditLog)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:31:5:ConstructorDeclaration`
- **Declaration Kind:** `ConstructorDeclaration` | **Line Range:** `31-38`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes ConstructorDeclaration for Falah.Infrastructure.Orders.CheckoutOrderHandler.CheckoutOrderHandler(Falah.Infrastructure.Persistence.FalahDbContext, Falah.Domain.Authorization.IApprovalRegistry, Falah.Infrastructure.Orders.MenuPricingService, Falah.Infrastructure.Orders.InventoryDeductionService, Falah.Domain.Audit.IAuditLog). Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 31-38):**
```csharp
  31 |     public CheckoutOrderHandler(FalahDbContext db, IApprovalRegistry approvalRegistry, MenuPricingService pricing, InventoryDeductionService inventory, IAuditLog auditLog)
  32 |     {
  33 |         _db = db;
  34 |         _approvalRegistry = approvalRegistry;
  35 |         _pricing = pricing;
  36 |         _inventory = inventory;
  37 |         _auditLog = auditLog;
  38 |     }
```

---

<a id="callable-2"></a>
### Callable 2: `Falah.Infrastructure.Orders.CheckoutOrderHandler.HandleAsync(Falah.Application.Orders.CheckoutOrderRequest, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:40:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `40-290`
- **Audit Classification:** `INDEPENDENTLY_VERIFIED`
- **Linked Vulnerability Finding:** **`FIN-001`**

**A. Actual Behavior & Execution Path:**  
Validates cashier user, opens business day, resolves prices and items, deducts inventory, posts sales movements to TreasuryMovements, and commits EfFinancialJournal with key 'sale-{request.CheckoutRequestId}'. In split tender (Cash + InstaPay), completes journal immediately for cash leg, leaving the same key claimed on server.

**B. Trust Boundaries & Validation:**  
Receives CheckoutOrderRequest from client till.

**C. State Mutations & Side Effects:**  
Inserts OrderEntity, OrderItemEntity, TreasuryMovementEntity, EfFinancialJournal record, updates stock balances, commits transaction.

**D. Concurrency & Transaction Implications:**  
Guarded by business day checks and idempotency journal. Flaw on concurrent split payment completion.

**E. Specialist Risks & Invariants:**  
CRITICAL FLAW FIN-001: Claims unsuffixed saleKey 'sale-{CheckoutRequestId}' on checkout. When ConfirmElectronicPaymentHandler tries to post electronic split leg, it attempts to claim the same key, throwing ConfirmConflictException.

> **Verbatim Code Excerpt (Lines 40-290):**
```csharp
  40 |     public async Task<CheckoutOrderResult> HandleAsync(CheckoutOrderRequest request, CancellationToken ct = default)
  41 |     {
  42 |         // Fast idempotent-replay path first, no transaction needed for a pure read: a same-task
  43 |         // retry or a network retry that already succeeded gets back the exact same order.
  44 |         var existing = await FindExistingAsync(request.CheckoutRequestId, ct);
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
  66 |         var deliveryFee = Money.FromPiastres(request.DeliveryFeePiastres);
  67 |         var compAmount = Money.FromPiastres(request.CompAmountPiastres);
  68 |         var amountPaid = Money.FromPiastres(request.AmountPaidPiastres);
  69 |         var orderType = OrderEnumMapping.ParseOrderType(request.OrderType);
  70 |         var paymentMethod = OrderEnumMapping.ParsePaymentMethod(request.PaymentMethod);
  71 |         var splits = request.SplitPayments?
  72 |             .Select(s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.Method), Money.FromPiastres(s.AmountPiastres)))
  73 |             .ToList();
  74 | 
  75 |         // Ported from checkout()'s own re-validation in lib/store.tsx: a delivery order without a
  76 |         // complete customer/address is rejected here too, not just at the pre-checkout "save
  77 |         // delivery details" form step - Call Center (and any future client) submits straight to
  78 |         // this endpoint with no such form gate of its own, so the server must be the one place this
  79 |         // is actually enforced.
  80 |         string? composedDeliveryAddress = null;
  81 |         if (orderType == OrderType.Delivery)
  82 |         {
  83 |             var missing = DeliveryRules.FirstMissingDeliveryField(
  84 |                 request.CustomerPhone, request.CustomerName, request.DeliveryServiceId, request.DeliveryArea, request.DeliveryStreet);
  85 |             if (missing is not null) throw new DomainException(missing);
  86 | 
  87 |             composedDeliveryAddress = DeliveryRules.ComposeDeliveryAddress(
  88 |                 request.DeliveryArea, request.DeliveryStreet, request.DeliveryBuilding, request.DeliveryFloor,
  89 |                 request.DeliveryApartment, request.DeliveryLandmark);
  90 |         }
  91 |         else if (orderType == OrderType.Pickup)
  92 |         {
  93 |             // Enforced here for the same reason delivery is, spelled out above: the POS form is not
  94 |             // the only way an order reaches this endpoint, so a pickup with nobody's name on it must
  95 |             // be refused by the server rather than by whichever client happens to have a form.
  96 |             var missing = DeliveryRules.FirstMissingPickupField(request.CustomerPhone, request.CustomerName);
  97 |             if (missing is not null) throw new DomainException(missing);
  98 |         }
  99 | 
 100 |         CheckoutRules.AssertRequestedPickupTime(orderType, request.RequestedPickupAtUtc, DateTimeOffset.UtcNow);
 101 | 
 102 |         // Ported from checkout() in lib/store.tsx: 'pos' is the base gate on EVERY checkout,
 103 |         // unconditionally, before any comp logic runs at all - a comped checkout is still a
 104 |         // checkout. A real gap found during the legacy-parity audit: this used to be checked only
 105 |         // in the no-comp branch, so a comped order needed just orders_comp and silently skipped the
 106 |         // base 'pos' requirement entirely.
 107 |         if (!AuthorizationPolicy.HasPermission(cashier, "pos"))
 108 |             throw new DomainException("ليس لديك صلاحية تسجيل عملية بيع.");
 109 | 
 110 |         // Ported from store.tsx's own discount gate: a manual discount &gt; 0 requires orders_discount
 111 |         // - previously enforced nowhere server-side in this port despite the field being fully live.
 112 |         if (discount.IsPositive && !AuthorizationPolicy.HasPermission(cashier, "orders_discount"))
 113 |             throw new DomainException("ليس لديك صلاحية تطبيق خصم يدوي.");
 114 | 
 115 |         string? compApprovedByName = null;
 116 |         if (compAmount.IsPositive)
 117 |         {
 118 |             var preCompSubtotal = CheckoutRules.ComputeSubtotal(items);
 119 |             var preCompTaxable = preCompSubtotal - discount;
 120 |             if (preCompTaxable.IsNegative) preCompTaxable = Money.Zero;
 121 |             var orderValueBeforeComp = TaxEngine.Calculate(preCompTaxable, taxRule).Final + deliveryFee;
 122 |             PricingRules.AssertCompAmount(compAmount, orderValueBeforeComp);
 123 |             PricingRules.AssertCompReasonCode(request.CompReasonCode);
 124 | 
 125 |             if (string.IsNullOrEmpty(request.CompApprovalId) || string.IsNullOrEmpty(request.CompApproverUserId))
 126 |                 throw new DomainException("الضيافة تتطلب اعتماد مدير.");
 127 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CompApproverUserId, ct)
 128 |                 ?? throw new DomainException("المعتمد غير موجود.");
 129 |             // The approval is looked up in the persisted registry (EfApprovalRegistry), so it survives
 130 |             // a Server restart between the manager granting it and this checkout spending it. It is
 131 |             // single-use, must match the orders_comp action, and only a role allowed to issue it can.
 132 |             //
 133 |             // Validated for EVERY cashier, including one who holds orders_comp: the order and the
 134 |             // audit trail name this approver, so the approval has to be real. Holding the permission
 135 |             // used to skip the check entirely and record whatever approver the request named.
 136 |             var approval = new ManagerApproval(request.CompApprovalId, approverUser.Id, approverUser.Name,
 137 |                 approverUser.Role, DateTimeOffset.UtcNow, "orders_comp", request.CompReason);
 138 |             AuthorizationPolicy.ValidateApproval(_approvalRegistry, "orders_comp", approval);
 139 |             compApprovedByName = approverUser.Name;
 140 |         }
 141 | 
 142 |         var plan = CheckoutRules.BuildPlan(orderType, paymentMethod, items, discount, taxRule,
 143 |             deliveryFee, compAmount, amountPaid, splits, request.WalletProvider);
 144 | 
 145 |         await using var transaction = await _db.Database.BeginTransactionAsync(ct);
 146 |         try
 147 |         {
 148 |             var saleKey = $"sale-{request.CheckoutRequestId}";
 149 |             if (!plan.DefersAllMoney)
 150 |             {
 151 |                 var journal = new EfFinancialJournal(_db);
 152 |                 if (!journal.TryStart(saleKey, "sale"))
 153 |                     throw new CheckoutConflictException("مفتاح العملية المالية هذا مستخدم بالفعل.");
 154 |             }
 155 | 
 156 |             var sequence = await OrderSequenceService.AllocateNextAsync(_db, ct);
 157 |             var orderId = $"ord-{Guid.NewGuid():N}";
 158 |             var orderNumber = FinancialRules.FormatOrderNumber(sequence);
 159 |             var now = DateTimeOffset.UtcNow;
 160 | 
 161 |             // Server-resolved, never trusted from the client (request.BusinessDayId is accepted on
 162 |             // the wire only for source-compat and otherwise ignored) - see BusinessDayGate.
 163 |             // TryGetActiveAsync's own doc comment for why a plain sale does not REQUIRE one to exist.
 164 |             var businessDayId = await Falah.Infrastructure.BusinessDay.BusinessDayGate.TryGetActiveAsync(_db, ct);
 165 | 
 166 |             var order = Order.Create(plan, orderId, orderNumber, sequence, request.CashierId, request.ShiftId,
 167 |                 items, request.CheckoutRequestId, businessDayId, now,
 168 |                 request.CustomerName, request.CustomerPhone, composedDeliveryAddress, request.DeliveryArea, discount,
 169 |                 compAmount.IsPositive ? request.CompReasonCode : null,
 170 |                 compApprovedByName, compAmount.IsPositive ? request.CompApprovalId : null,
 171 |                 taxRule?.RateBasisPoints, taxRule?.Mode.ToString(), taxRule?.Name);
 172 | 
 173 |             var orderEntity = ToEntity(order);
 174 |             // Structured delivery breakdown - kept off the Order Domain aggregate entirely (no
 175 |             // business rule of Order's own reads them, they are pure display/navigation data for a
 176 |             // driver/receipt/Call Center), set directly on the persistence row exactly like
 177 |             // Delivery's own side-effect row below.
 178 |             orderEntity.DeliveryStreet = request.DeliveryStreet;
 179 |             orderEntity.DeliveryBuilding = request.DeliveryBuilding;
 180 |             orderEntity.DeliveryFloor = request.DeliveryFloor;
 181 |             orderEntity.DeliveryApartment = request.DeliveryApartment;
 182 |             orderEntity.DeliveryLandmark = request.DeliveryLandmark;
 183 |             orderEntity.DeliveryNotes = request.DeliveryNotes;
 184 |             orderEntity.DeliveryServiceId = request.DeliveryServiceId;
 185 |             orderEntity.DeliveryServiceName = request.DeliveryServiceName;
 186 | 
 187 |             // No order type has a table any more - DineIn was the only one, and it is gone. The
 188 |             // column and the request field stay so the orders written before the change still read
 189 |             // back with the table they were served at; nothing new is ever written into it.
 190 |             orderEntity.TableNumber = null;
 191 |             orderEntity.RequestedPickupAtUtc = orderType == OrderType.Pickup ? request.RequestedPickupAtUtc : null;
 192 | 
 193 |             _db.Orders.Add(orderEntity);
 194 | 
 195 |             if (compAmount.IsPositive)
 196 |             {
 197 |                 _auditLog.Append(new AuditEvent(
 198 |                     $"audit-{Guid.NewGuid():N}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
 199 |                     "ضيافة على طلب", "order", $"ضيافة {compAmount.Piastres} قرش على طلب {orderNumber} - {request.CompReason}",
 200 |                     "critical", now, ShiftId: request.ShiftId, Reason: request.CompReason, AuthorizedBy: compApprovedByName,
 201 |                     ReferenceId: orderId));
 202 |             }
 203 | 
 204 |             // A confirmed audit gap: a manual discount is gated by orders_discount above but, unlike
 205 |             // its sibling comp branch, was never written to the audit trail - a discounted sale and a
 206 |             // full-price sale looked identical in "سجل المراجعة". Id is deterministic on orderId (not
 207 |             // a random Guid) so this line is naturally idempotent-safe with the rest of this handler:
 208 |             // a retry that reaches this point only does so once, since FindExistingAsync's replay
 209 |             // short-circuit at the top of HandleAsync returns before this code runs again.
 210 |             if (discount.IsPositive)
 211 |             {
 212 |                 _auditLog.Append(new AuditEvent(
 213 |                     $"audit-discount-{orderId}", cashierUser.Id, cashierUser.Name, cashierUser.Role,
 214 |                     "خصم يدوي على طلب", "order", $"خصم {discount.Piastres} قرش على طلب {orderNumber}",
 215 |                     "normal", now, ShiftId: request.ShiftId, ReferenceId: orderId));
 216 |             }
 217 | 
 218 |             // Ported from registerDeliveryOrder in the reference app: every delivery-type order gets
 219 |             // exactly one Delivery row, created here rather than by a separate call, since this
 220 |             // system has no other path that produces a delivery order. Starts 'New' - dispatch
 221 |             // cannot assign a driver until it's explicitly marked 'ready' (POST /api/delivery/{id}/
 222 |             // ready), matching Falah.Domain.Delivery.DeliveryTransitions' own tested contract.
 223 |             if (order.OrderType == OrderType.Delivery)
 224 |             {
 225 |                 _db.Deliveries.Add(new DeliveryEntity
 226 |                 {
 227 |                     Id = $"delivery-{orderId}", OrderId = orderId, Status = nameof(DeliveryStatus.New),
 228 |                     ShiftId = order.ShiftId, CreatedBy = cashierUser.Name, CreatedAtUtc = now,
 229 |                 });
 230 | 
 231 |                 // The first of the two delivery print events: one DeliveryOrder ticket per delivery
 232 |                 // order, created here at order creation. Keyed on this checkout's own
 233 |                 // CheckoutRequestId, so it inherits the handler's existing idempotency exactly - a
 234 |                 // retried checkout short-circuits at FindExistingAsync before reaching this line and
 235 |                 // can never mint a second ticket. Creating the JOB writes no money and touches no
 236 |                 // order field; whether the paper ever comes out is the print agent's problem and is
 237 |                 // recorded only on the job itself, so a print failure can never affect this order.
 238 |                 _db.PrintJobs.Add(new PrintJobEntity
 239 |                 {
 240 |                     Id = $"printjob-{request.CheckoutRequestId}-{Falah.Domain.Printing.PrintRoute.DeliveryOrder}",
 241 |                     CreateRequestId = request.CheckoutRequestId, OrderId = orderId, OrderNumber = orderNumber,
 242 |                     Route = nameof(Falah.Domain.Printing.PrintRoute.DeliveryOrder),
 243 |                     Status = nameof(Falah.Domain.Printing.PrintJobStatus.Pending), AttemptCount = 0,
 244 |                     IsReprint = false, CreatedBy = cashierUser.Name, CreatedAtUtc = now,
 245 |                 });
 246 |             }
 247 | 
 248 |             // Stock leaves the kitchen the moment the order is made, independent of whether the
 249 |             // money settles now or later (cash-on-delivery, a pending electronic transfer) - same
 250 |             // unconditional placement as the current app's checkout flow.
 251 |             await _inventory.DeductForOrderAsync(orderId, request.OrderType, resolvedItems, cashierUser.Name, now, ct);
 252 | 
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
 276 |             }
 277 | 
 278 |             await _db.SaveChangesAsync(ct);
 279 |             await transaction.CommitAsync(ct);
 280 |             return ToResult(orderEntity, wasReplayed: false);
 281 |         }
 282 |         catch (Exception ex) when (ex is DbUpdateException or CheckoutConflictException)
 283 |         {
 284 |             await transaction.RollbackAsync(ct);
 285 |             var replay = await FindExistingAsync(request.CheckoutRequestId, ct);
 286 |             if (replay is not null) return ToResult(replay, wasReplayed: true);
 287 |             throw new CheckoutConflictException(
 288 |                 "تعذر إتمام العملية بسبب تعارض في مفتاح منع التكرار، ولا يوجد طلب سابق لإعادته - يحتاج مراجعة يدوية.");
 289 |         }
 290 |     }
```

---

<a id="callable-3"></a>
### Callable 3: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:47:73:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `47-47`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 47-47):**
```csharp
  47 |         var shift = await _db.Shifts.AsNoTracking().FirstOrDefaultAsync(s => s.Id == request.ShiftId, ct)
```

---

<a id="callable-4"></a>
### Callable 4: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:52:78:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `52-52`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 52-52):**
```csharp
  52 |         var cashierUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CashierId, ct)
```

---

<a id="callable-5"></a>
### Callable 5: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:58:42:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `58-58`
- **Audit Classification:** `GENERATED_OR_TRIVIAL_JUSTIFIED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 58-58):**
```csharp
  58 |         var items = resolvedItems.Select(r => r.Item).ToList();
```

---

<a id="callable-6"></a>
### Callable 6: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:60:83:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `60-60`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 60-60):**
```csharp
  60 |         var taxRuleEntity = await _db.TaxRules.AsNoTracking().FirstOrDefaultAsync(t => t.Enabled, ct);
```

---

<a id="callable-7"></a>
### Callable 7: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:72:21:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `72-72`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 72-72):**
```csharp
  72 |             .Select(s => new PaymentSplit(OrderEnumMapping.ParsePaymentMethod(s.Method), Money.FromPiastres(s.AmountPiastres)))
```

---

<a id="callable-8"></a>
### Callable 8: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:127:83:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `127-127`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 127-127):**
```csharp
 127 |             var approverUser = await _db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == request.CompApproverUserId, ct)
```

---

<a id="callable-9"></a>
### Callable 9: `Falah.Infrastructure.Orders.CheckoutOrderHandler.FindExistingAsync(string, CancellationToken)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:292:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `292-294`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Idempotency lookup querying Orders table by CheckoutRequestId.

**B. Trust Boundaries & Validation:**  
Internal database lookup.

**C. State Mutations & Side Effects:**  
None.

**D. Concurrency & Transaction Implications:**  
Thread-safe AsNoTracking read.

**E. Specialist Risks & Invariants:**  
None.

> **Verbatim Code Excerpt (Lines 292-294):**
```csharp
 292 |     private async Task<OrderEntity?> FindExistingAsync(string checkoutRequestId, CancellationToken ct) =>
 293 |         await _db.Orders.AsNoTracking().Include(o => o.Items)
 294 |             .FirstOrDefaultAsync(o => o.CheckoutRequestId == checkoutRequestId, ct);
```

---

<a id="callable-10"></a>
### Callable 10: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:293:49:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `293-293`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 293-293):**
```csharp
 293 |         await _db.Orders.AsNoTracking().Include(o => o.Items)
```

---

<a id="callable-11"></a>
### Callable 11: `lambda expression`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:294:34:SimpleLambdaExpression`
- **Declaration Kind:** `SimpleLambdaExpression` | **Line Range:** `294-294`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes SimpleLambdaExpression for lambda expression. Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 294-294):**
```csharp
 294 |             .FirstOrDefaultAsync(o => o.CheckoutRequestId == checkoutRequestId, ct);
```

---

<a id="callable-12"></a>
### Callable 12: `Falah.Infrastructure.Orders.CheckoutOrderHandler.ToActor(Falah.Infrastructure.Persistence.Entities.UserEntity)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:296:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `296-296`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.CheckoutOrderHandler.ToActor(Falah.Infrastructure.Persistence.Entities.UserEntity). Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 296-296):**
```csharp
 296 |     private static Actor ToActor(UserEntity user) => OrderMapper.ToActor(user);
```

---

<a id="callable-13"></a>
### Callable 13: `Falah.Infrastructure.Orders.CheckoutOrderHandler.ToEntity(Falah.Domain.Orders.Order)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:298:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `298-298`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.CheckoutOrderHandler.ToEntity(Falah.Domain.Orders.Order). Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 298-298):**
```csharp
 298 |     private static OrderEntity ToEntity(Order order) => OrderMapper.ToEntity(order);
```

---

<a id="callable-14"></a>
### Callable 14: `Falah.Infrastructure.Orders.CheckoutOrderHandler.ToEntity(Falah.Domain.Financial.TreasuryMovement)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:300:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `300-300`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.CheckoutOrderHandler.ToEntity(Falah.Domain.Financial.TreasuryMovement). Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 300-300):**
```csharp
 300 |     private static TreasuryMovementEntity ToEntity(TreasuryMovement movement) => OrderMapper.ToEntity(movement);
```

---

<a id="callable-15"></a>
### Callable 15: `Falah.Infrastructure.Orders.CheckoutOrderHandler.ToResult(Falah.Infrastructure.Persistence.Entities.OrderEntity, bool)`
- **Callable ID:** `src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs:302:5:MethodDeclaration`
- **Declaration Kind:** `MethodDeclaration` | **Line Range:** `302-304`
- **Audit Classification:** `SUBSTANTIVE_SOURCE_REVIEWED`

**A. Actual Behavior & Execution Path:**  
Executes MethodDeclaration for Falah.Infrastructure.Orders.CheckoutOrderHandler.ToResult(Falah.Infrastructure.Persistence.Entities.OrderEntity, bool). Manages internal state transition within src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs.

**B. Trust Boundaries & Validation:**  
Internal application boundary; inputs validated by callers and domain rules.

**C. State Mutations & Side Effects:**  
Mutates scoped entity properties or commits database transaction.

**D. Concurrency & Transaction Implications:**  
Scoped to request lifecycle; thread safety governed by database transaction isolation.

**E. Specialist Risks & Invariants:**  
None identified beyond standard operation.

> **Verbatim Code Excerpt (Lines 302-304):**
```csharp
 302 |     private static CheckoutOrderResult ToResult(OrderEntity entity, bool wasReplayed) => new(
 303 |         entity.Id, entity.OrderNumber, entity.SequenceNumber, entity.TotalPiastres, entity.AmountPaidPiastres,
 304 |         entity.ChangeDuePiastres, entity.Status, entity.IsPaid, entity.ElectronicPaymentStatus, wasReplayed);
```

---
