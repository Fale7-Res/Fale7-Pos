# Code duplication and architecture

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. Source-only audit; no application fixes, refactoring, dependency changes, database operations or printing. The interim Git checkpoint remains `bc914a4d71a34de6ef6655887ce73140075c17e0`; this report and later work are local, unpublished.

تم استكمال طابور مقارنات الكود المكرر المحفوظ: لا توجد مجموعة تطبيق أو اختبارات معلقة فيه. ظهرت فروق مهمة في السياق والصلاحيات وعمر الاشتراكات رغم تطابق أجسام بعض الدوال. لا يعني ذلك اكتمال التدقيق المالي، ولا يثبت غياب كل تكرار معنوي أو كود غير مستخدم في المشروع.

## Scope, counts and evidence quality

The [semantic comparison registry](Duplication-and-Architecture/semantic-comparison-registry.json) additionally reconciles7bounded semantic groups and1private-constant observation across37source paths/152referenced callable contexts. These references overlap the AST scope and are **not** new financial function reviews. The exact-body denominator remains218; broader unbounded semantic/dead-code limitations remain below.

| Measure                                             |           Verified result | Meaning                                                                                    |
| --------------------------------------------------- | ------------------------: | ------------------------------------------------------------------------------------------ |
| C# files in original AST comparison inventory       |                       561 | Parsed source inventory; not 561 new full-file reviews                                     |
| Identified exact-body candidate groups              |                       218 | Bodies at least20tokens, identifiers/literals preserved                                    |
| Application context decisions                       |                        92 | 42prior +50new actual comparisons                                                          |
| Test context decisions                              |                        24 | All24previously pending groups compared                                                    |
| Generated migration/model groups                    |                       102 | Accounted historical repetition; not refactoring debt or fresh independent semantic review |
| Pending identified application/test groups          |                     0 / 0 | Original queue reconciled without resetting prior decisions                                |
| Distinct callable identities in context comparisons |                       287 | Includes nested lambdas/local functions; not 287 extra financial reviews                   |
| Distinct source paths in context comparisons        |                        92 | Comparison scope, not whole-file completeness                                              |
| All218groups, including generated history           | 2,607identities /127paths | Provenance accounting only                                                                 |

The [quality gate](Duplication-and-Architecture/mirror-final-quality-gate.json) validates source hashes, original AST identities, exact spans and individual saved decisions; it reports zero errors. It does **not** independently certify every conclusion or claim tests ran. The [complete ledger](Duplication-and-Architecture/clone-review-ledger.json) preserves every original group, classification and occurrence. The [status registry](MIRRORED_CODE_AUDIT_STATUS.json) has the exact current scope and no pending identified groups.

No similarity match was promoted merely because bodies hashed equally. All74previously pending groups have explicit comparison reasoning, caller/captured-context discussion, financial/security implications, recommendations and missing tests. Earlier42decisions and their provenance remain preserved. The102generated groups retain their historical classification and the existing model-equivalence evidence; changing old migration bodies to reduce repetition would be inappropriate.

## Architecture and ownership of truth

The [actual project-reference graph](Duplication-and-Architecture/project-reference-graph.json) contains8source projects and no project-reference cycle:

- Domain has no project reference; Application references Domain.
- Infrastructure references Application and Domain; Server references all three.
- Cashier WinUI, Call Center WinUI, legacy Cashier WPF and PrintAgent reference Application.

These are compile-time project edges, **not** proof that no runtime/DI/object-level cycle exists. A separate [declared constru ctor/service graph](Duplication-and-Architecture/constructor-service-reference-graph.json) resolves107explicit source service registrations and checks90constructor source files: no repeated service registration or cycle detected in those modeled edges. This is structural reference analysis, not107new deep reviews. Framework/DbContext registration extensions, service-location/scoped factories, callbacks and runtime constructor selection remain outside this graph; it cannot prove all services used or all runtime cycles absent. Typed endpoint wrappers and client API facades are useful boundaries. Removing wrappers solely for body equality would obscure request types, action-specific authorization and error contracts.

Server handlers and Domain rules remain the financial authority. Client estimates, entity caches and report projections are secondary views. Important multiple-representation risks already have financial findings: payroll status versus posted payment facts (FIN006 and the independently challenged payroll race), signed stock movements versus dashboard raw sums (FIN013), and employee cached `BalanceAfterPiastres` versus derived credit-minus-debit stock. This structural report links those records; it does not create duplicate financial defects or claim they were fixed.

