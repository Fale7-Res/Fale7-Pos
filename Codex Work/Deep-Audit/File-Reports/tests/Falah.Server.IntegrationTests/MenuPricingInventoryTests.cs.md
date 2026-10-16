# tests/Falah.Server.IntegrationTests/MenuPricingInventoryTests.cs

**path**: tests/Falah.Server.IntegrationTests/MenuPricingInventoryTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: bfce384120a53119d0cfe3574a2c0780ba61e65f7aeca6fbf6978e4c54489dc3

**git_blob_sha256**: 22c34ee767445893b62f4a58b60118b8643f832597c8579d8254d454dab81e91

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 268]]

**functionality_and_reasoning**: Tests server product/variant/rule pricing, inactive fallback and unavailable/productassociation rejection, recipe usage multiplication, samekey replay and distinct-key concurrent stock/order sequence conservation. Persisted snapshots survive live price/name/rule/recipe edits; checks mostly direct SQL and do not establish historical API/ticket/costing behavior. Concurrent two devices uses same HTTPclient/actor/shift with independentrequestIDs. Raw quantity sum deliberate usage-only fixture, not signed stockbalance proof. Sequential replay ignoresstatuses.

**significant_methods**: All55callable identities individually mapped in Knowledge shard

**interactions_and_risks**: Tests server product/variant/rule pricing, inactive fallback and unavailable/productassociation rejection, recipe usage multiplication, samekey replay and distinct-key concurrent stock/order sequence conservation. Persisted snapshots survive live price/name/rule/recipe edits; checks mostly direct SQL and do not establish historical API/ticket/costing behavior. Concurrent two devices uses same HTTPclient/actor/shift with independentrequestIDs. Raw quantity sum deliberate usage-only fixture, not signed stockbalance proof. Sequential replay ignoresstatuses.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 268lines fully read;55stable callable receipts saved atomically; no execution.

**recommendations**: Assert response identity/status, perorder200/300 movementpartition and DirectionOut; inspect actual API/ticket historicalsnapshots and faultrollback conservation.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
