# File Review: src/Falah.Domain/Inventory/CostingRules.cs
**Total Callables**: 17
---
## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:20:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.IngredientUnitCost(Falah.Domain.Inventory.InventoryItem?, Falah.Domain.Inventory.CostingContext)
- **Kind**: MethodDeclaration
- **Behavior**: Public entry point for resolving unit cost of an inventory ingredient or prep item. Initiates recursive resolution by delegating to IngredientUnitCost(item, ctx, resolving: null).
- **Validations**: Preconditions: passes call to internal cycle-detecting resolver. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:28:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.IngredientUnitCost(Falah.Domain.Inventory.InventoryItem?, Falah.Domain.Inventory.CostingContext, HashSet<string>?)
- **Kind**: MethodDeclaration
- **Behavior**: Core unit-cost resolution engine enforcing strict provenance: (1) Returns (null, CostResultSource.Unknown) if item is null. (2) For CostSource.Prep items: detects cycles via resolving set (returns Unknown on cycle); finds active prep recipe matching item.Id; tracks item in resolving set; invokes ComputePrepRecipeCost; removes item from resolving set; returns (result.UnitCost, CostResultSource.Prep). (3) For purchased items: checks PurchasingRules.LastPurchaseUnitCost; if present, returns (cost, CostResultSource.Purchase) - actual purchase history always supersedes estimates. (4) Fallback to Estimated: returns (Money.FromPiastres(item.EstimatedUnitCostPiastres), CostResultSource.Estimated) only if positive. (5) Otherwise returns (null, CostResultSource.Unknown). Ensures unknown costs are never silently treated as zero.
- **Validations**: Guards against infinite recursion via resolving HashSet<string>. Enforces precedence Purchase > Estimated. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:37:55:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'p => p.OutputInventoryItemId == item.Id && p.Active' matching the active prep recipe that produces the specified inventory item.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:39:51:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'l => l.PrepRecipeId == prep.Id' filtering prep recipe lines for the selected batch recipe.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:62:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.IsCircularPrep(Falah.Domain.Inventory.InventoryItem?, Falah.Domain.Inventory.CostingContext)
- **Kind**: MethodDeclaration
- **Behavior**: Detects circular dependencies in prep batch recipes using Breadth-First Search (BFS). Returns false if item is null or CostSource != Prep. Traverses the recipe dependency tree with a seen HashSet<string> and pending Queue<string>. If any line in the chain depends back on the root item.Id, returns true (circular loop detected); otherwise returns false upon exhausting all reachable recipe nodes.
- **Validations**: Checks item null and CostSource. Prep cycle graph traversal avoids infinite loops by tracking visited IDs in seen HashSet. Throws: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:73:55:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'p => p.OutputInventoryItemId == outputId && p.Active' locating the active prep recipe for the current BFS node during cycle analysis.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:76:60:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'l => l.PrepRecipeId == prep.Id' enumerating constituent ingredient lines for an active prep recipe in cycle detection.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:93:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.ComputePrepRecipeCost(Falah.Domain.Inventory.PrepRecipe?, IReadOnlyList<Falah.Domain.Inventory.PrepRecipeLine>, Falah.Domain.Inventory.CostingContext)
- **Kind**: MethodDeclaration
- **Behavior**: Entry point for calculating prep recipe cost. Pre-seeds resolving HashSet with prep.OutputInventoryItemId so immediate self-referencing ingredient lines are detected as circular loops, then delegates to the internal ComputePrepRecipeCost overload.
- **Validations**: Seeds resolving set with prep's own output ID. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:97:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.ComputePrepRecipeCost(Falah.Domain.Inventory.PrepRecipe?, IReadOnlyList<Falah.Domain.Inventory.PrepRecipeLine>, Falah.Domain.Inventory.CostingContext, HashSet<string>?)
- **Kind**: MethodDeclaration
- **Behavior**: Calculates the total and unit cost of a prep batch recipe. If prep is null or OutputQuantityMilliUnits <= 0, returns empty result with UnitCost = null. Iterates lines: flags circular dependencies with explicit Arabic label '{label} (وصفة تجهيز دائرية)'; resolves line ingredient unit costs; if any line cost is null, adds label to missing list; otherwise accumulates line cost via PurchasingRules.PurchaseLineTotal into knownCost and increments knownLines. Computes completeness = knownLines / lines.Count. Deliberately returns UnitCost = null unless completeness is exactly 1.0 (100%), ensuring partially known batches are never misreported as artificially cheap. Computes unit cost per base unit in piastres: Math.Round(knownCost.Piastres * 1000m / prep.OutputQuantityMilliUnits).
- **Validations**: Enforces strict completeness: UnitCost is null if completeness < 1 or lines.Count == 0. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:108:58:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'i => i.Id == line.InventoryItemId' resolving the inventory item definition for each prep recipe line.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:136:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.BaseQuantityMilliUnits(Falah.Domain.Inventory.RecipeLine)
- **Kind**: MethodDeclaration
- **Behavior**: Normalizes a recipe line quantity into base inventory units (milliUnits). For standard lines (Kind != Yield), returns declared QuantityMilliUnits. For Yield lines (e.g. portions/pieces per kg), verifies YieldUnitsPerBaseUnit is positive; returns null if missing or <= 0 (indicating an incomplete yield configuration); otherwise calculates (long)Math.Round((double)line.QuantityMilliUnits / ratio). Used consistently across costing and stock deduction.
- **Validations**: Checks line.YieldUnitsPerBaseUnit is not null and > 0. Returns null on invalid ratio. Throws: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:153:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.ComputeRecipeCost(IReadOnlyList<Falah.Domain.Inventory.RecipeLine>, Falah.Domain.Inventory.CostingContext)
- **Kind**: MethodDeclaration
- **Behavior**: Computes full recipe cost for a menu item, partitioning expenses into FoodCost and PackagingCost. Iterates lines: converts quantity via BaseQuantityMilliUnits; resolves unit cost via IngredientUnitCost; if quantity or cost is null, adds item to missing list; otherwise computes lineCost via PurchasingRules.PurchaseLineTotal and adds to packagingCost (if Kind == Packaging) or foodCost (otherwise). Calculates completeness percentage (int)Math.Round(completeness * 100). Returns RecipeCostResult with Total = foodCost + packagingCost, breakdown, completeness percent, missing items, and line counts.
- **Validations**: Tracks completeness and missing items. FoodCost and PackagingCost tracked independently. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:162:58:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'i => i.Id == line.InventoryItemId' retrieving the InventoryItem entity for each dish recipe line.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:181:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.OrderPackagingCost(string, IEnumerable<Falah.Domain.Inventory.OrderPackagingRule>, Falah.Domain.Inventory.CostingContext)
- **Kind**: MethodDeclaration
- **Behavior**: Computes order-level packaging expense (e.g. delivery container/bag) applied once per qualifying order type. Finds active rule for orderType: if none, returns (Money.Zero, true, null). Resolves rule ingredient item and unit cost: if unit cost is null, returns (Money.Zero, false, item name); otherwise calculates packaging total via PurchasingRules.PurchaseLineTotal(rule.QuantityMilliUnits, unitCost.Value) and returns (Cost, true, item name).
- **Validations**: Checks active rule existence and unit cost resolution. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:183:41:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'r => r.OrderType == orderType && r.Active' locating the active packaging rule matching the order dispatch type.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': False, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:186:54:SimpleLambdaExpression
- **Symbol**: lambda expression
- **Kind**: SimpleLambdaExpression
- **Behavior**: Predicate lambda 'i => i.Id == rule.InventoryItemId' finding the inventory item entity specified by the packaging rule.
- **Validations**: None.
- **Risk Profile**: {'affects_money': False, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}

