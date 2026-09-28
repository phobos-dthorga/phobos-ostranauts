"""Verify the reviewed English ledger. No command automatically approves new text."""
from pathlib import Path
import collections
import hashlib
import json
import re
import string
import sys

ROOT = Path(__file__).resolve().parents[1]
LEDGER = ROOT / 'config/english-language-audit.json'

def digest(text):
    return hashlib.sha256(text.replace('\r\n', '\n').encode('utf-8')).hexdigest()

def signature(text):
    return sorted((name, spec, conversion) for _, name, spec, conversion
                  in string.Formatter().parse(text) if name is not None)

def grammar(text):
    return sorted(re.findall(r'\[(?:us|them|crafts|checks)\]', text))

def verify(root=ROOT):
    ledger = json.loads((root / 'config/english-language-audit.json').read_text(encoding='utf-8'))
    errors = []
    actual = {}
    for path in sorted((root / 'translations').glob('*/en.json')):
        for key, value in json.loads(path.read_text(encoding='utf-8-sig')).items():
            actual[(path.parent.name, key)] = value
    reviewed = {(r['mod'], r['key']): r for r in ledger['entries']}
    if len(reviewed) != len(ledger['entries']): errors.append('Duplicate ledger entries')
    for identity in actual.keys() | reviewed.keys():
        row, value = reviewed.get(identity), actual.get(identity)
        if row is None or value is None:
            errors.append('Coverage changed: ' + '/'.join(identity)); continue
        if row['after'] != value: errors.append('Unreviewed wording: ' + '/'.join(identity))
        if signature(row['before']) != signature(value): errors.append('Placeholder contract changed: ' + '/'.join(identity))
        if grammar(row['before']) != grammar(value): errors.append('Native grammar changed: ' + '/'.join(identity))
        if not row['reason'] or not row['references']: errors.append('Missing review evidence: ' + '/'.join(identity))
    for row in ledger['documents'] + ledger['surfaces']:
        path = root / row['path']
        if not path.exists() or digest(path.read_text(encoding='utf-8-sig')) != row['sha256']:
            errors.append('Review is stale: ' + row['path'])
    document_paths = {p.relative_to(root).as_posix() for p in (root / 'docs').rglob('*.md')
                      if p.name not in ('english-language-audit.md', 'player-language.md')}
    document_paths.update(p.relative_to(root).as_posix() for p in (root / 'workshop').glob('*/page.bbcode'))
    document_paths.update(('README.md', 'SUPPORT.md'))
    if document_paths != {r['path'] for r in ledger['documents']}:
        errors.append('Document inventory changed; classify and review the new/removed document')
    counts = collections.Counter(r['mod'] for r in ledger['entries'])
    changed = collections.Counter(r['mod'] for r in ledger['entries'] if r['before'] != r['after'])
    return {'status': 'failed' if errors else 'valid', 'catalogueEntries': len(actual),
            'byMod': dict(counts), 'rewrittenByMod': dict(changed),
            'documents': len(ledger['documents']), 'otherSurfaces': len(ledger['surfaces']), 'errors': errors}

if __name__ == '__main__':
    report = verify()
    print(json.dumps(report, ensure_ascii=False, indent=2))
    sys.exit(bool(report['errors']))
