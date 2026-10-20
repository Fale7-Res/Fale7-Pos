# Database and financial integrity — ongoing review

Status: **IN_PROGRESS — completion gate NOT SATISFIED**. Updated UTC: 2026-10-20T03:07:25.813803+00:00.

Continuing audit, not a complete financial certificate or production readiness claim. Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`. One owner-authorized **INTERIM** documentation checkpoint pushed and verified: `bc914a4d71a34de6ef6655887ce73140075c17e0`. Subsequent batches are local, uncommitted; no second push. No application source/database/runtime state changes performed.

## Current verified accounting of evidence

- Financial scope327 paths,327 prior file reports; report presence alone does not certify full function depth.
- Original financial callable author-reasoning receipts **7516/8804**; **1288 missing**. Original specialist6362, coordinator1216; exact stable-ID union removes overlap. Current reviewer-owned machine-readable queue: [financial-current-union-progress.json](../../Sub-Agents/Results/financial-current-union-progress.json); parent global snapshot retained separately.
- Supplementary getters **77/77**; generated contexts **35/95**. Separate denominators, not total CLR methods. These receipts still require independent semantic quality assessment.
- Active exact remaining IDs/signatures/spans: [financial-current-union-progress.json](../../Sub-Agents/Results/financial-current-union-progress.json). The [coordinator coverage](../../Sub-Agents/Results/financial-coordinator-coverage.json) is a historical snapshot; [team state](../../Sub-Agents/Agent-Registry/financial-team-state.json) preserves assignment history. The original8804 denominator includes explicitly paused Printing residue; it has not been reduced.
- Original financial specialist session is now available and has read the complete October18 handoff. Earlier failed original/replacement recovery attempts remain historical evidence. Original specialist owns remaining financial source, report and independent challenges; root owns mirrored comparisons and shared canonical registries. Other specialists paused. No new commit/push authorized.

## Findings and candidates

The canonical registry retains original source records and predecessor history. Candidate entries below are not independently confirmed defects.

| ID | Severity | Result |
|---|---|---|
| FIN-001 | HIGH | Mixed cash/electronic checkout cannot confirm its electronic leg; SAVED_STATIC_CORROBORATION |
| FIN-002 | HIGH | Cancelling pending mixed electronic payment hides collected cash without reversal; SAVED_STATIC_CORROBORATION |
| FIN-003 | HIGH | Refund can debit money for an order that was never collected; SAVED_STATIC_CORROBORATION |
| FIN-004 | HIGH | Historical cash refund omitted from current-shift expected cash; SAVED_STATIC_CORROBORATION |
| FIN-005 | HIGH | Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache; SAVED_STATIC_CORROBORATION |
| FIN-006 | HIGH | Generic payroll transition marks run Paid without paying treasury or employee ledger; SAVED_STATIC_CORROBORATION |
| FIN-007 | MEDIUM | Backup filenames collide within a second and WITH FORMAT can overwrite prior backup; SAVED_STATIC_CORROBORATION |
| FIN-008 | HIGH | Restore pending-journal check does not exclude active or newly accepted financial work; SAVED_STATIC_CORROBORATION |
| FIN-009 | MEDIUM | Concurrent different open-day requests create multiple active business days; SAVED_STATIC_CORROBORATION |
| FIN-010 | MEDIUM | Collected sales report drops immediate money from pending mixed payments; SAVED_STATIC_CORROBORATION |
| FIN-011 | HIGH | Shift opening and purchase receipt check protected account balance before their transactions; SAVED_STATIC_CORROBORATION |
| FIN-012 | HIGH | Attendance correction credits do not enter calculated payroll; SAVED_STATIC_CORROBORATION |
| FIN-013 | MEDIUM | Owner dashboard adds outgoing stock instead of subtracting it; SAVED_STATIC_CORROBORATION |
| FIN-014 | HIGH | Restore success followed by MULTI_USER failure is classified as database not replaced; SAVED_STATIC_CORROBORATION |
| FIN-015 | HIGH | Checkout can commit after its previously open shift has completed handover; SAVED_STATIC_CORROBORATION |
| FIN-016 | HIGH | Unvalidated signed delivery fee can bypass intended discount and comp authorization; SAVED_STATIC_CORROBORATION |
| FIN-017 | MEDIUM | Shift close can attribute approval to an unvalidated optional presenter; SAVED_STATIC_CORROBORATION |
| FIN-018 | MEDIUM | Negative receipt quantity and unit cost can post stock reduction with supplier debt; SAVED_STATIC_CORROBORATION |
| FIN-019 | HIGH | Pending mixed delivery includes already collected payment leg in driver custody; independent static corroboration, no actual collection/SQL/HTTP |
| FIN-020 | MEDIUM | Unit metadata can change after stale no-movement check while first receipt commits; independent static corroboration, no SQL/measured loss |
| FIN-021 | MEDIUM | Defined numeric Inclusive tax mode is accepted then persisted raw and charged as Exclusive; independent static corroboration, no API/DB execution |
| FIN-022 | MEDIUM | Accepted compensation spelling is stored raw and misses canonical daily/monthly earning classification; independent static corroboration, no payroll/API/DB execution |
| FIN-023 | MEDIUM | Same Cairo-day attendance crossing UTC midnight reconstructs check-in on wrong UTC date; independent static corroboration, no host-zone/biometric/SQL test |
| FIN-026 | MEDIUM | Distinct credit commands can persist/display stale BalanceAfter; fact-derived balance and protected payout controls remain correct; independent static corroboration, no SQL/API/UI acceptance test |
| FIN-CAND-CLOSE-SNAPSHOT-REVISION-001 | MEDIUM | Source invariant gap only: no explicit single-authoritative/revision admission; concurrent duplicate commit outcome remains UNVERIFIED and provider-order dependent; qualified source-only evidence, no SQL/HTTP/UI execution |
| FIN-027 | MEDIUM | Frozen close snapshots expose financial report JSON without the live reports_view gate; legitimate session and existing snapshot required; qualified source-only evidence, no SQL/HTTP/UI execution |
| FIN-028 | MEDIUM | Delegated overtime/extra-shift committed retry rejects spent approval before durable receipt; retained client can loop unresolved, no duplicate-money claim |
| FIN-CAND-REFUND-REPLAY-001 | LOW | Source ordering gap only: overlapping full-refund duplicate can fail before locked receipt recheck; concurrent response NOT reproduced, later sequential retry works |
| FIN-025 | MEDIUM | Undefined numeric manual-ledger type is accepted but omitted from named financial categories; independent static corroboration, no SQL/API/UI acceptance test |
| FIN-024 | MEDIUM | Negative custom daily-wage/rate can post negative earning credit; authorized writer and ordinary UI controls retained; independent static corroboration, no SQL/API/UI acceptance test |
| FIN-CAND-CASHCOUNT-001 | MEDIUM | Independently corroborated static finding; no SQL/HTTP reproduction |
| FIN-CAND-PAYROLL-001 | HIGH | Independently corroborated static finding; no SQL/HTTP reproduction |
| FIN-CAND-PREPGRAPH-001 | MEDIUM | Independently corroborated static finding; no SQL/HTTP reproduction |

| FIN-CAND-CLOSEDAY-ADMISSION-001 | HIGH | Independently corroborated static finding; no SQL/HTTP reproduction |
| FIN-CAND-PACKAGING-COVERAGE-001 | MEDIUM | Independently corroborated static finding; no SQL/HTTP reproduction |

| FIN-CAND-REPORT-RANGE-001 | MEDIUM | Independently corroborated static finding; no SQL/HTTP reproduction |
| FIN-CAND-CORRECTION-BASIS-001 | MEDIUM | REQUIREMENTS QUESTION: correction revalues old effects at current rate; source behavior corroborated, actual-posted versus current-increment policy unresolved |

Source locations, hashes, symbols, root causes, preconditions, counterevidence and proposed tests are preserved in [MASTER_FINDINGS.json](../MASTER_FINDINGS.json), [original financial findings](../../Sub-Agents/Results/financial-findings.json), and [parent financial findings](parent-financial-findings.json). No findings are silently discarded.

## Latest completed source investigations

- FIN017: an authorized shift close may record an existing person from an unnecessary, unvalidated approval presentation. Does not itself bypass the required close permission or change amounts. Independent root source challenge saved.
- FIN018: both-negative purchase quantity/cost yields positive invoice debt while a negative In movement reduces stock. Valid purchasing authority/day/supplier/item still required. Independent source challenge saved; no SQL posting performed.
- Payroll candidate: cancellation validates Approved before payout, then can overwrite committed Paid; or payout validates Approved before cancellation, then pays cancelled run. Source-balance lock/idempotency do not protect payroll state transition. Unique live-period index is explicit counterevidence for schedules inserting a replacement run; no duplicate-pay claim made.
- Cash-count candidate: six isolated linked-Domain assertions reproduce normal count, intended negative-quantity clamp, accepted negative/unsupported denomination, multiplication wrap and difference wrap. Handler/DTO/endpoint source forwards values into finalized count/audit. Does not prove shift/day close accepts false balance.
- Prep-graph candidate: opposite new edges validate before transactions against stale graph. Domain cycle guard returns unknown costs and prevents recursion failure; impact is cost coverage, not established direct money loss.
- Structural-cycle warning hypothesis was not promoted after caller and recipe-save counterevidence. Yield ratio is nullable long; NaN/infinity concern explicitly rejected and earlier receipt corrected with history.

## Executed safe checks and limitations

- [Cash-count arithmetic results](../../Local-Verification/cash-denomination-pure-results.json):6 assertions,0 failures, synthetic in-memory only. Result assertions characterize behavior; not6 valid-business-policy passes. Source/binary hashes in [candidate provenance](cash-denomination-boundary-lead.json).
- Prior FIN016 arithmetic7 assertions and SEC001 binding10 cases retained at existing evidence paths. They are not SQL/backend financial acceptance tests.
- Independent exact-source reuse provenance check:34 migration/model files,2456 spans,0 mismatches. Text equivalence and predecessor evidence are not independent re-analysis of every schema block.
- Historical377 Domain tests used preexisting binary without current rebuild guarantee; preserved result must not be reported as complete financial integration verification.
- No audit SQL writes, live financial transactions, account permission changes, backup/restore, native application action or printer jobs executed. No complete concurrency/deadlock/isolation benchmark or live restart recovery test claimed.


## Latest additional source evidence

- DailyStatement71 and current DbContext212 original callable receipts;59typed DbSet getter facets separately. All77financial getter facets now receipted, not full generated-behavior completion.
- OwnerDashboard114 callables, clock/audit/pricing/backup scheduler23, treasury safe-drop/transfer/contention20, six financial Domain files54, employee/payroll queries45, payout/poster20, credit/manual23, attendance/correction/profile27, cashout14. Counts describe new coherent root batches, not independent whole-system acceptance.
- [Owner dashboard scope review](owner-dashboard-financial-scope-review.json): current stock versus selected-period flow intentional, FIN013 preserved, date/cost and packaging completeness candidates qualified.
- [Reverse date source chain](reverse-date-range-source-lead.json) and [8component assertions](../../Local-Verification/report-range-pure-results.json): actual linked Domain calls plus explicitly simulated raw-date cost predicate. Query service/API/DB/DTO not executed.
- [Packaging component assertions](../../Local-Verification/packaging-coverage-pure-results.json):4cases,0failures. Actual CostingRules/ProfitabilityRules plus explicit synthetic accumulation; not full report query reproduction.
- [Employee query scope](employee-query-scope-comparison.json): all-time remaining balance versus range flow; active-only staff versus dashboard all ledger obligations. Adjustment credits omission links FIN012, not new duplicate finding.
- [Employee posting concurrency](employee-posting-concurrency-source-notes.json): standalone payout DOES lock employee anchor/range; shared Poster alone does not. [Credit admission source leads](employee-credit-admission-source-leads.json) retain signed wages, manual numeric enum, disclosed no-idempotency contract and cached BalanceAfter concurrency questions.
- [Attendance boundary leads](attendance-financial-boundary-source-leads.json): compensation representation lead is independently corroborated and canonical FIN022; UTC midnight reconstruction is canonical FIN023; correction using current rates is canonical policy-qualified FIN-CAND-CORRECTION-BASIS-001, with owner valuation policy unresolved. No runtime attendance/payroll verification.
- [Payout policy comparison](../Duplication-and-Architecture/employee-payout-policy-comparison.json) and [safe-drop/transfer comparison](../Duplication-and-Architecture/treasury-safe-drop-transfer-comparison.json): intentional account/permission/shift/advance differences preserved; no refactor implemented.

## Current financial completion gates

| Gate | Status |
|---|---|
|327 financial scope files|Accounted by prior reports; per-function depth quality remains open|
|Original applicable callable reasoning|OPEN: 1288 missing original IDs; 8804 retained including paused Printing residue|
|Supplementary behavior|77/77 getter receipts; 35/95 generated contexts completed, 60 remain; independent depth not certified|
|Financial workflows, transaction/isolation/concurrency/restart|Source chains and risks saved; full closure and safe provider test plans still incomplete|
|Independent challenges|37 canonical financial records retain source evidence/challenges; requirements questions remain policy-qualified, not unconditional defects. No SQL/HTTP reproduction inferred|
|Runtime/hardware|No live SQL/financial records/printing; isolated components only. Provider/concurrency tests NOT_EXECUTED|
|Financial completion certificate|NOT ISSUED; material source-review gaps remain|

## Mirrored-code and paused backlog

Identified mirrored-code queue:218 AST groups accounted (116 manual/contextual comparisons and102 generated-history groups),0 identified groups pending. 16 supplemental semantic groups cover89 source paths and478 referenced contexts; these are bounded comparisons, not extra financial function reviews or an exhaustive renamed/dead-code/reflection guarantee. Phase11 remains qualified by those limits. [Canonical comparison report](../CODE_DUPLICATION_AND_ARCHITECTURE.md).

Entire original15phase/19deliverable backlog and paused agents preserved in [PAUSED_AUDIT_BACKLOG.json](../../Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json). Exact current next action and source queues: [NEXT_ACTION.md](../../Conversation-Recovery/NEXT_ACTION.md). No Printing/UI/general-performance resumption and no further interim Git publication without authorization.

## Latest original-specialist continuation

Six root-origin candidates have now received independent source challenge from the original financial specialist, with qualifications preserved in [payroll](../../Sub-Agents/Handoffs/payroll-terminal-state-independent-challenge.json), [business-day admission](../../Sub-Agents/Handoffs/business-day-admission-independent-challenge.json), [packaging](../../Sub-Agents/Handoffs/packaging-completeness-independent-challenge.json), [cash denomination](../../Sub-Agents/Handoffs/cash-denomination-independent-challenge.json), [reverse date range](../../Sub-Agents/Handoffs/reverse-date-range-independent-challenge.json), and [prep graph](../../Sub-Agents/Handoffs/prep-graph-independent-challenge.json). The coordinator has merged these same candidate IDs as independent static corroboration; no new financial runtime/SQL/HTTP reproduction occurred.

The payroll schedule assumes no replacement live-period run before resumed payout, preserving the unique-index counterexample. A stale cancel changes Status, not an invented whole-row erase of PaidAt/treasury metadata. The day-admission schedule pauses deposit before its transaction/journal, then resumes after close commits. Packaging retains its separate Missing list despite contradictory completeness. Count-row multiplication and final difference can wrap, but LINQ sum is checked and negative quantity clamp is intended. Prep-cycle fallback returns Unknown instead of crashing. Reversed date inconsistency is input scoping, not unauthorized report access.

Original uncovered callable receipts now include authorization helpers, ControlCenter rules, customer matching, delivery responsibility/collection/settlement/scope/address/driver rules, with individually reasoned generated record contexts. No named/indexed symbol was promoted merely because its containing file had a prior report. Full financial completion remains open.
## Pending mixed delivery collection

FIN-019 follows a mixed delivery whose immediate cash leg already posted, while its electronic leg remains Pending. Driver responsibility uses the entire order total because the whole-order IsPaid flag remains false, so custody can include the immediate portion again. Ready/Assign/Result APIs do not universally enforce the documented electronic-payment dispatch freeze. The accepted correction must either enforce that freeze or define an explicit payment-conversion policy; pending electronic money is not automatically residual cash on delivery. [Independent challenge](Independent-Challenges/mixed-pending-delivery-custody.json) preserves the complete source trace and qualifications. Regression tests must cover immediate cash plus pending electronic, all-immediate splits, pure COD, partial refunds and correctly reported remaining collection without artificial variance. No live customer collection, cash posting, HTTP or SQL reproduction occurred.

FIN-005 citation correction: Purchase concurrency evidence is DbContext277-305 plus PurchasingEntities20-60/75-112, replacing irrelevant legacy Customer/Delivery excerpt309-328. Predecessor retained; [independent correction](Independent-Challenges/FIN005-model-reference-correction.json). Source finding remains static-only.

## Inventory unit admission

FIN-020 concerns an item with no movements: metadata update checks that history is empty, pauses, a valid first purchase receipt commits under the old unit, then metadata saves a new unit without rechecking. Historical quantities have no unit snapshot, so the new label reinterprets already-posted facts. Sequential movement-exists rejection works; the narrow stale admission schedule bypasses it without overlapping transactions. Receipt foreign-key locks cannot protect after its commit, and adding a metadata token alone is insufficient if stock writers never update the item. [Independent challenge](Independent-Challenges/inventory-unit-first-movement.json) retains8source hashes, counterevidence and exact model mapping245. Require a barrier test plus all stock-writer admission paths in an authorized disposable environment; no SQL schedule or measured loss was executed.

## Correction-design regression constraints

[Remediation regression risks](REMEDIATION_REGRESSION_RISKS.md) records parent-reviewed preservation and regression requirements for the37 existing financial records. This is engineering assessment; no fixes or extra runtime tests were performed. It does not close the remaining source/callable review gates.

## Tax mode accepted-input interpretation

FIN-021 is a configuration contract defect for an authorized settings manager: numeric string0 parses as defined TaxMode.Inclusive, but the parsed value is discarded and the raw string stored. Server checkout and both client estimates accept only literal Inclusive, otherwise use Exclusive. Named controls and numeric1 work; zero rate or disabled rule masks the difference. The affected rule must actually be selected, and the ordinary settings UI may send canonical names only. [Independent written challenge](../../Sub-Agents/Handoffs/tax-mode-independent-challenge.json) verifies7source hashes and counterexamples; [semantic comparison](../Duplication-and-Architecture/tax-mode-semantic-comparison.json) retains the trace. Reject noncanonical strings or persist the parsed defined mode canonically and share an explicit consumer parser. No unauthorized configuration change, actual tax charge, HTTP/SQL or new runtime probe was performed; historical frozen order values remain unchanged.

## Compensation representation and wage classification

FIN-022 accepts case variants of defined Daily/Monthly at profile validation but stores the raw spelling. Exact CLR consumer comparisons can omit automatic daily-wage posting or produce a zero base for an active monthly employee with positive configured salary and no DailyWage/Salary credits. The employee line is not skipped. Current editor choices emit canonical names and resaving can normalize metadata; manual credits can mitigate the example, so ordinary UI reproduction and universal unpaid wages are not claimed. [Independent challenge](../../Sub-Agents/Handoffs/compensation-mode-independent-challenge.json) verifies10source identities and the exact controls; [comparison](../Duplication-and-Architecture/compensation-mode-semantic-comparison.json) preserves the original lead. Canonicalize or reject representation consistently, preserving deterministic wage keys and frozen paid payroll; never automatically mint historical wages already manually compensated. No biometric/API/SQL/wage posting or payment was executed.

## Attendance UTC reconstruction and correction basis

FIN-023 stores check-in as UTC HH:mm under a Cairo date, then reconstructs it using checkout UTC calendar date. Same Cairo-day events on opposite UTC dates can yield future reconstructed check-in and a zero clamp, omitting eligible overtime. [Specialist disposition](../../Sub-Agents/Handoffs/attendance-utc-reconstruction-independent-disposition.json) and [root independent challenge](Independent-Challenges/attendance-utc-reconstruction.json) preserve the conditional13-hour example, same-UTC-date and repeated-punch controls. No actual host-zone rule, biometric/API/SQL acceptance or wage posting was executed.

FIN-CAND-CORRECTION-BASIS-001 is a separate requirements question. Both old/new deltas use current wage/settings; an original1500 overtime credit after doubled wage and correction1hour to0 can produce a3000 debit. This is a component arithmetic counterexample, not whole-account negative balance or a passed test. Under actual-posted reconciliation it overreverses; intentionally current-rate incremental policy remains an owner decision. [Specialist disposition](../../Sub-Agents/Handoffs/attendance-correction-rate-independent-disposition.json) and [root challenge](Independent-Challenges/attendance-correction-rate-basis.json) retain real locks/audit replay and unchanged-rate controls. No silent historical wage creation or source fix is authorized.

## Employee credit completion dispositions

[Written dispositions](../../Sub-Agents/Handoffs/employee-credit-completion-dispositions.json) classify signed custom earnings (FIN024), undefined numeric manual-ledger categories (FIN025) and distinct-credit cached running balances (FIN026), now independently source-corroborated and canonical. [Root challenge](Independent-Challenges/employee-credit-admission-and-cache.json) and [schema counterevidence search](../../Sub-Agents/Handoffs/employee-ledger-schema-constraint-counterevidence.json) retain the exact source qualifications; no deployed database acceptance is established. Ordinary positive/canonical client controls, protected actual payouts and correct fact-derived totals are preserved. The manual GUID/no-RequestId behavior is explicitly documented by the request contract, so it is rejected as a hidden-contract defect and retained as an operational recovery limitation. These records add no callable coverage or runtime verification.

## Frozen close snapshot financial contracts

FIN-027 is a report-permission asymmetry: stored reconciliation JSON is returned from current/history close-snapshot endpoints to a legitimate authenticated session without the live report reports_view helper. Authentication remains required; anonymous access, actual request acceptance and universal ordinary-screen reachability are not claimed. Client live reconciliation has a reports_view guard while archived snapshot load and HasSnapshot visibility lack the equivalent source guard. [Specialist independent challenge](../../Sub-Agents/Handoffs/financial-snapshot-contract-independent-challenge.json) verifies9source hashes and the controls.

FIN-CAND-CLOSE-SNAPSHOT-REVISION-001 is SOURCE_INVARIANT_GAP_WITH_UNVERIFIED_CONCURRENT_COMMIT_OUTCOME. Count-plus-one, demotion and insertion lack an explicit common day admission lock/token or unique day/revision/current index in the inspected source. This does not establish two successful commits: actual EF DML ordering, parent FK locking, conversion deadlocks and demotion after an earlier commit can defeat the proposed schedule. The saved challenge preserves these counterexamples and requires a controlled disposable SQL provider test before any reproduced-corruption claim. No new callable count, source change or runtime operation was made.

## Delegated employee credit acknowledgement recovery

FIN-028 is an acknowledgement/recovery defect after a legitimate delegated overtime/extra-shift command commits and its response is lost. The identical retry consumes/validates approval before checking the durable ledger ID, so an actor lacking own employees_ledger permission is rejected for the already consumed approval before receiving the prior success. The retained client resends the original request before resolving a new approval and preserves it after later422, producing a conditional source recovery loop. Own permission, an uncommitted first attempt and a new valid approval are countercontrols; duplicate money, lost ledger data, permanent global irrecoverability and executed UI/HTTP/SQL reproduction are not claimed. [Independent challenge](../../Sub-Agents/Handoffs/financial-employee-credit-approval-replay-independent-challenge.json) verifies9source identities and exact client/server path. Replay correction must bind actor, employee target and intended payload while preserving legitimate permission/approval admission for new writes.

## Full-refund overlapping acknowledgement gap

FIN-CAND-REFUND-REPLAY-001 is LOW and retains an unexecuted source-ordering schedule: B sees no receipt, A commits full remaining refund, then B loads updated parent and rejects zero refundable before rechecking the now-durable same key. Row lock/cap/unique key protect money; no double payout or over-refund is inferred. Later sequential replay succeeds, and actual cashier IsRefunding guard blocks ordinary same-screen overlap. Existing concurrent test uses8000 of20000 rather than full exhaustion. [Independent written challenge](../../Sub-Agents/Handoffs/financial-refund-locked-replay-independent-challenge.json) verifies4source identities, actual client/test countercontrols and requires a provider barrier test before reproduced concurrent-response claim. This differs from FIN028 spent-approval retained-request loop.