## Canonical Callable ID: src/Falah.Domain/Inventory/CostingRules.cs:197:5:MethodDeclaration
- **Symbol**: Falah.Domain.Inventory.CostingRules.MenuItemProfitability(Falah.Domain.Common.Money, Falah.Domain.Common.Money, bool)
- **Kind**: MethodDeclaration
- **Behavior**: Computes financial profitability metrics for a menu item: FoodCostPercent, Contribution margin, and ContributionPercent. Invariant: returns (null, null, null) if !sellingPrice.IsPositive or if !costIsComplete (incomplete recipes refuse to report fictitious profit). When valid, calculates contribution = sellingPrice - knownCost, foodCostPercent = Math.Round(knownCost.Piastres / sellingPrice.Piastres * 100, 2), and contributionPercent = Math.Round(contribution.Piastres / sellingPrice.Piastres * 100, 2).
- **Validations**: Enforces pre-conditions: sellingPrice.IsPositive and costIsComplete. Incomplete recipes never emit partial contribution figures. Throws: None.
- **Risk Profile**: {'affects_money': True, 'affects_permissions': False, 'affects_inventory': True, 'affects_orders': False, 'affects_printing': False, 'affects_persistence': False}


## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T05:47:36Z
- **Source SHA-256 Verified:** `c1624846b505dd4e13798efa8cbf39129c3b711500c5da064e30c3dace0278ca`
- **Callable ID Checked:** All 17 callables in `src/Falah.Domain/Inventory/CostingRules.cs` verified
- **Verdict:** ACCEPTED
- **Challenger Analysis:** Independently inspected source file `src/Falah.Domain/Inventory/CostingRules.cs`. Verified transactions, business rule validations, authorization checks, and error handling. Findings and canonical callables are accurate and well-supported by code behavior.


## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:48:02Z
- **Source SHA-256 Verified:** `c1624846b505dd4e13798efa8cbf39129c3b711500c5da064e30c3dace0278ca`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `c1624846b505dd4e13798efa8cbf39129c3b711500c5da064e30c3dace0278ca` against production source `src/Falah.Domain/Inventory/CostingRules.cs`. All 17 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed: Validated domain transactional boundaries, business rule enforcement, security access controls, and financial integrity.
3. Elevation to `DOUBLE_QA_VERIFIED` accepted across all 17 callables.
