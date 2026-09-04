namespace Falah.Application.BusinessDay;

/// <summary>RequestId is minted once per "open business day" button press and must be reused
/// unchanged by a retry - same idempotency contract as OpenShiftRequest.RequestId. BusinessDate
/// ported from buildOpenedBusinessDay in lib/control-layer-domain.mjs, which lets an owner pick an
/// explicit date (format-validated) rather than always assuming "today" - a real gap found during
/// the legacy-parity audit. Null (the common case) keeps the existing server-authoritative "today
/// in Cairo time" behavior; DateOnly's own type system replaces legacy's regex format check.</summary>
public sealed record OpenBusinessDayRequest(string RequestId, string OpenedByUserId, DateOnly? BusinessDate = null);

public sealed record OpenBusinessDayResult(string BusinessDayId, string BusinessDate, string Status, bool WasReplayed);
