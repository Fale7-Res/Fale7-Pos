## Current evidence-quality checkpoint — 2026-10-24T12:19:35.062Z

Finite AST candidate dispositions remain 218/218. Bounded semantic comparisons now record 66 groups across 65 artifact inputs; reconciled inputs contain 455 source-reference records over 215 paths, 1027 stable symbol IDs and 705 line ranges. Source, symbol, line-range and artifact reference checks pass. This validates saved evidence identity only; Phase 11 remains OPEN.

SEM-MIRROR-066 compares active Call Center WinUI and Cashier WinUI session responsibilities. Shared permission and per-user cache patterns coexist with intentional role differences: Call Center refreshes the global open shift before checkout; Cashier carries its operator shift and tracks pending management operations. In the inspected state getter, Call Center busy blocking through CanEditOperation and Cashier’s explicit busy clause are equivalent because Cashier CanEditOperation already excludes busy state. No new finding. Artifact: callcenter-cashier-session-role-and-operation-guard-semantic-comparison.json.

SEM-MIRROR-065 records client/server attendance numeric-bound divergence; inspected payroll formulas clamp negative late/overtime financial effects to zero. It establishes malformed attendance persistence, not increased payout or a confirmed owner-policy defect. The 219 manifest paths are historical keyword leads, not proven uncovered clones. Renamed-equivalence, generated reachability and exhaustive dead-code review remain limited; no runtime/tests/database/source changes.

---

## Current evidence-quality checkpoint — 2026-10-24T12:15:43.224Z

Finite AST candidate dispositions remain 218/218. Bounded semantic comparisons now record 65 groups across 64 artifact inputs; reconciled inputs contain 451 source-reference records over 213 paths, 1021 stable symbol IDs and 701 line ranges. Mechanical artifact/source/index/span checks pass. This validates references only, not semantic completeness; Phase 11 remains OPEN.

SEM-MIRROR-065 compares AttendanceViewModel input checks with CorrectAttendanceHandler and PayrollRules: the client blocks negative/non-finite values, the server does not enforce equivalent numeric bounds, and negative late/overtime produce zero financial amount in the inspected domain rules. This establishes a persisted-data validation divergence, not additional payout, unauthorized change, or a confirmed owner-policy defect. No canonical finding added; owner/business bounds and tests remain open. Artifact: attendance-correction-client-server-validation-semantic-comparison.json.

No tests/runtime/db/source changes. The 219 manifest paths remain historical search leads; semantic renamed-equivalence and exhaustive dead-code/reachability coverage are not proven.

---

## Current evidence-quality checkpoint — 2026-10-24

Finite AST clone candidates: 218/218 dispositioned in the existing ledger. Bounded semantic comparisons currently recorded: 64 groups across 63 comparison artifact inputs. Reconciled mixed-schema evidence contains 448 source records over 212 paths, 1,014 stable symbol IDs and 696 line ranges; artifact/source/index/symbol/range integrity passes. These checks validate saved reference structure only, not semantic completeness or correctness.

The 219 paths in the saved remaining-semantic-search-leads manifest are keyword/search leads without a recorded semantic comparison; they are not proven clone groups. Reflection, XAML/generated reachability, renamed equivalence and exhaustive dead-code review remain outside this finite evidence set. Phase 11 remains OPEN.

Latest comparison SEM-MIRROR-063 compares `EmployeeQueryService.GetFinancialReportAsync` / `GetBalanceAsync` with `DailyStatementQueryService.BuildEmployeeRowsAsync`. Shared all-time balance arithmetic matches; report period, employee cohort and category projections differ. A possible daily statement category-completeness requirement remains unconfirmed; no defect or new canonical finding is asserted. Artifact: `Duplication-and-Architecture/employee-financial-report-vs-daily-statement-projection-semantic-comparison.json`.

Static comparison only; no runtime tests, database writes, source edits or physical operations were performed. Full Phase 11 semantic/dead-code clearance remains open.

---

## Current verified checkpoint (2026-10-21T09:17:21.946634+00:00)

The finite clone inventory remains 218/218 dispositioned. The bounded semantic registry now contains 45 groups, 44 artifact inputs, 345 source-reference records, 192 paths, 837 stable callable IDs, and 528 line ranges. The mechanical source/hash/symbol/range gate passed with zero errors, and report-link validation found 108 links with zero broken. These checks establish reference integrity only; they do not certify semantic exhaustiveness or full callable reasoning. The latest bounded comparison is [SEM-MIRROR-045](Duplication-and-Architecture/negative-variant-price-semantic-comparison.json), which records a conditional server-side variant-price validation gap and its FIN-CAND cross-reference. Phase 11 remains incomplete/non-exhaustive.

> **Latest coordinator reconciliation (2026-10-21):** finite AST candidates 218/218 dispositioned; semantic registry 37 groups / 36 evidence inputs. Latest mechanical identity gate: 272 source-reference records, 162 paths, 658 callable IDs and 419 ranges. SEM-MIRROR-034 records two shift-close parity gaps: the dedicated screen does not retain an unknown request key, and it can close while a draft cart remains because the cashier-only close guard is bypassed by shell navigation. Server protections counter duplicate money posting; the workflow consequence depends on the intended draft policy. The latest refreshed manifest records 232 unreferenced keyword-hit paths; these are leads only. SEM-MIRROR-035 covers expense entry/client-server boundaries and SEM-MIRROR-036 covers payroll read projection parity. Phase 11 remains INCOMPLETE/non-exhaustive. Phase 11 remains INCOMPLETE/non-exhaustive.


> Latest coordinator reconciliation (2026-10-21 09:25 UTC): 218 identified AST candidate groups remain dispositioned (92 application and 24 test context groups, plus 102 generated-history groups); no candidate group remains pending. The bounded semantic registry now has 31 comparisons/30 artifact inputs and its mechanical gate validates 227 source-reference records across 145 paths, 616 stable callable contexts and 363 ranges; report-link validation is being refreshed after this update. These are evidence identity/link checks, not exhaustive semantic coverage. Renamed semantic equivalents, reachability and inactive/dead-code review remain qualified. Joint matrix: [FINANCIAL_MIRROR_JOINT_RECONCILIATION.md](../Conversation-Recovery/FINANCIAL_MIRROR_JOINT_RECONCILIATION.md).

# Code duplication and architecture

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. Source-only audit; no application fixes, refactoring, dependency changes, database operations or printing. The interim Git checkpoint remains `bc914a4d71a34de6ef6655887ce73140075c17e0`; this report and later work are local, unpublished.

تم استكمال طابور مقارنات الكود المكرر المحفوظ: لا توجد مجموعة تطبيق أو اختبارات معلقة فيه. ظهرت فروق مهمة في السياق والصلاحيات وعمر الاشتراكات رغم تطابق أجسام بعض الدوال. لا يعني ذلك اكتمال التدقيق المالي، ولا يثبت غياب كل تكرار معنوي أو كود غير مستخدم في المشروع.

## Scope, counts and evidence quality

The [semantic comparison registry](Duplication-and-Architecture/semantic-comparison-registry.json) adds bounded comparisons beyond the exact-body candidate queue. Its references overlap AST and prior function scope and are **not** new financial function reviews. The exact-body denominator remains218; broader unbounded semantic/dead-code limitations remain below.

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

### Bounded semantic search beyond the AST candidates

The repository-wide keyword pass was recomputed on the 468 tracked `src/**/*.cs` files, excluding tests and non-C# files. Case-insensitive leads cover money/payment/financial terms, business-day/shift/close state, request-id/retry/idempotency, and permission/actor/approval. There are 360 files with at least one term. The current 31 artifact inputs reference 121 tracked source paths; 244 keyword-hit paths have no semantic artifact source reference. The updated path/hash lead inventory is [remaining-semantic-search-leads-20261021-0719.json](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-0719.json); the preceding scan/count method plus exclusions and negative evidence remain in [semantic-mirror-targeted-search-20261021.json](Duplication-and-Architecture/semantic-mirror-targeted-search-20261021.json).

Those 244 entries are **candidate leads**, not 251 proven missed duplicates: the set includes DTOs, migration history, and unrelated keyword contexts, and a file may have a separate file report. The keyword scan does not prove absence, call reachability, active/dead-code status, or semantic equivalence. SEM-MIRROR-030 is the purchase-receipt comparison linked to FIN-018; SEM-MIRROR-031 covers cancelled-order Call Center wording and SEM-MIRROR-032 compares WPF/WinUI electronic payment operations. The 32 semantic groups do not exhaust all 243 lexical leads. The practical remaining work is a prioritized review of concrete call chains, with generated and DTO-only repetition classified separately. Phase 11 therefore remains incomplete/non-exhaustive; no mathematical completeness claim is made.

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


## SEM-MIRROR-017 — standalone settlement versus frozen period payroll

The employee roster exposes a standalone payout from the current positive ledger balance, and Payroll exposes a separate period-run payout. PayEmployeeHandler posts a debit of type Settlement and a treasury movement after checking the current balance. CreatePayrollHandler includes period ledger rows in payroll input; PayrollRules.CalculatePayrollLines sums earnings but subtracts only Deduction and Adjustment, so Settlement is not netted. If a current-period daily wage is paid through the standalone route, the payroll line can still retain that full earning. PayPayrollHandler later pays the frozen net and posts another treasury movement without rechecking the employee balance.

The same-obligation double-payout path is source-supported when both exposed modes are used. The inspected code and project notes do not establish mutual exclusion. The tests cover the two routes separately and do not exercise the combined sequence. Record this as the HIGH-impact candidate FIN-CAND-EMPLOYEE-PAYROLL-OVERLAP-001; policy and database-backed runtime confirmation are pending.

