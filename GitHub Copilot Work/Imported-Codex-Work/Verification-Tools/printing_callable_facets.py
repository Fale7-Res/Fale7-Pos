"""Owned additive facets; no original map changes or application execution."""
import json
from knowledge_store import WORK,save_callable_facet_review,atomic_json,now
A='printing_workflows'
def row(reason,inputs,outputs,side,trust,db,ui,perf,dup,issues,tests,refs):
 return dict(reasoning=reason,inputs=inputs,outputs=outputs,side_effects=side,trust_boundaries=trust,database=db,printing_ui=ui,performance=perf,duplicates_conflicts=dup,issues_hypotheses=issues,tests_missing=tests,verified_callers_callees=refs)
def index():return json.loads((WORK/'Knowledge/Source-Symbols-CallableFacets/index.json').read_text(encoding='utf8'))
def batch(entries,label):
 ix=index();groups={}
 for kind,key,detail in entries:
  pool=ix['facets' if kind=='getter' else 'generated_behavior_inputs'];matches=[x for x in pool if x['owner']==A and (x['path'],x['startLine'])==key];assert len(matches)==1,key
  s=matches[0];p=s['path']
  if p not in groups:
   target=WORK/'Knowledge/Reviews-CallableFacets'/A/(p+'.json');old=json.loads(target.read_text(encoding='utf8')) if target.exists() else {};groups[p]={'facets':old.get('facets',[]),'generated_behavior_inputs':old.get('generated_behavior_inputs',[])}
  which='facets' if kind=='getter' else 'generated_behavior_inputs';existing={x['stable_id']:x for x in groups[p][which]};existing[s['stable_id']]={'stable_id':s['stable_id'],'status':'ANALYZED' if kind=='getter' else 'BEHAVIOR_ANALYZED','source_name':s['name'],'source_span':[s['startLine'],s['endLine']],**detail};groups[p][which]=list(existing.values())
 for path,g in groups.items():save_callable_facet_review(A,path,g['facets'],g['generated_behavior_inputs'])
 completed_get=set();completed_gen=set()
 for path in (WORK/'Knowledge/Reviews-CallableFacets'/A).rglob('*.json'):
  j=json.loads(path.read_text(encoding='utf8'));completed_get.update(x['stable_id'] for x in j['facets'] if x['status']=='ANALYZED');completed_gen.update(x['stable_id'] for x in j['generated_behavior_inputs'] if x['status']=='BEHAVIOR_ANALYZED')
 remaining_get=[x for x in ix['facets'] if x['owner']==A and x['stable_id'] not in completed_get];remaining_gen=[x for x in ix['generated_behavior_inputs'] if x['owner']==A and x['stable_id'] not in completed_gen]
 progress={'at_utc':now(),'state':'ACTIVE_ADDITIVE_FACET_REVIEW','last_completed':label,'getter_analyzed':len(completed_get),'generated_context_behavior_analyzed':len(completed_gen),'remaining_getters':remaining_get,'remaining_generated_contexts':remaining_gen,'counting_rule':'Generated context behavior is not a count of CLR methods;original388 and accepted130 records untouched.'}
 atomic_json(WORK/'Sub-Agents/Results/printing-callable-facet-progress.json',progress)
 cp=WORK/'Sub-Agents/Checkpoints/printing_workflows.json';c=json.loads(cp.read_text(encoding='utf8'));c['updated_at_utc']=now();c['additive_callable_facet_assignment']={'state':progress['state'],'last_completed':label,'getter_analyzed':len(completed_get),'getter_remaining':len(remaining_get),'generated_context_behavior_analyzed':len(completed_gen),'generated_context_remaining':len(remaining_gen),'exact_remaining_reference':'Sub-Agents/Results/printing-callable-facet-progress.json','next_action':'Continue actual per-record contract and caller/serialization/copy behavior;no blanket context promotion.','runtime_actions':False};atomic_json(cp,c)
 print(label,{'getters':len(completed_get),'getters_remaining':len(remaining_get),'contexts':len(completed_gen),'contexts_remaining':len(remaining_gen)})
