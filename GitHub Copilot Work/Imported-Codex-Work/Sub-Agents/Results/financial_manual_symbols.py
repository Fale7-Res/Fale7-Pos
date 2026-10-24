"""Persistence helper for MANUALLY authored symbol reasoning. Does not infer analysis from inventory."""
import sys,json
from pathlib import Path
sys.path.insert(0,str(Path('Codex Work/Verification-Tools').resolve()));import knowledge_store as k
class Review:
 def __init__(self,path):self.path=path;self.source=k.load_symbols(path);self.records=[]
 def add(self,line,col,reason,inputs,outputs,effects,db,trust,tests,refs,printing='NOT_APPLICABLE: no UI/hardware call.',performance='Bounded fixture source; production load UNKNOWN.',issues='No new promoted finding; limitations in reasoning.'):
  s=next(s for s in self.source['symbols'] if s['isCallable'] and s['startLine']==line and s['startColumn']==col)
  self.records.append(dict(stable_id=s['stable_id'],status='ANALYZED',signature=s['qualifiedSymbol'],span=[line,col,s['endLine'],s['endColumn']],reasoning=reason,inputs=inputs,outputs=outputs,side_effects=effects,database=db,trust_boundaries=trust,printing_ui=printing,performance=performance,duplicates_conflicts='No declaration conflict observed; replay/concurrency limits explicitly recorded.',issues_hypotheses=issues,tests_missing=tests,verified_callers_callees=refs))
 def save(self,next_action,complete=True):
  if complete:assert len(self.records)==sum(s['isCallable'] for s in self.source['symbols'])
  out=k.save_review('financial_integrity',self.path,dict(file_review_status='COMPLETE_STATIC_CALLABLE_REVIEW' if complete else 'PARTIAL_STATIC_CALLABLE_REVIEW',evidence='Manual bounded source read and individual exact span reasoning; no execution.',symbols=self.records))
  ledger=json.loads((k.WORK/'Sub-Agents/Results/financial-ledger.json').read_text(encoding='utf-8-sig'))
  remaining=[r['path'] for r in ledger['remaining'] if not(complete and r['path']==self.path)]
  k.atomic_json(k.WORK/'Sub-Agents/Checkpoints/financial_integrity.json',dict(baseline=k.BASELINE,last_completed_file=self.path if complete else None,last_completed_symbol=self.records[-1]['stable_id'],partial_file=None if complete else self.path,partial_symbol=None,remaining_files=remaining,remaining_function_ids_reference='Knowledge/Source-Symbols exact callable IDs minus ANALYZED in Reviews/financial_integrity. All legacy reports require individual reconciliation; raw presence never proves functions.',findings_reference='Sub-Agents/Results/financial-findings.json',independent_pending=([x['candidate_key'] for x in json.loads((k.WORK/'Sub-Agents/Results/financial-new-candidates.json').read_text(encoding='utf8')).get('candidates',[]) if x.get('independent_pending')] if (k.WORK/'Sub-Agents/Results/financial-new-candidates.json').exists() else []),independent_evidence_reference='Deep-Audit/Database-and-Finance/financial-parent-challenges.json plus Sub-Agents/Handoffs/financial-restore-outcome-challenge.json',blocker=None,next_action=next_action,updated_at_utc=k.now()))
  print('Saved',len(self.records),'callables:',out)