[Exact source hashes, callable identities and test gaps](Database-and-Finance/standalone-payroll-obligation-overlap-quality-review.json); [source comparison](Duplication-and-Architecture/payroll-settlement-overlap-semantic-comparison.json). No test or database operation was run.

Updated bounded semantic evidence totals: 17 groups, 92 distinct source paths, 480 referenced callable contexts. The mechanical identity gate passes; it does not certify semantic correctness or runtime behavior. The 218 identified AST candidate inventory remains accounted, and repository-wide renamed, reflection and reachability completeness remains qualified.


## SEM-MIRROR-018 — current treasury balance projections

The owner dashboard and daily statement each independently load all Posted treasury movements and compute each account balance as destination credits minus source debits. The expressions, status filter, and five account IDs match. The daily statement’s separate movement detail is intentionally filtered to the selected business day; its TreasuryNow is all-time, matching the dashboard. No divergence or current defect was found in these two implementations.

The existing dashboard test asserts that the balance is independent of the selected report period. The daily-statement test asserts that old movements are omitted from the day-scoped movement list. Neither test compares the two endpoints’ current-balance DTOs across transfers and non-posted entries. Reading the tests was static; they were not executed.

This is a bounded manual comparison, not an additional callable-review receipt. The copied ledger aggregation can drift if only one query is changed; consider sharing the projection and adding a cross-endpoint integration assertion. The comparison is source-bound to baseline blobs and records exact worktree hashes in [the evidence artifact](Duplication-and-Architecture/treasury-current-balance-semantic-comparison.json).


## SEM-MIRROR-019 — owner dashboard stock balance omits movement direction

The owner dashboard’s `LoadStockAsync` groups stock movements and sums the positive `QuantityMilliUnits` field without reading `Direction`. The inventory overview and domain `StockBalanceMilliUnits` both add `In` and subtract `Out`. The inspected writers persist positive quantities separately from direction: waste and order usage are `Out`, purchases are `In`, and count adjustments preserve the supplied direction. Therefore the dashboard reports an overstated stock balance after outbound movements and can suppress low-stock alerts. For example, receipt 5,000 plus consumption 4,000 is a true balance of 1,000 but the dashboard raw sum is 9,000.

This is a source-supported MEDIUM operational/inventory-integrity finding, independently confirmed by Financial Integrity (static source challenge). The existing inventory vertical-slice tests cover derived balance and low-stock behavior, but the owner-dashboard tests inspected do not assert that its stock projection agrees with inventory. No tests or runtime/database actions were run.

Recommended: reuse the direction-aware balance projection and add dashboard-versus-inventory regression coverage for receipt, consumption, and inbound/outbound count adjustments. Exact source hashes, baseline blob identities, symbols, and evidence are in [the comparison artifact](Duplication-and-Architecture/inventory-dashboard-balance-semantic-comparison.json).

## SEM-MIRROR-020 — dashboard net-sales headline and chart series

The owner dashboard obtains its current sales headline through the reporting query and `SalesRules.ComputeSalesReport`. The headline excludes cancelled orders, uses the selected business-date UTC interval, and calculates net sales as order total less its persisted refund amount. The hourly and daily series use that same UTC range and cancelled-order predicate, then apply the same per-order net formula. They group by Cairo wall-clock hour for one day or Cairo business date for a span; daily closed dates and empty hours are zero-filled. These are deliberate display-granularity differences, not a second net-sales formula.

Existing owner-dashboard integration tests assert that each series sums to the headline and that a single-day series has 24 hourly buckets. Those assertions were read, not executed. A canceled/partially refunded series case is not present in the inspected tests. Separate database reads leave an unverified response-level snapshot-consistency question if an order changes during dashboard assembly; no runtime race or formula defect was established.

Exact source identities and bounded reasoning are recorded in [the comparison artifact](Duplication-and-Architecture/reporting-net-series-semantic-comparison.json). This adds a manual comparison, not a function-review or runtime-test receipt.


## SEM-MIRROR-021 — reopened business day omitted from dashboard attention

The lifecycle marks `Open`, `Closing`, and `Reopened` as active. The business-day gate and reopen handler preserve that three-state contract, and reopening persists `Reopened` while resetting the close checklist. The owner dashboard instead filters for `Open` when it derives the open-day age and when it selects a day for close-blocker evaluation. A reopened day therefore appears absent from `HasOpenBusinessDay`, has no age, and gets no dashboard blocker evaluation, although the domain/gate considers it active and the reopened checklist still needs closing. This is a source-supported projection divergence; the endpoint behavior after reopen remains untested.

The business-day integration test proves the closed-to-reopened transition. The dashboard test covers only an ordinary open day and does not assert reopened-state indicators. No API, database, close workflow, or UI action was run. An independent Financial Integrity source challenge confirmed the limited mismatch and identified DTO wording plus Control Center access as counterevidence. Keep the issue as a low-severity candidate pending confirmation of the intended owner-facing meaning.

[Exact source hashes, symbols, ranges and bounded reasoning](Duplication-and-Architecture/reopened-business-day-dashboard-semantic-comparison.json).


## SEM-MIRROR-022 — dashboard exception count versus derived Control Center candidates

Control Center recomputes exception candidates from current business facts and overlays persisted review state, defaulting an unseen candidate to lowercase `open`. The state writer accepts only lowercase `acknowledged` or `resolved`, so a new open exception has no persisted state row. The owner dashboard instead counts persisted rows whose status equals capitalized `Open` and does not derive candidates. This means its `OpenExceptionCount` is not the open-candidate population and can remain zero while Control Center reports current open issues. The integration test demonstrates the default lowercase-open read and subsequent acknowledgment, but no test compares that candidate with the dashboard count. An independent Financial Integrity source challenge confirms the server-side mismatch. The coordinator also inspected `OwnerDashboardViewModel.RebuildAttention`: the cashier UI adds the Control Center attention card only when this count is greater than zero. Direct Control Center access remains counterevidence; no endpoint, database, or native UI execution was performed.

[Exact source identities and reasoning](Duplication-and-Architecture/exception-count-semantic-comparison.json).


## SEM-MIRROR-023 — WPF/WinUI cashier money-entry boundaries

**Status:** source-confirmed frontend divergence; independently corroborated LOW input-correctness risk; runtime not executed. Evidence: [`wpf-vs-winui-cash-boundary-semantic-comparison.json`](Duplication-and-Architecture/wpf-vs-winui-cash-boundary-semantic-comparison.json).

The current WinUI cashier routes shift opening and refunds through `CashInput.TryParse`, which normalizes Arabic/Persian input and enforces two-decimal precision and the Int64 piastre ceiling. The WPF reference parses and rounds directly in its shift-opening, refund, and checkout commands; its editable opening/refund fields have no corresponding local precision/range gate. `Money.FromEgp` explicitly rounds midpoint values away from zero, unlike the WPF default `Math.Round` midpoint rule. Existing `CashierCorrectnessTests` cover the strict helper, not these WPF command paths. A bounded independent Financial Integrity challenge confirmed that `10.005` EGP rounds to `1000` piastres in WPF (`ToEven`) versus `1001` under `Money.FromEgp` (`AwayFromZero`). Server guards reject zero refunds and cap accepted refunds to the remaining amount/account balance, bounding impact; they cannot restore the entered precision policy. WPF deployment and the intended policy for third-decimal inputs remain unverified.

Recommendation: if WPF remains deployable, align its boundary validation and add WPF-specific midpoint/overflow/server-validation tests; otherwise exclude the legacy executable from deployment. No source change or test execution was made. Independent evidence: [`financial-cashier-money-input-precision-independent-challenge.json`](../Sub-Agents/Handoffs/financial-cashier-money-input-precision-independent-challenge.json).


## SEM-MIRROR-024 — duplicate WinUI shift-close entry points

**Status:** source-confirmed recovery-behavior divergence; cross-referenced to existing [UI-016](MASTER_FINDINGS.json); no new duplicate finding ID. Runtime/tests not executed. Evidence: [`dual-shift-close-recovery-semantic-comparison.json`](Duplication-and-Architecture/dual-shift-close-recovery-semantic-comparison.json).

Both the POS-embedded and Shifts-screen close flows use the same blind count, server preview, conditional manager approval, and server close endpoint. The embedded flow retains a stable request ID and unresolved request, blocks cancellation, and retries the same payload. The Shifts-screen flow creates a new ID per confirmation and does not retain an unknown result. The server replays the same ID; it locks and checks shift status for a new ID, preventing duplicate close movements. The resulting risk is loss of clear recovery/confirmation at the Shifts screen, not a demonstrated duplicate cash transfer. Existing server tests cover same-key replay/concurrency and new-key rejection.

Recommendation: consolidate both presentations onto one retry-safe close workflow or give the Shifts screen equivalent pending-request handling; add a lost-response client test for both entry points.


## SEM-MIRROR-025 — WinUI cash-count preview and server calculation

**Status:** no separate mirror defect; cross-references existing [`FIN-CAND-CASHCOUNT-001`](Database-and-Finance/DATABASE_AND_FINANCIAL_INTEGRITY.md). Static source/test comparison only; tests and runtime not executed. Evidence: [`cash-count-client-server-semantic-comparison.json`](Duplication-and-Architecture/cash-count-client-server-semantic-comparison.json).

