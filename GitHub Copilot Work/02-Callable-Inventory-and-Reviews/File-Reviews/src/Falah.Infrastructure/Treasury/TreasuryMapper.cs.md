# src/Falah.Infrastructure/Treasury/TreasuryMapper.cs

**path**: src/Falah.Infrastructure/Treasury/TreasuryMapper.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 818d64bdb94fa957779a83a090b9921c0902b716c2e69564cac25772ac5e0400

**git_blob_sha256**: 3961e1899724b15b8610dc38406128331e657e73f428c2fee42fa8b520ae9237

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 80]]

**functionality_and_reasoning**: Display no locks; spend uses UPDLOCK/HOLDLOCK on account touching movement predicate in ambient transaction. Protects pooled balance but not invoice outstanding read earlier. Source-or-destination query no supporting account index in model, broad range/table contention possible. Known fixed accounts guard callers.

**significant_methods**: LoadAccountMovementsAsync; LoadAccountMovementsForUpdateAsync; TreasuryMapper.ToDomain; TreasuryAccountValidation.IsKnownAccount

**interactions_and_risks**: Display no locks; spend uses UPDLOCK/HOLDLOCK on account touching movement predicate in ambient transaction. Protects pooled balance but not invoice outstanding read earlier. Source-or-destination query no supporting account index in model, broad range/table contention possible. Known fixed accounts guard callers.

**findings**: ["FIN-005"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
