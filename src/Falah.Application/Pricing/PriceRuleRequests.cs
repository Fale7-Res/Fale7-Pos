namespace Falah.Application.Pricing;

/// <summary>One step of a gift offer: an order of ThresholdPiastres or more gets Quantity of the product (and size) free.</summary>
public sealed record GiftTierDto(long ThresholdPiastres, string ProductId, string? VariantId, int Quantity);

/// <summary>Ported from savePriceRule in lib/enterprise-store.tsx - gated on `offers_manage`
/// (distinct from `menu_manage`), WITH a manager-approval fallback (requireApproval:true), matching
/// the reference app's own authorizeCommand call exactly. A single "replace the whole rule" upsert
/// (RuleId null = create, set = update) - the same idiom SaveRecipeHandler already uses, and
/// idempotent by construction the same way: re-applying the exact same save reproduces the exact
/// same resulting row.
///
/// Owner, 2026-11-25: offers of every shape. Kind is an OfferKind by name ("ItemFixedPrice", "ItemPercent",
/// "CategoryAmount", "CategoryPercent", "OrderGift"); a request that leaves the tail out saves a fixed-price offer
/// for every order type and station, without dates or limits - what it always did. OrderTypes are "takeaway",
/// "delivery", "pickup"; Stations "till", "callcenter"; null or empty means all.</summary>
public sealed record SavePriceRuleRequest(
    string RequestId, string? RuleId, string Name, IReadOnlyList<string> ProductIds, long FixedPricePiastres,
    IReadOnlyList<int>? DaysOfWeek, TimeOnly? StartTime, TimeOnly? EndTime, bool Active,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId,
    string Kind = "ItemFixedPrice", int PercentBasisPoints = 0, long AmountOffPiastres = 0,
    IReadOnlyList<string>? CategoryIds = null, DateOnly? StartDate = null, DateOnly? EndDate = null,
    IReadOnlyList<string>? OrderTypes = null, IReadOnlyList<string>? Stations = null,
    int? MaxPerOrder = null, int? MaxPerDay = null, int? MaxTotal = null,
    IReadOnlyList<GiftTierDto>? GiftTiers = null, bool GiftIncludesDelivery = true, bool GiftAutoAdd = false);

public sealed record PriceRuleDto(
    string Id, string Name, IReadOnlyList<string> ProductIds, long FixedPricePiastres,
    IReadOnlyList<int>? DaysOfWeek, TimeOnly? StartTime, TimeOnly? EndTime, bool Active,
    string Kind = "ItemFixedPrice", int PercentBasisPoints = 0, long AmountOffPiastres = 0,
    IReadOnlyList<string>? CategoryIds = null, DateOnly? StartDate = null, DateOnly? EndDate = null,
    IReadOnlyList<string>? OrderTypes = null, IReadOnlyList<string>? Stations = null,
    int? MaxPerOrder = null, int? MaxPerDay = null, int? MaxTotal = null,
    IReadOnlyList<GiftTierDto>? GiftTiers = null, bool GiftIncludesDelivery = true, bool GiftAutoAdd = false);
