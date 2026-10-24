# src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs

**path**: src/Falah.Infrastructure/Orders/CheckoutOrderHandler.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: ff9f45f38b96377ef8f065884820752aa8483f9038b2141ed390222eac83413c

**git_blob_sha256**: b6950a26c147f81f5f78e5d6d6460d1d08a8bee3d8bef97571473b17fa4bdf27

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 305]]

**functionality_and_reasoning**: Atomic checkout composes authoritative pricing, approvals, stock, order and treasury. It opens unsuffixed sale journal for immediate mixed legs and completes it; confirm later needs same key. Open shift read before transaction can race close. Any DbUpdateException rewritten as conflict, obscuring original cause.

**significant_methods**: HandleAsync; FindExistingAsync; ToActor; ToEntity; ToResult

**interactions_and_risks**: Atomic checkout composes authoritative pricing, approvals, stock, order and treasury. It opens unsuffixed sale journal for immediate mixed legs and completes it; confirm later needs same key. Open shift read before transaction can race close. Any DbUpdateException rewritten as conflict, obscuring original cause.

**findings**: ["FIN-001", "FIN-002"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
