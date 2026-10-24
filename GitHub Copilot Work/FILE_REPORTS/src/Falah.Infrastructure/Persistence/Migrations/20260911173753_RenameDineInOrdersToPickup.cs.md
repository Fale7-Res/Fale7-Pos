# src/Falah.Infrastructure/Persistence/Migrations/20260911173753_RenameDineInOrdersToPickup.cs

**path**: src/Falah.Infrastructure/Persistence/Migrations/20260911173753_RenameDineInOrdersToPickup.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 1c505300965819d06f2ecc7ea359b3ab096baf9c233e488cf08d866945afa9c6

**git_blob_sha256**: e6c5f106ac12eaba3482cf830244ad10d33975bc23eccc52bb7a15000a41df14

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 27]]

**functionality_and_reasoning**: Up rewrites historical dine-in semantic type to pickup; old TableNumber remains. Down rewrites all pickups including originally pickup, hence not losslessly reversible. Only inspected; no migration run.

**significant_methods**: Up; Down

**interactions_and_risks**: Up rewrites historical dine-in semantic type to pickup; old TableNumber remains. Down rewrites all pickups including originally pickup, hence not losslessly reversible. Only inspected; no migration run.

**findings**: []

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
