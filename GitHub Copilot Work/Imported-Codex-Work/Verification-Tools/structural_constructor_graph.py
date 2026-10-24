"""Static declared-constructor graph for actual source DI registrations. No service instantiated."""
from knowledge_store import *
import re,collections
program='src/Falah.Server/Program.cs';text=(ROOT/program).read_text(encoding='utf-8-sig')
declared={};constructors=collections.defaultdict(list);source_by_path={}
for file in (WORK/'Knowledge/Source-Symbols/src').rglob('*.cs.json'):
 source=json.loads(file.read_text(encoding='utf8'));source_by_path[source['path']]=source
 for s in source['symbols']:
  q=s.get('qualifiedSymbol') or ''
  if s['kind'] in ('ClassDeclaration','InterfaceDeclaration') and q:
   declared[q]=source['path']
  if s['kind']=='ConstructorDeclaration' and '(' in q:
   parent=q.split('(',1)[0].rsplit('.',1)[0];constructors[parent].append((source,s))
def resolve(name):
 name=name.strip().replace('global::','').rstrip('?')
 if name in declared:return name
 matches=[q for q in declared if q.split('.')[-1]==name]
 return matches[0] if len(matches)==1 else None
def params(signature):
 values=signature.split('(',1)[1].rsplit(')',1)[0];result=[];depth=0;start=0
 for i,c in enumerate(values):
  if c in '<[(':depth+=1
  elif c in '>])':depth-=1
  elif c==',' and depth==0:result.append(values[start:i].strip());start=i+1
 if values.strip():result.append(values[start:].strip())
 return result
registrations=[];aliases={};unresolved=[]
for match in re.finditer(r'builder\.Services\.Add(Scoped|Singleton|Transient|HostedService)<([^>]+)>',text):
 names=[x.strip() for x in match.group(2).split(',')];service=resolve(names[0]);implementation=resolve(names[-1])
 row={'lifetime':match.group(1),'line':text.count('\n',0,match.start())+1,'source_service':names[0],'source_implementation':names[-1],'service':service,'implementation':implementation}
 registrations.append(row)
 if service and implementation:aliases[service]=implementation
 else:unresolved.append(row)
graph={impl:[] for impl in aliases.values()};nodes=[];verified_paths=set()
for impl in graph:
 ctors=constructors.get(impl,[]);entries=[]
 for source,s in ctors:
  path=source['path'];verified_paths.add(path)
  assert sha256((ROOT/path).read_bytes())==source['worktree_sha256']
  args=[]
  for parameter in params(s['qualifiedSymbol']):
   target=resolve(parameter);implementation=aliases.get(target)
   args.append({'declared_type':parameter,'resolved_source_type':target,'registered_implementation':implementation})
   if implementation:graph[impl].append(implementation)
  entries.append({'stable_id':s['stable_id'],'signature':s['qualifiedSymbol'],'lines':[s['startLine'],s['endLine']],'path':path,'worktree_sha256':source['worktree_sha256'],'semantic_binding_status':s['semanticStatus'],'parameters':args})
 nodes.append({'implementation':impl,'source_path':declared.get(impl),'declared_constructors':entries,'no_declared_constructor':'Implicit/generated or missing resolved declaration; not proof zero dependencies' if not entries else None})
cycles=[];active=set();done=set()
def walk(node,path):
 if node in active:cycles.append(path+[node]);return
 if node in done:return
 active.add(node)
 for child in graph.get(node,[]):walk(child,path+[node])
 active.remove(node);done.add(node)
for node in graph:walk(node,[])
service_counts=collections.Counter(r['service'] for r in registrations if r['service'])
payload={'at_utc':now(),'baseline':BASELINE,'program_path':program,'program_worktree_sha256':sha256((ROOT/program).read_bytes()),
 'registered_source_entries':len(registrations),'registered_unique_source_implementations':len(graph),'constructor_source_files_hash_checked':len(verified_paths),
 'registrations':registrations,'nodes':nodes,'edges':graph,'detected_cycles':cycles,'unresolved_registration_entries':unresolved,
 'repeated_service_registrations':{k:v for k,v in service_counts.items() if v>1},
 'scope':'Mechanical structural reference analysis, not new reviewed functions, full DI activation proof or unused-service verdict.',
 'limits':['Only explicit simple generic AddScoped/Singleton/Transient/HostedService registrations. DbContext/framework/extension registration and runtime service factories not exhaustively modeled.',
 'Source symbols resolve in partial compilation; ambiguous names stay unresolved. All declared constructors considered, not actual DI constructor selection.',
 'IServiceScopeFactory, service location, callback/delegate dependencies, methods constructing services and external framework object graphs are outside these edges.',
 'No container, backend, migrations, seeder, worker, database, network or printer started. No finding inferred merely from graph node existence.']}
atomic_json(WORK/'Deep-Audit/Duplication-and-Architecture/constructor-service-reference-graph.json',payload)
print(json.dumps({k:v for k,v in payload.items() if k in ('registered_source_entries','registered_unique_source_implementations','constructor_source_files_hash_checked','detected_cycles','unresolved_registration_entries','repeated_service_registrations')}))
