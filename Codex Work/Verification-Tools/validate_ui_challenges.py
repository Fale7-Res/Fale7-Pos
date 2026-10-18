"""Validate independent reviewer source identities; never certifies reasoning depth."""
import json
from knowledge_store import ROOT, WORK, BASELINE, atomic_json, now, sha256, load_symbols


def main():
    reports = []
    folder=WORK / 'Deep-Audit/UI-UX/Independent-Challenges'
    candidates=sorted(folder.glob('UI-*.json'))
    if (folder/'SEC-002.json').exists():candidates.append(folder/'SEC-002.json')
    for path in candidates:
        doc = json.loads(path.read_text(encoding='utf8'))
        errors = []
        for evidence in doc['evidence']:
            source = ROOT / evidence['path']
            indexed = load_symbols(evidence['path'])
            if sha256(source.read_bytes()) != evidence['worktree_sha256']:
                errors.append('Raw source hash: ' + evidence['path'])
            if indexed['git_blob_sha256'] != evidence['source_git_blob_sha256']:
                errors.append('Baseline hash: ' + evidence['path'])
            count = len(source.read_text(encoding='utf-8-sig').splitlines())
            if not 1 <= evidence['startLine'] <= evidence['endLine'] <= count:
                errors.append('Evidence range: ' + evidence['path'])
            symbol = evidence.get('symbol')
            if symbol:
                match = next((s for s in indexed['symbols'] if s['stable_id'] == symbol['stable_id']), None)
                if not match or any(match[k] != symbol[k] for k in ['kind', 'startLine', 'endLine', 'startColumn', 'endColumn']):
                    errors.append('Exact symbol identity: ' + evidence['path'])
        if doc.get('baseline') != BASELINE:
            errors.append('Baseline mismatch')
        reports.append({'id': doc.get('id',doc.get('candidate_id')), 'artifact': str(path.relative_to(WORK)).replace('\\', '/'),
                        'artifact_sha256': sha256(path.read_bytes()), 'source_references': len(doc['evidence']),
                        'disposition': doc['disposition'], 'severity_qualification': doc.get('severity_assessment',doc.get('severity')),
                        'errors': errors})
    result = {'at_utc': now(), 'reviewer': 'root', 'reports': reports,
              'errors': [e for r in reports for e in r['errors']],
              'scope': 'Identity/range/hash integrity plus preserved independent dispositions; no runtime or exhaustive reasoning-depth certificate.',
              'parent_focused_source_challenges': ['UI020 shared-bearer proof chain', 'UI018 current detail and independent editor data race',
                                                   'UI021 image completion, mutable request target and backend unmatched variant creation'],
              'limits': 'Other finding-local source reasoning belongs to independent reviewer, with native/runtime uncertainty explicitly retained.'}
    atomic_json(WORK / 'Knowledge/Quality-Reviews/ui-independent-challenge-validation.json', result)
    print(json.dumps({'reports': len(reports), 'source_references': sum(r['source_references'] for r in reports), 'errors': result['errors']}))
    if result['errors']:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
