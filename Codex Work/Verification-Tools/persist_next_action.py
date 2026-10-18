"""Create an interruption-safe queue from actual saved receipts, not estimated completion."""
from knowledge_store import *
import argparse,re,subprocess
parser=argparse.ArgumentParser()
parser.add_argument('--active',required=True)
parser.add_argument('--last',required=True)
parser.add_argument('--current-file',default='NONE')
parser.add_argument('--current-symbol',default='NONE')
parser.add_argument('--next',required=True)
args=parser.parse_args()
def read(p):return json.loads((WORK/p).read_text(encoding='utf8'))
coverage=read('Knowledge/coverage-index.json')
facet_index=read('Knowledge/Source-Symbols-CallableFacets/index.json') if (WORK/'Knowledge/Source-Symbols-CallableFacets/index.json').exists() else None
facet_coverage=read('Knowledge/callable-facet-coverage.json') if (WORK/'Knowledge/callable-facet-coverage.json').exists() else None
done={r['stable_id'] for r in coverage['receipts'] if r['isCallable']}
remaining=[]
for p in sorted((WORK/'Knowledge/Source-Symbols').rglob('*.json')):
    source=json.loads(p.read_text(encoding='utf8'))
    remaining.extend({'path':source['path'],'source_git_blob_sha256':source['git_blob_sha256'],
                      'stable_id':s['stable_id'],'kind':s['kind'],'symbol':s.get('qualifiedSymbol') or s['name'],
                      'start_line':s['startLine'],'end_line':s['endLine'],'status':'NOT_ANALYZED'}
                     for s in source['symbols'] if s['isCallable'] and s['stable_id'] not in done)
progress=read('Deep-Audit/Progress-20261017-Current.json')
file_ledger=read('Sub-Agents/Results/financial-ledger.json')
missing_files=[r['path'] for r in file_ledger.get('remaining',[]) if r['status']=='NOT_ANALYZED']
# All other streams have full file-pass receipts in current snapshot; reject unnoticed regression.
if len(missing_files)!=progress['files_without_full_review']:
    missing_files=sorted(set(subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE],cwd=ROOT,text=True).splitlines())-
                         {str(Path(p).relative_to('Deep-Audit/File-Reports')).removesuffix('.md').replace('\\','/') for p in progress['review_receipt_paths']})
phases=[line[2:] for line in (WORK/'Conversation-Recovery/Original-Deep-Audit-Request.md').read_text(encoding='utf8').splitlines() if line.startswith('# PHASE ')]
agents={name:read('Sub-Agents/Checkpoints/'+name+'.json') for name in ('financial_integrity','ui_visual','printing_workflows')}
agent_paths={name:'Sub-Agents/Checkpoints/'+name+'.json' for name in agents}
registry=read('Sub-Agents/Agent-Registry/agent-state.json')
for entry in registry['agents']:
    name=entry['name'].split('/')[-1]
    if name in agents:continue
    rel=entry.get('current_checkpoint',entry.get('checkpoint','Sub-Agents/Checkpoints/'+name+'.json')).removeprefix('Codex Work/')
    agent_paths[name]=rel
    agents[name]=read(rel) if (WORK/rel).exists() else {'next_action':entry.get('remaining','Read registry'),'last_completed':entry.get('last_turn','NO_SAVED_CHECKPOINT')}
route_decisions=read('Knowledge/Quality-Reviews/printing-parent-decisions.json')
nonc=read('Knowledge/Quality-Reviews/noncsharp-validation.json') if (WORK/'Knowledge/Quality-Reviews/noncsharp-validation.json').exists() else None
queue={'baseline':BASELINE,'at_utc':now(),'active_task':args.active,'last_successfully_completed':args.last,
       'current_file':args.current_file,'current_symbol':args.current_symbol,'next_unfinished_operation':args.next,
       'unreviewed_file_paths':missing_files,'unreviewed_callable_records':remaining,
       'scope_limits':'Missing exact receipts is not proof source was never read. Existing receipts are author assertions pending independent depth gate; legacy headings excluded.',
       'all_original_phases':phases,'phase_final_gate':'All15 remain subject to final evidence/coverage reconciliation; no whole-audit completion claimed.',
       'required_deliverable_status':progress['deliverables'],
       'other_remaining_queue':[f'Parent{route_decisions["pending_original_queue_count"]}/130 route quality challenges pending; {route_decisions["accepted_count"]} independently accepted with qualifications',
                                'Non-C#17supplementaryASTidentities/receipts validated; retain separate denominator and source-only limits' if nonc and not nonc['errors'] else '10 manual non-C# callable records need AST stable-map reconciliation',
                                'Canonical finding evidence/duplicates and High/Critical independent challenges',
                                'Security SQL/native/process/timer/network entry-to-sink completeness',
                                'Dependency patched alternatives/compatibility/licensing and metadata independent quality review',
                                'Performance/minimum hardware and duplication investigations',
                                'Pickup/delivery/printing/UI/financial category synthesis and missing-test plans',
                                'All19 substantive final deliverables, source/hash/link/privacy gates',
                                'One final normal main commit/push ONLY after genuine final completion'],
       'agent_checkpoint_references':agent_paths}
