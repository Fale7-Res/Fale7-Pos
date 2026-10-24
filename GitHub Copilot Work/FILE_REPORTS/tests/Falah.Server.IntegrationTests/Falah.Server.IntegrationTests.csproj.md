# tests/Falah.Server.IntegrationTests/Falah.Server.IntegrationTests.csproj

**path**: tests/Falah.Server.IntegrationTests/Falah.Server.IntegrationTests.csproj

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 807da9452aac70907db38ed12cd286c5492f4f9b05c2d2fb2be4cbeee557fb2e

**git_blob_sha256**: 60b5acd3ff7017a780f27f594afc5f49d0002a528c65b552125401710e2215aa

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 38]]

**functionality_and_reasoning**: net8 test project references real Server/Infrastructure/Application/Domain/PrintAgent, xunit2.5.3, MVC Testing8.0.11 and ZXing0.16.11. Explicit runner config copied; these are real SQL integration fixtures, not safe pure tests by project name alone. No custom build target present.

**significant_methods**: Project properties; PackageReference; ProjectReference

**interactions_and_risks**: net8 test project references real Server/Infrastructure/Application/Domain/PrintAgent, xunit2.5.3, MVC Testing8.0.11 and ZXing0.16.11. Explicit runner config copied; these are real SQL integration fixtures, not safe pure tests by project name alone. No custom build target present.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Entire38 lines read; no restore or execution in this audit.

**recommendations**: Run only after isolated SQL target and fixture cleanup effects independently approved; record tool/runtime results separately.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
