# src/Falah.Domain/Delivery/DeliveryRules.cs

**path**: src/Falah.Domain/Delivery/DeliveryRules.cs

**baseline**: c35816ecae49633674775dd32061c6ee92f94aaf

**sha256**: cf4ab3ba7e243a1ef76be56d3e5c4d0f5efe0d9c6c17d49e6a08ad4d6716eefa

**git_blob_sha256**: dba6f7c551676a5899ed1b211a0d8b1d9b8605a67c709a4ef47991de3ff0cfc1

**baseline_comparison**: EQUIVALENT_NORMALIZED_LINE_ENDINGS

**status**: REVIEWED_STATIC_COMPLETE_FILE

**reviewed_line_spans**: [[1, 188]]

**functionality_and_reasoning**: Full188 lines read. Settlement uses actual Collected custody, not Expected order obligation, so short customer collection must be visible separately. Driver responsible cash always even already paid; noncash unsettled also creates cash responsibility for electronic claim, risking double tender if later confirmed. Build scope distinguishes active/resolved includes prepaid resolved no custody. Failed can settle via handler bypass domain Failed->Settled transition. Field validation only nonblank not phone/service identity.

**significant_methods**: DeliveryCashResponsibility; CreateCashCustody; ReassignCustody; CalculateTripSettlement; BuildDriverSettlementScope; AssertCollectionAmount; UnsettledCustodyForShift; FinalOutcome; FirstMissingDeliveryField; FirstMissingPickupField; ComposeDeliveryAddress; EmployeeDriverEligible; AssertDriverAssignable; ResolvePaymentState

**interactions_and_risks**: Full188 lines read. Settlement uses actual Collected custody, not Expected order obligation, so short customer collection must be visible separately. Driver responsible cash always even already paid; noncash unsettled also creates cash responsibility for electronic claim, risking double tender if later confirmed. Build scope distinguishes active/resolved includes prepaid resolved no custody. Failed can settle via handler bypass domain Failed->Settled transition. Field validation only nonblank not phone/service identity.

**findings**: []

**evidence**: Full bounded source read and cross-path static reasoning; no runtime tests/DB writes.

**tests_and_evidence**: Compared assignment/result/settlement flow completely; no runtime records. Partial collection lower than order expected with reason is accepted intentionally, discrepancy reporting must carry both stages.

**recommendations**: Make collection discrepancy and driver handover discrepancy separate explicit fields; guard pending electronic dispatch and tender consistency; preserve prepaid resolved scope.

**uncertainties**: Static review only; no runtime reproduction or declaration-level semantic coverage claim. Reported leads not independently confirmed unless linked canonical finding says so.
