# tests/Falah.Client.Tests/FinalVerificationTests.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `c8764156b93073f68ee4017c1dc94be0f3434dd714bb45873653237ee5cc8299`; worktree SHA256 `bb47f9de7691ca50570694451b00545eb3aabdd26876011cd5c01d9a13c4bd00`; LF-equivalent `True`.

Actual role: Cross-client regression tests for read failure, date ranges, permissions, pending writes, draft isolation, queue status and tax estimates.

## Function/resource review

- `SendAsync(HttpRequestMessage request, CancellationToken ct)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:13`: Fixture delegates request and ignores cancellation; no actual network transport.

- `Json(string json, HttpStatusCode status = HttpStatusCode.OK)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:16`: Creates JSON response/status from fixture literal; sparse successful DTOs can mask missing required runtime fields.

- `Http(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:19`: Creates fake-handler HttpClient at fixture.invalid with no real login token or host service.

- `Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:22`: Creates all-permission synthetic manager AppSession; no shift set here, tests use explicit context.

- `ReadFixture(string path)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:25`: Path switch supplies menu/day/reports/treasury/inventory/coverage sparse read JSON, defaultemptyarray for any unknown route. This permissive default can hide new unexpected requests.

- `Secondary_read_failure_is_visible_and_retry_recovers(string module, string failedPath)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:51`: Ten module/path cases fail one secondary read then retry success; asserts actual failurehit andLoadError lifecycle viareflection. Empty successfuldata doesnot prove populatedbinding correctness.

- `Missing_blocker_response_never_means_business_day_can_close()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:75`: 503 blockerread despitevalidday mustdisableclose; afterretryemptyblockers allowsclose. Tests fail-closed readpath, notserveratomicclose recheck.

- `Failed_expenses_are_not_presented_as_a_zero_expense_report()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:93`: Reportexpenses503 mustsetLoadError/HasDatafalse rather than showzero. Supports viewHasData hiding; othersectionfailuresseparate.

- `Stepping_a_day_switches_the_screen_onto_that_day()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:107`: Defaulttodayquery thenPreviousDay selects dayperiod and allrequests useyesterday. Pollhelperwaits atmost2seconds; no explicit successfulidleassertion beyond request/statechecks.

- `Choosing_a_week_asks_every_report_for_the_whole_span()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:133`: Selectslast7 andasserts everyrequestfromtoday-6/totoday; real7dayrange notduplicatedsingle day. Doesnotverifydataaggregation.

- `WaitForIdleAsync(ReportsViewModel vm)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:154`: Pollsupto200times10ms whileloading/noData, returnswithoutthrowonexpiry. Callerassertions maycatchsomefailures but thishelperdoesnotguaranteeidle despitecomment.

- `Changing_report_date_during_a_read_reloads_a_consistent_new_date()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:160`: Firstrequestgated, NextDay setsHasDatafalse, release then10old+10newqueries finalsuccess. Verifies snapshotcover/reload schedule; notcancellation orintermediateallfieldsatomicity.

- `Printing_empty_state_is_notified_after_loading_finishes()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:187`: SubscribesPropertyChanged forHasNoPending afteroverview+emptyselectedroute. Asserts notificationandflag; no physicalprinter/routehealth proof.

- `Payroll_does_not_show_a_failed_line_read_as_zero_payable()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:203`: Selectedrunlineread503 leaveserrorandemdash, not0payable. Polls1secondandassertsloadingended. Doesnotreconstructretainedpayment.

- `Payroll_approval_permission_alone_does_not_bypass_the_management_gate()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:216`: payroll_approve alone withreviewedrun yieldsCanAdvancefalse becausemanagementgate also required. Clientruleonly; serverauthorizationseparate.

- `Control_center_view_permission_cannot_change_the_close_checklist()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:228`: control_center readpermission loadsdaybutCanControl/Closefalse; directnullchecklistcommandproducesnowrites. Nullparameter canitselfshortcircuit, so doesnotprove forgedvaliditemblocked independently.

- `Treasury_retries_original_payload_and_blocks_abandonment_after_an_unknown_outcome(string action)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:250`: deposit/withdraw/transfer loss→422→success, editsformbetween, asserts3identicalbodies/globalpending/closeblock/releaseandoriginalamountnote. Sameprocessmemoryonly, UI001restartgapuncovered.

- `Treasury_definite_rejection_allows_correction_and_releases_the_session()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:285`: Initial422knownrejection enablesform/releasesglobalpending andallowsclose, correctlydistinctfrom422afterunknown. Doesnotproveservercommitabsenceforarbitraryerrors.

- `Call_center_recovers_each_operators_own_draft_after_switching_back()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:300`: Twoin-memoryoperatorIDs haveisolateddraftVMs, returnsfirstsameobject+customer/cart. Notprocessrestartoractualtokenaccountswitch.

- `Electronic_payment_decision_cannot_switch_or_be_abandoned_after_response_loss(bool confirm)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:320`: Retainsconfirm/canceldecision andthreeidenticalrequestsafterloss/later422; oppositecommandnosend andstartnew/dismissblocked. InjectsLastCheckoutResult, thusdoesnotcoverUI005recoveredresultnull deadend.

- `Call_center_reprint_retries_the_original_request_and_prevents_abandonment()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:356`: Lookupreprint loss→403→success retainsbodyandblocksclose/edit/globalstateuntilresolved. DoesnotassertIncludeKitchenRoutesfalse orprocessrestartdiskclear, so UI011/UI012notrefuted.

- `Call_center_keeps_unresolved_request_after_a_later_rejection()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:388`: Checkoutlostthen422 keepscart/exactbody andpreventsrootlock/close evenafterfieldchange. SyntheticrealVMlifecyclecoverssameprocessonly.

