# tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/TreasuryVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 415f68db1d53ef7cbbbea80dc15cca48bf2562025b0b6596a964b0eb71966686

**git_blob_sha256**: 71f87063cf3c4168a382db3b75d23de93776bbd95527f5e88f913bad176a7dbc

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 395]]

**functionality_and_reasoning**: Covers day-open replay/sequential guard, deposits, withdrawals, transfers, safe drops, handover and permission delegation. Distinct withdrawal and transfer keys test pooled liquidity with independent source/destination balances; these do not include opening float or purchase receipt paths FIN011. Day opening concurrency FIN009 absent. Several guard tests have competing reasons (unfunded drawer/self-transfer also insufficient funds); wallet ban named but not exercised. Position pending/custody distinction tested only zero owed case. Same-key concurrent replay checks flags plus balances. Handover query limit50 and amount-only retry predicate not exact operation correlation.

**significant_methods**: All51 actual callable identities individually reasoned in Knowledge shard

**interactions_and_risks**: Covers day-open replay/sequential guard, deposits, withdrawals, transfers, safe drops, handover and permission delegation. Distinct withdrawal and transfer keys test pooled liquidity with independent source/destination balances; these do not include opening float or purchase receipt paths FIN011. Day opening concurrency FIN009 absent. Several guard tests have competing reasons (unfunded drawer/self-transfer also insufficient funds); wallet ban named but not exercised. Position pending/custody distinction tested only zero owed case. Same-key concurrent replay checks flags plus balances. Handover query limit50 and amount-only retry predicate not exact operation correlation.

**findings**: ["FIN-009", "FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 395 lines fully read in two bounded chunks;51 stable callable receipts already saved with current verified source hash; no execution.

**recommendations**: Add controlled cross-handler liquidity race, concurrent distinct business-day opening, funded invalid-account guards, nonzero pending/custody position and exact handover movement identity.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
