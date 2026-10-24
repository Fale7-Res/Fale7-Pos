# tests/Falah.Server.IntegrationTests/ManagerApprovalTests.cs

**path**: tests/Falah.Server.IntegrationTests/ManagerApprovalTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 23f69492d7f55e7c17a53f85ed0638a1aeb3f14e7c5086d94f49d63504c63470

**git_blob_sha256**: 23f69492d7f55e7c17a53f85ed0638a1aeb3f14e7c5086d94f49d63504c63470

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 196]]

**functionality_and_reasoning**: Tests approval single-use via two prevalidated DbContexts, delegated comp/refund, wrong-action/nonexistent evidence rejected even with comp permission, persistence across separate host and second-use denial with distinct checkout keys. Lifecycle writes funded SQL day/shift. Direct concurrency exception test does not establish complete HTTP sale rollback. Refund fixture is already collected cash so does not refute FIN-003. Same-key original sale replay after consumed approval, expiry and mismatched approver still absent.

**significant_methods**: All16 actual callable declarations individually mapped in Knowledge shard including local MakeRequest and three lambdas

**interactions_and_risks**: Tests approval single-use via two prevalidated DbContexts, delegated comp/refund, wrong-action/nonexistent evidence rejected even with comp permission, persistence across separate host and second-use denial with distinct checkout keys. Lifecycle writes funded SQL day/shift. Direct concurrency exception test does not establish complete HTTP sale rollback. Refund fixture is already collected cash so does not refute FIN-003. Same-key original sale replay after consumed approval, expiry and mismatched approver still absent.

**findings**: ["FIN-003"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 196 lines fully read;16 stable callable IDs individually reasoned and atomically saved; no tests run.

**recommendations**: Add transaction-conservation assertions and same-request approved sale replay across process restart.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
