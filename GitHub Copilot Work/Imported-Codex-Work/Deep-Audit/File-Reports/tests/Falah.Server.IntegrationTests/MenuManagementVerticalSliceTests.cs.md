# tests/Falah.Server.IntegrationTests/MenuManagementVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/MenuManagementVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 53f5c23c7f83daca94e2aa452fe130a6770de729a17724bddb1ddc88484bf096

**git_blob_sha256**: 4aa06d65f469d7efb99341f408561e7747edbba975788d61782b9fa100d8992d

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 405]]

**functionality_and_reasoning**: All 405 source lines read. Category creation/update/delete/replay and product variant identity retention are asserted with API readbacks. Tax checkout-named cases only assert rule presence/Enabled, never actual checkout totals or history. Offer scheduled days and time range are posted without proving clock-dependent applicability. Category concurrency checks primary ID presence rather than absence of logical duplicate IDs. Availability no-op proves success but not absence of extra audit/version writes. Ordering deliberately creates categories 3,1,2 and variants 9000,5000,7000 and verifies management/POS ascending sequences; ties untested. Variant update retains A ID, deletes B, adds C; sold variant FK history not exercised.

**significant_methods**: Category, product, variant, tax and offer management facts; replay helpers and ordering projections

**interactions_and_risks**: All 405 source lines read. Category creation/update/delete/replay and product variant identity retention are asserted with API readbacks. Tax checkout-named cases only assert rule presence/Enabled, never actual checkout totals or history. Offer scheduled days and time range are posted without proving clock-dependent applicability. Category concurrency checks primary ID presence rather than absence of logical duplicate IDs. Availability no-op proves success but not absence of extra audit/version writes. Ordering deliberately creates categories 3,1,2 and variants 9000,5000,7000 and verifies management/POS ascending sequences; ties untested. Variant update retains A ID, deletes B, adds C; sold variant FK history not exercised.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 66 exact manually reasoned callable receipts saved; static source only, no tests executed. Audit assertions mostly action/ID predicates rather than complete snapshots.

**recommendations**: Add actual priced checkout and tax history assertions, offer boundary clock tests, sold variant preservation, rollback, case/numeric enums, stale updates and exact audit counts.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
