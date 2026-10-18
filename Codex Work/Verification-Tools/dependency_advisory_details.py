"""Only public GitHub advisory metadata; bounded parallel retrieval and atomic files."""
import concurrent.futures,json
from dependency_advisory_collect import fetch_json,OUT

def main():
 candidates=json.loads((OUT/'nuget-advisory-candidate-ranges.json').read_text(encoding='utf8'))
 ids=sorted({p['advisory']['url'].rstrip('/').split('/')[-1] for p in candidates['advisory_range_pairs']})
 def fetch(ghsa):
  record=fetch_json('https://api.github.com/advisories/'+ghsa,'github-'+ghsa+'.json')
  return {'ghsa':ghsa,'status':record['status'],'cve':record.get('data',{}).get('cve_id')}
 with concurrent.futures.ThreadPoolExecutor(max_workers=4) as pool:
  for result in pool.map(fetch,ids):print(json.dumps(result),flush=True)

if __name__=='__main__':main()
