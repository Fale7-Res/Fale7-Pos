"""Consolidate public advisory metadata with static SBOM/runtime-manifest evidence."""
import collections,hashlib,json
from knowledge_store import WORK,ROOT,atomic_json,now
OUT=WORK/'Deep-Audit/Dependency-Security'
def read(name):return json.loads((OUT/name).read_text(encoding='utf8'))
def hashfile(path):return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
 sbom=read('SBOM.json');feed=read('nuget-advisory-range-evaluations.json');github=read('github-advisory-range-evaluations.json');candidates=read('nuget-advisory-candidate-ranges.json');runtime=read('test-package-runtime-selection-evidence.json')
 pairs=collections.defaultdict(list)
 for p in candidates['advisory_range_pairs']:pairs[p['bom_ref']].append(p)
 gh={}
 for p in sorted((OUT/'feeds').glob('github-GHSA-*.json')):
  d=json.loads(p.read_text(encoding='utf8'));gh[d['data']['html_url']]=d
 def sums(evaluations):
  d={}
  for x in evaluations:
   if x['status']=='EVALUATED_WITH_NUGET_VERSIONING':d[(x['bom_ref'],x['advisory_url'])]=d.get((x['bom_ref'],x['advisory_url']),False) or x['matches']
  return d
 feed_sums=sums(feed['evaluations']);github_sums=sums(github['evaluations']);disagreement=[{'bom_ref':key[0],'url':key[1],'feed':feed_sums.get(key),'github':github_sums.get(key)} for key in feed_sums.keys()|github_sums.keys() if feed_sums.get(key)!=github_sums.get(key)]
 assert not disagreement
 source_records=[];gaps=[]
 for path in sorted((OUT/'feeds').glob('*.json')):
  doc=json.loads(path.read_text(encoding='utf8'))
  if doc['status']!='RETRIEVED':gaps.append({'source':doc['url'],'error':doc.get('error')});continue
  assert hashlib.sha256(doc['raw_body_utf8'].encode('utf8')).hexdigest()==doc['raw_bytes_sha256']
  source_records.append({'url':doc['url'],'retrieved_at_utc':doc['retrieved_at_utc'],'decoded_body_sha256':doc['raw_bytes_sha256'],'transport_sha256':doc.get('transport_bytes_sha256'),'saved_json_sha256':hashfile(path),'saved_path':str(path.relative_to(WORK)),'headers':doc.get('headers',{})})
 components=[]
 for c in sbom['components']:
  checks=[]
  for row in feed['evaluations']:
   if row['bom_ref']!=c['bom_ref']:continue
   g=gh[row['advisory_url']]['data'];gv=[v for v in g['vulnerabilities'] if v['package']['ecosystem'].lower()=='nuget' and v['package']['name'].lower()==c['name'].lower()]
   checks.append({'advisory_url':row['advisory_url'],'ghsa_id':g['ghsa_id'],'cve_id':g.get('cve_id'),'summary':g['summary'],'feed_range':row['range'],'severity_feed':row['severity'],'severity_github':g['severity'],'nuget_range_matches':row.get('matches'),'github_any_range_matches':github_sums.get((c['bom_ref'],row['advisory_url'])),'github_ranges':[v['vulnerable_version_range'] for v in gv],'first_patched_versions':[v['first_patched_version'] for v in gv],'withdrawn_at':g.get('withdrawn_at'),'published_at':g['published_at'],'updated_at':g['updated_at'],'disposition':'PACKAGE_VERSION_RANGE_MATCH' if row.get('matches') else 'OUTSIDE_THIS_LISTED_RANGE','range_evaluation_status':row['status']})
  match=[v for v in checks if v['nuget_range_matches'] and not v['withdrawn_at']]
  testonly=all(p['path'].startswith('tests/') for p in c['projects'])
  runtime_evidence={'actual_loaded_assembly':'NOT_OBSERVED','framework_or_native_servicing':'Not established from NuGet package version;source/runtime/native identity may differ'}
  if match:
   runtime_evidence.update({'static_context':'TEST_ONLY_TRANSITIVE_RESTORE_GRAPH','dependency_chains':[x for x in runtime['assets'] if x['library']==c['name']+'/'+c['version']],'existing_test_deps_manifest_count':runtime['existing_test_deps_count'],'old_package_runtime_entries_in_existing_test_deps':False,'old_adjacent_implementation_dll_seen':False,'interpretation':'Matched legacy package appears in three net8 test restore graphs through xunit.assert->NETStandard.Library. All61 existing own-test deps omit runtime/native/runtimeTargets for both packages and no adjacent implementations exist. Static evidence counters an asserted production loaded vulnerability;actual test/app process assembly resolution remains unobserved.'})
  components.append({'bom_ref':c['bom_ref'],'name':c['name'],'version':c['version'],'projects':c['projects'],'relationship_classes':sorted({p['relationship'] for p in c['projects']}),'scope':'TEST_ONLY' if testonly else 'INCLUDES_APPLICATION_OR_TOOL_PROJECT','status':'MATCHED_PACKAGE_RANGE_TEST_ONLY_RUNTIME_NOT_ESTABLISHED' if match and testonly else 'MATCHED_PACKAGE_RANGE_RUNTIME_NOT_ESTABLISHED' if match else 'NO_MATCH_IN_LISTED_ADVISORY_RANGES' if checks else 'PACKAGE_ID_NOT_LISTED_IN_RETRIEVED_NUGET_FEED','advisory_checks':checks,'runtime_relevance':runtime_evidence,'not_a_safety_certificate':True})
 index=read('feeds/nuget-vulnerability-index.json')
 rangegaps=[x for x in feed['evaluations']+github['evaluations'] if x['status']!='EVALUATED_WITH_NUGET_VERSIONING']+github.get('conversion_gaps',[])
 statuscounts=dict(collections.Counter(x['status'] for x in components));matched=[x for x in components if x['status'].startswith('MATCHED')]
 evidence={'schema_version':1,'reviewer':'printing_workflows','baseline':sbom['baseline'],'assessed_at_utc':now(),'sbom_path':'Deep-Audit/Dependency-Security/SBOM.json','sbom_sha256':hashfile(OUT/'SBOM.json'),'component_count':len(components),'unique_package_ids_case_insensitive':len({x['name'].lower() for x in components}),'completion':'ALL210_COMPONENTS_CHECKED_AGAINST_ALL_ADVERTISED_CURRENT_NUGET_PAGES','method':'Case-insensitive package-ID join;union all NuGet VulnerabilityInfo pages;NuGet.Versioning6.10 VersionRange.Satisfies for every candidate version/range;independent GitHub comparator translation evaluated by same established NuGet engine;metadata crosscheck,not independent database discovery. No package install/restore/build/app/test/auth/DB/printer operations.','advertised_feed_pages':index['data'],'source_records':source_records,'range_engine':{k:feed[k] for k in ('assembly_path','assembly_sha256','assembly_full_name','tests')},'nuget_range_evaluations':len(feed['evaluations']),'github_range_evaluations':len(github['evaluations']),'github_advisories_retrieved':len(gh),'feed_github_package_advisory_match_disagreements':disagreement,'feed_access_gaps':gaps,'range_evaluation_gaps':rangegaps,'status_counts':statuscounts,'matched_component_refs':[x['bom_ref'] for x in matched],'confirmed_production_loaded_vulnerabilities':0,'confirmed_production_loaded_vulnerabilities_meaning':'No production-loaded exploitability established;this count is not a finding of absence.','runtime_evidence_path':'Deep-Audit/Dependency-Security/test-package-runtime-selection-evidence.json','preserved_parent_specific_checks':{'path':'Deep-Audit/Dependency-Security/advisory-checks.json','sha256':hashfile(OUT/'advisory-checks.json'),'overwritten':False},'components':components,'residual_coverage_gaps':['NuGet feed describes known curated advisories only;absence is not risk-free or zero-day proof. GitHub metadata is underlying/related data,not a separate exhaustive independent source.','SBOM uses existing local assets,not fresh reproducible locked restore or actual final publish. Allsix matched asset rows still hash-match SBOM.','No actual test or application process assembly load observed. Existing61 test output manifests provide static runtime-selection counterevidence only;other deployment/environment/custom AssemblyLoadContext can differ.','Installed shared .NET/ASP.NET/WindowsDesktop runtime servicing,Windows App SDK/VC/native transitive binaries,OS/drivers/SQL/MSBuild host vulnerabilities require separate inventory/advisory comparison.','No package file bytes/signatures/provenance/license or supply-chain integrity review performed by advisory lookup.','Public MSRC pages require JavaScript;equivalent public MSRC CVRF JSON for both matched CVEs successfully retrieved and preserved.']}
 assert len(components)==210 and not gaps and not rangegaps
 atomic_json(OUT/'dependency-advisory-assessment.json',evidence)
 cpath=WORK/'Sub-Agents/Checkpoints/printing_workflows.json';c=json.loads(cpath.read_text(encoding='utf8'));c['updated_at_utc']=now();c['dependency_advisory_assignment']={'state':'METADATA_ASSESSMENT_COMPLETE_RUNTIME_SCOPE_EXPLICIT','components_checked':210,'nuget_candidate_ranges_evaluated':len(feed['evaluations']),'github_ranges_crosschecked':len(github['evaluations']),'known_package_range_matches':len(matched),'confirmed_production_runtime_matches':'NOT_ESTABLISHED','inaccessible_feeds':[],'exact_remaining_component_refs':[],'last_completed':'All210 NuGet feed checks,24 GitHub details,2 MSRC CVRFs,61 static test deps/6 restore target rows','evidence':['Deep-Audit/Dependency-Security/dependency-advisory-assessment.json','Deep-Audit/Dependency-Security/test-package-runtime-selection-evidence.json'],'next_action':'Parent independently assess loaded-framework context if desired,incorporate limited package-range results into phase4;original130 route quality remains pending independent acceptance.'};atomic_json(cpath,c)
 print(json.dumps({'components':210,'statuses':statuscounts,'nuget_ranges':len(feed['evaluations']),'github_ranges':len(github['evaluations']),'package_range_matches':len(matched),'feeds':len(source_records),'no_feed_or_range_gaps':not gaps and not rangegaps}))

if __name__=='__main__':main()
