import json,sys
from pathlib import Path
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
l=json.loads((k.WORK/'Sub-Agents/Results/financial-ledger.json').read_text(encoding='utf8'));rem=json.loads((k.WORK/'Sub-Agents/Results/financial-symbol-remaining.json').read_text(encoding='utf8'));eq=json.loads((k.WORK/'Sub-Agents/Results/financial-model-equivalence-ledger.json').read_text(encoding='utf8'));fs=json.loads((k.WORK/'Sub-Agents/Results/financial-findings.json').read_text(encoding='utf8'));classes={}
for p in(k.WORK/'Knowledge/Reviews/financial_integrity').rglob('*.json'):
 d=json.loads(p.read_text(encoding='utf8'))
 for s in d.get('symbols',[]):
  if s.get('status')=='ANALYZED':c=s.get('coverage_classification','MANUAL_SOURCE_REASONING');classes[c]=classes.get(c,0)+1
out=dict(baseline=k.BASELINE,at_utc=k.now(),file_reports=l['reviewed_count'],file_scope=l['scope_count'],remaining_file_reports=l['remaining_count'],original_category_callable_receipts=sum(classes.values()),original_category_scope=8804,original_category_remaining=8804-sum(classes.values()),original_denominator_not_exhaustive=True,additive_getter_inventory_pending_parent=True,method_classification=classes,source_equivalence_files=len(eq['files']),independent_function_depth_total='UNKNOWN',next_action='OrderPaymentQueries/TreasuryMapper strictsymbols thenReceive/PayPurchase/payroll, remainingoriginalcallablequeue andsupplementarygetterfacets.',new_candidates_reference='Sub-Agents/Results/financial-new-candidates.json',property_evidence_reference='Sub-Agents/Results/financial-expression-getter-evidence.json')
k.atomic_json(k.WORK/'Sub-Agents/Results/financial-coverage-methods.json',out)
text='''# Financial, state and persistence review

Baseline: `c35816ecae49633674775dd32061c6ee92f94aaf`. Audit only. This agent has not changed application code/configuration, run tests, touched databases or printers, or performed Git publication.

Coverage: 327 complete files statically reviewed of 327 assigned; 0 explicitly NOT_ANALYZED at file-report level. This does **not** complete exhaustive function review. See [exact ledger](../../Sub-Agents/Results/financial-ledger.json), [coverage methods](../../Sub-Agents/Results/financial-coverage-methods.json), and [remaining original callable IDs](../../Sub-Agents/Results/financial-symbol-remaining.json).

## What the counts mean

'''
text+=f"There are {sum(classes.values()):,} saved original-category callable receipts and {8804-sum(classes.values()):,} remaining IDs from the original 8,804-category inventory. These are **not exhaustive denominators**: an executable expression-bodied getter (`Order.Refunds`, line64) was omitted from `isCallable`. The parent is building an additive getter/indexer inventory without replacing original identities. Supplemental reasoning is saved [separately](../../Sub-Agents/Results/financial-expression-getter-evidence.json). Independent function-depth acceptance total remains UNKNOWN.\n\n"
text+='| Review method | Saved receipts |\n|---|---:|\n'+''.join('| '+c.replace('_',' ')+' | '+str(n)+' |\n'for c,n in classes.items())
text+=f"\n{len(eq['files'])} generated model files use explicit difference review plus exact source-body equivalence: every reused body has actual text equality, SHA-256, predecessor stable ID/review hash, and a manually checked current parent-model context. New/changed entity, index, relationship, navigation and seed blocks are individually reasoned. This is not fresh independent rereading, runtime validation or automatic name matching. See [equivalence ledger](../../Sub-Agents/Results/financial-model-equivalence-ledger.json).\n\n## Financial findings\n\n"
text+=''.join(f"- **{f['id']} {f['severity']}**: {f['title']} — {f['status']}\n"for f in fs)
text+='''
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
'''
k.atomic_text(k.WORK/'Deep-Audit/Database-and-Finance/financial-summary.md',text)
print(json.dumps(out))