## Meaningful comparisons and divergences

### 1. Identical caller bodies can resolve different behavior

`StatusPillView.OnTextChanged`, both clients `Controls/StatusPillView.xaml.cs:22–29`, is identical. Its client-local converter differs:

- Call Center `Converters/Converters.cs:277–296` indexes `Application.Current.Resources` directly.
- Cashier `Converters/Converters.cs:767–786` calls `ThemeResources.Get`; `Services/ThemeResources.cs:18–34` checks the window's actual Light/Dark dictionary then flat resources.

This is actual contextual divergence, related to existing UI004. No native theme-switch test was run here. Shared substring-based status selection also treats names containing `paid`, `active` or `settled` as success; negative-name states require reachability/binding tests before asserting a new observed visual defect.

Safe proposal: one explicit semantic status map with a theme-aware platform adapter. Keep dependency-property, unknown-value and live-theme behavior covered. Do not replace all resource lookup with one untested indexer.

### 2. Identical empty-state callbacks hide subscription lifetime differences

`CallCenterOrderPanelView.UpdateEmptyCartVisibility:25–30` matches Cashier `CartPanelView.UpdateEmptyCartVisibility:59–64`. Their surrounding ownership differs:

- Call Center `Views/CallCenterOrderPanelView.xaml.cs:14–20` attaches `Cart.CollectionChanged` on each `DataContextChanged`, with no corresponding detach in the source file.
- Cashier `Views/CartPanelView.xaml.cs:24–25,52–54` detaches old/unloaded subscriptions and tracks the current owner.

This corroborates existing UI015; it is not another distinct memory-leak finding or an executed retention benchmark. Safe extraction must include subscription lifecycle, not just the small visibility function. Required tests: repeated lock/unlock, DataContext A→B→A, one active subscription and no callbacks into unloaded views.

### 3. Captured configuration changes catalogue behavior

Both `ProductCatalogueView.ApplyItemWidth:27–39` bodies divide usable width by a target and enforce at least2columns. The constants at line16 differ: Call Center target190, Cashier212. Thus identical methods intentionally produce different station densities. The private `ProductMinWidth`/`ProductMaxWidth` constants appear only in their declarations in `src` C# search and are not enforced by this algorithm.

This is confirmed unused private-constant maintenance debt, not proof of actual text clipping. Propose a shared catalogue control with explicit density/bounds policy, or remove misleading unused constants after owner-approved implementation. Test narrow/normal/wide widths, first layout before panel realization, offers and variable row height.

### 4. Shared monetary parsing must retain unit-specific rules

Four Arabic-digit normalization lambdas match across both CashInput helpers, `CashierWinUI/Services/MilliUnits.cs:18–20`, and `AttendanceViewModel.cs:146–148`. Parent rules differ:

| Parent           | Representation and bounds                                          |
| ---------------- | ------------------------------------------------------------------ |
| CashInput        | Decimal EGP,2decimal places, nonnegative, bounded before×100      |
| MilliUnits       | Decimal stock units,3decimal places, bounded before×1000          |
| Attendance hours | Finite nonnegative double hours; not limited to currency precision |
| Attendance count | Nonnegative integer count                                          |

Extracting character normalization is reasonable. Replacing all parsers with a money parser would lose behavior. Client validation is not protection for raw server requests; FIN016/FIN018 remain separate server-boundary findings.

### 5. Approval UX is repeated; action authority must stay on the server

Five approval resolver bodies match across Cashier Operations345–367, ControlCenter Reconciliation136–153, Employees390–410, Inventory696–716 and Purchasing604–624. They short-circuit when no prompt is needed, check minimum input length, make one `IssueManagerApproval` call and return issued IDs.

Additional renamed/captured-context duplication exists in `DeliveryDirectoryViewModel.ResolveApprovalAsync:246–262`: it captures prompt fields and hardcodes `delivery_manage`, rather than taking generic permission/error arguments. This is explicitly recorded as new semantic group SEM-MIRROR-001 outside the218ASTdenominator.

A typed shared issuer can reduce drift. It must preserve action-specific approval, one issuance, returned authority identity, caller retry state and error routing. A client `needsApproval=false` is advisory; the server must still enforce its own permission/token policy. No actual PIN value, token or login request was used in this comparison.

### 6. Same financial formula, different legitimate row scopes

