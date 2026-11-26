namespace Falah.Infrastructure.Persistence.Entities;

public class PriceRuleEntity
{
    public string Id { get; set; } = default!;
    public string Name { get; set; } = default!;
    public long FixedPricePiastres { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public bool Active { get; set; }

    // Owner, 2026-11-25: offers of every shape (Falah.Domain.Pricing.PriceRule). Each default keeps an offer saved before
    // this change a fixed-price offer for every order type, station and day, without limits - exactly what it was.
    /// <summary>OfferKind by name.</summary>
    public string Kind { get; set; } = "ItemFixedPrice";
    public int PercentBasisPoints { get; set; }
    public long AmountOffPiastres { get; set; }
    /// <summary>Cairo dates, inclusive.</summary>
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    /// <summary>Comma-separated order types ("takeaway,delivery,pickup"); null for all.</summary>
    public string? OrderTypes { get; set; }
    /// <summary>Comma-separated stations ("till,callcenter"); null for both.</summary>
    public string? Stations { get; set; }
    public int? MaxPerOrder { get; set; }
    public int? MaxPerDay { get; set; }
    public int? MaxTotal { get; set; }
    public bool GiftIncludesDelivery { get; set; } = true;
    public bool GiftAutoAdd { get; set; }

    public List<PriceRuleProductEntity> Products { get; set; } = new();
    public List<PriceRuleDayEntity> Days { get; set; } = new();
    public List<PriceRuleCategoryEntity> Categories { get; set; } = new();
    public List<PriceRuleGiftTierEntity> GiftTiers { get; set; } = new();
}

/// <summary>Which menu categories a category offer covers.</summary>
public class PriceRuleCategoryEntity
{
    public string PriceRuleId { get; set; } = default!;
    public string CategoryId { get; set; } = default!;
}

/// <summary>One step of a gift offer: an order of ThresholdPiastres or more gets Quantity of the product free.</summary>
public class PriceRuleGiftTierEntity
{
    public string Id { get; set; } = default!;
    public string PriceRuleId { get; set; } = default!;
    public long ThresholdPiastres { get; set; }
    public string ProductId { get; set; } = default!;
    public string? VariantId { get; set; }
    public int Quantity { get; set; }
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
