# tests/Falah.Server.IntegrationTests/CheckoutVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/CheckoutVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 5b33be2a2aa866d704a0d89fa1e0b3c9bef9a0d3f8ac3bf37a5fe50261571e44

**git_blob_sha256**: 9c7cec64a70c1fac53f18abb1fedc922ee862009ba504eae2c5252f3c89db47a

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 303]]

**functionality_and_reasoning**: Covers cashsale identity/moneycount,sequential/eightway replay,12distinct-order allocatoruniqueness,missing shift,single InstaPay defer,readback noextra rows,conflicting journal gate andcomp/discount authorities/reasontaxonomy. Single electronic onlydoesnotcountermixed FIN001/002/010. No truefaultinjectionafterstagedsave; orphancompletedjournalispreexisting gate. Reprint-namedtestonlyGETsorder. Severalcountassertionscannotdetectexistingrowchanges.

**significant_methods**: All57 exact callable spans individually reasoned

**interactions_and_risks**: Covers cashsale identity/moneycount,sequential/eightway replay,12distinct-order allocatoruniqueness,missing shift,single InstaPay defer,readback noextra rows,conflicting journal gate andcomp/discount authorities/reasontaxonomy. Single electronic onlydoesnotcountermixed FIN001/002/010. No truefaultinjectionafterstagedsave; orphancompletedjournalispreexisting gate. Reprint-namedtestonlyGETsorder. Severalcountassertionscannotdetectexistingrowchanges.

**findings**: ["FIN-001", "FIN-002", "FIN-010"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 303lines read in two bounded chunks;57stablecallable receipts saved; noexecution.

**recommendations**: Add fullmoney/stock/journal snapshots,changedpayload/restart/barrierraces,mixedconfirmation/cancellation andexactdiscountresult. Noapplicationfixes performed.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
