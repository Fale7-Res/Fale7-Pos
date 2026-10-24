"""Read installed NuGet nuspec metadata only; never installs or executes packages."""
import json
import xml.etree.ElementTree as ET
from urllib.parse import urlsplit
from knowledge_store import ROOT, WORK, atomic_json, now, sha256


def main():
    out = WORK / 'Deep-Audit/Dependency-Security'
    sbom = json.loads((out / 'SBOM.json').read_text(encoding='utf8'))
    folders = []
    for project in sbom['projects']:
        doc = json.loads((ROOT / project['asset_relative_path']).read_text(encoding='utf8'))
        for folder in doc.get('packageFolders', {}):
            if folder not in folders:
                folders.append(folder)
    from pathlib import Path
    records = []
    for c in sbom['components']:
        candidates = [Path(f) / c['name'].lower() / c['version'].lower() /
                      (c['name'].lower() + '.nuspec') for f in folders]
        source = next((p for p in candidates if p.is_file()), None)
        record = {'bom_ref': c['bom_ref'], 'status': 'LOCAL_NUSPEC_NOT_FOUND'}
        if source:
            raw = source.read_bytes()
            root = ET.fromstring(raw)
            nodes = {node.tag.rsplit('}', 1)[-1]: node for node in root.iter()}
            license_node = nodes.get('license')
            repository = nodes.get('repository')
            old_url = nodes.get('licenseUrl')
            record.update({
                'status': 'LOCAL_PACKAGE_METADATA_READ', 'nuspec_sha256': sha256(raw),
                'license_type': license_node.get('type') if license_node is not None else None,
                'license_value': license_node.text if license_node is not None else None,
                'legacy_license_url_host': urlsplit(old_url.text).hostname if old_url is not None and old_url.text else None,
                'repository_type': repository.get('type') if repository is not None else None,
                'repository_host': urlsplit(repository.get('url', '')).hostname if repository is not None else None,
                'repository_commit_declared': bool(repository is not None and repository.get('commit')),
                'signature_file_present': (source.parent / '.signature.p7s').is_file(),
                'signature_validation': 'NOT_EXECUTED',
                'license_interpretation': 'Declared metadata only; file licenses/EULAs and redistribution obligations require content review.'})
            if license_node is not None and license_node.get('type') == 'file':
                target = (source.parent / (license_node.text or '')).resolve()
                if target.is_relative_to(source.parent.resolve()) and target.is_file():
                    record['license_file_sha256'] = sha256(target.read_bytes())
                    record['license_file_content_review'] = 'NOT_ANALYZED'
        records.append(record)
    from collections import Counter
    result = {'at_utc': now(), 'method': 'Read local expanded package nuspec and contained license-file hashes. No restore/network/package execution.',
              'components': records, 'counts': dict(Counter(r['status'] for r in records)),
              'license_types': dict(Counter(str(r.get('license_type')) for r in records)),
              'limits': ['Package metadata can lie; no signature/certificate/byte provenance validation.',
                         'Declared license expression is not legal clearance; file licenses are hashed but not yet individually interpreted.',
                         'No license change, redistribution, fork or package upgrade performed.']}
    atomic_json(out / 'package-license-metadata.json', result)
    print(json.dumps({k: v for k, v in result.items() if k not in ('components', 'limits')}))


if __name__ == '__main__':
    main()
