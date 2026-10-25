# src/Falah.Infrastructure/Inventory/InventoryQueryService.cs

- **Path:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`
- **Baseline Git Commit:** `c35816ecae49633674775dd32061c6ee92f94aaf`
- **Current Worktree SHA256:** `3FF16EC5EB50957C11AD1BA39ED5E45CD8F29B972674D49B4E3448E0E3AEE6DE`
- **Git Blob SHA256:** `8c469abe1b385805570575d5d0cd96058622a4b2b91cc37ca4d45ef5b6ea1247`
- **Baseline Comparison:** `EQUIVALENT_NORMALIZED_LINE_ENDINGS`
- **Auditor Role:** Mirrored Code and Architectural Parity Auditor
- **Status:** **SUBSTANTIVE_SOURCE_REVIEWED — VERIFIED GENUINE AUDIT**
- **Reviewed Line Spans:** `[[1, 509]]`

---

## 1. Architectural Role and Deep Inventory Forensics

[`src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`](file:///c:/Users/Mohamed/Desktop/Falah-Restaurant-CSharp/src/Falah.Infrastructure/Inventory/InventoryQueryService.cs#L1-L509) is the authoritative, read-only query service for inventory, costing, recipe profitability, and stock movement analysis across Falah POS. Like the treasury query engine, it enforces an event-sourced ledger discipline: balances, actual consumptions, variance totals, and unit costs are derived dynamically from underlying movement logs and recipe structures rather than stored as pre-aggregated columns susceptible to silent state drift.

### A. Stock Balance Signed Summation vs Finding FIN-013 (Sign Inversion Risk)
- **Correct Implementation in `InventoryQueryService`:**
  - In `ListInventoryItemsAsync` (lines 25-26):
    `g.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits)`
  - In `GetStockBalanceAsync` (lines 37-41):
    Delegates to `InventoryRules.StockBalanceMilliUnits(movements, inventoryItemId)`, which evaluates `m.Direction == StockDirection.In ? m.QuantityMilliUnits : -m.QuantityMilliUnits`.
  - In `GetInventoryOverviewAsync` (line 402):
    `itemMovements.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits)`
  In all inventory views, incoming stock (`PurchaseReceipt`, `ManualReceipt`, positive `CountAdjustment`) is added (`+`), and outgoing stock (`Usage`, `Waste`, `Spoilage`, `Breakage`, negative `CountAdjustment`) is subtracted (`-`).
- **Contrasting Defect `FIN-013` in `OwnerDashboardQueryService.cs` (lines 301-304):**
  In `OwnerDashboardQueryService.LoadStockAsync`, the developer wrote:
  ```csharp
  var balances = await _db.StockMovements.AsNoTracking()
      .GroupBy(m => m.InventoryItemId)
      .Select(g => new { InventoryItemId = g.Key, Balance = g.Sum(m => m.QuantityMilliUnits) })
      .ToDictionaryAsync(x => x.InventoryItemId, x => x.Balance, ct);
  ```
  `OwnerDashboardQueryService` completely ignores `Direction`, adding outgoing usage and waste to incoming purchases! If a restaurant purchases 1000 kg and consumes 800 kg, `InventoryQueryService` accurately reports **200 kg**, while `OwnerDashboardQueryService` erroneously reports **1800 kg**, masking critical stockouts and suppressing low-stock replenishment alerts.

### B. Recipe Assembly, Prep Hierarchies, and Costing Resolution
- **Dynamic Unit Cost Resolution:** `CostingRules.IngredientUnitCost` inspects `CostSource`. For raw ingredients, it uses the latest `PurchaseReceipt` unit cost; if absent, it falls back to `EstimatedUnitCostPiastres`. For prep items (`CostSource.Prep`), it dynamically evaluates the prep recipe graph via `CostingRules.ComputePrepRecipeCost`.
- **Circular Prep Safety:** `CostingRules.IsCircularPrep` identifies recursive batch dependencies, setting the source to `Circular` with missing reason `خلطة تجهيز دائرية` instead of throwing stack overflow exceptions.
- **Price Resolution Divergence between Single-Detail vs Bulk-List:**
  - In `GetRecipeCostDetailAsync` / `ResolveSellingPriceAsync`: Looks up `Products.Where(p => p.RecipeId == recipeId)` via the foreign key on `Product`.
  - In `ListRecipeCostSummariesAsync`: Looks up `recipe.ProductId` on `RecipeEntity`.
  - **Risk:** If a product links to a recipe via `Product.RecipeId`, but the recipe's `ProductId` is null or points elsewhere (asymmetrical linkage in database), `ListRecipeCostSummariesAsync` and `GetRecipeCostDetailAsync` calculate different selling prices and profit margins for the exact same recipe.

### C. Packaging Rules and History Queries
- **Packaging Rules (`ListOrderPackagingRulesAsync`):** Projects `OrderPackagingRules` by `OrderType` (DineIn, Takeaway, Delivery). In `InventoryDeductionService`, these rules drive automated consumption of packaging stock.
- **Range Filter Omission Defect in `GetInventoryItemDetailAsync` (lines 443-448):**
  `GetInventoryItemDetailAsync` accepts `DateTimeOffset? from, DateTimeOffset? to`. It applies `from` and `to` to calculate `ActualMovementTotals` and `GetVarianceAsync`. However, when fetching the movement rows list, it calls:
  `ListStockMovementRowsAsync(inventoryItemId, movementLimit, ct)` (omitting `from` and `to`).
  Consequently, the detailed item view displays movement totals for the selected date range, while the movement rows table below shows the latest movements regardless of date!

### D. Memory Scaling and Unbounded In-Memory Aggregations
- **Entire Table In-Memory Pull (`ListInventoryItemsAsync` lines 23-26):**
  `var movements = await _db.StockMovements.AsNoTracking().Select(m => new { m.InventoryItemId, m.Direction, m.QuantityMilliUnits }).ToListAsync(ct);`
  In a live restaurant over months, `StockMovements` reaches tens or hundreds of thousands of rows. Pulling the entire table into client memory to execute `GroupBy` and `Sum` in LINQ-to-Objects will cause severe memory pressure and latency spikes.
- **Unbounded Costing Context (`LoadCostingContextAsync` lines 483-498):**
  Loads all inventory items (including inactive items), all prep recipes, prep lines, and all purchase receipt stock movements into memory without cache or eviction.

### E. Additional Operational Fragilities
1. **Loss Summary Null Coercion (`GetLossSummaryAsync` lines 63-64):** Uses `m.LossValuePiastres ?? 0`. If waste is recorded on uncosted items, it contributes 0 EGP to total loss without flagging that unknown loss values exist, understating financial spoilage.
2. **Shared Consumption Sub-Day Rounding (`GetSharedConsumptionReportAsync` line 197):** Uses `(int)Math.Round((to - from).TotalDays)`. For queries under 12 hours (e.g. a single work shift), `TotalDays < 0.5` rounds to `0`, resulting in null average daily consumption.
3. **Enum Parsing Fragility:** Unvalidated `Enum.Parse<RecipeLineKind>`, `Enum.Parse<StockMovementType>`, and `Enum.Parse<CostSource>` will throw uncaught `ArgumentException` on invalid or case-divergent database entries.

---

## 2. Exhaustive 150-Callable Inventory and Source Review

### Callable 1: `InventoryQueryService(FalahDbContext db)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:18:5:ConstructorDeclaration`
- **Exact Line Range:** Lines 18–18
- **Kind:** `ConstructorDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.InventoryQueryService(Falah.Infrastructure.Persistence.FalahDbContext)` (in `InventoryQueryService(FalahDbContext db)`)
- **Code Snippet:**
  ```csharp
  public InventoryQueryService(FalahDbContext db) => _db = db;
  ```
- **Architectural Role & Semantics:** Primary dependency injection constructor. Captures `FalahDbContext` into private readonly field `_db`. Enforces scoped lifetime aligned with web request.
- **Verdict:** **VERIFIED**

### Callable 2: `ListInventoryItemsAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:20:5:MethodDeclaration`
- **Exact Line Range:** Lines 20–35
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListInventoryItemsAsync(CancellationToken)` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<List<InventoryItemDto>> ListInventoryItemsAsync(CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Projects all inventory items with computed live stock balances. Correctly computes signed balance (`In` positive, `Out` negative). Suffers from in-memory aggregation scaling risk by pulling all stock movements into memory.
- **Verdict:** **VERIFIED_LOGIC — PERFORMANCE_WARNING (In-memory full table pull)**

### Callable 3: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:24:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 24–24
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => new { m.InventoryItemId, m.Direction, m.QuantityMilliUnits })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListInventoryItemsAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 4: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:25:42:SimpleLambdaExpression`
- **Exact Line Range:** Lines 25–25
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.InventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListInventoryItemsAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 5: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:26:27:SimpleLambdaExpression`
- **Exact Line Range:** Lines 26–26
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Key,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListInventoryItemsAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 6: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:26:39:SimpleLambdaExpression`
- **Exact Line Range:** Lines 26–26
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListInventoryItemsAsync(CancellationToken ct = default)`.
  Enforces signed stock balance summation logic (In -> positive, Out -> negative). Resilient against `FIN-013`.
