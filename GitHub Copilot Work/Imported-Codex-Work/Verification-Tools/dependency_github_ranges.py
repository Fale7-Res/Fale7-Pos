"""Translate only observed, strictly parsed comparator syntax; NuGet evaluates versions."""
import json,re,subprocess
from knowledge_store import WORK,atomic_json,now
ROOT=WORK/'Deep-Audit/Dependency-Security'

def convert(value):
 tokens=[x.strip() for x in value.split(',')];lower=upper=None;lo='(';hi=')'
 for token in tokens:
  m=re.fullmatch(r'(>=|<=|>|<|=)\s*([0-9][0-9A-Za-z.+-]*)',token)
  if not m:raise ValueError('Unsupported comparator syntax: '+value)
  op,v=m.groups()
  if op=='=':
   if len(tokens)!=1:raise ValueError('Mixed exact comparator')
   return '['+v+']'
  if op in ('>=','>'):
   if lower is not None:raise ValueError('Duplicate lower bound')
   lower=v;lo='[' if op=='>=' else '('
  else:
   if upper is not None:raise ValueError('Duplicate upper bound')
   upper=v;hi=']' if op=='<=' else ')'
 return lo+(lower or '')+','+(upper or '')+hi

def main():
 assert convert('>= 4.3.0, < 4.3.1')=='[4.3.0,4.3.1)'
 assert convert('= 5.0.0')=='[5.0.0]'
 assert convert('<= 1.1.3')=='(,1.1.3]'
 assert convert('>= 2.0.0-preview.11, <= 2.7.4')=='[2.0.0-preview.11,2.7.4]'
 sbom=json.loads((ROOT/'SBOM.json').read_text(encoding='utf8'));pairs=[];gaps=[]
 for path in sorted((ROOT/'feeds').glob('github-GHSA-*.json')):
  source=json.loads(path.read_text(encoding='utf8'))
  if source['status']!='RETRIEVED':gaps.append({'path':str(path),'reason':'GitHub inaccessible'});continue
  d=source['data']
  for vulnerability in d.get('vulnerabilities',[]):
   package=vulnerability['package']
   if package['ecosystem'].lower()!='nuget':continue
   for c in sbom['components']:
    if c['name'].lower()!=package['name'].lower():continue
    try:nrange=convert(vulnerability['vulnerable_version_range'])
    except ValueError as exc:gaps.append({'bom_ref':c['bom_ref'],'advisory':d['ghsa_id'],'reason':str(exc)});continue
    pairs.append({'bom_ref':c['bom_ref'],'package':c['name'],'version':c['version'],'advisory':{'url':d['html_url'],'versions':nrange,'severity':d['severity']},'original_github_range':vulnerability['vulnerable_version_range'],'first_patched_version':vulnerability['first_patched_version'],'withdrawn_at':d.get('withdrawn_at')})
 inputpath=ROOT/'github-advisory-candidate-ranges.json';atomic_json(inputpath,{'advisory_range_pairs':pairs,'conversion_gaps':gaps,'generated_at_utc':now()})
 r=subprocess.run(['pwsh','-NoProfile','-File',str(WORK/'Verification-Tools/dependency_nuget_ranges.ps1'),'-InputPath',str(inputpath)],capture_output=True,text=True,encoding='utf8',check=True)
 result=json.loads(r.stdout);result.update({'evaluated_at_utc':now(),'conversion_gaps':gaps,'parser_conversion_tests':4})
 atomic_json(ROOT/'github-advisory-range-evaluations.json',result)
 print(json.dumps({'github_range_pairs':len(pairs),'conversion_gaps':gaps,'matches':[x for x in result['evaluations'] if x.get('matches')]}))

if __name__=='__main__':main()
