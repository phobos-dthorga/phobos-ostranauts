"""Check coverage and freshness of the reviewed first-party performance ledger, or refresh it.

Verify (default): every C# source under src has one row whose hash, decision and findings are current.
Refresh: recompute hashes, carry unchanged rows verbatim, stamp changed and new files with the given
finding codes (or carry changed files with --carry), drop removed files and recompute the static
indicators of the rows that changed. A changed file with neither a finding nor --carry is an error,
so a refresh can never silently bless unreviewed work.
"""
import argparse
import datetime
import hashlib
import json
from pathlib import Path
import re
import subprocess
import sys

ROOT = Path(__file__).resolve().parents[1]
LEDGER = 'config/performance-audit.json'
REPORT = 'docs/development/performance-audit.md'
REVIEW = ('Static runtime-entry, collection, discovery, persistence/IO and presentation call-site inventory; '
          'disposition reviewed with the report findings.')
DECISION = 'Reviewed in the {date} performance pass; see the linked findings for the disposition.'
ROLES = (
    (re.compile(r'/(Core|Definitions|Content|Registration|Economy|Loot|Stock|AssemblyInfo)', re.I), 'registration, metadata or event-driven content'),
    (re.compile(r'(Panel|View|Presentation|Widget|Controls/)', re.I), 'shared presentation and input'),
    (re.compile(r'/Persistence/|Save|Compatibility|Upgrade', re.I), 'definition/state validation and load compatibility'),
    (re.compile(r'/PhobosAutoNav/', re.I), 'navigation, native boundary or flight policy'),
    (re.compile(r'/PhobosAgriculture/', re.I), 'biology, fluids, native boundary or process policy'),
    (re.compile(r'/PhobosFramework/', re.I), 'shared accounting, inventory, crew or native boundary'),
)
INDICATORS = {
    'runtime_hook': re.compile(r'\[HarmonyPatch|\bvoid (Update|LateUpdate|FixedUpdate|OnGUI)\(|\bPoll\('),
    'discovery': re.compile(r'\bmapCOs\b|\.GetCOs\(|\bdictShips\b|\baBOs\b|GetCOsAtWorldCoords'),
    'collection': re.compile(r'\.Where\(|\.Select\(|\.ToArray\(\)|\.ToList\(\)|\.OrderBy\(|\bnew List<'),
    'persistence_or_io': re.compile(r'\bTryWrite\(|\.Read\(out|mapGUIPropMaps|\bLog\(|File\.'),
    'profiling': re.compile(r'Performance\.(Measure|Increment)\('),
    'ui_mutation': re.compile(r'\.text\s*=|SetActive\(|\.color\s*='),
}


def sources(root):
    return {p.relative_to(root).as_posix(): p for p in (root / 'src').rglob('*.cs')
            if not {'bin', 'obj'}.intersection(p.relative_to(root).parts)}


def digest(path):
    return hashlib.sha256(path.read_text(encoding='utf-8-sig').replace('\r\n', '\n').encode()).hexdigest()


def verify(root=ROOT):
    ledger = json.loads((root / LEDGER).read_text(encoding='utf-8'))
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


def indicators(path):
    """Line numbers of the static call-site kinds the ledger records, per kind, empty kinds omitted."""
    found = {}
    for number, line in enumerate(path.read_text(encoding='utf-8-sig').splitlines(), 1):
        for kind, pattern in INDICATORS.items():
            if pattern.search(line): found.setdefault(kind, []).append(number)
    return found


def role_for(path, explicit=None):
    if explicit: return explicit
    for pattern, role in ROLES:
        if pattern.search('/' + path): return role
    return 'industrial service, native boundary or process policy'


def changed_since(root, since):
    out = subprocess.run(['git', 'diff', '--name-only', since, '--', 'src'], cwd=root, capture_output=True, text=True, check=True).stdout
    return {line.strip().replace('\\', '/') for line in out.splitlines() if line.strip()}


