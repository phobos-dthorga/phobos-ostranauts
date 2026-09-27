"""Check coverage and freshness of the reviewed first-party performance ledger."""
import hashlib
import json
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]


def sources(root):
    return {p.relative_to(root).as_posix(): p for p in (root / 'src').rglob('*.cs')
            if not {'bin', 'obj'}.intersection(p.relative_to(root).parts)}


def digest(path):
    return hashlib.sha256(path.read_text(encoding='utf-8-sig').replace('\r\n', '\n').encode()).hexdigest()


def verify(root=ROOT):
    ledger = json.loads((root / 'config/performance-audit.json').read_text(encoding='utf-8'))
    actual = sources(root)
    rows = {row['path']: row for row in ledger['files']}
    errors = []
    if len(rows) != len(ledger['files']): errors.append('Duplicate source entries')
    for path in sorted(actual.keys() | rows.keys()):
        if path not in actual or path not in rows:
            errors.append('Coverage changed: ' + path)
        elif rows[path]['sha256'] != digest(actual[path]):
            errors.append('Source review stale: ' + path)
        elif not rows[path].get('decision') or not rows[path].get('findings'):
            errors.append('Missing disposition: ' + path)
    return dict(status='failed' if errors else 'valid', sourceFiles=len(actual), errors=errors)


if __name__ == '__main__':
    result = verify()
    print(json.dumps(result, indent=2))
    sys.exit(bool(result['errors']))
