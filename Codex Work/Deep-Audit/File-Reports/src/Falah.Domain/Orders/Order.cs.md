# src/Falah.Domain/Orders/Order.cs

**path**: src/Falah.Domain/Orders/Order.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 4f93ebcba865636bbd00a6d6edbaab1453f72acb35d220de129d3ba8f47b1c4b

**git_blob_sha256**: 4f93ebcba865636bbd00a6d6edbaab1453f72acb35d220de129d3ba8f47b1c4b

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 233]]

**functionality_and_reasoning**: Immutable historical fields plus mutable payment/refund state. ApplyRefund checks cancelled/multi/positive remaining, but not IsPaid or collection; COD/pending electronic can refund money never collected. CancelElectronicPayment cancels whole mixed order; Confirm only checks pending leg and can follow refund.

**significant_methods**: Create; Reconstitute; ConfirmElectronicPayment; CancelElectronicPayment; ApplyRefund

**interactions_and_risks**: Immutable historical fields plus mutable payment/refund state. ApplyRefund checks cancelled/multi/positive remaining, but not IsPaid or collection; COD/pending electronic can refund money never collected. CancelElectronicPayment cancels whole mixed order; Confirm only checks pending leg and can follow refund.

**findings**: ["FIN-002", "FIN-003"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
