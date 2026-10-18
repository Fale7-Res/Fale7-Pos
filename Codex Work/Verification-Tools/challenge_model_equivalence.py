"""Independently verify persisted exact-source reuse, without certifying semantic depth."""
import json
from knowledge_store import ROOT, WORK, atomic_json, now, sha256, load_symbols


def span(path, symbol):
    lines = (ROOT / path).read_text(encoding='utf-8-sig').splitlines(keepends=True)
    first, last = symbol['startLine'] - 1, symbol['endLine'] - 1
    # Roslyn columns count UTF-16 units, not Python Unicode scalars.
    def part(line, begin, end=None):
        return line.encode('utf-16-le')[begin * 2:None if end is None else end * 2].decode('utf-16-le')
    if first == last:
        return part(lines[first], symbol['startColumn'] - 1, symbol['endColumn'] - 1)
    return (part(lines[first], symbol['startColumn'] - 1) + ''.join(lines[first + 1:last])
            + part(lines[last], 0, symbol['endColumn'] - 1))


def main():
    ledger = json.loads((WORK / 'Sub-Agents/Results/financial-model-equivalence-ledger.json').read_text(encoding='utf8'))
    cache = {}
    errors, checks = [], []
    def symbols(path):
        if path not in cache:
            d = load_symbols(path)
            if sha256((ROOT / path).read_bytes()) != d['worktree_sha256']:
                errors.append({'path': path, 'error': 'source hash changed'})
            cache[path] = {x['stable_id']: x for x in d['symbols']}
        return cache[path]
    for file in ledger['files']:
        current = symbols(file['path'])
        previous = symbols(file['predecessor'])
        review_path = WORK / 'Knowledge/Reviews/financial_integrity' / (file['path'] + '.json')
        review = json.loads(review_path.read_text(encoding='utf8'))
        reviewed = {x['stable_id']: x for x in review['symbols'] if x.get('status') == 'ANALYZED'}
        row = {'path': file['path'], 'verified_exact_reused_spans': 0,
               'manual_changed_ids_declared': len(file['manual_changed_stable_ids']),
               'context_claim': file['current_parent_context'], 'errors': []}
        for eq in file['equivalences']:
            a, b = current.get(eq['stable_id']), previous.get(eq['predecessor_stable_id'])
            if not a or not b:
                row['errors'].append('Unknown source identity')
                continue
            ta, tb = span(file['path'], a), span(file['predecessor'], b)
            prior_path = WORK / eq['predecessor_review_path']
            prior = json.loads(prior_path.read_text(encoding='utf8'))
            prior_receipt = next((x for x in prior['symbols'] if x['stable_id'] == eq['predecessor_stable_id']), None)
            if (ta != tb or sha256(ta.encode('utf8')) != eq['source_span_utf8_lf_sha256']
                or sha256(prior_path.read_bytes()) != eq['predecessor_review_sha256']
                or not prior_receipt or prior_receipt.get('status') != 'ANALYZED'
                or not eq['not_fresh_independent_reread']
                or eq['stable_id'] not in reviewed):
                row['errors'].append('Equivalence/provenance mismatch: ' + eq['stable_id'])
            else:
                row['verified_exact_reused_spans'] += 1
        for sid in file['manual_changed_stable_ids']:
            if sid not in current or sid not in reviewed:
                row['errors'].append('Missing changed-block identity/receipt: ' + sid)
        if row['verified_exact_reused_spans'] != file['equivalent_count']:
            row['errors'].append('Count mismatch')
        errors.extend({'path': file['path'], 'error': e} for e in row['errors'])
        checks.append(row)
    result = {'at_utc': now(), 'reviewer': 'root', 'files': checks,
              'equivalence_spans_checked': sum(x['verified_exact_reused_spans'] for x in checks),
              'errors': errors,
              'scope': 'Exact text/hash/stable-ID/predecessor reasoning provenance only; no claim of fresh independent semantic review.',
              'remaining': 'Independent source challenge of changed model contexts and base reasoning; all non-metadata function review gaps remain.'}
    atomic_json(WORK / 'Knowledge/Quality-Reviews/financial-equivalence-parent-validation.json', result)
    print(json.dumps({'files': len(checks), 'equivalent_spans_checked': result['equivalence_spans_checked'], 'errors': errors}))
    if errors:
        raise SystemExit(1)


if __name__ == '__main__':
    main()