def refresh(root=ROOT, findings=(), carry=False, touched=None, role=None, decision=None, commit=None, date=None):
    """Returns the refreshed ledger and a report; raises ValueError when a changed file has no disposition."""
    date = date or datetime.date.today().isoformat()
    ledger = json.loads((root / LEDGER).read_text(encoding='utf-8'))
    rows = {row['path']: row for row in ledger['files']}
    actual = sources(root)
    report = dict(unchanged=[], stamped=[], carried=[], added=[], removed=[], undisposed=[])
    files = []
    for path in sorted(actual):
        digest_now = digest(actual[path])
        row = rows.get(path)
        stamp = bool(findings) and (touched is None or path in touched)
        if row is not None and row['sha256'] == digest_now:
            files.append(row); report['unchanged'].append(path); continue
        if row is None:
            if not stamp: report['undisposed'].append(path); continue
            row = dict(path=path, sha256=digest_now, role=role_for(path, role), review=REVIEW,
                       decision=decision or DECISION.format(date=date), findings=list(findings), indicators=indicators(actual[path]), reviewed=date)
            files.append(row); report['added'].append(path); continue
        row = dict(row); row['sha256'] = digest_now; row['indicators'] = indicators(actual[path])
        if stamp:
            row['findings'] = list(row['findings']) + [f for f in findings if f not in row['findings']]
            row['decision'] = decision or DECISION.format(date=date); row['reviewed'] = date; row.pop('carried', None)
            report['stamped'].append(path)
        elif carry:
            row['carried'] = date; report['carried'].append(path)
        else:
            report['undisposed'].append(path); continue
        files.append(row)
    report['removed'] = sorted(set(rows) - set(actual))
    if report['undisposed']:
        raise ValueError('Changed or new files without a disposition (give --finding, or --carry for changed files): ' + ', '.join(report['undisposed']))
    ledger['files'] = files
    ledger['date'] = date
    if commit: ledger['baselineCommit'] = commit
    return ledger, report


def missing_findings(root, ledger):
    text = (root / REPORT).read_text(encoding='utf-8') if (root / REPORT).exists() else ''
    codes = sorted({f for row in ledger['files'] for f in row['findings']})
    return [c for c in codes if not re.search(r'\b' + re.escape(c) + r'\b', text)]


def write_ledger(root, ledger):
    (root / LEDGER).write_text(json.dumps(ledger, indent=2, ensure_ascii=False) + '\n', encoding='utf-8')


def main(argv=None):
    parser = argparse.ArgumentParser(description=__doc__.splitlines()[0])
    parser.add_argument('--refresh', action='store_true', help='rewrite the ledger instead of verifying it')
    parser.add_argument('--finding', action='append', default=[], help='finding code stamped on changed and new files (repeatable)')
    parser.add_argument('--since', help='git commit; only files changed since it receive the finding, others need --carry')
    parser.add_argument('--carry', action='store_true', help='carry changed files that receive no finding, recording the date')
    parser.add_argument('--role', help='role text for new rows instead of the path heuristic')
    parser.add_argument('--decision', help='decision text for stamped rows instead of the template')
    parser.add_argument('--commit', help='baseline commit to record (default: git HEAD)')
    parser.add_argument('--date', help='review date (default: today)')
    parser.add_argument('--report', action='store_true', help='print refresh counts and finding codes missing from the report')
    parser.add_argument('--strict', action='store_true', help='with --report, fail when a finding code is missing from the report')
    args = parser.parse_args(argv)
    if not args.refresh:
        result = verify()
        print(json.dumps(result, indent=2))
        return int(bool(result['errors']))
    touched = changed_since(ROOT, args.since) if args.since else None
    commit = args.commit or subprocess.run(['git', 'rev-parse', 'HEAD'], cwd=ROOT, capture_output=True, text=True, check=True).stdout.strip()
    try:
        ledger, report = refresh(ROOT, findings=args.finding, carry=args.carry, touched=touched, role=args.role, decision=args.decision, commit=commit, date=args.date)
    except ValueError as error:
        print(str(error)); return 1
    write_ledger(ROOT, ledger)
    missing = missing_findings(ROOT, ledger)
    if args.report:
        print(json.dumps(dict(status='refreshed', files=len(ledger['files']), counts={k: len(v) for k, v in report.items()},
                              removed=report['removed'], findingsMissingFromReport=missing), indent=2))
    check = verify()
    if check['errors']: print(json.dumps(check, indent=2)); return 1
    return int(bool(missing and args.strict))


if __name__ == '__main__':
    sys.exit(main())
