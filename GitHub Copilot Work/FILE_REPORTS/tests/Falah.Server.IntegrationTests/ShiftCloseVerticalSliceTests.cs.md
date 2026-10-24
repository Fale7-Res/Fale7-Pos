# tests/Falah.Server.IntegrationTests/ShiftCloseVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/ShiftCloseVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 33ca290e359ab0abb0a2eebb6e60bec2644ba25f101cf55d6e8d5b39faa3e9c3

**git_blob_sha256**: 71eaaa6a9506a79df7a53c002585cd3b1c3c1c61e9291dddd00734f6a8eb7aff

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 281]]

**functionality_and_reasoning**: Tests expected cash,balanced/shortage/overage directions and actual handover,approval rejection leaves shift open,approved close,replay/samekey concurrency,oneactive shift and permissions/current endpoint. Concurrent distinct opens use zero floats, so do not challenge FIN011 protected Main check; no sale-versus-close race or crossshift refund FIN004. Some setup responses unchecked; several counts narrower than wholeoperation. Preview onlyone shift cannot prove historical isolation by testtitle.

**significant_methods**: All52 exact callable identities individually reasoned

**interactions_and_risks**: Tests expected cash,balanced/shortage/overage directions and actual handover,approval rejection leaves shift open,approved close,replay/samekey concurrency,oneactive shift and permissions/current endpoint. Concurrent distinct opens use zero floats, so do not challenge FIN011 protected Main check; no sale-versus-close race or crossshift refund FIN004. Some setup responses unchecked; several counts narrower than wholeoperation. Preview onlyone shift cannot prove historical isolation by testtitle.

**findings**: ["FIN-004", "FIN-011"]

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 281lines fully read;52stable callable receipts saved; noexecution.

**recommendations**: Add controlled sale/refund/close races,positivefloat competing spend,exactthreshold,fullsnapshot and historicalrefund currentdrawer cases.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
