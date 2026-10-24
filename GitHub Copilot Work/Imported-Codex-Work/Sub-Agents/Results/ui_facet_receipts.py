from ui_source_receipts import *
from knowledge_store import save_callable_facet_review, atomic_text

def persist_facets(path,getters,contexts):
 source=json.loads((ROOT/'Codex Work/Knowledge/Source-Symbols-CallableFacets/Files'/Path(path+'.json')).read_text())
 target=ROOT/'Codex Work/Knowledge/Reviews-CallableFacets/ui_visual'/Path(path+'.json')
 old=json.loads(target.read_text()) if target.exists() else {}
 def append(entries,facts,prior,status):
  result=list(prior);seen={r['stable_id'] for r in result}
  for key,(reason,links,missing) in facts.items():
   s,=[s for s in entries if (s['startLine'],s['startColumn'])==key]
   assert s['stable_id'] not in seen
   result.append(dict(stable_id=s['stable_id'],status=status,exact_signature=s['qualifiedSymbol'],exact_kind=s['kind'],exact_span=[*key,s['endLine'],s['endColumn']],reasoning=reason,inputs='Exact source expression/field/attribute and owning state described in reasoning',outputs='Getter result or contextual generated API semantics described in reasoning',side_effects='Source-only audit; no application/native/authentication/DB/printer operation performed.',trust_boundaries='Presentation/generated command affordances are not authoritative backend validation or permission enforcement.',database='No database operations executed; source HTTP effects described only.',printing_ui='Native layout/accessibility/theme/printing NOT_OBSERVED by this reviewer.',performance='Per-expression enumeration/allocation or event/command work described; no profiling.',duplicates_conflicts='Additive facets preserve original exact source receipts; generated contexts not individually counted authored methods.',issues_hypotheses=missing,tests_missing=missing+' NOT_EXECUTED.',verified_callers_callees=links))
  return result
 a=append(source['facets'],getters,old.get('facets',[]),'ANALYZED')
 b=append(source['generated_behavior_inputs'],contexts,old.get('generated_behavior_inputs',[]),'BEHAVIOR_ANALYZED')
 save_callable_facet_review('ui_visual',path,a,b)
 checkpoint_facets(path,(b or a)[-1]['stable_id'])
 print(path,len(a),len(b))

def checkpoint_facets(path=None,sid=None):
 index=json.loads((ROOT/'Codex Work/Knowledge/Source-Symbols-CallableFacets/index.json').read_text())
 maps={k:[s for s in index[k] if s['owner']=='ui_visual'] for k in ['facets','generated_behavior_inputs']}
 done={k:set() for k in maps}
 for p in (ROOT/'Codex Work/Knowledge/Reviews-CallableFacets/ui_visual').rglob('*.json'):
  x=json.loads(p.read_text())
  for k in maps:done[k].update(r['stable_id'] for r in x.get(k,[]) if r.get('status') in ['ANALYZED','BEHAVIOR_ANALYZED'])
 pending={k:[s for s in maps[k] if s['stable_id'] not in done[k]] for k in maps}
 out=ROOT/'Codex Work/Sub-Agents/Results/ui-additive-facet-coverage.json'
 atomic_json(out,dict(at_utc=now(),original_syntax_ids=dict(analyzed=2461,total=2461),counts={k:dict(total=len(maps[k]),analyzed=len(done[k]),remaining=len(pending[k])) for k in maps},remaining=pending,limitations='Original syntax IDs exclude additive expression getters; generated contexts counted separately, not authored methods. Native NOT_OBSERVED.'))
 cp=ROOT/'Codex Work/Sub-Agents/Checkpoints/ui_visual.json';c=json.loads(cp.read_text());c.update(updated_at_utc=now(),additive_facet_counts={k:dict(total=len(maps[k]),analyzed=len(done[k]),remaining=len(pending[k])) for k in maps},additive_remaining_reference=str(out.relative_to(ROOT)),next_action='Review next exact additive getter/generated-context source from ui-additive-facet-coverage.json; original syntax queue2461 complete is not exhaustive mission completion.')
 if path:c.update(last_additive_file=path,last_additive_stable_id=sid)
 nxt=(pending['facets']+pending['generated_behavior_inputs'])
 c['NEXT_ACTION_CURRENT_ADDITIVE']=nxt[0] if nxt else None
 c['NEXT_ACTION_CURRENT']=dict(action='Continue additive source reasoning; original2461 syntax IDs complete, not exhaustive behavioral coverage.', additive_next=nxt[0] if nxt else None, original_remaining=0, additive_remaining=sum(len(v) for v in pending.values()), remainder_manifest=str(out.relative_to(ROOT)))
 atomic_json(cp,c)
 atomic_text(ROOT/'Codex Work/Deep-Audit/UI-UX/ui-persistent-status.md',f"""# UI source review checkpoint

Baseline c35816ecae49633674775dd32061c6ee92f94aaf. All 274 scoped files retain manual static reports.

Original syntax-callable IDs: 2461/2461, across 225 source receipt shards. That predicate was incomplete for application behavior. Additive expression getters: {len(done['facets'])}/{len(maps['facets'])}; generated behavior contexts: {len(done['generated_behavior_inputs'])}/{len(maps['generated_behavior_inputs'])}. These are separate counts; generated contexts are not individually counted authored/CLR methods.

Exact remaining IDs and next source: Codex Work/Sub-Agents/Results/ui-additive-facet-coverage.json and Sub-Agents/Checkpoints/ui_visual.json. No old name-heading promotion.

Native visuals/accessibility/theme are NOT_OBSERVED by this reviewer. Test sources reviewed, tests NOT_EXECUTED. No application/source/configuration, runtime/session/authentication, database, physical printer or Git operation performed. Parent owns canonical findings, independent challenges and final mission gates.
""")
