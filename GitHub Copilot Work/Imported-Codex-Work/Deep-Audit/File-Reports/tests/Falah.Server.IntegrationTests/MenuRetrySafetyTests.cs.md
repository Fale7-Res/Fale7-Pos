# tests/Falah.Server.IntegrationTests/MenuRetrySafetyTests.cs

**path**: tests/Falah.Server.IntegrationTests/MenuRetrySafetyTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: f6ce18dec473d9ae6617eee1b63f1faceb96553a160c06781e9eb453008c495f

**git_blob_sha256**: 4ca20db1b7e0c696eff619955e87d727213a034da511560b6f91137c0fa220f5

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 104]]

**functionality_and_reasoning**: Exercises first/later/old-key sequences so stale retry cannot undo newer category, product variants, tax or price rule state. Price rule replay returns original receipt while current stays newer, correctly distinguishing command receipt from current state. No-change status request persisted before later toggle protects replay. Sequential tests lack competing distinct request concurrency, crash commit boundaries or restart. Actor owner/all does not exercise permission narrowing.

**significant_methods**: Key; InitializeAsync; DisposeAsync; Category; old category/product/tax updates; price rule receipt replay; no-change status replay theory

**interactions_and_risks**: Exercises first/later/old-key sequences so stale retry cannot undo newer category, product variants, tax or price rule state. Price rule replay returns original receipt while current stays newer, correctly distinguishing command receipt from current state. No-change status request persisted before later toggle protects replay. Sequential tests lack competing distinct request concurrency, crash commit boundaries or restart. Actor owner/all does not exercise permission narrowing.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 104 lines fully read, no real API mutations executed.

**recommendations**: Preserve replay receipt invariants and add restart/cancellation/concurrent distinct update tests only on approved disposable DB.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