For normal valid denomination rows and in-range amounts, the WinUI running preview and server domain both multiply quantity by denomination and sum row totals. Invalid/nonpositive UI quantity text and negative server quantities both normalize to zero. The preview is explicitly informational; the server response is authoritative. The server’s normal denomination endpoint returns the fixed allowed list, but the POST handler accepts submitted denomination values without checking that list, while the shared calculation also has unchecked product and difference arithmetic. That preserves the existing candidate’s scope; it does not create evidence of a second mirror defect. Existing integration tests cover normal totals, same-ID replay, permission enforcement, and the fixed GET list, but not crafted denominations or overflow boundaries.

Recommendation: validate allowed denominations and bounded arithmetic server-side, then add isolated handler/API tests for invalid and boundary counts. No source change or test execution was made.


## SEM-MIRROR-026 — shift-close unsettled-custody warning scope

**Status:** source-confirmed scope divergence; LOW, requirements-dependent presentation risk; not a confirmed accounting defect. Runtime/database/printing tests were not executed. Evidence: [shift-close custody comparison](Duplication-and-Architecture/shift-close-custody-warning-scope-semantic-comparison.json).

The shift-close print job composes cash breakdown and paid-from-drawer expenses for the identified shift, and its pending-electronic/unpaid-order warnings are also shift-filtered. The unsettled-driver-custody warning instead sums every open custody row without joining its DeliveryId to the delivery ShiftId. The domain helper filters custody through the delivery shift, while business-day close intentionally treats unsettled custody globally. Existing shift-close comments require a warning on the handover slip but do not specify whether the warning should be shift-only or restaurant-wide; existing integration coverage has only same-shift custody.

If owners expect shift-only attribution, the global warning can show unrelated driver custody in a shift-specific handover context. It does not change the stored cash count, difference, or treasury movements. Obtain an owner decision: show only shift-linked custody, show a clearly labeled global total, or show both. Add a two-shift test for that choice while retaining the global business-day blocker. No canonical defect finding is asserted before the requirement is settled.


## Current evidence reconciliation — 2026-10-21 04:23 UTC

The saved semantic registry contains 28 bounded semantic comparison groups, 27 artifact inputs, 204 source-reference records, 134 distinct source paths, 595 stable callable-context identifiers and 329 checked line ranges. The source identity gate passed; the report-link check found 80 links and no broken links. The 218 identified AST candidate groups have dispositions (102 generated-history repetitions accounted separately, 116 context/manual dispositions); no candidate application or test group remains pending. These figures describe the identified inventory and saved bounded comparisons, not exhaustive semantic discovery or runtime testing.

`SEM-MIRROR-027` compares `OpenBusinessDayHandler`/`BusinessDayRules.Open` with the guarded `OpenShiftHandler` admission path. The existing business-day source checks and persists without the active-day uniqueness/serialization mechanism seen on the shift path. Independent source challenge supports a possible distinct-request concurrent dual-open outcome; no SQL Server interleaving was executed, and deployment-wide serialization is unverified. Preserve this as a candidate, not a reproduced production defect. The bounded search found lexical leads across 228 source files outside prior semantic artifact inputs; those are unreviewed leads, not completed comparisons. The search uses chosen financial, date/shift, retry/idempotency and authorization terms, and cannot rule out renamed or structurally different mirrors.

**Completion decision: `INCOMPLETE — SPECIFIC SOURCE/DOCUMENTATION GAPS REMAIN`.** The finite candidate queue is reconciled, but Phase 11 repository-wide semantic/dead-code/reachability scope is not mathematically exhaustive. No refactor is implemented or recommended solely to reduce repetition; generated migration/model repetition remains classified separately. Runtime flows, SQL concurrency, printer behavior and dead-code execution were not tested.

### Finance cross-check of ShiftClose print and custody mirrors

The print-query/reconciliation comparison found no money-moving behavior in the residual printing subset. Close-slip headline figures are persisted while detailed components are recomputed from current rows; whether this is a stale explanatory rendering depends on post-close correction/reopen policy. The warning scope diverges from `DeliveryRules.UnsettledCustodyForShift` (global open custody in the query versus shift-filtered domain helper). The shift-close comments and global business-day custody model provide counterevidence that global context may be intentional; owner clarification and a two-shift test are required. Preserve both observations as conditional/requirements questions, not confirmed accounting defects. See `Database-and-Finance/financial-residual-213-print-semantics-coordinator-challenge.json`.


## SEM-MIRROR-030 — purchase receipt validation: WinUI versus authoritative server

**Status:** source-confirmed client/server validation divergence, cross-referenced to existing [`FIN-018`](MASTER_FINDINGS.json); no duplicate finding ID. No API, database, or test execution. Evidence: [purchase receipt comparison](Duplication-and-Architecture/purchase-receipt-client-server-validation-semantic-comparison.json).

WinUI `PurchasingViewModel.SubmitReceiveAsync` rejects nonpositive quantity and unit cost for every line (source lines 438–467). The server `ReceivePurchaseHandler.HandleAsync` accepts the request DTO, computes signed line totals, checks only an empty line list and aggregate payment bounds, then persists purchase, supplier ledger and stock movement facts in one transaction (lines 36–75, 121–175). `PurchasingRules.AssertPurchasePayment` checks the paid amount against the total; it does not validate individual line signs. The DTO is a passive record. Thus the client form's guard is not an API trust-boundary invariant.

The finding already captured as FIN-018 describes the material combination: negative quantity and negative unit cost can multiply to a positive invoice line, while the negative quantity is persisted as an `In` stock movement and direction-aware stock calculation subtracts it. This comparison contributes client/server mirror evidence only; it does not increase the financial callable-review count and does not claim a live exploit or database mutation was performed. Normal WinUI interaction blocks these values, but direct or alternate clients reach the server boundary.

**Future repair guidance:** apply a server/domain per-line invariant before totals, journal construction or transaction posting: positive receipt quantity and positive unit cost if this command means a purchase receipt. If purchase returns are supported, model them as an explicit authorized return/credit operation with corresponding stock-out and supplier-credit facts. Required acceptance includes no persisted purchase/ledger/stock/treasury/journal facts for invalid lines, while valid receipt and credit workflows remain unchanged. Add direct API/handler regression cases for negative quantity, negative unit cost, both negative, and a return if supported. Atomic rollback is useful counterevidence against partial posting but does not make a signed line valid. See FIN-018 for full root cause, impact, counterevidence and tests.

## SEM-MIRROR-028 — cash lifecycle command family

A new bounded manual comparison covers `SafeDropHandler`, `CashierCashMovementHandler`, `TransferHandler`, and `WithdrawalHandler`, plus the common balance-lock query, movement factory, durable financial journal, API routes, and relevant integration-test source. The full hashes, stable callable IDs, ranges, behavior matrix, limitations, owner question, and regression plan are in [the comparison evidence](Duplication-and-Architecture/cash-lifecycle-command-family-semantic-comparison.json). These handler files were outside the prior semantic artifact inputs.

The four handlers share the same protected-balance/transaction/journal/movement/audit/replay protocol while intentionally applying distinct permissions, account directions, shift rules and side effects. The Financial Integrity specialist independently challenged this comparison and found a source-supported consequence of SafeDrop shift attribution: a SafeDrop can reference an existing closed shift while the current business day remains open; `ShiftReconciliation` later includes it in that historical shift, and reprint recomputes breakdown components while retaining persisted close headline values. The result is a **requirements-dependent reconciliation attribution risk**, not a confirmed accounting defect or demonstrated cash loss. Source comments say existence-only SafeDrop linkage intentionally preserves reference-app behavior, so owner policy is unresolved. Transfer alone creates a CashHandover in the same transaction; owner withdrawal only permits external-capital source accounts and has no destination. Any extraction of shared posting boilerplate must preserve these boundaries.

Existing integration-test source asserts cashier directions and shift gate, SafeDrop drawer/main movement, same-request replay, different-request overdraw protection for treasury withdrawal/transfer, and a single transfer handover. These tests were inspected but not run in this comparison. No existing cited test covers SafeDrop against a closed shift, its later reprinted breakdown, or cross-command concurrent distinct-ID spending from the same source account. Those are proposed owner-policy and SQL Server regression tests, not reproduced failures.

## SEM-MIRROR-029 — WPF and WinUI ambiguous refund retry

**Status:** conditional LOW client recovery divergence, corroborated by an independent static Financial Integrity challenge. It does not establish duplicate payout, over-refund, or cash loss. Evidence: [comparison artifact](Duplication-and-Architecture/wpf-winui-refund-ambiguous-retry-semantic-comparison.json) and [independent challenge](../Sub-Agents/Handoffs/financial-wpf-winui-refund-ambiguous-retry-independent-challenge-2026-10-21.json).

The WPF submit path creates a fresh refund request key for each submit and does not retain an explicit unknown-outcome command; transport exceptions are not mapped to the WinUI-style `OutcomeUnknown` result. WinUI retains the request key and retries the same request while the ViewModel remains alive, then rotates after success. That pending request is in memory, so restart durability is not established. The server replays identical keys, locks the order, caps refunds to the remaining refundable amount, checks the protected source balance, and commits refund/movement/audit facts together. A manual WPF resubmit after a committed-but-unseen partial refund is a distinct intentional refund command, subject to server caps and authorization; no automatic retry was identified.

Recommendation depends on whether WPF remains a supported deployed cashier. If supported, retain/reconcile the original operation ID after an ambiguous result; if retired, document that deployment decision. Decide whether WinUI recovery must survive process restart. Tests should fault the client response after server acceptance, verify same-ID replay/reconciliation and allow a separate intentional partial refund. No transport/database tests were run.


## SEM-MIRROR-031 — cancelled electronic order lookup payment label

**Status:** source-confirmed presentation inconsistency candidate, low severity; no ledger or collection defect established. Evidence: [cross-layer comparison](Duplication-and-Architecture/cancelled-electronic-order-callcenter-payment-label-semantic-comparison.json). Independent financial challenge is pending.