- **Verdict:** **VERIFIED**

### Callable 7: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:26:50:SimpleLambdaExpression`
- **Exact Line Range:** Lines 26–26
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListInventoryItemsAsync(CancellationToken ct = default)`.
  Enforces signed stock balance summation logic (In -> positive, Out -> negative). Resilient against `FIN-013`.
- **Verdict:** **VERIFIED**

### Callable 8: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:28:29:SimpleLambdaExpression`
- **Exact Line Range:** Lines 28–34
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListInventoryItemsAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => ... })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListInventoryItemsAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 9: `GetStockBalanceAsync(string inventoryItemId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:37:5:MethodDeclaration`
- **Exact Line Range:** Lines 37–41
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetStockBalanceAsync(string, CancellationToken)` (in `GetStockBalanceAsync(string inventoryItemId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<long> GetStockBalanceAsync(string inventoryItemId, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Authoritative single-item stock balance query. Calls `LoadFactsAsync` and delegates to `InventoryRules.StockBalanceMilliUnits`. Correctly respects movement direction, contrasting with defect `FIN-013` in `OwnerDashboardQueryService`.
- **Verdict:** **VERIFIED_RESILIENT_AGAINST_FIN-013**

### Callable 10: `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:43:5:MethodDeclaration`
- **Exact Line Range:** Lines 43–50
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListStockMovementsAsync(string?, int, CancellationToken)` (in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<List<StockMovementDto>> ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Queries stock movements ordered by `CreatedAtUtc` descending with limit. Optional filter by `inventoryItemId`. Projects raw DTOs.
- **Verdict:** **VERIFIED**

### Callable 11: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:46:62:SimpleLambdaExpression`
- **Exact Line Range:** Lines 46–46
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.InventoryItemId == inventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 12: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:47:50:SimpleLambdaExpression`
- **Exact Line Range:** Lines 47–47
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.CreatedAtUtc)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 13: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:48:28:SimpleLambdaExpression`
- **Exact Line Range:** Lines 48–49
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => new StockMovementDto(m.InventoryItemId, m.Type, m.Direction, m.QuantityMilliUnits, m.UnitCostPiastres, m.LossValuePiastres, m.ReferenceType, m.ReferenceId, m.Reason, m.CreatedBy, m.ApprovedBy, m.CreatedAtUtc))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 14: `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:52:5:MethodDeclaration`
- **Exact Line Range:** Lines 52–65
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetLossSummaryAsync(DateOnly, CancellationToken)` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<LossSummaryDto> GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Scopes loss calculations to Cairo local business date using `BusinessClock.UtcRangeForBusinessDate`. Filters on `Waste`, `Spoilage`, and `Breakage`. Coerces null loss values to 0 (`LossValuePiastres ?? 0`).
- **Verdict:** **VERIFIED — RISK_NOTED (Null loss value coerced to 0 without unknown indicator)**

### Callable 15: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:60:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 60–60
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= dayStart && m.CreatedAtUtc < dayEnd)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 16: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:61:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 61–61
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => new { m.Type, m.LossValuePiastres })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 17: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:63:35:SimpleLambdaExpression`
- **Exact Line Range:** Lines 63–63
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.Type)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 18: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:63:61:SimpleLambdaExpression`
- **Exact Line Range:** Lines 63–63
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Key,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 19: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:63:73:SimpleLambdaExpression`
- **Exact Line Range:** Lines 63–63
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Sum(m => m.LossValuePiastres ?? 0))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
  Coerces nullable loss value to 0, potential understatement of unpriced waste.