Treasury destination-minus-source aggregation repeats in DailyStatement146–147, BusinessDayCloseEvaluator75 and ReportingQueryService179. The first two use all-time Posted history; the selected business-day report filters `BusinessDayId` and Posted. A day net movement can be negative while the all-time account stock is positive. Consolidating queries without preserving this scope would change financial meaning.

Likewise, `EmployeeRules.CalculateBalance`, employee queries and owner obligation reports share credit-minus-debit stock, whereas earning/payment buckets are date-scoped. Cached per-row `BalanceAfter` is a third representation requiring writer serialization, not another authoritative total.

Propose pure aggregation helpers with explicitly supplied row scope. Do not hide filters inside a generic “balance service” without an agreed current-versus-period contract. Existing source financial findings and unresolved cache questions remain owned by Financial Integrity.

### 7. Treasury command scaffolding repeats, policies intentionally differ

Five `FindExistingAsync` helpers have the same NoTracking unique-key lookup. Their callers have different key prefixes, permissions, source/destination accounts, shift requirements and companion records.

- SafeDrop: fixed Drawer→Main, existing shift, `safe_drop`.
- Transfer: distinct known accounts, `treasury_manage`, movement-linked handover.
- Employee cashout: Drawer and Open shift; an advance intentionally permits employee debt, a withdrawal is limited by earned balance.
- Standalone employee payment: Main/Bank/InstaPay, no payroll run, employee balance limit.

Actual SafeDrop/Transfer account checks are inside their transaction, contrary to any blanket claim all treasury balance checks are outside. Both employee paid-debit paths lock an employee anchor first; different internal account/ledger lock order alone does not prove a same-employee deadlock. Durable shift/day admission remains a separate financial concurrency investigation.

Safe shared infrastructure may cover lookup, transaction/replay response handling and journal plumbing. Do not unify business eligibility, approval attribution or source account policy merely because surrounding code resembles another handler.

### 8. Client checkout estimates and server calculation are related, not interchangeable

Cashier `CashierViewModel.cs:250–259` estimates tax on max(0,subtotal−discount), then delivery fee−comp. Call Center `CallCenterViewModel.cs:267–273` estimates tax on subtotal plus delivery fee, with no manual discount/comp capability in this path. Server `CheckoutRules.BuildPlan:93–97` and `ComputePayableTotal:35–39` use the same central TaxEngine and corresponding operation order.

The supported-input operation order agrees; this is not a full runtime numerical parity proof. The financial reviewer independently traced the server repeating unchecked price×quantity and stock-quantity multiplication; [qualified FIN016 scope extension](../Sub-Agents/Handoffs/financial-pricing-overflow-fin016-extension.json) requires extreme authoritative catalog setup for that example. Server price authority therefore does not establish range-safe arithmetic. No extra finding count or runtime claim was added. Menu/offer staleness, server revalidation and fee bounds remain material. Shared checkout-item selectors serialize identity/quantity/offer/notes, not arbitrary client effective unit prices. Preserve station-specific request envelopes, split payments and authoritative server results.

### 9. Typed query/mapping repetition is usually intentional

- PrepRecipeLine/PurchaseLine EF lambdas have matching key/restrictive inventory FK statements but bind different entity types and parent lifecycles.
- Six reporting timestamp predicates preserve the same noncancelled sale cohort; they do not repair upstream reversed-date normalization (FIN-CAND-REPORT-RANGE-001).
- Inventory and food-cost loaders map current items/purchase facts identically; central CostingRules remains the formula authority. They both use current cost context, not historical sold-date cost snapshots.
- Active/all delivery-service DTO projections are equal, with intentionally different outer filters.
  -13typed endpoint adapter groups bind distinct request/handler types. Shared exception translation is possible after contract tests; shared authorization inferred from text equality is unsafe.

Prefer a small shared mapper/loader only where it removes drift without concealing query scope, financial history policy or typed boundary.

### 10. Test duplication must preserve scenario and temporal independence

All24test groups have individual context decisions. Many are repeated readbacks/counts in materially different scenarios: sequential replay versus concurrent same-key race, distinct-key settlement races, shortage versus overage, before/after frozen financial fields, and changes to settings versus original snapshots.

Do not remove a concurrency scenario because its final count assertion matches another test. A shared before/after read helper must remain fresh and NoTracking; caching a returned entity can make an immutability assertion vacuous. Request builders must capture one stable ID outside a repeated-send function. Setup helpers with equal bodies may have different default employee numbers, permissions and whether a shift is already open.

