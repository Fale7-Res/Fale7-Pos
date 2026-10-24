# tests/Falah.Server.IntegrationTests/FalahApiFactory.cs

**path**: tests/Falah.Server.IntegrationTests/FalahApiFactory.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: facb84fa0d81937e87988e95e7b960a0515240180fce2403d445f5ee21d8574c

**git_blob_sha256**: 680aaac7a7c9167413141df6a4759467370437070d926bd2c33d51a9fe00258a

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 337]]

**functionality_and_reasoning**: Real fixture constructor CREATE DATABASE on fixed SQLEXPRESS and E drive; Dispose SINGLE_USER ROLLBACK IMMEDIATE then DROP DATABASE. Consequently not run. Session fixture directly seeds real tokens and bypasses actual PIN login. QueryAsync delegate can write. Inherited background config needs control before executing tests.

**significant_methods**: Constructor; CreateDatabase; ConfigureWebHost; TokenFor; AnySessionToken; CreateAuthenticatedClient overloads; SeedUserAsync; SeedProductAsync; SeedVariantAsync; SeedPriceRuleAsync; SeedProductWithRecipeAsync; SeedOpenBusinessDayWithFundedMainAsync; FundMainAccountAsync; QueryAsync; Dispose

**interactions_and_risks**: Real fixture constructor CREATE DATABASE on fixed SQLEXPRESS and E drive; Dispose SINGLE_USER ROLLBACK IMMEDIATE then DROP DATABASE. Consequently not run. Session fixture directly seeds real tokens and bypasses actual PIN login. QueryAsync delegate can write. Inherited background config needs control before executing tests.

**findings**: []

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
