namespace Falah.Application.Printing;

/// <summary>A narrowly-scoped write: assigns/clears a product's preparation station only. Full menu
/// management (name/price/category CRUD) is separate, deferred future work (see
/// IMPLEMENTATION_MATRIX.md row 2) - this exists solely because print-job routing needs SOME way to
/// configure "which station cooks this" beyond whatever DemoDataSeeder hardcodes at first boot.
/// Station is one of "Charcoal"/"Grill", or null to clear it (no kitchen ticket for this product).</summary>
public sealed record SetProductStationRequest(string? Station, string ActorUserId);