Several fixtures bypass business commands to seed cost facts; fake HTTP callbacks only prove request/UI behavior. Neither establishes real backend acceptance or purchasing accounting. No fixture was executed by this workstream and no database/setting was changed.

A [targeted financial test-context spot-check](../Knowledge/Quality-Reviews/financial-test-scenario-coverage-spotcheck.json) preserves6method contexts across3test files without new reviewed-function claims. The confirm fixture creates standalone InstaPay, not mixed payment. Purchase concurrency reuses one request ID, not distinct invoice payments. The payroll lifecycle test's name ends “to_paid” but its body stops at Approved, while the separate concurrent-pay test never cancels. These useful tests do not establish the missing invariants; this is a scoped source comparison, not a claim no other tests exist or that any test passed.

### 11. Short-body/class copies outside the original threshold

The complete `Models/CartLine.cs` sources in Cashier WinUI, Call Center WinUI and legacy WPF were actually read and compared: equal after namespace substitution. Quantity/notes Toolkit input fields, readonly product/offer metadata, local price×quantity display getters and quantity notifications match. This is new semantic group SEM-MIRROR-003, separate from the218ASTdenominator; small accessor/notification bodies are not silently covered by the20token inventory.

A shared client presentation model is technically plausible because these classes depend on Toolkit rather than native framework controls. Preserve generated binding names/notifications and the estimate-only financial role. Unchecked local multiplication can misdisplay extreme input; server revalidation is a separate boundary, not a blanket guarantee of zero operational consequences. No new shared project or source change was created.

## Safe refactoring priorities, without implementation

| Priority | Proposal                                                           | Required preservation and tests                                                                                        |
| -------- | ------------------------------------------------------------------ | ---------------------------------------------------------------------------------------------------------------------- |
| First    | Fix confirmed divergent behavior before broad extraction           | UI004/UI015 source evidence, actual theme/lifecycle tests; financial findings handled separately                       |
| Next     | Shared transport/request journal and unit normalization            | Station storage ownership, atomic retained bytes, stable retry IDs, outcome-unknown versus read errors, unit precision |
| Next     | Shared approval issuance and typed DTO mapping                     | Action token semantics, server authorization, typed handlers and caller-owned query scopes                             |
| Later    | Common WinUI controls/catalogue adapters                           | Theme resources, automation IDs, RTL, density, routed-event behavior, detach/dispose lifecycle                         |
| Later    | Shared integration-test builders                                   | Per-test DB/actor isolation, distinct defaults, explicit setup mutations, serial/concurrent assertions preserved       |
| Avoid    | Rewriting generated migration history or merging all command rules | Historical schema identity, different permissions/accounts/eligibility and independent business invariants             |

No new backend is required by this structural comparison. The existing Server/Infrastructure/Domain responsibilities are real. A new shared library would be a future owner-approved implementation decision, not an audit action.

## Remaining limits and original Phase11 status

The identified comparison queue is complete and the written report is substantive. Original Phase11's broad repository-wide semantic/dead-code claim remains **qualified**: the original AST finder excludes bodies below20tokens and does not establish all renamed equivalents, inactive preprocessing branches, generated runtime members, reflection/XAML/DI reachability, unused-service safety or every type-level dependency cycle. One additional renamed group, explicit financial semantic comparisons and confirmed unused private constants supplement the inventory; they do not make those exclusions disappear.

Therefore this report must not be used to claim an exhaustive “no duplicates/dead code/cycles” clearance or all15audit phases complete. It gives precise completed coverage and actionable proposals with those limitations visible. Further whole-repository symbol/behavior closure remains tied to the original audit backlog. Financial completion is still a separate open gate; paused Printing/UI/general Performance must not restart automatically.

## Canonical evidence

