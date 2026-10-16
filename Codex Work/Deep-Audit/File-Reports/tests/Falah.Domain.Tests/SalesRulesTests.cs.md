# tests/Falah.Domain.Tests/SalesRulesTests.cs

**path**: tests/Falah.Domain.Tests/SalesRulesTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 9be0e1bcf37a53487a3157d4ce299c39b0c893feb461f793425bd3983680c52a

**git_blob_sha256**: 36504a299b03be33d99ffd038993819cc19466fcb56c352ed464d332d887f3b2

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 197]]

**functionality_and_reasoning**: Full197 lines read. Tests split giveaways, paid-channelCard, single pending transfer, COD outside/settled flags and combined disjoint exclusions. No mixed payment report fact despite helper supports splits. COD settled test passes NoCashOutside artificially without verifying delivered outcome/actual custody amount, so zero collection failure scenario not tested. MakeOrder constructs TimeNow static and aggregate bypasses EF.

**significant_methods**: Item; MakeOrder; all facts

**interactions_and_risks**: Full197 lines read. Tests split giveaways, paid-channelCard, single pending transfer, COD outside/settled flags and combined disjoint exclusions. No mixed payment report fact despite helper supports splits. COD settled test passes NoCashOutside artificially without verifying delivered outcome/actual custody amount, so zero collection failure scenario not tested. MakeOrder constructs TimeNow static and aggregate bypasses EF.

**findings**: ["FIN-010"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Confirms FIN-010 regression gap and COD set-contract assumption; no tests executed.

**recommendations**: Add pending mixed cash/card+electronic sum invariant and failed/returned zero-collected custody settlement end-to-end report.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
