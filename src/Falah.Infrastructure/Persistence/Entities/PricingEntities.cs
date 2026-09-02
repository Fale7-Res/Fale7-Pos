namespace Falah.Infrastructure.Persistence.Entities;

public class PriceRuleEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long FixedPricePiastres { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool Active { get; set; }

    public List<PriceRuleProductEntity> Products { get; set; } = new();
    public List<PriceRuleDayEntity> Days { get; set; } = new();
}

/// <summary>Many-to-many join: which products this offer applies to. A real relation, not a JSON
/// array of product ids.</summary>
public class PriceRuleProductEntity
{
    public string PriceRuleId { get; set; } = default!;
    public string ProductId { get; set; } = default!;
}

/// <summary>Which days of week the offer is gated to. An absent set of rows means "every day" -
/// matching the original's "empty/null daysOfWeek = no day gate" semantics. A real relation, not a
/// CSV/JSON column, for the same reason as PriceRuleProducts.</summary>
public class PriceRuleDayEntity
{
    public string PriceRuleId { get; set; } = default!;
    public int DayOfWeek { get; set; }
}

public class TaxRuleEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public int RateBasisPoints { get; set; }
    public string Mode { get; set; } = default!;
    public bool Enabled { get; set; }
}