- [Initial AST inventory](Duplication-and-Architecture/ast-duplicate-candidates.json), [per-group ledger](Duplication-and-Architecture/clone-review-ledger.json), [source/evidence quality gate](Duplication-and-Architecture/mirror-final-quality-gate.json).
- [Financial query/mapping comparisons](Duplication-and-Architecture/mirror-financial-batch-evidence.json).
- [Converters and unit normalization](Duplication-and-Architecture/mirror-converters-batch-evidence.json).
- [Client identity/cart/approval/state](Duplication-and-Architecture/mirror-client-state-batch-evidence.json).
- [Control adapters and contextual divergences](Duplication-and-Architecture/mirror-controls-batch-evidence.json).
- [Test fixtures and assertions A](Duplication-and-Architecture/mirror-tests-a-evidence.json), [tests B](Duplication-and-Architecture/mirror-tests-b-evidence.json).
- [Additional semantic groups and dead constants](Duplication-and-Architecture/semantic-supplements.json), [project graph](Duplication-and-Architecture/project-reference-graph.json).
- [Earlier API transport comparisons](Duplication-and-Architecture/client-api-financial-mirror-review.json), [payout policy](Duplication-and-Architecture/employee-payout-policy-comparison.json), [treasury policy](Duplication-and-Architecture/treasury-safe-drop-transfer-comparison.json).
- [Master findings](MASTER_FINDINGS.json), [ongoing financial report](Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md), [original15phase backlog](../Conversation-Recovery/PAUSED_AUDIT_BACKLOG.json).

## Additional semantic contract comparison — delivery payment state and amount

[SEM-MIRROR-004 source comparison](Duplication-and-Architecture/delivery-payment-contract-semantic-comparison.json) traces per-leg money, delivery DTO, wire strings, client default and server result validation. It is outside the218exact-body groups, not another whole-file audit or additional financial callable count.

- Per-leg immediate collection versus whole-order custody is already FIN019; no duplicate finding ID.
- Canonical server electronic status is lowercase `pending`; the dispatch client tests uppercase `Pending`. Source predicts the pending warning is missed and the label falls back to collection on delivery. No native observation claimed.
- A prepaid non-Cash delivery DTO still carries gross net-of-refund amount; client outcome defaults to that amount while server expects zero. Unchanged positive input with no reason is rejected; manually entering zero is a workaround. A reason permits positive collection, but actual double collection was not tested.

The two new source-supported financial presentation/default candidates await the Financial Integrity specialist's independent challenge and deduplication. They are not promoted to confirmed runtime defects. Preserve the gross amount as a separately named fact, unify the payment-state wire contract, and derive actual collection/defaults from authoritative per-leg responsibility. No UI visual or printing specialist was resumed.

Independent followup: [original financial specialist challenge](../Sub-Agents/Handoffs/delivery-payment-contract-independent-challenge.json) corroborated the wire-case mismatch as a FIN019 source extension and the prepaid default mismatch with the server variance guard intact. The latter remains a usability/regression followup; no new canonical defect ID or real double-collection claim. Case comparisons inside translated SQL predicates elsewhere are separately provider/collation dependent; the in-memory client string defect must not be generalized to those queries without database collation evidence.

## Additional semantic comparison — stock units and cost arithmetic

[SEM-MIRROR-005](Duplication-and-Architecture/cost-quantity-semantic-comparison.json) traces the shared BaseQuantityMilliUnits and PurchaseLineTotal calls through recipe costing, stock deduction, inventory line projection and sold-cost reporting. Conversion is shared, not independently duplicated: unknown Yield conversion skips posting but marks costing incomplete. Money boundary conversion, stock milliunits and per-portion costs have distinct units/rounding roles. Per-order packaging runs once, while its completeness issue is already recorded separately. No new defect or financial callable count added; policy/rounding and isolated integration tests remain required. Existing tests were inspected, not run.

## Additional semantic comparison — signed money versus command admission

[SEM-MIRROR-006](Duplication-and-Architecture/signed-money-boundary-semantic-comparison.json) compares Money, deposit, checkout fee, purchase-line and cash-count admission. Shared signed Money is intentional for balances/differences; Deposit correctly requires a positive amount. Existing FIN016/FIN018/cash-count defects are specific boundary gaps, not proof that all monetary values must be positive or every primitive operation wraps. Reuse validation only where sign/zero/authority/unit policies agree. No new defect, function receipt, test execution or fix.

## Additional semantic comparison — tax-mode validation and interpretation

[SEM-MIRROR-007](Duplication-and-Architecture/tax-mode-semantic-comparison.json), independently source-challenged in [the financial handoff](../Sub-Agents/Handoffs/tax-mode-independent-challenge.json), is canonical FIN021. Create/update tax validation accepts defined enum numeric string `0` (Inclusive) but persists the raw string. Checkout and both client estimates compare only literal `Inclusive` and default everything else to Exclusive. Agreement between the three copied mappers therefore does not establish correct interpretation of accepted input.

