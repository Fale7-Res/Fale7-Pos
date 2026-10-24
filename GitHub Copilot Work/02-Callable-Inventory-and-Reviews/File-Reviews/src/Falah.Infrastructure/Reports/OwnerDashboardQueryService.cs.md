# src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs

**path**: src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 67c66a18a228c9e34b3d42aad4c609f75c5ae0bde36d87e8fabc366d8119bd29

**git_blob_sha256**: 7625c154354862c524bae91d48da98cbcf1d90ba1c69aba1f3bdc87e551572a1

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 420]]

**functionality_and_reasoning**: Full420 lines read2chunks. LoadStock sums all quantities unsigned =>Out increases balance, deterministic FIN-013; low conditionMinimum>0 also differs inventory <=minimum. OpenExceptionCount queries persisted stateOpen, but state writer onlyack/resolved and defaultopen derived unsaved =>often0 despite real issues. Open day checks onlyOpen notReopened. Dashboard period queries sequential no consistent snapshot. LastSuccessfulBackup may invalid/pruned unlike dailyverified present. Customer collection mixed FIN-010. Current balances and obligations explicitly current separate period good but history report not frozen. Missing packaging cost completeness ignored by BuildProfitability. Health loads all jobs+attempts perroute lifetime.

**significant_methods**: Constructor; GetAsync; LoadTrendAsync/localChange; LoadDailySeriesAsync; LoadHourlySeriesAsync; CurrentTreasuryAsync/localBalance; LoadLeakageAsync; LoadWagesPostedAsync; BuildProfitability; LoadSettledFromDriversAsync; LoadAttentionAsync; LoadStockAsync; LoadObligationsAsync; LoadPeopleAsync; LoadHealthAsync

**interactions_and_risks**: Full420 lines read2chunks. LoadStock sums all quantities unsigned =>Out increases balance, deterministic FIN-013; low conditionMinimum>0 also differs inventory <=minimum. OpenExceptionCount queries persisted stateOpen, but state writer onlyack/resolved and defaultopen derived unsaved =>often0 despite real issues. Open day checks onlyOpen notReopened. Dashboard period queries sequential no consistent snapshot. LastSuccessfulBackup may invalid/pruned unlike dailyverified present. Customer collection mixed FIN-010. Current balances and obligations explicitly current separate period good but history report not frozen. Missing packaging cost completeness ignored by BuildProfitability. Health loads all jobs+attempts perroute lifetime.

**findings**: ["FIN-013", "FIN-010"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared inventory signed balance and all relevant source writers, no live dashboard query.

**recommendations**: Fix signed movement aggregation and derive attention using shared exception projection, active day includesReopened; bounded grouped queries and consistency indicators. Add stock In/Out and unresolved issue tests.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