The cancellation domain transition and handler persist `Status=cancelled`, `IsPaid=false` and `ElectronicPaymentStatus=cancelled`; the saved integration-test source asserts cancellation and zero treasury movements (test source read only, not run). The order-number lookup returns the DTO without filtering cancelled orders and the mapper preserves all three state fields. In Call Center `LookedUpPaymentLine`, only `pending` and `IsPaid` are special-cased; every remaining unpaid state is labelled “تحصيل عند التسليم”. The lookup card separately shows status through a converter that maps `cancelled` to “ملغي”. Thus an operator sees both “ملغي” and a collection-on-delivery message for the same order. The visible cancelled status is material counterevidence and reduces the likely consequence; no real customer action or cash movement was observed.

Recommended future correction: make the payment projection inspect order status before falling back to COD, and suppress collection language for cancelled orders while preserving pending, paid and active COD states. Add projection/view-model regression cases for the cancelled tuple plus those controls. This remains a candidate pending specialist challenge and UI verification; do not treat it as a proven financial loss.

## Latest bounded semantic reconciliation — 2026-10-21 09:55 UTC

The registry now records 31 bounded comparisons / 30 artifacts, 227 source-reference records, 145 distinct paths, 616 stable callable contexts and 363 line ranges. The latest keyword lead manifest contains 246 keyword-hit paths not referenced by those artifacts. It incorporates five paths newly covered by SEM-MIRROR-031; this lexical count is a lead queue, not a semantic comparison count. The finite AST queue remains 218/218 dispositioned. Phase 11 remains qualified and incomplete for renamed semantic equivalence, dead-code/reachability and reflection/generated behavior. No tests or runtime checks were executed.

## Bounded semantic update — SEM-MIRROR-033

[Purchase due-date lifecycle comparison](Duplication-and-Architecture/purchase-due-date-lifecycle-semantic-comparison.json) traces receipt-time nullable due date and later due-date edits through shared WinUI parser, typed request, API, route, and authorized/audited handler. The one parser is reused, the API route ID is authoritative, and the later edit changes only the due-date field with audit metadata; no duplicated settlement or posting logic was identified. This is one targeted lead disposition, not coverage of the remaining semantic search leads. No runtime/API/database tests were run.


Current lexical manifest: [remaining semantic search leads](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-0732.json), 243 keyword-hit paths lack a semantic artifact reference. These remain leads, not confirmed mirrors.


## Shift-close retry comparison — SEM-MIRROR-034

[Source comparison](Duplication-and-Architecture/shift-close-uncertain-retry-semantic-comparison.json) finds that the POS-embedded cashier retains and retries the exact close request after OutcomeUnknown, while the dedicated Shifts screen generates a new ID and does not retain it. The server locks the shift row, replays only the stored close key, and rejects a closed shift before posting movements, so source does not establish duplicate reconciliation/handover. The difference may leave an ambiguous stale UI after a committed response is lost. This remains a conditional recovery concern pending isolated UI/API tests; no physical or runtime test ran. No canonical financial defect ID was added.

Current bounded lexical lead manifest: [240 unreferenced keyword-hit paths](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-0735.json).


Updated [semantic lead manifest](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-0738.json): 240 keyword-hit paths lack semantic evidence references. This is a bounded search queue only.



## SEM-MIRROR-035 — expense entry and server authority boundary

**Status:** bounded client/server contract comparison; no duplicate money-moving client implementation found. No runtime/database execution. Evidence: [expense entry comparison](Duplication-and-Architecture/expense-entry-client-server-boundary-semantic-comparison.json).

Only the Cashier WinUI view-model constructs and submits an expense request in the tracked-source call-site search. The Call Center API has a typed `AddExpenseAsync` wrapper with the same endpoint, but no caller was found; the legacy WPF cashier has no corresponding request submission. This wrapper repetition is transport boilerplate, not a second implementation of expense accounting.

The active UI fixes the payment account to the drawer, requires a positive parsed amount and description, asks for manager approval when the session lacks drawer-expense permission, and retains the same request after an unknown outcome. The server independently validates amount, account, description, category, open shift/business day and source balance, derives the actor from the persisted user row, and applies `AuthorizeExpense`; expense, treasury movement, audit event, journal completion and voucher print-job row are saved in one transaction. The UI narrows the supported flow to drawer payments; the server supports other protected accounts under its authorization policy for a future management flow. No amount/accounting divergence was found in this bounded trace. API/provider locking and actor trust-boundary questions remain outside this mirror conclusion and no DB tests ran.

Recommendation: keep accounting centralized in the handler. Any future management client should preserve the server's authorization/balance invariants and get direct API tests. Suggested tests and owner decisions are in the evidence artifact.

**Updated bounded search snapshot:** [remaining semantic leads](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-continued.json). Its lexical hits are only leads, not evidence that a duplicate exists or that unreferenced files were substantively compared.


## SEM-MIRROR-036 — payroll run read projection parity

**Status:** bounded persistence/projection comparison; no accounting divergence found. Evidence: [payroll projection comparison](Duplication-and-Architecture/payroll-run-read-projection-parity-semantic-comparison.json).

`PayrollQueryService.ListRunsAsync` maps the same persisted fields as `CreatePayrollHandler.ToDto`; `GetRunAsync` uses that shared mapper, with `WasReplayed=false` on reads. `ListLinesAsync` reads the stored frozen payroll line values and payment status rather than recalculating from current employee ledgers. `PayPayrollHandler` changes the persisted line/run settlement state which this projection reads. The only maintenance concern is the separate list projection can omit future DTO fields; no current defect is identified. Existing tests exercise payroll creation/payment/replay and treasury posting, but the inspected test spans do not assert read-projection parity.

Recommendation: preserve frozen historical amounts and add DTO parity/read projection tests (or share mapping) without recalculating payroll on reads. Runtime and provider behavior were not tested.

**Latest bounded lexical lead snapshot:** [remaining leads](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-continued.json), refreshed after SEM-MIRROR-036.


## SEM-MIRROR-037 — cash-drawer command/client boundary

**Status:** bounded client/server workflow comparison; no duplicate financial implementation. Evidence: [cash-drawer comparison](Duplication-and-Architecture/cash-drawer-command-client-server-semantic-comparison.json).

The Cashier WinUI flow builds the command, resolves approval, preserves the same request through an unknown API result, and rotates the key after success. The Call Center API has a matching thin wrapper but no usage was found; legacy WPF has no corresponding request. One server handler records a `Requested` command and an audit warning without touching treasury. A later result endpoint records physical success/failure; existing test source checks no treasury change and same-key replay. The comments explicitly distinguish a requested row from proof that the physical drawer opened. No physical device was activated.

The code describes cashier-machine-only usage as architecture but has no device identity in the request. This comparison does not establish whether the server must enforce that policy; owner/security clarification belongs to the paused security scope. Keep the command separate from money movements and preserve its retry behavior.

**Latest bounded search snapshot:** [remaining semantic leads](Duplication-and-Architecture/remaining-semantic-search-leads-20261021-continued.json); lexical-hit paths remain leads, not manual coverage.


## SEM-MIRROR-038 — normal menu recipes versus prep/batch recipes

**Disposition:** The replace-line-set handlers share a useful command shape, while the recipe models and their validation/costing effects are intentionally different. No new defect is recorded. The existing `FIN-CAND-PREPGRAPH-001` concurrency candidate remains conditional on provider-level behavior. Evidence: [normal/prep recipe comparison](Duplication-and-Architecture/normal-vs-prep-recipe-semantic-comparison.json).

`SaveRecipeHandler.HandleAsync` binds a menu product, stores line kind/yield semantics, blocks an empty active recipe, and synchronizes `Product.RecipeId`; the sold-item cost path consumes that link. `SavePrepRecipeHandler.HandleAsync` creates an inventory output with a positive batch yield and ingredient quantities, then validates that the current active prep graph has no path back to the output. `CostingRules` separately guards recursive cost resolution and reports an unknown cost when it sees a cycle. These are connected costing responsibilities, not duplicate recipe semantics.

The integration-test source covers direct and sequential multi-batch cycles, shared-ingredient acceptance, and an empty active menu recipe. The graph precheck occurs before the DB transaction, so `FIN-CAND-PREPGRAPH-001` still requires a barrier-controlled concurrent SQL Server test; static evidence alone does not establish that opposing writes can both commit. Client search found the normal recipe submission but no in-repository prep-recipe write caller; external API clients and deployment tooling were not assessed. Preserve the model-specific handlers.


## SEM-MIRROR-039 — item contribution versus aggregate dashboard profitability

**Disposition:** These calculations are complementary, not competing copies. Per-item profitability withholds its margin when any recipe cost is incomplete; the aggregate dashboard can show a bounded partial total while carrying `IsFoodCostComplete=false` and a visible warning. No new finding was created. This comparison cross-checks [FIN-CAND-PACKAGING-COVERAGE-001](MASTER_FINDINGS.json), which remains open for the separate unknown order-packaging completeness input. Evidence: [item/dashboard comparison](Duplication-and-Architecture/item-vs-dashboard-profitability-semantic-comparison.json).

`InventoryQueryService` derives item completeness from recipe line counts and passes it to `CostingRules.MenuItemProfitability`. The owner dashboard instead aggregates known recipe costs and passes sold/known line counts to `ProfitabilityRules`; its DTO carries incompleteness and missing names. The cashier dashboard displays the priced/sold count, warning text that profit is overstated, and an incomplete-cost banner. Tests assert the partial aggregate is not complete, while zero known costs suppress profit. The unit-level and aggregate functions have different contracts and should remain distinct.

