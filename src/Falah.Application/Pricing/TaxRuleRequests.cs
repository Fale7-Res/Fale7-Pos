namespace Falah.Application.Pricing;

/// <summary>Ported from addTaxRule/updateTaxRule in lib/enterprise-store.tsx - both gated on
/// `settings_manage`, no approval fallback. There is no separate activate/deactivate command in the
/// reference app - Enabled is just one field on the same full-replace `updateTaxRule` call, so this
/// system does the same rather than inventing a dedicated toggle endpoint. Code/ActiveFrom/ActiveTo
/// from the reference app's own TaxRule type are deliberately NOT carried here - the existing
/// TaxRuleEntity (and the only thing CheckoutOrderHandler ever reads) has never had them, and no
/// checkout/report logic in this system consumes them.</summary>
public sealed record CreateTaxRuleRequest(string RequestId, string Name, int RateBasisPoints, string Mode, bool Enabled, string ActorUserId);

public sealed record CreateTaxRuleResult(string TaxRuleId, bool WasReplayed);

public sealed record UpdateTaxRuleRequest(string TaxRuleId, string Name, int RateBasisPoints, string Mode, bool Enabled, string ActorUserId);

public sealed record TaxRuleDto(string Id, string Name, int RateBasisPoints, string Mode, bool Enabled);
