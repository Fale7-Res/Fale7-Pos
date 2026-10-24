# tests/Falah.Server.IntegrationTests/PrintingVerticalSliceTests.cs

**path**: tests/Falah.Server.IntegrationTests/PrintingVerticalSliceTests.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: 1fc09dadf93db845d38d53eec0b08ff8abcb7e40bc98dc94957a66b6f8a3782c

**git_blob_sha256**: 1fc09dadf93db845d38d53eec0b08ff8abcb7e40bc98dc94957a66b6f8a3782c

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 801]]

**functionality_and_reasoning**: All 801 lines read in four bounded segments,96 exact callable receipts saved. Explicit fixtures disable auto receipt/preparation so manual endpoint counts remain isolated; later facts reenable flags. Substantive tests prove old per-item station snapshots despite swaps, route partition, samekey job/report replay and failures isolated to another pending station, accepted-report state/history. All output is API DTO/queue metadata, not actual transport or physical success. Stock/treasury invariance compares counts only, no rowcontent/account/journal equality; report statuses often ignored so failed branch may not be reached. Pickup future timestamp persisted toseconds but printedcaption only; missing both name/phone isolates phone, not name. Pending-route fixture has no failed/succeeded jobs so doesnot prove statusfilter. Total-present receipt checks label/ordercode not amount. Menu URL protocol validation,trim,patch preservation,clear all use newlycreatedreceipts, no historicalURL immutability proof. Explicitcreate helper ContinueWith+Result lacks status check and direct diagnostics.

**significant_methods**: Fixture/settings/product/checkout/job helpers; pickup validation/snapshot; route selection and historical station freeze; job replay/reprint/report; movement invariance; overview/pending queries; automatic printing; menu QR settings

**interactions_and_risks**: All 801 lines read in four bounded segments,96 exact callable receipts saved. Explicit fixtures disable auto receipt/preparation so manual endpoint counts remain isolated; later facts reenable flags. Substantive tests prove old per-item station snapshots despite swaps, route partition, samekey job/report replay and failures isolated to another pending station, accepted-report state/history. All output is API DTO/queue metadata, not actual transport or physical success. Stock/treasury invariance compares counts only, no rowcontent/account/journal equality; report statuses often ignored so failed branch may not be reached. Pickup future timestamp persisted toseconds but printedcaption only; missing both name/phone isolates phone, not name. Pending-route fixture has no failed/succeeded jobs so doesnot prove statusfilter. Total-present receipt checks label/ordercode not amount. Menu URL protocol validation,trim,patch preservation,clear all use newlycreatedreceipts, no historicalURL immutability proof. Explicitcreate helper ContinueWith+Result lacks status check and direct diagnostics.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: 96 individual exactsymbol records; no tests/DB/printer execution. Correct routing snapshots stronger than unrelatedservicefee test FIN-TST003.

**recommendations**: Add exact movementcontents/balances/journal unchanged; require report success and failure persisted; mixedstatus queue fixtures; distinct-name/phone errors; exact rendered pickup time andmoney; fullauto flagmatrix; URL changes onoldjobs; safe renderer/QRdecode separate fromprinter.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
