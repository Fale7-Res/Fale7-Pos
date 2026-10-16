# tests/Falah.Server.IntegrationTests/DeliveryOrderIntakeTests.cs

**path**: tests/Falah.Server.IntegrationTests/DeliveryOrderIntakeTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 84cc05fc71d3df8ba3720a3eed99f16cec9c749962cbd5f0a5c864b8bd9a4530

**git_blob_sha256**: 3526609e5e5d3db76e621c433e0c7359adcc33ef84707f347535cbbaf470c291

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 139]]

**functionality_and_reasoning**: Server checkout receives full structured delivery snapshot, composes address, enforces five required field categories and creates New delivery. Helper sends service id/name without seeding actual service, so passing fixture would establish only presence validation, not trusted active service lookup. Address test claims not trusted client but sets client DeliveryAddress null, so conflicting precomposed malicious address not challenged. All orders COD cash, no paid/pending mixed dispatch or refund state.

**significant_methods**: InitializeAsync; DisposeAsync; DeliveryOrderAsync; complete/missing/non-delivery/address/snapshot/delivery-row facts

**interactions_and_risks**: Server checkout receives full structured delivery snapshot, composes address, enforces five required field categories and creates New delivery. Helper sends service id/name without seeding actual service, so passing fixture would establish only presence validation, not trusted active service lookup. Address test claims not trusted client but sets client DeliveryAddress null, so conflicting precomposed malicious address not challenged. All orders COD cash, no paid/pending mixed dispatch or refund state.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 139 lines fully read; no API or database writes executed.

**recommendations**: Add conflicting address input, unknown/inactive service and authoritative fee cases according to contract; test pending split custody separately.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