- `Call_center_lock_waits_for_an_in_flight_operation_then_returns_to_login()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:427`: ManuallytoggleIsProcessingCheckout provesrootclose/lockblockedthenloginviewandcachedeskafteridle. DoesnotassertAPIbearerlogout/revocation; UIloginpresencealone notsessiontermination.

- `Call_center_no_longer_requests_its_own_tickets_after_checkout()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:450`: SuccessfulcheckoutprintJobsQueuedtrue requiresonecheckout andnoprintPOST, clearcart/noPendingPrint/nonewarning, neworderready. Serveractuallyqueuedjobsassumedfixtureflag, nottestedtransaction.

- `Call_center_says_so_when_the_server_could_not_queue_the_tickets()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:501`: printJobsQueuedfalse stillsuccessorder+warninginstructsreprint. Distinguishessalecommitfromprintenqueuefailure, noactualticketorserverfailuretested.

- `Call_center_estimate_uses_the_existing_tax_rules(string mode, long expected)` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:532`: 14percentExclusive→11401/Inclusive→10001 on10001piastrecart; actualVMdomainestimate, noorderwrite. Menuemptyvalidstateasserted.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Failsspecifiedsecondarypath503oncewhileflagtrueandrecordsactualhit; otherGETsReadFixture defaultarray. Noactualservice.

- `Load()` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:65`: ReflectivelyinvokesactualpublicLoadAsync onchosenVMandawaitscastTask. Missingmethod/nullthrows, nofalsepassifinvocationnotfound.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Onlyclose-blockers503whenfail; dayandothersusesReadFixture. Aftertoggleblockersempty allowsCanClose.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Expenseendpoint503, allotherssuccessfulsparsereportDTOs; isolatesexpensefailure ratherthanmanufacturingzero.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: RecordsreportURIsandreturnsReadFixture perpath; noactualrestaurantreportquery.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertsinitialrequestquerycontainscapturedtodaydate; testsroutingnotreportcontent.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertsafterstepquerycontainscapturedyesterdaydateforallrequests; dateisVMinitialnotindependentclock.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: RecordsallweeklyreportURIswithsparsereadfixtures; noactualaggregation.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: SelectsPeriodOptionkeylast7exactly; knownlistcontainsoneoption, predicatepure.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertsweeklyfromquerytodayminus6inclusive; no dateparsing/escapingassertion.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertstodayinclusiveweeklytoqueryforallrequests; verifiesendpointspanconsistency.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: CapturesURI; firstsignalsgateandwaitsrelease; returnsvalidfixturesafterrelease. Deterministicold/newoverlapwithoutserverwrites.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertseachfirst10queryusesoriginaldate; ensuresfirstloadcapturedrangeheldacrossawaits.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertseachremaining10queryusesnewdate, matchingqueuedreload.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Emptyprintingovervieworjobsarraydependingpath; noactualroutehardware.

- `ParenthesizedLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: SetsseenonlywhenHasNoPendingpropertynotifiedandtrue. ExternalVMsubscriptionlocaltestlifetime, noappvisualtreeleakclaim.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Allpayrollreads503; ignoresrequestandreturnscompletedTask, noactualpayrollrecord.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: EmptyarrayHTTPfixture forpermission-onlypayrollVM; noauthaccountchanges.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: CountsnonGETthenreturnsReadFixture; zeroexpectedbutcommandnullparameterlimitsstrengthofpermissionproof.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: GETemptyarray; commandbodycaptured; firstthrowsloss,next422,thirdreplayDTOsuccess. Noactualcommittedmoneydespitewording.

- `ParenthesizedLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: CountsOperationStateChangednotifications toassertglobalpendingstatebroadcastatleasttwice; noordering/identityassertion.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Comparesallthreecapturedtreasurybodystringsagainstfirst, includingkeys andamount. Doesnotverifyactualserverdeduplication.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Initialtreasury422fixturewitherrorinvalid; ignoredrequestmeansanyroutewouldreject.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Emptyarrayclientfixture forin-memoryoperatorcachedrafttest; noactualtokenorlogin.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Captureselectronicdecisionpath/body, loss→422→confirmedDTO; cancelcasealsogetsconfirmedshape, so testprimarilyidentityrelease notactualcancelledstate.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Assertseverydecisionpath+bodytupleequalsfirst, ensuringoppositedecisionnotdispatchedthroughcapturedlist.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Captureslookupreprintbody, loss→403→emptyjobsreplay. Doesnotinspectkitchenroutefanout.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Comparesreprintbodiesbyte-stringequivalent3attempts; includesoriginalrequestID.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: GETcurrent-shiftfixture, checkoutbodycapturefirstlossandlater422. OthernonGETalso422, noactualcustomerupsert/transaction.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: EmptyobjectHTTPfixturefor manuallybusylocktest; noliveauthrevocationandnoheaderassertions.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Tracksanyprint-jobsPOST, GETshift, checkoutsuccessprintqueuedtrueandcounter; unknownPOSTemptyobject canmasknewauxiliarywrites.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: GETshift, checkoutsuccessqueuedfalse, otherpaths{}; createswarningcasewithoutrealsaleorprint.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/FinalVerificationTests.cs:UNKNOWN`: Menuemptycategories, everyotherreadtaxarraywithrequestedmode. Broadfallbackcouldhideunexpectedendpoint; actualVMestimateexecutediftestRun.

## Tests and limitations

SOURCE_REVIEW_ONLY. All59 exact callable spans individually reconciled including eachlambda/localfunction. No tests executed. Testnames containing final/verification do not certify exhaustive native or server operation.

## Recommendations (not implemented)

Add timeout/assertion for idle helpers, strict fixture route schemas, token-bearing auth, committed-response-loss and new-process recovered request cases without production data.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
