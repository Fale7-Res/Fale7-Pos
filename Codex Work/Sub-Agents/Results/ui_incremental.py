"""Incremental audit-note persistence; never generates review from an unread source file."""
from pathlib import Path
import json,hashlib,subprocess,collections
ROOT=Path(__file__).resolve().parents[3]
RESULTS=ROOT/'Codex Work/Sub-Agents/Results'
REPORTS=ROOT/'Codex Work/Deep-Audit/File-Reports'
SHA='c35816ecae49633674775dd32061c6ee92f94aaf'
P='src/Falah.Client.CashierWinUI/'; C='src/Falah.Client.CallCenterWinUI/'; W='src/Falah.Client.Cashier/';T='tests/Falah.Client.Tests/'
def record(path,role,notes,tests,recommendations,status='REVIEWED_SOURCE'):
 data=json.loads((RESULTS/'ui-ledger.json').read_text(encoding='utf-8-sig'))
 item=next(x for x in data['files'] if x['path']==path)
 raw=subprocess.check_output(['git','show',f'{SHA}:{path}'],cwd=ROOT)
 wt=(ROOT/path).read_bytes(); source=raw.decode('utf-8-sig'); lines=source.splitlines()
 reviewed=[]; body=[f'# {path}',f'Baseline `{SHA}`. Review: **{status}**. Source-based; native UI/runtime **NOT_TESTED by this reviewer**.',f'Git blob SHA256 `{hashlib.sha256(raw).hexdigest()}`; worktree SHA256 `{hashlib.sha256(wt).hexdigest()}`; LF-equivalent `{wt.replace(bytes([13,10]),bytes([10]))==raw.replace(bytes([13,10]),bytes([10]))}`.',f'Actual role: {role}','## Function/resource review']
 for anchor,note in notes:
  loc=[i+1 for i,l in enumerate(lines) if anchor in l]
  body.append(f'- `{anchor}` at `{path}:{loc[0] if loc else "UNKNOWN"}`: {note}')
  reviewed.append({'anchor':anchor,'matchingSourceLines':loc,'reviewed':True,'reviewKind':'MANUAL_DATAFLOW' if path.endswith('.cs') else 'MANUAL_XAML_RESOURCE_BINDING'})
 body.extend(['## Tests and limitations',tests,'## Recommendations (not implemented)',recommendations,'## Applicability','No source, configuration, runtime, database, printer, package or system changes were executed. No screenshot has been reviewed by this agent. A client permission display is not server authorization; server endpoints/transactions are independently reviewed. Pure view metadata has NOT_APPLICABLE database transactions/direct printing. Other unknowns remain UNKNOWN; no arbitrary count inferred as zero. The source anchors support actual reasoning, not declaration enumeration.'])
 target=REPORTS/(path+'.md');target.parent.mkdir(parents=True,exist_ok=True);target.write_text('\n\n'.join(body)+'\n',encoding='utf-8')
 item.update(status=status,fileReviewed=True,function_review='MANUAL_FLOW_REVIEW' if path.endswith('.cs') else 'NOT_APPLICABLE',functionReviewAnchors=[x[0] for x in notes],reviewedUnits=reviewed,report=str(target.relative_to(ROOT)).replace('\\','/'),git_blob_sha256=hashlib.sha256(raw).hexdigest(),worktree_sha256=hashlib.sha256(wt).hexdigest(),normalized_lf_equivalent=wt.replace(b'\r\n',b'\n')==raw.replace(b'\r\n',b'\n'))
 for e in data['files']:
  if e.get('fileReviewed'):e['report']='Codex Work/Deep-Audit/File-Reports/'+e['path']+'.md'
 data['counts']=dict(collections.Counter(e['status'] for e in data['files']))
 (RESULTS/'ui-ledger.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
 print(path, data['counts'])
