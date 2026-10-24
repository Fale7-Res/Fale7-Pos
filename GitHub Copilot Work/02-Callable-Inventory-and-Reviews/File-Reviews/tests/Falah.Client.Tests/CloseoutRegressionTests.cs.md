# tests/Falah.Client.Tests/CloseoutRegressionTests.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `d2872ab6f2c2db02198fd6ee882d46341c44fa6267a73689ca12a15e3ee8195c`; worktree SHA256 `d2872ab6f2c2db02198fd6ee882d46341c44fa6267a73689ca12a15e3ee8195c`; LF-equivalent `True`.

Actual role: Client pickup identity, safe-drop immutable retry, dated-report reload, reprint route and opening-float regressions.

## Function/resource review

- `SendAsync(HttpRequestMessage request, CancellationToken ct)` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:16`: Forwards fixture HTTP request to delegate, ignoring cancellation. Does not allow accidental actual network because HttpClient uses this handler.

- `Json(string text, HttpStatusCode status = HttpStatusCode.OK)` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:18`: Wraps supplied literal JSON/status UTF8application/json; no schema completeness guarantee. Fixture bodies intentionally sparse.

- `Session(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:20`: Creates synthetic all-permission manager+shift at fixture.invalid without real token. Returns AppSession for real VM exercise; no live login.

- `Pickup_checkout_sends_the_identity_collected_by_the_form()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:25`: Pickup form fields travel checkout body, no delivery area/fee.422 stops fixture before sale. Verifies identity contract but no real order persisted.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Deserializes checkout request into typed DTO then returns422. Synthetic contact details, no real customer data.

- `Uncertain_safe_drop_keeps_the_exact_request_and_blocks_editing()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:47`: ControlCenter safedrop100 then unknown, close refused; edit200 then retry same serialized body. Live VM snapshot behavior only.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Captures safedrop body then throwsHttpRequestException. Label loss does not implement committed server state.

- `Daily_statement_reloads_if_the_date_changes_while_loading()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:69`: Task gates first query, moves date, releases503, asserts queuedsecondquery includesselecteddate and loadingfalse. Ensures reload scheduling afterfailure.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Captures query, first call signals/awaits gate, all calls return503 fixtureerror. Deterministic ordering without real network.

- `Daily_statement_never_shows_a_response_for_a_date_no_longer_selected()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:90`: Two staged422 errors, switchesdate before firstreturns, asserts staleerror neverpublished and selectederror final. Strong publication test for errorpath, not full success snapshot.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Firstcall waitsfirstrelease returnsstaleday422; second signals/waits andreturnsselectedday422. Error labels differentiated for test.

- `The_shift_and_expense_reprint_buttons_only_ask_for_a_reprint()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:122`: Executes3realVM reprintcommands againstfakeendpoint, asserts documentkind/ID/reason and3distinctintentkeys. No shift/expense write routes called. Does not test ambiguous reprint retry.

- `Record(HttpRequestMessage request)` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:125`: Captures typedReprintDocumentRequest+path andreturns validemptyjobs success. Isolates reprintcommands from actualagent.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Assert each of3capturedcalls targets documentreprintpath; excludes additionalmutationendpoint dispatch inthisfixturesequence.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Projects DocumentKind for ordered expected ShiftOpen/ShiftClose/Expense comparison. Pure DTOselection, no mutation.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Projects DocumentId to compare exact original shift/expense target IDs. Ensures clienttargetmapping, notserverownership.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Asserts everyreprint reason nonwhitespace, no claim reasoncontent meaningful. Pure assertion.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Projects RequestId thenDistinct.Count3 verifies separate userintents getdifferentkeys. Doesnotrequire differentkeyonretry; unknownretry needssamekey.

- `The_open_shift_dialog_offers_the_configured_default_and_sends_what_was_typed()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:155`: Settings150000piastres offers1500.00, availablemain500000permits; usertypes1750, request175000.422preventsfixtureactualshift. Doesnotcover unknown-outcome UI016.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Routesettings/mainbalanceGET provide syntheticvalues; openPOSTdeserializesbody andreturns422, others404. Exactknownroutes, no actualbusinesswrite.

- `The_offered_float_is_capped_by_what_the_main_treasury_holds()` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:187`: Configured1500EGP but main400EGP displays400.00 afterdialogload. Verifies offereddefaultcap only, not reservation or servermoney transfer.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CloseoutRegressionTests.cs:UNKNOWN`: Returns settings150000 and balance40000 completedJSON; unknownroutes404. No requestmutation or realnetwork.

## Tests and limitations

SOURCE_REVIEW_ONLY. All22 callable spans including each lambda/localfunction individually reconciled. No tests executed; fixtures cannot prove physical printing, realcommit or process recovery.

## Recommendations (not implemented)

Add explicit timeout to gated tests and restart/token/backendcommit cases without productiondata.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
