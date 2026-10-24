from ui_source_receipts import *
facts={
(17,9):('FakeHttpMessageHandler forwardsrequest to supplieddelegate, ignoresCancellationToken. Tests exerciseclientVM/serialization, not realtransporttimeouts/cancellation/servertransactions.','Handler.SendAsync -> send(request)','Real cancellation and bearerheaders not covered.'),
(20,5):('GenericDTO helper deserializes JSONwithWeboptions andnullforgivingassertion; incompletefixtures populate defaultfields withoutvalidatingservercontract. Synthetic identitiesnotrealdata.','JsonSerializer.Deserialize<T>','Missingrequiredproperties/domaininvariants nottested.'),
(22,5):('Builds synthetic JSONHttpResponseMessage withUTF8content andchosenstatus, no server/DBprocess. Testresultack shapedminimally, notproof financialhandlercorrect.','StringContent; HttpResponseMessage','Malformedbody/statuscontentclassification onlyexplicitbranches.'),
(27,5):('Factoryconstructseleven liveVMflowadapters forassign/outcome/settlement/payrollcreate/pay/employee/count/waste/supplier/receive/pay. Grantsall/shiftfixture andalreadyopenmodal, so verifies sameprocess pendingimmutability notrestarttargetrecovery/realpermissions. SettlementawaitsrealVMreadagainstfakehandler.','new VMs; commandExecute; Flow delegates','UI017 notrefutedbecausePayrollSelectedRun/IsPayOpenexplicitlyseeded.'),
(117,5):('Theoryruns11 flows firstsynthetictransportloss then422 thenminimalvalidJSON, asserts unresolved/global/edit/closeblocked, mutatesformdirectly, retries andassertsexact(path,body)triplicate andfinalunblockedclosed. Strong sameprocessDTOretentiontest, no processrestart/journalorservercommitactuallyoccurs.','CreateFlowAsync; Assert.Single/All/Equal/True/False','No proofactualidempotency/DBatomicity/ACL/nativebindings; NOT_EXECUTED.'),
(132,47):('FakehandlerGETreturnsaccountDTOor[]; writesrecordsbody, firstthrowsclaimedpostcommit but no actualcommit, second422, thirdgenericwasReplayedJSON. Fixtureactorall bypassesmanagerapproval paths.','requests.Add; HttpRequestException; Json','No servereffect, financialdatafabricationforfixture only, notruntimeclaim.'),
(157,30):('Assert.Allcompares each recordedpath+bodytuple tofirst, provingexactserializationreplaywithinVM. Doesnotcompareheaders/bearer/cancellation orjournalbytes.','Assert.Equal(requests[0],request)','Sharedbearermismatch UI020 notcovered.'),
(168,5):('Payrolltransitiontest seedsselecteddraft, simulateslostwrite+offlineproof, assertsglobalpendingandSelectRunrefused, retry422pluscalculatedread clearsintent andbodiesequal. Covers liveoutcomereconciliationonly, not UI017 restartselectednull.','AdvanceRunCommand; SelectRunCommand; Assert.Equal/False/Null','Restart/missingtarget/readpermission andnativeactionvisibility absent.'),
(173,47):('HandlerrecordsPOSTbodies, firsttransportloss thenalreadytransitioned422; single-runreadfirst503 thenknowncalculated. OtherGET[] preventsrealdata. CorrectlychallengescallbackreconciliationnotDBtransaction.','writes.Add; reads counter; Json','CanacceptdefaultmissingDTOfields; servermonotonicstatuscontractseparate.')
}
flows=[
('assign',[(37,28),(37,78),(38,21),(38,64),(38,87)],'ConfirmAssignCommand','AssignDriver=null','CloseAssignCommand','IsAssignOpen','CanEditAction'),
('outcome',[(43,28),(43,79),(44,21),(44,65),(44,89)],'ConfirmOutcomeCommand','OutcomeCollectedText=invalid','CloseOutcomeCommand','IsOutcomeOpen','CanEditAction'),
('settlement',[(51,24),(51,73),(52,17),(52,67),(52,97)],'SettleDriverCommand','SettleActualCashText=invalid','CloseDriverAccountCommand','IsDriverAccountOpen','CanEditAction'),
('payroll-create',[(60,28),(60,74),(61,21),(61,62),(61,87)],'CreateRunCommand','NewPeriod=invalid','SelectRunCommand(null)','!CanEditAction syntheticopenproxy','CanEditAction'),
('payroll-pay',[(65,24),(65,67),(66,17),(66,57),(66,77)],'PayRunCommand','PaymentAccount=secondoption','ClosePayCommand','IsPayOpen','CanEditAction'),
('employee',[(79,24),(80,17),(81,17),(81,61),(81,85)],'SubmitCashOutCommand','CashOutAmountText=invalid and session.ShiftId=null','CloseCashOutCommand','IsCashOutOpen','CanEditAction'),
('count',[(89,28),(89,76),(90,21),(90,63),(90,85)],'SubmitCountCommand','CountActualText=invalid','CloseCountCommand','IsCountOpen','CanEditAction'),
('waste',[(93,24),(93,72),(94,17),(94,59),(94,81)],'SubmitWasteCommand','WasteQuantityText=invalid','CloseWasteCommand','IsWasteOpen','CanEditAction'),
('supplier',[(100,24),(100,83),(101,17),(101,73),(101,106)],'SubmitSupplierCommand','SupplierName=empty','CloseAddSupplierCommand','IsSupplierOpen','CanEditAction'),
('receive',[(109,24),(109,82),(110,17),(110,69),(110,101)],'SubmitReceiveCommand','capturedline.QuantityText=invalid','CloseReceiveCommand','IsReceiveOpen','CanEditAction'),
('purchase-pay',[(113,20),(113,74),(114,13),(114,61),(114,89)],'SubmitPayCommand','PayAmountText=invalid','ClosePayCommand','IsPayOpen','CanEditAction')]
for kind,keys,submit,corrupt,close,opened,editable in flows:
 reasons=[
  ('Exact '+kind+' submitadapter invokes '+submit+' ExecuteAsync on alreadyconstructedVM/modal. Captures sameVMbetweenretries, so testcannotcertify restartrecovery.',submit,'NeednewVM/journalrestartfixture separately.'),
  ('Exact '+kind+' corruptadapter directlysets '+corrupt+' afterunknown. ThisbypassesdisabledUIandtestsimmutablepayloadreplayagainstchangedform, notnativeinputlocking.',corrupt,'DoesnotchallengeactualbindingIsEnabled, no financialwritefromsetter.'),
  ('Exact '+kind+' closeadapter invokes '+close+' whilepending; outertheoryasserts remainsopen/proxy. Normalcloseguard testedwithoutOStermination.',close,'Forcekill/powerloss outsidecommandnotcovered.'),
  ('Exact '+kind+' opennessadapter reads '+opened+' forpostcondition; payrollcreateusesinverseeditproxyinsteadactualmodal, so no nativepresentationproof.',opened,'Booleanstateisnotactualrenderingorcommandreachabilityafterrestart.'),
  ('Exact '+kind+' editadapter reads '+editable+'; theoryassertsfalsependingthen truesuccess. Doesnotproveserverpermissionorcompletedfinancialoperation.',editable,'GlobalpendingandVMpropertyonly, no nativecommandsorrealaccount.')]
 for key,value in zip(keys,reasons):facts[key]=value
persist_exact(T+'ManagementWriteSafetyTests.cs',facts,'Read complete205-line fixture and bothassertionmethods;64 exact handler/helper/test/flowdelegate spans reconciled. Source tests NOT_EXECUTED, sameprocessretention evidence explicitly separated fromdurability/server/native proofs.')