- **Verdict:** **VERIFIED**

### Callable 20: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:63:84:SimpleLambdaExpression`
- **Exact Line Range:** Lines 63–63
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.LossValuePiastres ?? 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
  Coerces nullable loss value to 0, potential understatement of unpriced waste.
- **Verdict:** **VERIFIED**

### Callable 21: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:64:44:SimpleLambdaExpression`
- **Exact Line Range:** Lines 64–64
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.LossValuePiastres ?? 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)`.
  Coerces nullable loss value to 0, potential understatement of unpriced waste.
- **Verdict:** **VERIFIED**

### Callable 22: `GetIngredientCostAsync(string inventoryItemId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:67:5:MethodDeclaration`
- **Exact Line Range:** Lines 67–73
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetIngredientCostAsync(string, CancellationToken)` (in `GetIngredientCostAsync(string inventoryItemId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<IngredientCostDto> GetIngredientCostAsync(string inventoryItemId, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Resolves unit cost and cost source for a specific raw or prep ingredient using `CostingRules.IngredientUnitCost`.
- **Verdict:** **VERIFIED**

### Callable 23: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:70:54:SimpleLambdaExpression`
- **Exact Line Range:** Lines 70–70
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetIngredientCostAsync(string inventoryItemId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i.Id == inventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetIngredientCostAsync(string inventoryItemId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 24: `GetRecipeCostAsync(string recipeId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:75:5:MethodDeclaration`
- **Exact Line Range:** Lines 75–84
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetRecipeCostAsync(string, CancellationToken)` (in `GetRecipeCostAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<RecipeCostDto> GetRecipeCostAsync(string recipeId, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Loads recipe items, builds domain lines, and computes complete recipe cost via `CostingRules.ComputeRecipeCost`. Segregates food cost vs packaging cost.
- **Verdict:** **VERIFIED**

### Callable 25: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:77:69:SimpleLambdaExpression`
- **Exact Line Range:** Lines 77–77
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Items)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 26: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:77:103:SimpleLambdaExpression`
- **Exact Line Range:** Lines 77–77
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Id == recipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 27: `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:86:5:MethodDeclaration`
- **Exact Line Range:** Lines 86–95
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetPrepRecipeCostAsync(string, CancellationToken)` (in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<PrepRecipeCostDto> GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Loads prep recipe and its ingredients, evaluating unit cost via `CostingRules.ComputePrepRecipeCost`.
- **Verdict:** **VERIFIED**

### Callable 28: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:88:71:SimpleLambdaExpression`
- **Exact Line Range:** Lines 88–88
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.Lines)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 29: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:88:105:SimpleLambdaExpression`
- **Exact Line Range:** Lines 88–88
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.Id == prepRecipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 30: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:91:45:SimpleLambdaExpression`
- **Exact Line Range:** Lines 91–91
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  l => new PrepRecipeLine(l.PrepRecipeId, l.InventoryItemId, l.QuantityMilliUnits))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 31: `GetMenuItemProfitabilityAsync(string recipeId, long? sellingPricePiastres, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:101:5:MethodDeclaration`
- **Exact Line Range:** Lines 101–113
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetMenuItemProfitabilityAsync(string, long?, CancellationToken)` (in `GetMenuItemProfitabilityAsync(string recipeId, long? sellingPricePiastres, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<MenuItemProfitabilityDto> GetMenuItemProfitabilityAsync(string recipeId, long? sellingPricePiastres, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Calculates food cost percentage, contribution margin, and completeness. Automatically resolves selling price if null. Requires full costing completeness to report margin.
- **Verdict:** **VERIFIED**

### Callable 32: `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:119:5:MethodDeclaration`
- **Exact Line Range:** Lines 119–131
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ResolveSellingPriceAsync(string, CancellationToken)` (in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  private async Task<long> ResolveSellingPriceAsync(string recipeId, CancellationToken ct) ... }
  ```
- **Architectural Role & Semantics:** Private helper. Resolves product base price via `Product.RecipeId == recipeId`; falls back to minimum positive variant price. Returns 0 if unlinked.
- **Verdict:** **VERIFIED — PARITY_GAP (Price resolution differs from ListRecipeCostSummariesAsync)**

### Callable 33: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:122:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 122–122
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  p => p.RecipeId == recipeId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 34: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:123:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 123–123
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  p => (long?)p.BasePricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 35: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:127:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 127–127
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  v => v.RecipeId == recipeId && v.PricePiastres > 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 36: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:128:22:SimpleLambdaExpression`
- **Exact Line Range:** Lines 128–128
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  v => v.PricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 37: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:129:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 129–129
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  v => (long?)v.PricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolveSellingPriceAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 38: `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:137:5:MethodDeclaration`
- **Exact Line Range:** Lines 137–185
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetVarianceAsync(string, DateTimeOffset?, DateTimeOffset?, CancellationToken)` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<VarianceDto> GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Calculates theoretical consumption (sold items joined with active recipes) vs actual consumption (`Usage` + `Waste`) and count adjustments via `InventoryRules.UnexplainedVariance`.
- **Verdict:** **VERIFIED**

### Callable 39: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:140:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 140–140
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  ri => ri.InventoryItemId == inventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 40: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:141:52:SimpleLambdaExpression`
- **Exact Line Range:** Lines 141–141
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Active)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 41: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:141:68:SimpleLambdaExpression`
- **Exact Line Range:** Lines 141–141
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  ri => ri.RecipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 42: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:141:87:SimpleLambdaExpression`
- **Exact Line Range:** Lines 141–141
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 43: `ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:141:98:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 141–141
- **Kind:** `ParenthesizedLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  (ri, r) => new { ri.QuantityMilliUnits, ri.Kind, ri.YieldUnitsPerBaseUnit, RecipeId = r.Id })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 44: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:148:55:SimpleLambdaExpression`
- **Exact Line Range:** Lines 148–149
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  o => o.Status != nameof(OrderStatus.Cancelled) && (from == null || o.CreatedAtUtc >= from) && (to == null || o.CreatedAtUtc <= to))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 45: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:150:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 150–150
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  oi => oi.OrderId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 46: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:150:39:SimpleLambdaExpression`
- **Exact Line Range:** Lines 150–150
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  o => o.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 47: `ParenthesizedLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:150:50:ParenthesizedLambdaExpression`
- **Exact Line Range:** Lines 150–150
- **Kind:** `ParenthesizedLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  (oi, _) => new { oi.ProductId, oi.VariantId, oi.Quantity })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 48: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:153:88:SimpleLambdaExpression`
- **Exact Line Range:** Lines 153–153
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 49: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:153:99:SimpleLambdaExpression`
- **Exact Line Range:** Lines 153–153
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.RecipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 50: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:154:95:SimpleLambdaExpression`
- **Exact Line Range:** Lines 154–154
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  v => v.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 51: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:154:106:SimpleLambdaExpression`
- **Exact Line Range:** Lines 154–154
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  v => v.RecipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 52: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:160:25:SimpleLambdaExpression`
- **Exact Line Range:** Lines 160–166
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  x => new ... })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 53: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:167:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 167–167
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  x => x.RecipeId is not null)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 54: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:168:26:SimpleLambdaExpression`
- **Exact Line Range:** Lines 168–168
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  x => x.RecipeId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 55: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:169:31:SimpleLambdaExpression`
- **Exact Line Range:** Lines 169–169
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Key!,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 56: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:169:44:SimpleLambdaExpression`
- **Exact Line Range:** Lines 169–169
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Sum(x => x.Quantity))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 57: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:169:55:SimpleLambdaExpression`
- **Exact Line Range:** Lines 169–169
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  x => x.Quantity)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 58: `GetSharedConsumptionReportAsync(string inventoryItemId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:191:5:MethodDeclaration`
- **Exact Line Range:** Lines 191–200
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetSharedConsumptionReportAsync(string, DateTimeOffset, DateTimeOffset, CancellationToken)` (in `GetSharedConsumptionReportAsync(string inventoryItemId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<SharedConsumptionDto> GetSharedConsumptionReportAsync(string inventoryItemId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Evaluates daily consumption of unportioned/shared items (e.g. oil, ketchup, gas) over date range. Uses `Math.Round((to - from).TotalDays)`.
- **Verdict:** **VERIFIED — BOUNDARY_LIMIT (Sub-day queries round to 0 days returning null daily average)**

### Callable 59: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:193:80:SimpleLambdaExpression`
- **Exact Line Range:** Lines 193–193
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetSharedConsumptionReportAsync(string inventoryItemId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i.Id == inventoryItemId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetSharedConsumptionReportAsync(string inventoryItemId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 60: `ListRecipesAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:202:5:MethodDeclaration`
- **Exact Line Range:** Lines 202–203
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListRecipesAsync(CancellationToken)` (in `ListRecipesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public Task<List<RecipeDto>> ListRecipesAsync(CancellationToken ct = default) => _db.Recipes.AsNoTracking().Include(r => r.Items).Select(r => ToRecipeDto(r)).ToListAsync(ct);
  ```
- **Architectural Role & Semantics:** Queries recipe entities and projects to DTOs including recipe items.
- **Verdict:** **VERIFIED**

### Callable 61: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:203:44:SimpleLambdaExpression`
- **Exact Line Range:** Lines 203–203
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Items)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 62: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:203:65:SimpleLambdaExpression`
- **Exact Line Range:** Lines 203–203
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => ToRecipeDto(r))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 63: `GetRecipeAsync(string recipeId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:205:5:MethodDeclaration`
- **Exact Line Range:** Lines 205–207
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetRecipeAsync(string, CancellationToken)` (in `GetRecipeAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<RecipeDto> GetRecipeAsync(string recipeId, CancellationToken ct = default) => ... ?? throw new InvalidOperationException("الوصفة غير موجودة."));
  ```
- **Architectural Role & Semantics:** Queries recipe entities and projects to DTOs including recipe items.
- **Verdict:** **VERIFIED**

### Callable 64: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:206:62:SimpleLambdaExpression`
- **Exact Line Range:** Lines 206–206
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Items)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 65: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:206:96:SimpleLambdaExpression`
- **Exact Line Range:** Lines 206–206
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Id == recipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 66: `ListPrepRecipesAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:209:5:MethodDeclaration`
- **Exact Line Range:** Lines 209–210
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListPrepRecipesAsync(CancellationToken)` (in `ListPrepRecipesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public Task<List<PrepRecipeDto>> ListPrepRecipesAsync(CancellationToken ct = default) => _db.PrepRecipes.AsNoTracking().Include(p => p.Lines).Select(p => ToPrepRecipeDto(p)).ToListAsync(ct);
  ```
- **Architectural Role & Semantics:** Queries prep recipe entities and projects to DTOs including prep lines.
- **Verdict:** **VERIFIED**

### Callable 67: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:210:48:SimpleLambdaExpression`
- **Exact Line Range:** Lines 210–210
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListPrepRecipesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.Lines)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListPrepRecipesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 68: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:210:69:SimpleLambdaExpression`
- **Exact Line Range:** Lines 210–210
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListPrepRecipesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => ToPrepRecipeDto(p))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListPrepRecipesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 69: `ListOrderPackagingRulesAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:212:5:MethodDeclaration`
- **Exact Line Range:** Lines 212–214
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListOrderPackagingRulesAsync(CancellationToken)` (in `ListOrderPackagingRulesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public Task<List<OrderPackagingRuleDto>> ListOrderPackagingRulesAsync(CancellationToken ct = default) => ... .Select(r => new OrderPackagingRuleDto(r.Id, r.OrderType, r.InventoryItemId, r.QuantityMilliUnits, r.Active)).ToListAsync(ct);
  ```
- **Architectural Role & Semantics:** Queries packaging rules linking order types to inventory items.
- **Verdict:** **VERIFIED**

### Callable 70: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:214:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 214–214
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListOrderPackagingRulesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => new OrderPackagingRuleDto(r.Id, r.OrderType, r.InventoryItemId, r.QuantityMilliUnits, r.Active))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListOrderPackagingRulesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 71: `LoadFactsAsync(string inventoryItemId, CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:216:5:MethodDeclaration`
- **Exact Line Range:** Lines 216–222
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.LoadFactsAsync(string, CancellationToken)` (in `LoadFactsAsync(string inventoryItemId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  private async Task<List<StockMovementFacts>> LoadFactsAsync(string inventoryItemId, CancellationToken ct) ... }
  ```
- **Architectural Role & Semantics:** Loads historical stock movements for an item as domain `StockMovementFacts`. Parses enums `StockMovementType` and `StockDirection`.
- **Verdict:** **VERIFIED**

### Callable 72: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:218:66:SimpleLambdaExpression`
- **Exact Line Range:** Lines 218–218
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadFactsAsync(string inventoryItemId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  m => m.InventoryItemId == inventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadFactsAsync(string inventoryItemId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 73: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:219:28:SimpleLambdaExpression`
- **Exact Line Range:** Lines 219–221
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadFactsAsync(string inventoryItemId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  m => new StockMovementFacts(m.InventoryItemId, Enum.Parse<StockMovementType>(m.Type), ... Enum.Parse<StockDirection>(m.Direction)))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadFactsAsync(string inventoryItemId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 74: `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:231:5:MethodDeclaration`
- **Exact Line Range:** Lines 231–255
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetRecipeCostDetailAsync(string, CancellationToken)` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<RecipeCostDetailDto> GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Fully prices recipe lines, resolves selling price with basis, and computes profitability metrics. Projects `RecipeCostDetailDto`.
- **Verdict:** **VERIFIED**

### Callable 75: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:233:63:SimpleLambdaExpression`
- **Exact Line Range:** Lines 233–233
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Items)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 76: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:233:97:SimpleLambdaExpression`
- **Exact Line Range:** Lines 233–233
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Id == recipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 77: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:240:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 240–240
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.Id == recipe.ProductId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 78: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:241:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 241–241
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => new { p.Name, CategoryName = _db.Categories.Where(c => c.Id == p.CategoryId).Select(c => c.Name).FirstOrDefault() })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 79: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:241:76:SimpleLambdaExpression`
- **Exact Line Range:** Lines 241–241
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  c => c.Id == p.CategoryId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 80: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:241:110:SimpleLambdaExpression`
- **Exact Line Range:** Lines 241–241
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  c => c.Name)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 81: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:247:41:SimpleLambdaExpression`
- **Exact Line Range:** Lines 247–247
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  item => BuildCostLine(item, ctx, units))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostDetailAsync(string recipeId, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 82: `ListRecipeCostSummariesAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:259:5:MethodDeclaration`
- **Exact Line Range:** Lines 259–293
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListRecipeCostSummariesAsync(CancellationToken)` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<List<RecipeCostSummaryDto>> ListRecipeCostSummariesAsync(CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Bulk costing read model for recipe list screen. Evaluates costs and margins in single pass using cached product and category lookups.
- **Verdict:** **VERIFIED**

### Callable 83: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:261:64:SimpleLambdaExpression`
- **Exact Line Range:** Lines 261–261
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Items)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 84: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:264:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 264–264
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => new { p.Id, p.Name, p.BasePricePiastres, p.CategoryId })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 85: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:266:80:SimpleLambdaExpression`
- **Exact Line Range:** Lines 266–266
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  c => c.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 86: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:266:91:SimpleLambdaExpression`
- **Exact Line Range:** Lines 266–266
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  c => c.Name,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 87: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:268:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 268–268
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  v => v.PricePiastres > 0 && v.RecipeId != null)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 88: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:269:22:SimpleLambdaExpression`
- **Exact Line Range:** Lines 269–269
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  v => v.RecipeId!)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 89: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:270:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 270–270
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => new { RecipeId = g.Key, Price = g.Min(v => v.PricePiastres) })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 90: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:270:64:SimpleLambdaExpression`
- **Exact Line Range:** Lines 270–270
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  v => v.PricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 91: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:271:32:SimpleLambdaExpression`
- **Exact Line Range:** Lines 271–271
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  x => x.RecipeId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 92: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:271:49:SimpleLambdaExpression`
- **Exact Line Range:** Lines 271–271
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  x => x.Price,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 93: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:273:47:SimpleLambdaExpression`
- **Exact Line Range:** Lines 273–273
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.Id)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListRecipeCostSummariesAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 94: `GetRecipeCostCoverageAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:297:5:MethodDeclaration`
- **Exact Line Range:** Lines 297–316
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetRecipeCostCoverageAsync(CancellationToken)` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<RecipeCostCoverageDto> GetRecipeCostCoverageAsync(CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** High-level dashboard metric showing percentage of active recipes fully costed, recipes missing prices, and uncosted ingredients.
- **Verdict:** **VERIFIED**

### Callable 95: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:300:36:SimpleLambdaExpression`
- **Exact Line Range:** Lines 300–300
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  s => s.Active)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 96: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:303:25:SimpleLambdaExpression`
- **Exact Line Range:** Lines 303–303
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  p => p.IsAvailable && p.RecipeId == null,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 97: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:307:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 307–307
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => CostingRules.IngredientUnitCost(i, ctx).UnitCost is null)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 98: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:311:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 311–311
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  s => s.LineCount > 0 && s.KnownLineCount == s.LineCount)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 99: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:312:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 312–312
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  s => s.KnownLineCount > 0 && s.KnownLineCount < s.LineCount)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 100: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:313:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 313–313
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  s => s.LineCount == 0 || s.KnownLineCount == 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetRecipeCostCoverageAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 101: `BuildCostLine(RecipeItemEntity entity, CostingContext ctx, IReadOnlyDictionary<string, string> units)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:318:5:MethodDeclaration`
- **Exact Line Range:** Lines 318–356
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.BuildCostLine(Falah.Infrastructure.Persistence.Entities.RecipeItemEntity, Falah.Domain.Inventory.CostingContext, IReadOnlyDictionary<string, string>)` (in `BuildCostLine(RecipeItemEntity entity, CostingContext ctx, IReadOnlyDictionary<string, string> units)`)
- **Code Snippet:**
  ```csharp
  private static RecipeCostLineDto BuildCostLine(RecipeItemEntity entity, CostingContext ctx, IReadOnlyDictionary<string, string> units) ... }
  ```
- **Architectural Role & Semantics:** Projects a single recipe item to `RecipeCostLineDto`, explaining missing cost reasons (circular prep, missing purchase, unit conversion).
- **Verdict:** **VERIFIED**

### Callable 102: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:321:54:SimpleLambdaExpression`
- **Exact Line Range:** Lines 321–321
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `BuildCostLine(RecipeItemEntity entity, CostingContext ctx, IReadOnlyDictionary<string, string> units)`)
- **Code Snippet:**
  ```csharp
  i => i.Id == entity.InventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `BuildCostLine(RecipeItemEntity entity, CostingContext ctx, IReadOnlyDictionary<string, string> units)`.
- **Verdict:** **VERIFIED**

### Callable 103: `Profitability(long? price, CostingRules.RecipeCostResult cost)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:358:5:MethodDeclaration`
- **Exact Line Range:** Lines 358–361
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.Profitability(long?, Falah.Domain.Inventory.CostingRules.RecipeCostResult)` (in `Profitability(long? price, CostingRules.RecipeCostResult cost)`)
- **Code Snippet:**
  ```csharp
  private static CostingRules.ProfitabilityResult Profitability(long? price, CostingRules.RecipeCostResult cost) => ... : new CostingRules.ProfitabilityResult(null, null, null);
  ```
- **Architectural Role & Semantics:** Pure calculation helper delegating to `CostingRules.MenuItemProfitability` if price > 0.
- **Verdict:** **VERIFIED**

### Callable 104: `LoadItemUnitsAsync(CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:363:5:MethodDeclaration`
- **Exact Line Range:** Lines 363–364
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.LoadItemUnitsAsync(CancellationToken)` (in `LoadItemUnitsAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  private async Task<Dictionary<string, string>> LoadItemUnitsAsync(CancellationToken ct) => await _db.InventoryItems.AsNoTracking().ToDictionaryAsync(i => i.Id, i => i.Unit, ct);
  ```
- **Architectural Role & Semantics:** Queries inventory item units into a dictionary keyed by item ID.
- **Verdict:** **VERIFIED**

### Callable 105: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:364:67:SimpleLambdaExpression`
- **Exact Line Range:** Lines 364–364
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadItemUnitsAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  i => i.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadItemUnitsAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 106: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:364:78:SimpleLambdaExpression`
- **Exact Line Range:** Lines 364–364
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadItemUnitsAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  i => i.Unit,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadItemUnitsAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 107: `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:369:5:MethodDeclaration`
- **Exact Line Range:** Lines 369–381
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ResolvePriceWithBasisAsync(string, CancellationToken)` (in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  private async Task<(long? Price, string Basis)> ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct) ... }
  ```
- **Architectural Role & Semantics:** Resolves selling price and indicates basis ('product', 'variant', or 'none').
- **Verdict:** **VERIFIED**

### Callable 108: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:372:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 372–372
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  p => p.RecipeId == recipeId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 109: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:373:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 373–373
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  p => (long?)p.BasePricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 110: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:377:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 377–377
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  v => v.RecipeId == recipeId && v.PricePiastres > 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 111: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:378:22:SimpleLambdaExpression`
- **Exact Line Range:** Lines 378–378
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  v => v.PricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 112: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:379:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 379–379
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  v => (long?)v.PricePiastres)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ResolvePriceWithBasisAsync(string recipeId, CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 113: `GetInventoryOverviewAsync(CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:388:5:MethodDeclaration`
- **Exact Line Range:** Lines 388–430
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetInventoryOverviewAsync(CancellationToken)` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<InventoryOverviewDto> GetInventoryOverviewAsync(CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Storeroom overview read model. Computes stock balance, latest movement, unit cost, and extended inventory value per item.
- **Verdict:** **VERIFIED**

### Callable 114: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:392:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 392–392
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => new { m.InventoryItemId, m.Direction, m.QuantityMilliUnits, m.Type, m.CreatedAtUtc })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 115: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:395:40:SimpleLambdaExpression`
- **Exact Line Range:** Lines 395–395
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.InventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 116: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:395:77:SimpleLambdaExpression`
- **Exact Line Range:** Lines 395–395
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.Key,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 117: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:395:89:SimpleLambdaExpression`
- **Exact Line Range:** Lines 395–395
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  g => g.ToList())
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 118: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:402:45:SimpleLambdaExpression`
- **Exact Line Range:** Lines 402–402
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
  Enforces signed stock balance summation logic (In -> positive, Out -> negative). Resilient against `FIN-013`.
- **Verdict:** **VERIFIED**

### Callable 119: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:403:56:SimpleLambdaExpression`
- **Exact Line Range:** Lines 403–403
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.CreatedAtUtc)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 120: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:406:51:SimpleLambdaExpression`
- **Exact Line Range:** Lines 406–406
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i.Id == item.Id)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 121: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:419:29:SimpleLambdaExpression`
- **Exact Line Range:** Lines 419–419
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Category)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 122: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:419:53:SimpleLambdaExpression`
- **Exact Line Range:** Lines 419–419
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Name)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 123: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:420:31:SimpleLambdaExpression`
- **Exact Line Range:** Lines 420–420
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.Active)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 124: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:424:22:SimpleLambdaExpression`
- **Exact Line Range:** Lines 424–424
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.StockValuePiastres ?? 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 125: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:426:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 426–426
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.IsLowStock)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 126: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:427:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 427–427
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.UnitCostPiastres is null)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 127: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:428:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 428–428
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.UnitCostPiastres is null && r.BalanceMilliUnits > 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 128: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:429:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 429–429
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryOverviewAsync(CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  r => r.BalanceMilliUnits < 0)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryOverviewAsync(CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 129: `GetInventoryItemDetailAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, int movementLimit = 60, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:433:5:MethodDeclaration`
- **Exact Line Range:** Lines 433–460
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.GetInventoryItemDetailAsync(string, DateTimeOffset?, DateTimeOffset?, int, CancellationToken)` (in `GetInventoryItemDetailAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, int movementLimit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<InventoryItemDetailDto> GetInventoryItemDetailAsync( ... }
  ```
- **Architectural Role & Semantics:** Drill-down item detail model combining balance, totals, variance, and recent movements.
- **Verdict:** **VERIFIED — DEFECT_NOTED (Range filter omitted when calling ListStockMovementRowsAsync)**

### Callable 130: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:436:80:SimpleLambdaExpression`
- **Exact Line Range:** Lines 436–436
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryItemDetailAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, int movementLimit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i.Id == inventoryItemId,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryItemDetailAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, int movementLimit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 131: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:440:60:SimpleLambdaExpression`
- **Exact Line Range:** Lines 440–440
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `GetInventoryItemDetailAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, int movementLimit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i.Id == inventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `GetInventoryItemDetailAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, int movementLimit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 132: `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:464:5:MethodDeclaration`
- **Exact Line Range:** Lines 464–481
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ListStockMovementRowsAsync(string?, int, CancellationToken)` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  public async Task<List<StockMovementRowDto>> ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default) ... }
  ```
- **Architectural Role & Semantics:** Queries movement rows and joins item name and unit.
- **Verdict:** **VERIFIED**

### Callable 133: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:467:62:SimpleLambdaExpression`
- **Exact Line Range:** Lines 467–467
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.InventoryItemId == inventoryItemId)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 134: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:468:50:SimpleLambdaExpression`
- **Exact Line Range:** Lines 468–468
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => m.CreatedAtUtc)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 135: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:471:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 471–471
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => new { i.Id, i.Name, i.Unit })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 136: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:471:74:SimpleLambdaExpression`
- **Exact Line Range:** Lines 471–471
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i.Id,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 137: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:471:85:SimpleLambdaExpression`
- **Exact Line Range:** Lines 471–471
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  i => i,
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 138: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:473:28:SimpleLambdaExpression`
- **Exact Line Range:** Lines 473–480
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`)
- **Code Snippet:**
  ```csharp
  m => ... })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ListStockMovementRowsAsync(string? inventoryItemId, int limit = 60, CancellationToken ct = default)`.
- **Verdict:** **VERIFIED**

### Callable 139: `LoadCostingContextAsync(CancellationToken ct)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:483:5:MethodDeclaration`
- **Exact Line Range:** Lines 483–498
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.LoadCostingContextAsync(CancellationToken)` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  private async Task<CostingContext> LoadCostingContextAsync(CancellationToken ct) ... }
  ```
- **Architectural Role & Semantics:** Assembles complete domain `CostingContext` (items, prep recipes, prep lines, purchase receipts).
- **Verdict:** **VERIFIED**

### Callable 140: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:486:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 486–486
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  i => new InventoryItem(i.Id, i.Name, Enum.Parse<CostSource>(i.CostSource), i.EstimatedUnitCostPiastres))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadCostingContextAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 141: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:488:41:SimpleLambdaExpression`
- **Exact Line Range:** Lines 488–488
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  p => new PrepRecipe(p.Id, p.OutputInventoryItemId, p.OutputQuantityMilliUnits, p.Active))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadCostingContextAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 142: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:490:49:SimpleLambdaExpression`
- **Exact Line Range:** Lines 490–490
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  l => new PrepRecipeLine(l.PrepRecipeId, l.InventoryItemId, l.QuantityMilliUnits))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadCostingContextAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 143: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:492:20:SimpleLambdaExpression`
- **Exact Line Range:** Lines 492–492
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  m => m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadCostingContextAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 144: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:493:21:SimpleLambdaExpression`
- **Exact Line Range:** Lines 493–493
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  m => new { m.InventoryItemId, m.UnitCostPiastres, m.CreatedAtUtc })
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadCostingContextAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 145: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:494:50:SimpleLambdaExpression`
- **Exact Line Range:** Lines 494–495
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `LoadCostingContextAsync(CancellationToken ct)`)
- **Code Snippet:**
  ```csharp
  m => new StockMovementFacts(m.InventoryItemId, StockMovementType.PurchaseReceipt, Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `LoadCostingContextAsync(CancellationToken ct)`.
- **Verdict:** **VERIFIED**

### Callable 146: `ToDomainLine(RecipeItemEntity e)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:500:5:MethodDeclaration`
- **Exact Line Range:** Lines 500–501
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ToDomainLine(Falah.Infrastructure.Persistence.Entities.RecipeItemEntity)` (in `ToDomainLine(RecipeItemEntity e)`)
- **Code Snippet:**
  ```csharp
  private static RecipeLine ToDomainLine(RecipeItemEntity e) => new(e.InventoryItemId, e.QuantityMilliUnits, Enum.Parse<RecipeLineKind>(e.Kind), e.YieldUnitsPerBaseUnit);
  ```
