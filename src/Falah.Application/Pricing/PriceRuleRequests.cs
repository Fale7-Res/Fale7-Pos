namespace Falah.Application.Pricing;

/// <summary>Ported from savePriceRule in lib/enterprise-store.tsx - gated on `offers_manage`
/// (distinct from `menu_manage`), WITH a manager-approval fallback (requireApproval:true), matching
/// the reference app's own authorizeCommand call exactly. A single "replace the whole rule" upsert
/// (RuleId null = create, set = update) - the same idiom SaveRecipeHandler already uses, and
/// idempotent by construction the same way: re-applying the exact same save reproduces the exact
/// same resulting row.</summary>
public sealed record SavePriceRuleRequest(
    string RequestId, string? RuleId, string Name, IReadOnlyList<string> ProductIds, long FixedPricePiastres,
    IReadOnlyList<int>? DaysOfWeek, TimeOnly? StartTime, TimeOnly? EndTime, bool Active,
    string ActorUserId, string? ManagerApprovalId, string? ManagerApproverUserId);

public sealed record PriceRuleDto(
    string Id, string Name, IReadOnlyList<string> ProductIds, long FixedPricePiastres,
    IReadOnlyList<int>? DaysOfWeek, TimeOnly? StartTime, TimeOnly? EndTime, bool Active);
