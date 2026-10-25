# src/Falah.Domain/Pricing/ItemPricingRules.cs

- **Path:** `src/Falah.Domain/Pricing/ItemPricingRules.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `47248fb19dfcb29d219a349e99e1e60995bccd12a84afbd4139b081467161303`
- **Git Blob SHA256:** `47248fb19dfcb29d219a349e99e1e60995bccd12a84afbd4139b081467161303`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 29]]`

---

## 1. Architectural Scope and Design Role

[`ItemPricingRules.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs#L1-L29) is a pure domain service located in `Falah.Domain.Pricing`. It encapsulates the authoritative pricing resolution logic that maps a catalog item request (product ID, variant list price, and an optional requested promotion/offer rule ID) into an immutable, frozen [`ResolvedItemPrice`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs#L14-L17) structure.

### Core Architectural Principles:
1. **Server Pricing Sovereignty:** The client application requests an item and optionally suggests a price rule (via chip selection or dispatcher intake), but the client **never** dictates the price charged on the wire.
2. **Graceful Offer Degradation:** If a requested promotion rule is expired, disabled, or outside its active time/day window, the system does not fail or reject the checkout; it silently falls back to the item's list price (`listUnitPrice`).
3. **Stateless Purity:** Contains no I/O, no database queries, and no state mutation. All inputs (`whenUtc`, `timeZone`, `allRules`) are supplied by the caller.

---

## 2. Callable Inventory and Forensic Source Analysis

### Callable 1: `ResolvedItemPrice` (Record Struct & Members)

```csharp
public readonly record struct ResolvedItemPrice(
    Money ListUnitPrice,
    Money EffectiveUnitPrice,
    string? AppliedPriceRuleId,
    string? AppliedPriceRuleName)
{
    public bool RuleApplied => AppliedPriceRuleId is not null;
}
```

- **Kind:** `RecordStructDeclaration`
- **Signature:** `Falah.Domain.Pricing.ItemPricingRules.ResolvedItemPrice`
- **Location:** Lines 14–17
- **Actual Behavior & Semantics:**
  - `ListUnitPrice`: Represents the standard catalog price (either `ProductEntity.BasePricePiastres` or `ProductVariantEntity.PricePiastres`).
  - `EffectiveUnitPrice`: The actual price per unit to be billed on the order line. When a valid promotion rule applies, this equals `PriceRule.FixedPrice`; otherwise, it equals `ListUnitPrice`.
  - `AppliedPriceRuleId` & `AppliedPriceRuleName`: Identifiers of the applied promotional offer. Null if list price was charged.
  - `RuleApplied` (Property Getter): Expression-bodied property returning `true` when `AppliedPriceRuleId is not null`.
- **Inputs & Invariants:**
  - Uses [`Money`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Common/Money.cs#L9-L36) value object representing integer piastres (`long Piastres`).
  - Immutability: Declared as `readonly record struct`, ensuring zero heap allocation and value-type stack semantics during cart iteration.
- **Rounding Logic & Mathematics:**
  - No rounding performed. Integer piastre values are copied directly.
- **Side Effects:** None.
- **Callers & Call Chain:**
  - Instantiated exclusively inside [`ItemPricingRules.Resolve`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/ItemPricingRules.cs#L19-L27).
  - Consumed by [`MenuPricingService.ResolveAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/MenuPricingService.cs#L55-L64) to construct [`OrderItem`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Orders/OrderItem.cs#L12-L24).

---

### Callable 2: `ItemPricingRules.Resolve`

```csharp
public static ResolvedItemPrice Resolve(
    string productId,
    Money listUnitPrice,
    string? requestedPriceRuleId,
    IEnumerable<PriceRule> allRules,
    DateTimeOffset whenUtc,
    TimeZoneInfo timeZone)
```

- **Kind:** `MethodDeclaration`
- **Signature:** `Falah.Domain.Pricing.ItemPricingRules.Resolve(string, Falah.Domain.Common.Money, string?, System.Collections.Generic.IEnumerable<Falah.Domain.Pricing.PriceRule>, System.DateTimeOffset, System.TimeZoneInfo)`
- **Location:** Lines 19–27
- **Actual Behavior & Execution Flow:**
  1. Delegates rule validation to [`PricingRules.ValidateAppliedRule(productId, requestedPriceRuleId, allRules, whenUtc, timeZone)`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Domain/Pricing/PricingRules.cs#L35-L42).
  2. `ValidateAppliedRule` checks:
     - If `string.IsNullOrEmpty(requestedPriceRuleId)`, immediately returns `null`.
     - Searches `allRules` for matching `ruleId`. If not found, returns `null`.
     - Verifies `rule.ProductIds.Contains(productId)` and `PricingRules.IsActiveAt(rule, whenUtc, timeZone)`.
       - Evaluates `rule.Active`.
       - Converts `whenUtc` to `timeZone` (Cairo standard time) and validates allowed `DaysOfWeek`.
       - Evaluates time range: Supports midnight wrapping (`start <= end ? now >= start && now <= end : now >= start || now <= end`).
  3. **Conditional Resolution:**
     - **Branch A (`validRule is null`):** Returns `new ResolvedItemPrice(listUnitPrice, listUnitPrice, null, null)`.
     - **Branch B (`validRule is not null`):** Returns `new ResolvedItemPrice(listUnitPrice, validRule.FixedPrice, validRule.Id, validRule.Name)`.
- **Inputs & Type Specifications:**
  - `string productId`: Non-null product identifier.
  - `Money listUnitPrice`: Baseline price from product or selected variant.
  - `string? requestedPriceRuleId`: Optional promotion rule requested by cashier.
  - `IEnumerable<PriceRule> allRules`: Active price rules pre-filtered by caller.
  - `DateTimeOffset whenUtc`: Transaction timestamp (prevents clock skew during evaluation).
  - `TimeZoneInfo timeZone`: Business local timezone (`BusinessClock.CairoTimeZone`).
- **Mathematical Formulas & Rounding:**
  - Direct assignment: No floating-point operations.
  - Fixed-Price Behavior: If `validRule` is present, `validRule.FixedPrice` replaces `listUnitPrice`. Notice that the domain deliberately does **NOT** enforce that `validRule.FixedPrice < listUnitPrice`. A price rule can legally set a higher price (e.g. holiday surcharge or peak pricing bundle).
- **Dependencies & Downstream Call Chain:**
  - Depends on `PricingRules.ValidateAppliedRule`.
  - Sole production caller: [`MenuPricingService.ResolveAsync`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/MenuPricingService.cs#L55).
  - Downstream caller chain:
    `CheckoutOrderHandler.HandleAsync` -> `MenuPricingService.ResolveAsync` -> `ItemPricingRules.Resolve` -> `OrderItem` -> `CheckoutRules.BuildPlan`.

---

## 3. Client vs Server Validation Divergence Analysis

### Divergence 1: Local Cart Display vs Server Resolution (SEM-MIRROR-068)
- **Client Estimation:**
  - **WPF Cashier** ([`CashierViewModel.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.Cashier/ViewModels/CashierViewModel.cs#L112-L125)):
    ```csharp
    var offer = offerId is null ? null : product.ApplicableOffers.FirstOrDefault(o => o.Id == offerId);
    var unitPrice = offer?.FixedPricePiastres ?? variant?.PricePiastres ?? product.BasePricePiastres;
    ```
  - **WinUI Cashier** ([`ProductTile.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Client.CashierWinUI/Models/ProductTile.cs#L23-L25)):
    Estimates starting price based on cached catalog data.
- **Server Enforcement:**
  - When the cart is submitted to `/api/orders/checkout`, [`CheckoutOrderHandler`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs#L57) executes `MenuPricingService.ResolveAsync`, which calls `ItemPricingRules.Resolve`.
  - **Silent Drift Risk:** If a cashier rings up an order during a promotion window (e.g., Happy Hour ending at 17:00), but pauses checkout until 17:01, `ItemPricingRules.Resolve` silently revokes the offer and charges the full list price. The client UI displays no warning that the promotion expired between cart creation and payment submission.

### Divergence 2: Pass-Through of Negative Variant Prices (`FIN-CAND-NEGATIVE-VARIANT-PRICE-001`)
- **Vulnerability Mechanism:**
  - `ItemPricingRules.Resolve` blindly accepts `listUnitPrice` without verifying that `listUnitPrice.IsPositive` or `!listUnitPrice.IsNegative`.
  - If a product variant was saved with a negative price (e.g. `-5000` piastres), and no promotion rule is applied:
    `return new ResolvedItemPrice(listUnitPrice, listUnitPrice, null, null);`
  - `EffectiveUnitPrice` evaluates to `-5000` piastres.
  - In `MenuPricingService.cs`:
    `orderItem.TotalPrice = price.EffectiveUnitPrice * request.Quantity` produces a negative total price.
  - In `CheckoutRules.cs`:
    `ComputeSubtotal` sums the negative total price, directly reducing the cart subtotal and payable total!
  - **Conclusion:** `ItemPricingRules` assumes upstream catalog handlers validate price positivity; because `ProductHandlers.cs` fails to do so, `ItemPricingRules.Resolve` acts as a passive conduit for negative price propagation.

---

## 4. Test Coverage Assessment & Missing Scenarios

### Existing Tests:
In [`tests/Falah.Domain.Tests/ItemPricingRulesTests.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/tests/Falah.Domain.Tests/ItemPricingRulesTests.cs#L1-L60):
1. `Falls_back_to_list_price_when_no_rule_requested()`: Verifies that passing `requestedPriceRuleId: null` returns `listUnitPrice` and `RuleApplied == false`.
2. `Falls_back_to_list_price_when_requested_rule_unknown()`: Verifies fallback when rule ID does not exist in `allRules`.
3. `Falls_back_to_list_price_when_rule_expired()`: Verifies fallback when current time is outside rule window.
4. `Applies_active_rule_even_when_more_expensive_than_list_price()`: Confirms that active rules with `FixedPrice > listUnitPrice` are applied.

### Critical Missing Regression Tests:
1. **Negative `listUnitPrice` Pass-Through:** Test asserting that `ItemPricingRules.Resolve` does not clamp or reject negative input prices.
2. **Zero `listUnitPrice` Behavior:** Test asserting behavior when an item is cataloged at 0 piastres (free promotional sample vs misconfigured item).
3. **PriceRule with Negative Fixed Price:** Test asserting whether a promotional rule with negative fixed price overrides list price.
4. **Timezone Boundary Edge Cases:** Test evaluating midnight rollover in Cairo timezone (`23:59:59` to `00:00:01`).

---

## 5. Auditor Recommendations

1. **Defense-in-Depth Invariant Assertion:**
   While catalog handlers should enforce positive prices upon creation, `ItemPricingRules.Resolve` should assert:
   ```csharp
   if (listUnitPrice.IsNegative)
       throw new DomainException($"سعر الصنف غير صالح ({listUnitPrice.Piastres} قرش).");
   ```
2. **Explicit Zero-Price Policy:**
   Define business policy on whether 0-EGP items (complimentary condiments, promotional samplers) are permissible, and enforce `listUnitPrice >= Money.Zero`.