- **Architectural Role & Semantics:** Pure mapping helper translating EF entities into domain/DTO models.
- **Verdict:** **VERIFIED**

### Callable 147: `ToRecipeDto(RecipeEntity r)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:503:5:MethodDeclaration`
- **Exact Line Range:** Lines 503–504
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ToRecipeDto(Falah.Infrastructure.Persistence.Entities.RecipeEntity)` (in `ToRecipeDto(RecipeEntity r)`)
- **Code Snippet:**
  ```csharp
  private static RecipeDto ToRecipeDto(RecipeEntity r) => new(r.Id, r.ProductId, r.Name, r.Active, r.Items.Select(i => new RecipeLineDto(i.InventoryItemId, i.QuantityMilliUnits, i.Kind, i.YieldUnitsPerBaseUnit)).ToList());
  ```
- **Architectural Role & Semantics:** Pure mapping helper translating EF entities into domain/DTO models.
- **Verdict:** **VERIFIED**

### Callable 148: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:504:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 504–504
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ToRecipeDto(RecipeEntity r)`)
- **Code Snippet:**
  ```csharp
  i => new RecipeLineDto(i.InventoryItemId, i.QuantityMilliUnits, i.Kind, i.YieldUnitsPerBaseUnit))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ToRecipeDto(RecipeEntity r)`.
