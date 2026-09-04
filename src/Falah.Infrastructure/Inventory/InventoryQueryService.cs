namespace Falah.Infrastructure.Inventory;

using Falah.Application.Inventory;
using Falah.Domain.Common;
using Falah.Domain.Inventory;
using Falah.Domain.Orders;
using Falah.Infrastructure.Persistence;
using Falah.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

/// <summary>Read-only projections over Inventory/Recipe/Costing state. Every balance/cost figure is
/// derived at query time from the movement log and current recipe/purchase data - nothing is stored
/// as a separately-maintained running total that could drift (same principle as
/// TreasuryBalanceQuery for cash).</summary>
public sealed class InventoryQueryService
{
    private readonly FalahDbContext _db;
    public InventoryQueryService(FalahDbContext db) => _db = db;

    public async Task<List<InventoryItemDto>> ListInventoryItemsAsync(CancellationToken ct = default)
    {
        var items = await _db.InventoryItems.AsNoTracking().ToListAsync(ct);
        var movements = await _db.StockMovements.AsNoTracking()
            .Select(m => new { m.InventoryItemId, m.Direction, m.QuantityMilliUnits }).ToListAsync(ct);
        var balances = movements.GroupBy(m => m.InventoryItemId)
            .ToDictionary(g => g.Key, g => g.Sum(m => m.Direction == nameof(StockDirection.In) ? m.QuantityMilliUnits : -m.QuantityMilliUnits));

        return items.Select(i =>
        {
            var balance = balances.GetValueOrDefault(i.Id, 0);
            return new InventoryItemDto(i.Id, i.Name, i.Category, i.Unit, i.MinimumMilliUnits, i.Active,
                i.IsPackaging, i.CostSource, i.StandardDailyUsageMilliUnits, balance, balance <= i.MinimumMilliUnits);
        }).ToList();
    }

    public async Task<long> GetStockBalanceAsync(string inventoryItemId, CancellationToken ct = default)
    {
        var movements = await LoadFactsAsync(inventoryItemId, ct);
        return InventoryRules.StockBalanceMilliUnits(movements, inventoryItemId);
    }

    public async Task<List<StockMovementDto>> ListStockMovementsAsync(string? inventoryItemId, int limit = 50, CancellationToken ct = default)
    {
        var query = _db.StockMovements.AsNoTracking().AsQueryable();
        if (inventoryItemId is not null) query = query.Where(m => m.InventoryItemId == inventoryItemId);
        var rows = await query.OrderByDescending(m => m.CreatedAtUtc).Take(limit).ToListAsync(ct);
        return rows.Select(m => new StockMovementDto(m.InventoryItemId, m.Type, m.Direction, m.QuantityMilliUnits,
            m.UnitCostPiastres, m.LossValuePiastres, m.ReferenceType, m.ReferenceId, m.Reason, m.CreatedBy, m.ApprovedBy, m.CreatedAtUtc)).ToList();
    }

    public async Task<LossSummaryDto> GetLossSummaryAsync(DateOnly businessDate, CancellationToken ct = default)
    {
        var dayStart = businessDate.ToDateTime(TimeOnly.MinValue);
        var dayEnd = dayStart.AddDays(1);
        var lossTypes = new[] { nameof(StockMovementType.Waste), nameof(StockMovementType.Spoilage), nameof(StockMovementType.Breakage) };
        var rows = await _db.StockMovements.AsNoTracking()
            .Where(m => lossTypes.Contains(m.Type) && m.CreatedAtUtc >= dayStart && m.CreatedAtUtc < dayEnd)
            .Select(m => new { m.Type, m.LossValuePiastres }).ToListAsync(ct);

        var byType = rows.GroupBy(m => m.Type).ToDictionary(g => g.Key, g => g.Sum(m => m.LossValuePiastres ?? 0));
        return new LossSummaryDto(rows.Sum(m => m.LossValuePiastres ?? 0), rows.Count, byType);
    }