The existing packaging candidate concerns a missing active order-level packaging cost that is appended to missing-cost output without affecting sold/known line counts used for the dashboard completeness flag. Do not fold that distinct aggregation gap into a generic duplication finding. Native visual/runtime behavior and database-backed report execution were not run.


## SEM-MIRROR-040 — order-packaging completeness through the dashboard

**Disposition:** Source confirms the existing [FIN-CAND-PACKAGING-COVERAGE-001](MASTER_FINDINGS.json); no duplicate finding was created. An active sold-order packaging rule with unresolved ingredient cost is added to the food-cost report's missing list, but it does not alter the dish sold/known line counters. The owner dashboard derives `IsFoodCostComplete` only from those counters, so all-costed dish recipes can leave that flag true even when packaging is missing. The Cashier dashboard's attention warning and banner are gated by the false flag, so the packaging item name alone does not activate them. Evidence: [cross-layer packaging comparison](Duplication-and-Architecture/order-packaging-completeness-cross-layer-semantic-comparison.json).

`OrderPackagingCost` distinguishes absent/inactive optional rules (known zero under current code policy) from active rules whose item cost is unknown. The reporting path correctly counts packaging once per sold order type; the gap lies in propagating its `Known=false` state into aggregate completeness. This can overstate reported contribution/profit and suppress a warning, without changing a ledger or proving a cash loss. Existing tests cover rule persistence and line-cost completeness but no known-dish/unknown-active-packaging aggregate case was found. Endpoint, SQL, test-runner, and native UI tests were not executed.


### SEM-MIRROR-041 — estimated inventory cost vs actual purchase and prep cost

**Disposition:** separate command paths, no duplicate implementation or confirmed defect. Create/update maintain `CostSource`; the estimate handler stores an audited estimate without stock, supplier, or treasury postings; costing uses a prep recipe exclusively for `Prep`, otherwise latest actual purchase before a strictly positive estimate. The existing integration-test source covers a positive non-prep estimate, but no test was run.

Two semantics need an owner decision before implementation: the setter accepts zero but costing treats zero as unknown, and it accepts estimates on `Prep` items although Prep resolution ignores them. These may be intentional and are not recorded as defects. A bounded search of the named active client trees found no estimated-cost API call; that does not establish dead code.

Evidence: [comparison artifact](Duplication-and-Architecture/inventory-estimated-vs-actual-cost-source-semantic-comparison.json). Runtime/API/database/UI tests not executed.


### SEM-MIRROR-042 — blind shift-close preview and committed close

**Disposition:** the prepare and close server commands share the expected-cash calculation and blind-close domain rule. Prepare is read-only and advisory; commit recalculates under a read-committed transaction and shift-row update/hold lock, rechecks approval, and commits close state, treasury postings, audit event and print job together. A preview can become stale, but the client preview is not trusted as the committed expected value. This comparison does not establish that every money-movement writer observes the same shift lock. The existing SEM-MIRROR-034 separately documents differing uncertain-response retry behavior between the two UI entry points.

Evidence: [comparison artifact](Duplication-and-Architecture/shift-close-preview-vs-commit-semantic-comparison.json). Runtime and lock interleaving tests not executed.


### SEM-MIRROR-043 — product and category report projections

**Disposition:** four related report methods intentionally provide different projections over a shared order-line/date/status cohort. TopSelling, SoldProducts and ProductHourly aggregate by frozen `ProductName`; CategorySales instead joins current product/category metadata and aggregates by current `CategoryId`. The daily statement passes the same business date as both bounds to SoldProducts and ProductHourly. No new canonical defect is established. The shared `Cancelled` versus persisted `cancelled` comparison remains conditional on SQL Server collation; no database collation or query was inspected. Existing tests were read but not run.

CategorySales can present historical lines under a changed current category or omit them if the current product row is absent. Whether historical reports should freeze sale-time category remains a product requirement. The Cashier WinUI `ReportsViewModel.LoadAsync` fetches category rows into `CategorySales`, while a bounded literal search of `ReportsView.xaml` found no binding; visible product intent remains unverified and is left to the paused UI review. Preserve separate projections; do not consolidate solely to reduce duplication. If collation-independent cancellation exclusion is required, centralize the wire-state comparison and cover every endpoint under relevant collations. If sale-time category is required, define a snapshot/data-model decision separately. Evidence: [report family comparison](Duplication-and-Architecture/report-product-aggregation-semantic-comparison.json).

### SEM-MIRROR-044 — Cashier and Call Center delivery area/service fields

**Disposition:** In the current source baseline, the two WinUI clients preserve the delivery area separately from the selected service ID/name, and the server validates and persists those concepts separately. The Call Center customer-view comment claiming current Cashier WinUI sends the service name as its area contradicts the actual inspected input binding and checkout request construction; it appears stale or historical. No behavior defect was established in this path, and the comment itself is a source-documentation cleanup candidate for a future authorized code change.

Cashier WinUI binds a dedicated area text box and service selector, saves the typed/saved `DeliveryArea` separately, and constructs checkout fields from `DeliveryArea` and `SelectedDeliveryService` independently. Call Center follows the same request contract. `CheckoutOrderHandler` checks area and service ID as separate required delivery fields, composes the address from area/street/address parts, and persists the service name separately. No UI, API, or database execution was performed.

Evidence: [field-contract comparison](Duplication-and-Architecture/cashier-callcenter-delivery-area-semantic-comparison.json). This is a bounded current-source comparison, not a complete delivery workflow audit; paused delivery/UI work remains pending.


## SEM-MIRROR-045 — catalog variant price validation and checkout

[Source comparison](Duplication-and-Architecture/negative-variant-price-semantic-comparison.json) compares the WinUI variant editor guard, create/update server handlers, persistence shape, checkout price resolution, signed money calculation and inventory orchestration. The client rejects nonpositive variant prices; the server handlers and inspected initial schema do not enforce the same rule. A stored negative variant can remain the authoritative list price without a valid price-rule override. Checkout clamps a negative taxable base/payable to zero while still persisting the order and deducting stock. This is a conditional, privileged configuration candidate, not proof of unauthorized access or a live financial loss. It is separately recorded as `FIN-CAND-NEGATIVE-VARIANT-PRICE-001`; it is not merged with FIN-016 because the entry point and actor capability differ. Owner policy for zero prices and independent financial review remain open. No runtime test or DB action was performed.


## SEM-MIRROR-046 — dormant order-value helper versus checkout calculation

[Source comparison](Duplication-and-Architecture/order-value-breakdown-vs-checkout-semantic-comparison.json) found no exact-name call sites for `OrderValueBreakdown` outside its declaration in current `src` and `tests`. Its value split covers line list/charged/comp values only; the active `CheckoutRules.BuildPlan` additionally applies discount, tax, delivery fee and payment/deferred-state rules, and `CheckoutOrderHandler` persists the resulting plan. The helper is therefore not an active competing total today. Treating it as a full checkout calculation in future work would omit material amounts/states. This is a bounded unreferenced-helper/maintenance observation, not a current financial defect or proof of binary-level dead code. No code changed and no tests ran.


## SEM-MIRROR-047 — Checkout recovery across Call Center and Cashier

The current Call Center persists its immutable checkout DTO before network dispatch under an operator-scoped key, restores it when constructing the view model, and offers the same request after an unknown outcome. The Cashier WinUI app configures the same retained-request store for other operations, but its checkout path holds `CurrentCheckoutRequestId` and `_pendingCheckout` only in the view model. Its same-process retry is sound; the inspected checkout path does not restore the in-flight request after process reconstruction. The server's replay lookup is keyed by `CheckoutRequestId`, so losing that client key can prevent a restarted cashier from reconciling the original order.

This is a conditional recovery/duplicate-intent risk, not evidence that two orders have been created. The original finding UI-001 already covers the cashier restart-recovery gap, so this comparison creates no duplicate canonical finding. Proposed future correction: persist the typed immutable request before dispatch and gate new checkout until the original outcome is reconciled. Validate with a fresh-view-model test for a committed-but-lost response and assert one order and one set of side effects. Source evidence: [comparison artifact](Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json). Runtime verification was not performed.


## SEM-MIRROR-047 — Checkout recovery across Call Center and Cashier

The current Call Center persists its immutable checkout DTO before network dispatch under an operator-scoped key, restores it when constructing the view model, and offers the same request after an unknown outcome. The Cashier WinUI app configures the same retained-request store for other operations, but its checkout path holds `CurrentCheckoutRequestId` and `_pendingCheckout` only in the view model. Its same-process retry is sound; the inspected checkout path does not restore the in-flight request after process reconstruction. The server's replay lookup is keyed by `CheckoutRequestId`, so losing that client key can prevent a restarted cashier from reconciling the original order.

This is a conditional recovery/duplicate-intent risk, not evidence that two orders have been created. The original finding UI-001 already covers the cashier restart-recovery gap, so this comparison creates no duplicate canonical finding. Proposed future correction: persist the typed immutable request before dispatch and gate new checkout until the original outcome is reconciled. Validate with a fresh-view-model test for a committed-but-lost response and assert one order and one set of side effects. Source evidence: [comparison artifact](Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json). Runtime verification was not performed.


## SEM-MIRROR-047 — Checkout recovery across Call Center and Cashier

The current Call Center persists its immutable checkout DTO before network dispatch under an operator-scoped key, restores it when constructing the view model, and offers the same request after an unknown outcome. The Cashier WinUI app configures the same retained-request store for other operations, but its checkout path holds `CurrentCheckoutRequestId` and `_pendingCheckout` only in the view model. Its same-process retry is sound; the inspected checkout path does not restore the in-flight request after process reconstruction. The server's replay lookup is keyed by `CheckoutRequestId`, so losing that client key can prevent a restarted cashier from reconciling the original order.

