"""One-time identity-map creation; existing reviewed knowledge is never regenerated."""
from knowledge_store import *
from collections import defaultdict
import re
inventory=json.loads((WORK/'Deep-Audit/Source-Inventory/verified-baseline-hashes.json').read_text(encoding='utf8'))
index=json.loads((WORK/'Deep-Audit/Source-Inventory/Current/FUNCTION_INDEX.json').read_text(encoding='utf8'))
by=defaultdict(list)
for symbol in index['declarations']:by[symbol['path']].append(symbol)
summary=[];seen=set();created=0
for file in inventory['files']:
    path=file['path'];raw=(ROOT/path).read_bytes()
    if sha256(raw)!=file['worktree_sha256']:raise ValueError('Current source hash mismatch '+path)
    dest=WORK/'Knowledge/Source-Symbols'/Path(path+'.json')
    if dest.exists():
        existing=json.loads(dest.read_text(encoding='utf8'))
        if existing['git_blob_sha256']!=file['git_blob_sha256']:raise ValueError('Existing knowledge belongs to another baseline')
        summary.append({'path':path,'record':str(dest.relative_to(WORK)).replace('\\','/'),'symbols':len(existing['symbols'])});continue
    symbols=[]
    for symbol in by[path]:
        sid=stable_symbol_id(path,file['git_blob_sha256'],symbol)
        if sid in seen:raise ValueError('Stable symbol collision')
        seen.add(sid)
        symbols.append({'stable_id':sid,**symbol,'status':'INDEXED_NOT_ANALYZED'})
    report=WORK/'Deep-Audit/File-Reports'/Path(path+'.md')
    atomic_json(dest,{'schema_version':1,'baseline':BASELINE,'path':path,'git_blob_sha256':file['git_blob_sha256'],
                     'worktree_sha256':file['worktree_sha256'],'purpose':'Identity and unreviewed declaration map; NO review promotion',
                     'legacy_report':str(report.relative_to(WORK)).replace('\\','/') if report.exists() else None,'symbols':symbols})
    created+=1;summary.append({'path':path,'record':str(dest.relative_to(WORK)).replace('\\','/'),'symbols':len(symbols)})
atomic_json(WORK/'Knowledge/source-identity-map.json',{'baseline':BASELINE,'source_files':len(summary),'declarations':sum(r['symbols'] for r in summary),'records':summary,'all_review_states':'INDEXED_NOT_ANALYZED; review shards are separate'})
print(json.dumps({'new_identity_records':created,'source_files':len(summary),'declarations':sum(r['symbols'] for r in summary),'symbols_automatically_promoted':0}))
