"""Retrieve public advisory JSON only; no restore/package/app execution."""
import gzip,hashlib,json,urllib.request
from pathlib import Path
from knowledge_store import WORK,atomic_json,now
OUT=WORK/'Deep-Audit/Dependency-Security'

def fetch_json(url,name):
 record={'url':url,'retrieved_at_utc':now()}
 try:
  req=urllib.request.Request(url,headers={'User-Agent':'Fale7-audit-public-advisory-metadata','Accept':'application/json'})
  with urllib.request.urlopen(req,timeout=30) as response:
   wire=response.read();raw=gzip.decompress(wire) if wire[:2]==b'\x1f\x8b' else wire
   record.update({'http_status':response.status,'headers':{k:v for k,v in response.headers.items() if k.lower() in ('date','last-modified','etag','content-type','content-encoding')},'raw_body_utf8':raw.decode('utf-8-sig'),'raw_bytes_sha256':hashlib.sha256(raw).hexdigest(),'transport_bytes_sha256':hashlib.sha256(wire).hexdigest(),'decompression':'gzip' if raw!=wire else 'none','data':json.loads(raw)})
  record['status']='RETRIEVED'
 except Exception as exc:record.update({'status':'INACCESSIBLE_COVERAGE_GAP','error_type':type(exc).__name__,'error':str(exc)})
 atomic_json(OUT/'feeds'/name,record)
 return record

def main():
 service=fetch_json('https://api.nuget.org/v3/index.json','nuget-service-index.json')
 if service['status']!='RETRIEVED':return
 resources=[r for r in service['data']['resources'] if r.get('@type','').startswith('VulnerabilityInfo/')]
 assert len(resources)==1
 index=fetch_json(resources[0]['@id'],'nuget-vulnerability-index.json')
 if index['status']!='RETRIEVED':return
 for n,page in enumerate(index['data']):
  record=fetch_json(page['@id'],f'nuget-vulnerability-page-{n}.json')
  print(json.dumps({'page':n,'source':page,'status':record['status'],'package_keys':len(record.get('data',{}))}))

if __name__=='__main__':main()