if facet_index and facet_coverage:
    getter_pending=set(facet_coverage['getter_ids_without_receipts'])
    generated_pending=set(facet_coverage['generated_context_ids_without_receipts'])
    queue['supplementary_getter_queue']=[f for f in facet_index['facets'] if f['stable_id'] in getter_pending]
    queue['generated_behavior_context_queue']=[f for f in facet_index['generated_behavior_inputs'] if f['stable_id'] in generated_pending]
    queue['callable_denominator_limit']='Original11653 syntax categories plus684 source-derived getters; generated behavior contexts separately accounted, exact runtime-generated method count UNKNOWN.'
queue['candidate_artifacts_requiring_disposition_check']=[str(p.relative_to(WORK)).replace('\\','/') for p in sorted((WORK/'Sub-Agents/Results').glob('*candidate*.json'))]
queue['candidate_artifacts_requiring_disposition_check'] += [str(p.relative_to(WORK)).replace('\\','/') for p in sorted((WORK/'Deep-Audit/Security').glob('*candidate*.json'))]
atomic_json(WORK/'Conversation-Recovery/remaining-audit-queue.json',queue)
lines=['# NEXT ACTION — read this first on every resume',f'\nRecorded UTC: {queue["at_utc"]}. Source baseline `{BASELINE}`.',
       '\nAUDIT ONLY. No source/XAML/package/config/DB modifications or physical printing. No partial Git commits/pushes.',
       '\n## Current exact checkpoint',f'\n- Active task: {args.active}',f'- Last verified saved task: {args.last}',
       f'- Current file: `{args.current_file}`; symbol: `{args.current_symbol}`.',f'- Next unfinished operation: {args.next}',
       f'\nFile-pass receipts: {progress["full_file_review_receipts"]}/690; {len(missing_files)} lack a full static report. '
       f'C# exact author callable receipts: {coverage["unique_analyzed_callable_receipts"]}/{coverage["indexed_callables"]}; '
       f'{len(remaining)} missing stable reasoning receipts. Independent depth total UNKNOWN. Counts are snapshots, reconcile live own ledgers.',
       '\n## Original agents and saved assignments']
if facet_index and facet_coverage:
    lines.insert(-1,f'\nMaterial denominator correction: original11653 syntax count excludes expression getters. '
        f"Additive map has{len(facet_index['facets'])} getters, of which{len(getter_pending)} lack receipts, and{len(generated_pending)}/{len(facet_index['generated_behavior_inputs'])} generated behavior contexts remain. "
        'Exact supplementary IDs are preserved in remaining-audit-queue.json; not an exact CLR-generated method total.')
for n,j in agents.items():
    entry=next((a for a in registry['agents'] if a['name'].split('/')[-1]==n),{})
    lines.append(f'\n- `{n}`: registry status `{entry.get("status","UNKNOWN")}`; last saved file/task `{j.get("last_completed_file") or j.get("last_completed","UNKNOWN")}`. '
                 f'Next: {j.get("next_action","Read own handoff")}. Checkpoint: [saved state](../{agent_paths[n]}).')
lines+=['\nOriginal sessions reported usage-limit failures. Financial/UI same-session retry is active as of latest root observation. '
        'Printing retry returned agent thread limit. Replacement performance_reviewer saved substantive performance and SEC003 source-challenge reports; '
        'its new UI follow-up was unavailable, so replacement ui_independent_challenge completed11 HIGH UI source challenges and SEC002. '
        'That same reviewer now has a separate SQL-boundary assignment and checkpoint. No inaccessible hidden memory reconstructed.',
        '\n## Entire remaining queue — do not discard after interrupted task',
        '\nExact remaining paths and all stable IDs/signatures/spans: [remaining-audit-queue.json](remaining-audit-queue.json).',
        '\n'+ '\n'.join('- '+x for x in queue['other_remaining_queue']),
        '\nAll original phases must still clear final gates:\n\n'+'\n'.join('- '+x for x in phases),
        '\n## Saved evidence',
        '\n- [Current progress snapshot](../Deep-Audit/Progress-20261017-Current.json).',
        '- [Stable symbol receipts](../Knowledge/coverage-index.json), [parent independent decisions](../Knowledge/Quality-Reviews/printing-parent-decisions.json).',
        '- [Domain377-test sanitized result](../Deep-Audit/Test-Coverage/domain-tests-20261017-sanitized.json): existing binary, no rebuild provenance guarantee.',
        '- [SEC001 isolated binding result](../Knowledge/Flows/SEC-001/minimal-api-binding-results.json):10assertions; no live exploit/DB mutation.',
        '- [Dependency metadata handoff](../Deep-Audit/Dependency-Security/dependency-audit-handoff.md):210components;2test-only historic matches, actual runtime exposure not established.',
        '\nBefore another substantial task, verify preceding evidence exists and update this checkpoint atomically.']
atomic_text(WORK/'Conversation-Recovery/NEXT_ACTION.md','\n'.join(lines)+'\n')
print(json.dumps({'remaining_file_paths':len(missing_files),'remaining_callable_ids':len(remaining),'checkpoint_readback_verified':True}))
