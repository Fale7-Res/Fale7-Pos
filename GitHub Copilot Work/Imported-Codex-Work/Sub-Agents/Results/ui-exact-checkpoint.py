from ui_persistent import *
from pathlib import Path
ledger=json.loads((RESULTS/'ui-ledger.json').read_text(encoding='utf-8-sig'))
files=[]; total=done=0; shards=0
for entry in ledger['files']:
 path=entry['path']; source=load_symbols(path)
 shard=ROOT/'Codex Work/Knowledge/Reviews/ui_visual'/Path(path+'.json')
 receipt=json.loads(shard.read_text(encoding='utf8')) if shard.exists() else {}
 if shard.exists():shards+=1
 analyzed={s['stable_id'] for s in receipt.get('symbols',[]) if s.get('status')=='ANALYZED'}
 callables=[s for s in source['symbols'] if s['isCallable']]
 pending=[dict(stable_id=s['stable_id'],qualified_signature=s['qualifiedSymbol'],kind=s['kind'],span=[s['startLine'],s['startColumn'],s['endLine'],s['endColumn']]) for s in callables if s['stable_id'] not in analyzed]
 total+=len(callables); done+=sum(s['stable_id'] in analyzed for s in callables)
 files.append(dict(path=path,git_blob_sha256=source['git_blob_sha256'],legacy_file_status=entry['status'],persistent_shard_present=shard.exists(),callable_total=len(callables),analyzed_callable_count=len(callables)-len(pending),remaining_callables=pending))
target=ROOT/'Codex Work/Sub-Agents/Results/ui-exact-symbol-coverage.json'
atomic_json(target,dict(baseline=BASELINE if 'BASELINE' in globals() else 'c35816ecae49633674775dd32061c6ee92f94aaf',reviewer='ui_visual',updated_at_utc=now(),scope_file_total=len(files),persistent_shard_file_count=shards,total_callable_ids=total,analyzed_callable_ids=done,remaining_callable_ids=total-done,legacy_name_heading_matches_not_promoted=True,files=files))
cp=ROOT/'Codex Work/Sub-Agents/Checkpoints/ui_visual.json'; c=json.loads(cp.read_text(encoding='utf8'))
c.update(updated_at_utc=now(),exact_remaining_files=[],exact_remaining_symbols_reference=str(target.relative_to(ROOT)).replace('\\','/'),exact_symbol_counts=dict(total=total,analyzed=done,remaining=total-done),next_action='Continue exact callable reconciliation of earlier file reports. Do not promote legacy507 name matches; independently challenge high findings. File-level static coverage complete, exhaustive symbol/native audit incomplete.',pending_independent_challenges=list(dict.fromkeys(c.get('pending_independent_challenges',[])+['UI findings parent canonical registry','UI018 added XAML corroboration','UI021 image editor target race','Shifts overlay row0','Inventory batch count retention/filter lifecycle','Waste date consistency','UI-022 ingredient picker60 cap/search binding'])),blockers=['Native observation unavailable; screenshot claims forbidden'])
priority=['TreasuryViewModel.cs','ControlCenterViewModel.Operations.cs','ControlCenterViewModel.cs','AttendanceViewModel.cs','DailySummaryViewModel.cs','DeliveryDirectoryViewModel.cs','ManagementWriteSafetyTests.cs','RetainedRequestDurabilityTests.cs']
pending_files=[f for f in files if f['remaining_callables']]
pending_files.sort(key=lambda f:(next((i for i,n in enumerate(priority) if f['path'].endswith('/'+n)),len(priority)),f['path']))
if pending_files:
 first=pending_files[0]; symbol=first['remaining_callables'][0]
 c['NEXT_ACTION_CURRENT']=dict(action='Read actual source and reconcile this exact unreviewed symbol, then remaining symbols in file; do not rerun completed receipt scripts.',path=first['path'],stable_id=symbol['stable_id'],signature=symbol['qualified_signature'],span=symbol['span'],exact_remaining_count=total-done,remainder_manifest=str(target.relative_to(ROOT)).replace('\\','/'))
 c['next_action']='NEXT_ACTION_CURRENT: '+first['path']+' '+symbol['qualified_signature']+'; exact remainder '+str(total-done)+'. No application/runtime/DB/authentication/Git actions.'
c['NEXT_ACTION_checkpoint_counts']=dict(analyzed=done,total=total,remaining=total-done,last_completed_file=c.get('last_completed_file'),current_partial_file=c.get('current_partial_file'),updated_at_utc=now())
atomic_json(cp,c)
(ROOT/'Codex Work/Deep-Audit/UI-UX/ui-persistent-status.md').write_text(f'''# UI review persistent checkpoint

Baseline `c35816ecae49633674775dd32061c6ee92f94aaf`.

All 274 original scoped files have manually reasoned static file reports. This is **not** exhaustive per-symbol or native verification.

Persistent review shards: {shards} files. Exact callable receipts: **{done}/{total}**, with **{total-done} remaining**. Earlier 507 heading-name matches are historical and have not been promoted. Exact symbol reconciliation of the earlier report set remains incomplete; resource files have no C# callable declarations.

Exact remainder and source identities: `Codex Work/Sub-Agents/Results/ui-exact-symbol-coverage.json`. Atomic resume state: `Codex Work/Sub-Agents/Checkpoints/ui_visual.json`.

No application/source/configuration, runtime/session, database or printer operations were performed. No screenshots were observed and no tests executed by this reviewer. Parent alone owns canonical findings/global progress and Git publication.
''',encoding='utf8')
print(json.dumps(dict(files=len(files),shards=shards,total=total,analyzed=done,remaining=total-done)))
