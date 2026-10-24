# tests/Falah.Server.IntegrationTests/PrintAgentTests.cs

**path**: tests/Falah.Server.IntegrationTests/PrintAgentTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 971c4739d41221b832270de9d7e29a23dfbf37d9d0201f649067690db86d0163

**git_blob_sha256**: c520fc4e0cf71b132b600b0e934bc9bf8ca91de9c4a4a567566cb4f13385149e

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 377]]

**functionality_and_reasoning**: Fake server/transport tests loop outcome truth,cancellation uncertainjournal,failclosed corruptstore,routefilter/order,retryidentity,noresendafterreportfailure,DTOmapping/cuts/config. No actual server orprinter. Tempfiles ifexecutednotcleaned. Cancel token alreadycancelled andfakefailurebeforecapture,notrealmidwrite. Sharedstorerecoveryfactdoesnotreloadprocess/disk; cancellationfactdoesnewstore. Queueoldesttitle onlyinsertionorder.Fakeauthalwayssuccess. Cross-hostunclaimedqueuePRT001notcountered.

**significant_methods**: All55 exact callable identities including accessors and nested lambdas individually reasoned

**interactions_and_risks**: Fake server/transport tests loop outcome truth,cancellation uncertainjournal,failclosed corruptstore,routefilter/order,retryidentity,noresendafterreportfailure,DTOmapping/cuts/config. No actual server orprinter. Tempfiles ifexecutednotcleaned. Cancel token alreadycancelled andfakefailurebeforecapture,notrealmidwrite. Sharedstorerecoveryfactdoesnotreloadprocess/disk; cancellationfactdoesnewstore. Queueoldesttitle onlyinsertionorder.Fakeauthalwayssuccess. Cross-hostunclaimedqueuePRT001notcountered.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 377lines read in two bounded chunks;55stable callable receipts saved; no test execution,filesystemfixturewrites orprinting.

**recommendations**: Add partialwrite/barrier/crash/store-reload,realserialization,logs/payloadassertions andcrosshostclaimcases in safe isolated verification.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
