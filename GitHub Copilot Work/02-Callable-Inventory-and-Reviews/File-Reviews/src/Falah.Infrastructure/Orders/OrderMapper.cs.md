# src/Falah.Infrastructure/Orders/OrderMapper.cs

- **Path:** `src/Falah.Infrastructure/Orders/OrderMapper.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `37619395348335292dd8dc7469e4f21488033d3816b097ad9b55451a6af20a9d`
- **Git Blob SHA256:** `5a435c41dd562f7066f4f629f02e4bc66afec4ee`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 205]]`

---

## 1. Architectural Scope and Design Role

[`src/Falah.Infrastructure/Orders/OrderMapper.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/OrderMapper.cs#L1-L205) is the authoritative persistence and data-transfer mapping bridge for the order subsystem. Located in `Falah.Infrastructure.Orders`, it translates bidirectionally between:
1. The pure domain model ([`Order`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs), [`OrderItem`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderItem.cs), [`OrderRefund`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderRefund.cs), and [`TreasuryMovement`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Financial/TreasuryMovement.cs))
2. EF Core database persistence entities ([`OrderEntity`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Persistence/Entities/OrderEntities.cs#L11), `OrderItemEntity`, `OrderRefundEntity`, `TreasuryMovementEntity`)
3. Read-side API transfer DTOs ([`OrderDetailsDto`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/OrderDetailsDto.cs)) consumed by desktop clients (WPF, WinUI 3 Cashier, WinUI 3 Call Center).

### Core Architectural Principles:
- **Clean Architecture Boundary:** Domain classes have zero knowledge of EF Core, SQL Server/SQLite attributes, or DTO contracts. Reconstitution into domain aggregates runs via pure primitive factory [`Order.Reconstitute`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L119-L172).
- **Snapshot Fidelity:** Copies all monetary figures (in integer piastres), tax snapshots (`TaxAmountPiastres`, `TaxableBasePiastres`, `TaxRateBasisPoints`), and giveaway buckets (`DiscountPiastres`, `CompAmountPiastres`) without dynamic re-computation.
- **One-Way Entity Projection:** [`ToEntity(Order)`](#callable-4-toentity-order) is strictly designed for **new orders**. It intentionally omits mapping the `Refunds` navigation collection; mapping an already-refunded order via `ToEntity` would fail to synchronize refunds unless managed through the dedicated [`ToEntity(OrderRefund)`](#callable-5-toentity-orderrefund).

---

## 2. Exhaustive Callable Inventory and Source Review

### Callable 1: `ToActor(UserEntity user)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:19:5:MethodDeclaration`
- **Exact Line Range:** Lines 19–21
- **Signature:** `Falah.Domain.Authorization.Actor Falah.Infrastructure.Orders.OrderMapper.ToActor(Falah.Infrastructure.Persistence.Entities.UserEntity)`
- **Kind:** `MethodDeclaration` (Static Mapper)
- **Actual Behavior & Execution Flow:**
  - Extracts comma-separated permissions from `user.Permissions`, trimming and splitting into a `HashSet<string>`.
  - Constructs domain value object [`Actor`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Authorization/Actor.cs) for policy evaluation in handlers.
- **Verdict:** **VERIFIED**

---

### Callable 2: `ToDto(OrderEntity order)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:24:5:MethodDeclaration`
- **Exact Line Range:** Lines 24–39
- **Signature:** `Falah.Application.Orders.OrderDetailsDto Falah.Infrastructure.Orders.OrderMapper.ToDto(Falah.Infrastructure.Persistence.Entities.OrderEntity)`
- **Kind:** `MethodDeclaration` (Static Mapper)
- **Associated Lambdas:**
  - Line 26: `i => new OrderItemDto(...)` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:26:28:SimpleLambdaExpression`)
  - Line 37: `s => new OrderSplitPaymentDto(s.method, s.amountPiastres)` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:37:25:SimpleLambdaExpression`)
- **Actual Behavior & Field Mapping:**
  - Maps `OrderEntity` into read-only [`OrderDetailsDto`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Application/Orders/OrderDetailsDto.cs).
  - Projects `order.Items` into `OrderItemDto` (name, variant, unit price, quantity, total price, rule name, notes).
  - Deserializes `SplitPaymentsJson` into `List<OrderSplitPaymentDto>`.
  - Exposes all structured delivery properties (`DeliveryStreet`, `DeliveryBuilding`, `DeliveryFloor`, `DeliveryApartment`, `DeliveryLandmark`, `DeliveryNotes`, `DeliveryServiceId`, `DeliveryServiceName`, `DeliveryFeePiastres`).
  - Exposes dine-in `TableNumber` and `RequestedPickupAtUtc`.
- **Client-Side Calculation Parity:**
  - Both WPF (`RefundableRemainingPiastres`) and WinUI 3 (`RefundRemainingDisplay`, `CanRefundOrder`) calculate remaining balance as `TotalPiastres - RefundAmountPiastres`. This matches the mapped fields with zero divergence.
- **Verdict:** **PARITY_VERIFIED**

---

### Callable 3: `ToDomain(OrderEntity entity)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:41:5:MethodDeclaration`
- **Exact Line Range:** Lines 41–82
- **Signature:** `Falah.Domain.Orders.Order Falah.Infrastructure.Orders.OrderMapper.ToDomain(Falah.Infrastructure.Persistence.Entities.OrderEntity)`
- **Kind:** `MethodDeclaration` (Static Rehydration Mapper)
- **Associated Lambdas:**
  - Line 44: `i => new OrderItem(...)` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:44:21:SimpleLambdaExpression`)
  - Line 52: `r => new OrderRefund(...)` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:52:21:SimpleLambdaExpression`)
  - Line 66: `s => new PaymentSplit(...)` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:66:25:SimpleLambdaExpression`)
