# tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/ReportingVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: aadfd8e50005f491d00bb119f296951c4ac3f059d371297242b033498d0cf651

**git_blob_sha256**: 49e54fce7eef21d7e7f3b2102bd6b0f2c032041936c651c2897ddf23bfff4a38

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 551]]

**functionality_and_reasoning**: Tests sales/expenses/refunds/day/headline/tax/cost/history/custody and empty-shift query regression. Class documentation stale about absentpurchasing/payroll. No mixedpendingpayment orcancelledorder fixturehere,contraryto othercomments claiming exhaustiveSalesRulescoverage. Offerbucket namedbutnotseeded/asserted; frozentaxsnapshot namedbutnoruleedit. Currentrecipecostexplicitintent. Negativeunapprovedexpense significance test canpassifuncheckedexpensePOSTfails. COD settlesexactexpectedonly. Hourclockrace possible.

**significant_methods**: All48 exact callable identities individually reasoned

**interactions_and_risks**: Tests sales/expenses/refunds/day/headline/tax/cost/history/custody and empty-shift query regression. Class documentation stale about absentpurchasing/payroll. No mixedpendingpayment orcancelledorder fixturehere,contraryto othercomments claiming exhaustiveSalesRulescoverage. Offerbucket namedbutnotseeded/asserted; frozentaxsnapshot namedbutnoruleedit. Currentrecipecostexplicitintent. Negativeunapprovedexpense significance test canpassifuncheckedexpensePOSTfails. COD settlesexactexpectedonly. Hourclockrace possible.

**findings**: ["FIN-004", "FIN-010"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 551lines read in three bounded chunks;48stable callable receipts saved; noexecution.

**recommendations**: Add deterministicdates,actualsnapshotedits,typedidentitycorrelation,mixed/crossshiftfinancial paths,partial/failedcustody andcompletepermissionresponseassertions.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
