# src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs

**path**: src/Falah.Infrastructure/Treasury/WithdrawalHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 6b2ca2fb64a59b458c81db17e17a82b568286136576b143644b90e0c712981c3

**git_blob_sha256**: f6e7c05ee8a55b7c2d485365b2adf9ba86081e74a95b56f95d2791499ef3de14

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 116]]

**functionality_and_reasoning**: Full116 lines read. Protected source checked under transaction unlike shift/receive. Owner label but permission treasury_manage+approval any actor, domain no owner role restriction (may be intentional delegation). Movement+audit/journal atomic and stable replay. Account-row scan range lock may broadly serialize; no global maintenance barrier. No actual cash drawer allowed.

**significant_methods**: Constructor; HandleAsync; FindExistingAsync; ToResult

**interactions_and_risks**: Full116 lines read. Protected source checked under transaction unlike shift/receive. Owner label but permission treasury_manage+approval any actor, domain no owner role restriction (may be intentional delegation). Movement+audit/journal atomic and stable replay. Account-row scan range lock may broadly serialize; no global maintenance barrier. No actual cash drawer allowed.

**findings**: ["FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Provides correct counterexample lock ordering for FIN-011; no writes.

**recommendations**: Reuse invariant transaction pattern in all spending handlers; add cross-handler race tests rather than same-key only.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