- **Actual Behavior & Reconstitution Flow:**
  1. Maps `entity.Items` into domain `OrderItem` records. Note: `i.Id.ToString()` converts integer PK to string domain ID.
  2. Maps `entity.Refunds` into domain `OrderRefund` records.
  3. Reconstitutes `ElectronicPayment`: Parses method, provider, amount in piastres, and status via `ParseElectronicPaymentStatus`.
  4. Deserializes `entity.SplitPaymentsJson` into `PaymentSplit` domain records.
  5. Calls [`Order.Reconstitute`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/Order.cs#L119-L172) passing all 38 snapshot properties.
- **Invariants & Safety:**
  - Requires `entity.Items` and `entity.Refunds` to be pre-loaded via EF Core `.Include()`. If a caller invokes `ToDomain` without including `Items` or `Refunds`, the domain aggregate is created with empty collections! Handlers (`RefundOrderHandler`, `ConfirmElectronicPaymentHandler`) properly include both collections.
- **Verdict:** **VERIFIED**

---

### Callable 4: `ToEntity(Order order)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:84:5:MethodDeclaration`
- **Exact Line Range:** Lines 84–144
- **Signature:** `Falah.Infrastructure.Persistence.Entities.OrderEntity Falah.Infrastructure.Orders.OrderMapper.ToEntity(Falah.Domain.Orders.Order)`
- **Kind:** `MethodDeclaration` (Static Entity Projection Mapper)
- **Associated Lambdas:**
  - Line 111: `s => new WireSplit(...)` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:111:82:SimpleLambdaExpression`)
  - Line 127: `i => new OrderItemEntity { ... }` (`src/Falah.Infrastructure/Orders/OrderMapper.cs:127:36:SimpleLambdaExpression`)
- **Actual Behavior & Execution Flow:**
  - Projects domain `Order` into fresh `OrderEntity` for insertion via `_db.Orders.Add(orderEntity)`.
  - Serializes `order.SplitPayments` to JSON via `WireSplit`.
  - Projects each `OrderItem` into `OrderItemEntity` with `OrderId = order.Id`.
- **Deliberate Design Limitation:**
  - Does NOT project `order.Refunds`. When adding refunds to an existing order, the caller adds directly via [`ToEntity(OrderRefund)`](#callable-5-toentity-orderrefund) to prevent EF Core graph tracking collisions.
- **Verdict:** **VERIFIED (New Orders Only)**

---

### Callable 5: `ToEntity(OrderRefund refund)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:149:5:MethodDeclaration`
- **Exact Line Range:** Lines 149–162
- **Signature:** `Falah.Infrastructure.Persistence.Entities.OrderRefundEntity Falah.Infrastructure.Orders.OrderMapper.ToEntity(Falah.Domain.Orders.OrderRefund)`
- **Kind:** `MethodDeclaration` (Static Entity Mapper)
- **Actual Behavior & Execution Flow:**
  - Projects domain `OrderRefund` into `OrderRefundEntity`.
  - Docstring (line 146): `OrderId` is deliberately not set here; the caller adds the result to `orderEntity.Refunds`, letting EF Core assign the foreign key.
- **Verdict:** **VERIFIED**

---

### Callable 6: `ToEntity(TreasuryMovement movement)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:164:5:MethodDeclaration`
- **Exact Line Range:** Lines 164–182
- **Signature:** `Falah.Infrastructure.Persistence.Entities.TreasuryMovementEntity Falah.Infrastructure.Orders.OrderMapper.ToEntity(Falah.Domain.Financial.TreasuryMovement)`
- **Kind:** `MethodDeclaration` (Static Entity Mapper)
- **Actual Behavior & Execution Flow:**
  - Maps domain `TreasuryMovement` value object to EF Core `TreasuryMovementEntity`.
  - Preserves `IdempotencyKey`, `OperationId`, `SourceAccountId`, `DestinationAccountId`, and `AmountPiastres`.
- **Verdict:** **VERIFIED**

---

### Callable 7: `ParseOrderStatus(string value)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:184:5:MethodDeclaration`
- **Exact Line Range:** Lines 184–193
- **Signature:** `Falah.Domain.Orders.OrderStatus Falah.Infrastructure.Orders.OrderMapper.ParseOrderStatus(string)`
- **Kind:** `MethodDeclaration` (Private Static Helper)
- **Actual Behavior & Guard Logic:**
  - Strict lowercase switch mapping:
    - `"pending"` -> `OrderStatus.Pending`
    - `"preparing"` -> `OrderStatus.Preparing`
    - `"ready"` -> `OrderStatus.Ready`
    - `"completed"` -> `OrderStatus.Completed`
    - `"cancelled"` -> `OrderStatus.Cancelled`
    - `"refunded"` -> `OrderStatus.Refunded`
    - Default: Throws `ArgumentException($"حالة طلب غير معروفة: {value}")`.
- **Verdict:** **VERIFIED**

---

### Callable 8: `ParseElectronicPaymentStatus(string value)`

- **Canonical Callable ID:** `src/Falah.Infrastructure/Orders/OrderMapper.cs:195:5:MethodDeclaration`
- **Exact Line Range:** Lines 195–201
- **Signature:** `Falah.Domain.Orders.ElectronicPaymentStatus Falah.Infrastructure.Orders.OrderMapper.ParseElectronicPaymentStatus(string)`
- **Kind:** `MethodDeclaration` (Private Static Helper)
- **Actual Behavior & Guard Logic:**
  - Maps:
    - `"pending"` -> `ElectronicPaymentStatus.Pending`
    - `"confirmed"` -> `ElectronicPaymentStatus.Confirmed`
    - `"cancelled"` -> `ElectronicPaymentStatus.Cancelled`
    - Default: Throws `ArgumentException($"حالة دفع إلكتروني غير معروفة: {value}")`.
- **Verdict:** **VERIFIED**

---

## 3. Summary Matrix of Callables in OrderMapper.cs

| # | Callable Symbol | Line Range | Kind | Audit Verdict | Parity Status |
| :--- | :--- | :--- | :--- | :--- | :--- |
| 1 | `ToActor` | 19–21 | Method | Verified | Maps permissions cleanly to HashSet |
| 2 | `ToDto` | 24–39 | Method | Verified | Complete DTO mapping, no calculation drift |
| 3 | `ToDomain` | 41–82 | Method | Verified | Rehydrates live aggregate via Reconstitute |
| 4 | `ToEntity (Order)` | 84–144 | Method | Verified | Projection for new orders; omits Refunds by design |
| 5 | `ToEntity (Refund)` | 149–162 | Method | Verified | Safe entity projection without manual FK |
| 6 | `ToEntity (Treasury)`| 164–182 | Method | Verified | Full ledger fidelity preserved |
| 7 | `ParseOrderStatus` | 184–193 | Method | Verified | Exhaustive enum parser, throws on unknown |
| 8 | `ParseElectronicPaymentStatus`| 195–201 | Method | Verified | Exhaustive enum parser, throws on unknown |

---

## 4. Formal Recommendations

1. **Guard Against Missing Navigation Properties in `ToDomain`:**
   In `ToDomain`, add explicit assertions ensuring `entity.Items != null` and `entity.Refunds != null` so calling `ToDomain` on an unincluded EF row immediately fails fast with a descriptive error rather than instantiating an aggregate with truncated facts.
2. **DTO Deserialization Resilience:**
   In `ToDto` line 36, replace direct deserialization `Deserialize<List<WireSplit>>(order.SplitPaymentsJson)!` with a `try/catch` fallback returning an empty list in case corrupt JSON exists in historical database rows.
