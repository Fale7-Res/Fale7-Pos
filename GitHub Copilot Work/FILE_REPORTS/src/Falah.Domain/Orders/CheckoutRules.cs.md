# src/Falah.Domain/Orders/CheckoutRules.cs

**path**: src/Falah.Domain/Orders/CheckoutRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: a594ce2aa6ab60d232942ef4af790c0dd7c2a90b49db772e277eaf79d5af84d4

**git_blob_sha256**: 4bd2468030f58b4381166d58c8c6d0efa441a85264c37a5b179a7f794664e07e

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 142]]

**functionality_and_reasoning**: Computes tax/comp/change/COD and pickup bounds. Mixed electronic split explicitly retains immediate legs. SelectElectronicLeg stores first electronic leg only, while split validator permits both InstaPay and Wallet. Negative fee/discount not checked here; comp capped by outer handler. Single electronic is pending even when zero payable.

**significant_methods**: ComputeSubtotal; ComputePayableTotal; IsCashOnDelivery; SelectElectronicLeg; DefersAllMoney; ComputeIsPaid; BuildPlan; AssertRequestedPickupTime

**interactions_and_risks**: Computes tax/comp/change/COD and pickup bounds. Mixed electronic split explicitly retains immediate legs. SelectElectronicLeg stores first electronic leg only, while split validator permits both InstaPay and Wallet. Negative fee/discount not checked here; comp capped by outer handler. Single electronic is pending even when zero payable.

**findings**: ["FIN-001", "FIN-002"]

**evidence**: Complete bounded source read, cross-path static reasoning; tests not executed.

**recommendations**: Use linked finding designs; add regression cases for stated gaps only after approval. Preserve historical accounting semantics and explicit transaction boundaries.

**uncertainties**: No runtime reproduction; live schema not assumed from model. No claim of Roslyn semantic/declaration coverage in this manual report.