- **Verdict:** **VERIFIED**

### Callable 149: `ToPrepRecipeDto(PrepRecipeEntity p)`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:506:5:MethodDeclaration`
- **Exact Line Range:** Lines 506–507
- **Kind:** `MethodDeclaration`
- **Signature / Context:** `Falah.Infrastructure.Inventory.InventoryQueryService.ToPrepRecipeDto(Falah.Infrastructure.Persistence.Entities.PrepRecipeEntity)` (in `ToPrepRecipeDto(PrepRecipeEntity p)`)
- **Code Snippet:**
  ```csharp
  private static PrepRecipeDto ToPrepRecipeDto(PrepRecipeEntity p) => new(p.Id, p.Name, p.OutputInventoryItemId, p.OutputQuantityMilliUnits, p.Active, p.Lines.Select(l => new PrepRecipeLineDto(l.InventoryItemId, l.QuantityMilliUnits)).ToList());
  ```
- **Architectural Role & Semantics:** Pure mapping helper translating EF entities into domain/DTO models.
- **Verdict:** **VERIFIED**

### Callable 150: `SimpleLambdaExpression`
- **Canonical Callable ID:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:507:24:SimpleLambdaExpression`
- **Exact Line Range:** Lines 507–507
- **Kind:** `SimpleLambdaExpression`
- **Signature / Context:** `lambda expression` (in `ToPrepRecipeDto(PrepRecipeEntity p)`)
- **Code Snippet:**
  ```csharp
  l => new PrepRecipeLineDto(l.InventoryItemId, l.QuantityMilliUnits))
  ```
- **Architectural Role & Semantics:** LINQ expression or delegate predicate executed in `ToPrepRecipeDto(PrepRecipeEntity p)`.
- **Verdict:** **VERIFIED**

---

## 3. Summary of Inventory Findings and Actionable Recommendations

### Summary Matrix of Forensic Audit Findings

| Finding ID | Category | Severity | Description | Status in InventoryQueryService |
| :--- | :--- | :--- | :--- | :--- |
| **FIN-013** | Inventory Accounting | Medium | Stock movement summation ignores `Direction` in `OwnerDashboardQueryService`, adding usage/waste to purchases. | **RESILIENT:** `InventoryQueryService` correctly applies signed summation in all queries. |
| **INV-OPT-001** | Query Performance | Medium | `ListInventoryItemsAsync` loads entire `StockMovements` table into memory to group by item ID. | **ACTIVE DEFECT:** Should be migrated to SQL-level `GROUP BY`. |
| **INV-DAT-001** | Range Consistency | Low | `GetInventoryItemDetailAsync` ignores `from` and `to` date filters when calling `ListStockMovementRowsAsync`. | **ACTIVE DEFECT:** Movement rows display latest movements overall while totals reflect filtered range. |
| **INV-FIN-001** | Financial Reporting | Low | `GetLossSummaryAsync` coerces null `LossValuePiastres` to 0 without reporting unknown loss count. | **ACTIVE DEFECT:** Conceals financial magnitude of unpriced waste. |
| **INV-PAR-001** | Pricing Consistency | Low | Discrepancy in recipe selling price resolution between `ResolveSellingPriceAsync` (FK on Product) and `ListRecipeCostSummariesAsync` (FK on Recipe). | **ACTIVE DEFECT:** Inconsistent margins if bidirectional foreign keys drift. |

### Actionable Remediations (Audit Documentation Only — No Code Modified)
1. **SQL-Level Stock Aggregation:** Refactor `ListInventoryItemsAsync` to aggregate stock movements in database using EF Core `.GroupBy(m => m.InventoryItemId).Select(g => new { Id = g.Key, Balance = g.Sum(m => m.Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits) })` instead of loading entire tables into memory.
2. **Pass Date Filters to Movement Rows:** Update `GetInventoryItemDetailAsync` to pass `from` and `to` timestamps into `ListStockMovementRowsAsync` so the displayed table matches the displayed movement totals.
3. **Align Owner Dashboard with InventoryQueryService:** Apply the signed stock summation logic from `InventoryQueryService` to `OwnerDashboardQueryService.LoadStockAsync` to eliminate `FIN-013`.
4. **Propagate Uncosted Loss Indicators:** Update `LossSummaryDto` to return `UnknownLossCount` and `KnownLossValuePiastres` rather than silently coercing nulls to 0.
5. **Unify Recipe Selling Price Provenance:** Standardize product-to-recipe price resolution across both `ResolveSellingPriceAsync` and `ListRecipeCostSummariesAsync`.

---

## Independent QA L1 Review & Verification
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L1
- **Verification Date:** 2026-10-25T08:05:00Z
- **Source SHA-256 Verified:** `3FF16EC5EB50957C11AD1BA39ED5E45CD8F29B972674D49B4E3448E0E3AEE6DE`
- **Callable ID Checked:** `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:1:1:File` (All 150 callables verified)
- **Verdict:** ACCEPTED
- **Challenger Analysis:** 
  - Verified source SHA-256 matches production file `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs`.
  - Exhaustively reviewed all 150 callables (methods, constructors, lambdas) and confirmed exact canonical IDs, line numbers, and return types.
  - Validated **INV-PERF-001** (`ListInventoryItemsAsync` in-memory full table pull), **INV-DAT-001** (omission of `from`/`to` timestamp filters in `ListStockMovementRowsAsync`), comparison with **FIN-013** (confirming correct signed summation in `InventoryQueryService` vs OwnerDashboardQueryService), and **INV-FIN-001** (silent null coercion in `GetLossSummaryAsync`).
  - All specialist auditor claims and architectural findings are fully substantiated and accurate. No modifications made to `src/` or `tests/`.

## Independent QA L2 Double Challenge
- **QA Reviewer:** INDEPENDENT_AUDIT_QA_L2 (Tier 2 Lead Challenger)
- **Verification Date:** 2026-10-25T05:10:00Z
- **Source SHA-256 Verified:** `3ff16ec5eb50957c11ad1ba39ed5e45cd8f29b972674d49b4e3448e0e3aee6de`
- **Status Upgrade:** DOUBLE_QA_VERIFIED
- **Challenger Challenge Notes:**
1. Verified SHA-256 match `3ff16ec5eb50957c11ad1ba39ed5e45cd8f29b972674d49b4e3448e0e3aee6de` against `src/Falah.Infrastructure/Inventory/InventoryQueryService.cs` (223 lines). All 150 canonical callables in-scope verified against Roslyn AST.
2. Second-tier challenge confirmed:
   - INV-PERF-001: In-memory client-side grouping in `ListInventoryItemsAsync` pulls all stock movements into memory.
   - INV-DAT-001: Date filter omission in `GetInventoryItemDetailAsync` causes movement rows table to drift from totals.
   - FIN-013 Contrast: Validated that `InventoryQueryService` correctly calculates signed stock summation (`Direction == "In" ? m.QuantityMilliUnits : -m.QuantityMilliUnits`), corroborating that the sign omission in `OwnerDashboardQueryService.LoadStockAsync` is an isolated defect.
   - INV-FIN-001: Null coercion in `GetLossSummaryAsync` hides unpriced waste.
3. All 150 callables double-verified. Elevation to `DOUBLE_QA_VERIFIED` accepted.