This is a conditional recovery/duplicate-intent risk, not evidence that two orders have been created. The original finding UI-001 already covers the cashier restart-recovery gap, so this comparison creates no duplicate canonical finding. Proposed future correction: persist the typed immutable request before dispatch and gate new checkout until the original outcome is reconciled. Validate with a fresh-view-model test for a committed-but-lost response and assert one order and one set of side effects. Source evidence: [comparison artifact](Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json). Runtime verification was not performed.


## SEM-MIRROR-047 — Checkout recovery across Call Center and Cashier

The current Call Center persists its immutable checkout DTO before network dispatch under an operator-scoped key, restores it when constructing the view model, and offers the same request after an unknown outcome. The Cashier WinUI app configures the same retained-request store for other operations, but its checkout path holds `CurrentCheckoutRequestId` and `_pendingCheckout` only in the view model. Its same-process retry is sound; the inspected checkout path does not restore the in-flight request after process reconstruction. The server's replay lookup is keyed by `CheckoutRequestId`, so losing that client key can prevent a restarted cashier from reconciling the original order.

This is a conditional recovery/duplicate-intent risk, not evidence that two orders have been created. The original finding UI-001 already covers the cashier restart-recovery gap, so this comparison creates no duplicate canonical finding. Proposed future correction: persist the typed immutable request before dispatch and gate new checkout until the original outcome is reconciled. Validate with a fresh-view-model test for a committed-but-lost response and assert one order and one set of side effects. Source evidence: [comparison artifact](Duplication-and-Architecture/cashier-callcenter-checkout-recovery-semantic-comparison.json). Runtime verification was not performed.


## SEM-MIRROR-048 — Station-scoped retained request store implementations

The Call Center and Cashier WinUI `FileRetainedRequestStore` files are identical after replacing only the file-scoped namespace. The comparison covered key sanitization, JSON envelope, temp-write/replace, error handling, clear, load/type recovery, and listing. Each app initializes the same helper with a different required station name (`callcenter` versus `cashier`), so the duplicate implementations preserve intentional local-data partitioning and showed no behavioral divergence. No consolidation is needed for correctness; if later centralized, preserve the explicit station partition and the tested persistence contract. Source evidence: [comparison artifact](Duplication-and-Architecture/callcenter-cashier-retained-store-semantic-comparison.json). No tests or filesystem data were touched.


## SEM-MIRROR-049 — Pickup time validation shared by cashier and server

The cashier Pickup form converts a complete Cairo-local date/time to UTC, requires date and time together, validates phone/name, and invokes the shared `CheckoutRules.AssertRequestedPickupTime`. Checkout sends the value only for Pickup. `CheckoutOrderHandler` independently applies the same domain rule and identity requirements before persistence, then stores the timestamp only for Pickup. The inspected Call Center path formats a stored pickup time for lookup; its source comment says the station creates delivery orders only. Existing domain test source covers absent, stale, future, far-future and wrong-order-type times plus pickup identity. The policy is shared and rechecked server-side; no divergence or repair is indicated. Evidence: [full semantic comparison](Duplication-and-Architecture/cashier-pickup-time-client-server-validation-semantic-comparison.json). Tests/runtime were not executed.


## SEM-MIRROR-050 — Cashier shift-open uncertain-result retry

The two Cashier WinUI shift-opening routes share the same `OpenShiftRequest`, API client and server handler, but their uncertain-result behavior diverges. The POS-embedded route keeps the pending request and retries its original `RequestId`; the Shifts modal creates a fresh key on each confirm and leaves no retained request when `OutcomeUnknown` is returned. The API transport explicitly classifies connection/cancellation/JSON/IO failures as uncertain and asks callers to retry unchanged data.

On the server, `OpenShiftHandler` replays only when the exact request ID already exists. For a new key it checks for an active shift, authorizes the actor, and writes the shift plus any positive-float Main-to-Drawer movement and print job in one transaction. The unique active-shift constraint prevents the second key from opening a duplicate shift while the first is active, so this comparison does not establish duplicate money movement. It does establish that the modal cannot reconcile the original successful-but-unacknowledged result through the documented same-key replay path; an operator may see “shift already open” and need a reload/current-shift recovery.

Classify this as a conditional uncertain-outcome recovery inconsistency, with no confirmed accounting defect. Preserve the screen-specific shift name and treasury-balance suggestion behavior. A future repair should retain the immutable request after `OutcomeUnknown` or reconcile the active shift before allowing a new request ID. Validate with a lost-response ViewModel test and a disposable SQL Server test asserting exactly one shift, treasury movement and print job, plus a two-key concurrent-open test. Source-only; no runtime tests were executed. Evidence: [comparison artifact](Duplication-and-Architecture/cashier-shift-open-uncertain-retry-semantic-comparison.json).


## SEM-MIRROR-051 — Protected balance lock transaction boundary

`ReceivePurchaseHandler` checks the protected account balance through the shared `UPDLOCK, HOLDLOCK` helper before opening its explicit transaction; `PayPayrollHandler` and the compared `PayPurchaseHandler` and `AddExpenseHandler` controls start the transaction first and hold the lock while their treasury/accounting writes commit. The helper documentation says the caller must already be in a transaction because the range lock is released when that transaction ends. This source-confirmed caller-order difference cross-references existing `FIN-011`; it does not create a second finding or prove that an overspend occurred.

Future repair should move the initial-purchase balance assertion inside its existing atomic write transaction. Validate with a disposable SQL Server interleaving against another spend, plus existing idempotency/atomicity tests. The business-day admission window remains a separate question. No SQL/provider tests were executed. Evidence: [comparison artifact](Duplication-and-Architecture/initial-purchase-vs-payroll-balance-lock-transaction-boundary-semantic-comparison.json).


## SEM-MIRROR-052 — Pending split-payment report versus channel totals

`SalesRules.ComputeSalesReport` excludes an order's full `Total` from `CollectedSales` whenever its electronic payment remains Pending. `OrderPaymentQueries.PaymentMethodTotals` separately counts immediate cash/card split legs and waits only on the matching electronic leg; shift expected cash reuses that channel helper. Thus a cash/card plus pending electronic split can leave the report's collected aggregate below both the immediate channel total and drawer cash. This corroborates existing finding FIN-010; no duplicate finding was created. Existing tests cover pure pending electronic and pure COD/pending orders, but not mixed immediate/pending legs. Confirm the owner-facing meaning of `CollectedSales` for this mixed state before implementation. Evidence and proposed acceptance tests: [SEM-MIRROR-052 artifact](Duplication-and-Architecture/pending-split-collected-vs-channel-totals-semantic-comparison.json). No tests were run.


## SEM-MIRROR-053 — Pending electronic cancellation versus split cash reporting

The source first recognizes an immediate cash split leg while a sibling electronic leg is pending. The cancellation path then marks the whole order Cancelled and persists that state without reversing the already-posted cash movement; payment-channel and cash-refund projections both skip cancelled orders. This corroborates existing FIN-002 and creates no duplicate finding. Current tests separately cover pending mixed split accounting and pure electronic cancellation, but not their combination. The correct repair depends on the owner-approved accounting policy for a collected leg when the pending leg is cancelled. Evidence and acceptance tests: [SEM-MIRROR-053 artifact](Duplication-and-Architecture/pending-electronic-cancel-vs-split-cash-semantic-comparison.json). No tests/runtime were executed.


## SEM-MIRROR-054 — Checkout admission versus shift close

Checkout verifies the selected shift is Open before its transaction, then later begins posting without a shift-row lock/recheck in the inspected handler. `CloseShiftHandler` begins a transaction first, locks that shift row, computes expected cash, and commits the Closed status and handover. A sale request that passed the earlier read can therefore contend with close without shared admission serialization. This corroborates existing FIN-015; the precise SQL Server interleaving remains unexecuted. Future correction should serialize checkout admission with close while preserving idempotency and define whether the sale is included in close or rejected. Evidence and proposed schedules: [SEM-MIRROR-054 artifact](Duplication-and-Architecture/checkout-vs-shift-close-admission-semantic-comparison.json).


## SEM-MIRROR-055 — Treasury unknown-result recovery across client and server

The cashier treasury form creates a request ID when an action opens, builds one typed deposit/withdraw/transfer request, and retains its delegate after `OutcomeUnknown`. `AppSession` and its unresolved-operation registry are process-only; navigation is blocked only while that in-memory state exists. The server handlers derive durable replay keys from the request IDs and transactionally write movement/journal/audit (plus a linked handover for transfers). Existing integration tests cover same-ID server replay/concurrency, but do not reconstruct the client after a committed-but-unacknowledged request.

This supplements existing `UI-001`; it does not prove a duplicate transaction and creates no new canonical finding. Same-process retry and server replay are counterevidence to duplicate posting under the same identity. A future repair can persist the immutable request and ID before dispatch and reconcile it after relogin, or expose an operation-status lookup. Do not persist credentials or PINs. Required tests should reconstruct the client after a committed-but-lost response for all three commands and assert one movement/journal/audit and one transfer handover. No runtime test was run. See [comparison artifact](Duplication-and-Architecture/treasury-unknown-result-client-server-idempotency-semantic-comparison.json).


## SEM-MIRROR-055 — Treasury unknown-result recovery across client and server