This requires legitimate `settings_manage`, noncanonical configuration input, a selected enabled rule and positive rate; current UI may only send named values. Named modes work, lowercase names are rejected by the case-sensitive parser, numeric1 maps consistently to Exclusive, and historical frozen taxes are not rewritten. MEDIUM source defect, no actual charge/API/SQL/component test reproduced. Reject unsupported representations or persist a validated canonical enum, then use an explicit shared parser rather than an unknown-to-Exclusive fallback. Preserve zero/disabled controls, config replay and old order snapshots.

## SEM-MIRROR-008 — compensation representation and earnings

Profile create/update accepts case variants with ignore-case enum parsing, then stores the raw string. Attendance tests exact `Daily`; payroll materializes employees then tests exact `Monthly`. Accepted `daily` can omit automatic daily credit, and active `monthly` with positive configured salary but no DailyWage/Salary period credits gets zero base. This is an accepted-input interpretation defect, independently corroborated as MEDIUM FIN-022; no payroll/SQL/API execution or measured loss. Shipped editor sends canonical names and resave can normalize; manual credits can mitigate. Manual ledger correctly persists parsed enum.ToString, so its lowercase bonus path is a positive control. Preserve frozen payroll and previously compensated days before any future normalization/backfill.

Evidence: [ten source identities and exact symbol/range chain](Duplication-and-Architecture/compensation-mode-semantic-comparison.json), [independent challenge](../Sub-Agents/Handoffs/compensation-mode-independent-challenge.json).

Current bounded semantic registry: 8 groups, 46 referenced paths and 188 callable contexts; these are not additional authored financial function receipts. The218 identified AST groups remain accounted for. Exhaustive renamed/dead-code/runtime reachability remains qualified.

## SEM-MIRROR-009 — employee balance contracts

The repeated credit-minus-debit formula has four contracts: saved facts, own pending EF entries, locked spendable balance, and persisted statement snapshots. Poster includes its own pending entries; query balance intentionally does not. Payout locks employee then ledger before checking funds. Manual/earning append snapshots do not establish equivalent cross-command serialization, and their BalanceAfter is displayed in the statement. Preserve those differences during any future extraction; a shared persisted-only sum could regress combined adjustments, and sharing an unlocked helper could weaken payout controls. Underlying additive facts can remain correct even when a displayed cached balance is stale.

[Source identities and four comparisons](Duplication-and-Architecture/employee-balance-contract-semantic-comparison.json); [qualified financial challenge](Database-and-Finance/Independent-Challenges/employee-credit-admission-and-cache.json). No provider concurrency/UI tests or new primary callable receipts.

Latest bounded semantic union:9groups, 49 paths, 225 referenced callable contexts. Historical earlier counts above describe earlier checkpoints. Identified218ASTgroups remain accounted; unbounded renamed/dead-code/runtime reachability remains qualified.

## SEM-MIRROR-010 — financial lock and recovery contracts

Standalone payout takes employee anchor, employee ledger, then protected treasury movements. Drawer cashout takes the same employee anchor, treasury movements, then employee ledger. The inner inversion alone does **not** establish a same-employee deadlock: the common outer anchor serializes those commands before either reaches its inner sequence. Different employees do not share the same logical ledger range, although actual SQL plans, range overlap and escalation still require provider tests. Attendance correction acquires attendance then ledger, with no later employee update-lock in the inspected handler; no immediate reverse-anchor cycle is established.

Both payout handlers roll back before replay lookup. The contention helper recognizes1205/1222 and performs up tofive delayed readlookups; it does not automatically rerun financial commands. Its same-request comment is stronger than reality because different keys can contend too; the no-sibling path returns retry conflict. Preserve transaction and outer-anchor contracts during any consolidation. FIN011 remains the separate known before-transaction balance-read issue.

[Five source identities, exact symbols/ranges, acquisition orders and rejected hypotheses](Duplication-and-Architecture/employee-financial-lock-order-comparison.json). No SQL/deadlock/performance tests or new financial finding/author receipt. Latest bounded semantic union:10groups, 53 paths, 243 callable contexts.218identifiedASTgroups remain accounted; exhaustive renamed/dead-code/global lock reachability is qualified.

## SEM-MIRROR-011 — live and frozen financial report contracts

The close handler serializes the shared reconciliation DTO; snapshot endpoints return that stored JSON; the closed-day client deserializes the same DTO with matching default serializer and labels it as frozen. Reopen keeps archive history while the client loads the frozen card only for closed days. No current casing/DTO mismatch or silent recomputation of archived figures was established.

