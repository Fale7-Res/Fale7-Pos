# tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/CashierCashMovementVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 7494487952ecd877f4a3d1ec22b62d70227d623364a6cc28e023bf693e7a781a

**git_blob_sha256**: 37c82e519fcf2087a5dae55f94e696c7766c9fd3f22920c6cc92fed8b3b36243

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 153]]

**functionality_and_reasoning**: Tests one cash_in row, correct Main-to-drawer and reverse directions, denied unknown type/permission/closed shift/overlarge cashout, same-key sequential and six concurrent dedup, and expected drawer change. mainBefore55-57 is queried but never asserted against after state. Same-request race cannot establish distinct-operation liquidity serialization FIN-011; no transaction barrier. Uses current shift scenarios only, no historical refunds or generic transfer omission.

**significant_methods**: InitializeAsync; DisposeAsync; Movement; type/direction/permission/open shift/balance/replay/concurrency/reconciliation facts

**interactions_and_risks**: Tests one cash_in row, correct Main-to-drawer and reverse directions, denied unknown type/permission/closed shift/overlarge cashout, same-key sequential and six concurrent dedup, and expected drawer change. mainBefore55-57 is queried but never asserted against after state. Same-request race cannot establish distinct-operation liquidity serialization FIN-011; no transaction barrier. Uses current shift scenarios only, no historical refunds or generic transfer omission.

**findings**: ["FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 153 lines fully read; no tests or money movements executed.

**recommendations**: Add Main/drawer conservation assertion and distinct spend barrier tests, plus close-versus-cashmove ordering.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