The cashier treasury form creates a request ID when an action opens, builds one typed deposit/withdraw/transfer request, and retains its delegate after `OutcomeUnknown`. `AppSession` and its unresolved-operation registry are process-only; navigation is blocked only while that in-memory state exists. The server handlers derive durable replay keys from the request IDs and transactionally write movement/journal/audit (plus a linked handover for transfers). Existing integration tests cover same-ID server replay/concurrency, but do not reconstruct the client after a committed-but-unacknowledged request.

This supplements existing `UI-001`; it does not prove a duplicate transaction and creates no new canonical finding. Same-process retry and server replay are counterevidence. A future repair can persist the immutable request and ID before dispatch and reconcile it after relogin, or expose an operation-status lookup. Do not persist credentials or PINs. Required tests should reconstruct the client after a committed-but-lost response for all three commands and assert one movement/journal/audit and one transfer handover. No runtime test was run. See [comparison artifact](Duplication-and-Architecture/treasury-unknown-result-client-server-idempotency-semantic-comparison.json).


## SEM-MIRROR-056 — Cashier and Control Center safe-drop recovery

The two active Cashier WinUI surfaces send the same `SafeDropRequest` to the same server handler, but their uncertain-result recovery differs. `CashierViewModel.Operations` holds its pending request and ID only in fields, so a same-process retry preserves identity but the inspected entry point does not restore it after process restart. `ControlCenterViewModel.Reconciliation` uses the stable owner-keyed `RetainedRequest`, reconstructs pending amount/reason and retries the same ID after a restart when local persistence succeeds. Its store catches persistence errors; app bootstrap supplies no error callback and send is not gated on confirmed save, so the durable guarantee is best-effort. Both paths have context-specific shift/day admission gates; no defect is inferred from those differences.

This gives the additional safe-drop source evidence requested by the existing `UI-001` challenge. The canonical record was extended; no duplicate finding was created and no repeat movement was runtime-proven. The existing client test only verifies identical same-instance retry, not restart restoration. Future work should align both entry points on one retained-request mechanism, define behavior on store-write failure, and test reconstruction plus one movement/journal/audit under a lost response. No tests were run. See [comparison artifact](Duplication-and-Architecture/cashier-controlcenter-safe-drop-recovery-semantic-comparison.json).


### SEM-MIRROR-057 — Employee compensation validation across UI, API and payroll (2026-10-22)

The Cashier WinUI editor rejects negative daily/shift amounts and negative optional monthly/overtime values before forming create/update DTOs (`EmployeeEditorViewModel.CreateRequest`, lines 186–225). The server handlers validate identity text and enum/authorization, then copy signed compensation fields without a matching amount guard (`EmployeeManagementHandlers.cs`, lines 26–49 and 79–111; `EmployeeRules.AssertEmployeeInput`, lines 11–17). The request records use signed `long` fields. Downstream wage/overtime/extra-shift and biometric paths can send those values to `EmployeeLedgerPoster`; payroll sums credit fields and floors only final net.

This is a source-confirmed client/server validation divergence. It is not an authorization bypass: profile write endpoints remain protected by `employees_manage`. Negative earning credits are a source-supported consequence, but policy, actual API input, persistence and settlement effects were not runtime-tested. Existing compensation-mode casing comparison (SEM-MIRROR-008) is separate. This comparison cross-references the existing unpromoted `employee-credit-admission-source-leads.json`; no duplicate master finding was created pending specialist disposition. Proposed repair and tests are detailed in [the comparison evidence](Duplication-and-Architecture/employee-compensation-client-server-validation-semantic-comparison.json).


### SEM-MIRROR-057 — Employee compensation validation across UI, API and payroll (2026-10-22)

The Cashier WinUI editor rejects negative daily, shift, optional monthly and overtime values before it constructs its request. `CreateEmployeeHandler` and `UpdateEmployeeHandler` validate identity text, compensation enum and `employees_manage`, then copy signed request values to the employee entity without a matching amount invariant. Daily-wage, overtime and extra-shift handlers can write their computed signed values to ledger credit columns; biometric check-in posts stored daily wage directly for daily compensation. Payroll aggregation sums these credit columns and floors only the final net at zero.

This is a source-confirmed cross-layer validation divergence, not an authorization bypass or proof of a paid negative amount. The owner/domain contract for negative, zero and nullable compensation values remains unresolved; actual API input, SQL persistence and payroll settlement were not exercised. SEM-MIRROR-008 concerns compensation-type casing and remains a separate issue. Do not create a duplicate canonical finding until the Financial Integrity reviewer challenges the existing employee-credit lead. A future repair, if negative earnings are disallowed, should enforce the documented invariant at server/domain write boundaries and test API create/update plus manual and biometric credit paths while preserving allowed zero/null and legacy behavior. Full repair/test matrix is in the comparison artifact.

Evidence: [comparison](Duplication-and-Architecture/employee-compensation-client-server-validation-semantic-comparison.json), [coordinator source validation](Duplication-and-Architecture/employee-compensation-client-server-validation-coordinator-validation-2026-10-22.json). Mechanical identity gate: 57 semantic groups, 56 artifact inputs, 415 source-reference records, 207 distinct source paths, 991 stable symbols, 647 ranges, zero errors. The report-link gate checked 126 links with zero broken. These checks validate saved references, not exhaustive semantic coverage or runtime behavior.


### SEM-MIRROR-058 — Recipe retarget and current food-cost pointer resolution (2026-10-22T04:06:35+00:00)

`SaveRecipeRequest` allows an existing recipe ID and product ID together. `SaveRecipeHandler` changes the recipe’s `ProductId`, then assigns that recipe ID to the requested product; when the product changes, it does not clear the previous product’s `RecipeId`. The model stores both fields independently, and its comment describes the product pointer as non-FK while the recipe’s `ProductId` is the intended relation. `GetFoodCostReportAsync` resolves sold lines using the current `Product.RecipeId` and current recipe lines, rather than `RecipeEntity.ProductId` or a recipe ID frozen on the order line.

Therefore, if existing-recipe retargeting is supported and product-recipe ownership is exclusive, a stale old product pointer can cause the current report to use the retargeted recipe for sales of both products. This is a plausible reporting-integrity risk from source behavior, not an observed wrong report. The current report already uses current recipe/current purchase cost, so no historical-cost snapshot promise is inferred. Confirm the recipe association contract and actual editor workflow before classifying a defect. A targeted future correction is to clear the former product backlink atomically when it still points to the moved recipe, or reject retargets and provide an explicit detach/attach operation; sharing, if intended, needs explicit deterministic modeling.

No API/DB/report/test was run. Required retarget and food-cost tests, policy gates and acceptance criteria are in [the comparison artifact](Duplication-and-Architecture/recipe-retarget-foodcost-pointer-semantic-comparison.json). This is a new bounded semantic comparison, not an exact AST clone defect and not a canonical finding pending specialist/owner disposition.


### SEM-MIRROR-059 — Business-day close snapshot and live delivery exceptions

SOURCE-CONFIRMED INPUT-DOMAIN DIVERGENCE: the live exception query supplies current delivery rows, so shared rules can emit delivery warnings; the frozen close snapshot omits Deliveries and therefore cannot capture those warning categories. Whether that omission is a defect depends on the intended close-snapshot contract: the code separately treats delivery warnings as live operational state and does not make these warnings close blockers. Record as a conditional documentation/contract gap, not a confirmed financial defect.

The two paths reuse `ControlCenterRules.BuildExceptions` but supply different inputs. The live query includes current delivery records; the frozen close snapshot supplies only shift, refund, expense, custody and journal records. The shared rule can therefore emit current delivery warnings on the live endpoint that are absent from the close snapshot. `BusinessDayCloseEvaluator` does not treat those warnings as close blockers. This is a source-confirmed scope difference, not a confirmed accounting defect; owner intent for `ExceptionsJson` remains unresolved.

Evidence: [comparison artifact](Duplication-and-Architecture/business-day-close-snapshot-vs-live-exceptions-semantic-comparison.json). No runtime/database test was run. Preserve distinct live versus snapshot time semantics if future work consolidates these paths.


## Bounded semantic comparison SEM-MIRROR-060 — reversed dashboard ranges (2026-10-24)

A targeted edge-case review compared the owner-dashboard API, BusinessDateRange conversion/enumeration, sales headline and daily chart. For a reversed multi-date input, the API constructs the range without rejecting it. The sales headline uses ToUtcRange(), which swaps endpoints, while the daily series uses the original From/To in Days(), yielding no points. This is a source-confirmed report-consistency divergence, but the required input policy is unspecified. It extends the existing FIN-CAND-REPORT-RANGE-001 scope and does not create a duplicate master finding. The actionable alternatives are to reject reversed endpoints at the boundary or canonicalize them once so all range operations agree. Required API and value-object regression tests are specified in the evidence artifact.

Evidence: Deep-Audit/Duplication-and-Architecture/reversed-dashboard-range-series-semantic-comparison.json. Related existing comparison: SEM-MIRROR-020, which had covered forward daily/hourly series parity but not this reversed-range edge case. Current registry reconciliation: Deep-Audit/Duplication-and-Architecture/semantic-registry-current-source-integrity-2026-10-24.json (60 groups, 58 unique artifact paths; all artifact paths parse and resolve; 403 source-reference records, 200 source paths, 923 stable symbol IDs, 618 explicit ranges; no path/hash/range metadata errors found by the recomputation). These metrics differ from stale status snapshots, so the exact ledger reconciliation is retained separately. This gate proves artifact/reference integrity only, not semantic completeness. The finite AST queue remains 218/218 dispositioned; Phase 11 remains OPEN.


## Bounded client/server comparison SEM-MIRROR-061 — order-history scope