Financial access diverges: live reconciliation checks reports_view/all, while frozen latest/history routes have session authentication and no matching report gate. The frozen client path also differs from the live permission guard. This is independently source-corroborated MEDIUM FIN027; no real session/API/UI test, and ordinary navigation reachability remains bounded.

The close-versus-close revision/Authoritative invariant is a separate qualified candidate: stale state and Count+1/demotion have no explicit per-day serializer in inspected source. Actual FK locks, EF DML order and failed transactions can defeat a proposed double-commit schedule. Duplicate authoritative snapshots were **not** reproduced or unconditionally confirmed. Preserve this counterevidence when proposing provider tests or constraints.

[Exact12source identities, symbols/ranges and comparisons](Duplication-and-Architecture/live-frozen-close-snapshot-comparison.json); [independent financial challenge](../Sub-Agents/Handoffs/financial-snapshot-contract-independent-challenge.json). No new authored-callable counts or financial/runtime operation. Latest bounded semantic union:11groups, 63 paths and 290 referenced contexts;218identified ASTgroups remain accounted, broader Phase11 limits unchanged.

## SEM-MIRROR-012 — durable replay and one-use approvals

CommandReceipt and attendance correction bind a saved receipt to actor and target; employee payout/cashout helpers use deterministic keys without the same explicit tuple checks. These are different contracts, not automatic proof of unauthorized writes. A receipt replay must be distinguished from posting a second financial movement, and neither tuple helper binds the whole payload. Daily wage deliberately rejects its natural-key duplicate and must retain that business rule.

Overtime and extra-shift commands authorize before looking up their deterministic saved credit. An approved first commit consumes the delegated token. If its response is lost, an identical retry by an actor without employees_ledger/all rejects the spent approval before reaching the saved entry. The actual client reuses its retained DTO before requesting another approval and keeps a previously unresolved request after a later422. This is independently source-corroborated MEDIUM FIN028: an acknowledgement/recovery failure, not duplicate payment or lost ledger facts. Own-permission actors and a newly issued approval can reach replay; the first operation must actually have committed. No API, native UI, transport-loss or SQL test was executed.

A future shared replay resolver must validate actor/target/payload and current session before returning a durable receipt, without consuming new approval for an already committed command. Preserve fresh-write authorization, atomic token/ledger persistence and payout-specific transaction/locking controls.

[15 source identities and exact symbols/ranges](Duplication-and-Architecture/employee-replay-contract-semantic-comparison.json); [independent source challenge](../Sub-Agents/Handoffs/financial-employee-credit-approval-replay-independent-challenge.json). Latest bounded semantic union:12 groups,72 source paths,349 referenced contexts; no new authored financial callable receipts.218 identified AST groups remain accounted; exhaustive Phase11 renamed/dead-code/runtime reachability remains qualified.

## SEM-MIRROR-013 — payroll state labels and settlement effects

Generic transition and dedicated payment both assign Paid, but only payment writes the treasury movement, line status and ledger settlements atomically. The generic employees_manage route accepts Approved→Paid from the shared table without payroll_pay, leaving a paid-looking run that dedicated payout subsequently rejects when no durable movement exists. This is existing HIGH FIN006, not a new finding. The payment comment that Paid proves a committed movement is too broad in the presence of that other writer; its actual lookup/fallthrough does not fabricate a receipt or pay twice.

The shipped client stops generic progression at Approved and uses a separate payment gate. That is a real normal-UI countercontrol, while the API still exposes generic terminal targets. Same-run replay keys and the six-way duplicate-pay test address repeated payment, not competing pay/cancel stale state. The existing pay/cancel candidate remains source-supported with provider acceptance untested. Client monotonic status reconciliation is also a different contract from an actor-bound immutable financial receipt.

[Eight source identities and exact symbols/ranges, call paths and test assertions](Duplication-and-Architecture/payroll-state-settlement-semantic-comparison.json). Existing source challenges and candidate limits are cross-referenced there. No tests executed, no new financial finding or authored callable receipt. Latest semantic union:13bounded groups,78 paths,387 referenced contexts. The218 identified AST groups remain accounted; broader original Phase11 reachability/renamed equivalence limits remain explicit.

## SEM-MIRROR-014 — financial approval provenance

Gate success and persisted approver identity diverge in two related implementations. Shift close can retain a presented name that policy did not validate; driver settlement validates its stricter variance approval but discards that gate result and persists the earlier base actor fallback. For a dispatcher with delivery_manage but without delivery_settlement_approve/all, a valid manager-approved variance is consequently attributed to the dispatcher in the financial settlement, event, audit and positive-cash movement. The strict permission check still runs. Own strict-permission actor attribution remains a valid control; the approval registry retains historical manager identity even though these rows do not link its approval ID.

