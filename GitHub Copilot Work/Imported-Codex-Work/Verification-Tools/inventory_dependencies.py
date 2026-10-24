"""Read existing restore assets only; no restore, install, package code or network execution."""
from knowledge_store import *
import subprocess,xml.etree.ElementTree as ET
paths=[p for p in subprocess.check_output(['git','ls-tree','-r','--name-only',BASELINE],cwd=ROOT,text=True).splitlines() if p.endswith('.csproj')]
projects=[];components={};edges=set();missing=[]
for path in paths:
    source=ROOT/path;xml=ET.parse(source).getroot()
    direct={r.get('Include'):r.get('Version') or r.findtext('Version') for r in xml.iter('PackageReference') if r.get('Include')}
    refs=[r.get('Include') for r in xml.iter('ProjectReference')]
    asset=source.parent/'obj/project.assets.json'
    project={'path':path,'source_sha256':sha256(source.read_bytes()),'frameworks':[n.text for tag in ('TargetFramework','TargetFrameworks') for n in xml.iter(tag)],
             'direct_package_requirements':direct,'project_reference_requirements':refs,'restore_asset_status':'MISSING','advisory_status':'NOT_ANALYZED'}
    if not asset.exists():missing.append(path);projects.append(project);continue
    data=asset.read_bytes();j=json.loads(data.decode('utf-8-sig'))
    project.update(restore_asset_status='EXISTING_LOCAL_ASSET_PARSED_NOT_FRESH_RESTORE',asset_relative_path=str(asset.relative_to(ROOT)).replace('\\','/'),asset_sha256=sha256(data),target_names=list(j['targets']))
    for key,lib in j['libraries'].items():
        if lib.get('type')!='package':continue
        name,version=key.rsplit('/',1);cid='pkg:nuget/'+name+'@'+version
        item=components.setdefault(cid,{'bom_ref':cid,'name':name,'version':version,'type':'NuGet package','nuget_content_sha512_metadata':lib.get('sha512'),
            'projects':[],'license':'UNKNOWN_NOT_CHECKED','known_vulnerability_status':'NOT_ANALYZED','content_integrity':'Asset SHA512 metadata only; downloaded bytes not independently hashed'})
        item['projects'].append({'path':path,'relationship':'DIRECT' if name in direct else 'TRANSITIVE'})
    for target,libs in j['targets'].items():
        for key,lib in libs.items():
            if lib.get('type')!='package':continue
            name,version=key.rsplit('/',1);parent='pkg:nuget/'+name+'@'+version
            for dep,constraint in lib.get('dependencies',{}).items():edges.add((parent,dep,str(constraint),path,target))
    projects.append(project)
atomic_json(WORK/'Deep-Audit/Dependency-Security/SBOM.json',{'schema_version':1,'format':'Explicit NuGet/project inventory, not certified CycloneDX/SPDX','baseline':BASELINE,'generated_at_utc':now(),
    'method':'Tracked csproj XML + already existing project.assets.json; no source/config edits, restore, build, package install or DB operations',
    'coverage_limits':['Framework-servicing components, installed native binaries and build-host tools require separate inventory/advisory review','Restore assets are local generated evidence and may not describe a reproducible locked deployment','License, signature/content integrity and advisory checks remain incomplete'],
    'projects':projects,'missing_assets':missing,'components':list(components.values()),'dependency_edges':[{'from':a,'dependency_name':b,'required_version':c,'project':d,'target':e} for a,b,c,d,e in sorted(edges)]})
print(json.dumps({'projects':len(projects),'missing_assets':missing,'package_name_version_components':len(components),'dependency_edges':len(edges),'advisories_checked':0}))