The active Cashier WinUI list sends CurrentUser.Id as actorUserId; its local filter narrows the response for presentation. The API middleware resolves bearer identity and rejects a query actor ID that differs before the route runs. The server then applies orders_view_all or restricts the list to the currently open shift. The reviewed client/server behavior is consistent with server-owned scope; no duplicate finding was created. This targeted check does not clear SEC-001’s separate JSON body casing issue or certify other routes. The server comment describes current-open-shift scope, rather than cashier-owned history, as intentional legacy behavior; the historical implementation source is absent, so parity with that old implementation cannot be independently verified.

Evidence: Deep-Audit/Duplication-and-Architecture/cashier-order-history-session-scope-semantic-comparison.json. Required matching/mismatching actor and permission integration tests are listed there; none were executed.


### SEM-MIRROR-062 owner dashboard and inventory balance parity

OwnerDashboardQueryService.LoadStockAsync at src/Falah.Infrastructure/Reports/OwnerDashboardQueryService.cs:297-315 sums movement quantities without Direction. InventoryRules.StockBalanceMilliUnits at src/Falah.Domain/Inventory/InventoryRules.cs:50-52 and InventoryQueryService.GetInventoryOverviewAsync at src/Falah.Infrastructure/Inventory/InventoryQueryService.cs:388-416 sign positive-magnitude Out movements as negative. RecordWasteHandler persists positive quantity with Direction=Out at src/Falah.Infrastructure/Inventory/StockMovementHandlers.cs:106-149. With 10,000 In and 2,000 Out, dashboard raw sum is 12,000 while inventory overview is 8,000; an existing integration test asserts the latter at tests/Falah.Server.IntegrationTests/InventoryVerticalSliceTests.cs:533-553. This matches existing finding FIN-013 and creates no duplicate. The test was inspected but not executed. Evidence and tests are in owner-dashboard-vs-inventory-overview-stock-balance-semantic-comparison.json.


## SEM-MIRROR-063 — employee financial report projection (2026-10-24)

The two report paths share the same all-time employee balance calculation, through `EmployeeQueryService.GetBalanceAsync`. The range report includes active employees and range-scoped ledger buckets; the daily statement uses a single business date, the employee list cohort, and a narrower row shape. The missing salary/extra-shift/other-earning/adjustment daily columns may be intentional for this DTO; no product contract confirms the expected categories. Treat as a requirements question, not a confirmed defect. Keep the report shapes distinct unless the owner confirms parity requirements, and test each bucket and cohort when the contract is decided. Full comparison evidence and source hashes are in the linked JSON artifact.


## SEM-MIRROR-064 — daily net versus signed shift differences (2026-10-24)

Compared the cashier daily summary net (ShiftsOveragePiastres minus ShiftsShortagePiastres) with the server same-business-date aggregation and the shift-history signed row. For ordinary supported values, the daily net equals the sum of signed per-shift differences. This is consistent logic at different granularities, not a duplicate write or mismatch. Keep the daily net and per-shift detail distinct. Add a regression proving the same-date aggregate identity for positive, negative, zero and nullable values. Math.Abs at signed-long extremes was not tested; no supported amount bounds were established, so no defect is asserted. Source evidence: Duplication-and-Architecture/daily-summary-net-vs-shift-signed-difference-semantic-comparison.json.


## SEM-MIRROR-067 — Inventory waste entry and server valuation (2026-10-24)

Compared the WinUI waste submission path (InventoryViewModel.SubmitWasteAsync / CompleteWasteAsync) with the API route, RecordWasteHandler.HandleAsync, InventoryRules.AssertWasteInput, PurchasingRules.LastPurchaseUnitCost / ValueStockLoss, and vertical-slice test source. The client preflights positive quantity and a nonblank reason; the server independently checks the loss type, positive quantity and reason, then authorizes and writes an outbound movement plus audit event. No divergence was confirmed for these validation rules; the server remains authoritative.

The handler values waste using the latest positive purchase receipt cost and stores the value at posting, leaving it unknown when there is no qualifying purchase. Existing test source covers valuation, unknown cost, invalid movement type, repeated and concurrent same-key single-write behavior, negative-stock discrepancy recording, and critical/warning severity around 10,000 milli-units. Tests were inspected, not run; test-host results do not establish deployed SQL Server concurrency behavior.

Two bounded leads remain outside this comparison conclusion: the idempotency lookup precedes the handler actor authorization, so the complete endpoint authentication pipeline needs review during the Security phase; and reusing one RequestId with a different payload lacks an inspected contract test. No bypass, disclosure, or financial defect is asserted here. Detailed paths, hashes, IDs and ranges: Duplication-and-Architecture/inventory-waste-client-server-validation-valuation-semantic-comparison.json.


## SEM-MIRROR-068 — Price-rule authoring and checkout-time authority

The bounded comparison found no client/server validation divergence or stale-rule charging defect in the reviewed path. `MenuEditorViewModel.CreateRequest` provides ordinary-input preflight; `SavePriceRuleHandler` and `PricingRules.AssertPriceRuleInput` independently enforce nonblank name, nonempty known products, positive price, valid weekday values, and `offers_manage` authorization. At checkout, `MenuPricingService.ResolveAsync` loads server catalog prices and active offers, then `ItemPricingRules.Resolve` revalidates the requested offer at the checkout timestamp and falls back to list price when the rule is no longer applicable. The server creates the frozen order-line values; no client-supplied charge is used.

The domain source expressly preserves midnight-wrapping windows, ignores a half-configured time window, and does not reject overlapping offers. These were recorded as current behavior, not asserted as requirements beyond the source contract. Existing test source covers create/read, invalid or missing products, permission/approval, update-in-place, active-rule override, inactive-rule fallback, and preserving an old order price after deactivation. Tests were read but not run. Gaps include invalid price/day boundaries, one-sided times, overlapping offer selection, expiry between selection and checkout, and changed-payload request-ID replay. Exact paths, hashes, symbols, and ranges are in [the SEM-068 evidence](Duplication-and-Architecture/price-rule-authoring-checkout-semantic-comparison.json). This is one lead-driven comparison; Phase 11 remains open.


## SEM-MIRROR-069 — Configured delivery fee versus client-submitted checkout fee (2026-10-24)

The Cashier and Call Center WinUI checkout paths are aligned: both submit the selected delivery service ID/name and the selected DTO fee (falling back to zero when nullable). The server fee path is different in authority: Create/UpdateDeliveryServiceHandler accept nullable fee values without a visible numeric sign/range check in the reviewed handlers, while CheckoutOrderHandler converts request.DeliveryFeePiastres directly to Money and builds the payable plan. Its full current source has no DeliveryServices lookup; the submitted service ID/name therefore do not bind the fee to the persisted active service at checkout. The order snapshots the request-derived plan fee. DeliveryOrderIntakeTests use a request with ID svc-1 and fee 1000 and assert that submitted fee is snapshotted; they do not resolve or compare a configured service fee.

This comparison supplements **FIN-016** and introduces no duplicate finding. The normal clients submit the selected fee consistently; direct request manipulation, stale configured values, and whether overrides are allowed depend on the server trust boundary and owner policy. Future implementation must decide whether the active configured service is authoritative or a separately authorized override is permitted. If authoritative, resolve and validate the service at checkout; if overrides are permitted, enforce documented range/sign and approval rules. Preserve historical fee snapshots, COD, approved comp and signed balances. Required tests include stale/mismatched/unknown/inactive service, negative and boundary fees, and asserting rejected requests create no order, inventory, journal or treasury effects. Evidence and hashes: [SEM-069 source comparison](Duplication-and-Architecture/delivery-fee-client-server-authority-semantic-comparison.json). No runtime/API/database test was executed. SEM-044 remains the source for separate area/name field behavior.


## SEM-MIRROR-070 — WinUI refund preflight vs server amount clamp

Compared Cashier WinUI refund entry/request retention with the refund endpoint, RefundOrderHandler, domain refund rules and aggregate mutation, plus vertical-slice test source. The client rejects nonpositive and over-displayed-remainder amounts; the server independently caps a positive request to the remaining refundable amount, rejects a resulting zero, then applies same-order locking, aggregate guards, protected source-account checks, refund/treasury/audit writes in a transaction. Existing test source covers clamp behavior, same-key replay/concurrency and same-order overdraw protection. This is an intentional stricter-client/permissive-server contract bounded by the authoritative server remainder; no new divergence or finding was confirmed. SQL Server runtime/transaction behavior and tests were not executed. The earlier WPF/WinUI comparison remains responsible for ambiguous request retry identity; this comparison is limited to amount validation, authorization and ledger effects. Evidence: [comparison artifact](Duplication-and-Architecture/winui-refund-amount-server-clamp-semantic-comparison.json).


## SEM-MIRROR-071 — Business-day close-blocker consumer parity

Compared the close-blocker preview, actual close admission and owner-dashboard blocker count. All three use the same BusinessDayCloseEvaluator; the GET/close routes dispatch to the matching consumers, and BusinessDayCloseVerticalSliceTests source asserts that OPEN_SHIFT appears in preview and that the subsequent close attempt returns the same blocker-code set. This is shared authoritative logic rather than duplicated blocker rules, so no semantic divergence or finding was confirmed. The evaluator draws unresolved shifts, driver custodies, journals, pending electronic payments and protected balances from global persisted state; checklist and some discrepancy inputs are target-day scoped. Whether these financial blockers are intentionally global or must be tied to the selected business day is a policy/scope question not settled by source. No SQL/runtime or test execution occurred. Retain the shared evaluator and document scope before changing it; add cross-day fixtures and verify dashboard/preview/close parity under the chosen policy. Evidence: [comparison artifact](Duplication-and-Architecture/business-day-close-blocker-consumer-parity-semantic-comparison.json).