This extends the existing MEDIUM FIN017 provenance family with a separately preserved driver variant; no duplicate finding ID was created. The shipped client explicitly requests the strict manager approval for a variance. The current integration method asserts rejection without reason/approval and acceptance with expected difference, but does not assert persisted manager attribution. No UI/API/database test was executed. A future change should carry verified relevant gate provenance separately from the acting user, preserving permission-or-approval policy rather than requiring another manager unconditionally.

[Five source identities, exact symbols/ranges and comparison](Duplication-and-Architecture/financial-approval-provenance-semantic-comparison.json); [independent financial challenge](../Sub-Agents/Handoffs/financial-driver-variance-approver-independent-challenge.json). Latest bounded semantic registry14groups81paths395contexts; no new authored financial receipts.218identified ASTgroups still accounted; originalPhase11 global reachability/renamed equivalence limitations unchanged.

## SEM-MIRROR-015 — supplier facts versus cached invoice ageing

The domain supplier outstanding rule supports a per-invoice filter on PurchaseId or ReferenceId. The query’s supplier total aggregates all supplier facts; ageing instead reads cached positive invoice remaining amounts. These populations intentionally differ. Actual source contradicts the ageing comment that ledger payments cannot be attributed to invoices. An OR match does not count the same row twice. Initial receipt and sequential subsequent payment are intended to keep ledger facts and cache consistent, but this does not establish concurrent consistency.

Existing FIN005 admission reads invoice ledger outstanding before the transaction; the correctly placed protected-cash balance lock protects cash liquidity, rather than serializing invoice remaining admission. Keep these distinctions when consolidating repeated balance logic. The existing six-way payment test uses one captured request key and asserts one movement; it does not test distinct requests competing for the same invoice. The per-invoice domain test and sequential full settlement test are useful controls, read here without execution.

[Six source identities, exact symbols/ranges and actual assertions](Duplication-and-Architecture/purchase-balance-projection-semantic-comparison.json). No new finding, primary callable receipt or runtime operation. Latest semantic union15bounded groups85paths438referenced contexts;218identified ASTgroups remain accounted and broader originalPhase11 limits are preserved.

## SEM-MIRROR-016 — durable replay after serialization

Standalone employee payout and drawer cashout recheck their saved receipt after acquiring the employee anchor. Refund checks only before acquiring its order lock. If a duplicate has already observed no receipt, then loads the order after its sibling committed a full refund, zero remaining rejects it before duplicate-write recovery. This is the independently challenged LOW candidate FIN-CAND-REFUND-REPLAY-001: conditional acknowledgement failure, not extra money, over-refund, or permanent loss. A later sequential retry reaches the saved receipt and succeeds.

The existing six-way same-key refund test uses8000 out of20000, so remaining is not exhausted. The client blocks ordinary same-screen overlap with IsRefunding. These are substantive countercontrols and limit the finding; no provider/API/UI concurrency run was performed. Future in-lock replay resolution must retain actor/order/payload binding, current-result versus original-response policy, refund caps, funds checks and rejection of unrelated new requests.

[Six exact source identities and bounded comparison](Duplication-and-Architecture/refund-locked-replay-semantic-comparison.json); [independent source challenge](../Sub-Agents/Handoffs/financial-refund-locked-replay-independent-challenge.json).16 bounded semantic groups/89paths/478referenced contexts; no primary callable review counts added.218identified ASTgroups remain reconciled with the broader Phase11 limitations preserved.

## Earlier financial comparisons — provenance reconciliation

Four earlier bounded comparison artifacts remain part of the evidence and were not regenerated: employee payout policy, safe-drop versus transfer, business-day authorization mirrors, and deposit/withdrawal/display versus locked balance. [Current source and saved-receipt linkage gate](Duplication-and-Architecture/legacy-financial-comparison-provenance-gate.json) verifies16 source-reference records against current hashes and saved reviewer shards. It adds no manual-comparison or callable credit and preserves historical qualifiers, including old pending independent-review statements. Employee payout overlaps SEM009/010/012 and is not another duplicate group. Safe-drop/transfer policy differences and raw approval-ID metadata questions remain discoverable in their original artifacts; no global semantic equivalence or reachability clearance follows from this mechanical check.
