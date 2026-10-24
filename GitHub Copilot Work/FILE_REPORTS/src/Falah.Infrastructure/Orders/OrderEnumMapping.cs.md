# src/Falah.Infrastructure/Orders/OrderEnumMapping.cs

**path**: src/Falah.Infrastructure/Orders/OrderEnumMapping.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 9fae8f656f096ef16f0dfb68904957037bd4e611b6ae4404bef215857d5a1b1b

**git_blob_sha256**: 2b8b7326e580341dee9b67975c4edd735541f3f015a9c4c6a161e2425185c6c8

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 65]]

**functionality_and_reasoning**: Full65 lines read. Order/payment parses case-insensitive via lower; legacy dinein aliases map Pickup; enum outputs canonical lowercase. Null input dereference, unknown throws ArgumentException rather than DomainException, root middleware maps errors? Raw request orderType later packaging matching means parsed aliases not consistently propagated. No trim.

**significant_methods**: ParseOrderType; ParsePaymentMethod; ToWireString overloads

**interactions_and_risks**: Full65 lines read. Order/payment parses case-insensitive via lower; legacy dinein aliases map Pickup; enum outputs canonical lowercase. Null input dereference, unknown throws ArgumentException rather than DomainException, root middleware maps errors? Raw request orderType later packaging matching means parsed aliases not consistently propagated. No trim.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared checkout raw packaging and persisted lower status queries.

**recommendations**: Canonicalize once at API boundary and use enum throughout; known invalid user input map validation errors with same Arabic contract.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
