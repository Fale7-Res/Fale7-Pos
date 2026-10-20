# Financial, state and persistence review

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. Audit only. This agent has not changed application code/configuration, run tests, touched databases or printers, or performed Git publication.

Coverage: 327 complete files statically reviewed of 327 assigned; 0 explicitly NOT_ANALYZED at file-report level. This does **not** complete exhaustive function review. See [exact ledger](../../Sub-Agents/Results/financial-ledger.json), [coverage methods](../../Sub-Agents/Results/financial-coverage-methods.json), and [remaining original callable IDs](../../Sub-Agents/Results/financial-symbol-remaining.json).

## What the counts mean

There are 4,102 saved original-category callable receipts and 4,702 remaining IDs from the original 8,804-category inventory. These are **not exhaustive denominators**: an executable expression-bodied getter (`Order.Refunds`, line64) was omitted from `isCallable`. The parent is building an additive getter/indexer inventory without replacing original identities. Supplemental reasoning is saved [separately](../../Sub-Agents/Results/financial-expression-getter-evidence.json). Independent function-depth acceptance total remains UNKNOWN.

| Review method | Saved receipts |
|---|---:|
| MANUAL SOURCE REASONING | 1456 |
| MANUAL CHANGED BLOCK AND CURRENT CONTEXT | 114 |
| EXACT SOURCE EQUIVALENCE PLUS MANUAL CURRENT MODEL CONTEXT | 2456 |
| MANUALLY REASONED AUTO ACCESSOR FIELD SPECIFIC | 76 |

34 generated model files use explicit difference review plus exact source-body equivalence: every reused body has actual text equality, SHA-256, predecessor stable ID/review hash, and a manually checked current parent-model context. New/changed entity, index, relationship, navigation and seed blocks are individually reasoned. This is not fresh independent rereading, runtime validation or automatic name matching. See [equivalence ledger](../../Sub-Agents/Results/financial-model-equivalence-ledger.json).

## Financial findings

- **FIN-001 HIGH**: Mixed cash/electronic checkout cannot confirm its electronic leg — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-002 HIGH**: Cancelling pending mixed electronic payment hides collected cash without reversal — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-003 HIGH**: Refund can debit money for an order that was never collected — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-004 HIGH**: Historical cash refund omitted from current-shift expected cash — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-005 HIGH**: Concurrent distinct invoice settlements can overpay supplier and corrupt invoice cache — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-006 HIGH**: Generic payroll transition marks run Paid without paying treasury or employee ledger — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-007 MEDIUM**: Backup filenames collide within a second and WITH FORMAT can overwrite prior backup — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-008 HIGH**: Restore pending-journal check does not exclude active or newly accepted financial work — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-009 MEDIUM**: Concurrent different open-day requests create multiple active business days — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-010 MEDIUM**: Collected sales report drops immediate money from pending mixed payments — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-011 HIGH**: Shift opening and purchase receipt check protected account balance before their transactions — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-012 HIGH**: Attendance correction credits do not enter calculated payroll — CONFIRMED_STATIC_INDEPENDENT_REVIEW
- **FIN-013 MEDIUM**: Owner dashboard adds outgoing stock instead of subtracting it — CONFIRMED_STATIC_INDEPENDENT_REVIEW

These 13 source-registry findings have independent static corroboration. They are not runtime reproductions. Their exact conditions, counterevidence, excerpts, correction designs and proposed regressions remain in the [financial registry](../../Sub-Agents/Results/financial-findings.json). Root corroboration for FIN-007/008/009 is [saved here](financial-parent-challenges.json); FIN-009's DbContext reference was corrected to137–141 rather than the unrelated Expenses block148–152.

FIN-014 is parent-canonical: after a successful RESTORE, a later MULTI_USER failure can leave `databaseWasReplaced=false`, report Failed, and skip recreating the outcome record. My [independent qualified challenge](../../Sub-Agents/Handoffs/financial-restore-outcome-challenge.json) preserves the conditional path and recovery counterevidence. The parent alone owns [MASTER_FINDINGS](../MASTER_FINDINGS.json); this summary is not another master.

Two additional candidates are explicitly pending independent disposition in [new candidates](../../Sub-Agents/Results/financial-new-candidates.json): stale checkout admission after completed shift handover, and close approval attribution derived from an unvalidated optional presentation. The second concerns audit identity only; the operation may already be legitimately authorized. Neither is silently counted as confirmed.

## Test evidence and limits

All owned integration/domain test files now have file reports. Reviewed tests often assert less than their names: five separate [test-quality observations](../../Sub-Agents/Results/financial-test-coverage-observations.json) preserve admin-fixture interference, confounded deactivation, unrelated delivery-service fee editing, reprint attribution already present in the original, and non-numerical average/variance assertions. These are not additional confirmed production vulnerabilities.

No integration fixtures were executed by this agent. `FalahApiFactory` creates and disposes databases; source review does not authorize touching the user-active `Fale7CSharp_Test`. Parent-run tests or isolated harness results require their own provenance and are not represented as this agent's execution.

## Evidence-backed qualifications

- Permission-OR-approval is the documented restore authorization policy (`BackupRequests19–26`). A direct `backup_manage` holder bypassing the optional approval is **not** a demonstrated vulnerability. The narrower delegated fallback versus actor-only safety-backup permission mismatch is an unpromoted availability lead.
- Current `ManagerApproval.Consumed` is an EF concurrency token. A simple read/read/consume race allegation without accounting for the guarded update and transaction rollback is rejected.
- Account locks protect liquidity only while held inside the caller's transaction. They do not enforce invoice entitlement, collected-order entitlement, shift admission or operational allocation.
- Stock not being restored on electronic cancellation is explicit current policy; it is not independently called a defect. The mixed immediate-money loss is FIN-002.
- A schema snapshot is not a live database, upgrade execution or proof of safe backfill. Historical PIN and inventory-direction migration defaults need actual startup/repair-chain trace; fresh demo seeding cannot establish populated-upgrade safety.
- Metadata and queue/job DTOs do not prove physical printing, bank transfer settlement, backup recoverability or production performance.

## Next work

Continue the exact original callable queue through payment queries, protected-account validation, purchasing/payroll and the remaining domain/infrastructure/test callables. Reconcile supplemental getter facets when the parent publishes their map. Preserve source-only candidates, rejected interpretations and uncertain workflow assumptions; do not publish a partial Git checkpoint or claim the original15 phases are complete.

FIN-005 citation correction: Purchase concurrency evidence is DbContext277-305 plus PurchasingEntities20-60/75-112, replacing irrelevant legacy Customer/Delivery excerpt309-328. Predecessor retained; [independent correction](Independent-Challenges/FIN005-model-reference-correction.json). Source finding remains static-only.
