# tests/Falah.Domain.Tests/FinancialRulesTests.cs

**path**: tests/Falah.Domain.Tests/FinancialRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 1703dcb464ce67bbd56b33f05c48d9a82efc9a006adbaa4deae0de17aa06d103

**git_blob_sha256**: 1703dcb464ce67bbd56b33f05c48d9a82efc9a006adbaa4deae0de17aa06d103

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 94]]

**functionality_and_reasoning**: Full94 lines read. Exactsplit/over-onepiastre/duplicate method, pure sequence floor/recovery, postedbalance/excludedreversed, threshold within/beyond, protected/wallet policy. Sequence recovery tests unused pure helper not actual SQL allocator. No distinct-operation SQL balance-lock race, zero/negative split or multiple electronic/nestedMulti guards.

**significant_methods**: All facts; MakeMovement

**interactions_and_risks**: Full94 lines read. Exactsplit/over-onepiastre/duplicate method, pure sequence floor/recovery, postedbalance/excludedreversed, threshold within/beyond, protected/wallet policy. Sequence recovery tests unused pure helper not actual SQL allocator. No distinct-operation SQL balance-lock race, zero/negative split or multiple electronic/nestedMulti guards.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared actual orchestration FIN-001/011; no tests executed here.

**recommendations**: Test actual transaction boundaries across different handlers, not only pure liquidity formula; method whitelist and finite bounds.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
