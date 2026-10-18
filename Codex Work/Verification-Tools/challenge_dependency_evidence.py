"""Read-only independent joins/hash checks of saved public dependency evidence.

Writes only a parent quality record. Does not restore packages or load POS code.
"""
import collections
import json
from knowledge_store import ROOT, WORK, atomic_json, now, sha256

OUT = WORK / 'Deep-Audit/Dependency-Security'


def read(name):
    return json.loads((OUT / name).read_text(encoding='utf8'))


def main():
    errors = []
    sources = {}
    for path in (OUT / 'feeds').glob('*.json'):
        d = json.loads(path.read_text(encoding='utf8'))
        if d['status'] != 'RETRIEVED':
            errors.append('Unretrieved source: ' + path.name)
            continue
        if sha256(d['raw_body_utf8'].encode('utf8')) != d['raw_bytes_sha256']:
            errors.append('Body hash: ' + path.name)
        if json.loads(d['raw_body_utf8']) != d['data']:
            errors.append('Parsed body mismatch: ' + path.name)
        sources[d['url']] = d
    feed = collections.defaultdict(list)
    for page in read('feeds/nuget-vulnerability-index.json')['data']:
        if page['@id'] not in sources:
            errors.append('Missing advertised page: ' + page['@id'])
            continue
        for package, advisories in sources[page['@id']]['data'].items():
            feed[package.lower()].extend(advisories)
    sbom = read('SBOM.json')
    assessment = read('dependency-advisory-assessment.json')
    assessed = {c['bom_ref']: c for c in assessment['components']}
    expected_pairs = collections.Counter()
    for c in sbom['components']:
        a = assessed.get(c['bom_ref'])
        if a is None or a['version'] != c['version'] or a['projects'] != c['projects']:
            errors.append('Component mismatch: ' + c['bom_ref'])
        for advisory in feed[c['name'].lower()]:
            expected_pairs[(c['bom_ref'], advisory['url'], advisory['versions'])] += 1
    evaluated = read('nuget-advisory-range-evaluations.json')
    actual_pairs = collections.Counter((v['bom_ref'], v['advisory_url'], v['range'])
                                       for v in evaluated['evaluations'])
    if expected_pairs != actual_pairs:
        errors.append('Feed join/range coverage differs')
    file_checks = []
    for project in sbom['projects']:
        p = ROOT / project['asset_relative_path']
        ok = p.exists() and sha256(p.read_bytes()) == project['asset_sha256']
        file_checks.append({'path': project['asset_relative_path'], 'hash_matches': ok})
        if not ok:
            errors.append('Asset changed: ' + project['path'])
    runtime = read('test-package-runtime-selection-evidence.json')
    runtime_count = 0
    unexpected_assets = []
    for record in runtime['deps_records']:
        p = ROOT / record['path']
        if not p.exists() or sha256(p.read_bytes()) != record['sha256']:
            errors.append('Test manifest changed: ' + record['path'])
            continue
        runtime_count += 1
        d = json.loads(p.read_text(encoding='utf8'))
        for target, libraries in d['targets'].items():
            for name in ('System.Net.Http/4.3.0', 'System.Text.RegularExpressions/4.3.0'):
                entry = libraries.get(name, {})
                if any(entry.get(k) for k in ('runtime', 'runtimeTargets', 'native')):
                    unexpected_assets.append({'path': record['path'], 'target': target, 'library': name})
        for name in ('System.Net.Http.dll', 'System.Text.RegularExpressions.dll'):
            if (p.parent / name).exists():
                unexpected_assets.append({'path': record['path'], 'adjacent_dll': name})
    if unexpected_assets:
        errors.append('Runtime implementation counterevidence changed')
    result = {
        'at_utc': now(), 'reviewer': 'root', 'scope': 'Independent saved-data joins and current file hash/manifest challenge',
        'source_snapshots': len(sources), 'component_count': len(sbom['components']),
        'feed_candidate_pairs_independently_reconstructed': sum(expected_pairs.values()),
        'asset_file_checks': file_checks, 'test_manifests_rechecked': runtime_count,
        'unexpected_old_runtime_assets': unexpected_assets, 'errors': errors,
        'limits': ['No actual loaded-module observation, new restore, signature/license validation, or application process run.',
                   'NuGet and GitHub metadata are related sources, not independently exhaustive vulnerability databases.',
                   'No listed advisory is not a security certificate; old test graph range matches are not production exploits.'],
        'remaining': 'Range parser rerun, current primary-source qualification, licensing/provenance and deployment compatibility assessment remain separate.'}
    atomic_json(WORK / 'Knowledge/Quality-Reviews/dependency-parent-validation.json', result)
    print(json.dumps({k: v for k, v in result.items() if k not in ('asset_file_checks', 'limits')}))
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
