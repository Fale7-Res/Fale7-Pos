# tests/Falah.Server.IntegrationTests/UserManagementVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/UserManagementVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 6507c7deedcd751a74d4d9c1c7c8e3823821405faa450fee11a9b9d59e6f1f58

**git_blob_sha256**: b4dd8f5d6fb8f9498388b18576c343dcaa61475774d51980c371eb5f926ebf07

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 555]]

**functionality_and_reasoning**: Covers user/PIN/permission lifecycle, same-key replay, login limiter, owner and administrative survival guards. Reports omit credentials. Static fixture defect: concurrent self-revocation496 does not remove seeded demo manager/all, so both named revocations may legitimately succeed; Assert.Single505 overconstrains invariant. Confirmed source startup Program202-215/Factory37-68/Seeder16,34-37. Last-admin deactivate528 ends self-deactivate, already forbidden separately, so not independent guard proof. Role-based approval authority with allowed permission subset, distinct-key credential races and inactive reactivation collisions absent.

**significant_methods**: All78 exact callable spans including getters, constructor, helper methods and lambdas individually reasoned

**interactions_and_risks**: Covers user/PIN/permission lifecycle, same-key replay, login limiter, owner and administrative survival guards. Reports omit credentials. Static fixture defect: concurrent self-revocation496 does not remove seeded demo manager/all, so both named revocations may legitimately succeed; Assert.Single505 overconstrains invariant. Confirmed source startup Program202-215/Factory37-68/Seeder16,34-37. Last-admin deactivate528 ends self-deactivate, already forbidden separately, so not independent guard proof. Role-based approval authority with allowed permission subset, distinct-key credential races and inactive reactivation collisions absent.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 555 lines read in three bounded chunks;78 exact stable callable records saved and source hash validated; no tests run.

**recommendations**: Isolate exact admin set, remove self-rule confound, assert entire credential redaction, effective permission use/session lifecycle and distinct-operation uniqueness races. No new security finding promoted by this test review.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
