# tests/Falah.Server.IntegrationTests/EmployeeVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/EmployeeVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: b3fee1d79bc1f607001471d543bd0f24985056dfe84b80aa6257009c97d23882

**git_blob_sha256**: 582f1333b04594dae58c3bad6b20d0f0669978376d001dfcfe1ce163ab1e8f10

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 244]]

**functionality_and_reasoning**: Covers employee create/update, ledger arithmetic, advance/withdraw permissions and liquidity, sequential/concurrent samekey replay including delegated approval and depleted drawer. Some names stronger than setup: user-link preservation tests null only; manage-versus-ledger denial actor lacks both; advance source drawer not asserted. ID-count query guarantees existence rather than excludes distinct duplicate movement IDs. Distinct-key payout races, manual entry retries and payroll interaction absent.

**significant_methods**: All28 callable spans individually reasoned in Knowledge shard

**interactions_and_risks**: Covers employee create/update, ledger arithmetic, advance/withdraw permissions and liquidity, sequential/concurrent samekey replay including delegated approval and depleted drawer. Some names stronger than setup: user-link preservation tests null only; manage-versus-ledger denial actor lacks both; advance source drawer not asserted. ID-count query guarantees existence rather than excludes distinct duplicate movement IDs. Distinct-key payout races, manual entry retries and payroll interaction absent.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 244lines read;28exact callable receipts persisted; no execution.

**recommendations**: Add linked-user fixture, manage-only actor, cross-table conservation and distinct-command races.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
