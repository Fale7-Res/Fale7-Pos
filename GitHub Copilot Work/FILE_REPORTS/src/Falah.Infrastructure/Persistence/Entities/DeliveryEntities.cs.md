# src/Falah.Infrastructure/Persistence/Entities/DeliveryEntities.cs

**path**: src/Falah.Infrastructure/Persistence/Entities/DeliveryEntities.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 154147d00ec004748752fcae6a3eeedb23290384e0ba13f73ebb20841d1fe559

**git_blob_sha256**: 154147d00ec004748752fcae6a3eeedb23290384e0ba13f73ebb20841d1fe559

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 142]]

**functionality_and_reasoning**: Full142 lines read. Delivery manual Version; custody no token but host locks delivery for result/reassign and sets for settlement. Settlement expected vs actual audit captures driver handover, not original customer shortfall. No separate trip table despite IDs. CashCustody OrderId lacks FK direct but DeliveryFK ensures owning order transitively if correct pair, no constraint enforces pair equality. DriverNameAtHandover mutable per reassignment history preserves old. Service fee nullable no positivity schema.

**significant_methods**: DeliveryServiceEntity; DriverEntity; DeliveryEntity; DriverCustodyEntity; DeliveryReassignmentEntity; DeliveryEventEntity; DriverSettlementEntity properties

**interactions_and_risks**: Full142 lines read. Delivery manual Version; custody no token but host locks delivery for result/reassign and sets for settlement. Settlement expected vs actual audit captures driver handover, not original customer shortfall. No separate trip table despite IDs. CashCustody OrderId lacks FK direct but DeliveryFK ensures owning order transitively if correct pair, no constraint enforces pair equality. DriverNameAtHandover mutable per reassignment history preserves old. Service fee nullable no positivity schema.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared full delivery handlers and context constraints, no writes.

**recommendations**: Explicit original expected/collected/handed discrepancies and mapping consistency; enforce financial operation payload/date provenance.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