    public async Task<IngredientCostDto> GetIngredientCostAsync(string inventoryItemId, CancellationToken ct = default)
    {
        var ctx = await LoadCostingContextAsync(ct);
        var item = ctx.InventoryItems.FirstOrDefault(i => i.Id == inventoryItemId);
        var result = CostingRules.IngredientUnitCost(item, ctx);
        return new IngredientCostDto(inventoryItemId, result.UnitCost?.Piastres, result.Source.ToString());
    }

    public async Task<RecipeCostDto> GetRecipeCostAsync(string recipeId, CancellationToken ct = default)
    {
        var recipeEntity = await _db.Recipes.AsNoTracking().Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == recipeId, ct)
            ?? throw new InvalidOperationException("الوصفة غير موجودة.");
        var lines = recipeEntity.Items.Select(ToDomainLine).ToList();
        var ctx = await LoadCostingContextAsync(ct);
        var result = CostingRules.ComputeRecipeCost(lines, ctx);
        return new RecipeCostDto(result.KnownCost.Piastres, result.FoodCost.Piastres, result.PackagingCost.Piastres,
            result.CompletenessPercent, result.Missing, result.LineCount, result.KnownLineCount);
    }

    public async Task<PrepRecipeCostDto> GetPrepRecipeCostAsync(string prepRecipeId, CancellationToken ct = default)
    {
        var prepEntity = await _db.PrepRecipes.AsNoTracking().Include(p => p.Lines).FirstOrDefaultAsync(p => p.Id == prepRecipeId, ct)
            ?? throw new InvalidOperationException("الخلطة غير موجودة.");
        var prep = new PrepRecipe(prepEntity.Id, prepEntity.OutputInventoryItemId, prepEntity.OutputQuantityMilliUnits, prepEntity.Active);
        var lines = prepEntity.Lines.Select(l => new PrepRecipeLine(l.PrepRecipeId, l.InventoryItemId, l.QuantityMilliUnits)).ToList();
        var ctx = await LoadCostingContextAsync(ct);
        var result = CostingRules.ComputePrepRecipeCost(prep, lines, ctx);
        return new PrepRecipeCostDto(result.UnitCost?.Piastres, result.KnownCost.Piastres, result.Completeness, result.Missing, result.LineCount, result.KnownLineCount);
    }

    public async Task<MenuItemProfitabilityDto> GetMenuItemProfitabilityAsync(string recipeId, long sellingPricePiastres, CancellationToken ct = default)
    {
        var recipeCost = await GetRecipeCostAsync(recipeId, ct);
        var result = CostingRules.MenuItemProfitability(Money.FromPiastres(sellingPricePiastres), Money.FromPiastres(recipeCost.KnownCostPiastres));
        return new MenuItemProfitabilityDto(result.FoodCostPercent, result.Contribution?.Piastres, result.ContributionPercent);
    }

    /// <summary>Theoretical usage (sales x CURRENT recipe - recipes are not versioned per sale, a
    /// known, stated limitation, not a silent inaccuracy) vs what was actually logged as usage/
    /// waste for one ingredient. A physical count's own net adjustment is surfaced alongside,
    /// never merged into the variance figure - see InventoryRules.UnexplainedVariance.</summary>
    public async Task<VarianceDto> GetVarianceAsync(string inventoryItemId, DateTimeOffset? from, DateTimeOffset? to, CancellationToken ct = default)
    {
        var recipeLineRows = await _db.RecipeItems.AsNoTracking()
            .Where(ri => ri.InventoryItemId == inventoryItemId)
            .Join(_db.Recipes.AsNoTracking().Where(r => r.Active), ri => ri.RecipeId, r => r.Id, (ri, r) => new { ri.QuantityMilliUnits, ri.Kind, ri.YieldUnitsPerBaseUnit, RecipeId = r.Id })
            .ToListAsync(ct);

        long theoretical = 0;
        if (recipeLineRows.Count > 0)
        {
            var orderItemRows = await _db.OrderItems.AsNoTracking()
                .Join(_db.Orders.AsNoTracking().Where(o => o.Status != nameof(OrderStatus.Cancelled)
                        && (from == null || o.CreatedAtUtc >= from) && (to == null || o.CreatedAtUtc <= to)),
                    oi => oi.OrderId, o => o.Id, (oi, _) => new { oi.ProductId, oi.VariantId, oi.Quantity })
                .ToListAsync(ct);

            var productRecipeIds = await _db.Products.AsNoTracking().ToDictionaryAsync(p => p.Id, p => p.RecipeId, ct);
            var variantRecipeIds = await _db.ProductVariants.AsNoTracking().ToDictionaryAsync(v => v.Id, v => v.RecipeId, ct);

            // Effective recipe per sold line: variant's own recipe override, falling back to the
            // product's recipe when the variant has none - exactly `variant?.recipeId ||
            // product?.recipeId` from the reference app's theoreticalIngredientUsage.
            var soldQuantityByRecipe = orderItemRows
                .Select(x => new
                {
                    RecipeId = (x.VariantId is not null && variantRecipeIds.TryGetValue(x.VariantId, out var variantRecipeId) && variantRecipeId is not null)
                        ? variantRecipeId
                        : productRecipeIds.GetValueOrDefault(x.ProductId),
                    x.Quantity,
                })
                .Where(x => x.RecipeId is not null)
                .GroupBy(x => x.RecipeId)
                .ToDictionary(g => g.Key!, g => g.Sum(x => x.Quantity));

            foreach (var line in recipeLineRows)
            {
                var domainLine = new RecipeLine(inventoryItemId, line.QuantityMilliUnits, Enum.Parse<RecipeLineKind>(line.Kind), line.YieldUnitsPerBaseUnit);
                var baseQty = CostingRules.BaseQuantityMilliUnits(domainLine);
                if (baseQty is null) continue;

                theoretical += baseQty.Value * soldQuantityByRecipe.GetValueOrDefault(line.RecipeId, 0);
            }
        }

        var facts = await LoadFactsAsync(inventoryItemId, ct);
        var actual = InventoryRules.ActualMovementTotals(facts, inventoryItemId, from, to);
        var result = InventoryRules.UnexplainedVariance(theoretical, actual);
        return new VarianceDto(result.TheoreticalUsageMilliUnits, result.RecordedConsumptionMilliUnits, result.VarianceMilliUnits, result.CountAdjustmentNetMilliUnits);
    }

    /// <summary>Ported from sharedConsumptionReport in lib/costing-domain.mjs - standard vs actual
    /// daily usage for a shared/communal item (no per-order recipe line) over an explicit date
    /// range, days computed from that range exactly like the reference app's own CostingScreen.tsx
    /// caller does.</summary>
    public async Task<SharedConsumptionDto> GetSharedConsumptionReportAsync(string inventoryItemId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        var item = await _db.InventoryItems.AsNoTracking().FirstOrDefaultAsync(i => i.Id == inventoryItemId, ct)
            ?? throw new InvalidOperationException("الخامة غير موجودة.");
        var facts = await LoadFactsAsync(inventoryItemId, ct);
        var actual = InventoryRules.ActualMovementTotals(facts, inventoryItemId, from, to);
        var days = Math.Max(0, (int)Math.Round((to - from).TotalDays));
        var result = InventoryRules.SharedConsumptionReport(item.StandardDailyUsageMilliUnits, actual, days);
        return new SharedConsumptionDto(result.ActualDailyAvgMilliUnits, result.StandardDailyUsageMilliUnits, result.VarianceMilliUnitsPerDay, result.ConsumedMilliUnits);
    }

    public Task<List<RecipeDto>> ListRecipesAsync(CancellationToken ct = default) =>
        _db.Recipes.AsNoTracking().Include(r => r.Items).Select(r => ToRecipeDto(r)).ToListAsync(ct);

    public async Task<RecipeDto> GetRecipeAsync(string recipeId, CancellationToken ct = default) =>
        ToRecipeDto(await _db.Recipes.AsNoTracking().Include(r => r.Items).FirstOrDefaultAsync(r => r.Id == recipeId, ct)
            ?? throw new InvalidOperationException("الوصفة غير موجودة."));

    public Task<List<PrepRecipeDto>> ListPrepRecipesAsync(CancellationToken ct = default) =>
        _db.PrepRecipes.AsNoTracking().Include(p => p.Lines).Select(p => ToPrepRecipeDto(p)).ToListAsync(ct);

    public Task<List<OrderPackagingRuleDto>> ListOrderPackagingRulesAsync(CancellationToken ct = default) =>
        _db.OrderPackagingRules.AsNoTracking()
            .Select(r => new OrderPackagingRuleDto(r.Id, r.OrderType, r.InventoryItemId, r.QuantityMilliUnits, r.Active)).ToListAsync(ct);

    private async Task<List<StockMovementFacts>> LoadFactsAsync(string inventoryItemId, CancellationToken ct)
    {
        var rows = await _db.StockMovements.AsNoTracking().Where(m => m.InventoryItemId == inventoryItemId).ToListAsync(ct);
        return rows.Select(m => new StockMovementFacts(m.InventoryItemId, Enum.Parse<StockMovementType>(m.Type),
            m.UnitCostPiastres is { } c ? Money.FromPiastres(c) : null, m.CreatedAtUtc, m.QuantityMilliUnits,
            Enum.Parse<StockDirection>(m.Direction))).ToList();
    }

    /// <summary>Only PurchaseReceipt movements are loaded here - the sole movement type
    /// PurchasingRules.LastPurchaseUnitCost (and therefore every costing function) ever looks at.</summary>
    private async Task<CostingContext> LoadCostingContextAsync(CancellationToken ct)
    {
        var items = await _db.InventoryItems.AsNoTracking()
            .Select(i => new InventoryItem(i.Id, i.Name, Enum.Parse<CostSource>(i.CostSource))).ToListAsync(ct);
        var prepEntities = await _db.PrepRecipes.AsNoTracking().ToListAsync(ct);
        var preps = prepEntities.Select(p => new PrepRecipe(p.Id, p.OutputInventoryItemId, p.OutputQuantityMilliUnits, p.Active)).ToList();
        var prepLineEntities = await _db.PrepRecipeLines.AsNoTracking().ToListAsync(ct);
        var prepLines = prepLineEntities.Select(l => new PrepRecipeLine(l.PrepRecipeId, l.InventoryItemId, l.QuantityMilliUnits)).ToList();
        var purchaseMovements = await _db.StockMovements.AsNoTracking()
            .Where(m => m.Type == nameof(StockMovementType.PurchaseReceipt) && m.UnitCostPiastres != null)
            .Select(m => new { m.InventoryItemId, m.UnitCostPiastres, m.CreatedAtUtc }).ToListAsync(ct);
        var movements = purchaseMovements.Select(m =>
            new StockMovementFacts(m.InventoryItemId, StockMovementType.PurchaseReceipt, Money.FromPiastres(m.UnitCostPiastres!.Value), m.CreatedAtUtc)).ToList();

        return new CostingContext(items, preps, prepLines, movements);
    }

    private static RecipeLine ToDomainLine(RecipeItemEntity e) =>
        new(e.InventoryItemId, e.QuantityMilliUnits, Enum.Parse<RecipeLineKind>(e.Kind), e.YieldUnitsPerBaseUnit);

    private static RecipeDto ToRecipeDto(RecipeEntity r) => new(r.Id, r.ProductId, r.Name, r.Active,
        r.Items.Select(i => new RecipeLineDto(i.InventoryItemId, i.QuantityMilliUnits, i.Kind, i.YieldUnitsPerBaseUnit)).ToList());

    private static PrepRecipeDto ToPrepRecipeDto(PrepRecipeEntity p) => new(p.Id, p.Name, p.OutputInventoryItemId, p.OutputQuantityMilliUnits, p.Active,
        p.Lines.Select(l => new PrepRecipeLineDto(l.InventoryItemId, l.QuantityMilliUnits)).ToList());
}
