# tests/Falah.Client.Tests/CashierCorrectnessTests.cs

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. Review: **REVIEWED_SOURCE**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.

Git blob SHA256 `baf4b720fcdbf84824b6a2b4b3952be1b25fd4452b5661ef780dd7ca4415d59d`; worktree SHA256 `baf4b720fcdbf84824b6a2b4b3952be1b25fd4452b5661ef780dd7ca4415d59d`; LF-equivalent `True`.

Actual role: Cashier correctness tests using actual VM/parser with synthetic HTTP.

## Function/resource review

- `Money_entry_preserves_piastre_units(string input, long expected)` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:15`: Theory checks Arabic/Persian digits, decimal separators and whitespace against integer piastres. It exercises actual CashInput.TryParse rather than formatting imitation. Missing/limit: No round-trip, all localized separators or maximum valid boundary in this method.

- `Ambiguous_or_unrepresentable_money_is_rejected(string input)` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:21`: Rejects fractional piastre, negative, currency suffix, grouping comma and long overflow; false result asserted without accepting partial parse. Missing/limit: Add zero/business-context distinction and exact max valid value.

- `Exact_tender_and_change_use_egp_to_piastre_conversion()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:25`: Creates synthetic current shift,12550 piastre line, opens checkout and verifies exact125.50 then Arabic200 yields7450change and exact quick key zeros change. Does not submit transaction. Missing/limit: Actual backend repricing/tax could change final tender; real settlement excluded.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Unused HTTP fixture immediately returns404 completed task; protects this tender-only test from requiring a live backend but no assertion of request absence. Missing/limit: Could additionally assert no dispatch.

- `Touch_keypad_replaces_the_initial_exact_amount()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:38`: Three command key presses replace initial exact amount rather than append; asserts200EGP and correct change. No native key routing tested. Missing/limit: Decimal/backspace/native touch input missing.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Completed404 fixture for keypad-only test; no network side effect outside fake handler. Missing/limit: Request count not asserted.

- `Lost_checkout_response_retries_identical_payload_despite_changed_fields()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:48`: Captures two checkout bodies on one VM with thrown HTTP each time. Corrupt amount after unknown then exact equality verifies immutable snapshot. Close/clear refused while pending. Does not restart process or confirm real backend committed. Missing/limit: UI001 process-death identity remains uncovered.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Awaits serialization capture before throwing simulated lost response; no stateful server commit exists despite exception wording. Missing/limit: Need commit-aware backend fixture/integration for duplicate effects.

- `Lost_cash_movement_response_retries_even_if_form_is_now_invalid()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:71`: Cash-out25EGP with reason, unknown first result, invalidated form, second send identical. One VM retained context only, no proof of durable journal. Missing/limit: No success cleanup/restart/global navigation assertion.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Captures fake request body then throws loss, never contacts HTTP network. Called twice by cash movement submissions. Missing/limit: Cannot prove server commit/idempotency.

- `Malformed_success_response_is_unresolved(string body)` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:85`: 200 null and invalid JSON from GetCurrentShift classify failure+OutcomeUnknown. This is a read contract test, not evidence every mutation endpoint handles malformed success. Missing/limit: Add actual mutation malformed-response and truncated-body cases.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Builds200 application/json response with provided malformed body; synchronous completedTask. No backend write. Missing/limit: No streaming timeout/partial content simulation.

- `Draft_is_retained_per_user_across_navigation()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:94`: Changes session.CurrentUser to other and back, asserting cachedVM separation/reuse. Tests in-memory identity keyed draft only, no actual navigation/login or token changes. Missing/limit: Permission changes sameID and cross-process recovery absent.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Completed404 used while retrieving cached drafts; no live API authentication. Missing/limit: Does not verify token/header isolation.

- `Tax_snapshot_uses_existing_domain_rounding_for_exact_tender()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:104`: Loads fake empty menu+14percent exclusive tax, adds10001piastre line, verifies11401rounded estimate and114.01tender. Uses real VM/domain logic, no write. Missing/limit: Multiple inclusive/exclusive rates and changed-tax-after-checkout absent.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Path menu returns empty categories; every other request returns enabled VAT1400 Exclusive array. Broad fallback could hide unintended endpoint calls; still isolated. Missing/limit: Assert exact expected routes to avoid false fixture coverage.

- `Lost_refund_response_preserves_id_amount_and_reason()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:118`: Injects paid completed cash order, refunds25withreason, corrupts live fields after loss and compares2 bodies; cancel refused. Real refund state and process restart untested. Missing/limit: No backend refund cap/idempotency transaction proof.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Captures refund body before simulated HttpRequestException. Does not create restaurant record. Missing/limit: Commit/loss is assumed fixture label only.

- `Unauthorized_discount_never_reaches_checkout_api()` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:134`: Synthetic pos-only account attempts enabled10discount; asserts no handler calls and CheckoutError. Validates client gating, not server authorization. Missing/limit: Server forged request/role mutation independent review required.

- `SimpleLambdaExpression` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:UNKNOWN`: Counts any HTTP call then returns400; expected unreachable and zero count asserted by caller. Missing/limit: No real financial write.

- `Create(Func<HttpRequestMessage, Task<HttpResponseMessage>> send)` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:145`: Builds fake HttpClient at http://test with synthetic all-permission cashier and shift, returns session cached VM. Creates no actual account or authentication. Missing/limit: Shared durable stores/default configuration isolation requires harness review; not run.

- `SendAsync(HttpRequestMessage request, CancellationToken ct)` at `tests/Falah.Client.Tests/CashierCorrectnessTests.cs:153`: Forwards request to injected delegate; cancellation ignored. This creates deterministic isolated responses while excluding real transport/token/server behavior. Missing/limit: Cancellation/native transport not simulated.

## Tests and limitations

SOURCE_REVIEW_ONLY; all22 immutable callable spans explicitly inspected, no tests executed. Fixture assertions do not prove native/server/process durability.

## Recommendations (not implemented)

Add isolated-process recovery and real token/transport contracts without production data.

## Applicability

No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.
